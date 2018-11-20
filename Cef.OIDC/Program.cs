namespace Cef.OIDC
{
    using Cef.Core.Extensions;
    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Azure.KeyVault;
    using Microsoft.Azure.Services.AppAuthentication;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Configuration.AzureKeyVault;

    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args)
                .Build()
                .SeedDatabaseAsync()
                .Result.Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
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
                .UseStartup<Startup>();
    }
}
