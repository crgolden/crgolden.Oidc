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

    public class SecretsFacts
    {
        private static Random Random => new Random();
        private static string DatabaseNamePrefix => typeof(SecretsFacts).FullName;

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
                ClientSecrets = new List<ClientSecret>
                {
                    new ClientSecret(),
                    new ClientSecret(),
                    new ClientSecret()
                }
            };
            SecretsModel model;
            IActionResult get;
            using (var context = new OidcDbContext(options))
            {
                context.Add(client);
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new OidcDbContext(options))
            {
                model = new SecretsModel(context);
                get = await model.OnGetAsync(client.Id);
            }

            // Assert
            Assert.NotNull(model.Client);
            Assert.Equal(client.Id, model.Client.Id);
            var secrets = Assert.IsAssignableFrom<IEnumerable<ClientSecret>>(model.Secrets);
            Assert.Equal(client.ClientSecrets.Count, secrets.Count());
            Assert.IsType<PageResult>(get);
        }

        [Fact]
        public async Task OnGetAsync_InvalidId()
        {
            // Arrange
            var model = new SecretsModel(new Mock<IConfigurationDbContext>().Object);

            // Act

            var get = await model.OnGetAsync(0);

            // Assert
            Assert.Null(model.Client);
            Assert.Null(model.Secrets);
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
            SecretsModel model;
            IActionResult get;

            // Act
            using (var context = new OidcDbContext(options))
            {
                model = new SecretsModel(context);
                get = await model.OnGetAsync(Random.Next());
            }

            // Assert
            Assert.Null(model.Client);
            Assert.Null(model.Secrets);
            Assert.IsType<NotFoundResult>(get);
        }

        [Fact]
        public async Task OnPostAsync()
        {
            // Arrange
            const string secret1OriginalValue = "Original Value";
            const string secret1EditedValue = "Edited Value";
            const string newSecretValue = "New Value";
            var databaseName = $"{DatabaseNamePrefix}.{nameof(OnPostAsync)}";
            var options = new DbContextOptionsBuilder<OidcDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;
            SecretsModel secrets;
            IActionResult post;
            var secret1 = new ClientSecret
            {
                Id = Random.Next(),
                Value = secret1OriginalValue
            };
            var secret2 = new ClientSecret {Id = Random.Next()};
            var client = new Client
            {
                Id = Random.Next(),
                ClientSecrets = new List<ClientSecret>
                {
                    secret1,
                    secret2
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
                secrets = new SecretsModel(context)
                {
                    Client = new Client
                    {
                        Id = client.Id,
                        ClientSecrets = new List<ClientSecret>
                        {
                            new ClientSecret
                            {
                                Id = secret1.Id,
                                Value = secret1EditedValue
                            },
                            new ClientSecret {Value = newSecretValue}
                        }
                    }
                };
                post = await secrets.OnPostAsync();
            }

            // Assert
            using (var context = new OidcDbContext(options))
            {
                client = await context.Clients
                    .Include(x => x.ClientSecrets)
                    .SingleOrDefaultAsync(x => x.Id.Equals(client.Id));
                secret1 = client.ClientSecrets.SingleOrDefault(x => x.Id.Equals(secret1.Id));
                secret2 = client.ClientSecrets.SingleOrDefault(x => x.Id.Equals(secret2.Id));
                var newSecret = client.ClientSecrets.SingleOrDefault(x => x.Value.Equals(newSecretValue));

                Assert.NotNull(secret1);
                Assert.Equal(secret1EditedValue, secret1.Value);
                Assert.Null(secret2);
                Assert.NotNull(newSecret);
            }

            var result = Assert.IsType<RedirectToPageResult>(post);
            Assert.Equal("../Details/Secrets", result.PageName);
            Assert.Collection(result.RouteValues, routeValue =>
            {
                var (key, value) = routeValue;
                Assert.Equal(nameof(Client.Id), key);
                Assert.Equal(secrets.Client.Id, value);
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
            SecretsModel secrets;
            IActionResult post;
            var client = new Client
            {
                Id = Random.Next(),
                ClientSecrets = new List<ClientSecret>
                {
                    new ClientSecret {Id = Random.Next()}
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
                secrets = new SecretsModel(context)
                {
                    Client = new Client {Id = client.Id}
                };
                post = await secrets.OnPostAsync();
            }

            // Assert
            using (var context = new OidcDbContext(options))
            {
                client = await context.Clients
                    .Include(x => x.ClientSecrets)
                    .SingleOrDefaultAsync(x => x.Id.Equals(client.Id));

                Assert.Empty(client.ClientSecrets);
            }

            var result = Assert.IsType<RedirectToPageResult>(post);
            Assert.Equal("../Details/Secrets", result.PageName);
            Assert.Collection(result.RouteValues, routeValue =>
            {
                var (key, value) = routeValue;
                Assert.Equal(nameof(Client.Id), key);
                Assert.Equal(secrets.Client.Id, value);
            });
        }

        [Fact]
        public async Task OnPostAsync_InvalidId()
        {
            // Arrange
            var context = new Mock<IConfigurationDbContext>();
            var secrets = new SecretsModel(context.Object)
            {
                Client = new Client {Id = 0}
            };

            // Act
            var post = await secrets.OnPostAsync();

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
                var secrets = new SecretsModel(context)
                {
                    Client = new Client {Id = Random.Next()}
                };
                post = await secrets.OnPostAsync();
            }

            // Assert
            Assert.IsType<PageResult>(post);
        }
    }
}
