using Codesanook.AmazonS3.Models;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Handlers;
using Orchard.Localization;

namespace Codesanook.AmazonS3.Handlers {
    public class AwsS3SettingPartHandler : ContentHandler {
        public Localizer T { get; set; }

        public AwsS3SettingPartHandler() {
            T = NullLocalizer.Instance;

            // Attach part to content item Site
            Filters.Add(new ActivatingFilter<AwsS3SettingPart>("Site"));

        }

        protected override void GetItemMetadata(GetContentItemMetadataContext context) {
            if (context.ContentItem.ContentType != "Site") {
                return;
            }
            base.GetItemMetadata(context);
            context.Metadata.EditorGroupInfo.Add(new GroupInfo(T("AWS S3")));
        }
    }
}
