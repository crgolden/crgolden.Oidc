namespace Clarity.Oidc.Tests.Pages.Clients.Details
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
    using Oidc.Pages.Clients.Details;
    using Xunit;

    public class GrantTypesFacts
    {
        private static Random Random => new Random();
        private static string DatabaseNamePrefix => typeof(GrantTypesFacts).FullName;

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
                AllowedGrantTypes = new List<ClientGrantType>
                {
                    new ClientGrantType(),
                    new ClientGrantType(),
                    new ClientGrantType()
                }
            };
            GrantTypesModel model;
            IActionResult get;
            using (var context = new OidcDbContext(options))
            {
                context.Add(client);
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new OidcDbContext(options))
            {
                model = new GrantTypesModel(context);
                get = await model.OnGetAsync(client.Id);
            }

            // Assert
            Assert.NotNull(model.Client);
            Assert.Equal(client.Id, model.Client.Id);
            var grantTypes = Assert.IsAssignableFrom<IEnumerable<ClientGrantType>>(model.GrantTypes);
            Assert.Equal(client.AllowedGrantTypes.Count, grantTypes.Count());
            Assert.IsType<PageResult>(get);
        }

        [Fact]
        public async Task OnGetAsync_InvalidId()
        {
            // Arrange
            var model = new GrantTypesModel(new Mock<IConfigurationDbContext>().Object);

            // Act

            var get = await model.OnGetAsync(0);

            // Assert
            Assert.Null(model.Client);
            Assert.Null(model.GrantTypes);
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
            GrantTypesModel model;
            IActionResult get;

            // Act
            using (var context = new OidcDbContext(options))
            {
                model = new GrantTypesModel(context);
                get = await model.OnGetAsync(Random.Next());
            }

            // Assert
            Assert.Null(model.Client);
            Assert.Null(model.GrantTypes);
            Assert.IsType<NotFoundResult>(get);
        }
    }
}
