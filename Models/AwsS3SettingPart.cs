using Newtonsoft.Json;
using Orchard.ContentManagement;

namespace Codesanook.AmazonS3.Models {
    public class AwsS3SettingPart : ContentPart {

        public bool UseLocalS3rver {
            get => this.Retrieve(x => x.UseLocalS3rver);
            set => this.Store(x => x.UseLocalS3rver, value);
        }

        public string LocalS3rverServiceUrl {
            get => this.Retrieve(x => x.LocalS3rverServiceUrl);
            set => this.Store(x => x.LocalS3rverServiceUrl, value);
        }

        public string AwsS3ServiceUrl {
            get => this.Retrieve(x => x.AwsS3ServiceUrl);
            set => this.Store(x => x.AwsS3ServiceUrl, value);
        }

        public string AwsS3BucketName {
            get => this.Retrieve(x => x.AwsS3BucketName);
            set => this.Store(x => x.AwsS3BucketName, value);
        }

        [JsonProperty]
        public string AwsS3PublicUrl {
            get => this.Retrieve(x => x.AwsS3PublicUrl);
            set => this.Store(x => x.AwsS3PublicUrl, value);
        }
    }
}
