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
using Amazon.Runtime;
using Codesanook.Common.Models;

namespace Codesanook.AmazonS3.Services {
    [OrchardSuppressDependency("Orchard.FileSystems.Media.FileSystemStorageProvider")]
    public class AmazonS3StorageProvider : IAmazonS3StorageProvider, IStorageProvider {
        private readonly ISiteService siteService;

        public AmazonS3StorageProvider(
            ISiteService siteService
        ) {
            this.siteService = siteService;
        }

        private AwsS3SettingPart GetAwsS3Setting()
            => siteService.GetSiteSettings().As<AwsS3SettingPart>();

        private IAmazonS3 GetS3Client() {
            var awsS3Setting = GetAwsS3Setting();
            if (awsS3Setting.UseLocalS3rver) {
                var credentials = new BasicAWSCredentials("", "");
                var config = new AmazonS3Config {
                    ServiceURL = awsS3Setting.LocalS3rverServiceUrl,
                    UseHttp = true,
                    ForcePathStyle = true,
                };
                return new AmazonS3Client(credentials, config);
            }
            else {

                var sharedSetting = siteService.GetSiteSettings().As<SharedSettingPart>();
                var credentials = new BasicAWSCredentials(
                    sharedSetting.AwsAccessKey,
                    sharedSetting.AwsSecretKey
                );
                var config = new AmazonS3Config {
                    ServiceURL = awsS3Setting.AwsS3ServiceUrl,
                    UseHttp = false,
                };
                return new AmazonS3Client(credentials, config);
            }

        }

        public bool FileExists(string path) => GetS3File(path).Exists;

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

        public IStorageFile GetFile(string path) =>
            new AmazonS3StorageFile(GetS3File(path), this);

        private S3FileInfo GetS3File(string path) {
            var setting = GetAwsS3Setting();
            using (var s3Client = GetS3Client()) {
                path = CleanPath(path);
                var file = new S3FileInfo(
                    s3Client,
                    setting.AwsS3BucketName,
                    path);
                return file;
            }
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

        //private string CleanFolderPath(string path)
        //{
        //    path = CleanPath(path);
        //    path = Regex.Replace(path, @"[\\]$", "") + "\\";
        //    if (path == "\\")
        //        return "";
        //    return path;
        //}

        public static string PathToKey(string path) {
            var result = Regex.Replace(path, @"^[^:]+\:", "");
            result = result.Replace("\\", "/");
            result = Regex.Replace(result, @"^[\/]+", "");
            return result;
        }

        public IEnumerable<IStorageFile> ListFiles(string path) =>
            GetDirectory(CleanPath(path))
            .GetFiles()
            .Where(x => !x.Name.EndsWith("_$folder$"))
            .Select(x => new AmazonS3StorageFile(x, this)).ToList();

        public bool FolderExists(string path) => GetDirectory(path).Exists;

        public IEnumerable<IStorageFolder> ListFolders(string path) {
            var dir = GetDirectory(path);
            return dir.GetDirectories("*", SearchOption.TopDirectoryOnly)
                .Select(x => new AmazonS3StorageFolder(x)).ToList();
        }

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
            using (var s3Client = GetS3Client()) {
                var setting = GetAwsS3Setting();
                return new S3DirectoryInfo(
                    s3Client,
                    setting.AwsS3BucketName,
                    CleanPath(path)
                );
            }
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
            using (var s3Client = GetS3Client()) {
                var setting = GetAwsS3Setting();
                s3Client.PutACL(new PutACLRequest {
                    BucketName = setting.AwsS3BucketName,
                    Key = key,
                    CannedACL = S3CannedACL.PublicRead
                });
            }
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

        public string Combine(string path1, string path2) =>
            CleanPath(PathUtils.Combine(path1, path2));

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
                    using (var s3Client = GetS3Client()) {
                        var response = s3Client.ListObjects(request);
                        result.AddRange(response.S3Objects.Where(x => filterfFunc(x)));

                        if (response.IsTruncated && response.S3Objects.Any()) {
                            request.Marker = response.NextMarker;
                        }
                        else {
                            break;
                        }
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
            using (var client = GetS3Client()) {
                var s3directory = new S3DirectoryInfo(client, setting.AwsS3BucketName);
                if (!s3directory.Exists) {
                    // create a new buck if not exist
                    // Construct request
                    var request = new PutBucketRequest {
                        BucketName = setting.AwsS3BucketName,
                        BucketRegion = S3Region.APS1,      // set region to asia pacific south east => Singapore 
                        CannedACL = S3CannedACL.Private // make bucket publicly readable
                    };
                    // Issue call
                    client.PutBucket(request);
                }
            }
        }


        //private Stream Download(string key) {
        //    var stream = service.TransferUtility.OpenStream(new TransferUtilityOpenStreamRequest() {
        //        BucketName = "",
        //        Key = key,
        //    });
        //    return stream;
        //}

        //    private bool Upload(Stream stream, string fileKey, bool asPublic = false, bool closeStream = false) {
        //        try {
        //            var request = new TransferUtilityUploadRequest() {
        //                BucketName = "",
        //                Key = fileKey,
        //                InputStream = stream,
        //                AutoCloseStream = closeStream,
        //                AutoResetStreamPosition = true,
        //            };
        //            if (asPublic) {
        //                request.CannedACL = S3CannedACL.PublicRead;
        //            }

        //            service.TransferUtility.Upload(request);
        //            if (stream.CanSeek) {
        //                stream.Seek(0, SeekOrigin.Begin);
        //            }
        //        }
        //        catch {
        //            return false;
        //        }
        //        return true;
        //    }
        //}
    }
}
