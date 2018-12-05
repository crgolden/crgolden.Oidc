namespace Cef.OIDC.Extensions
{
    using System.IO;
    using System.Security.Cryptography.X509Certificates;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;

    public static class ConfigurationExtensions
    {
        public static X509Certificate2 GetRegistryCertificate(this IConfiguration configuration)
        {
            var thumbprint = configuration.GetValue<string>("CertificateThumbprint");
            if (string.IsNullOrEmpty(thumbprint)) { return null; }

            using (var certStore = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                X509Certificate2 cert = null;
                certStore.Open(OpenFlags.ReadOnly);
                var certCollection = certStore.Certificates.Find(
                    findType: X509FindType.FindByThumbprint,
                    findValue: thumbprint,
                    validOnly: false);
                if (certCollection.Count > 0) { cert = certCollection[0]; }

                return cert;
            }
        }

        public static X509Certificate2 GetLocalCertificate(this IConfiguration configuration, IHostingEnvironment environment)
        {
            /***********************************************************************************************
             *  Please note that here we are using a local certificate only for testing purposes. In a 
             *  real environment the certificate should be created and stored in a secure way
             **********************************************************************************************/

            var password = configuration.GetValue<string>("CertificatePassword");
            if (string.IsNullOrEmpty(password)) { return null; }

            var path = Path.Combine(
                path1: environment.ContentRootPath,
                path2: "Cef.OIDC.pfx");
            var cert = new X509Certificate2(
                fileName: path,
                password: password,
                keyStorageFlags: X509KeyStorageFlags.MachineKeySet);
            return cert;
        }
    }
}
