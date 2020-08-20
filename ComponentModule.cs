using Amazon.Runtime;
using Amazon.S3;
using Autofac;
using Codesanook.AmazonS3.Models;
using Codesanook.Common.Models;
using Orchard.ContentManagement;
using Orchard.Settings;

namespace Codesanook.AmazonS3 {
    public class ComponentModule : Module {
        protected override void Load(ContainerBuilder builder) {
            // Register expressions that execute to create an object
            // https://autofaccn.readthedocs.io/en/latest/register/registration.html#lambda-expression-components
            // Delayed Instantiation
            // https://autofaccn.readthedocs.io/en/latest/resolve/relationships.html#delayed-instantiation-lazy-b
            builder.Register(c => {
                var (credentials, config) = CreateS3Config(c);
                return new AmazonS3Client(credentials, config);
            }).As<IAmazonS3>().InstancePerDependency();
        }

        private static (BasicAWSCredentials Credential, AmazonS3Config Config) CreateS3Config(IComponentContext componentContext) {
            var siteService = componentContext.Resolve<ISiteService>();
            var siteSetting = siteService.GetSiteSettings();
            var awsS3SettingPart = siteSetting.As<AwsS3SettingPart>();
            var commonSetting = siteSetting.As<CommonSettingPart>();

            // Note: For local stack, it doesn't matter what your AWS key & secret are,
            // as long as they aren't empty.
            var credentials = new BasicAWSCredentials(
                commonSetting.AwsAccessKey,
                commonSetting.AwsSecretKey
            );

            if (awsS3SettingPart.UseLocalStackS3) {
                var config = new AmazonS3Config {
                    UseHttp = true,
                    ServiceURL = awsS3SettingPart.LocalStackS3ServiceUrl,
                    ForcePathStyle = true,
                };
                // Both ServiceUrl and ForcePathStyle are important here.
                // ForcePathStyle tells the SDK to use URL of the from hostname/bucket instead of bucket.hostname
                return (credentials, config);
            }
            else {
                var config = new AmazonS3Config {
                    UseHttp = false,
                    ServiceURL = awsS3SettingPart.AwsS3ServiceUrl,
                };
                return (credentials, config);
            }
        }
    }
}
