using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace S3Encryption.Exceptions
{
    public class SecretsSdkException : Exception
    {
        public SecretsSdkException(string message) : base(message)
        { }
    }
}
