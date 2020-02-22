using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.UI.WebControls;
using Amazon.S3;
using Amazon.S3.IO;
using Amazon.S3.Model;
using Orchard.Environment.Extensions;
using Orchard.FileSystems.Media;
using PathUtils = System.IO.Path;
using System.Text.RegularExpressions;
using UrlHelper = Flurl.Url;
using Orchard.Settings;
using Orchard.ContentManagement;
using Codesanook.AmazonS3.Models;
using Amazon;

namespace Codesanook.AmazonS3.Services {
    [OrchardSuppressDependency("Orchard.FileSystems.Media.FileSystemStorageProvider")]
    public class AmazonS3StorageProvider : IAmazonS3StorageProvider, IStorageProvider {
        private readonly IAmazonS3Service amazonS3Service;
        private readonly ISiteService siteService;

        public AmazonS3StorageProvider(
            IAmazonS3Service amazonS3Service,
            ISiteService siteService
        ) {
            this.amazonS3Service = amazonS3Service;
            this.siteService = siteService;
        }

        private AwsS3SettingPart GetAwsS3Setting()
            => siteService.GetSiteSettings().As<AwsS3SettingPart>();

        public bool FileExists(string path) {
            return GetS3File(path).Exists;

        }

        public string GetPublicUrl(string path) {
            var setting = GetAwsS3Setting();
            return UrlHelper.Combine(
                setting.AwsS3PublicUrl,
                setting.AwsS3BucketName,
               CleanPath(path).Replace("\\", "/")
            );
        }

        public string GetStoragePath(string url) {
            var setting = GetAwsS3Setting();
            var rootPath = UrlHelper.Combine(
                setting.AwsS3PublicUrl,
                setting.AwsS3BucketName
            );

            if (string.IsNullOrWhiteSpace(url) || url.Length < rootPath.Length) {
                return rootPath;
            }

            return url.Substring(rootPath.Length);
        }

        public IStorageFile GetFile(string path) => new AmazonS3StorageFile(GetS3File(path), this);

        private S3FileInfo GetS3File(string path) {
            var setting = GetAwsS3Setting();
            path = CleanPath(path);
            var s3Client = amazonS3Service.GetS3Client();
            var file = new S3FileInfo(
                s3Client,
                setting.AwsS3BucketName,
                path);
            return file;
        }

        public static string CleanPath(string path) {
            if (string.IsNullOrWhiteSpace(path)) {
                return string.Empty;
            }

            path = Regex.Replace(path, @"^[^:]+\:", "");
            path = path.Replace("/", "\\");
            path = Regex.Replace(path, @"^[\\]+", "");
            return path;
        }

        public static string PathToKey(string path) {
            var result = Regex.Replace(path, @"^[^:]+\:", "");
            result = result.Replace("\\", "/");
            result = Regex.Replace(result, @"^[\/]+", "");
            return result;
        }

        public IEnumerable<IStorageFile> ListFiles(string path) {
            return GetDirectory(CleanPath(path))
            .GetFiles()
            .Where(x => !x.Name.EndsWith("_$folder$"))
            .Select(x => new AmazonS3StorageFile(x, this)).ToList();
        }

        public bool FolderExists(string path) => GetDirectory(path).Exists;

        public IEnumerable<IStorageFolder> ListFolders(string path) =>
            GetDirectory(path)
            .GetDirectories("*", SearchOption.TopDirectoryOnly)
            .Select(x => new AmazonS3StorageFolder(x)).ToList();

        public bool TryCreateFolder(string path) {
            try {
                var dir = GetDirectory(path);
                dir.Create();
            }
            catch {
                return false;
            }
            return true;
        }

        public void CreateFolder(string path) => GetDirectory(path).Create();
        public void DeleteFolder(string path) => GetDirectory(path).Delete();

