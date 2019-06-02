namespace crgolden.Oidc.Pages.Tests.Users.Details
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
    using Microsoft.Extensions.Options;
    using Moq;
    using Pages.Users.Details;
    using Xunit;

    public class ClaimsFacts
    {
        private static string DatabaseNamePrefix => typeof(ClaimsFacts).FullName;
        private readonly Mock<UserManager<User>> _userManager;

        public ClaimsFacts()
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
                Claims = new List<UserClaim>
                {
                    new UserClaim {ClaimType = "Claim 1 Type"},
                    new UserClaim {ClaimType = "Claim 2 Type"},
                    new UserClaim {ClaimType = "Claim 3 Type"}
                }
            };
            using (var context = new OidcDbContext(options))
            {
                context.Add(user);
                await context.SaveChangesAsync().ConfigureAwait(false);
            }

            ClaimsModel model;
            IActionResult get;

            // Act
            using (var context = new OidcDbContext(options))
            {
                _userManager.Setup(x => x.Users).Returns(context.Users);
                model = new ClaimsModel(_userManager.Object);
                get = await model.OnGetAsync(user.Id).ConfigureAwait(false);
            }

            // Assert
            Assert.NotNull(model.UserModel);
            Assert.Equal(user.Id, model.UserModel.Id);
            var claims = Assert.IsAssignableFrom<IEnumerable<UserClaim>>(model.Claims);
            Assert.Equal(user.Claims.Count, claims.Count());
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
            var user = new User
            {
                Claims = new List<UserClaim>
                {
                    new UserClaim(),
                    new UserClaim(),
                    new UserClaim()
                }
            };
            using (var context = new OidcDbContext(options))
            {
                context.Add(user);
                await context.SaveChangesAsync().ConfigureAwait(false);
            }

            ClaimsModel model;
            IActionResult get;

            // Act
            using (var context = new OidcDbContext(options))
            {
                _userManager.Setup(x => x.Users).Returns(context.Users);
                model = new ClaimsModel(_userManager.Object);
                get = await model.OnGetAsync(Guid.Empty).ConfigureAwait(false);
            }

            // Assert
            Assert.Null(model.UserModel);
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
            var user = new User
            {
                Claims = new List<UserClaim>
                {
                    new UserClaim(),
                    new UserClaim(),
                    new UserClaim()
                }
            };
            using (var context = new OidcDbContext(options))
            {
                context.Add(user);
                await context.SaveChangesAsync().ConfigureAwait(false);
            }

            ClaimsModel model;
            IActionResult get;

            // Act
            using (var context = new OidcDbContext(options))
            {
                _userManager.Setup(x => x.Users).Returns(context.Users);
                model = new ClaimsModel(_userManager.Object);
                get = await model.OnGetAsync(Guid.NewGuid()).ConfigureAwait(false);
            }

            // Assert
            Assert.Null(model.UserModel);
            Assert.Null(model.Claims);
            Assert.IsType<NotFoundResult>(get);
        }
    }
}
