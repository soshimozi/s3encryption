using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using S3Encryption.Models;
using Amazon;
using S3Encryption.Exceptions;
using Amazon.Runtime;
using Amazon.S3.Encryption;
using S3Encryption.Crypto;
using Amazon.S3.Model;
using System.IO;
using System.Security.Cryptography;
using S3Encryption.Utils;
using Amazon.KeyManagementService;
using Amazon.S3;

namespace S3Encryption
{
    public class SecretsService : ISecretsService
    {
        private AmazonS3Client _s3Client;

        private static readonly log4net.ILog logger =
            log4net.LogManager.GetLogger(typeof(SecretsService));

        public async Task<Secret> GetSecretAsync(string secretName, string secretVersion)
        {
            if (await IsSecretRevoked(secretName))
            {
                throw new SecretsSdkException($"Can't get revoked secret, {secretName}");
            }

            var objectKey = GetObjectKey(secretName);
            logger.Info($"get I-KEK from secret metadata {secretName}");
            var s3Client = GetS3Client();
            var getObjMetadataReq = new GetObjectMetadataRequest { BucketName = BucketName, Key = objectKey };
            if (!string.IsNullOrEmpty(secretVersion))
            {
                getObjMetadataReq.VersionId = secretVersion;
            }

            var metadataResponse = await s3Client.GetObjectMetadataAsync(getObjMetadataReq);
            var metadata = metadataResponse.Metadata;
            var ikekVersionId = metadata["x-intu-ikek"];
            byte[] decryptedIKEK = await DownloadIkek(GetIkekObjectKey(secretName), ikekVersionId);
            var ikek = RSA.Create();
            ikek.ImportEncodedParameters(decryptedIKEK);
            var s3EncryptedClient = GetS3EncryptionClient(new EncryptionMaterials(ikek), new AmazonS3CryptoConfiguration { RegionEndpoint = AwsRegion });

            var aSecret = new Secret()
            {
                Name = secretName,
                Version = metadataResponse.VersionId,
                UserMetadata = GetUserMetadata(metadata)
            };


            var getObjectRequest = new GetObjectRequest { BucketName = BucketName, Key = objectKey };
            if (!string.IsNullOrEmpty(secretVersion))
            {
                getObjectRequest.VersionId = secretVersion;
            }
            var downloadedSecret = await s3EncryptedClient.GetObjectAsync(getObjectRequest);

            aSecret.Value = await GetByteArray(downloadedSecret);
            aSecret.LastModified = downloadedSecret.LastModified;
            logger.Info($"downloaded secret {secretName}");

            return aSecret;
        }

        public async Task<List<Secret>> ListSecrets(string filter, bool includeAllVersions)
        {
            logger.Info($"list secrets filter={filter}, allVersion={includeAllVersions}");
            var secrets = new List<Secret>();

            var s3Client = GetS3Client();

            if (includeAllVersions)
            {
                var listRequest = new ListVersionsRequest { BucketName = BucketName };
                if (!string.IsNullOrEmpty(filter))
                {
                    listRequest.Prefix = filter;
                }

                var revokedSecrets = new List<string>();
                string currentObjectKey = null;
                ListVersionsResponse listResponse;
                do
                {
                    logger.Info($"list secrets form marker {listRequest.KeyMarker}");
                    listResponse = await s3Client.ListVersionsAsync(listRequest);

                    foreach (var summary in listResponse.Versions)
                    {
                        var objectKey = summary.Key;
                        if (objectKey.EndsWith(".isec"))
                        {
                            continue;
                        }

                        if (currentObjectKey?.CompareTo(objectKey) != 0)
                        {
                            currentObjectKey = objectKey;
                            if (summary.IsDeleteMarker)
                            {
                                revokedSecrets.Add(objectKey);
                                continue;
                            }
                        }

                        var secret = new Secret(objectKey.Substring(0, objectKey.LastIndexOf(".isec")))
                        {
                            Version = summary.VersionId,
                            LastModified = summary.LastModified
                        };
                        secrets.Add(secret);
                    }

                    listRequest.KeyMarker = listResponse.NextKeyMarker;
                } while (listResponse.IsTruncated);
            }
            else
            {
                var listRequest = new ListObjectsRequest { BucketName = BucketName };
                if (!string.IsNullOrEmpty(filter))
                {
                    listRequest.Prefix = filter;
                }

                ListObjectsResponse response;

                do
                {
                    logger.Info($"list secrets from marker {listRequest.Marker}");
                    response = await s3Client.ListObjectsAsync(listRequest);

                    foreach (var summary in response.S3Objects)
                    {
                        var objectKey = summary.Key;
                        if (!objectKey.EndsWith(".isec"))
                        {
                            continue;
                        }

                        var secret = new Secret(objectKey.Substring(0, objectKey.LastIndexOf(".isec")));
                        secret.LastModified = summary.LastModified;
                        secrets.Add(secret);
                    }

                    listRequest.Marker = response.NextMarker;
                } while (response.IsTruncated);
            }

            return secrets;
        }

