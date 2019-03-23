namespace Clarity.Oidc
{
    using System.Collections.Generic;

    public class GenerateRecoveryCodesModel
    {
        public IEnumerable<string> RecoveryCodes { get; set; }

        public string Message { get; set; }
    }
}
