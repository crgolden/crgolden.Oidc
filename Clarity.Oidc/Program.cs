namespace Clarity.Oidc
{
    using System.Threading;
    using System.Threading.Tasks;
    using Core;
    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Hosting;

    public class Program
    {
        public static async Task Main(string[] args)
        {
            using (var tokenSource = new CancellationTokenSource())
            {
                var webHost = WebHost
                    .CreateDefaultBuilder(args)
                    .ConfigureAppConfiguration(configBuilder => configBuilder.AddAzureKeyVault())
                    .ConfigureLogging(loggingBuilder => loggingBuilder.AddLogging())
                    .UseApplicationInsights()
                    .UseStartup<Startup>()
                    .Build();
                await webHost.MigrateDatabaseAsync(tokenSource.Token);
                await webHost.SeedDatabaseAsync();
                await webHost.RunAsync(tokenSource.Token);
            }
        }
    }
}
