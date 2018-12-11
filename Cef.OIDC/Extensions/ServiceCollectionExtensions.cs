namespace Cef.OIDC.Extensions
{
    using System;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using Core.Extensions;
    using Core.Models;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Options;

    public static class ServiceCollectionExtensions
    {
        public static void AddIdentityServer(this IServiceCollection services,
            IConfiguration configuration, IHostingEnvironment environment)
        {
            var dbContextOptions = configuration.GetDbContextOptions();
            if (environment.IsDevelopment())
            {
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

        public static void AddAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var authenticationOptionsSection = configuration.GetSection(nameof(AuthenticationOptions));
            var authenticationOptions = authenticationOptionsSection.Exists()
                ? authenticationOptionsSection.Get<AuthenticationOptions>()
                : null;

            services.AddAuthentication("Bearer")
                .AddIdentityServerAuthentication("identity", options =>
                {
                    var identityServerAddress = configuration.GetValue<string>("IdentityServerAddress");
                    if (string.IsNullOrEmpty(identityServerAddress)) return;

                    options.Authority = identityServerAddress;
                    options.ApiName = "identity";
                })
                .AddFacebook(options =>
                {
                    if (authenticationOptions?.Facebook == null) return;

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
