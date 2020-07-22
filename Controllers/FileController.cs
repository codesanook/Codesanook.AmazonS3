using Amazon.S3;
using Amazon.S3.Model;
using Codesanook.AmazonS3.Models;
using Orchard;
using Orchard.ContentManagement;
using Orchard.Localization;
using Orchard.Logging;
using Orchard.Security;
using Orchard.Settings;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Codesanook.AmazonS3.Controllers {
    public class FileController : Controller {
        // Property injection
        public ILogger Logger { get; set; }
        public Localizer T { get; set; }

        private readonly IOrchardServices orchardService;
        private readonly IAmazonS3 s3Client;
        private readonly ISiteService siteService;
        private readonly IAuthenticationService authenticationService;

        public FileController(
            IOrchardServices orchardService,
            IAmazonS3 s3Client,
            ISiteService siteService,
            IAuthenticationService authenticationService
        ) {
            this.orchardService = orchardService;
            this.s3Client = s3Client;
            this.siteService = siteService;
            this.authenticationService = authenticationService;
            Logger = NullLogger.Instance;
            T = NullLocalizer.Instance;
        }

        public async Task<object> GetFile(string fileKey) {
            var loggedInUser = authenticationService.GetAuthenticatedUser();
            if (loggedInUser == null) {
                return new HttpNotFoundResult($"No file key {fileKey}");
            }

            var s3Setting = siteService.GetSiteSettings().As<AwsS3SettingPart>();
            var request = new GetObjectRequest() {
                BucketName = s3Setting.AwsS3BucketName,
                Key = fileKey,
            };
            var response = await s3Client.GetObjectAsync(request);
            return new FileStreamResult(response.ResponseStream, response.Headers.ContentType);
        }
    }
}