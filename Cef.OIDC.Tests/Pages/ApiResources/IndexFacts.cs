namespace Cef.OIDC.Tests.Pages.ApiResources
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Cef.OIDC.Pages.ApiResources;
    using IdentityServer4.EntityFramework.Entities;
    using IdentityServer4.EntityFramework.Interfaces;
    using Microsoft.EntityFrameworkCore;
    using Moq;
    using Xunit;

    [ExcludeFromCodeCoverage]
    public class IndexFacts
    {
        [Fact]
        public void OnGet()
        {
            // Arrange
            var apiResources = new Mock<DbSet<ApiResource>>();
            var context = new Mock<IConfigurationDbContext>();
            context.Setup(x => x.ApiResources).Returns(apiResources.Object);
            var index = new IndexModel(context.Object);

            // Act
            index.OnGet();

            // Assert
            var result = Assert.IsAssignableFrom<IEnumerable<ApiResource>>(index.ApiResources);
            Assert.NotNull(result);
        }
    }
}
