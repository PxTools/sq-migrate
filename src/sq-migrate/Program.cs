using Spectre.Console.Cli;

namespace sq_migrate
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            var app = new CommandApp();
            app.Configure(config =>
            {
                config.AddCommand<StatsCommand>("stats");
            });

            return await app.RunAsync(args);
        }
    }
}
