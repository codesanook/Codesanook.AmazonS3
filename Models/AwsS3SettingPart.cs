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

        // https://stackoverflow.com/a/18766082/1872200
        public bool MapSubdomainToBucketName {
            get => this.Retrieve(x => x.MapSubdomainToBucketName);
            set => this.Store(x => x.MapSubdomainToBucketName, value);
        }
    }
}
