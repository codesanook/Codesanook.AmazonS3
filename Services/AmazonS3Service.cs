using Amazon.Runtime;
using Amazon.S3;
using Codesanook.AmazonS3.Models;
using Codesanook.Common.Models;
using Orchard.ContentManagement;
using Orchard.Settings;

namespace Codesanook.AmazonS3.Services {
    public sealed class AmazonS3Service : IAmazonS3Service {

        private readonly ISiteService siteService;
        private IAmazonS3 s3Client;

        public AmazonS3Service(ISiteService siteService) => this.siteService = siteService;

        public IAmazonS3 GetS3Client() {
            if (s3Client != null) {
                return s3Client;
            }

            var awsS3Setting = siteService.GetSiteSettings().As<AwsS3SettingPart>();
            if (awsS3Setting.UseLocalStackS3) {
                var credentials = new BasicAWSCredentials("", "");
                var config = new AmazonS3Config {
                    ServiceURL = awsS3Setting.LocalStackS3ServiceUrl,
                    UseHttp = true,
                    ForcePathStyle = true,
                };
                s3Client = new AmazonS3Client(credentials, config);
            }
            else {
                var commonSetting = siteService.GetSiteSettings().As<CommonSettingPart>();
                var credentials = new BasicAWSCredentials(
                    commonSetting.AwsAccessKey,
                    commonSetting.AwsSecretKey
                );

                var config = new AmazonS3Config {
                    ServiceURL = awsS3Setting.AwsS3ServiceUrl,
                    UseHttp = false,
                };
                s3Client = new AmazonS3Client(credentials, config);
            }
            return s3Client;
        }

        public void Dispose() {
            if (s3Client != null) {
                s3Client.Dispose();
            }
        }
    }
}
