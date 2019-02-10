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
    }
}
