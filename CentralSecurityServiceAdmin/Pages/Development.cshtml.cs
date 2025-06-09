using CentralSecurityServiceAdmin.PagesAdditional;
using CentralSecurityServiceAdmin.Sessions;
using Eadent.Identity.Access;
using Eadent.Identity.Definitions;
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

            (bool hasRole, IUserSession.IRole role) = UserSession.HasRole(Role.GlobalAdministrator);

            if (!hasRole)
            {
                return LocalRedirect("/"); // User does not have the required Role.
            }

#if DEBUG
            // TODOL Consider returning LocalRedirect("/"); instead of Page() in RELEASE/!DEBUG mode.
            Message = "Development page is only available in DEBUG mode. If you see this message, it means you are in DEBUG mode.";
#endif

            return Page();
        }
    }
}