        private S3DirectoryInfo GetDirectory(string path) {
            var setting = GetAwsS3Setting();
            var s3Client = amazonS3Service.GetS3Client();
            return new S3DirectoryInfo(
                s3Client,
                setting.AwsS3BucketName,
                CleanPath(path)
            );
        }

        public void RenameFolder(string oldPath, string newPath) {
            oldPath = CleanPath(oldPath);
            newPath = CleanPath(newPath);
            var oldDir = GetDirectory(oldPath);
            var newDir = GetDirectory(newPath);
            oldDir.MoveToLocal(newDir.FullName);
        }

        public void DeleteFile(string path) => GetS3File(path).Delete();

        public void RenameFile(string oldPath, string newPath) {
            var file = GetS3File(oldPath);
            newPath = CleanPath(newPath);
            file.MoveToLocal(newPath);
        }

        public void CopyFile(string originalPath, string duplicatePath) {
            var file = GetS3File(originalPath);
            duplicatePath = CleanPath(duplicatePath);
            file.CopyToLocal(duplicatePath);
        }

        public void PublishFile(string path) {
            var key = PathToKey(path);
            var setting = GetAwsS3Setting();
            var s3Client = amazonS3Service.GetS3Client();
            s3Client.PutACL(new PutACLRequest {
                BucketName = setting.AwsS3BucketName,
                Key = key,
                CannedACL = S3CannedACL.PublicRead
            });
        }

        public IStorageFile CreateFile(string path) {
            var file = GetS3File(path);
            using (file.Create()) { }
            PublishFile(path);
            return new AmazonS3StorageFile(file, this);
        }

        public bool TrySaveStream(string path, Stream inputStream) {
            try {
                path = CleanPath(path);
                SaveStream(path, inputStream);
            }
            catch {
                return false;
            }
            return true;
        }

        public void SaveStream(string path, Stream inputStream) {
            path = CleanPath(path);
            var file = GetS3File(path);
            var isNew = !file.Exists;
            using (var stream = file.Exists ? file.OpenWrite() : file.Create()) {
                inputStream.CopyTo(stream);
            }
            if (isNew) {
                PublishFile(path);
            }
        }

        public string Combine(string path1, string path2) => CleanPath(PathUtils.Combine(path1, path2));

        public List<S3Object> ListObjects(string prefix, Func<S3Object, bool> filterfFunc = null) {
            try {
                if (filterfFunc == null) {
                    filterfFunc = (obj) => { return true; };
                }
                prefix = CleanPath(prefix);
                var setting = GetAwsS3Setting();
                var result = new List<S3Object>();
                var request = new ListObjectsRequest {
                    BucketName = setting.AwsS3BucketName,
                    Prefix = prefix,
                    MaxKeys = 1000
                };

                do {
                    var s3Client = amazonS3Service.GetS3Client();
                    var response = s3Client.ListObjects(request);
                    result.AddRange(response.S3Objects.Where(x => filterfFunc(x)));

                    if (response.IsTruncated && response.S3Objects.Any()) {
                        request.Marker = response.NextMarker;
                    }
                    else {
                        break;
                    }
                } while (request != null);
                return result;
            }
            catch (AmazonS3Exception ex) {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                    return new List<S3Object>();
                throw;
            }
        }

        public Stream GetObjectStream(string path) => GetS3File(path).OpenRead();

        public void CreateBucketIfNotExist() {
            //TODO trim space
            var setting = GetAwsS3Setting();
            var client = amazonS3Service.GetS3Client();
            var s3directory = new S3DirectoryInfo(client, setting.AwsS3BucketName);

            if (!s3directory.Exists) {
                // Create a new buck if not exist
                var request = new PutBucketRequest {
                    BucketName = setting.AwsS3BucketName,
                    // To get localstack work we need to set bucket ACL to public read
                    // https://github.com/localstack/localstack/issues/406
                    CannedACL = setting.UseLocalStackS3
                        ? S3CannedACL.PublicRead
                        : S3CannedACL.Private
                };

                client.PutBucket(request);
            }
        }
    }
}

