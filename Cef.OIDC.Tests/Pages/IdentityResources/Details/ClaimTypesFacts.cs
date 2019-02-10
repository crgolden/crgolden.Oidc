namespace Cef.OIDC.Tests.Pages.IdentityResources.Details
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading.Tasks;
    using Cef.OIDC.Pages.IdentityResources.Details;
    using Data;
    using IdentityServer4.EntityFramework.Entities;
    using IdentityServer4.EntityFramework.Interfaces;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;
    using Moq;
    using Xunit;

    [ExcludeFromCodeCoverage]
    public class ClaimTypesFacts
    {
        private static Random Random => new Random();
        private static string DatabaseNamePrefix => typeof(ClaimTypesFacts).FullName;

        [Fact]
        public async Task OnGetAsync()
        {
            // Arrange
            var databaseName = $"{DatabaseNamePrefix}.{nameof(OnGetAsync)}";
            var options = new DbContextOptionsBuilder<OidcDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;
            var identityResource = new IdentityResource
            {
                Id = Random.Next(),
                UserClaims = new List<IdentityClaim>
                {
                    new IdentityClaim(),
                    new IdentityClaim(),
                    new IdentityClaim()
                }
            };
            ClaimTypesModel model;
            IActionResult get;
            using (var context = new OidcDbContext(options))
            {
                context.Add(identityResource);
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new OidcDbContext(options))
            {
                model = new ClaimTypesModel(context);
                get = await model.OnGetAsync(identityResource.Id);
            }

            // Assert
            Assert.NotNull(model.IdentityResource);
            Assert.Equal(identityResource.Id, model.IdentityResource.Id);
            var claimTypes = Assert.IsAssignableFrom<IEnumerable<IdentityClaim>>(model.ClaimTypes);
            Assert.Equal(identityResource.UserClaims.Count, claimTypes.Count());
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
            Assert.Null(model.IdentityResource);
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
            Assert.Null(model.IdentityResource);
            Assert.Null(model.ClaimTypes);
            Assert.IsType<NotFoundResult>(get);
        }
    }
}
