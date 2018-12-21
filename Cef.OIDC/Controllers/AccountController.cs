namespace Cef.OIDC.Controllers
{
    using System.Collections.Generic;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using Extensions;
    using Models;
    using IdentityModel;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Identity.UI.Services;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using ViewModels.AccountViewModels;

    [AllowAnonymous]
    [ApiController]
    [Produces("application/json")]
    [Route("[controller]/[action]")]
    public class AccountController : ControllerBase
    {
        private readonly ILogger<AccountController> _logger;
        private readonly IEmailSender _emailSender;
        private readonly UserManager<User> _userManager;

        public AccountController(
            ILogger<AccountController> logger,
            IEmailSender emailSender,
            UserManager<User> userManager)
        {
            _logger = logger;
            _emailSender = emailSender;
            _userManager = userManager;
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordViewModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null && await _userManager.IsEmailConfirmedAsync(user))
            {
                await _emailSender.SendPasswordResetEmailAsync(
                    email: user.Email,
                    origin: Request.GetOrigin(),
                    code: await _userManager.GeneratePasswordResetTokenAsync(user));
            }

            return Ok("Please check your email to reset your password.");
        }

        [HttpPost]
        public async Task<IActionResult> Register([FromBody] RegisterViewModel model)
        {
            var user = new User { UserName = model.Email, Email = model.Email };
            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded) { return BadRequest(result.Errors); }

            _logger.LogInformation($"User with email '{user.Email}' created a new account with password.");
            await _userManager.AddToRoleAsync(user, "User");
            await _userManager.AddClaimsAsync(user, new List<Claim>
            {
                new Claim(JwtClaimTypes.GivenName, model.FirstName),
                new Claim(JwtClaimTypes.FamilyName, model.LastName),
                new Claim(ClaimTypes.Role, "User")
            });
            await _emailSender.SendConfirmationEmailAsync(
                userId: user.Id,
                email: user.Email,
                origin: Request.GetOrigin(),
                code: await _userManager.GenerateEmailConfirmationTokenAsync(user));
            return Ok("Registration successful! Please check your email for the confirmation link");
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordViewModel model)
        {
            const string success = "Your password has been reset.";
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return Ok(success);
            }

            var result = await _userManager.ResetPasswordAsync(
                user: user,
                token: model.Code,
                newPassword: model.Password);
            if (result.Succeeded)
            {
                _logger.LogInformation($"Password reset successfully for user with email '{model.Email}'");
                return Ok(success);
            }

            _logger.LogInformation($"Password reset failed for user with email '{model.Email}'");
            return BadRequest(result.Errors);
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
                _logger.LogInformation($"Email confirmed for user with ID '{user.Id}'");
                return Ok("Thank you for confirming your email.");
            }

            _logger.LogInformation($"Error confirming email for user with ID '{user.Id}'");
            return BadRequest(result.Errors);
        }
    }
}