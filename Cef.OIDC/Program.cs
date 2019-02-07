namespace Cef.OIDC
{
    using System.Threading.Tasks;
    using Cef.Core.Extensions;
    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Azure.KeyVault;
    using Microsoft.Azure.Services.AppAuthentication;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Configuration.AzureKeyVault;
    using Microsoft.Extensions.Logging;

    public class Program
    {
        public static async Task Main(string[] args)
        {
            var webHost = CreateWebHostBuilder(args).Build();
            await webHost.MigrateDatabaseAsync();
            await webHost.SeedDatabaseAsync();
            await webHost.RunAsync();
        }

        private static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, configBuilder) =>
                {
                    var configRoot = configBuilder.Build();
                    var keyVaultName = configRoot.GetValue<string>("KeyVaultName");
                    if (string.IsNullOrEmpty(keyVaultName)) { return; }

                    var azureServiceTokenProvider = new AzureServiceTokenProvider();
                    var keyVaultClient = new KeyVaultClient(
                        authenticationCallback: new KeyVaultClient.AuthenticationCallback(
                            azureServiceTokenProvider.KeyVaultTokenCallback));
                    configBuilder.AddAzureKeyVault(
                        vault: $"https://{keyVaultName}.vault.azure.net/",
                        client: keyVaultClient,
                        manager: new DefaultKeyVaultSecretManager());
                })
                .ConfigureLogging((context, loggingBuilder) =>
                {
                    loggingBuilder.AddAzureWebAppDiagnostics();
                    loggingBuilder.AddApplicationInsights();
                })
                .UseStartup<Startup>();
    }
}
