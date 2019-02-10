namespace Cef.OIDC.Tests.Pages.Roles.Details
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;
    using Cef.OIDC.Pages.Roles.Details;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Logging;
    using Models;
    using Moq;
    using Xunit;

    [ExcludeFromCodeCoverage]
    public class IndexFacts
    {
        private readonly Mock<RoleManager<Role>> _roleManager;

        public IndexFacts()
        {
            _roleManager = new Mock<RoleManager<Role>>(
                Mock.Of<IRoleStore<Role>>(),
                new List<IRoleValidator<Role>>(),
                Mock.Of<ILookupNormalizer>(),
                new IdentityErrorDescriber(),
                Mock.Of<ILogger<RoleManager<Role>>>());
        }

        [Fact]
        public async Task OnGetAsync()
        {
            // Arrange
            var role = new Role {Id = Guid.NewGuid()};
            _roleManager.Setup(x => x.FindByIdAsync($"{role.Id}")).ReturnsAsync(role);
            var index = new IndexModel(_roleManager.Object);

            // Act
            var get = await index.OnGetAsync(role.Id);

            // Assert
            _roleManager.Verify(x => x.FindByIdAsync($"{role.Id}"), Times.Once);
            Assert.Equal(role, index.Role);
            Assert.IsType<PageResult>(get);
        }

        [Fact]
        public async Task OnGetAsync_InvalidId()
        {
            // Arrange
            var role = new Role {Id = Guid.NewGuid()};
            _roleManager.Setup(x => x.FindByIdAsync($"{role.Id}")).ReturnsAsync(role);
            var index = new IndexModel(_roleManager.Object);

            // Act
            var get = await index.OnGetAsync(Guid.Empty);

            // Assert
            _roleManager.Verify(x => x.FindByIdAsync($"{role.Id}"), Times.Never);
            Assert.Null(index.Role);
            Assert.IsType<NotFoundResult>(get);
        }

        [Fact]
        public async Task OnGetAsync_InvalidModel()
        {
            // Arrange
            var role = new Role {Id = Guid.NewGuid()};
            _roleManager.Setup(x => x.FindByIdAsync($"{role.Id}")).ReturnsAsync(role);
            var index = new IndexModel(_roleManager.Object);
            var id = Guid.NewGuid();

            // Act
            var get = await index.OnGetAsync(id);

            // Assert
            _roleManager.Verify(x => x.FindByIdAsync($"{id}"), Times.Once);
            Assert.Null(index.Role);
            Assert.IsType<NotFoundResult>(get);
        }
    }
}
