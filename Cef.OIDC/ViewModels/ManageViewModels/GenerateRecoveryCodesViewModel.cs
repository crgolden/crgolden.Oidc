namespace Cef.OIDC.ViewModels.ManageViewModels
{
    using System.Collections.Generic;

    public class GenerateRecoveryCodesViewModel
    {
        public IEnumerable<string> RecoveryCodes { get; set; }

        public string Message { get; set; }
    }
}
