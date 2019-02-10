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
    public class PropertiesFacts
    {
        private static Random Random => new Random();
        private static string DatabaseNamePrefix => typeof(PropertiesFacts).FullName;

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
                Properties = new List<ApiResourceProperty>
                {
                    new ApiResourceProperty(),
                    new ApiResourceProperty(),
                    new ApiResourceProperty()
                }
            };
            PropertiesModel model;
            IActionResult get;
            using (var context = new OidcDbContext(options))
            {
                context.Add(apiResource);
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new OidcDbContext(options))
            {
                model = new PropertiesModel(context);
                get = await model.OnGetAsync(apiResource.Id);
            }

            // Assert
            Assert.NotNull(model.ApiResource);
            Assert.Equal(apiResource.Id, model.ApiResource.Id);
            var properties = Assert.IsAssignableFrom<IEnumerable<ApiResourceProperty>>(model.Properties);
            Assert.Equal(apiResource.Properties.Count, properties.Count());
            Assert.IsType<PageResult>(get);
        }

        [Fact]
        public async Task OnGetAsync_InvalidId()
        {
            // Arrange
            var model = new PropertiesModel(new Mock<IConfigurationDbContext>().Object);

            // Act

            var get = await model.OnGetAsync(0);

            // Assert
            Assert.Null(model.ApiResource);
            Assert.Null(model.Properties);
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
            PropertiesModel model;
            IActionResult get;

            // Act
            using (var context = new OidcDbContext(options))
            {
                model = new PropertiesModel(context);
                get = await model.OnGetAsync(Random.Next());
            }

            // Assert
            Assert.Null(model.ApiResource);
            Assert.Null(model.Properties);
            Assert.IsType<NotFoundResult>(get);
        }

        [Fact]
        public async Task OnPostAsync()
        {
            // Arrange
            const string property1OriginalValue = "Original Value";
            const string property1EditedValue = "Edited Value";
            const string newPropertyValue = "New Value";
            var databaseName = $"{DatabaseNamePrefix}.{nameof(OnPostAsync)}";
            var options = new DbContextOptionsBuilder<OidcDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;
            PropertiesModel properties;
            IActionResult post;
            var property1 = new ApiResourceProperty
            {
                Id = Random.Next(),
                Value = property1OriginalValue
            };
            var property2 = new ApiResourceProperty {Id = Random.Next()};
            var apiResource = new ApiResource
            {
                Id = Random.Next(),
                Properties = new List<ApiResourceProperty>
                {
                    property1,
                    property2
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
                properties = new PropertiesModel(context)
                {
                    ApiResource = new ApiResource
                    {
                        Id = apiResource.Id,
                        Properties = new List<ApiResourceProperty>
                        {
                            new ApiResourceProperty
                            {
                                Id = property1.Id,
                                Value = property1EditedValue
                            },
                            new ApiResourceProperty {Value = newPropertyValue}
                        }
                    }
                };
                post = await properties.OnPostAsync();
            }

            // Assert
            using (var context = new OidcDbContext(options))
            {
                apiResource = await context.ApiResources
                    .Include(x => x.Properties)
                    .SingleOrDefaultAsync(x => x.Id.Equals(apiResource.Id));
                property1 = apiResource.Properties.SingleOrDefault(x => x.Id.Equals(property1.Id));
                property2 = apiResource.Properties.SingleOrDefault(x => x.Id.Equals(property2.Id));
                var newProperty = apiResource.Properties.SingleOrDefault(x => x.Value.Equals(newPropertyValue));

                Assert.NotNull(property1);
                Assert.Equal(property1EditedValue, property1.Value);
                Assert.Null(property2);
                Assert.NotNull(newProperty);
            }

            var result = Assert.IsType<RedirectToPageResult>(post);
            Assert.Equal("../Details/Properties", result.PageName);
            Assert.Collection(result.RouteValues, routeValue =>
            {
                var (key, value) = routeValue;
                Assert.Equal(nameof(ApiResource.Id), key);
                Assert.Equal(properties.ApiResource.Id, value);
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
            PropertiesModel properties;
            IActionResult post;
            var apiResource = new ApiResource
            {
                Id = Random.Next(),
                Properties = new List<ApiResourceProperty>
                {
                    new ApiResourceProperty {Id = Random.Next()}
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
                properties = new PropertiesModel(context)
                {
                    ApiResource = new ApiResource {Id = apiResource.Id}
                };
                post = await properties.OnPostAsync();
            }

            // Assert
            using (var context = new OidcDbContext(options))
            {
                apiResource = await context.ApiResources
                    .Include(x => x.Properties)
                    .SingleOrDefaultAsync(x => x.Id.Equals(apiResource.Id));

                Assert.Empty(apiResource.Properties);
            }

            var result = Assert.IsType<RedirectToPageResult>(post);
            Assert.Equal("../Details/Properties", result.PageName);
            Assert.Collection(result.RouteValues, routeValue =>
            {
                var (key, value) = routeValue;
                Assert.Equal(nameof(ApiResource.Id), key);
                Assert.Equal(properties.ApiResource.Id, value);
            });
        }

        [Fact]
        public async Task OnPostAsync_InvalidId()
        {
            // Arrange
            var context = new Mock<IConfigurationDbContext>();
            var properties = new PropertiesModel(context.Object)
            {
                ApiResource = new ApiResource {Id = 0}
            };

            // Act
            var post = await properties.OnPostAsync();

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
                var properties = new PropertiesModel(context)
                {
                    ApiResource = new ApiResource {Id = Random.Next()}
                };
                post = await properties.OnPostAsync();
            }

            // Assert
            Assert.IsType<PageResult>(post);
        }
    }
}
