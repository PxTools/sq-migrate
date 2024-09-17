using PCAxis.Query;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using ValidationResult = Spectre.Console.ValidationResult;
using System.Text;

namespace sq_migrate
{
    public class StatsCommand : AsyncCommand<StatsCommand.Settings>
    {
        public class Settings : CommandSettings
        {
            [CommandOption("-t|--storage-type")]
            [Description("The type of database")]
            [DefaultValue(StorageTypes.File)]
            public StorageTypes StorageType { get; set; }

            [CommandOption("-l|--storage-location")]
            [Description("Search math to the database in case of file based storage")]
            public string? Database { get; set; }

            [CommandOption("-d|--database-type")]
            [Description("The type of relational database")]
            [DefaultValue(DatabaseTypes.MSSQL)]
            public DatabaseTypes DatabaseType { get; set; }

            [CommandOption("-o|--database-schema-owner")]
            [Description("The type of relational database")]
            [DefaultValue("dbo")]
            public string? DatabaseSchemaOwner { get; set; }

            [CommandOption("-c|--database-connection-string")]
            [Description("The type of relational database")]
            public string? ConnectionString { get; set; }

            [CommandOption("-f|--output-file")]
            [Description("Name of the file to put the information to.")]
            [DefaultValue("stats.csv")]
            public string? FileOutput { get; set; }
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            AnsiConsole.Write(
                new FigletText("sq migrate")
                    .LeftJustified()
                    .Color(Color.Green));

            var type = settings.StorageType;

            IStatsBackend statsBackend = GetBackend(settings);

            if (statsBackend is null)
            {
                AnsiConsole.Markup("[red]Failed to initialize the backend[/]");
                return 1;
            }

            // Fetching the numer of queries
            var numberOfQueries = 0;
            AnsiConsole
            .Status()
            .Spinner(Spinner.Known.Default)
            .Start(
                "Finding saved queries...",
                _ => numberOfQueries = statsBackend.NumberOfQueries());

            AnsiConsole.Markup($"Found [green]{numberOfQueries}[/] saved queries");

            var writer = new StreamWriter(settings.FileOutput ?? "stats.csv", Encoding.UTF8, new FileStreamOptions() {Mode = FileMode.Create, Access = FileAccess.ReadWrite});
            writer.WriteLine($"LoadedQueryName;LastExecuted;RunCounter;FailCounter;NumberOfPerPart;NumberOfChangeValueOrder;NumberOfDeleteValue;NumberOfDeleteVariable;NumberOfSortTime;NumberOfSplitTime;NumberOfSum;NumberOfChangeDecimals;NumberOfChangeText;NumberOfPivotTimeToHeading;NumberOfChangeCodeTextPresentation");
            var statsResults = await AnsiConsole
                .Progress()
                .StartAsync(async ctx =>
                {
                    var migrationTask = ctx.AddTask(
                        "Extractiong statistics...",
                        maxValue: numberOfQueries);

                    var successes = 0;
                    var failures = 0;

                    await foreach (var query in statsBackend.GetQueries())
                    {
                        writer.WriteLine($"{query.LoadedQueryName};{query.Stats.LastExecuted};{query.Stats.RunCounter};{query.Stats.FailCounter};{query.NumberOfPerPart()};{query.NumberOfChangeValueOrder()};{query.NumberOfDeleteValue()};{query.NumberOfDeleteVariable()};{query.NumberOfSortTime()};{query.NumberOfSplitTime()};{query.NumberOfSum()};{query.NumberOfChangeDecimals()};{query.NumberOfChangeText()};{query.NumberOfPivotTimeToHeading()};{query.NumberOfChangeCodeTextPresentation()}");
                        migrationTask.Increment(1);
                    }

                    return (successes, failures);
                });

            writer.Close();

            return 0;
        }


        private static IStatsBackend GetBackend(Settings settings)
        {
            if (settings.StorageType == StorageTypes.Database)
            {
                var connectionString = AssureConnectionString(settings.ConnectionString);

                if (settings.DatabaseType == DatabaseTypes.MSSQL)
                {
                    return new StatsDatabaseBackend(connectionString, settings.DatabaseSchemaOwner ?? "dbo", new MSSqlDbProvider());
                }
                else
                {
                    return new StatsDatabaseBackend(connectionString, settings.DatabaseSchemaOwner ?? "dbo", new OracleDbProvider());
                }
            }
            
            var location = AssureStorageLocation(settings.Database);
            return new StatsFileBackend(location);
        }

        private static string AssureConnectionString(string? connectionString)
        {
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                return connectionString;
            }

            return AnsiConsole.Prompt(
                new TextPrompt<string>("What's the connection string for the database?")
                    .Validate(location
                        => !string.IsNullOrWhiteSpace(location)
                            ? ValidationResult.Success()
                            : ValidationResult.Error("[yellow]Invalid connection string[/]")));
            
        }

        private static string AssureStorageLocation(string? location)
        {
            if (!string.IsNullOrWhiteSpace(location))
            {
                return location;
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
