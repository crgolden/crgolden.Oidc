namespace Cef.OIDC.Pages.Account
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Threading.Tasks;
    using Models;
    using Extensions;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Logging;

    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(
            SignInManager<User> signInManager,
            UserManager<User> userManager,
            ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public IEnumerable<AuthenticationScheme> ExternalLogins { get; set; }

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
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            await _signInManager.SignOutAsync();

            ExternalLogins = await _signInManager.GetExternalAuthenticationSchemesAsync();
            ReturnUrl = returnUrl ?? Url.Content("~/");
            if (string.IsNullOrEmpty(Origin))
            {
                Origin = Request.GetOrigin();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null, string origin = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");
            TempData[nameof(Origin)] = origin;

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null || returnUrl.Equals(Url.Content("~/")) && !await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return Page();
            }

            var result = await _signInManager.PasswordSignInAsync(
                userName: Input.Email,
                password: Input.Password,
                isPersistent: Input.RememberMe,
                lockoutOnFailure: true);
            if (result.Succeeded)
            {
                _logger.LogInformation($"User with email '{Input.Email}' logged in.");
                return LocalRedirect(returnUrl);
            }

            if (result.RequiresTwoFactor)
            {
                return RedirectToPage("./LoginWith2fa", new { returnUrl, Input.RememberMe  });
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning($"User account with email '{Input.Email}' locked out.");
                return RedirectToPage("./Lockout");
            }

            if (result.IsNotAllowed)
            {
                return RedirectToPage("./VerifyEmail", new { returnUrl });
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            Origin = origin;
            ReturnUrl = returnUrl;
            return Page();
        }
    }
}
