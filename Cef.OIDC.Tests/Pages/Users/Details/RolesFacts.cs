namespace Cef.OIDC.Tests.Pages.Users.Details
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading.Tasks;
    using Cef.OIDC.Pages.Users.Details;
    using Data;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Models;
    using Moq;
    using Relationships;
    using Xunit;

    [ExcludeFromCodeCoverage]
    public class RolesFacts
    {
        private static string DatabaseNamePrefix => typeof(RolesFacts).FullName;
        private readonly Mock<UserManager<User>> _userManager;

        public RolesFacts()
        {
            _userManager = new Mock<UserManager<User>>(
                Mock.Of<IUserStore<User>>(),
                Mock.Of<IOptions<IdentityOptions>>(),
                Mock.Of<IPasswordHasher<User>>(),
                new List<IUserValidator<User>>(),
                new List<IPasswordValidator<User>>(),
                Mock.Of<ILookupNormalizer>(),
                new IdentityErrorDescriber(),
                Mock.Of<IServiceProvider>(),
                Mock.Of<ILogger<UserManager<User>>>());
        }

        [Fact]
        public async Task OnGetAsync()
        {
            // Arrange
            var databaseName = $"{DatabaseNamePrefix}.{nameof(OnGetAsync)}";
            var options = new DbContextOptionsBuilder<OidcDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;
            var user = new User
            {
                Id = Guid.NewGuid(),
                UserRoles = new List<UserRole>
                {
                    new UserRole {Role = new Role()},
                    new UserRole {Role = new Role()},
                    new UserRole {Role = new Role()}
                }
            };
            using (var context = new OidcDbContext(options))
            {
                context.Add(user);
                await context.SaveChangesAsync();
            }

            RolesModel model;
            IActionResult get;

            // Act
            using (var context = new OidcDbContext(options))
            {
                _userManager.Setup(x => x.Users).Returns(context.Users);
                model = new RolesModel(_userManager.Object);
                get = await model.OnGetAsync(user.Id);
            }

            // Assert
            Assert.NotNull(model.UserModel);
            Assert.Equal(user.Id, model.UserModel.Id);
            var roles = Assert.IsAssignableFrom<IEnumerable<Role>>(model.Roles);
            Assert.Equal(user.UserRoles.Count, roles.Count());
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

            RolesModel model;
            IActionResult get;

            // Act
            using (var context = new OidcDbContext(options))
            {
                _userManager.Setup(x => x.Users).Returns(context.Users);
                model = new RolesModel(_userManager.Object);
                get = await model.OnGetAsync(Guid.Empty);
            }

            // Assert
            Assert.Null(model.UserModel);
            Assert.Null(model.Roles);
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

            RolesModel model;
            IActionResult get;

            // Act
            using (var context = new OidcDbContext(options))
            {
                _userManager.Setup(x => x.Users).Returns(context.Users);
                model = new RolesModel(_userManager.Object);
                get = await model.OnGetAsync(Guid.NewGuid());
            }

            // Assert
            Assert.Null(model.UserModel);
            Assert.Null(model.Roles);
            Assert.IsType<NotFoundResult>(get);
        }
    }
}
