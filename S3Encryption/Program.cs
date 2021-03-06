﻿using S3Encryption.CommandLineOptions;
using CommandLine;
using System;
using S3Encryption.Models;
using System.Collections.Generic;
using S3Encryption.Exceptions;
using S3Encryption.Utils;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using System.Xml;
using System.Reflection;

namespace S3Encryption
{
    class Program
    {
        static SecretsService _secretService;

        static int Main(string[] args)
        {
            _secretService = new SecretsService();

            var log4netConfig = new XmlDocument();
            log4netConfig.Load(File.OpenRead("log4net.config"));

            var repo = log4net.LogManager.CreateRepository(
                Assembly.GetEntryAssembly(), typeof(log4net.Repository.Hierarchy.Hierarchy));

            log4net.Config.XmlConfigurator.Configure(repo, log4netConfig["log4net"]);

            var parsedCommandLine = Parser.Default.ParseArguments<GetObjectVerbOptions, PutObjectVerbOptions>(args);

            var result = parsedCommandLine
                .MapResult(
                (GetObjectVerbOptions opts) => RunGetAndReturnExitCode(opts),
                (PutObjectVerbOptions opts) => RunPutAndReturnExitCode(opts),
                _ => { return 0; });

            return result;
        }

        private static int RunGetAndReturnExitCode(GetObjectVerbOptions opts)
        {
            _secretService.SetRegionByName(opts.Region);
            _secretService.KmsCmkId = opts.KmsKeyId;
            _secretService.BucketName = opts.S3BucketName;
            _secretService.AccountId = opts.AccountId;
            _secretService.AccountSecret = opts.AccountSecret;

            var task = Task.Run(() =>
            {
                return _secretService.GetSecretAsync(opts.SecretName, opts.SecretVersion);
            });

            task.Wait();

            File.WriteAllBytes(opts.Output, task.Result.Value);

            Console.WriteLine(JsonConvert.SerializeObject(task.Result));
            return 0;
        }

        private static int RunPutAndReturnExitCode(PutObjectVerbOptions opts)
        {
            _secretService.SetRegionByName(opts.Region);
            _secretService.KmsCmkId = opts.KmsKeyId;
            _secretService.BucketName = opts.S3BucketName;
            _secretService.AccountId = opts.AccountId;
            _secretService.AccountSecret = opts.AccountSecret;

            if (string.IsNullOrEmpty(opts.SecretValue) && !opts.GeneratePassword && !opts.GenerateSymetricKey)
            {
                throw new SecretsSdkException($"You must specify either --secret-value, --generate-password, or --generate-symetric-key");
            }

            var secretValueInBytes = GenerateSecretValue(opts.SecretValue, opts.GeneratePassword, opts.GenerateSymetricKey, opts.PasswordLength);
            if (secretValueInBytes == null)
            {
                return 0;
            }

            var container = new List<Secret>();
            // for each we do a put?
            foreach (var name in opts.SecretNames)
            {
                var task = Task.Run(() => {

                    return _secretService.PutSecretAsync(name, secretValueInBytes, new Dictionary<string, string>());
                });

                task.Wait();
                container.Add(task.Result);
            }

            Console.WriteLine(JsonConvert.SerializeObject(container));

            return 0;
        }

        private static byte[] GenerateSecretValue(string secretValue, bool generatePassword,
            bool generateSymmetricKey, int passwordLength)
        {

            if (generatePassword)
            {
                try
                {
                    return PasswordUtilities.GenerateSecuredPassword(passwordLength);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Cannot generate password - {e.Message}");
                    return null;
                }
            }

            if (generateSymmetricKey)
            {
                try
                {
                    return EncryptionUtilities.GenerateEncodedSymmetricKey();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Cannot generate symmetric key - {e.Message}");
                    return null;
                }
            }

            if (!File.Exists(secretValue))
            {
                Console.WriteLine("Argument \"--secret-value\" must refer to a valid filepath");
            }
            else
            {
                byte[] secretValueInBytes = File.ReadAllBytes(secretValue);
                if (secretValueInBytes != null && secretValueInBytes.Length != 0)
                {
                    return secretValueInBytes;
                }
                Console.WriteLine("Argument \"--secret-value\" has file without content");
            }
            return null;

        }
    }
}