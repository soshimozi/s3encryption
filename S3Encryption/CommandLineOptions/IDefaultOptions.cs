using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace S3Encryption.CommandLineOptions
{
    interface IDefaultOptions
    {
        [Option('k', "kms-key-id", HelpText = "Id of KMS Key to use for serverside encryption", Required = true)]
        string KmsKeyId { get; set; }

        [Option('b', "s3-bucket-name", HelpText = "S3 Bucket name.", Required = true)]
        string S3BucketName { get; set; }

        [Option('r', "region", HelpText = "Region name", Required = true)]
        string Region { get; set; }

        [Option('i', "account-id", HelpText = "Account Id", Required = false)]
        string AccountId { get; set; }

        [Option('s', "account-secret", HelpText = "Account Secret", Required = false)]
        string AccountSecret { get; set; }

    }
}
