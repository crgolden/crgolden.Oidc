namespace Clarity.Oidc.Pages.Tests.ApiResources.Edit
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
    using Pages.ApiResources.Edit;
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

        [Fact]
        public async Task OnPostAsync()
        {
            // Arrange
            const string claim1OriginalType = "Original Type";
            const string claim1EditedType = "Edited Type";
            const string newClaimType = "New Type";
            var databaseName = $"{DatabaseNamePrefix}.{nameof(OnPostAsync)}";
            var options = new DbContextOptionsBuilder<OidcDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;
            ClaimTypesModel claims;
            IActionResult post;
            var claim1 = new ApiResourceClaim
            {
                Id = Random.Next(),
                Type = claim1OriginalType
            };
            var claim2 = new ApiResourceClaim {Id = Random.Next()};
            var apiResource = new ApiResource
            {
                Id = Random.Next(),
                UserClaims = new List<ApiResourceClaim>
                {
                    claim1,
                    claim2
                }
            };
            using (var context = new OidcDbContext(options))
            {
                context.Add(apiResource);
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new OidcDbContext(options))
            {
                claims = new ClaimTypesModel(context)
                {
                    ApiResource = new ApiResource
                    {
                        Id = apiResource.Id,
                        UserClaims = new List<ApiResourceClaim>
                        {
                            new ApiResourceClaim
                            {
                                Id = claim1.Id,
                                Type = claim1EditedType
                            },
                            new ApiResourceClaim {Type = newClaimType}
                        }
                    }
                };
                post = await claims.OnPostAsync();
            }

            // Assert
            using (var context = new OidcDbContext(options))
            {
                apiResource = await context.ApiResources
                    .Include(x => x.UserClaims)
                    .SingleOrDefaultAsync(x => x.Id.Equals(apiResource.Id));
                claim1 = apiResource.UserClaims.SingleOrDefault(x => x.Id.Equals(claim1.Id));
                claim2 = apiResource.UserClaims.SingleOrDefault(x => x.Id.Equals(claim2.Id));
                var newClaim = apiResource.UserClaims.SingleOrDefault(x => x.Type.Equals(newClaimType));

                Assert.NotNull(claim1);
                Assert.Equal(claim1EditedType, claim1.Type);
                Assert.Null(claim2);
                Assert.NotNull(newClaim);
            }

            var result = Assert.IsType<RedirectToPageResult>(post);
            Assert.Equal("../Details/ClaimTypes", result.PageName);
            Assert.Collection(result.RouteValues, routeValue =>
            {
                var (key, value) = routeValue;
                Assert.Equal(nameof(ApiResource.Id), key);
                Assert.Equal(claims.ApiResource.Id, value);
            });
        }

        [Fact]
        public async Task OnPostAsync_AllRemoved()
        {
            // Arrange
            var databaseName = $"{DatabaseNamePrefix}.{nameof(OnPostAsync_AllRemoved)}";
            var options = new DbContextOptionsBuilder<OidcDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;
            ClaimTypesModel claims;
            IActionResult post;
            var apiResource = new ApiResource
            {
                Id = Random.Next(),
                UserClaims = new List<ApiResourceClaim>
                {
                    new ApiResourceClaim {Id = Random.Next()}
                }
            };
            using (var context = new OidcDbContext(options))
            {
                context.Add(apiResource);
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new OidcDbContext(options))
            {
                claims = new ClaimTypesModel(context)
                {
                    ApiResource = new ApiResource {Id = apiResource.Id}
                };
                post = await claims.OnPostAsync();
            }

            // Assert
            using (var context = new OidcDbContext(options))
            {
                apiResource = await context.ApiResources
                    .Include(x => x.UserClaims)
                    .SingleOrDefaultAsync(x => x.Id.Equals(apiResource.Id));

                Assert.Empty(apiResource.UserClaims);
            }

            var result = Assert.IsType<RedirectToPageResult>(post);
            Assert.Equal("../Details/ClaimTypes", result.PageName);
            Assert.Collection(result.RouteValues, routeValue =>
            {
                var (key, value) = routeValue;
                Assert.Equal(nameof(ApiResource.Id), key);
                Assert.Equal(claims.ApiResource.Id, value);
            });
        }

        [Fact]
        public async Task OnPostAsync_InvalidId()
        {
            // Arrange
            var context = new Mock<IConfigurationDbContext>();
            var claims = new ClaimTypesModel(context.Object)
            {
                ApiResource = new ApiResource {Id = 0}
            };

            // Act
            var post = await claims.OnPostAsync();

            // Assert
            context.Verify(x => x.SaveChangesAsync(), Times.Never);
            Assert.IsType<PageResult>(post);
        }

        [Fact]
        public async Task OnPostAsync_InvalidModel()
        {
            // Arrange
            var databaseName = $"{DatabaseNamePrefix}.{nameof(OnPostAsync)}";
            var options = new DbContextOptionsBuilder<OidcDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;
            IActionResult post;
            var apiResource = new ApiResource {Id = Random.Next()};
            using (var context = new OidcDbContext(options))
            {
                context.Add(apiResource);
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new OidcDbContext(options))
            {
                var claims = new ClaimTypesModel(context)
                {
                    ApiResource = new ApiResource {Id = Random.Next()}
                };
                post = await claims.OnPostAsync();
            }

            // Assert
            Assert.IsType<PageResult>(post);
        }
    }
}
