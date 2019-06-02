namespace crgolden.Oidc.Pages.Tests.Clients.Details
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using IdentityServer4.EntityFramework.Entities;
    using IdentityServer4.EntityFramework.Interfaces;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;
    using Moq;
    using Pages.Clients.Details;
    using Xunit;

    public class PostLogoutRedirectUrisFacts
    {
        private static Random Random => new Random();
        private static string DatabaseNamePrefix => typeof(PostLogoutRedirectUrisFacts).FullName;

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
                PostLogoutRedirectUris = new List<ClientPostLogoutRedirectUri>
                {
                    new ClientPostLogoutRedirectUri(),
                    new ClientPostLogoutRedirectUri(),
                    new ClientPostLogoutRedirectUri()
                }
            };
            PostLogoutRedirectUrisModel model;
            IActionResult get;
            using (var context = new OidcDbContext(options))
            {
                context.Add(client);
                await context.SaveChangesAsync().ConfigureAwait(false);
            }

            // Act
            using (var context = new OidcDbContext(options))
            {
                model = new PostLogoutRedirectUrisModel(context);
                get = await model.OnGetAsync(client.Id).ConfigureAwait(false);
            }

            // Assert
            Assert.NotNull(model.Client);
            Assert.Equal(client.Id, model.Client.Id);
            var postLogoutRedirectUris = Assert.IsAssignableFrom<IEnumerable<ClientPostLogoutRedirectUri>>(model.PostLogoutRedirectUris);
            Assert.Equal(client.PostLogoutRedirectUris.Count, postLogoutRedirectUris.Count());
            Assert.IsType<PageResult>(get);
        }

        [Fact]
        public async Task OnGetAsync_InvalidId()
        {
            // Arrange
            var model = new PostLogoutRedirectUrisModel(new Mock<IConfigurationDbContext>().Object);

            // Act

            var get = await model.OnGetAsync(0).ConfigureAwait(false);

            // Assert
            Assert.Null(model.Client);
            Assert.Null(model.PostLogoutRedirectUris);
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
            PostLogoutRedirectUrisModel model;
            IActionResult get;

            // Act
            using (var context = new OidcDbContext(options))
            {
                model = new PostLogoutRedirectUrisModel(context);
                get = await model.OnGetAsync(Random.Next()).ConfigureAwait(false);
            }

            // Assert
            Assert.Null(model.Client);
            Assert.Null(model.PostLogoutRedirectUris);
            Assert.IsType<NotFoundResult>(get);
        }
    }
}
