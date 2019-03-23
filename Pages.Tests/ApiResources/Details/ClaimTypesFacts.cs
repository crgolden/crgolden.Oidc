namespace Clarity.Oidc.Pages.Tests.ApiResources.Details
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
    using Pages.ApiResources.Details;
    using Xunit;

    public class ClaimsFacts
    {
        private static Random Random => new Random();
        private static string DatabaseNamePrefix => typeof(ClaimsFacts).FullName;

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
                UserClaims = new List<ApiResourceClaim>
                {
                    new ApiResourceClaim(),
                    new ApiResourceClaim(),
                    new ApiResourceClaim()
                }
            };
            ClaimTypesModel model;
            IActionResult get;
            using (var context = new OidcDbContext(options))
            {
                context.Add(apiResource);
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new OidcDbContext(options))
            {
                model = new ClaimTypesModel(context);
                get = await model.OnGetAsync(apiResource.Id);
            }

            // Assert
            Assert.NotNull(model.ApiResource);
            Assert.Equal(apiResource.Id, model.ApiResource.Id);
            var claims = Assert.IsAssignableFrom<IEnumerable<ApiResourceClaim>>(model.ClaimTypes);
            Assert.Equal(apiResource.UserClaims.Count, claims.Count());
            Assert.IsType<PageResult>(get);
        }

        [Fact]
        public async Task OnGetAsync_InvalidId()
        {
            // Arrange
            var model = new ClaimTypesModel(new Mock<IConfigurationDbContext>().Object);

            // Act

            var get = await model.OnGetAsync(0);

            // Assert
            Assert.Null(model.ApiResource);
            Assert.Null(model.ClaimTypes);
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
            ClaimTypesModel model;
            IActionResult get;

            // Act
            using (var context = new OidcDbContext(options))
            {
                model = new ClaimTypesModel(context);
                get = await model.OnGetAsync(Random.Next());
            }

            // Assert
            Assert.Null(model.ApiResource);
            Assert.Null(model.ClaimTypes);
            Assert.IsType<NotFoundResult>(get);
        }
    }
}
