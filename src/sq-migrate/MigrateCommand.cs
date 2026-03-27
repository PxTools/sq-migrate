using Spectre.Console;
using Spectre.Console.Cli;
using sq_migrate.Datasource;
using sq_migrate.StorageBackends;
using System.ComponentModel;

namespace sq_migrate
{
    public class MigrateCommand : AsyncCommand<MigrateCommand.Settings>
    {
        public class Settings : CommandSettings
        {
            [CommandOption("-t|--storeage-type")]
            [Description("The type of database")]
            [DefaultValue(StorageTypes.File)]
            public StorageTypes StorageType { get; set; }

            [CommandOption("-x|--source-type")]
            [Description("The type of database PX/CNMM")]
            [DefaultValue(SourceTypes.PX)]
            public SourceTypes SourceType { get; set; }

            [CommandOption("-s|--source-storage-location")]
            [Description("Path/connection string where to find old saved queries that should be migrated")]
            public string? Source { get; set; }

            [CommandOption("-d|--destination-storage-location")]
            [Description("Path where to output migrated saved queries")]
            public string? Destination { get; set; }

            [CommandOption("-p|--database")]
            [Description("Path where the Menu.xml and PX files are located or Database Id in SqlDb.Config")]
            public string? SourcePath { get; set; }

            [CommandOption("-v|--database-vendor")]
            [Description("The type of relational database")]
            [DefaultValue(DatabaseTypes.MSSQL)]
            public DatabaseTypes DatabaseType { get; set; }

            [CommandOption("-o|--database-schema-owner")]
            [Description("The owner of the database tables")]
            [DefaultValue("dbo")]
            public string? SourceSchemaOwner { get; set; }


        }


        private HashSet<string> _failedQueries = new HashSet<string>();


        public async override Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            try
            {                           

            FileLogger.Info($"Migrate command started. StorageType={settings.StorageType}, SourceType={settings.SourceType}, DatabaseType={settings.DatabaseType}");

            ReadFailedQueries();

            int count = 0;
            if (settings.StorageType == StorageTypes.File)
            {
                count = await MigrateFileAsync(context, settings);
            }

            if (settings.StorageType == StorageTypes.Database)
            {
                count = await MigrateDatabaseAsync(context, settings);
            }

            AnsiConsole.Markup($"[green]{count}[/] queries converted\n");

            WriteFailedQueries();

            FileLogger.Info($"Migrate command completed. ConvertedQueries={count}, FailedQueries={_failedQueries.Count}");

            return 0;

            }
            catch (Exception e)
            {
                FileLogger.Error("An error occurred during migration", e);
                throw;
            }
        }

        private void WriteFailedQueries()
        {
            using var writer = new StreamWriter("failed-queries.txt", false);
            if (_failedQueries.Count > 0)
            {
                foreach (var query in _failedQueries)
                {
                    writer.WriteLine(query);
                }
            }
        }

