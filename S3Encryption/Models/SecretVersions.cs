using S3Encryption.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace S3Encryption.Models
{
    public class SecretVersions
    {
        public SecretVersions()
        {
            Versions = new Dictionary<string, Secret>();
        }

        public string SecretName
        {
            get; set;
        }

        public Dictionary<string, Secret> Versions
        {
            get;
        }

        public void AddSecretVersion(Secret secret)
        {
            if(secret.Name.Equals(SecretName))
            {
                Versions.Add(secret.Version, secret);
                return;
            }

            throw new SecretsSdkException($"The secret with name {secret.Name} cannot be added to to the map of secrets with name {SecretName}");
        }

        public Secret GetSecretByVersion(string version)
        {
            if(this.Versions.ContainsKey(version))
            {
                return Versions[version];
            }

            return null;
        }

        public Secret GetCurrentVersionSecret()
        {
            Secret newest = null;
            foreach(var version in Versions)
            {
                if (newest == null)
                    newest = version.Value;
                else
                {
                    if(newest.LastModified.CompareTo(version.Value.LastModified) < 0)
                    {
                        newest = version.Value;
                    }
                }
            }

            return newest;
        }

    }
}
