using System;
using System.Collections.Generic;
using System.IO;
using Amazon.S3.Model;
using Orchard.FileSystems.Media;

namespace Codesanook.AmazonS3.Services
{
    public interface IAmazonS3StorageProvider : IStorageProvider
    {
        List<S3Object> ListObjects(
            string prefix,
            Func<S3Object, bool> filterfFunc = null
        );

        Stream GetObjectStream(string path);
        void PublishFile(string path);
        void CreateBucketIfNotExist(); 
    }
}
