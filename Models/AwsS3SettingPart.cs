using Orchard.ContentManagement;

namespace Codesanook.AmazonS3.Models {
    public class AwsS3SettingPart : ContentPart {

        public bool UseLocalStackS3 {
            get => this.Retrieve(x => x.UseLocalStackS3);
            set => this.Store(x => x.UseLocalStackS3, value);
        }

        public string LocalStackS3ServiceUrl {
            get => this.Retrieve(x => x.LocalStackS3ServiceUrl);
            set => this.Store(x => x.LocalStackS3ServiceUrl, value);
        }

        public string AwsS3ServiceUrl {
            get => this.Retrieve(x => x.AwsS3ServiceUrl);
            set => this.Store(x => x.AwsS3ServiceUrl, value);
        }

        public string AwsS3BucketName {
            get => this.Retrieve(x => x.AwsS3BucketName);
            set => this.Store(x => x.AwsS3BucketName, value);
        }

        public string AwsS3PublicUrl {
            get => this.Retrieve(x => x.AwsS3PublicUrl);
            set => this.Store(x => x.AwsS3PublicUrl, value);
        }
    }
}
