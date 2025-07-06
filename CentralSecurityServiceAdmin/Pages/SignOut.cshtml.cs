using CentralSecurityService.Common.DataAccess.CentralSecurityService.Repositories;
using CentralSecurityServiceAdmin.PagesAdditional;
using CentralSecurityServiceAdmin.Sessions;
using Eadent.Common.WebApi.Helpers;
using Eadent.Identity.Access;
using Eadent.Identity.Definitions;
using Microsoft.AspNetCore.Mvc;

namespace CentralSecurityServiceAdmin.Pages
{
    public class SignOutModel : BasePageModel
    {
        private ILogger<SignOutModel> Logger { get; }

        public string Message { get; set; }

        public SignOutModel(ILogger<SignOutModel> logger, IConfiguration configuration, IUserSession userSession, IEadentUserIdentity eadentUserIdentity)
            : base(logger, configuration, userSession, eadentUserIdentity)
        {
            Logger = logger;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!UserSession.IsSignedIn)
            {
                Logger.LogInformation("User is not Signed In. Redirecting to SignIn page at {DateTimeUtc}.", DateTime.UtcNow);

                return LocalRedirect("/SignIn");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string action)
        {
            if (!UserSession.IsSignedIn)
            {
                Logger.LogInformation("User is not Signed In. Redirecting to SignIn page at {DateTimeUtc}.", DateTime.UtcNow);

                return LocalRedirect("/SignIn");
            }

            IActionResult actionResult = Page();

            if (action == "Sign Out")
            {
                SignOutStatus signOutStatusId = await EadentUserIdentity.SignOutUserAsync(UserSession.SessionToken, HttpHelper.GetRemoteIpAddress(Request), HttpContext.RequestAborted);

                if (signOutStatusId != SignOutStatus.Error)
                {
                    UserSession.SignOut();

                    actionResult = LocalRedirect("/SignIn");
                }
                else
                {
                    Message = "An error occurred while Signing Out. Please try again later.";

                    Logger.LogError("SignOut failed for User Session Token {UserSessionToken} at {DateTimeUtc}. SignOutStatusId: {SignOutStatusId}.", UserSession.SessionToken, DateTime.UtcNow, signOutStatusId);
                }
            }
            else
            {
                Message = "You must click the 'Sign Out' button to sign out.";
            }

            return actionResult;
        }
    }
}
