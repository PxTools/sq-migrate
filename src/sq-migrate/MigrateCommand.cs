using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

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
            public string? SourceDatabase { get; set; }

            [CommandOption("-d|--destination-storage-location")]
            [Description("Path where to output migrated saved queries")]
            public string? DestinationDatabase { get; set; }

            [CommandOption("-v|--database-vendor")]
            [Description("The type of relational database")]
            [DefaultValue(DatabaseTypes.MSSQL)]
            public DatabaseTypes DatabaseType { get; set; }

            [CommandOption("-o|--source-database-schema-owner")]
            [Description("The owner of the source database table")]
            [DefaultValue("dbo")]
            public string? SourceSchemaOwner { get; set; }

            [CommandOption("-p|--destination-database-schema-owner")]
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

        public Task<int> MigrateFileAsync(CommandContext context, Settings settings)
        {
            var sourceLocation = AssureSourceLocation(settings.SourceDatabase);

            AnsiConsole.Markup($"Source location: [green]{sourceLocation}[/]");

            throw new NotImplementedException();
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
