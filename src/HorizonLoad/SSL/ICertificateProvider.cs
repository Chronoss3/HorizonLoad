
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace HorizonLoad.SSL
{
    public interface ICertificateProvider
    {
        public X509Certificate2 GetCertificate();
        public RSA GetPrivateRsaKey();
    }
}