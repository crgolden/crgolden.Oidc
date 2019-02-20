namespace Clarity.Oidc.Extensions
{
    using System.Threading.Tasks;
    using Interfaces;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;

    public static class WebHostExtensions
    {
        public static async Task<IWebHost> SeedDatabaseAsync(this IWebHost webHost)
        {
            using (var scope = webHost.Services.CreateScope())
            {
                var seedService = scope.ServiceProvider.GetRequiredService<ISeedService>();
                await seedService.SeedAsync();
            }

            return webHost;
        }
    }
}
