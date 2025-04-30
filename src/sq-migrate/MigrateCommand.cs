using PCAxis.Paxiom;
using PCAxis.Query;
using PxWeb.Api2.Server.Models;
using Spectre.Console;
using Spectre.Console.Cli;
using sq_migrate.Datasource;
using System.ComponentModel;
using System.Text.Json;

namespace sq_migrate
{
    public class MigrateCommand : AsyncCommand<MigrateCommand.Settings>
    {
        public class Settings : CommandSettings
        {
            [CommandOption("-t|--storage-type")]
            [Description("The type of database")]
            [DefaultValue(StorageTypes.File)]
            public StorageTypes StorageType { get; set; }

            [CommandOption("-s|--source-storage-location")]
            [Description("Path where to find old saved queries that should be migrated")]
            public string? Source { get; set; }

            [CommandOption("-d|--destination-storage-location")]
            [Description("Path where to output migrated saved queries")]
            public string? Destination { get; set; }

            [CommandOption("-p|--store-database-path")]
            [Description("Path where the Menu.xml and PX files are located")]
            public string? SourcePath { get; set; }

            [CommandOption("-v|--database-vendor")]
            [Description("The type of relational database")]
            [DefaultValue(DatabaseTypes.MSSQL)]
            public DatabaseTypes DatabaseType { get; set; }

            [CommandOption("-o|--source-database-schema-owner")]
            [Description("The owner of the source database table")]
            [DefaultValue("dbo")]
            public string? SourceSchemaOwner { get; set; }

            [CommandOption("-u|--destination-database-schema-owner")]
            [Description("The owner of the source database table")]
            [DefaultValue("dbo")]
            public string? DestinationSchemaOwner { get; set; }

            [CommandOption("-c|--source-connection-string")]
            [Description("The connection string for the source database")]
            public string? SourceConnectionString { get; set; }


            [CommandOption("-b|--destination-connection-string")]
            [Description("The connection string for the source database")]
            public string? DestinationConnectionString { get; set; }

        }

        public override Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {

            if (settings.StorageType == StorageTypes.File)
            {
                return MigrateFileAsync(context, settings);
            }

            if (settings.StorageType == StorageTypes.Database)
            {
                return MigrateFileAsync(context, settings);
            }

            AnsiConsole.Markup("[red]Failed to initialize storage type[/]");

            return Task.FromResult(-1);
        }

