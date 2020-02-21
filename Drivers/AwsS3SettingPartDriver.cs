using Codesanook.AmazonS3.Models;
using Codesanook.AmazonS3.Services;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Drivers;
using Orchard.Localization;
using Orchard.UI.Notify;

namespace Codesanook.AmazonS3 {
    public class AwsS3SettingPartDriver : ContentPartDriver<AwsS3SettingPart> {
        private readonly IAmazonS3StorageProvider amazonS3StorageProvider;
        private readonly INotifier notifier;
        protected override string Prefix => "AwsS3Setting";
        public Localizer T { get; set; }

        public AwsS3SettingPartDriver(
            IAmazonS3StorageProvider amazonS3StorageProvider,
            INotifier notifier
        ) {
            this.amazonS3StorageProvider = amazonS3StorageProvider;
            this.notifier = notifier;
            T = NullLocalizer.Instance;
        }

        protected override DriverResult Editor(
            AwsS3SettingPart part,
            dynamic shapeHelper
        ) {

            return ContentShape("Parts_AwsS3Setting",
                () => shapeHelper.EditorTemplate(
                        TemplateName: "Parts/AwsS3Setting",
                        Model: part,
                        Prefix: Prefix
                    )
                ).OnGroup("Codesanook Module"); // Show setting in group
        }

        protected override DriverResult Editor(
            AwsS3SettingPart part,
            IUpdateModel updater,
            dynamic shapeHelper
        ) {
            updater.TryUpdateModel(part, Prefix, null, null);
            amazonS3StorageProvider.CreateBucketIfNotExist();
            return Editor(part, shapeHelper);
        }
    }
}
