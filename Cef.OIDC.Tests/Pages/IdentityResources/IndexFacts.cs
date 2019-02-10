namespace Cef.OIDC.Tests.Pages.IdentityResources
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Cef.OIDC.Pages.IdentityResources;
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
            var identityResources = new Mock<DbSet<IdentityResource>>();
            var context = new Mock<IConfigurationDbContext>();
            context.Setup(x => x.IdentityResources).Returns(identityResources.Object);
            var index = new IndexModel(context.Object);

            // Act
            index.OnGet();

            // Assert
            var result = Assert.IsAssignableFrom<IEnumerable<IdentityResource>>(index.IdentityResources);
            Assert.NotNull(result);
        }
    }
}