        public async Task<int> MigrateFileAsync(CommandContext context, Settings settings)
        {
            var sourceLocation = AssureSourceLocation(settings.Source);
            var destinationLocation = AssureDestinationLocation(settings.Destination);
            var sourcePath = AssureSourcePath(settings.SourcePath);



            AnsiConsole.Markup($"Source location (Saved queries): [green]{sourceLocation}[/]\n");
            AnsiConsole.Markup($"Destination location: [green]{destinationLocation}[/]\n");

            //TODO Add check for database type PX/CNMM
            AnsiConsole.Markup($"Source path location(PX files): [green]{sourcePath}[/]\n\n");
            var datasource = new PxFileDatasource(sourcePath);



            int counter = 0;

            foreach (var srcFile in Directory.GetFiles(sourceLocation, "*.pxsq", SearchOption.AllDirectories))
            {
                var name = Path.GetFileNameWithoutExtension(srcFile);
                var destFile = Path.Combine(destinationLocation, name.Substring(0, 2), name + ".sq");

                if (File.Exists(destFile))
                {
                    AnsiConsole.Markup($"{name} [blue]Already in destionation[/]\n");
                }
                else
                {
                    // Make sure directory exists for the destination file
                    if (!Directory.Exists(Path.GetDirectoryName(destFile)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
                    }

                    // TODO - Convert and save the file


                    string query = await File.ReadAllTextAsync(srcFile);
                    var sq = JsonHelper.Deserialize<PCAxis.Query.SavedQuery>(query) as PCAxis.Query.SavedQuery;
                    if (sq != null)
                    {
                        sq.LoadedQueryName = Path.GetFileNameWithoutExtension(name);
                    }
                    else
                    {
                        AnsiConsole.Markup($"{name} [red]Failed to parse query[/]\n");
                        continue;
                    }



                    var sqa = Convert(sq, datasource);

                    if (sqa is null)
                    {
                        AnsiConsole.Markup($"{name} [red]Failed to convert query[/]\n");
                        continue;
                    }

                    var savedQueryString = JsonSerializer.Serialize(sqa);
                    File.WriteAllText(destFile, savedQueryString);


                    counter++;
                    AnsiConsole.Markup($"{name} [green]Converted[/]\n");
                }
            }

            return counter;
        }





        private PxWeb.Api2.Server.Models.SavedQuery? Convert(PCAxis.Query.SavedQuery sq, IDatasource datasource)
        {

            //Check that we do not have any operations other then Pivot
            if (sq.Workflow.FirstOrDefault(step => !string.Equals(step.Type, "PIVOT")) != null)
            {
                return null;
            }


            try
            {

                // TODO - Convert the query to the new format
                var sqa = new PxWeb.Api2.Server.Models.SavedQuery();
                sqa.Selection = new PxWeb.Api2.Server.Models.VariablesSelection();
                sqa.Selection.Selection = new List<PxWeb.Api2.Server.Models.VariableSelection>();


                sqa.Language = sq.Sources[0].Language;
                sqa.Id = sq.LoadedQueryName;

                var tableId = datasource.ResolveTableId(sq.Sources[0].Source);
                if (tableId is not null)
                {
                    sqa.TableId = tableId;
                }
                else
                {
                    return null;
                }


                foreach (var query in sq.Sources[0].Quieries)
                {
                    var selection = new PxWeb.Api2.Server.Models.VariableSelection();
                    selection.ValueCodes = new List<string>();
                    selection.VariableCode = query.Code;
                    if (query.Selection.Filter.StartsWith("agg:", StringComparison.OrdinalIgnoreCase) || query.Selection.Filter.StartsWith("vs:", StringComparison.OrdinalIgnoreCase))
                    {
                        selection.CodeList = query.Selection.Filter.Substring(query.Selection.Filter.IndexOf(':'));
                        selection.ValueCodes.AddRange(query.Selection.Values.ToList());
                    }
                    else if (string.Equals(query.Selection.Filter, "TOP", StringComparison.OrdinalIgnoreCase))
                    {
                        selection.ValueCodes.Add($"TOP({query.Selection.Values[0]})");
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

                var builder = datasource.GetBuiler(sq.Sources[0].Source, sqa.Language);
                if (builder is null)
                {
                    AnsiConsole.Markup($"[red]Failed to get builder for {sqa.TableId}[/]");
                    return null;
                }

                builder.BuildForSelection();

                var (format, outputFormatParams) = TranslateOutputFormat(sq.Output.Type);

                sqa.OutputFormat = format;
                sqa.OutputFormatParams = outputFormatParams;

                if (string.Equals(sq.Output.Type, "CSV2", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(sq.Output.Type, "CSV3", StringComparison.OrdinalIgnoreCase))
                {
                    //TODO move everything to the stub last variable should be content variable then time.
                }
                else if (string.Equals(sq.Output.Type, "RELATIONAL_TABLE", StringComparison.OrdinalIgnoreCase))
                {
                    // TODO move all to the stub stub+heading

                }
                else
                {
                    //Set Placement to the last Pivot operation
                    var op = sq.Workflow.LastOrDefault(s => s.Type == "PIVOT");
                    if (op != null)
                    {
                        var placemnt = Convert(op, builder.Model.Meta);
                    }
                }



                return sqa;

            }
            catch (Exception ex)
            {
                AnsiConsole.Markup($"[red]Failed to convert query {sq.LoadedQueryName}[/]");
                AnsiConsole.Markup($"[red]{ex.Message}[/]\n");
            }

            return null;
        }


        private (OutputFormatType, List<OutputFormatParamType>) TranslateOutputFormat(string outputFormat)
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
                    // TODO - Check how we should solve when we should present on the web
                    format = OutputFormatType.JsonStat2Enum;
                    break;
                default:
                    throw new ArgumentException($"Output format {outputFormat} not supported");
            }

            return (format, parameters);
        }

        private VariablePlacementType Convert(WorkStep step, PXMeta meta)
        {
            // TODO - Convert the work step to the new format
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



        public Task<int> MigrateDatabaseAsync(CommandContext context, Settings settings)
        {


            throw new NotImplementedException();
        }


        private static string AssureSourceLocation(string? sourceLocation)
        {
            if (!string.IsNullOrWhiteSpace(sourceLocation) && Directory.Exists(sourceLocation))
            {
                return sourceLocation;
            }

            return AnsiConsole.Prompt(
                new TextPrompt<string>("What's the path to the directory where the saved queries are stored?")
                    .Validate(location
                        => Directory.Exists(location)
                            ? ValidationResult.Success()
                            : ValidationResult.Error("[yellow]Invalid path[/]")));

        }


        private static string AssureSourcePath(string? sourcePath)
        {
            if (!string.IsNullOrWhiteSpace(sourcePath) && Directory.Exists(sourcePath))
            {
                return sourcePath;
            }

            return AnsiConsole.Prompt(
                new TextPrompt<string>("What's the path to the directory where the PX files are stored?")
                    .Validate(location
                        => Directory.Exists(location)
                            ? ValidationResult.Success()
                            : ValidationResult.Error("[yellow]Invalid path[/]")));

        }

        private static string AssureDestinationLocation(string? destinationLocation)
        {
            if (!string.IsNullOrWhiteSpace(destinationLocation))
            {
                return destinationLocation;
            }

            return AnsiConsole.Prompt(
                new TextPrompt<string>("What's the path to the directory where the saved queries are stored?")
                    .Validate(location
                        => Directory.Exists(location)
                            ? ValidationResult.Success()
                            : ValidationResult.Error("[yellow]Invalid path[/]")));

        }

    }
}
