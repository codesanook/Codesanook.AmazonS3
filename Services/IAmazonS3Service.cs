using Amazon.S3;
using Orchard;

namespace Codesanook.AmazonS3.Services {
    public interface IAmazonS3Service : IDependency {
        IAmazonS3 GetS3Client();
    }
}
