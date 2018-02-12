using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace S3Encryption.Models
{
    public class SecretContainer
    {
        public SecretContainer()
        {
            Secrets = new Dictionary<string, SecretVersions>();
        }

        public Dictionary<string, SecretVersions> Secrets
        {
            get; set;
        }

        public void AddSecretVersions(SecretVersions versions)
        {
            Secrets.Add(versions.SecretName, versions);
        }

        public SecretVersions GetSecretVersionsByName(string name)
        {
            if(Secrets.ContainsKey(name))
            {
                return Secrets[name];
            }

            return null;
        }
        
    }
}
