using CentralSecurityService.Common.Configuration;
using CentralSecurityService.Common.DataAccess.CentralSecurityService.Repositories;
using CentralSecurityServiceAdmin.PagesAdditional;
using CentralSecurityServiceAdmin.Sessions;
using Eadent.Common.WebApi.Helpers;
using Eadent.Identity.Access;
using Eadent.Identity.DataAccess.EadentUserIdentity.Entities;
using Eadent.Identity.Definitions;
using Microsoft.AspNetCore.Mvc;

namespace CentralSecurityServiceAdmin.Pages
{
    public class SignInModel : BasePageModel
    {
        private ILogger<SignInModel> Logger { get; }

        public string Message { get; set; }

        [BindProperty]
        public string EMailAddress { get; set; }

        [BindProperty]
        public string Password { get; set; }

        [BindProperty]
        public string ReturnUrl { get; set; }

        public SignInModel(ILogger<SignInModel> logger, IConfiguration configuration, IUserSession userSession, IEadentUserIdentity eadentUserIdentity)
            : base(logger, configuration, userSession, eadentUserIdentity)
        {
            Logger = logger;
        }

        public async Task<IActionResult> OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");

            if (UserSession.IsSignedIn)
            {
                Logger.LogInformation("User is already Signed In. Redirecting to ReturnUrl: {ReturnUrl} at {DateTimeUtc}.", ReturnUrl, DateTime.UtcNow);

                return LocalRedirect(ReturnUrl);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string action)
        {
            if (UserSession.IsSignedIn)
            {
                Logger.LogInformation("User is already Signed In. Redirecting to ReturnUrl: {ReturnUrl} at {DateTimeUtc}.", ReturnUrl, DateTime.UtcNow);

                return LocalRedirect(ReturnUrl);
            }

            IActionResult actionResult = Page();

            (bool success, decimal googleReCaptchaScore) = await GoogleReCaptchaAsync();

            GoogleReCaptchaScore = googleReCaptchaScore;

            if (googleReCaptchaScore < CentralSecurityServiceCommonSettings.Instance.GoogleReCaptcha.MinimumScore)
            {
                Message = "You are unable to Sign In because of a poor Google ReCaptcha Score.";
            }
            else
            {
                if (action == "Cancel")
                {
                    Message = "You chose to Cancel.";

                    EMailAddress = string.Empty;
                    Password = null;
                }
                else if (action == "Sign In")
                {
                    (SignInStatus signInStatusId, UserSessionEntity userSessionEntity, DateTime? previousUserSignInDateTimeUtc) = await EadentUserIdentity.SignInUserAsync(SignInType.WebSite, EMailAddress, Password, HttpHelper.GetRemoteIpAddress(Request), googleReCaptchaScore, HttpContext.RequestAborted);

                    if (signInStatusId == SignInStatus.Success)
                    {
                        UserSession.SignIn(userSessionEntity);

                        actionResult = LocalRedirect(ReturnUrl ?? Url.Content("~/"));
                    }
                    else
                    {
                        Message = $"SignInStatusId = {signInStatusId} : PreviousUserSignInDateTimeUtc = {previousUserSignInDateTimeUtc}";
                    }
                }
            }

            return actionResult;
        }
    }
}
