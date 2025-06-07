using CentralSecurityService.Common.Configuration;
using CentralSecurityServiceAdmin.Sessions;
using Eadent.Common.WebApi.ApiClient;
using Eadent.Common.WebApi.DataTransferObjects.Google;
using Eadent.Common.WebApi.Helpers;
using Eadent.Identity.Access;
using Eadent.Identity.DataAccess.EadentUserIdentity.Entities;
using Eadent.Identity.Definitions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Serilog.Context;

namespace CentralSecurityServiceAdmin.Pages
{
    public class SignInModel : PageModel
    {
        private ILogger<SignInModel> Logger { get; }

        protected IEadentUserIdentity EadentUserIdentity { get; }

        public IUserSession UserSession { get; }
        public string Message { get; set; }

        [BindProperty]
        public string EMailAddress { get; set; }

        [BindProperty]
        public string Password { get; set; }

        public string GoogleReCaptchaSiteKey => CentralSecurityServiceCommonSettings.Instance.GoogleReCaptcha.SiteKey;

        public decimal GoogleReCaptchaScore { get; set; }

        [BindProperty]
        public string GoogleReCaptchaValue { get; set; }

        public SignInModel(ILogger<SignInModel> logger, IEadentUserIdentity eadentUserIdentity, IUserSession userSession)
        {
            Logger = logger;
            EadentUserIdentity = eadentUserIdentity;
            UserSession = userSession;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            LogContext.PushProperty("SessionGuid", Guid.NewGuid().ToString());

            Logger.LogInformation("SignIn page accessed at {DateTimeUtc}", DateTime.UtcNow);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string action)
        {
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
                    (SignInStatus signInStatusId, UserSessionEntity userSessionEntity, DateTime? previousUserSignInDateTimeUtc) = EadentUserIdentity.SignInUser(SignInType.WebSite, EMailAddress, Password, HttpHelper.GetRemoteIpAddress(Request), googleReCaptchaScore);

                    if (signInStatusId == SignInStatus.Success)
                    {
                        UserSession.SignIn(userSessionEntity);

                        actionResult = Redirect("CheckAndUpdateUserSession");
                    }
                    else
                    {
                        Message = $"SignInStatusId = {signInStatusId} : PreviousUserSignInDateTimeUtc = {previousUserSignInDateTimeUtc}";
                    }
                }
            }

            return actionResult;
        }

        protected async Task<(bool success, decimal googleReCaptchaScore)> GoogleReCaptchaAsync()
        {
            var verifyRequestDto = new ReCaptchaVerifyRequestDto()
            {
                secret = CentralSecurityServiceCommonSettings.Instance.GoogleReCaptcha.Secret,
                response = GoogleReCaptchaValue,
                remoteip = HttpHelper.GetLocalIpAddress(Request)
            };

            bool success = false;

            decimal googleReCaptchaScore = -1M;

            IApiClientResponse<ReCaptchaVerifyResponseDto> response = null;

            using (var apiClient = new ApiClientUrlEncoded(Logger, "https://www.google.com/"))
            {
                response = await apiClient.PostAsync<ReCaptchaVerifyRequestDto, ReCaptchaVerifyResponseDto>("/recaptcha/api/siteverify", verifyRequestDto, null);
            }

            if (response.ResponseDto != null)
            {
                googleReCaptchaScore = response.ResponseDto.score;

                success = true;
            }

            return (success, googleReCaptchaScore);
        }
    }
}
