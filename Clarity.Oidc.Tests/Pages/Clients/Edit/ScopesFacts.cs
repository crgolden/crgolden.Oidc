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

    public class ScopesFacts
    {
        private static Random Random => new Random();
        private static string DatabaseNamePrefix => typeof(ScopesFacts).FullName;

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
                AllowedScopes = new List<ClientScope>
                {
                    new ClientScope(),
                    new ClientScope(),
                    new ClientScope()
                }
            };
            ScopesModel model;
            IActionResult get;
            using (var context = new OidcDbContext(options))
            {
                context.Add(client);
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new OidcDbContext(options))
            {
                model = new ScopesModel(context);
                get = await model.OnGetAsync(client.Id);
            }

            // Assert
            Assert.NotNull(model.Client);
            Assert.Equal(client.Id, model.Client.Id);
            var scopes = Assert.IsAssignableFrom<IEnumerable<ClientScope>>(model.Scopes);
            Assert.Equal(client.AllowedScopes.Count, scopes.Count());
            Assert.IsType<PageResult>(get);
        }

        [Fact]
        public async Task OnGetAsync_InvalidId()
        {
            // Arrange
            var model = new ScopesModel(new Mock<IConfigurationDbContext>().Object);

            // Act

            var get = await model.OnGetAsync(0);

            // Assert
            Assert.Null(model.Client);
            Assert.Null(model.Scopes);
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
            ScopesModel model;
            IActionResult get;

            // Act
            using (var context = new OidcDbContext(options))
            {
                model = new ScopesModel(context);
                get = await model.OnGetAsync(Random.Next());
            }

            // Assert
            Assert.Null(model.Client);
            Assert.Null(model.Scopes);
            Assert.IsType<NotFoundResult>(get);
        }

        [Fact]
        public async Task OnPostAsync()
        {
            // Arrange
            const string scope1OriginalScope = "Original Scope";
            const string scope1EditedScope = "Edited Scope";
            const string newScopeScope = "New Scope";
            var databaseName = $"{DatabaseNamePrefix}.{nameof(OnPostAsync)}";
            var options = new DbContextOptionsBuilder<OidcDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;
            ScopesModel scopes;
            IActionResult post;
            var scope1 = new ClientScope
            {
                Id = Random.Next(),
                Scope = scope1OriginalScope
            };
            var scope2 = new ClientScope {Id = Random.Next()};
            var client = new Client
            {
                Id = Random.Next(),
                AllowedScopes = new List<ClientScope>
                {
                    scope1,
                    scope2
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
                scopes = new ScopesModel(context)
                {
                    Client = new Client
                    {
                        Id = client.Id,
                        AllowedScopes = new List<ClientScope>
                        {
                            new ClientScope
                            {
                                Id = scope1.Id,
                                Scope = scope1EditedScope
                            },
                            new ClientScope {Scope = newScopeScope}
                        }
                    }
                };
                post = await scopes.OnPostAsync();
            }

            // Assert
            using (var context = new OidcDbContext(options))
            {
                client = await context.Clients
                    .Include(x => x.AllowedScopes)
                    .SingleOrDefaultAsync(x => x.Id.Equals(client.Id));
                scope1 = client.AllowedScopes.SingleOrDefault(x => x.Id.Equals(scope1.Id));
                scope2 = client.AllowedScopes.SingleOrDefault(x => x.Id.Equals(scope2.Id));
                var newScope = client.AllowedScopes.SingleOrDefault(x => x.Scope.Equals(newScopeScope));

                Assert.NotNull(scope1);
                Assert.Equal(scope1EditedScope, scope1.Scope);
                Assert.Null(scope2);
                Assert.NotNull(newScope);
            }

            var result = Assert.IsType<RedirectToPageResult>(post);
            Assert.Equal("../Details/Scopes", result.PageName);
            Assert.Collection(result.RouteValues, routeValue =>
            {
                var (key, value) = routeValue;
                Assert.Equal(nameof(Client.Id), key);
                Assert.Equal(scopes.Client.Id, value);
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
            ScopesModel scopes;
            IActionResult post;
            var client = new Client
            {
                Id = Random.Next(),
                AllowedScopes = new List<ClientScope>
                {
                    new ClientScope {Id = Random.Next()}
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
                scopes = new ScopesModel(context)
                {
                    Client = new Client {Id = client.Id}
                };
                post = await scopes.OnPostAsync();
            }

            // Assert
            using (var context = new OidcDbContext(options))
            {
                client = await context.Clients
                    .Include(x => x.AllowedScopes)
                    .SingleOrDefaultAsync(x => x.Id.Equals(client.Id));

                Assert.Empty(client.AllowedScopes);
            }

            var result = Assert.IsType<RedirectToPageResult>(post);
            Assert.Equal("../Details/Scopes", result.PageName);
            Assert.Collection(result.RouteValues, routeValue =>
            {
                var (key, value) = routeValue;
                Assert.Equal(nameof(Client.Id), key);
                Assert.Equal(scopes.Client.Id, value);
            });
        }

        [Fact]
        public async Task OnPostAsync_InvalidId()
        {
            // Arrange
            var context = new Mock<IConfigurationDbContext>();
            var scopes = new ScopesModel(context.Object)
            {
                Client = new Client {Id = 0}
            };

            // Act
            var post = await scopes.OnPostAsync();

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
                var scopes = new ScopesModel(context)
                {
                    Client = new Client {Id = Random.Next()}
                };
                post = await scopes.OnPostAsync();
            }

            // Assert
            Assert.IsType<PageResult>(post);
        }
    }
}
