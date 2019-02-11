﻿namespace Cef.OIDC.Tests.Pages.Account
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;
    using Cef.OIDC.Pages.Account;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.ModelBinding;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.AspNetCore.Mvc.Routing;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
    using Microsoft.AspNetCore.Routing;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Models;
    using Moq;
    using Xunit;

    [ExcludeFromCodeCoverage]
    public class LoginFacts
    {
        private readonly Mock<SignInManager<User>> _signInManager;
        private readonly Mock<UserManager<User>> _userManager;
        private readonly Mock<ILogger<LoginModel>> _logger;

        public LoginFacts()
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
            _signInManager = new Mock<SignInManager<User>>(
                _userManager.Object,
                Mock.Of<IHttpContextAccessor>(),
                Mock.Of<IUserClaimsPrincipalFactory<User>>(),
                Mock.Of<IOptions<IdentityOptions>>(),
                Mock.Of<ILogger<SignInManager<User>>>(),
                Mock.Of<IAuthenticationSchemeProvider>());
            _logger = new Mock<ILogger<LoginModel>>();
        }

        [Fact]
        public async Task OnGetAsync()
        {
            // Arrange
            const string returnUrl = "~/return-url";
            var httpContext = new DefaultHttpContext();
            var modelState = new ModelStateDictionary();
            var actionContext = new ActionContext(
                httpContext: httpContext,
                routeData: new RouteData(),
                actionDescriptor: new PageActionDescriptor(),
                modelState: modelState);
            var viewData = new ViewDataDictionary(
                metadataProvider: new EmptyModelMetadataProvider(),
                modelState: modelState);
            var handlerType = typeof(JwtBearerHandler);
            var authenticationSchemes = new List<AuthenticationScheme>
            {
                new AuthenticationScheme("Scheme1", "Scheme 1", handlerType),
                new AuthenticationScheme("Scheme2", "Scheme 2", handlerType),
                new AuthenticationScheme("Scheme3", "Scheme 3", handlerType)
            };
            _signInManager.Setup(x => x.GetExternalAuthenticationSchemesAsync()).ReturnsAsync(authenticationSchemes);
            var login = new LoginModel(_signInManager.Object, _userManager.Object, _logger.Object)
            {
                PageContext = new PageContext(actionContext)
                {
                    ViewData = viewData,
                    HttpContext = httpContext
                },
                Url = new UrlHelper(actionContext)
            };

            // Act
            var get = await login.OnGetAsync(returnUrl);

            // Assert
            Assert.IsType<PageResult>(get);
            Assert.Equal(login.ReturnUrl, returnUrl);
            Assert.Equal(login.ExternalLogins, authenticationSchemes);
        }

        [Fact]
        public async Task OnGetAsync_WithError()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var modelState = new ModelStateDictionary();
            var actionContext = new ActionContext(
                httpContext: httpContext,
                routeData: new RouteData(),
                actionDescriptor: new PageActionDescriptor(),
                modelState: modelState);
            var viewData = new ViewDataDictionary(
                metadataProvider: new EmptyModelMetadataProvider(),
                modelState: modelState);
            var login = new LoginModel(_signInManager.Object, _userManager.Object, _logger.Object)
            {
                PageContext = new PageContext(actionContext)
                {
                    ViewData = viewData,
                    HttpContext = httpContext
                },
                Url = new UrlHelper(actionContext),
                ErrorMessage = "Error"
            };

            // Act
            var get = await login.OnGetAsync();

            // Assert
            Assert.IsType<PageResult>(get);
            Assert.Equal(1, login.ModelState.ErrorCount);
        }

        [Fact]
        public async Task OnPostAsync()
        {
            // Arrange
            const string returnUrl = "~/return-url";
            const string password = "Password";
            const bool rememberMe = true;
            var user = new User
            {
                Email = "e@m.ail"
            };
            var httpContext = new DefaultHttpContext();
            var modelState = new ModelStateDictionary();
            var actionContext = new ActionContext(
                httpContext: httpContext,
                routeData: new RouteData(),
                actionDescriptor: new PageActionDescriptor(),
                modelState: modelState);
            var viewData = new ViewDataDictionary(
                metadataProvider: new EmptyModelMetadataProvider(),
                modelState: modelState);
            _userManager.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
            _signInManager
                .Setup(x => x.PasswordSignInAsync(user.Email, password, rememberMe, true))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
            var login = new LoginModel(_signInManager.Object, _userManager.Object, _logger.Object)
            {
                PageContext = new PageContext(actionContext)
                {
                    ViewData = viewData,
                    HttpContext = httpContext
                },
                Url = new UrlHelper(actionContext),
                Input = new LoginModel.InputModel
                {
                    Email = user.Email,
                    Password = password,
                    RememberMe = rememberMe
                },
                TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
            };

            // Act
            var post = await login.OnPostAsync(returnUrl);

            // Assert
            var result = Assert.IsType<LocalRedirectResult>(post);
            Assert.Equal(returnUrl, result.Url);
        }

        [Fact]
        public async Task OnPostAsync_InvalidUser()
        {
            // Arrange
            const string password = "Password";
            const bool rememberMe = true;
            var user = new User
            {
                Email = "e@m.ail"
            };
            var httpContext = new DefaultHttpContext();
            var modelState = new ModelStateDictionary();
            var actionContext = new ActionContext(
                httpContext: httpContext,
                routeData: new RouteData(),
                actionDescriptor: new PageActionDescriptor(),
                modelState: modelState);
            var viewData = new ViewDataDictionary(
                metadataProvider: new EmptyModelMetadataProvider(),
                modelState: modelState);
            _userManager.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(default(User));
            var login = new LoginModel(_signInManager.Object, _userManager.Object, _logger.Object)
            {
                PageContext = new PageContext(actionContext)
                {
                    ViewData = viewData,
                    HttpContext = httpContext
                },
                Url = new UrlHelper(actionContext),
                Input = new LoginModel.InputModel
                {
                    Email = user.Email,
                    Password = password,
                    RememberMe = rememberMe
                },
                TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
            };

            // Act
            var post = await login.OnPostAsync();

            // Assert
            Assert.IsType<PageResult>(post);
            _signInManager.Verify(
                x => x.PasswordSignInAsync(user.Email, password, rememberMe, true),
                Times.Never);
        }

        [Fact]
        public async Task OnPostAsync_NoReturnUrl_UserIsNotAdmin()
        {
            // Arrange
            const string password = "Password";
            const bool rememberMe = true;
            var user = new User
            {
                Email = "e@m.ail"
            };
            var httpContext = new DefaultHttpContext();
            var modelState = new ModelStateDictionary();
            var actionContext = new ActionContext(
                httpContext: httpContext,
                routeData: new RouteData(),
                actionDescriptor: new PageActionDescriptor(),
                modelState: modelState);
            var viewData = new ViewDataDictionary(
                metadataProvider: new EmptyModelMetadataProvider(),
                modelState: modelState);
            _userManager.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
            _userManager.Setup(x => x.IsInRoleAsync(user, "Admin")).ReturnsAsync(false);
            var login = new LoginModel(_signInManager.Object, _userManager.Object, _logger.Object)
            {
                PageContext = new PageContext(actionContext)
                {
                    ViewData = viewData,
                    HttpContext = httpContext
                },
                Url = new UrlHelper(actionContext),
                Input = new LoginModel.InputModel
                {
                    Email = user.Email,
                    Password = password,
                    RememberMe = rememberMe
                },
                TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
            };

            // Act
            var post = await login.OnPostAsync();

            // Assert
            Assert.IsType<PageResult>(post);
            _signInManager.Verify(
                x => x.PasswordSignInAsync(user.Email, password, rememberMe, true),
                Times.Never);
        }

        [Fact]
        public async Task OnPostAsync_RequiresTwoFactor()
        {
            // Arrange
            const string returnUrl = "~/return-url";
            const string password = "Password";
            const bool rememberMe = true;
            var user = new User
            {
                Email = "e@m.ail"
            };
            var httpContext = new DefaultHttpContext();
            var modelState = new ModelStateDictionary();
            var actionContext = new ActionContext(
                httpContext: httpContext,
                routeData: new RouteData(),
                actionDescriptor: new PageActionDescriptor(),
                modelState: modelState);
            var viewData = new ViewDataDictionary(
                metadataProvider: new EmptyModelMetadataProvider(),
                modelState: modelState);
            _userManager.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
            _signInManager
                .Setup(x => x.PasswordSignInAsync(user.Email, password, rememberMe, true))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.TwoFactorRequired);
            var login = new LoginModel(_signInManager.Object, _userManager.Object, _logger.Object)
            {
                PageContext = new PageContext(actionContext)
                {
                    ViewData = viewData,
                    HttpContext = httpContext
                },
                Url = new UrlHelper(actionContext),
                Input = new LoginModel.InputModel
                {
                    Email = user.Email,
                    Password = password,
                    RememberMe = rememberMe
                },
                TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
            };

            // Act
            var post = await login.OnPostAsync(returnUrl);

            // Assert
            var result = Assert.IsType<RedirectToPageResult>(post);
            Assert.Equal("./LoginWith2fa", result.PageName);
            Assert.Collection(
                result.RouteValues,
                routeValue1 =>
                {
                    var (key, value) = routeValue1;
                    Assert.Equal(nameof(returnUrl), key);
                    Assert.Equal(returnUrl, value);
                },
                routeValue2 =>
                {
                    var (key, value) = routeValue2;
                    Assert.Equal(nameof(login.Input.RememberMe), key);
                    Assert.Equal(rememberMe, value);
                });
        }

        [Fact]
        public async Task OnPostAsync_IsLockedOut()
        {
            // Arrange
            const string returnUrl = "~/return-url";
            const string password = "Password";
            const bool rememberMe = true;
            var user = new User
            {
                Email = "e@m.ail"
            };
            var httpContext = new DefaultHttpContext();
            var modelState = new ModelStateDictionary();
            var actionContext = new ActionContext(
                httpContext: httpContext,
                routeData: new RouteData(),
                actionDescriptor: new PageActionDescriptor(),
                modelState: modelState);
            var viewData = new ViewDataDictionary(
                metadataProvider: new EmptyModelMetadataProvider(),
                modelState: modelState);
            _userManager.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
            _signInManager
                .Setup(x => x.PasswordSignInAsync(user.Email, password, rememberMe, true))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);
            var login = new LoginModel(_signInManager.Object, _userManager.Object, _logger.Object)
            {
                PageContext = new PageContext(actionContext)
                {
                    ViewData = viewData,
                    HttpContext = httpContext
                },
                Url = new UrlHelper(actionContext),
                Input = new LoginModel.InputModel
                {
                    Email = user.Email,
                    Password = password,
                    RememberMe = rememberMe
                },
                TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
            };

            // Act
            var post = await login.OnPostAsync(returnUrl);

            // Assert
            var result = Assert.IsType<RedirectToPageResult>(post);
            Assert.Equal("./Lockout", result.PageName);
        }

        [Fact]
        public async Task OnPostAsync_IsNotAllowed()
        {
            // Arrange
            const string returnUrl = "~/return-url";
            const string password = "Password";
            const bool rememberMe = true;
            var user = new User
            {
                Email = "e@m.ail"
            };
            var httpContext = new DefaultHttpContext();
            var modelState = new ModelStateDictionary();
            var actionContext = new ActionContext(
                httpContext: httpContext,
                routeData: new RouteData(),
                actionDescriptor: new PageActionDescriptor(),
                modelState: modelState);
            var viewData = new ViewDataDictionary(
                metadataProvider: new EmptyModelMetadataProvider(),
                modelState: modelState);
            _userManager.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
            _signInManager
                .Setup(x => x.PasswordSignInAsync(user.Email, password, rememberMe, true))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.NotAllowed);
            var login = new LoginModel(_signInManager.Object, _userManager.Object, _logger.Object)
            {
                PageContext = new PageContext(actionContext)
                {
                    ViewData = viewData,
                    HttpContext = httpContext
                },
                Url = new UrlHelper(actionContext),
                Input = new LoginModel.InputModel
                {
                    Email = user.Email,
                    Password = password,
                    RememberMe = rememberMe
                },
                TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
            };

            // Act
            var post = await login.OnPostAsync(returnUrl);

            // Assert
            var result = Assert.IsType<RedirectToPageResult>(post);
            Assert.Equal("./VerifyEmail", result.PageName);
            Assert.Collection(
                result.RouteValues,
                routeValue =>
                {
                    var (key, value) = routeValue;
                    Assert.Equal(nameof(returnUrl), key);
                    Assert.Equal(returnUrl, value);
                });
        }

        [Fact]
        public async Task OnPostAsync_InvalidAttempt()
        {
            // Arrange
            const string returnUrl = "~/return-url";
            const string password = "Password";
            const bool rememberMe = true;
            var user = new User
            {
                Email = "e@m.ail"
            };
            var httpContext = new DefaultHttpContext();
            var modelState = new ModelStateDictionary();
            var actionContext = new ActionContext(
                httpContext: httpContext,
                routeData: new RouteData(),
                actionDescriptor: new PageActionDescriptor(),
                modelState: modelState);
            var viewData = new ViewDataDictionary(
                metadataProvider: new EmptyModelMetadataProvider(),
                modelState: modelState);
            _userManager.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
            _signInManager
                .Setup(x => x.PasswordSignInAsync(user.Email, password, rememberMe, true))
                .ReturnsAsync(new Microsoft.AspNetCore.Identity.SignInResult());
            var login = new LoginModel(_signInManager.Object, _userManager.Object, _logger.Object)
            {
                PageContext = new PageContext(actionContext)
                {
                    ViewData = viewData,
                    HttpContext = httpContext
                },
                Url = new UrlHelper(actionContext),
                Input = new LoginModel.InputModel
                {
                    Email = user.Email,
                    Password = password,
                    RememberMe = rememberMe
                },
                TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
            };

            // Act
            var post = await login.OnPostAsync(returnUrl);

            // Assert
            Assert.IsType<PageResult>(post);
            Assert.Equal(1, login.ModelState.ErrorCount);
        }
    }
}
