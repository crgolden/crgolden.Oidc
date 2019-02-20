namespace Clarity.Oidc.Tests.Pages.Clients.Edit
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Data;
    using IdentityServer4.EntityFramework.Entities;
    using IdentityServer4.EntityFramework.Interfaces;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;
    using Moq;
    using Oidc.Pages.Clients.Edit;
    using Xunit;

    public class RedirectUrisFacts
    {
        private static Random Random => new Random();
        private static string DatabaseNamePrefix => typeof(RedirectUrisFacts).FullName;

        [Fact]
        public async Task OnGetAsync()
        {
            // Arrange
            var databaseName = $"{DatabaseNamePrefix}.{nameof(OnGetAsync)}";
            var options = new DbContextOptionsBuilder<OidcDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;
            var client = new Client
            {
                Id = Random.Next(),
                RedirectUris = new List<ClientRedirectUri>
                {
                    new ClientRedirectUri(),
                    new ClientRedirectUri(),
                    new ClientRedirectUri()
                }
            };
            RedirectUrisModel model;
            IActionResult get;
            using (var context = new OidcDbContext(options))
            {
                context.Add(client);
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new OidcDbContext(options))
            {
                model = new RedirectUrisModel(context);
                get = await model.OnGetAsync(client.Id);
            }

            // Assert
            Assert.NotNull(model.Client);
            Assert.Equal(client.Id, model.Client.Id);
            var redirectUris = Assert.IsAssignableFrom<IEnumerable<ClientRedirectUri>>(model.RedirectUris);
            Assert.Equal(client.RedirectUris.Count, redirectUris.Count());
            Assert.IsType<PageResult>(get);
        }

        [Fact]
        public async Task OnGetAsync_InvalidId()
        {
            // Arrange
            var model = new RedirectUrisModel(new Mock<IConfigurationDbContext>().Object);

            // Act

            var get = await model.OnGetAsync(0);

            // Assert
            Assert.Null(model.Client);
            Assert.Null(model.RedirectUris);
            Assert.IsType<NotFoundResult>(get);
        }

        [Fact]
        public async Task OnGetAsync_InvalidModel()
        {
            // Arrange
            var databaseName = $"{DatabaseNamePrefix}.{nameof(OnGetAsync_InvalidModel)}";
            var options = new DbContextOptionsBuilder<OidcDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;
            RedirectUrisModel model;
            IActionResult get;

            // Act
            using (var context = new OidcDbContext(options))
            {
                model = new RedirectUrisModel(context);
                get = await model.OnGetAsync(Random.Next());
            }

            // Assert
            Assert.Null(model.Client);
            Assert.Null(model.RedirectUris);
            Assert.IsType<NotFoundResult>(get);
        }

        [Fact]
        public async Task OnPostAsync()
        {
            // Arrange
            const string redirectUri1OriginalRedirectUri = "Original RedirectUri";
            const string redirectUri1EditedRedirectUri = "Edited RedirectUri";
            const string newRedirectUriRedirectUri = "New RedirectUri";
            var databaseName = $"{DatabaseNamePrefix}.{nameof(OnPostAsync)}";
            var options = new DbContextOptionsBuilder<OidcDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;
            RedirectUrisModel redirectUris;
            IActionResult post;
            var redirectUri1 = new ClientRedirectUri
            {
                Id = Random.Next(),
                RedirectUri = redirectUri1OriginalRedirectUri
            };
            var redirectUri2 = new ClientRedirectUri {Id = Random.Next()};
            var client = new Client
            {
                Id = Random.Next(),
                RedirectUris = new List<ClientRedirectUri>
                {
                    redirectUri1,
                    redirectUri2
                }
            };
            using (var context = new OidcDbContext(options))
            {
                context.Add(client);
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new OidcDbContext(options))
            {
                redirectUris = new RedirectUrisModel(context)
                {
                    Client = new Client
                    {
                        Id = client.Id,
                        RedirectUris = new List<ClientRedirectUri>
                        {
                            new ClientRedirectUri
                            {
                                Id = redirectUri1.Id,
                                RedirectUri = redirectUri1EditedRedirectUri
                            },
                            new ClientRedirectUri {RedirectUri = newRedirectUriRedirectUri}
                        }
                    }
                };
                post = await redirectUris.OnPostAsync();
            }

            // Assert
            using (var context = new OidcDbContext(options))
            {
                client = await context.Clients
                    .Include(x => x.RedirectUris)
                    .SingleOrDefaultAsync(x => x.Id.Equals(client.Id));
                redirectUri1 = client.RedirectUris.SingleOrDefault(x => x.Id.Equals(redirectUri1.Id));
                redirectUri2 = client.RedirectUris.SingleOrDefault(x => x.Id.Equals(redirectUri2.Id));
                var newRedirectUri = client.RedirectUris.SingleOrDefault(x => x.RedirectUri.Equals(newRedirectUriRedirectUri));

                Assert.NotNull(redirectUri1);
                Assert.Equal(redirectUri1EditedRedirectUri, redirectUri1.RedirectUri);
                Assert.Null(redirectUri2);
                Assert.NotNull(newRedirectUri);
            }

            var result = Assert.IsType<RedirectToPageResult>(post);
            Assert.Equal("../Details/RedirectUris", result.PageName);
            Assert.Collection(result.RouteValues, routeValue =>
            {
                var (key, value) = routeValue;
                Assert.Equal(nameof(Client.Id), key);
                Assert.Equal(redirectUris.Client.Id, value);
            });
        }

        [Fact]
        public async Task OnPostAsync_AllRemoved()
        {
            // Arrange
            var databaseName = $"{DatabaseNamePrefix}.{nameof(OnPostAsync_AllRemoved)}";
            var options = new DbContextOptionsBuilder<OidcDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;
            RedirectUrisModel redirectUris;
            IActionResult post;
            var client = new Client
            {
                Id = Random.Next(),
                RedirectUris = new List<ClientRedirectUri>
                {
                    new ClientRedirectUri {Id = Random.Next()}
                }
            };
            using (var context = new OidcDbContext(options))
            {
                context.Add(client);
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new OidcDbContext(options))
            {
                redirectUris = new RedirectUrisModel(context)
                {
                    Client = new Client {Id = client.Id}
                };
                post = await redirectUris.OnPostAsync();
            }

            // Assert
            using (var context = new OidcDbContext(options))
            {
                client = await context.Clients
                    .Include(x => x.RedirectUris)
                    .SingleOrDefaultAsync(x => x.Id.Equals(client.Id));

                Assert.Empty(client.RedirectUris);
            }

            var result = Assert.IsType<RedirectToPageResult>(post);
            Assert.Equal("../Details/RedirectUris", result.PageName);
            Assert.Collection(result.RouteValues, routeValue =>
            {
                var (key, value) = routeValue;
                Assert.Equal(nameof(Client.Id), key);
                Assert.Equal(redirectUris.Client.Id, value);
            });
        }

        [Fact]
        public async Task OnPostAsync_InvalidId()
        {
            // Arrange
            var context = new Mock<IConfigurationDbContext>();
            var redirectUris = new RedirectUrisModel(context.Object)
            {
                Client = new Client {Id = 0}
            };

            // Act
            var post = await redirectUris.OnPostAsync();

            // Assert
            context.Verify(x => x.SaveChangesAsync(), Times.Never);
            Assert.IsType<PageResult>(post);
        }

        [Fact]
        public async Task OnPostAsync_InvalidModel()
        {
            // Arrange
            var databaseName = $"{DatabaseNamePrefix}.{nameof(OnPostAsync)}";
            var options = new DbContextOptionsBuilder<OidcDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;
            IActionResult post;
            var client = new Client {Id = Random.Next()};
            using (var context = new OidcDbContext(options))
            {
                context.Add(client);
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new OidcDbContext(options))
            {
                var redirectUris = new RedirectUrisModel(context)
                {
                    Client = new Client {Id = Random.Next()}
                };
                post = await redirectUris.OnPostAsync();
            }

            // Assert
            Assert.IsType<PageResult>(post);
        }
    }
}
