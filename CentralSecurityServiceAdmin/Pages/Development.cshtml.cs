using CentralSecurityServiceAdmin.PagesAdditional;
using CentralSecurityServiceAdmin.Sessions;
using Eadent.Identity.Access;
using Microsoft.AspNetCore.Mvc;

namespace CentralSecurityServiceAdmin.Pages
{
    public class DevelopmentModel : BasePageModel
    {
        public string Message { get; set; }

        public DevelopmentModel(ILogger<DevelopmentModel> logger, IConfiguration configuration, IUserSession userSession, IEadentUserIdentity eadentUserIdentity)
            : base(logger, configuration, userSession, eadentUserIdentity)
        {
        }

        public async Task<IActionResult> OnGetAsync()
        {
            IActionResult actionResult = EnsureUserIsSignedIn();

            if (actionResult != null)
            {
                return actionResult; // Redirect to SignIn if user is not Signed In.
            }

            return Page();
        }
    }
}
