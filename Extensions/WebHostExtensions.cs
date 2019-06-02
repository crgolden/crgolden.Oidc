namespace crgolden.Oidc
{
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;

    [ExcludeFromCodeCoverage]
    public static class WebHostExtensions
    {
        public static async Task<IWebHost> SeedDatabaseAsync(this IWebHost webHost)
        {
            using (var scope = webHost.Services.CreateScope())
            {
                var seedService = scope.ServiceProvider.GetRequiredService<ISeedService>();
                await seedService.SeedAsync().ConfigureAwait(false);
            }

            return webHost;
        }
    }
}
