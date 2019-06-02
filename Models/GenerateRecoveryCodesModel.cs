namespace crgolden.Oidc
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    [ExcludeFromCodeCoverage]
    public class GenerateRecoveryCodesModel
    {
        public IEnumerable<string> RecoveryCodes { get; set; }

        public string Message { get; set; }
    }
}
