namespace Cef.OIDC.Tests.Pages.ApiResources.Details
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading.Tasks;
    using Cef.OIDC.Pages.ApiResources.Details;
    using Data;
    using IdentityServer4.EntityFramework.Entities;
    using IdentityServer4.EntityFramework.Interfaces;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;
    using Moq;
    using Xunit;

    [ExcludeFromCodeCoverage]
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
            var apiResource = new ApiResource
            {
                Id = Random.Next(),
                Secrets = new List<ApiSecret>
                {
                    new ApiSecret(),
                    new ApiSecret(),
                    new ApiSecret()
                }
            };
            SecretsModel model;
            IActionResult get;
            using (var context = new OidcDbContext(options))
            {
                context.Add(apiResource);
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new OidcDbContext(options))
            {
                model = new SecretsModel(context);
                get = await model.OnGetAsync(apiResource.Id);
            }

            // Assert
            Assert.NotNull(model.ApiResource);
            Assert.Equal(apiResource.Id, model.ApiResource.Id);
            var secrets = Assert.IsAssignableFrom<IEnumerable<ApiSecret>>(model.Secrets);
            Assert.Equal(apiResource.Secrets.Count, secrets.Count());
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
            Assert.Null(model.ApiResource);
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
            Assert.Null(model.ApiResource);
            Assert.Null(model.Secrets);
            Assert.IsType<NotFoundResult>(get);
        }
    }
}
