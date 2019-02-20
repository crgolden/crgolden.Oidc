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

    public class PropertiesFacts
    {
        private static Random Random => new Random();
        private static string DatabaseNamePrefix => typeof(PropertiesFacts).FullName;

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
                Properties = new List<ClientProperty>
                {
                    new ClientProperty(),
                    new ClientProperty(),
                    new ClientProperty()
                }
            };
            PropertiesModel model;
            IActionResult get;
            using (var context = new OidcDbContext(options))
            {
                context.Add(client);
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new OidcDbContext(options))
            {
                model = new PropertiesModel(context);
                get = await model.OnGetAsync(client.Id);
            }

            // Assert
            Assert.NotNull(model.Client);
            Assert.Equal(client.Id, model.Client.Id);
            var properties = Assert.IsAssignableFrom<IEnumerable<ClientProperty>>(model.Properties);
            Assert.Equal(client.Properties.Count, properties.Count());
            Assert.IsType<PageResult>(get);
        }

        [Fact]
        public async Task OnGetAsync_InvalidId()
        {
            // Arrange
            var model = new PropertiesModel(new Mock<IConfigurationDbContext>().Object);

            // Act

            var get = await model.OnGetAsync(0);

            // Assert
            Assert.Null(model.Client);
            Assert.Null(model.Properties);
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
            PropertiesModel model;
            IActionResult get;

            // Act
            using (var context = new OidcDbContext(options))
            {
                model = new PropertiesModel(context);
                get = await model.OnGetAsync(Random.Next());
            }

            // Assert
            Assert.Null(model.Client);
            Assert.Null(model.Properties);
            Assert.IsType<NotFoundResult>(get);
        }

        [Fact]
        public async Task OnPostAsync()
        {
            // Arrange
            const string property1OriginalValue = "Original Value";
            const string property1EditedValue = "Edited Value";
            const string newPropertyValue = "New Value";
            var databaseName = $"{DatabaseNamePrefix}.{nameof(OnPostAsync)}";
            var options = new DbContextOptionsBuilder<OidcDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;
            PropertiesModel properties;
            IActionResult post;
            var property1 = new ClientProperty
            {
                Id = Random.Next(),
                Value = property1OriginalValue
            };
            var property2 = new ClientProperty {Id = Random.Next()};
            var client = new Client
            {
                Id = Random.Next(),
                Properties = new List<ClientProperty>
                {
                    property1,
                    property2
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
                properties = new PropertiesModel(context)
                {
                    Client = new Client
                    {
                        Id = client.Id,
                        Properties = new List<ClientProperty>
                        {
                            new ClientProperty
                            {
                                Id = property1.Id,
                                Value = property1EditedValue
                            },
                            new ClientProperty {Value = newPropertyValue}
                        }
                    }
                };
                post = await properties.OnPostAsync();
            }

            // Assert
            using (var context = new OidcDbContext(options))
            {
                client = await context.Clients
                    .Include(x => x.Properties)
                    .SingleOrDefaultAsync(x => x.Id.Equals(client.Id));
                property1 = client.Properties.SingleOrDefault(x => x.Id.Equals(property1.Id));
                property2 = client.Properties.SingleOrDefault(x => x.Id.Equals(property2.Id));
                var newProperty = client.Properties.SingleOrDefault(x => x.Value.Equals(newPropertyValue));

                Assert.NotNull(property1);
                Assert.Equal(property1EditedValue, property1.Value);
                Assert.Null(property2);
                Assert.NotNull(newProperty);
            }

            var result = Assert.IsType<RedirectToPageResult>(post);
            Assert.Equal("../Details/Properties", result.PageName);
            Assert.Collection(result.RouteValues, routeValue =>
            {
                var (key, value) = routeValue;
                Assert.Equal(nameof(Client.Id), key);
                Assert.Equal(properties.Client.Id, value);
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
            PropertiesModel properties;
            IActionResult post;
            var client = new Client
            {
                Id = Random.Next(),
                Properties = new List<ClientProperty>
                {
                    new ClientProperty {Id = Random.Next()}
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
                properties = new PropertiesModel(context)
                {
                    Client = new Client {Id = client.Id}
                };
                post = await properties.OnPostAsync();
            }

            // Assert
            using (var context = new OidcDbContext(options))
            {
                client = await context.Clients
                    .Include(x => x.Properties)
                    .SingleOrDefaultAsync(x => x.Id.Equals(client.Id));

                Assert.Empty(client.Properties);
            }

            var result = Assert.IsType<RedirectToPageResult>(post);
            Assert.Equal("../Details/Properties", result.PageName);
            Assert.Collection(result.RouteValues, routeValue =>
            {
                var (key, value) = routeValue;
                Assert.Equal(nameof(Client.Id), key);
                Assert.Equal(properties.Client.Id, value);
            });
        }

        [Fact]
        public async Task OnPostAsync_InvalidId()
        {
            // Arrange
            var context = new Mock<IConfigurationDbContext>();
            var properties = new PropertiesModel(context.Object)
            {
                Client = new Client {Id = 0}
            };

            // Act
            var post = await properties.OnPostAsync();

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
                var properties = new PropertiesModel(context)
                {
                    Client = new Client {Id = Random.Next()}
                };
                post = await properties.OnPostAsync();
            }

            // Assert
            Assert.IsType<PageResult>(post);
        }
    }
}
