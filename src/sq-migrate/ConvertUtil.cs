using PCAxis.Paxiom;
using PCAxis.Query;
using PxWeb.Api2.Server.Models;
using Spectre.Console;
using sq_migrate.Datasource;

namespace sq_migrate
{
    internal class ConvertUtil
    {
        public static PxWeb.Api2.Server.Models.SavedQuery? Convert(PCAxis.Query.SavedQuery sq, IDatasource datasource, HashSet<string> failedQueries)
        {

            // Check that we do not have any operations other then Pivot since they are not implemented in the API yet.
            if (sq.Workflow.FirstOrDefault(step => !string.Equals(step.Type, "PIVOT")) != null)
            {
                return null;
            }

            try
            {
                // Init the saved query 
                var sqa = new PxWeb.Api2.Server.Models.SavedQuery();
                sqa.Selection = new PxWeb.Api2.Server.Models.VariablesSelection();
                sqa.Selection.Selection = new List<PxWeb.Api2.Server.Models.VariableSelection>();


                sqa.Language = sq.Sources[0].Language;
                sqa.Id = sq.LoadedQueryName;

                // Resolves the TableId from the datasource
                var tableId = datasource.ResolveTableId(sq.Sources[0].Source);
                if (tableId is not null)
                {
                    sqa.TableId = tableId;
                }
                else
                {
                    return null;
                }

                // Read in the metadata for the table
                var builder = datasource.GetBuiler(sq.Sources[0].Source, sqa.Language);
                if (builder is null)
                {
                    AnsiConsole.Markup($"[red]Failed to get builder for {sqa.TableId}[/]");
                    return null;
                }

                builder.BuildForSelection();

                // Loop throw all the variables in the old svaed query and add them to the new saved query
                foreach (var query in sq.Sources[0].Quieries)
                {
                    var selection = new PxWeb.Api2.Server.Models.VariableSelection();
                    selection.ValueCodes = new List<string>();
                    selection.VariableCode = query.Code;
                    if (query.Selection.Filter.StartsWith("agg:", StringComparison.OrdinalIgnoreCase))
                    {
                        selection.CodeList = "agg_" + query.Selection.Filter.Substring(query.Selection.Filter.IndexOf(':') + 1);
                        selection.ValueCodes.AddRange(query.Selection.Values.ToList());
                    }
                    else if (query.Selection.Filter.StartsWith("vs:", StringComparison.OrdinalIgnoreCase))
                    {
                        selection.CodeList = "vs_" + query.Selection.Filter.Substring(query.Selection.Filter.IndexOf(':') + 1);
                        selection.ValueCodes.AddRange(query.Selection.Values.ToList());
                    }
                    else if (string.Equals(query.Selection.Filter, "TOP", StringComparison.OrdinalIgnoreCase))
                    {
                        selection.ValueCodes.Add($"TOP({query.Selection.Values[0]})");
                    }
                    else if (string.Equals(query.Selection.Filter, "FROM", StringComparison.OrdinalIgnoreCase))
                    {
                        selection.ValueCodes.Add($"FROM({query.Selection.Values[0]})");
                    }
                    else if (string.Equals(query.Selection.Filter, "ALL", StringComparison.OrdinalIgnoreCase))
                    {
                        selection.ValueCodes.Add(query.Selection.Values[0]);
                    }
                    else
                    {
                        selection.ValueCodes.AddRange(query.Selection.Values.ToList());
                    }
                    sqa.Selection.Selection.Add(selection);
                }

                // Check if we have variables that might have been removed from the old saved query Eliminate SingleContents and variables that are not eliminaable
                var definedVariableCodes = new HashSet<string>(sq.Sources[0].Quieries.Select(q => q.Code));
                foreach (var variable in builder.Model.Meta.Variables)
                {
                    if (!definedVariableCodes.Contains(variable.Code))
                    {
                        var selection = new PxWeb.Api2.Server.Models.VariableSelection();
                        selection.ValueCodes = new List<string>();
                        selection.VariableCode = variable.Code;
                        selection.ValueCodes.Add("*");
                        sqa.Selection.Selection.Add(selection);
                    }
                }



                var (format, outputFormatParams) = TranslateOutputFormat(sq.Output.Type);

                sqa.OutputFormat = format;
                sqa.OutputFormatParams = outputFormatParams;

                if (string.Equals(sq.Output.Type, "CSV2", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(sq.Output.Type, "CSV3", StringComparison.OrdinalIgnoreCase))
                {
                    // Move everything to the stub last variable should be time then contents.
                    sqa.Selection.Placement = GetCsvPlacement(builder.Model.Meta);
                }
                else if (string.Equals(sq.Output.Type, "RELATIONAL_TABLE", StringComparison.OrdinalIgnoreCase))
                {
                    //Move all variables in the heading to the stub
                    sqa.Selection.Placement = GetRelationalPlacement(builder.Model.Meta);
                }
                else
                {
                    //Set Placement to the last Pivot operation
                    var op = sq.Workflow.LastOrDefault(s => s.Type == "PIVOT");
                    if (op != null)
                    {
                        var placemnt = Convert(op, builder.Model.Meta);
                        sqa.Selection.Placement = placemnt;
                    }
                }

                return sqa;

            }
            catch (Exception ex)
            {
                AnsiConsole.Markup($"[red]Failed to convert query: {sq.LoadedQueryName} [/]");
                AnsiConsole.Markup($"[red] {ex.Message}[/]\n");
                failedQueries.Add(sq.LoadedQueryName);
            }

            return null;
        }

        private static VariablePlacementType GetCsvPlacement(PXMeta meta)
        {
            var placement = new VariablePlacementType();
            placement.Heading = new List<string>();
            placement.Stub = new List<string>();

            // Add all variables to the stub except time and contents

            placement.Stub.AddRange(meta.Variables
                .Where(v => (!v.IsTime) && (!v.IsContentVariable))
                .Select(v => v.Code));

            // Add time variable to stub
            var time = meta.Variables.FirstOrDefault(v => v.IsTime);
            if (time is not null)
            {
                placement.Stub.Add(time.Code);
            }

            // Add contents variable to stub
            var contents = meta.Variables.FirstOrDefault(v => v.IsContentVariable);
            if (contents is not null)
            {
                placement.Stub.Add(contents.Code);
            }

            return placement;
        }

        private static VariablePlacementType GetRelationalPlacement(PXMeta meta)
        {
            var placement = new VariablePlacementType();
            placement.Heading = new List<string>();
            placement.Stub = new List<string>();

            // Add all variables from stub to stub
            if (meta.Stub.Count > 0)
            {
                placement.Stub.AddRange(meta.Stub.Select(v => v.Code));
            }

            // Add all variables from heading to stub
            if (meta.Heading.Count > 0)
            {
                placement.Stub.AddRange(meta.Heading.Select(v => v.Code));
            }

            return placement;
        }


        private static (OutputFormatType, List<OutputFormatParamType>) TranslateOutputFormat(string outputFormat)
        {
            outputFormat = outputFormat.ToUpper();
            OutputFormatType format = OutputFormatType.PxEnum;
            var parameters = new List<OutputFormatParamType>();
            switch (outputFormat)
            {
                case "FILETYPEEXCELX":
                    format = OutputFormatType.XlsxEnum;
                    parameters.Add(OutputFormatParamType.IncludeTitleEnum);
                    break;
                case "FILETYPEEXCELXDOUBLECOLUMN":
                    format = OutputFormatType.XlsxEnum;
                    parameters.Add(OutputFormatParamType.IncludeTitleEnum);
                    parameters.Add(OutputFormatParamType.UseCodesAndTextsEnum);
                    break;
                case "FILETYPECSVWITHOUTHEADINGANDTABULATOR":
                    format = OutputFormatType.CsvEnum;
                    parameters.Add(OutputFormatParamType.SeparatorTabEnum);
                    break;
                case "FILETYPECSVWITHHEADINGANDTABULATOR":
                    format = OutputFormatType.CsvEnum;
                    parameters.Add(OutputFormatParamType.SeparatorTabEnum);
                    parameters.Add(OutputFormatParamType.IncludeTitleEnum);
                    break;
                case "FILETYPECSVWITHOUTHEADINGANDCOMMA":
                    format = OutputFormatType.CsvEnum;
                    // Default separator is comma
                    break;
                case "FILETYPECSVWITHHEADINGANDCOMMA":
                    format = OutputFormatType.CsvEnum;
                    // Default separator is comma
                    parameters.Add(OutputFormatParamType.IncludeTitleEnum);
                    break;
                case "FILETYPECSVWITHOUTHEADINGANDSPACE":
                    format = OutputFormatType.CsvEnum;
                    parameters.Add(OutputFormatParamType.SeparatorSpaceEnum);
                    break;
                case "FILETYPECSVWITHHEADINGANDSPACE":
                    format = OutputFormatType.CsvEnum;
                    parameters.Add(OutputFormatParamType.SeparatorSpaceEnum);
                    parameters.Add(OutputFormatParamType.IncludeTitleEnum);
                    break;
                case "FILETYPECSVWITHOUTHEADINGANDSEMICOLON":
                    format = OutputFormatType.CsvEnum;
                    parameters.Add(OutputFormatParamType.SeparatorSemicolonEnum);
                    parameters.Add(OutputFormatParamType.IncludeTitleEnum);
                    break;
                case "FILETYPECSVWITHHEADINGANDSEMICOLON":
                    format = OutputFormatType.CsvEnum;
                    parameters.Add(OutputFormatParamType.SeparatorSemicolonEnum);
                    parameters.Add(OutputFormatParamType.IncludeTitleEnum);
                    break;
                case "FILETYPECSV2":
                    format = OutputFormatType.CsvEnum;
                    // Comma separated
                    parameters.Add(OutputFormatParamType.UseTextsEnum);
                    break;
                case "FILETYPECSV3":
                    format = OutputFormatType.CsvEnum;
                    // Comma separated
                    parameters.Add(OutputFormatParamType.UseCodesEnum);
                    break;
                case "FILETYPEJSON":
                    format = OutputFormatType.JsonPxEnum;
                    break;
                case "FILETYPEJSONSTAT":
                    throw new ArgumentException($"Output format JSON-STAT not longer supported");
                case "FILETYPEJSONSTAT2":
                    format = OutputFormatType.JsonStat2Enum;
                    break;
                case "FILETYPEHTML5TABLE":
                    format = OutputFormatType.HtmlEnum;
                    break;
                case "FILETYPERELATIONAL":
                    format = OutputFormatType.CsvEnum;
                    parameters.Add(OutputFormatParamType.UseTextsEnum);
                    parameters.Add(OutputFormatParamType.SeparatorTabEnum);
                    break;
                case "FILETYPEPX":
                    format = OutputFormatType.PxEnum;
                    break;
                case "TABLE":
                case "CHART":
                    format = OutputFormatType.JsonStat2Enum;
                    break;
                default:
                    throw new ArgumentException($"Output format {outputFormat} not supported");
            }

            return (format, parameters);
        }

        private static VariablePlacementType Convert(WorkStep step, PXMeta meta)
        {
            if (!string.Equals(step.Type, "PIVOT", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Only pivot steps are vaild");
            }

            var placement = new VariablePlacementType();
            placement.Heading = new List<string>();
            placement.Stub = new List<string>();

            if (!int.TryParse(step.Params["_count"], out int count))
            {
                throw new ArgumentException("Count is not a number");
            }

            for (int i = 0; i < count; i++)
            {
                var variableName = step.Params[$"{i}.name"];
                var variablePlacment = step.Params[$"{i}.placement"];

                // Convert the variable name to the variable code
                var variableCode = meta.Variables.Where(v => string.Equals(v.Name, variableName, StringComparison.OrdinalIgnoreCase))
                    .Select(v => v.Code)
                    .FirstOrDefault();

                // Check if the variable code is valid
                if (string.IsNullOrWhiteSpace(variableCode))
                {
                    throw new ArgumentException($"Variable {variableName} not found");
                }

                // Add the variable code to the placement
                if (string.Equals(variablePlacment, "Heading", StringComparison.OrdinalIgnoreCase))
                {
                    placement.Heading.Add(variableCode);
                }
                else if (string.Equals(variablePlacment, "Stub", StringComparison.OrdinalIgnoreCase))
                {
                    placement.Stub.Add(variableCode);
                }
                else
                {
                    throw new ArgumentException($"Placement {variablePlacment} not found");
                }

            }

            return placement;
        }
    }
}
