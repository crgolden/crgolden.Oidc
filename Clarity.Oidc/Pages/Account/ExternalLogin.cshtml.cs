namespace Clarity.Oidc.Pages.Account
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using Core;
    using Extensions;
    using IdentityModel;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Logging;
    using Models;

    [AllowAnonymous]
    public class ExternalLoginModel : PageModel
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly IEmailService _emailService;
        private readonly ILogger<ExternalLoginModel> _logger;

        public ExternalLoginModel(
            SignInManager<User> signInManager,
            UserManager<User> userManager,
            IEmailService emailService,
            ILogger<ExternalLoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _emailService = emailService;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string LoginProvider { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        [TempData]
        [ViewData]
        public string Origin { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [Display(Name = "First Name")]
            public string FirstName { get; set; }

            [Required]
            [Display(Name = "Last Name")]
            public string LastName { get; set; }
        }

        public IActionResult OnGetAsync(string returnUrl = null)
        {
            TempData[nameof(Origin)] = Origin;
            returnUrl = returnUrl ?? Url.Content("~/");

            return RedirectToPage("./Login", new { returnUrl });
        }

        public IActionResult OnPost(string provider, string returnUrl = null, string origin = null)
        {
            // Request a redirect to the external login provider.
            var redirectUrl = Url.Page(
                pageName: "./ExternalLogin",
                pageHandler: "Callback",
                values: new { returnUrl, origin });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(
                provider: provider,
                redirectUrl: redirectUrl);
            if (!string.IsNullOrEmpty(origin))
            {
                properties.Items.Add("Referer", $"{origin}");
            }

            return new ChallengeResult(
                authenticationScheme: provider,
                properties: properties);
        }

        public async Task<IActionResult> OnGetCallbackAsync(string returnUrl = null, string origin = null, string remoteError = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");
            TempData[nameof(Origin)] = origin;

            if (!string.IsNullOrEmpty(remoteError))
            {
                ErrorMessage = $"Error from external provider: {remoteError}";
                return RedirectToPage("./Login", new { returnUrl });
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Error loading external login information.";
                return RedirectToPage("./Login", new { returnUrl });
            }

            var user = await _userManager.FindByLoginAsync(
                loginProvider: info.LoginProvider,
                providerKey: info.ProviderKey);
            if (user != null)
            {
                if (returnUrl.Equals(Url.Content("~/")) && !await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    return RedirectToPage("./Login");
                }

                // Sign in the user with this external login provider if the user already has a login.
                var result = await _signInManager.ExternalLoginSignInAsync(
                    loginProvider: info.LoginProvider,
                    providerKey: info.ProviderKey,
                    isPersistent: false,
                    bypassTwoFactor: true);
                if (result.Succeeded)
                {
                    _logger.LogInformation($"User account with email '{user.Email}' logged in with {info.LoginProvider} provider.");
                    return LocalRedirect(returnUrl);
                }

                if (result.IsLockedOut)
                {
                    _logger.LogWarning($"User account with email '{user.Email}' locked out.");
                    return RedirectToPage("./Lockout");
                }

                if (result.IsNotAllowed)
                {
                    return RedirectToPage("./VerifyEmail", new { returnUrl });
                }
            }
            else if (info.Principal.HasClaim(x => x.Type.Equals(ClaimTypes.Email)))
            {
                user = await _userManager.FindByEmailAsync(info.Principal.FindFirstValue(ClaimTypes.Email));
                if (user != null)
                {
                    var result = await _userManager.AddLoginAsync(
                        user: user,
                        login: info);
                    if (result.Succeeded)
                    {
                        return await UpdateUser(
                            user: user,
                            info: info,
                            returnUrl: returnUrl);
                    }
                }
            }

            // If the user does not have an account, then ask the user to create an account.
            Input = new InputModel();

            if (info.Principal.HasClaim(x => x.Type.Equals(ClaimTypes.Email)))
            {
                Input.Email = info.Principal.FindFirstValue(ClaimTypes.Email);
            }

            if (info.Principal.HasClaim(x => x.Type.Equals(ClaimTypes.GivenName)))
            {
                Input.FirstName = info.Principal.FindFirstValue(ClaimTypes.GivenName);
            }

            if (info.Principal.HasClaim(x => x.Type.Equals(ClaimTypes.Surname)))
            {
                Input.LastName = info.Principal.FindFirstValue(ClaimTypes.Surname);
            }

            LoginProvider = info.LoginProvider;
            Origin = origin;
            ReturnUrl = returnUrl;
            return Page();
        }

        public async Task<IActionResult> OnPostConfirmationAsync(string returnUrl = null, string origin = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");
            TempData[nameof(Origin)] = origin;

            // Get the information about the user from the external login provider
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Error loading external login information during confirmation.";
                return RedirectToPage("./Login", new { returnUrl });
            }

            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(Input.Email);
                if (user != null)
                {
                    var result = await _userManager.AddLoginAsync(
                        user: user,
                        login: info);
                    if (result.Succeeded)
                    {
                        return await UpdateUser(
                            user: user,
                            info: info,
                            returnUrl: returnUrl,
                            updateClaims: true);
                    }
                }
                else if (!returnUrl.Equals(Url.Content("~/")))
                {
                    user = new User { UserName = Input.Email, Email = Input.Email };
                    var result = await _userManager.CreateAsync(user);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation($"User with email '${user.Email}' created an account using {info.LoginProvider} provider.");
                        result = await _userManager.AddLoginAsync(
                            user: user,
                            login: info);
                        if (result.Succeeded)
                        {
                            await _userManager.AddToRoleAsync(user, "User");
                            await _emailService.SendConfirmationEmailAsync(
                                userId: user.Id,
                                email: user.Email,
                                origin: origin,
                                code: await _userManager.GenerateEmailConfirmationTokenAsync(user));
                            await _userManager.AddClaimsAsync(user, new List<Claim>
                            {
                                new Claim(JwtClaimTypes.GivenName, Input.FirstName),
                                new Claim(JwtClaimTypes.FamilyName, Input.LastName),
                                new Claim(ClaimTypes.Role, "User")
                            });
                            return RedirectToPage("./VerifyEmail", new { returnUrl });
                        }
                    }

                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }

            LoginProvider = info.LoginProvider;
            Origin = origin;
            ReturnUrl = returnUrl;
            return Page();
        }

        private async Task<IActionResult> UpdateUser(User user, UserLoginInfo info, string returnUrl, bool updateClaims = false)
        {
            if (updateClaims)
            {
                await UpdateClaims(user);
            }
            if (await _userManager.IsLockedOutAsync(user))
            {
                _logger.LogWarning($"User account with email '{user.Email}' locked out.");
                return RedirectToPage("./Lockout");
            }

            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                return RedirectToPage("./VerifyEmail", new { returnUrl });
            }

            if (returnUrl.Equals(Url.Content("~/")) && !await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return RedirectToPage("./Login");
            }

            _logger.LogInformation($"User account with email '{user.Email}' logged in with {info.LoginProvider} provider.");
            await _signInManager.SignInAsync(
                user: user,
                isPersistent: false);
            return LocalRedirect(returnUrl);
        }

        private async Task UpdateClaims(User user)
        {
            foreach (var claim in await _userManager.GetClaimsAsync(user))
            {
                switch (claim.Type)
                {
                    case JwtClaimTypes.GivenName:
                    {
                        await _userManager.ReplaceClaimAsync(
                            user: user,
                            claim: claim,
                            newClaim: new Claim(JwtClaimTypes.GivenName, Input.FirstName));
                        break;
                    }
                    case JwtClaimTypes.FamilyName:
                    {
                        await _userManager.ReplaceClaimAsync(
                            user: user,
                            claim: claim,
                            newClaim: new Claim(JwtClaimTypes.FamilyName, Input.LastName));
                        break;
                    }
                }
            }
        }
    }
}