        public async Task<Secret> PutSecretAsync(string secretName, byte[] secretValue, Dictionary<string, string> p2)
        {
            var objectKey = GetObjectKey(secretName);

            var ikek = EncryptionUtilities.GenerateSecretKey();

            var s3Client = GetS3EncryptionClient(new EncryptionMaterials(ikek), new AmazonS3CryptoConfiguration { RegionEndpoint = AwsRegion });

            logger.Info("check if s3 bucket versioning enabled");
            var versioned = await s3Client.GetBucketVersioningAsync(new GetBucketVersioningRequest { BucketName = BucketName });
            if(string.Compare("enabled", versioned.VersioningConfig.Status, true) != 0)
            {
                throw new SecretsSdkException($"s3 Bucket versioning is not enabled, {BucketName}");
            }

            var ikekVersionId = await UploadIkek(GetIkekObjectKey(secretName), ikek.GetEncoded());
            logger.Info($"encrypt and upload secret to {secretName}");

            var putRequest = new PutObjectRequest()
            {
                BucketName = BucketName,
                Key = objectKey,
                InputStream = new MemoryStream(secretValue)
            };
            putRequest.Metadata.Add("x-intu-ikek", ikekVersionId);

            var result = await s3Client.PutObjectAsync(putRequest);

            logger.Info($"uploaded secret to {secretName}");
            return new Secret(secretName)
            {
                Version = result.VersionId
            };
        }

        public async Task<string> RecoverRevokedSecret(string p0)
        {
            throw new NotImplementedException();
        }

        public async Task<Secret> RevokeSecret(string secretName)
        {
            var objectKey = GetObjectKey(secretName);
            var secretObjectKey = GetIkekObjectKey(secretName);

            var s3Client = GetS3Client();

            var deleteRequest = new DeleteObjectsRequest { BucketName = BucketName };
            deleteRequest.AddKey(objectKey);
            deleteRequest.AddKey(secretObjectKey);

            var deletedObjectsResponse = await s3Client.DeleteObjectsAsync(deleteRequest);
            var secret = new Secret(secretName);
            foreach(var deletedObject in deletedObjectsResponse.DeletedObjects)
            {
                if(objectKey.Equals(deletedObject.Key))
                {
                    secret.Version = deletedObject.VersionId;
                    break;
                }
            }

            logger.Info($"revoked secret {secretName} in bucket {BucketName}");
            return secret;
        }


        public RegionEndpoint AwsRegion
        {
            get;
            private set;
        }

        public void SetRegionByName(string regionName)
        {
            RegionEndpoint region;
            try
            {
                region = RegionEndpoint.GetBySystemName(regionName);
            }
            catch (Exception e)
            {
                throw new SecretsSdkException($"{regionName} is not a valid AWS region");
            }
            AwsRegion = region;
        }

        public String RegionName
        {
            get
            {
                return AwsRegion.SystemName;
            }
        }

        public String BucketName
        {
            get; set;
        }

        public String KmsCmkId
        {
            get; set;
        }

        public String Profile
        {
            get; set;
        }

        public String AccountId
        {
            get; set;
        }

        protected async Task<string> UploadIkek(string objectKeyName, byte[] ikek)
        {
            logger.Info($"encrypt I-KEK using {KmsCmkId}");
            logger.Info($"upload I-KEK to {objectKeyName}");

            using (var algorithm = new KMSAlgorithm(new AmazonKeyManagementServiceClient(AwsRegion), KmsCmkId))
            {
                var materials = new EncryptionMaterials(algorithm);
                using (var s3Client = GetS3EncryptionClient(materials, new AmazonS3CryptoConfiguration { RegionEndpoint = AwsRegion }))
                {
                    var putRequest = new PutObjectRequest
                    {
                        BucketName = BucketName,
                        Key = objectKeyName,
                        InputStream = new MemoryStream(ikek),
                        ContentType = "application/octet-stream",
                        ServerSideEncryptionKeyManagementServiceKeyId = KmsCmkId,
                        ServerSideEncryptionMethod = ServerSideEncryptionMethod.AWSKMS
                    };

                    var putResult = await s3Client.PutObjectAsync(putRequest);

                    logger.Info($"uploaded I-KEK to {objectKeyName}");
                    return putResult.VersionId;
                }
            }
        }

