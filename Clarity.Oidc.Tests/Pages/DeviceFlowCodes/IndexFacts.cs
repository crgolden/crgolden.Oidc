namespace Clarity.Oidc.Tests.Pages.DeviceFlowCodes
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using IdentityServer4.EntityFramework.Entities;
    using IdentityServer4.EntityFramework.Interfaces;
    using Microsoft.EntityFrameworkCore;
    using Moq;
    using Oidc.Pages.DeviceFlowCodes;
    using Xunit;

    [ExcludeFromCodeCoverage]
    public class IndexFacts
    {
        [Fact]
        public void OnGet()
        {
            // Arrange
            var deviceFlowCodes = new Mock<DbSet<DeviceFlowCodes>>();
            var context = new Mock<IPersistedGrantDbContext>();
            context.Setup(x => x.DeviceFlowCodes).Returns(deviceFlowCodes.Object);
            var index = new IndexModel(context.Object);

            // Act
            index.OnGet();

            // Assert
            var result = Assert.IsAssignableFrom<IEnumerable<DeviceFlowCodes>>(index.DeviceFlowCodes);
            Assert.NotNull(result);
        }
    }
}
