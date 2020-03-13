using System;
using System.Collections.Generic;
using System.Text;

namespace PoweredSoft.Docker.MysqlBackup.Storage.S3
{
    class S3Configuration
    {
        public bool Enabled { get; set; } = false;
        public string Endpoint { get; set; }
        public string BucketName { get; set; }
        public string AccessKey { get; set; }
        public string Secret { get; set; }
        public bool Minio { get; set; } = false;
    }
}
