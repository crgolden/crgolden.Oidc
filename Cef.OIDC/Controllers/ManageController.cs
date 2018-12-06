namespace Cef.OIDC.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Encodings.Web;
    using System.Threading.Tasks;
    using Core.DbContexts;
    using Core.Models;
    using IdentityModel;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Identity.UI.Services;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Models.ManageModels;
    using Newtonsoft.Json;

    [Authorize(AuthenticationSchemes = "identity")]
    [ApiController]
    [Produces("application/json")]
    [Route("[controller]/[action]")]
    public class ManageController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly CefDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<ManageController> _logger;
        private readonly UrlEncoder _urlEncoder;

        private const string AuthenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}";

        public ManageController(
          UserManager<User> userManager,
          SignInManager<User> signInManager,
          CefDbContext context,
          IEmailSender emailSender,
          ILogger<ManageController> logger,
          UrlEncoder urlEncoder)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _emailSender = emailSender;
            _logger = logger;
            _urlEncoder = urlEncoder;
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (!changePasswordResult.Succeeded)
            {
                return BadRequest(changePasswordResult.Errors);
            }

            await _signInManager.RefreshSignInAsync(user);
            _logger.LogInformation($"User with email {user.Email} changed their password successfully.");
            return Ok("Your password has been changed.");
        }

        [HttpPost]
        public async Task<IActionResult> DeletePersonalData([FromBody] DeletePersonalDataViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (await _userManager.HasPasswordAsync(user) &&
                !await _userManager.CheckPasswordAsync(user, model.Password))
            {
                return BadRequest("Password not correct.");
            }

            var userId = await _userManager.GetUserIdAsync(user);
            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest($"Unexpected error occurred deleting user with ID '{userId}'.");
            }

            await _signInManager.SignOutAsync();
            _logger.LogInformation($"User with ID '{userId}' deleted themselves.");
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> Disable2fa()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var disable2faResult = await _userManager.SetTwoFactorEnabledAsync(user, false);
            if (!disable2faResult.Succeeded)
            {
                return BadRequest(disable2faResult.Errors);
            }

            _logger.LogInformation($"User with ID '{_userManager.GetUserId(User)}' has disabled 2fa.");
            return Ok("2fa has been disabled. You can reenable 2fa when you setup an authenticator app");
        }

        [HttpGet]
        public async Task<IActionResult> DownloadPersonalData()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            _logger.LogInformation($"User with ID '{user.Id}' asked for their personal data.");

            // Only include personal data for download
            var personalData = typeof(User)
                .GetProperties()
                .Where(x => Attribute.IsDefined(x, typeof(PersonalDataAttribute)))
                .ToDictionary(x => x.Name, p => p.GetValue(user)?.ToString() ?? "null");
            var firstName = User.Claims?.SingleOrDefault(x => x.Type == JwtClaimTypes.GivenName);
            if (firstName != null) { personalData.Add("First Name", firstName.Value); }

            var lastName = User.Claims?.SingleOrDefault(x => x.Type == JwtClaimTypes.FamilyName);
            if (lastName != null) { personalData.Add("Last Name", lastName.Value); }

            Response.Headers.Add("Content-Disposition", "attachment; filename=PersonalData.json");
            return new FileContentResult(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(personalData)), "text/json");
        }

        [HttpGet]
        public async Task<IActionResult> EnableAuthenticator()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(unformattedKey))
            {
                await _userManager.ResetAuthenticatorKeyAsync(user);
                unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            }

            return Ok(new EnableAuthenticatorViewModel
            {
                SharedKey = FormatKey(unformattedKey),
                AuthenticatorUri = GenerateQrCodeUri(await _userManager.GetEmailAsync(user), unformattedKey)
            });
        }

        [HttpGet]
        public async Task<IActionResult> ExternalAuthenticationSchemes()
        {
            return Ok(await _signInManager.GetExternalAuthenticationSchemesAsync());
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLogins()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var currentLogins = await _userManager.GetLoginsAsync(user);
            var externalAuthenticationSchemes = await _signInManager.GetExternalAuthenticationSchemesAsync();

            return new OkObjectResult(new ExternalLoginsViewModel
            {
                CurrentLogins = currentLogins,
                OtherLogins = externalAuthenticationSchemes
                    .Where(auth => currentLogins.All(ul => auth.Name != ul.LoginProvider))
                    .ToList(),
                ShowRemoveButton = user.PasswordHash != null || currentLogins.Count > 1
            });
        }

        [HttpPost]
        public async Task<IActionResult> ForgetTwoFactorClient()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await _signInManager.ForgetTwoFactorClientAsync();
            return Ok("The current browser has been forgotten. When you login again from this browser you will be prompted for your 2fa code.");
        }

        [HttpGet]
        public async Task<IActionResult> GenerateRecoveryCodes()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var isTwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
            var userId = await _userManager.GetUserIdAsync(user);
            if (!isTwoFactorEnabled)
            {
                return BadRequest($"Cannot generate recovery codes for user with ID '{userId}' as they do not have 2FA enabled.");
            }

            _logger.LogInformation("User with ID '{UserId}' has generated new 2FA recovery codes.", userId);
            return Ok(new GenerateRecoveryCodesViewModel
            {
                Message = "You have generated new recovery codes.",
                RecoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(
                    user: user,
                    number: 10)
            });
        }

        [HttpGet]
        public async Task<IActionResult> HasPassword()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            return Ok(await _userManager.HasPasswordAsync(user));
        }

        [HttpGet]
        public async Task<IActionResult> IsTwoFactorEnabled()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            return Ok(await _userManager.GetTwoFactorEnabledAsync(user));
        }

        [HttpPost]
        public async Task<IActionResult> Profile([FromBody] ProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var errors = new List<IdentityError>();

            if (model.Email != null && model.Email != await _userManager.GetEmailAsync(user))
            {
                var setEmailResult = await _userManager.SetEmailAsync(user, model.Email);
                if (!setEmailResult.Succeeded) { errors.AddRange(setEmailResult.Errors); }
            }

            if (model.PhoneNumber != null && model.PhoneNumber != await _userManager.GetPhoneNumberAsync(user))
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, model.PhoneNumber);
                if (!setPhoneResult.Succeeded) { errors.AddRange(setPhoneResult.Errors); }
            }

            var firstName = await _context.UserClaims.SingleOrDefaultAsync(x => x.ClaimType == JwtClaimTypes.GivenName && x.UserId == user.Id);
            if (model.FirstName != null && firstName != null && model.FirstName != firstName.ClaimValue)
            {
                firstName.ClaimValue = model.FirstName;
                await _context.SaveChangesAsync();
            }

            var lastName = await _context.UserClaims.SingleOrDefaultAsync(x => x.ClaimType == JwtClaimTypes.FamilyName && x.UserId == user.Id);
            if (model.LastName != null && lastName != null && model.LastName != lastName.ClaimValue)
            {
                lastName.ClaimValue = model.LastName;
                await _context.SaveChangesAsync();
            }

            if (errors.Any()) { return BadRequest(errors); }

            await _signInManager.RefreshSignInAsync(user);
            return Ok(new ProfileViewModel
            {
                Email = await _userManager.GetEmailAsync(user),
                EmailConfirmed = await _userManager.IsEmailConfirmedAsync(user),
                PhoneNumber = await _userManager.GetPhoneNumberAsync(user),
                PhoneNumberConfirmed = await _userManager.IsPhoneNumberConfirmedAsync(user),
                FirstName = user.Claims.SingleOrDefault(x => x.ClaimType == JwtClaimTypes.GivenName)?.ClaimValue,
                LastName = user.Claims.SingleOrDefault(x => x.ClaimType == JwtClaimTypes.FamilyName)?.ClaimValue
            });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveLogin([FromBody] RemoveLoginViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var result = await _userManager.RemoveLoginAsync(
                user: user,
                loginProvider: model.LoginProvider,
                providerKey: model.ProviderKey);
            if (!result.Succeeded)
            {
                var userId = await _userManager.GetUserIdAsync(user);
                return BadRequest($"Unexpected error occurred removing external login for user with ID '{userId}'.");
            }

            await _signInManager.RefreshSignInAsync(user);
            var currentLogins = await _userManager.GetLoginsAsync(user);
            var externalAuthenticationSchemes = await _signInManager.GetExternalAuthenticationSchemesAsync();
            return new OkObjectResult(new ExternalLoginsViewModel
            {
                CurrentLogins = currentLogins,
                OtherLogins = externalAuthenticationSchemes
                    .Where(auth => currentLogins.All(ul => auth.Name != ul.LoginProvider))
                    .ToList(),
                ShowRemoveButton = user.PasswordHash != null || currentLogins.Count > 1
            });
        }

        [HttpPost]
        public async Task<IActionResult> ResetAuthenticator()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await _userManager.SetTwoFactorEnabledAsync(user, false);
            await _userManager.ResetAuthenticatorKeyAsync(user);
            _logger.LogInformation($"User with id '{user.Id}' has reset their authentication app key.");

            await _signInManager.RefreshSignInAsync(user);
            return Ok("Your authenticator app key has been reset, you will need to configure your authenticator app using the new key.");
        }

        [HttpGet]
        public async Task<IActionResult> SendVerificationEmail()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var userId = await _userManager.GetUserIdAsync(user);
            var email = await _userManager.GetEmailAsync(user);
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { userId, code },
                protocol: Request.Scheme);
            await _emailSender.SendEmailAsync(
                email: email,
                subject: "Confirm your email",
                htmlMessage: $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

            return Ok("Verification email sent. Please check your email.");
        }

        [HttpPost]
        public async Task<IActionResult> SetPassword([FromBody] SetPasswordViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var addPasswordResult = await _userManager.AddPasswordAsync(user, model.NewPassword);
            if (!addPasswordResult.Succeeded)
            {
                return BadRequest(addPasswordResult.Errors);
            }

            await _signInManager.RefreshSignInAsync(user);
            _logger.LogInformation($"User with email '{user.Email}' set their password successfully.");
            return Ok("Your password has been set.");
        }

        [HttpGet]
        public async Task<IActionResult> TwoFactorAuthentication()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var model = new TwoFactorAuthenticationViewModel
            {
                HasAuthenticator = await _userManager.GetAuthenticatorKeyAsync(user) != null,
                Is2faEnabled = await _userManager.GetTwoFactorEnabledAsync(user),
                IsMachineRemembered = await _signInManager.IsTwoFactorClientRememberedAsync(user),
                RecoveryCodesLeft = await _userManager.CountRecoveryCodesAsync(user)
            };

            return Ok(model);
        }

        [HttpPost]
        public async Task<IActionResult> VerifyAuthenticator([FromBody] EnableAuthenticatorViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!await _userManager.VerifyTwoFactorTokenAsync(
                user: user,
                tokenProvider: _userManager.Options.Tokens.AuthenticatorTokenProvider,
                token: model.Code.Replace(" ", string.Empty).Replace("-", string.Empty)))
            {
                return BadRequest("Verification code is invalid.");
            }

            await _userManager.SetTwoFactorEnabledAsync(
                user: user,
                enabled: true);
            _logger.LogInformation($"User with ID '{user.Id}' has enabled 2FA with an authenticator app.");
            return Ok(new EnableAuthenticatorViewModel
            {
                Message = "Your authenticator app has been verified.",
                RecoveryCodes = await _userManager.CountRecoveryCodesAsync(user) == 0
                    ? await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(
                        user: user,
                        number: 10)
                    : new List<string>()
            });
        }

        #region Private

        private static string FormatKey(string unformattedKey)
        {
            var result = new StringBuilder();
            var currentPosition = 0;
            while (currentPosition + 4 < unformattedKey.Length)
            {
                result.Append(unformattedKey.Substring(currentPosition, 4)).Append(" ");
                currentPosition += 4;
            }
            if (currentPosition < unformattedKey.Length)
            {
                result.Append(unformattedKey.Substring(currentPosition));
            }

            return result.ToString().ToLowerInvariant();
        }

        private string GenerateQrCodeUri(string email, string unformattedKey)
        {
            return string.Format(
                AuthenticatorUriFormat,
                _urlEncoder.Encode("Cef.OIDC"),
                _urlEncoder.Encode(email),
                unformattedKey);
        }

        #endregion
    }
}
