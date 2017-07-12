using Amazon.S3;
using Amazon.S3.Transfer;
using Codesanook.Configuration.Models;
using Orchard;

namespace Codesanook.AmazonS3.Services
{
    public interface IAmazonS3Service:IDependency
    {
        ModuleSettingPart Setting { get; }
        IAmazonS3 S3Clicent { get; }
        ITransferUtility TransferUtility { get; }
    }
}