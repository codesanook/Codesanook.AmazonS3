using Amazon.Runtime;
using Amazon.S3;
using Codesanook.AmazonS3.Models;
using Codesanook.Configuration.Models;
using Orchard.ContentManagement;
using Orchard.Settings;

namespace Codesanook.AmazonS3.Services {
    public class AmazonS3Service : IAmazonS3Service {
        private readonly ISiteService siteService;

        public AmazonS3Service(
            ISiteService siteService
        ) {
            this.siteService = siteService;
        }


        //public ITransferUtility TransferUtility {
        //    get {
        //        if (transferUtility != null) {
        //            return transferUtility;
        //        }

        //        var config = new TransferUtilityConfig();
        //        transferUtility = new TransferUtility(S3Clicent, config);
        //        return transferUtility;
        //    }
        //}
    }
}
