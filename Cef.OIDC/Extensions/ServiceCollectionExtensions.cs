namespace Cef.OIDC.Extensions
{
    using System;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using Core.Extensions;
    using Core.Models;
    using Core.Options;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    public static class ServiceCollectionExtensions
    {
        public static void AddIdentityServerDevelopment(this IServiceCollection services, IConfiguration configuration)
        {
            var dbContextOptions = configuration.GetDbContextOptions();
            services
                .AddIdentityServer(config => config.Authentication.CookieLifetime = TimeSpan.FromHours(2))
                .AddConfigurationStore(options => options.ConfigureDbContext = dbContextOptions)
                .AddOperationalStore(options =>
                {
                    options.ConfigureDbContext = dbContextOptions;
                    options.EnableTokenCleanup = true;
                    options.TokenCleanupInterval = 30;
                })
                .AddDeveloperSigningCredential()
                .AddAspNetIdentity<User>();
        }

        public static void AddIdentityServerProduction(this IServiceCollection services, IConfiguration configuration)
        {
            var dbContextOptions = configuration.GetDbContextOptions();
            var signingCredential = configuration.GetValue<string>("SigningCredential");
            var validationKey = configuration.GetValue<string>("ValidationKey");

            services
                .AddIdentityServer(config => config.Authentication.CookieLifetime = TimeSpan.FromHours(2))
                .AddConfigurationStore(options => options.ConfigureDbContext = dbContextOptions)
                .AddOperationalStore(options => options.ConfigureDbContext = dbContextOptions)
                .AddConfigurationStoreCache()
                .AddSigningCredential(new X509Certificate2(
                    Convert.FromBase64String(signingCredential),
                    (string) null,
                    X509KeyStorageFlags.MachineKeySet))
                .AddValidationKey(new X509Certificate2(
                    Convert.FromBase64String(validationKey),
                    (string) null,
                    X509KeyStorageFlags.MachineKeySet))
                .AddAspNetIdentity<User>();
        }

        public static void AddAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var authenticationOptionsSection = configuration.GetSection(nameof(AuthenticationOptions));
            if (!authenticationOptionsSection.Exists()) return;

            var authenticationOptions = authenticationOptionsSection.Get<AuthenticationOptions>();
            if (authenticationOptions?.Facebook == null) return;

            services.AddAuthentication()
                .AddIdentityServerAuthentication("token", options =>
                {
                    options.Authority = "https://clarity-oidc.azurewebsites.net";
                    options.ApiName = "api1";
                })
                .AddFacebook(options =>
                {
                    options.AppId = authenticationOptions.Facebook.AppId;
                    options.AppSecret = authenticationOptions.Facebook.AppSecret;
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
