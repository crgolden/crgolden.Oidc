namespace Clarity.Oidc
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Security.Claims;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    [ExcludeFromCodeCoverage]
    public static class ServiceCollectionExtensions
    {
        public static void AddIdentityServer(
            this IServiceCollection services,
            IConfiguration configuration,
            IHostingEnvironment environment,
            Action<DbContextOptionsBuilder> dbContextOptions)
        {
            if (environment.IsDevelopment())
            {
                services
                    .AddIdentityServer(config => config.Authentication.CookieLifetime = TimeSpan.FromHours(2))
                    .AddConfigurationStore(options => options.ConfigureDbContext = dbContextOptions)
                    .AddOperationalStore(options => options.ConfigureDbContext = dbContextOptions)
                    .AddDeveloperSigningCredential()
                    .AddAspNetIdentity<User>();
            }
            else if (environment.IsProduction())
            {
                var signingCredential = configuration.GetValue<string>("SigningCredential");
                var validationKey = configuration.GetValue<string>("ValidationKey");

                services
                    .AddIdentityServer(config => config.Authentication.CookieLifetime = TimeSpan.FromHours(2))
                    .AddConfigurationStore(options => options.ConfigureDbContext = dbContextOptions)
                    .AddOperationalStore(options => options.ConfigureDbContext = dbContextOptions)
                    .AddConfigurationStoreCache()
                    .AddSigningCredential(new X509Certificate2(
                        Convert.FromBase64String(signingCredential),
                        (string)null,
                        X509KeyStorageFlags.MachineKeySet))
                    .AddValidationKey(new X509Certificate2(
                        Convert.FromBase64String(validationKey),
                        (string)null,
                        X509KeyStorageFlags.MachineKeySet))
                    .AddAspNetIdentity<User>();
            }
        }

        public static void AddAuthentication(
            this IServiceCollection services,
            IConfiguration configuration,
            string apiName)
        {
            var authBuilder = services.AddAuthentication("Bearer")
                .AddIdentityServerAuthentication("Bearer", options =>
                {
                    var identityServerAddress = configuration.GetValue<string>("IdentityServerAddress");
                    if (string.IsNullOrEmpty(identityServerAddress)) return;

                    options.Authority = identityServerAddress;
                    options.ApiName = apiName;
                    options.RoleClaimType = ClaimTypes.Role;
                });
            var authenticationOptionsSection = configuration.GetSection(nameof(AuthenticationOptions));
            var authenticationOptions = authenticationOptionsSection.Exists()
                ? authenticationOptionsSection.Get<AuthenticationOptions>()
                : null;
            if (authenticationOptions?.FacebookOptions != null)
            {
                authBuilder.AddFacebook(options =>
                {
                    options.AppId = authenticationOptions.FacebookOptions.AppId;
                    options.AppSecret = authenticationOptions.FacebookOptions.AppSecret;
                    options.SignInScheme = IdentityConstants.ExternalScheme;
                    options.Events.OnRemoteFailure = context =>
                    {
                        context.Response.Redirect($"{context.Properties.Items["Referer"]}");
                        context.HandleResponse();
                        return Task.CompletedTask;
                    };
                });
            }
        }
    }
}
