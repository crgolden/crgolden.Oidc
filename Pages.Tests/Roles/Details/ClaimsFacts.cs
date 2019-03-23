namespace Clarity.Oidc.Pages.Tests.Roles.Details
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Moq;
    using Pages.Roles.Details;
    using Xunit;

    public class ClaimsFacts
    {
        private static string DatabaseNamePrefix => typeof(ClaimsFacts).FullName;
        private readonly Mock<RoleManager<Role>> _roleManager;

        public ClaimsFacts()
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
            var databaseName = $"{DatabaseNamePrefix}.{nameof(OnGetAsync)}";
            var options = new DbContextOptionsBuilder<OidcDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;
            var role = new Role
            {
                Id = Guid.NewGuid(),
                RoleClaims = new List<RoleClaim>
                {
                    new RoleClaim(),
                    new RoleClaim(),
                    new RoleClaim()
                }
            };
            using (var context = new OidcDbContext(options))
            {
                context.Add(role);
                await context.SaveChangesAsync();
            }

            ClaimsModel model;
            IActionResult get;

            // Act
            using (var context = new OidcDbContext(options))
            {
                _roleManager.Setup(x => x.Roles).Returns(context.Roles);
                model = new ClaimsModel(_roleManager.Object);
                get = await model.OnGetAsync(role.Id);
            }

            // Assert
            Assert.NotNull(model.Role);
            Assert.Equal(role.Id, model.Role.Id);
            var claims = Assert.IsAssignableFrom<IEnumerable<RoleClaim>>(model.Claims);
            Assert.Equal(role.RoleClaims.Count, claims.Count());
            Assert.IsType<PageResult>(get);
        }

        [Fact]
        public async Task OnGetAsync_InvalidId()
        {
            // Arrange
            var databaseName = $"{DatabaseNamePrefix}.{nameof(OnGetAsync_InvalidId)}";
            var options = new DbContextOptionsBuilder<OidcDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;
            var role = new Role
            {
                Id = Guid.NewGuid(),
                RoleClaims = new List<RoleClaim>
                {
                    new RoleClaim(),
                    new RoleClaim(),
                    new RoleClaim()
                }
            };
            using (var context = new OidcDbContext(options))
            {
                context.Add(role);
                await context.SaveChangesAsync();
            }

            ClaimsModel model;
            IActionResult get;

            // Act
            using (var context = new OidcDbContext(options))
            {
                _roleManager.Setup(x => x.Roles).Returns(context.Roles);
                model = new ClaimsModel(_roleManager.Object);
                get = await model.OnGetAsync(Guid.Empty);
            }

            // Assert
            Assert.Null(model.Role);
            Assert.Null(model.Claims);
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
            var role = new Role
            {
                Id = Guid.NewGuid(),
                RoleClaims = new List<RoleClaim>
                {
                    new RoleClaim(),
                    new RoleClaim(),
                    new RoleClaim()
                }
            };
            using (var context = new OidcDbContext(options))
            {
                context.Add(role);
                await context.SaveChangesAsync();
            }

            ClaimsModel model;
            IActionResult get;

            // Act
            using (var context = new OidcDbContext(options))
            {
                _roleManager.Setup(x => x.Roles).Returns(context.Roles);
                model = new ClaimsModel(_roleManager.Object);
                get = await model.OnGetAsync(Guid.NewGuid());
            }

            // Assert
            Assert.Null(model.Role);
            Assert.Null(model.Claims);
            Assert.IsType<NotFoundResult>(get);
        }
    }
}
