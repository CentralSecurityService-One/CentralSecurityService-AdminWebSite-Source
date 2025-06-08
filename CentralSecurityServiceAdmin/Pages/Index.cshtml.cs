using CentralSecurityServiceAdmin.PagesAdditional;
using CentralSecurityServiceAdmin.Sessions;
using Eadent.Identity.Access;
using Microsoft.AspNetCore.Mvc;

namespace CentralSecurityServiceAdmin.Pages
{
    public class IndexModel : BasePageModel
    {
        private ILogger<IndexModel> Logger { get; }

        public IndexModel(ILogger<IndexModel> logger, IConfiguration configuration, IUserSession userSession, IEadentUserIdentity eadentUserIdentity) : base(logger, configuration, userSession, eadentUserIdentity)
        {
            Logger = logger;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            Logger.LogInformation("Index page accessed at {DateTimeUtc}", DateTime.UtcNow);

            IActionResult actionResult = EnsureUserIsSignedIn();

            if (actionResult != null)
            {
                return actionResult; // Redirect to SignIn if user is not Signed In.
            }

            return Page();
        }
    }
}
