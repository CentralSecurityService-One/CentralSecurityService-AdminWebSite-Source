using CentralSecurityService.Common.Configuration;
using CentralSecurityServiceAdmin.Sessions;
using Eadent.Identity.Access;
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
    }
}
