namespace Clarity.Oidc
{
    using System.Threading;
    using System.Threading.Tasks;
    using Core;
    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Hosting;

    public class Program
    {
        // https://docs.microsoft.com/en-us/aspnet/core/migration/1x-to-2x/#update-main-method-in-programcs
        public static async Task Main(string[] args)
        {
            var webHost = BuildWebHost(args);
            using (var tokenSource = new CancellationTokenSource())
            {
                await webHost.MigrateDatabaseAsync(tokenSource.Token);
                await webHost.SeedDatabaseAsync();
                await webHost.RunAsync(tokenSource.Token);
            }
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost
                .CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(configBuilder => configBuilder.AddAzureKeyVault())
                .UseSerilog("Clarity.Oidc")
                .UseStartup<Startup>()
                .Build();
    }
}
