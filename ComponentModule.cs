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
                var siteService = c.Resolve<ISiteService>();
                var siteSetting = siteService.GetSiteSettings();
                var awsS3SettingPart = siteSetting.As<AwsS3SettingPart>();

                if (awsS3SettingPart.UseLocalStackS3) {
                    var credentials = new BasicAWSCredentials("", "");
                    var config = new AmazonS3Config {
                        ServiceURL = awsS3SettingPart.LocalStackS3ServiceUrl,
                        UseHttp = true,
                        ForcePathStyle = true,
                    };
                    return new AmazonS3Client(credentials, config);
                }
                else {
                    var commonSetting = siteSetting.As<CommonSettingPart>();
                    var credentials = new BasicAWSCredentials(
                        commonSetting.AwsAccessKey,
                        commonSetting.AwsSecretKey
                    );

                    var config = new AmazonS3Config {
                        ServiceURL = awsS3SettingPart.AwsS3ServiceUrl,
                        UseHttp = false,
                    };
                    return new AmazonS3Client(credentials, config);
                }

            }).As<IAmazonS3>().InstancePerDependency();
        }
    }
}
