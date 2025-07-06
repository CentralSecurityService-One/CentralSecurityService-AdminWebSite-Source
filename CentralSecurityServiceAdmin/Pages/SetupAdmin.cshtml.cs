using CentralSecurityService.Common.Configuration;
using CentralSecurityServiceAdmin.Configuration;
using CentralSecurityServiceAdmin.PagesAdditional;
using CentralSecurityServiceAdmin.Sessions;
using Eadent.Common.WebApi.Helpers;
using Eadent.Identity.Access;
using Eadent.Identity.DataAccess.EadentUserIdentity.Entities;
using Eadent.Identity.Definitions;
using Microsoft.AspNetCore.Mvc;

namespace CentralSecurityServiceAdmin.Pages.Special
{
    public class SetupAdminModel : BasePageModel
    {
        private ILogger<SetupAdminModel> Logger { get; }

        public string Message { get; set; }

        [BindProperty]
        public string ConfirmCurrentDate { get; set; }

        public SetupAdminModel(ILogger<SetupAdminModel> logger, IConfiguration configuration, IUserSession userSession, IEadentUserIdentity eadentUserIdentity)
            : base(logger, configuration, userSession, eadentUserIdentity)
        {
            Logger = logger;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string action)
        {
            IActionResult actionResult = Page();

            (bool success, decimal googleReCaptchaScore) = await GoogleReCaptchaAsync();

            GoogleReCaptchaScore = googleReCaptchaScore;

            if (googleReCaptchaScore < CentralSecurityServiceCommonSettings.Instance.GoogleReCaptcha.MinimumScore)
            {
                Message = "You are unable to perform this action because of a poor Google ReCaptcha Score.";
            }
            else
            {
                if (action == "Confirm")
                {
                    if (string.IsNullOrWhiteSpace(ConfirmCurrentDate) || !DateTime.TryParse(ConfirmCurrentDate, out DateTime currentDateTime))
                    {
                        Message = "You must enter a valid Date in the Date field.";
                    }
                    else if ($"{DateTime.UtcNow.Date:yyyy/MM/dd}" != ConfirmCurrentDate)
                    {
                        Message = "The Date you entered does not match the current date. Please try again.";
                    }
                    else
                    {
                        bool userExists = await EadentUserIdentity.AdminDoesUserExistAsync(CentralSecurityServiceAdminSensitiveSettings.Instance.AdminAccount.AdminEMailAddress, HttpContext.RequestAborted);

                        if (userExists)
                        {
                            try
                            {
                                UserEntity userEntity = await EadentUserIdentity.AdminForceUserPasswordChangeAsync(
                                    CentralSecurityServiceAdminSensitiveSettings.Instance.AdminAccount.AdminEMailAddress,
                                    Guid.Parse(CentralSecurityServiceAdminSensitiveSettings.Instance.AdminAccount.AdminUserGuid),
                                    CentralSecurityServiceAdminSensitiveSettings.Instance.AdminAccount.AdminPassword,
                                    HttpHelper.GetRemoteIpAddress(Request), googleReCaptchaScore, HttpContext.RequestAborted);

                                if (userEntity != null)
                                {
                                    actionResult = Redirect("/SignIn");
                                }
                                else
                                {
                                    Message = "Failed to Update the Global Administrator account. Please try again later.";
                                }
                            }
                            catch (Exception exception)
                            {
                                Logger.LogError(exception, "An error occurred while trying to Update the Global Administrator account at {DateTimeUtc}.", DateTime.UtcNow);

                                Message = "An error occurred while trying to Update the Global Administrator account. Please try again later.";
                            }
                        }
                        else
                        {
                            try
                            {
                                int createdByApplicationId = 0;
                                string userGuidString = null;
                                string displayName = CentralSecurityServiceAdminSensitiveSettings.Instance.AdminAccount.AdminDisplayName;
                                string eMailAddress = CentralSecurityServiceAdminSensitiveSettings.Instance.AdminAccount.AdminEMailAddress;
                                string password = CentralSecurityServiceAdminSensitiveSettings.Instance.AdminAccount.AdminPassword;
                                string mobilePhoneNumber = CentralSecurityServiceAdminSensitiveSettings.Instance.AdminAccount.AdminMobilePhoneNumber;

                                if (string.IsNullOrWhiteSpace(mobilePhoneNumber))
                                {
                                    mobilePhoneNumber = null;
                                }

                                (RegisterUserStatus registerUserStatusId, UserEntity userEntity) = EadentUserIdentity.RegisterUser(createdByApplicationId, userGuidString, Role.GlobalAdministrator,
                                    displayName, eMailAddress, mobilePhoneNumber, password, HttpHelper.GetRemoteIpAddress(Request), googleReCaptchaScore);

                                if (registerUserStatusId == RegisterUserStatus.Success)
                                {
                                    actionResult = Redirect("/SignIn");
                                }
                                else
                                {
                                    Message = $"RegisterUserStatusId = {registerUserStatusId}";
                                }
                            }
                            catch (Exception exception)
                            {
                                Logger.LogError(exception, "An error occurred while trying to Create the Global Administrator account at {DateTimeUtc}.", DateTime.UtcNow);

                                Message = "An error occurred while trying to Create the Global Administrator account. Please try again later.";
                            }
                        }
                    }
                }
            }

            return actionResult;
        }
    }
}
