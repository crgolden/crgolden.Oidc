namespace Cef.OIDC.Models.AccountModels
{
    using System.ComponentModel.DataAnnotations;

    public class LoginWithFacebookViewModel
    {
        [Required]
        public string AccessToken { get; set; }

        public long ExpiresIn { get; set; }

        public string GrantedScopes { get; set; }

        public long ReauthorizeRequiredIn { get; set; }

        public string SignedRequest { get; set; }

        /// <summary>
        /// The Facebook UserId
        /// </summary>
        [Required]
        public string UserId { get; set; }

        public string Message { get; set; }

        public bool Succeeded { get; set; }

        public bool IsLockedOut { get; set; }

        public bool RequiresTwoFactor { get; set; }

        public bool IsNotAllowed { get; set; }

        public bool Register { get; set; }

        public string Email { get; set; }
    }
}