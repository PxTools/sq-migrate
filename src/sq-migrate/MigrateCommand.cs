using PCAxis.Paxiom;
using PCAxis.Query;
using PxWeb.Api2.Server.Models;
using Spectre.Console;
using Spectre.Console.Cli;
using sq_migrate.Datasource;
using System.ComponentModel;
using System.Text.Json;
using System.Xml;

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


            var lookup = GetMap(sourcePath);

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



                    var sqa = Convert(sq, lookup, datasource);

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

        private static Dictionary<string, string> GetMap(string sourcePath)
        {
            var lookup = new Dictionary<string, string>();
            var menuFile = Path.Combine(sourcePath, "Menu.xml");

            var xdoc = new XmlDocument();
            xdoc.Load(menuFile);

            string xpath = "//Link";
            var nodeList = xdoc.SelectNodes(xpath);

            if (nodeList != null)
            {
                foreach (XmlElement childEl in nodeList)
                {
                    string selection = childEl.GetAttribute("selection").Replace('\\', '/');
                    string tableId = childEl.GetAttribute("tableId").ToUpper();
                    if (!lookup.ContainsKey(selection))
                    {
                        lookup.Add(selection, tableId);
                    }
                }

            }

            return lookup;
        }


        private PxWeb.Api2.Server.Models.SavedQuery? Convert(PCAxis.Query.SavedQuery sq, Dictionary<string, string> lookup, IDatasource datasource)
        {
            // TODO - Convert the query to the new format
            var sqa = new PxWeb.Api2.Server.Models.SavedQuery();
            sqa.Selection = new PxWeb.Api2.Server.Models.VariablesSelection();
            sqa.Selection.Selection = new List<PxWeb.Api2.Server.Models.VariableSelection>();


            sqa.Language = sq.Sources[0].Language;
            sqa.Id = sq.LoadedQueryName;

            if (lookup.ContainsKey(sq.Sources[0].Source))
            {
                sqa.TableId = lookup[sq.Sources[0].Source];
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

            //Set Placement to the last Pivot operation
            var op = sq.Workflow.LastOrDefault(s => s.Type == "PIVOT");
            if (op != null)
            {
                var placemnt = Convert(op, builder.Model.Meta);
            }

            return sqa;
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
