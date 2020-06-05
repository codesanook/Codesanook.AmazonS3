using System;
using System.IO;
using Amazon.S3.IO;
using Orchard.FileSystems.Media;
using PathUtils = System.IO.Path;
using System.Text.RegularExpressions;

namespace Codesanook.AmazonS3.Services
{
    public class AmazonS3StorageFile : IStorageFile
    {
        private readonly S3FileInfo s3FileInfo;
        private readonly IAmazonS3StorageProvider storageProvider;

        public AmazonS3StorageFile(
            S3FileInfo s3FileInfo,
            IAmazonS3StorageProvider storageProvider
        )
        {
            this.s3FileInfo = s3FileInfo;
            this.storageProvider = storageProvider;
        }

        public string GetPath() => Regex.Replace(s3FileInfo.FullName, @"^[^:]+:", "");

        public string GetName() => s3FileInfo.Name;
        public long GetSize() => s3FileInfo.Length;
        public DateTime GetLastUpdated() => s3FileInfo.LastWriteTime;
        public string GetFileType() => PathUtils.GetExtension(GetName());
        public Stream OpenRead() => s3FileInfo.OpenRead();
        public Stream OpenWrite() => new AmazonS3StreamProxy(s3FileInfo.OpenWrite(), storageProvider, s3FileInfo);

        public Stream CreateFile()
        {
            if (s3FileInfo.Exists)
            {
                return OpenWrite();
            }

            using (var stream = s3FileInfo.Create()) { };
            storageProvider.PublishFile(s3FileInfo.FullName);
            return s3FileInfo.OpenWrite();
        }
    }
}

