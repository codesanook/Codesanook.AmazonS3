using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.UI.WebControls;
using Amazon.S3;
using Amazon.S3.IO;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Orchard.Environment.Extensions;
using Orchard.FileSystems.Media;
using PathUtils = System.IO.Path;
using System.Text.RegularExpressions;
using UrlHelper = Flurl.Url;

namespace Codesanook.AmazonS3.Services
{
    [OrchardSuppressDependency("Orchard.FileSystems.Media.FileSystemStorageProvider")]
    public class AmazonS3StorageProvider : IAmazonS3StorageProvider, IStorageProvider
    {
        private IAmazonS3Service service;

        public AmazonS3StorageProvider(IAmazonS3Service service)
        {
            this.service = service;
        }

        public bool FileExists(string path)
        {
            var s3File = GetS3File(path);
            return s3File.Exists;
        }

        public string GetPublicUrl(string path)
        {
            path = CleanPath(path);
            return UrlHelper.Combine(
                service.Setting.AwsS3PublicUrl,
                service.Setting.AwsS3BucketName,
                path.Replace("\\", "/"));
        }

        public string GetStoragePath(string url)
        {
            var rootPath = UrlHelper.Combine(
                service.Setting.AwsS3PublicUrl,
                service.Setting.AwsS3BucketName);
            if (string.IsNullOrWhiteSpace(url) || url.Length < rootPath.Length)
            {
                return rootPath;
            }

            return url.Substring(rootPath.Length);
        }

        public IStorageFile GetFile(string path)
        {
            var s3File = GetS3File(path);
            return new AmazonS3StorageFile(s3File, this);
        }

        private S3FileInfo GetS3File(string path)
        {
            path = CleanPath(path);
            var file = new S3FileInfo(
                service.S3Clicent,
                service.Setting.AwsS3BucketName,
                path);
            return file;
        }

        public static string CleanPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;
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

        public static string PathToKey(string path)
        {
            var result = Regex.Replace(path, @"^[^:]+\:", "");
            result = result.Replace("\\", "/");
            result = Regex.Replace(result, @"^[\/]+", "");
            return result;
        }

        public IEnumerable<IStorageFile> ListFiles(string path)
        {
            var dir = GetDirectory(path);
            return dir.GetFiles().Where(x => !x.Name.EndsWith("_$folder$"))
                .Select(x => new AmazonS3StorageFile(x, this)).ToList();
        }
        private string GetFolderKey(string path)
        {
            path = CleanPath(path);
            path = Regex.Replace(path, "/$", "");
            var folderName = PathUtils.GetFileName(path);
            return path + "/" + folderName + "_$folder$";
        }

        public bool FolderExists(string path)
        {
            var dir = GetDirectory(path);
            return dir.Exists;
        }

        public IEnumerable<IStorageFolder> ListFolders(string path)
        {
            var dir = GetDirectory(path);
            return dir.GetDirectories("*", SearchOption.TopDirectoryOnly)
                .Select(x => new AmazonS3StorageFolder(x)).ToList();
        }

        public bool TryCreateFolder(string path)
        {
            try
            {
                var dir = GetDirectory(path);
                dir.Create();
            }
            catch
            {
                return false;
            }
            return true;
        }

        public void CreateFolder(string path)
        {
            var dir = GetDirectory(path);
            dir.Create();
        }

        public void DeleteFolder(string path)
        {
            var dir = GetDirectory(path);
            dir.Delete();
        }

        private S3DirectoryInfo GetDirectory(string path)
        {
            path = CleanPath(path);
            var dir = new S3DirectoryInfo(
                service.S3Clicent,
                service.Setting.AwsS3BucketName,
                path);
            return dir;
        }

        public void RenameFolder(string oldPath, string newPath)
        {
            oldPath = CleanPath(oldPath);
            newPath = CleanPath(newPath);
            var oldDir = GetDirectory(oldPath);
            var newDir = GetDirectory(newPath);
            oldDir.MoveToLocal(newPath);
        }

