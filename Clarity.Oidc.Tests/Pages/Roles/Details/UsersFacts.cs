namespace Clarity.Oidc.Tests.Pages.Roles.Details
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Data;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Models;
    using Moq;
    using Oidc.Pages.Roles.Details;
    using Relationships;
    using Xunit;

    public class UsersFacts
    {
        private static string DatabaseNamePrefix => typeof(UsersFacts).FullName;
        private readonly Mock<RoleManager<Role>> _roleManager;

        public UsersFacts()
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
                UserRoles = new List<UserRole>
                {
                    new UserRole {User = new User()},
                    new UserRole {User = new User()},
                    new UserRole {User = new User()}
                }
            };
            using (var context = new OidcDbContext(options))
            {
                context.Add(role);
                await context.SaveChangesAsync();
            }

            UsersModel model;
            IActionResult get;

            // Act
            using (var context = new OidcDbContext(options))
            {
                _roleManager.Setup(x => x.Roles).Returns(context.Roles);
                model = new UsersModel(_roleManager.Object);
                get = await model.OnGetAsync(role.Id);
            }

            // Assert
            Assert.NotNull(model.Role);
            Assert.Equal(role.Id, model.Role.Id);
            var users = Assert.IsAssignableFrom<IEnumerable<User>>(model.Users);
            Assert.Equal(role.UserRoles.Count, users.Count());
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
            var role = new Role {Id = Guid.NewGuid()};
            using (var context = new OidcDbContext(options))
            {
                context.Add(role);
                await context.SaveChangesAsync();
            }

            UsersModel model;
            IActionResult get;

            // Act
            using (var context = new OidcDbContext(options))
            {
                _roleManager.Setup(x => x.Roles).Returns(context.Roles);
                model = new UsersModel(_roleManager.Object);
                get = await model.OnGetAsync(Guid.Empty);
            }

            // Assert
            Assert.Null(model.Role);
            Assert.Null(model.Users);
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
            var role = new Role {Id = Guid.NewGuid()};
            using (var context = new OidcDbContext(options))
            {
                context.Add(role);
                await context.SaveChangesAsync();
            }

            UsersModel model;
            IActionResult get;

            // Act
            using (var context = new OidcDbContext(options))
            {
                _roleManager.Setup(x => x.Roles).Returns(context.Roles);
                model = new UsersModel(_roleManager.Object);
                get = await model.OnGetAsync(Guid.NewGuid());
            }

            // Assert
            Assert.Null(model.Role);
            Assert.Null(model.Users);
            Assert.IsType<NotFoundResult>(get);
        }
    }
}
