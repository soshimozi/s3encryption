using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace S3Encryption.Models
{
    public class Secret
    {
        public Secret()
        {
        }

        public Secret(string secretName)
        {
            Name = secretName;
        }

        public string Name
        {
            get; set;
        }

        public string Version
        {
            get;
            set;
        }

        public byte[] Value
        {
            get; set;
        }

        public DateTime LastModified
        {
            get; set;
        }

        public Dictionary<string, string> UserMetadata
        {
            get; set;
        }
    }
}
