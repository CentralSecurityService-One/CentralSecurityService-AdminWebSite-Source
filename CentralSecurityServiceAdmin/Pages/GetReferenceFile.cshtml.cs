using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.StaticFiles;

namespace CentralSecurityServiceAdmin.Pages
{
    public class GetReferenceFileModel : PageModel
    {
        public async Task<IActionResult> OnGetAsync(string referenceFile)
        {
            if (string.IsNullOrEmpty(referenceFile))
                return BadRequest("Reference File name cannot be null or empty.");

            // TODO: Make path configurable or use a safer method to construct paths.
            var filePathAndName = Path.Combine("/CentralSecurityService/ReferenceFiles", referenceFile);

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
