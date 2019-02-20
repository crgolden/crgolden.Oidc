namespace Clarity.Oidc.Extensions
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Text.Encodings.Web;
    using System.Threading.Tasks;
    using Core;

    [ExcludeFromCodeCoverage]
    public static class EmailServiceExtensions
    {
        public static async Task SendConfirmationEmailAsync(this IEmailService emailSender,
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

        public static async Task SendPasswordResetEmailAsync(this IEmailService emailSender,
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