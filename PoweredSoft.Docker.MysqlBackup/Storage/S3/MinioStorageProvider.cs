using Amazon.S3;
using PoweredSoft.Storage.S3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Amazon.Internal.RegionEndpointProviderV2;

namespace PoweredSoft.Docker.MysqlBackup.Storage.S3
{
    public class MinioStorageProvider : S3StorageProvider
    {
        public MinioStorageProvider(string endpoint, string bucketName, string accessKey, string secret) : base(endpoint, bucketName, accessKey, secret)
        {
        }

        protected override IAmazonS3 GetClient()
        {
            var config = new AmazonS3Config
            {
                USEast1RegionalEndpointValue = Amazon.Runtime.S3UsEast1RegionalEndpointValue.Legacy,
                ServiceURL = endpoint,
                ForcePathStyle = true
            };
            var client = new AmazonS3Client(this.accessKey, this.secret, config);
            return client;
        }
    }
}
