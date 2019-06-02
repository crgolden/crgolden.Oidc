namespace crgolden.Oidc
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Claims;
    using System.Text;
    using System.Text.Encodings.Web;
    using System.Threading.Tasks;
    using Core;
    using IdentityModel;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using Shared;

    [Authorize(AuthenticationSchemes = "Bearer")]
    [ApiController]
    [Produces("application/json")]
    [Route("[controller]/[action]")]
    public class ManageController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IQueueClient _emailQueueClient;
        private readonly ILogger<ManageController> _logger;
        private readonly IConfiguration _configuration;
        private readonly UrlEncoder _urlEncoder;

        private const string AuthenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}";

        public ManageController(
          UserManager<User> userManager,
          SignInManager<User> signInManager,
          IEnumerable<IQueueClient> queueClients,
          IOptions<ServiceBusOptions> serviceBusOptions,
          ILogger<ManageController> logger,
          IConfiguration configuration,
          UrlEncoder urlEncoder)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailQueueClient = queueClients.Single(x => x.QueueName == serviceBusOptions.Value.EmailQueueName);
            _logger = logger;
            _configuration = configuration;
            _urlEncoder = urlEncoder;
        }

        [HttpPost]
        public virtual async Task<IActionResult> ChangePassword([FromBody] ChangePasswordModel model)
        {
            var user = await _userManager.GetUserAsync(User).ConfigureAwait(false);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(
                user: user,
                currentPassword: model.OldPassword,
                newPassword: model.NewPassword).ConfigureAwait(false);
            if (!changePasswordResult.Succeeded)
            {
                return BadRequest(changePasswordResult.Errors);
            }

            await _signInManager.RefreshSignInAsync(user).ConfigureAwait(false);
            _logger.LogInformation($"User with email {user.Email} changed their password successfully.");
            return Ok("Your password has been changed.");
        }

        [HttpPost]
        public virtual async Task<IActionResult> DeletePersonalData([FromBody] DeletePersonalDataModel model)
        {
            var user = await _userManager.GetUserAsync(User).ConfigureAwait(false);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (await _userManager.HasPasswordAsync(user).ConfigureAwait(false) &&
                !await _userManager.CheckPasswordAsync(user, model.Password).ConfigureAwait(false))
            {
                return BadRequest("Password not correct.");
            }

            var userId = await _userManager.GetUserIdAsync(user).ConfigureAwait(false);
            var result = await _userManager.DeleteAsync(user).ConfigureAwait(false);
            if (!result.Succeeded)
            {
                return BadRequest($"Unexpected error occurred deleting user with ID '{userId}'.");
            }

            await _signInManager.SignOutAsync().ConfigureAwait(false);
            _logger.LogInformation($"User with ID '{userId}' deleted themselves.");
            return Ok();
        }

        [HttpPost]
        public virtual async Task<IActionResult> Disable2fa()
        {
            var user = await _userManager.GetUserAsync(User).ConfigureAwait(false);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var disable2faResult = await _userManager.SetTwoFactorEnabledAsync(user, false).ConfigureAwait(false);
            if (!disable2faResult.Succeeded)
            {
                return BadRequest(disable2faResult.Errors);
            }

            _logger.LogInformation($"User with ID '{_userManager.GetUserId(User)}' has disabled 2fa.");
            return Ok("2fa has been disabled. You can reenable 2fa when you setup an authenticator app");
        }

        [HttpGet]
        public virtual async Task<IActionResult> DownloadPersonalData()
        {
            var user = await _userManager.GetUserAsync(User).ConfigureAwait(false);
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
            return new FileContentResult(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(personalData, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            })), "text/json");
        }

        [HttpGet]
        public virtual async Task<IActionResult> EnableAuthenticator()
        {
            var user = await _userManager.GetUserAsync(User).ConfigureAwait(false);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user).ConfigureAwait(false);
            if (string.IsNullOrEmpty(unformattedKey))
            {
                await _userManager.ResetAuthenticatorKeyAsync(user).ConfigureAwait(false);
                unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user).ConfigureAwait(false);
            }

            return Ok(new EnableAuthenticatorModel
            {
                SharedKey = FormatKey(unformattedKey),
                AuthenticatorUri = GenerateQrCodeUri(
                    issuer: _configuration.GetValue<string>("IdentityServerAddress"),
                    email: await _userManager.GetEmailAsync(user).ConfigureAwait(false),
                    secret: unformattedKey)
            });
        }

        [HttpGet]
        public virtual async Task<IActionResult> ExternalAuthenticationSchemes()
        {
            return Ok(await _signInManager.GetExternalAuthenticationSchemesAsync().ConfigureAwait(false));
        }

        [HttpGet]
        public virtual async Task<IActionResult> ExternalLogins()
        {
            var user = await _userManager.GetUserAsync(User).ConfigureAwait(false);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var currentLogins = await _userManager.GetLoginsAsync(user).ConfigureAwait(false);
            var externalAuthenticationSchemes = await _signInManager.GetExternalAuthenticationSchemesAsync().ConfigureAwait(false);

            return new OkObjectResult(new ExternalLoginsModel
            {
                CurrentLogins = currentLogins,
                OtherLogins = externalAuthenticationSchemes
                    .Where(auth => currentLogins.All(ul => auth.Name != ul.LoginProvider))
                    .ToList(),
                ShowRemoveButton = user.PasswordHash != null || currentLogins.Count > 1
            });
        }

        [HttpPost]
        public virtual async Task<IActionResult> ForgetTwoFactorClient()
        {
            var user = await _userManager.GetUserAsync(User).ConfigureAwait(false);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await _signInManager.ForgetTwoFactorClientAsync().ConfigureAwait(false);
            return Ok("The current browser has been forgotten. When you login again from this browser you will be prompted for your 2fa code.");
        }

        [HttpGet]
        public virtual async Task<IActionResult> GenerateRecoveryCodes()
        {
            var user = await _userManager.GetUserAsync(User).ConfigureAwait(false);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var isTwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user).ConfigureAwait(false);
            var userId = await _userManager.GetUserIdAsync(user).ConfigureAwait(false);
            if (!isTwoFactorEnabled)
            {
                return BadRequest($"Cannot generate recovery codes for user with ID '{userId}' as they do not have 2FA enabled.");
            }

            _logger.LogInformation("User with ID '{UserId}' has generated new 2FA recovery codes.", userId);
            return Ok(new GenerateRecoveryCodesModel
            {
                Message = "You have generated new recovery codes.",
                RecoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(
                    user: user,
                    number: 10).ConfigureAwait(false)
            });
        }

        [HttpGet]
        public virtual async Task<IActionResult> HasPassword()
        {
            var user = await _userManager.GetUserAsync(User).ConfigureAwait(false);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            return Ok(await _userManager.HasPasswordAsync(user).ConfigureAwait(false));
        }

        [HttpGet]
        public virtual async Task<IActionResult> IsTwoFactorEnabled()
        {
            var user = await _userManager.GetUserAsync(User).ConfigureAwait(false);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            return Ok(await _userManager.GetTwoFactorEnabledAsync(user).ConfigureAwait(false));
        }

        [HttpPost]
        public virtual async Task<IActionResult> Profile([FromBody] ProfileModel model)
        {
            var user = await _userManager.GetUserAsync(User).ConfigureAwait(false);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var errors = new List<IdentityError>();

            if (model.Email != user.Email)
            {
                user.Email = model.Email;
                user.EmailConfirmed = false;

                var updateResult = await _userManager.UpdateAsync(user).ConfigureAwait(false);
                if (!updateResult.Succeeded)
                {
                    errors.AddRange(updateResult.Errors);
                }
                else
                {
                    model.EmailConfirmed = false;
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user).ConfigureAwait(false);
                    await _emailQueueClient.SendConfirmationEmailAsync(
                        userId: user.Id,
                        email: user.Email,
                        origin: Request.GetOrigin(),
                        code: code).ConfigureAwait(false);
                }
            }

            if (!string.IsNullOrEmpty(model.PhoneNumber) && model.PhoneNumber != user.PhoneNumber)
            {
                user.PhoneNumber = model.PhoneNumber;
                user.PhoneNumberConfirmed = false;

                var updateResult = await _userManager.UpdateAsync(user).ConfigureAwait(false);
                if (!updateResult.Succeeded)
                {
                    errors.AddRange(updateResult.Errors);
                }
                else
                {
                    model.PhoneNumberConfirmed = false;
                }
            }

            var userClaims = await _userManager.GetClaimsAsync(user).ConfigureAwait(false);
            foreach (var claim in userClaims)
            {
                switch (claim.Type)
                {
                    case JwtClaimTypes.GivenName:
                        {
                            if (model.FirstName != claim.Value)
                            {
                                await _userManager.ReplaceClaimAsync(
                                    user: user,
                                    claim: claim,
                                    newClaim: new Claim(JwtClaimTypes.GivenName, model.FirstName)).ConfigureAwait(false);
                            }
                            break;
                        }
                    case JwtClaimTypes.FamilyName:
                        {
                            if (model.LastName != claim.Value)
                            {
                                await _userManager.ReplaceClaimAsync(
                                    user: user,
                                    claim: claim,
                                    newClaim: new Claim(JwtClaimTypes.FamilyName, model.LastName)).ConfigureAwait(false);
                            }
                            break;
                        }
                    case JwtClaimTypes.Address:
                        {
                            if (model.Address != null)
                            {
                                var address = JsonConvert.SerializeObject(model.Address, new JsonSerializerSettings
                                {
                                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                                });
                                if (!string.IsNullOrEmpty(address) && address != claim.Value)
                                {
                                    await _userManager.ReplaceClaimAsync(
                                        user: user,
                                        claim: claim,
                                        newClaim: new Claim(JwtClaimTypes.Address, address)).ConfigureAwait(false);
                                }
                            }
                            else
                            {
                                await _userManager.RemoveClaimAsync(user, claim).ConfigureAwait(false);
                            }
                            break;
                        }
                }
            }

            if (model.Address != null && userClaims.All(x => x.Type != JwtClaimTypes.Address))
            {
                var addressClaim = new Claim(JwtClaimTypes.Address, JsonConvert.SerializeObject(model.Address, new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                }));
                await _userManager.AddClaimAsync(user, addressClaim).ConfigureAwait(false);
            }

            if (errors.Any()) { return BadRequest(errors); }

            await _signInManager.RefreshSignInAsync(user).ConfigureAwait(false);
            return Ok(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> RemoveLogin([FromBody] RemoveLoginModel model)
        {
            var user = await _userManager.GetUserAsync(User).ConfigureAwait(false);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var result = await _userManager.RemoveLoginAsync(
                user: user,
                loginProvider: model.LoginProvider,
                providerKey: model.ProviderKey).ConfigureAwait(false);
            if (!result.Succeeded)
            {
                var userId = await _userManager.GetUserIdAsync(user).ConfigureAwait(false);
                return BadRequest($"Unexpected error occurred removing external login for user with ID '{userId}'.");
            }

            await _signInManager.RefreshSignInAsync(user).ConfigureAwait(false);
            var currentLogins = await _userManager.GetLoginsAsync(user).ConfigureAwait(false);
            var externalAuthenticationSchemes = await _signInManager.GetExternalAuthenticationSchemesAsync().ConfigureAwait(false);
            return new OkObjectResult(new ExternalLoginsModel
            {
                CurrentLogins = currentLogins,
                OtherLogins = externalAuthenticationSchemes
                    .Where(auth => currentLogins.All(ul => auth.Name != ul.LoginProvider))
                    .ToList(),
                ShowRemoveButton = user.PasswordHash != null || currentLogins.Count > 1
            });
        }

        [HttpPost]
        public virtual async Task<IActionResult> ResetAuthenticator()
        {
            var user = await _userManager.GetUserAsync(User).ConfigureAwait(false);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await _userManager.SetTwoFactorEnabledAsync(user, false).ConfigureAwait(false);
            await _userManager.ResetAuthenticatorKeyAsync(user).ConfigureAwait(false);
            _logger.LogInformation($"User with id '{user.Id}' has reset their authentication app key.");

            await _signInManager.RefreshSignInAsync(user).ConfigureAwait(false);
            return Ok("Your authenticator app key has been reset, you will need to configure your authenticator app using the new key.");
        }

        [HttpGet]
        public virtual async Task<IActionResult> SendVerificationEmail(string origin = null)
        {
            var user = await _userManager.GetUserAsync(User).ConfigureAwait(false);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user).ConfigureAwait(false);
            await _emailQueueClient.SendConfirmationEmailAsync(
                userId: user.Id,
                email: user.Email,
                origin: origin ?? Request.GetOrigin(),
                code: code).ConfigureAwait(false);

            return Ok("Verification email sent. Please check your email.");
        }

        [HttpPost]
        public virtual async Task<IActionResult> SetPassword([FromBody] SetPasswordModel model)
        {
            var user = await _userManager.GetUserAsync(User).ConfigureAwait(false);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var addPasswordResult = await _userManager.AddPasswordAsync(user, model.NewPassword).ConfigureAwait(false);
            if (!addPasswordResult.Succeeded)
            {
                return BadRequest(addPasswordResult.Errors);
            }

            await _signInManager.RefreshSignInAsync(user).ConfigureAwait(false);
            _logger.LogInformation($"User with email '{user.Email}' set their password successfully.");
            return Ok("Your password has been set.");
        }

        [HttpGet]
        public virtual async Task<IActionResult> TwoFactorAuthentication()
        {
            var user = await _userManager.GetUserAsync(User).ConfigureAwait(false);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var model = new TwoFactorAuthenticationModel
            {
                HasAuthenticator = await _userManager.GetAuthenticatorKeyAsync(user).ConfigureAwait(false) != null,
                Is2faEnabled = await _userManager.GetTwoFactorEnabledAsync(user).ConfigureAwait(false),
                IsMachineRemembered = await _signInManager.IsTwoFactorClientRememberedAsync(user).ConfigureAwait(false),
                RecoveryCodesLeft = await _userManager.CountRecoveryCodesAsync(user).ConfigureAwait(false)
            };

            return Ok(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> VerifyAuthenticator([FromBody] EnableAuthenticatorModel model)
        {
            var user = await _userManager.GetUserAsync(User).ConfigureAwait(false);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!await _userManager.VerifyTwoFactorTokenAsync(
                user: user,
                tokenProvider: _userManager.Options.Tokens.AuthenticatorTokenProvider,
                token: model.Code.Replace(" ", string.Empty).Replace("-", string.Empty)).ConfigureAwait(false))
            {
                return BadRequest("Verification code is invalid.");
            }

            await _userManager.SetTwoFactorEnabledAsync(
                user: user,
                enabled: true).ConfigureAwait(false);
            _logger.LogInformation($"User with ID '{user.Id}' has enabled 2FA with an authenticator app.");
            return Ok(new EnableAuthenticatorModel
            {
                Message = "Your authenticator app has been verified.",
                RecoveryCodes = await _userManager.CountRecoveryCodesAsync(user).ConfigureAwait(false) == 0
                    ? await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(
                        user: user,
                        number: 10).ConfigureAwait(false)
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

        private string GenerateQrCodeUri(string issuer, string email, string secret)
        {
            return string.Format(
                format: AuthenticatorUriFormat,
                arg0: _urlEncoder.Encode(issuer),
                arg1: _urlEncoder.Encode(email),
                arg2: secret);
        }

        #endregion
    }
}
