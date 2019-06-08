namespace crgolden.Oidc
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using IdentityModel;
    using IdentityServer4;
    using IdentityServer4.EntityFramework.Interfaces;
    using IdentityServer4.EntityFramework.Mappers;
    using IdentityServer4.Models;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Options;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using Shared;

    [ExcludeFromCodeCoverage]
    public class SeedDataService : ISeedService
    {
        private readonly IConfigurationDbContext _configurationDbContext;
        private readonly UserOptions _userOptions;
        private readonly CorsOptions _corsOptions;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;

        public SeedDataService(
            IConfigurationDbContext configurationDbContext,
            IOptions<UserOptions> userOptions,
            IOptions<CorsOptions> corsOptions,
            UserManager<User> userManager,
            RoleManager<Role> roleManager)
        {
            _configurationDbContext = configurationDbContext;
            _corsOptions = corsOptions.Value;
            _userOptions = userOptions.Value;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task SeedAsync()
        {
            if (!await _roleManager.Roles.AnyAsync()) await SeedRolesAsync();
            if (!await _userManager.Users.AnyAsync()) await SeedUsersAsync();
            if (!await _configurationDbContext.Clients.AnyAsync()) await SeedClientsAsync();
            if (!await _configurationDbContext.IdentityResources.AnyAsync()) await SeedIdentityResourcesAsync();
            if (!await _configurationDbContext.ApiResources.AnyAsync()) await SeedApiResourcesAsync();
        }

        private async Task SeedRolesAsync()
        {
            foreach (var role in SeedData.Roles)
            {
                await _roleManager.CreateAsync(role);
            }
        }

        private async Task SeedUsersAsync()
        {
            foreach (var userOption in _userOptions.Users)
            {
                var user = new User
                {
                    UserName = userOption.Email,
                    Email = userOption.Email,
                    SecurityStamp = $"{Guid.NewGuid()}",
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = true,
                    PhoneNumber = userOption.PhoneNumber
                };
                var claims = new List<Claim>
                {
                    new Claim(JwtClaimTypes.GivenName, userOption.FirstName),
                    new Claim(JwtClaimTypes.FamilyName, userOption.LastName),
                    new Claim(JwtClaimTypes.Address, JsonConvert.SerializeObject(userOption.Address, new JsonSerializerSettings
                    {
                        ContractResolver = new CamelCasePropertyNamesContractResolver()
                    }))
                };
                claims.AddRange(SeedData.Roles.Select(x => new Claim(ClaimTypes.Role, x.Name)));

                await _userManager.CreateAsync(user, userOption.Password);
                await _userManager.AddToRolesAsync(user, SeedData.Roles.Select(role => role.Name));
                await _userManager.AddClaimsAsync(user, claims.Union(SeedData.Claims));
            }
        }

        private async Task SeedClientsAsync()
        {
            if (_corsOptions?.Origins != null && _corsOptions.Origins.Any())
            {
                foreach (var origin in _corsOptions.Origins)
                {
                    var client = new Client
                    {
                        ClientId = $"{Guid.NewGuid()}",
                        ClientName = "Angular Client",
                        AllowedGrantTypes = GrantTypes.Implicit,
                        AllowAccessTokensViaBrowser = true,
                        RedirectUris =
                        {
                            $"{origin}/account/login-success",
                            $"{origin}/silent-callback.html"
                        },
                        RequireConsent = false,
                        PostLogoutRedirectUris = { $"{origin}/logout-success" },
                        AllowedCorsOrigins = { origin },
                        AllowedScopes =
                        {
                            IdentityServerConstants.StandardScopes.OpenId,
                            IdentityServerConstants.StandardScopes.Profile,
                            IdentityServerConstants.StandardScopes.Email,
                            IdentityServerConstants.StandardScopes.Phone,
                            IdentityServerConstants.StandardScopes.Address,
                            "api1",
                            "api2.full_access",
                            "api2.read_only",
                            "roles"
                        }
                    };
                    _configurationDbContext.Clients.Add(client.ToEntity());
                }
            }

            await _configurationDbContext.SaveChangesAsync();
        }

        private async Task SeedIdentityResourcesAsync()
        {
            var identityResources = SeedData.IdentityResources.Select(x => x.ToEntity());
            await _configurationDbContext.IdentityResources.AddRangeAsync(identityResources);
            await _configurationDbContext.SaveChangesAsync();
        }

        private async Task SeedApiResourcesAsync()
        {
            var apiResources = SeedData.ApiResources().Select(x => x.ToEntity());
            await _configurationDbContext.ApiResources.AddRangeAsync(apiResources);
            await _configurationDbContext.SaveChangesAsync();
        }
    }

    [ExcludeFromCodeCoverage]
    public static class SeedData
    {
        public static readonly IEnumerable<Claim> Claims = new Claim[]
        {
        };

        public static readonly IEnumerable<Role> Roles = new[]
        {
            new Role("User"),
            new Role("Admin")
        };

        // Identity resources are data like user ID, name, or email address of a user
        // see: http://docs.identityserver.io/en/release/configuration/resources.html
        public static readonly IEnumerable<IdentityResource> IdentityResources = new []
        {
            new IdentityResource("roles", "Your assigned roles", new[] { ClaimTypes.Role }),

            // some standard scopes from the OIDC spec
            new IdentityResources.OpenId(),
            new IdentityResources.Email(),
            new IdentityResources.Profile(),
            new IdentityResources.Phone(),
            new IdentityResources.Address()
        };

        public static IEnumerable<ApiResource> ApiResources() => new[]
        {
            // simple version with ctor
            new ApiResource(
                name: "api1",
                displayName: "Some API 1",
                claimTypes: new[] { ClaimTypes.Role })
            //{
                // this is needed for introspection when using reference tokens
                // ApiSecrets = { }
            /*}*/,

            // expanded version if more control is needed
            new ApiResource
            {
                Name = "api2",

                ApiSecrets =
                {
                    new Secret("secret".Sha256())
                },

                UserClaims =
                {
                    JwtClaimTypes.Name,
                    JwtClaimTypes.Email
                },

                Scopes =
                {
                    new Scope
                    {
                        Name = "api2.full_access",
                        DisplayName = "Full access to API 2"
                    },
                    new Scope
                    {
                        Name = "api2.read_only",
                        DisplayName = "Read only access to API 2"
                    }
                }
            }
        };

        public static readonly IEnumerable<Client> Clients = new[]
        {
            ///////////////////////////////////////////
            // Console Client Credentials Flow Sample
            //////////////////////////////////////////
            new Client
            {
                ClientId = "client",
                ClientSecrets =
                {
                    new Secret("secret".ToSha256())
                },

                AllowedGrantTypes = GrantTypes.ClientCredentials,
                AllowedScopes = { "api1", "api2.read_only" },
                Properties =
                {
                    { "foo", "bar" }
                }
            },

            ///////////////////////////////////////////
            // Console Client Credentials Flow with client JWT assertion
            //////////////////////////////////////////
            new Client
            {
                ClientId = "client.jwt",
                ClientSecrets =
                {
                    new Secret
                    {
                        Type = IdentityServerConstants.SecretTypes.X509CertificateBase64,
                        Value = "MIIDATCCAe2gAwIBAgIQoHUYAquk9rBJcq8W+F0FAzAJBgUrDgMCHQUAMBIxEDAOBgNVBAMTB0RldlJvb3QwHhcNMTAwMTIwMjMwMDAwWhcNMjAwMTIwMjMwMDAwWjARMQ8wDQYDVQQDEwZDbGllbnQwggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQDSaY4x1eXqjHF1iXQcF3pbFrIbmNw19w/IdOQxbavmuPbhY7jX0IORu/GQiHjmhqWt8F4G7KGLhXLC1j7rXdDmxXRyVJBZBTEaSYukuX7zGeUXscdpgODLQVay/0hUGz54aDZPAhtBHaYbog+yH10sCXgV1Mxtzx3dGelA6pPwiAmXwFxjJ1HGsS/hdbt+vgXhdlzud3ZSfyI/TJAnFeKxsmbJUyqMfoBl1zFKG4MOvgHhBjekp+r8gYNGknMYu9JDFr1ue0wylaw9UwG8ZXAkYmYbn2wN/CpJl3gJgX42/9g87uLvtVAmz5L+rZQTlS1ibv54ScR2lcRpGQiQav/LAgMBAAGjXDBaMBMGA1UdJQQMMAoGCCsGAQUFBwMCMEMGA1UdAQQ8MDqAENIWANpX5DZ3bX3WvoDfy0GhFDASMRAwDgYDVQQDEwdEZXZSb290ghAsWTt7E82DjU1E1p427Qj2MAkGBSsOAwIdBQADggEBADLje0qbqGVPaZHINLn+WSM2czZk0b5NG80btp7arjgDYoWBIe2TSOkkApTRhLPfmZTsaiI3Ro/64q+Dk3z3Kt7w+grHqu5nYhsn7xQFAQUf3y2KcJnRdIEk0jrLM4vgIzYdXsoC6YO+9QnlkNqcN36Y8IpSVSTda6gRKvGXiAhu42e2Qey/WNMFOL+YzMXGt/nDHL/qRKsuXBOarIb++43DV3YnxGTx22llhOnPpuZ9/gnNY7KLjODaiEciKhaKqt/b57mTEz4jTF4kIg6BP03MUfDXeVlM1Qf1jB43G2QQ19n5lUiqTpmQkcfLfyci2uBZ8BkOhXr3Vk9HIk/xBXQ="
                    }
                },

                AllowedGrantTypes = GrantTypes.ClientCredentials,
                AllowedScopes = { "api1", "api2.read_only" }
            },

            ///////////////////////////////////////////
            // Custom Grant Sample
            //////////////////////////////////////////
            new Client
            {
                ClientId = "client.custom",
                ClientSecrets =
                {
                    new Secret("secret".Sha256())
                },

                AllowedGrantTypes = { "custom", "custom.nosubject" },
                AllowedScopes = { "api1", "api2.read_only" }
            },

            ///////////////////////////////////////////
            // Console Resource Owner Flow Sample
            //////////////////////////////////////////
            new Client
            {
                ClientId = "roclient",
                ClientSecrets =
                {
                    new Secret("secret".Sha256())
                },

                AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,

                AllowOfflineAccess = true,
                AllowedScopes =
                {
                    IdentityServerConstants.StandardScopes.OpenId,
                    "custom.profile",
                    "api1", "api2.read_only"
                }
            },

            ///////////////////////////////////////////
            // Console Public Resource Owner Flow Sample
            //////////////////////////////////////////
            new Client
            {
                ClientId = "roclient.public",
                RequireClientSecret = false,

                AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,

                AllowOfflineAccess = true,
                AllowedScopes =
                {
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Email,
                    "api1", "api2.read_only"
                }
            },

            ///////////////////////////////////////////
            // Console Hybrid with PKCE Sample
            //////////////////////////////////////////
            new Client
            {
                ClientId = "console.hybrid.pkce",
                ClientName = "Console Hybrid with PKCE Sample",
                RequireClientSecret = false,

                AllowedGrantTypes = GrantTypes.Hybrid,
                RequirePkce = true,

                RedirectUris = { "http://127.0.0.1" },

                AllowOfflineAccess = true,

                AllowedScopes =
                {
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile,
                    IdentityServerConstants.StandardScopes.Email,
                    "api1", "api2.read_only"
                }
            },

            ///////////////////////////////////////////
            // Introspection Client Sample
            //////////////////////////////////////////
            new Client
            {
                ClientId = "roclient.reference",
                ClientSecrets =
                {
                    new Secret("secret".Sha256())
                },

                AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
                AllowedScopes = { "api1", "api2.read_only" },

                AccessTokenType = AccessTokenType.Reference
            },

            ///////////////////////////////////////////
            // MVC Implicit Flow Samples
            //////////////////////////////////////////
            new Client
            {
                ClientId = "mvc.implicit",
                ClientName = "MVC Implicit",
                ClientUri = "http://identityserver.io",

                AllowedGrantTypes = GrantTypes.Implicit,
                AllowAccessTokensViaBrowser = true,

                RedirectUris =  { "http://localhost:44077/signin-oidc" },
                FrontChannelLogoutUri = "http://localhost:44077/signout-oidc",
                PostLogoutRedirectUris = { "http://localhost:44077/signout-callback-oidc" },

                AllowedScopes =
                {
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile,
                    IdentityServerConstants.StandardScopes.Email,
                    "api1", "api2.read_only"
                }
            },

            ///////////////////////////////////////////
            // MVC Manual Implicit Flow Sample
            //////////////////////////////////////////
            new Client
            {
                ClientId = "mvc.manual",
                ClientName = "MVC Manual",
                ClientUri = "http://identityserver.io",

                AllowedGrantTypes = GrantTypes.Implicit,

                RedirectUris = { "http://localhost:44078/home/callback" },
                FrontChannelLogoutUri = "http://localhost:44078/signout-oidc",
                PostLogoutRedirectUris = { "http://localhost:44078/" },

                AllowedScopes = { IdentityServerConstants.StandardScopes.OpenId }
            },

            ///////////////////////////////////////////
            // MVC Hybrid Flow Samples
            //////////////////////////////////////////
            new Client
            {
                ClientId = "mvc.hybrid",
                ClientName = "MVC Hybrid",
                ClientUri = "http://identityserver.io",
                //LogoUri = "https://pbs.twimg.com/profile_images/1612989113/Ki-hanja_400x400.png",

                ClientSecrets =
                {
                    new Secret("secret".Sha256())
                },

                AllowedGrantTypes = GrantTypes.Hybrid,
                AllowAccessTokensViaBrowser = false,

                RedirectUris = { "http://localhost:21402/signin-oidc" },
                FrontChannelLogoutUri = "http://localhost:21402/signout-oidc",
                PostLogoutRedirectUris = { "http://localhost:21402/signout-callback-oidc" },

                AllowOfflineAccess = true,

                AllowedScopes =
                {
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile,
                    IdentityServerConstants.StandardScopes.Email,
                    "api1", "api2.read_only"
                }
            },

            ///////////////////////////////////////////
            // JS OAuth 2.0 Sample
            //////////////////////////////////////////
            new Client
            {
                ClientId = "js_oauth",
                ClientName = "JavaScript OAuth 2.0 Client",
                ClientUri = "http://identityserver.io",
                //LogoUri = "https://pbs.twimg.com/profile_images/1612989113/Ki-hanja_400x400.png",

                AllowedGrantTypes = GrantTypes.Implicit,
                AllowAccessTokensViaBrowser = true,

                RedirectUris = { "http://localhost:28895/index.html" },
                AllowedScopes = { "api1", "api2.read_only" }
            },
                
            ///////////////////////////////////////////
            // JS OIDC Sample
            //////////////////////////////////////////
            new Client
            {
                ClientId = "js_oidc",
                ClientName = "JavaScript OIDC Client",
                ClientUri = "http://identityserver.io",
                //LogoUri = "https://pbs.twimg.com/profile_images/1612989113/Ki-hanja_400x400.png",

                AllowedGrantTypes = GrantTypes.Implicit,
                AllowAccessTokensViaBrowser = true,
                RequireClientSecret = false,
                AccessTokenType = AccessTokenType.Jwt,

                RedirectUris =
                {
                    "http://localhost:7017/index.html",
                    "http://localhost:7017/callback.html",
                    "http://localhost:7017/silent.html",
                    "http://localhost:7017/popup.html"
                },

                PostLogoutRedirectUris = { "http://localhost:7017/index.html" },
                AllowedCorsOrigins = { "http://localhost:7017" },

                AllowedScopes =
                {
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile,
                    IdentityServerConstants.StandardScopes.Email,
                    "api1", "api2.read_only", "api2.full_access"
                }
            }
        };
    }
}