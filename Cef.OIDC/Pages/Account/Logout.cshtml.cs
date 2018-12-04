namespace Cef.OIDC.Pages.Account
{
    using System;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using IdentityModel;
    using IdentityServer4;
    using IdentityServer4.Services;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Logging;

    [AllowAnonymous]
    public class LogoutModel : PageModel
    {
        private readonly IIdentityServerInteractionService _interaction;
        private readonly ILogger<LogoutModel> _logger;

        public LogoutModel(IIdentityServerInteractionService interaction, ILogger<LogoutModel> logger)
        {
            _interaction = interaction;
            _logger = logger;
        }

        public string LogoutId { get; set; }

        public async Task<IActionResult> OnGetAsync(string logoutId)
        {
            if (User.Identity.IsAuthenticated == false)
            {
                // if the user is not authenticated, then just show logged out page
                return await OnPostAsync(logoutId);
            }

            //Test for Xamarin. 
            var context = await _interaction.GetLogoutContextAsync(logoutId);
            if (context?.ShowSignoutPrompt == false)
            {
                //it's safe to automatically sign-out
                return await OnPostAsync(logoutId);
            }

            // show the logout prompt. this prevents attacks where the user
            // is automatically signed out by another malicious web page.
            LogoutId = logoutId;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string logoutId)
        {
            var idp = User?.FindFirst(JwtClaimTypes.IdentityProvider)?.Value;

            if (idp != null && !idp.Equals(IdentityServerConstants.LocalIdentityProvider))
            {
                if (string.IsNullOrEmpty(logoutId))
                {
                    // if there's no current logout context, we need to create one
                    // this captures necessary info from the current logged in user
                    // before we signout and redirect away to the external IdP for signout
                    logoutId = await _interaction.CreateLogoutContextAsync();
                }

                try
                {
                    // hack: try/catch to handle social providers that throw
                    await HttpContext.SignOutAsync(idp, new AuthenticationProperties
                    {
                        RedirectUri = $"/Account/Logout?logoutId={logoutId}"
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex.Message);
                }
            }

            // delete authentication cookie
            await HttpContext.SignOutAsync();

            await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);

            _logger.LogInformation("User logged out.");

            // set this so UI rendering sees an anonymous user
            HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

            // get context information (client name, post logout redirect URI and iframe for federated signout)
            var logout = await _interaction.GetLogoutContextAsync(logoutId);

            return Redirect(logout?.PostLogoutRedirectUri);
        }
    }
}