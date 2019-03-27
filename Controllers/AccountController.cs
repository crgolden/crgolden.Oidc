namespace Clarity.Oidc
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using Core;
    using IdentityModel;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using Shared;

    [AllowAnonymous]
    [ApiController]
    [Produces("application/json")]
    [Route("[controller]/[action]")]
    public class AccountController : ControllerBase
    {
        private readonly ILogger<AccountController> _logger;
        private readonly IQueueClient _emailQueueClient;
        private readonly UserManager<User> _userManager;

        public AccountController(
            ILogger<AccountController> logger,
            IEnumerable<IQueueClient> queueClients,
            IOptions<ServiceBusOptions> serviceBusOptions,
            UserManager<User> userManager)
        {
            _logger = logger;
            _emailQueueClient = queueClients.Single(x => x.QueueName == serviceBusOptions.Value.EmailQueueName);
            _userManager = userManager;
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null && await _userManager.IsEmailConfirmedAsync(user))
            {
                await _emailQueueClient.SendPasswordResetEmailAsync(
                    email: user.Email,
                    origin: Request.GetOrigin(),
                    code: await _userManager.GeneratePasswordResetTokenAsync(user));
            }

            return Ok("Please check your email to reset your password.");
        }

        [HttpPost]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            var user = new User { UserName = model.Email, Email = model.Email };
            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded) { return BadRequest(result.Errors); }

            _logger.LogInformation($"User with email '{user.Email}' created a new account with password.");
            await _userManager.AddToRoleAsync(user, "User");
            var claims = new List<Claim>
            {
                new Claim(JwtClaimTypes.GivenName, model.FirstName),
                new Claim(JwtClaimTypes.FamilyName, model.LastName),
                new Claim(ClaimTypes.Role, "User")
            };
            if (model.Address != null)
            {
                var address = JsonConvert.SerializeObject(model.Address, new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                });
                claims.Add(new Claim(JwtClaimTypes.Address, address));
            }
            await _userManager.AddClaimsAsync(user, claims);
            await _emailQueueClient.SendConfirmationEmailAsync(
                userId: user.Id,
                email: user.Email,
                origin: Request.GetOrigin(),
                code: await _userManager.GenerateEmailConfirmationTokenAsync(user));
            return Ok("Registration successful! Please check your email for the confirmation link");
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel model)
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
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailModel model)
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