        public void DeleteFile(string path)
        {
            var file = GetS3File(path);
            file.Delete();
        }

        public void RenameFile(string oldPath, string newPath)
        {
            var file = GetS3File(oldPath);

            newPath = CleanPath(newPath);
            file.MoveToLocal(newPath);
        }

        public void CopyFile(string originalPath, string duplicatePath)
        {
            var file = GetS3File(originalPath);

            duplicatePath = CleanPath(duplicatePath);
            file.CopyToLocal(duplicatePath);
        }

        public void PublishFile(string path)
        {
            var key = PathToKey(path);
            Console.WriteLine("Publish key:" + key);
            service.S3Clicent.PutACL(new PutACLRequest
            {
                BucketName = service.Setting.AwsS3BucketName,
                Key = key,
                CannedACL = S3CannedACL.PublicRead
            });
        }

        public IStorageFile CreateFile(string path)
        {
            var file = GetS3File(path);
            using (file.Create()) { }
            PublishFile(path);
            return new AmazonS3StorageFile(file, this);
        }

        public bool TrySaveStream(string path, Stream inputStream)
        {
            try
            {
                path = CleanPath(path);
                SaveStream(path, inputStream);
            }
            catch
            {
                return false;
            }
            return true;
        }

        public void SaveStream(string path, Stream inputStream)
        {
            path = CleanPath(path);
            var file = GetS3File(path);
            var isNew = !file.Exists;
            using (var stream = file.Exists ? file.OpenWrite() : file.Create())
            {
                inputStream.CopyTo(stream);
            }
            if (isNew)
            {
                PublishFile(path);
            }
        }

        public string Combine(string path1, string path2)
        {
            return CleanPath(Path.Combine(path1, path2));
        }

        public List<S3Object> ListObjects(string prefix, Func<S3Object, bool> filterfFunc = null)
        {
            try
            {
                if (filterfFunc == null)
                {
                    filterfFunc = (obj) => { return true; };
                }
                prefix = CleanPath(prefix);

                List<S3Object> result = new List<S3Object>();
                ListObjectsRequest request = new ListObjectsRequest
                {
                    BucketName = service.Setting.AwsS3BucketName,
                    Prefix = prefix,
                    MaxKeys = 1000
                };

                do
                {
                    ListObjectsResponse response = service.S3Clicent.ListObjects(request);
                    result.AddRange(response.S3Objects.Where(x => filterfFunc(x)));

                    if (response.IsTruncated && response.S3Objects.Any())
                    {
                        request.Marker = response.NextMarker;
                    }
                    else
                    {
                        break;
                    }
                } while (request != null);
                return result;
            }
            catch (AmazonS3Exception ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                    return new List<S3Object>();
                throw;
            }
        }

        public Stream GetObjectStream(string path)
        {
            var file = GetS3File(path);
            return file.OpenRead();
        }

        private Stream Download(string key)
        {
            var stream = service.TransferUtility.OpenStream(new TransferUtilityOpenStreamRequest()
            {
                BucketName = service.Setting.AwsS3BucketName,
                Key = key,
            });
            return stream;
        }

        private bool Upload(Stream stream, string fileKey, bool asPublic = false, bool closeStream = false)
        {
            try
            {
                var request = new TransferUtilityUploadRequest()
                {
                    BucketName = service.Setting.AwsS3BucketName,
                    Key = fileKey,
                    InputStream = stream,
                    AutoCloseStream = closeStream,
                    AutoResetStreamPosition = true,
                };
                if (asPublic)
                {
                    request.CannedACL = S3CannedACL.PublicRead;
                }

               service.TransferUtility.Upload(request);
                if (stream.CanSeek)
                {
                    stream.Seek(0, SeekOrigin.Begin);
                }
            }
            catch
            {
                return false;
            }
            return true;
        }
    }
}