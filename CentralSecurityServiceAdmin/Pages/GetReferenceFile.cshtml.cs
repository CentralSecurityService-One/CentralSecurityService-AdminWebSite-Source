using CentralSecurityServiceAdmin.Configuration;
using CentralSecurityServiceAdmin.PagesAdditional;
using CentralSecurityServiceAdmin.Sessions;
using Eadent.Identity.Access;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace CentralSecurityServiceAdmin.Pages
{
    public class GetReferenceFileModel : BasePageModel
    {
        private IWebHostEnvironment WebHostEnvironment { get; }

        public GetReferenceFileModel(ILogger<GetReferenceFileModel> logger, IWebHostEnvironment webHostEnvironment, IConfiguration configuration, IUserSession userSession, IEadentUserIdentity eadentUserIdentity) : base(logger, configuration, userSession, eadentUserIdentity)
        {
            WebHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> OnGetAsync(string referenceFile)
        {
            IActionResult actionResult = EnsureUserIsSignedIn();

            if (actionResult != null)
            {
                return actionResult; // Redirect to SignIn if user is not Signed In.
            }

            if (string.IsNullOrEmpty(referenceFile))
                return BadRequest("Reference File name cannot be null or empty.");

            string referenceFilesFolder = null;

            if (WebHostEnvironment.IsDevelopment())
            {
                referenceFilesFolder = CentralSecurityServiceAdminSettings.Instance.References.DevelopmentReferenceFilesFolder;
            }
            else
            {
                referenceFilesFolder = CentralSecurityServiceAdminSettings.Instance.References.ProductionReferenceFilesFolder;
            }

            var filePathAndName = Path.Combine(referenceFilesFolder, referenceFile);

            if (!System.IO.File.Exists(filePathAndName))
                return NotFound();

            var provider = new FileExtensionContentTypeProvider();

            if (!provider.TryGetContentType(filePathAndName, out string contentType))
            {
                contentType = "application/octet-stream"; // Default fallback.
            }

            var file = System.IO.File.OpenRead(filePathAndName);

            return File(file, contentType); // ASP.NET Core [allegedly] disposes stream after response.
        }
    }
}
