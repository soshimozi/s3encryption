using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace S3Encryption.CommandLineOptions
{
    [Verb("get", HelpText = "Get an object from an S3 bucket")]
    class GetObjectVerbOptions : IDefaultOptions
    {
        [Option('o', "output", HelpText = "Output file", Required = true)]
        public string Output { get; set; }

        [Option('n', "secret-name", HelpText = "Object key to download", Required = true)]
        public string SecretName { get; set; }

        [Option('t', "secret-version", HelpText = "Version to download")]
        public string SecretVersion { get; set; }

        public string S3BucketName { get; set; }
        public string Region { get; set; }
        public string KmsKeyId { get; set; }
        public string AccountId { get; set; }
        public string AccountSecret { get; set; }
    }
}
