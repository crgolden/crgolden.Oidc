namespace Cef.OIDC.Tests.Pages.ApiResources.Edit
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading.Tasks;
    using Cef.OIDC.Pages.ApiResources.Edit;
    using Data;
    using IdentityServer4.EntityFramework.Entities;
    using IdentityServer4.EntityFramework.Interfaces;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;
    using Moq;
    using Xunit;

    [ExcludeFromCodeCoverage]
    public class SecretsFacts
    {
        private static Random Random => new Random();
        private static string DatabaseNamePrefix => typeof(SecretsFacts).FullName;

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
                Secrets = new List<ApiSecret>
                {
                    new ApiSecret(),
                    new ApiSecret(),
                    new ApiSecret()
                }
            };
            SecretsModel model;
            IActionResult get;
            using (var context = new OidcDbContext(options))
            {
                context.Add(apiResource);
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new OidcDbContext(options))
            {
                model = new SecretsModel(context);
                get = await model.OnGetAsync(apiResource.Id);
            }

            // Assert
            Assert.NotNull(model.ApiResource);
            Assert.Equal(apiResource.Id, model.ApiResource.Id);
            var secrets = Assert.IsAssignableFrom<IEnumerable<ApiSecret>>(model.Secrets);
            Assert.Equal(apiResource.Secrets.Count, secrets.Count());
            Assert.IsType<PageResult>(get);
        }

        [Fact]
        public async Task OnGetAsync_InvalidId()
        {
            // Arrange
            var model = new SecretsModel(new Mock<IConfigurationDbContext>().Object);

            // Act

            var get = await model.OnGetAsync(0);

            // Assert
            Assert.Null(model.ApiResource);
            Assert.Null(model.Secrets);
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
            SecretsModel model;
            IActionResult get;

            // Act
            using (var context = new OidcDbContext(options))
            {
                model = new SecretsModel(context);
                get = await model.OnGetAsync(Random.Next());
            }

            // Assert
            Assert.Null(model.ApiResource);
            Assert.Null(model.Secrets);
            Assert.IsType<NotFoundResult>(get);
        }

        [Fact]
        public async Task OnPostAsync()
        {
            // Arrange
            const string secret1OriginalValue = "Original Value";
            const string secret1EditedValue = "Edited Value";
            const string newSecretValue = "New Value";
            var databaseName = $"{DatabaseNamePrefix}.{nameof(OnPostAsync)}";
            var options = new DbContextOptionsBuilder<OidcDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;
            SecretsModel secrets;
            IActionResult post;
            var secret1 = new ApiSecret
            {
                Id = Random.Next(),
                Value = secret1OriginalValue
            };
            var secret2 = new ApiSecret {Id = Random.Next()};
            var apiResource = new ApiResource
            {
                Id = Random.Next(),
                Secrets = new List<ApiSecret>
                {
                    secret1,
                    secret2
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
                secrets = new SecretsModel(context)
                {
                    ApiResource = new ApiResource
                    {
                        Id = apiResource.Id,
                        Secrets = new List<ApiSecret>
                        {
                            new ApiSecret
                            {
                                Id = secret1.Id,
                                Value = secret1EditedValue
                            },
                            new ApiSecret {Value = newSecretValue}
                        }
                    }
                };
                post = await secrets.OnPostAsync();
            }

            // Assert
            using (var context = new OidcDbContext(options))
            {
                apiResource = await context.ApiResources
                    .Include(x => x.Secrets)
                    .SingleOrDefaultAsync(x => x.Id.Equals(apiResource.Id));
                secret1 = apiResource.Secrets.SingleOrDefault(x => x.Id.Equals(secret1.Id));
                secret2 = apiResource.Secrets.SingleOrDefault(x => x.Id.Equals(secret2.Id));
                var newSecret = apiResource.Secrets.SingleOrDefault(x => x.Value.Equals(newSecretValue));

                Assert.NotNull(secret1);
                Assert.Equal(secret1EditedValue, secret1.Value);
                Assert.Null(secret2);
                Assert.NotNull(newSecret);
            }

            var result = Assert.IsType<RedirectToPageResult>(post);
            Assert.Equal("../Details/Secrets", result.PageName);
            Assert.Collection(result.RouteValues, routeValue =>
            {
                var (key, value) = routeValue;
                Assert.Equal(nameof(ApiResource.Id), key);
                Assert.Equal(secrets.ApiResource.Id, value);
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
            SecretsModel secrets;
            IActionResult post;
            var apiResource = new ApiResource
            {
                Id = Random.Next(),
                Secrets = new List<ApiSecret>
                {
                    new ApiSecret {Id = Random.Next()}
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
                secrets = new SecretsModel(context)
                {
                    ApiResource = new ApiResource {Id = apiResource.Id}
                };
                post = await secrets.OnPostAsync();
            }

            // Assert
            using (var context = new OidcDbContext(options))
            {
                apiResource = await context.ApiResources
                    .Include(x => x.Secrets)
                    .SingleOrDefaultAsync(x => x.Id.Equals(apiResource.Id));

                Assert.Empty(apiResource.Secrets);
            }

            var result = Assert.IsType<RedirectToPageResult>(post);
            Assert.Equal("../Details/Secrets", result.PageName);
            Assert.Collection(result.RouteValues, routeValue =>
            {
                var (key, value) = routeValue;
                Assert.Equal(nameof(ApiResource.Id), key);
                Assert.Equal(secrets.ApiResource.Id, value);
            });
        }

        [Fact]
        public async Task OnPostAsync_InvalidId()
        {
            // Arrange
            var context = new Mock<IConfigurationDbContext>();
            var secrets = new SecretsModel(context.Object)
            {
                ApiResource = new ApiResource {Id = 0}
            };

            // Act
            var post = await secrets.OnPostAsync();

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
                var secrets = new SecretsModel(context)
                {
                    ApiResource = new ApiResource {Id = Random.Next()}
                };
                post = await secrets.OnPostAsync();
            }

            // Assert
            Assert.IsType<PageResult>(post);
        }
    }
}
