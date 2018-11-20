namespace Cef.OIDC.Models.ManageModels
{
    using System.Collections.Generic;

    public class GenerateRecoveryCodesViewModel
    {
        public IEnumerable<string> RecoveryCodes { get; set; }

        public string Message { get; set; }
    }
}