        protected async Task<byte[]> DownloadIkek(string objectKeyName, string versionId)
        {
            logger.Info($"download I-KEK from {objectKeyName} versionId={versionId}");
            var getObject = new GetObjectRequest { BucketName = BucketName, Key = objectKeyName };
            if (!string.IsNullOrEmpty(versionId)) {
                getObject.VersionId = versionId;
            }

            using (var algorithm = new KMSAlgorithm(new AmazonKeyManagementServiceClient(AwsRegion), KmsCmkId))
            {
                var materials = new EncryptionMaterials(algorithm);
                using (var s3Client = GetS3EncryptionClient(materials, new AmazonS3CryptoConfiguration { RegionEndpoint = AwsRegion }))
                {
                    var s3Object = await s3Client.GetObjectAsync(getObject);

                    using (var reader = new StreamReader(s3Object.ResponseStream))
                    {
                        var fileContents = await reader.ReadToEndAsync();

                        return ASCIIEncoding.UTF8.GetBytes(fileContents);
                    }
                }
            }
        }
    
        protected async Task<bool> IsSecretRevoked(string secretName)
        {
            var s3Client = GetS3Client();
            string objectName = GetObjectKey(secretName);
            ListVersionsRequest listRequest = new ListVersionsRequest { BucketName = BucketName, Prefix = objectName };
            bool isSecretRevoked = false;
            ListVersionsResponse listing;
            do {
                logger.Info($"list secrets from marker {listRequest.KeyMarker}");
                listRequest.MaxKeys = 1;
                listing = await s3Client.ListVersionsAsync(listRequest);
                if (null != listing.Versions && listing.Versions.Count > 0) {
                    var summary = listing.Versions[0];
                    if (summary.IsDeleteMarker) {
                        isSecretRevoked = true;
                        break;
                    }
                }
                listRequest.KeyMarker = listing.NextKeyMarker;
            } while (listing.IsTruncated);
            return isSecretRevoked;
        }

        protected AmazonS3Client GetS3Client()
        {
            if (null == _s3Client) {
                _s3Client = new AmazonS3Client(GetAwsCredentials(), new AmazonS3Config { RegionEndpoint = AwsRegion } );
            }
            return _s3Client;
        }

        private AWSCredentials GetAwsCredentials()
        {
            FallbackCredentialsFactory.CredentialsGenerators = new List<FallbackCredentialsFactory.CredentialsGenerator>
            {
                () => new StoredProfileAWSCredentials(Profile),
                () => new EnvironmentVariablesAWSCredentials()

            };

            var creds = FallbackCredentialsFactory.GetCredentials();

            return creds;
        }

        private AmazonS3EncryptionClient GetS3EncryptionClient(EncryptionMaterials materials, AmazonS3CryptoConfiguration cryptoConfig)
        {
            return new AmazonS3EncryptionClient(GetAwsCredentials(), cryptoConfig, materials);
        }

        private string GetObjectKey(string secretName)
        {
            return secretName + ".isec";
        }

        private string GetIkekObjectKey(string secretName)
        {
            return secretName + ".ikek";
        }

        private async Task<byte[]> GetByteArray(GetObjectResponse getObjectResponse)
        {
            using (var reader = new StreamReader(getObjectResponse.ResponseStream))
            {
                var data = await reader.ReadToEndAsync();

                return Encoding.UTF8.GetBytes(data);
            }
        }

        private Dictionary<string, string> GetUserMetadata(MetadataCollection objMetadata)
        {
            var userMetaData = new Dictionary<string, string>();
            foreach (var key in objMetadata.Keys)
            {
                if (!key.StartsWith("x-amz-", StringComparison.OrdinalIgnoreCase))
                {
                    if (key.StartsWith("x-intu-ikek", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    userMetaData.Add(key, objMetadata[key]);
                }
            }

            return userMetaData;
        }
    }
}
