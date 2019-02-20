namespace Clarity.Oidc.ViewModels.ManageViewModels
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    [ExcludeFromCodeCoverage]
    public class GenerateRecoveryCodesViewModel
    {
        public IEnumerable<string> RecoveryCodes { get; set; }

        public string Message { get; set; }
    }
}
