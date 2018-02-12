using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace S3Encryption.CommandLineOptions
{
    [Verb("put", HelpText = "Put an object in an S3 bucket")]
    class PutObjectVerbOptions : IDefaultOptions
    {
        [Option('n', "secret-names", HelpText = "List of filenames to send", Required = true)]
        public IEnumerable<string> SecretNames { get; set; }

        [Option('p', "generate-password", HelpText = "Generate a password")]
        public bool GeneratePassword { get; set; }

        [Option('z', "generate-semetric-key", HelpText = "Generate a semetric key")]
        public bool GenerateSymetricKey { get; set; }

        [Option('l', "password-length")]
        public int PasswordLength { get; set; }


        [Option('v', "secret-value")]
        public string SecretValue { get; set; }

        public string S3BucketName { get; set; }
        public string Region { get; set; }
        public string KmsKeyId { get; set; }
    }
}
