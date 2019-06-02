namespace crgolden.Oidc
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Text;
    using System.Text.Encodings.Web;
    using System.Threading.Tasks;
    using Microsoft.Azure.ServiceBus;

    [ExcludeFromCodeCoverage]
    public static class EmailQueueClientExtensions
    {
        public static async Task SendConfirmationEmailAsync(this IQueueClient emailQueueClient,
            Guid userId, string email, string origin, string code)
        {
            var confirmEmailUrl = HtmlEncoder.Default.Encode($"{origin}/account/confirm-email?" +
                                                             $"userId={userId}&" +
                                                             $"code={Uri.EscapeDataString(code)}");
            var htmlMessage = $"Please confirm your account by <a href='{confirmEmailUrl}'>clicking here</a>.";
            var body = Encoding.UTF8.GetBytes(htmlMessage);
            var message = new Message(body);
            message.UserProperties.Add("email", email);
            message.UserProperties.Add("subject", "Confirm your email");
            await emailQueueClient.SendAsync(message).ConfigureAwait(false);
        }

        public static async Task SendPasswordResetEmailAsync(this IQueueClient emailQueueClient,
            string email, string origin, string code)
        {
            // For more information on how to enable account confirmation and password reset please
            // visit https://go.microsoft.com/fwlink/?LinkID=532713
            var resetPasswordUrl = HtmlEncoder.Default.Encode($"{origin}/account/reset-password?" +
                                                              $"code={Uri.EscapeDataString(code)}");
            var htmlMessage = $"Please reset your password by <a href='{resetPasswordUrl}'>clicking here</a>";
            var body = Encoding.UTF8.GetBytes(htmlMessage);
            var message = new Message(body);
            message.UserProperties.Add("email", email);
            message.UserProperties.Add("subject", "Reset Password");
            await emailQueueClient.SendAsync(message).ConfigureAwait(false);
        }
    }
}