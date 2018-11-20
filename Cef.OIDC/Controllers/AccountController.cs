namespace Cef.OIDC.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Security.Claims;
    using System.Text.Encodings.Web;
    using System.Threading.Tasks;
    using Core.Models;
    using Core.Responses;
    using IdentityServer4.Events;
    using IdentityServer4.Extensions;
    using IdentityServer4.Services;
    using IdentityServer4.Stores;
    using Microsoft.AspNetCore.Antiforgery;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Identity.UI.Services;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Models.AccountModels;
    using Newtonsoft.Json;
    using Services;

    [AllowAnonymous]
    [ApiController]
    [Produces("application/json")]
    [Route("api/[controller]/[action]")]
    public class AccountController : ControllerBase
    {
        private readonly IIdentityServerInteractionService _interactionService;
        private readonly IClientStore _clientStore;
        private readonly IAuthenticationSchemeProvider _schemeProvider;
        private readonly IEventService _eventService;
        private readonly ILogger<AccountController> _logger;
        private readonly IEmailSender _emailSender;
        private readonly IAntiforgery _antiforgery;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly Core.Options.AuthenticationOptions _authenticationOptions;

        public AccountController(
            IIdentityServerInteractionService interactionService,
            IClientStore clientStore,
            IAuthenticationSchemeProvider schemeProvider,
            IEventService eventService,
            ILogger<AccountController> logger,
            IEmailSender emailSender,
            IAntiforgery antiforgery,
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IOptions<Core.Options.AuthenticationOptions> authenticationOptions)
        {
            _interactionService = interactionService;
            _clientStore = clientStore;
            _schemeProvider = schemeProvider;
            _eventService = eventService;
            _logger = logger;
            _emailSender = emailSender;
            _antiforgery = antiforgery;
            _userManager = userManager;
            _signInManager = signInManager;
            _authenticationOptions = authenticationOptions.Value;
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginViewModel model)
        {
            var result = await _signInManager.PasswordSignInAsync(
                userName: model.Email,
                password: model.Password,
                isPersistent: model.RememberMe,
                lockoutOnFailure: true);
            model.Password = null;
            if (result.Succeeded)
            {
                _logger.LogInformation($"Login succeeded for email: \"{model.Email}\"");
                var user = await _userManager.FindByEmailAsync(model.Email);
                await _signInManager.SignInAsync(
                    user: user,
                    authenticationProperties: new AuthenticationProperties
                    {
                        IsPersistent = model.RememberMe,
                        ExpiresUtc = model.RememberMe
                            ? (DateTime?)DateTime.Now.ToUniversalTime().AddDays(30)
                            : null
                    });
                model.Succeeded = true;
                await _eventService.RaiseAsync(new UserLoginSuccessEvent(
                    username: user.Email,
                    subjectId: $"{user.Id}",
                    name: user.Email));
            }
            else if (result.IsNotAllowed)
            {
                _logger.LogInformation($"User account not allowed for email: \"{model.Email}\"");
                model.IsNotAllowed = true;
                model.Message = "Email verification required.";
            }
            else if (result.IsLockedOut)
            {
                _logger.LogInformation($"User account locked out for email: \"{model.Email}\"");
                model.IsLockedOut = true;
            }
            else if (result.RequiresTwoFactor)
            {
                model.RequiresTwoFactor = true;
            }
            else
            {
                await _eventService.RaiseAsync(new UserLoginFailureEvent(
                    username: model.Email,
                    error: "invalid credentials"));
                model.Message = "Invalid login attempt.";
            }

            return Ok(model);
        }

        [HttpPost]
        public async Task<IActionResult> LoginWith2fa([FromBody] LoginWith2faViewModel model)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return BadRequest("Unable to load two-factor authentication user.");
            }

            var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(
                code: model.TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty),
                isPersistent: model.RememberMe,
                rememberClient: model.RememberMachine);
            model.TwoFactorCode = null;
            if (result.Succeeded)
            {
                _logger.LogInformation($"User with ID '{user.Id}' logged in with 2fa.");
                model.Succeeded = true;
            }
            else if (result.IsLockedOut)
            {
                _logger.LogWarning($"User with ID '{user.Id}' account locked out.");
                model.IsLockedOut = true;
            }
            else
            {
                _logger.LogWarning($"Invalid authenticator code entered for user with ID '{user.Id}'.");
                model.Message = "Invalid authenticator code.";
            }

            return Ok(model);
        }

        [HttpPost]
        public async Task<IActionResult> LoginWithRecoveryCode([FromBody] LoginWithRecoveryCodeViewModel model)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return BadRequest("Unable to load two-factor authentication user.");
            }

            var result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(
                recoveryCode: model.RecoveryCode.Replace(" ", string.Empty));
            model.RecoveryCode = null;
            if (result.Succeeded)
            {
                _logger.LogInformation($"User with ID {user.Id} logged in with a recovery code.");
                model.Succeeded = true;
            }
            else if (result.IsLockedOut)
            {
                _logger.LogWarning("User with ID {UserId} account locked out.", user.Id);
                model.IsLockedOut = true;
            }
            else
            {
                _logger.LogWarning($"Invalid recovery code entered for user with ID {user.Id}");
                model.Message = "Invalid recovery code entered.";
            }

            return Ok(model);
        }

        [HttpPost]
        public async Task<IActionResult> LoginWithFacebook([FromBody] LoginWithFacebookViewModel model)
        {
            FacebookUserData userData = null;
            var user = await _userManager.FindByLoginAsync(
                    loginProvider: "Facebook",
                    providerKey: model.UserId);
            var hasExternalLogin = user != null;
            if (user == null)
            {
                using (var client = new HttpClient())
                {
                    var appAccessToken = await GetFacebookAppAccessTokenAsync(client);
                    var userAccessTokenValidation = await GetFacebookUserAccessTokenValidationAsync(
                        client: client,
                        inputToken: model.AccessToken,
                        accessToken: appAccessToken?.AccessToken);
                    if (userAccessTokenValidation?.Data?.IsValid == true)
                    {
                        userData = await GetFacebookUserDataAsync(
                            client: client,
                            accessToken: model.AccessToken);
                    }
                }

                if (userData == null)
                {
                    return BadRequest("Unable to retrieve Facebook user data");
                }
                if (!string.IsNullOrEmpty(userData.Email))
                {
                    user = await _userManager.FindByEmailAsync(userData.Email);
                }
            }

            if (user != null)
            {
                if (!await _userManager.IsEmailConfirmedAsync(user))
                {
                    model.IsNotAllowed = true;
                    model.Message = "Email verification required.";
                }
                else if (await _userManager.GetTwoFactorEnabledAsync(user))
                {
                    model.RequiresTwoFactor = true;
                }
                else if (await _userManager.IsLockedOutAsync(user))
                {
                    _logger.LogWarning("User account locked out.");
                    model.IsLockedOut = true;
                }
                else
                {
                    await _signInManager.SignInAsync(user, true);
                    if (!hasExternalLogin)
                    {
                        await _userManager.AddLoginAsync(user, new UserLoginInfo(
                            loginProvider: "Facebook",
                            providerKey: $"{userData.Id}",
                            displayName: "Facebook"));
                    }
                    model.Succeeded = true;
                }
            }
            else
            {
                model.Register = true;
                model.Email = userData.Email;
            }

            return Ok(model);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            if (User?.Identity.IsAuthenticated != true)
            {
                return Ok();
            }

            await _signInManager.SignOutAsync();
            await _eventService.RaiseAsync(new UserLogoutSuccessEvent(
                subjectId: User.GetSubjectId(),
                name: User.GetDisplayName()));

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> Register([FromBody] RegisterViewModel model)
        {
            var user = new User { UserName = model.Email, Email = model.Email };
            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                _logger.LogInformation($"User creation failed for email: \"{model.Email}\"; Errors: {result.Errors}");
                return BadRequest(result.Errors);
            }

            _logger.LogInformation($"User created for email: \"{model.Email}\".");

            await AddUserClaimsAsync(user);
            await SendConfirmationEmailAsync(user);

            return Ok("Registration successful! Please check your email for the confirmation link.");
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{model.UserId}'.");
            }

            var result = await _userManager.ConfirmEmailAsync(user, model.Code);
            if (result.Succeeded)
            {
                return Ok("Email confirmed.");
            }

            _logger.LogInformation($"Email confirmation failed for user ID: \"{model.UserId}\", code: \"{model.Code}\"");
            return BadRequest(result.Errors);
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordViewModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null && await _userManager.IsEmailConfirmedAsync(user))
            {
                await SendPasswordResetEmailAsync(user);
            }

            return Ok("Please check your email to reset your password.");
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordViewModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return Ok();
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
            if (result.Succeeded)
            {
                return Ok();
            }

            _logger.LogInformation($"Password reset failed for email: \"{model.Email}\"; code: {model.Code}");
            return BadRequest(result.Errors);
        }

        #region Helpers

        private async Task SendConfirmationEmailAsync(User user)
        {
            var url = Request.Headers.TryGetValue("Origin", out var origin)
                ? $"{origin}"
                : $"{Request.Scheme}://{Request.Host}";
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmEmailUrl = HtmlEncoder.Default.Encode($"{url}/Account/ConfirmEmail?" +
                                                             $"userId={user.Id}&" +
                                                             $"code={Uri.EscapeDataString(code)}");
            var htmlMessage = $"Please confirm your account by <a href='{confirmEmailUrl}'>clicking here</a>.";
            await _emailSender.SendEmailAsync(user.Email, "Confirm your email", htmlMessage);
        }

        private async Task SendPasswordResetEmailAsync(User user)
        {
            var url = Request.Headers.TryGetValue("Origin", out var origin)
                ? $"{origin}"
                : $"{Request.Scheme}://{Request.Host}";
            // For more information on how to enable account confirmation and password reset please
            // visit https://go.microsoft.com/fwlink/?LinkID=532713
            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetPasswordUrl = HtmlEncoder.Default.Encode($"{url}/Account/ResetPassword?" +
                                                              $"code={Uri.EscapeDataString(code)}");
            var htmlMessage = $"Please reset your password by <a href='{resetPasswordUrl}'>clicking here</a>";
            await _emailSender.SendEmailAsync(user.Email, "Reset Password", htmlMessage);
        }

        private async Task<FacebookAppAccessToken> GetFacebookAppAccessTokenAsync(HttpClient client)
        {
            var appAccessTokenUri = "https://graph.facebook.com/oauth/access_token?" +
                                    $"client_id={_authenticationOptions.Facebook.AppId}&" +
                                    $"client_secret={_authenticationOptions.Facebook.AppSecret}&" +
                                    "grant_type=client_credentials";
            var appAccessTokenResponse = await client.GetStringAsync(appAccessTokenUri);
            return JsonConvert.DeserializeObject<FacebookAppAccessToken>(appAccessTokenResponse);
        }

        private static async Task<FacebookUserAccessTokenValidation> GetFacebookUserAccessTokenValidationAsync(HttpClient client, string inputToken, string accessToken)
        {
            var userAccessTokenValidationUri = "https://graph.facebook.com/debug_token?" +
                                               $"input_token={inputToken}&" +
                                               $"access_token={accessToken}";
            var userAccessTokenValidationResponse = await client.GetStringAsync(userAccessTokenValidationUri);
            return JsonConvert.DeserializeObject<FacebookUserAccessTokenValidation>(userAccessTokenValidationResponse);
        }

        private static async Task<FacebookUserData> GetFacebookUserDataAsync(HttpClient client, string accessToken)
        {
            var userInfoRequestUri = "https://graph.facebook.com/v2.8/me?" +
                                     "fields=id,email,first_name,last_name,name,picture&" +
                                     $"access_token={accessToken}";
            var userInfoResponse = await client.GetStringAsync(userInfoRequestUri);
            return JsonConvert.DeserializeObject<FacebookUserData>(userInfoResponse);
        }

        private async Task AddUserClaimsAsync(User user)
        {
            var claims = new List<Claim>();
            claims.AddRange(SeedData.Claims.Where(claim => claim.Type.Equals("Index")));
            claims.AddRange(SeedData.Claims.Where(claim => claim.Type.Equals("Details")));
            await _userManager.AddToRoleAsync(user, "User");
            await _userManager.AddClaimsAsync(user, claims);
        }

        private void AddAntiForgeryCookie()
        {
            var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
            HttpContext.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken, new CookieOptions
            {
                HttpOnly = false,
                SameSite = SameSiteMode.None,
                //TODO
                Secure = false
            });
        }

        #endregion
    }
}