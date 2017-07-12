using System;
using System.IO;
using Amazon.S3.IO;
using Orchard.FileSystems.Media;
using PathUtils = System.IO.Path;
using System.Text.RegularExpressions;
using Codesanook.AmazonS3.Services;

namespace Codesanook.AmazonS3.Services
{
    public class AmazonS3StorageFile : IStorageFile
    {
        private readonly S3FileInfo _s3FileInfo;
        private readonly IAmazonS3StorageProvider _storageProvider;


        public AmazonS3StorageFile(S3FileInfo s3FileInfo, IAmazonS3StorageProvider storageProvider)
        {
            _s3FileInfo = s3FileInfo;
            _storageProvider = storageProvider;
        }

        public string GetPath()
        {
            var path = Regex.Replace(_s3FileInfo.FullName, @"^[^:]+:", "");
            return path;
        }

        public string GetName()
        {
            return _s3FileInfo.Name;
        }

        public long GetSize()
        {
            return _s3FileInfo.Length;
        }

        public DateTime GetLastUpdated()
        {
            return _s3FileInfo.LastWriteTime;
        }

        public string GetFileType()
        {
            return PathUtils.GetExtension(GetName());
        }

        public Stream OpenRead()
        {
            return _s3FileInfo.OpenRead();
        }

        public Stream OpenWrite()
        {
            var stream = _s3FileInfo.OpenWrite();
            return new AmazonS3StreamProxy(stream, _storageProvider, _s3FileInfo);
        }

        public Stream CreateFile()
        {
            if (_s3FileInfo.Exists)
            {
                return OpenWrite();
            }
            using (var stream = _s3FileInfo.Create()) { };
            _storageProvider.PublishFile(_s3FileInfo.FullName);
            return _s3FileInfo.OpenWrite();
        }

    }
}