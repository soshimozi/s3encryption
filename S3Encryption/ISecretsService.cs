using S3Encryption.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace S3Encryption
{
    interface ISecretsService
    {
        Task<Secret> PutSecretAsync(String p0, byte[] p1, Dictionary<String, String> p2);
        Task<Secret> GetSecretAsync(String p0, String p1);

        Task<Secret> RevokeSecret(String p0);

        Task<String> RecoverRevokedSecret(String p0);

        Task<List<Secret>> ListSecrets(String p0, bool p1);
    }
}