        private void ReadFailedQueries()
        {
            if (File.Exists("failed-queries.txt"))
            {
                using var reader = new StreamReader("failed-queries.txt");

                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    _failedQueries.Add(line);
                }
            }
        }

        public async Task<int> MigrateFileAsync(CommandContext context, Settings settings)
        {
            var sourceLocation = AssureFileSourceLocation(settings.Source);
            var destinationLocation = AssureFileDestinationLocation(settings.Destination);
            var databasePath = AssureFileSourcePath(settings.SourcePath);
            var sourceType = settings.SourceType;


            AnsiConsole.Markup($"Source location (Saved queries): [green]{sourceLocation}[/]\n");
            AnsiConsole.Markup($"Destination location: [green]{destinationLocation}[/]\n");

            AnsiConsole.Markup($"Source path location(PX files): [green]{databasePath}[/]\n\n");
            var datasource = CreateDatasource(databasePath, sourceType);


            ISaveQueryStorageBackend sourceBackend = new SavedQueryFileStorageBackend(sourceLocation, _failedQueries);
            var destinationBackend = new SavedQueryFileStorageBackend(destinationLocation, _failedQueries);
            return await MigrateQueries(datasource, sourceBackend, destinationBackend);
        }



        public async Task<int> MigrateDatabaseAsync(CommandContext context, Settings settings)
        {
            var sourceConnectionString = AssureDatabaseSourceLocation(settings.Source);
            var databaseId = AssureDatabaseSourcePath(settings.SourcePath);
            var sourceSchemaOwner = settings.SourceSchemaOwner ?? "dbo";
            var databaseType = settings.DatabaseType;
            var storageType = settings.StorageType;
            var sourceType = settings.SourceType;

            AnsiConsole.Markup($"Source location (Saved queries): [green]{sourceConnectionString}[/]\n");
            AnsiConsole.Markup($"Source path location(PX files): [green]{databaseId}[/]\n\n");

            var datasource = CreateDatasource(databaseId, sourceType);

            var dbType = sourceType == SourceTypes.PX ? "PX" : "CNMM";

            var sourceBackend = new SavedQueryDatabaseStorageBackend(databaseType, sourceConnectionString, sourceSchemaOwner, dbType, databaseId, _failedQueries);
            var destinationBackend = new SavedQueryDatabaseStorageBackend(databaseType, sourceConnectionString, sourceSchemaOwner, dbType, databaseId, _failedQueries);

            return await MigrateQueries(datasource, sourceBackend, destinationBackend);
        }

        private static IDatasource CreateDatasource(string databaseId, SourceTypes sourceType)
        {
            IDatasource datasource;
            if (sourceType == SourceTypes.PX)
            {
                datasource = new PxFileDatasource(databaseId);
            }
            else
            {
                datasource = new CnmmDatasource(databaseId);
            }

            return datasource;
        }

        private async Task<int> MigrateQueries(IDatasource datasource, ISaveQueryStorageBackend sourceBackend, ISaveQueryStorageBackend destinationBackend)
        {
            int counter = 0;
            
            await foreach (var sq in sourceBackend.GetSavedQueries())
            {

                // Check if the query has a previouse conversion attempt
                if (_failedQueries.Contains(sq.LoadedQueryName))
                {
                    AnsiConsole.Markup($"{sq.LoadedQueryName} [yellow]Skiped failed in previouse run[/]\n");
                    FileLogger.Info($"Skipped query that failed in previous run: {sq.LoadedQueryName}");
                    continue;
                }

                // Check if the query is already migrated
                if (destinationBackend.AlreadyMigrated(sq.LoadedQueryName))
                {
                    AnsiConsole.Markup($"{sq.LoadedQueryName} [blue]Already in destination[/]\n");
                    FileLogger.Info($"Skipped already migrated query: {sq.LoadedQueryName}");
                    continue;
                }

                // Convert the query to the new format
                var sqa = ConvertUtil.Convert(sq, datasource, _failedQueries);

                // Check if the conversion was successful
                if (sqa is null)
                {
                    AnsiConsole.Markup($"{sq.LoadedQueryName} [red]Failed to convert query[/]\n");
                    FileLogger.Error($"Failed to convert query: {sq.LoadedQueryName}");
                    continue;
                }

                // Save the converted query to the destination
                destinationBackend.StoreMigratedQuery(sqa, datasource);

                AnsiConsole.Markup($"{sq.LoadedQueryName} [green]Converted[/]\n");
                FileLogger.Info($"Converted query: {sq.LoadedQueryName}");

                counter++;
            }

            return counter;
        }


        private static string AssureFileSourceLocation(string? sourceLocation)
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

        private static string AssureDatabaseSourceLocation(string? sourceLocation)
        {
            if (!string.IsNullOrWhiteSpace(sourceLocation))
            {
                return sourceLocation;
            }

            return AnsiConsole.Prompt(
                new TextPrompt<string>("What's the connection string to the database where the saved queries are stored?"));

        }


        private static string AssureFileSourcePath(string? sourcePath)
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

        private static string AssureDatabaseSourcePath(string? sourcePath)
        {
            if (!string.IsNullOrWhiteSpace(sourcePath))
            {
                return sourcePath;
            }

            return AnsiConsole.Prompt(
                new TextPrompt<string>("Type in the name of the database id where the data is stored?"));

        }

        private static string AssureFileDestinationLocation(string? destinationLocation)
        {
            if (!string.IsNullOrWhiteSpace(destinationLocation))
            {
                return destinationLocation;
            }

            return AnsiConsole.Prompt(
                new TextPrompt<string>("What's the path to the directory where to store the migrated queries?"));

        }

    }
}
