using System;
using Amazon.S3.IO;
using Orchard.FileSystems.Media;
using System.Text.RegularExpressions;

namespace Codesanook.AmazonS3.Services
{
    public class AmazonS3StorageFolder : IStorageFolder
    {
        private readonly S3DirectoryInfo _s3DirectoryInfo;

        public AmazonS3StorageFolder(S3DirectoryInfo s3DirectoryInfo) =>
            _s3DirectoryInfo = s3DirectoryInfo;

        public string GetPath() => Regex.Replace(_s3DirectoryInfo.FullName, @"^[^:]*:", "");
        public string GetName() => _s3DirectoryInfo.Name;
        public long GetSize() => 1;
        public DateTime GetLastUpdated() => DateTime.MinValue;
        public IStorageFolder GetParent() =>
            new AmazonS3StorageFolder(_s3DirectoryInfo.Parent);
    }
}
