using System;
using Amazon.S3.IO;
using Orchard.FileSystems.Media;
using System.Text.RegularExpressions;

namespace Codesanook.AmazonS3.Services
{
    public class AmazonS3StorageFolder : IStorageFolder
    {
        private readonly S3DirectoryInfo _s3DirectoryInfo;

        public AmazonS3StorageFolder(S3DirectoryInfo s3DirectoryInfo)
        {
            _s3DirectoryInfo = s3DirectoryInfo;
        }

        public string GetPath()
        {
            var path = Regex.Replace(_s3DirectoryInfo.FullName, @"^[^:]*:", "");
            return path;
        }

        public string GetName()
        {
            return _s3DirectoryInfo.Name;
        }

        public long GetSize()
        {
            return 1;
        }

        public DateTime GetLastUpdated()
        {
            return DateTime.MinValue;
        }

        public IStorageFolder GetParent()
        {
            return new AmazonS3StorageFolder(_s3DirectoryInfo.Parent);
        }
    }
}