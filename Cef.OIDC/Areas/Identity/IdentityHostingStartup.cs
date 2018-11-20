using Microsoft.AspNetCore.Hosting;

[assembly: HostingStartup(typeof(Cef.OIDC.Areas.Identity.IdentityHostingStartup))]
namespace Cef.OIDC.Areas.Identity
{
    public class IdentityHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
        }
    }
}