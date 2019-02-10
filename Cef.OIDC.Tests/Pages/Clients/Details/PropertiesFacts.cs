namespace Cef.OIDC.Tests.Pages.Clients.Details
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading.Tasks;
    using Cef.OIDC.Pages.Clients.Details;
    using Data;
    using IdentityServer4.EntityFramework.Entities;
    using IdentityServer4.EntityFramework.Interfaces;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;
    using Moq;
    using Xunit;

    [ExcludeFromCodeCoverage]
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
    }
}
