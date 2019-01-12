namespace Cef.OIDC.Extensions
{
    using System;
    using System.Text.Encodings.Web;
    using System.Threading.Tasks;
    using Core.Interfaces;

    public static class EmailSenderExtensions
    {
        public static async Task SendConfirmationEmailAsync(this IEmailSender emailSender,
            Guid userId, string email, string origin, string code)
        {
            var confirmEmailUrl = HtmlEncoder.Default.Encode($"{origin}/account/confirm-email?" +
                                                             $"userId={userId}&" +
                                                             $"code={Uri.EscapeDataString(code)}");
            var htmlMessage = $"Please confirm your account by <a href='{confirmEmailUrl}'>clicking here</a>.";
            await emailSender.SendEmailAsync(
                email: email,
                subject: "Confirm your email",
                htmlMessage: htmlMessage);
        }

        public static async Task SendPasswordResetEmailAsync(this IEmailSender emailSender,
            string email, string origin, string code)
        {
            // For more information on how to enable account confirmation and password reset please
            // visit https://go.microsoft.com/fwlink/?LinkID=532713
            var resetPasswordUrl = HtmlEncoder.Default.Encode($"{origin}/account/reset-password?" +
                                                              $"code={Uri.EscapeDataString(code)}");
            var htmlMessage = $"Please reset your password by <a href='{resetPasswordUrl}'>clicking here</a>";
            await emailSender.SendEmailAsync(
                email: email,
                subject: "Reset Password",
                htmlMessage: htmlMessage);
        }
    }
}