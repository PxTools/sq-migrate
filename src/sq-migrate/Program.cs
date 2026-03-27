using PCAxis.Sql.ApiUtils;
using PCAxis.Sql.DbConfig;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Collections;
using System.Runtime.Intrinsics.X86;
using static PCAxis.Paxiom.Settings;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace sq_migrate
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            FileLogger.Info($"Application starting. Args: {string.Join(' ', args)}");

            


            //args = new string[] {
            //    "migrate",
            //    "-t", "Database",
            //    "-x", "cnmm",
            //    "-s", "Data Source=ssdyttre.test.sql;Initial Catalog=SDB_MetabasVy23;Integrated Security=True; enlist=false;",
            //    "-p", "ssd",
            //    "-v", "mssql",
            //    "-d", "Data Source=ssdyttre.test.sql;Initial Catalog=SDB_MetabasVy23;Integrated Security=True; enlist=false;"
            //};




            var app = new CommandApp();
            app.Configure(config =>
            {
                config.AddCommand<StatsCommand>("stats");
                config.AddCommand<MigrateCommand>("migrate");
            });

            try
            {
                var exitCode = await app.RunAsync(args);
                FileLogger.Info($"Application finished with exit code {exitCode}");
                return exitCode;
            }
            catch (Exception ex)
            {
                FileLogger.Error("Unhandled exception in application", ex);
                AnsiConsole.MarkupLine($"[red]An unhandled error occurred. See log file:[/] {FileLogger.LogFilePath}");
                return 1;
            }
        }
    }
}
