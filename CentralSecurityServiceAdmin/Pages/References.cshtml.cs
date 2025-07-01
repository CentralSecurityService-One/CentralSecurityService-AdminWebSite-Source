using CentralSecurityService.Common.DataAccess.CentralSecurityService.Entities;
using CentralSecurityService.Common.DataAccess.CentralSecurityService.Repositories;
using CentralSecurityServiceAdmin.PagesAdditional;
using CentralSecurityServiceAdmin.Sessions;
using Eadent.Identity.Access;
using Microsoft.AspNetCore.Mvc;

namespace CentralSecurityServiceAdmin.Pages
{
    public class ReferencesModel : BasePageModel
    {
        private ILogger<ReferencesModel> Logger { get; }

        private IReferencesRepository ReferencesRepository { get; set; }

        public List<ReferenceEntity> References { get; set; }

        public ReferencesModel(ILogger<ReferencesModel> logger, IConfiguration configuration, IUserSession userSession, IEadentUserIdentity eadentUserIdentity, IReferencesRepository referencesRepository)
            : base(logger, configuration, userSession, eadentUserIdentity)
        {
            Logger = logger;
            ReferencesRepository = referencesRepository;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            Logger.LogInformation("References page accessed.");

            IActionResult actionResult = EnsureUserIsSignedIn();

            if (actionResult != null)
            {
                return actionResult; // Redirect to SignIn if user is not Signed In.
            }

            References = ReferencesRepository.GetAll().ToList();

            return Page();
        }
    }
}
