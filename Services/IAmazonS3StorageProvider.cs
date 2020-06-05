using Orchard.FileSystems.Media;

namespace Codesanook.AmazonS3.Services {
    public interface IAmazonS3StorageProvider : IStorageProvider
    {
        void PublishFile(string path);
        void CreateBucketIfNotExist(); 
    }
}
