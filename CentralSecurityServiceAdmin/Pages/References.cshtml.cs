using CentralSecurityService.Common.DataAccess.CentralSecurityService.Entities;
using CentralSecurityService.Common.DataAccess.CentralSecurityService.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CentralSecurityServiceAdmin.Pages
{
    public class ReferencesModel : PageModel
    {
        private ILogger<ReferencesModel> Logger;

        private IReferencesRepository ReferencesRepository { get; set; }

        public List<ReferenceEntity> References { get; set; }

        public ReferencesModel(ILogger<ReferencesModel> logger, IReferencesRepository referencesRepository)
        {
            Logger = logger;
            ReferencesRepository = referencesRepository;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            Logger.LogInformation("References page accessed.");

            References = ReferencesRepository.GetAll().ToList(); // Example usage of the repository to fetch all references.

            return Page();
        }
    }
}
