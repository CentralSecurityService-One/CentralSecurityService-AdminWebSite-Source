using CentralSecurityService.Common.Configuration;
using CentralSecurityServiceAdmin.Configuration;
using CentralSecurityServiceAdmin.PagesAdditional;
using CentralSecurityServiceAdmin.Sessions;
using Eadent.Common.WebApi.ApiClient;
using Eadent.Common.WebApi.DataTransferObjects.Google;
using Eadent.Common.WebApi.Helpers;
using Eadent.Identity.Access;
using Eadent.Identity.DataAccess.EadentUserIdentity.Entities;
using Eadent.Identity.Definitions;
using Microsoft.AspNetCore.Mvc;

namespace CentralSecurityServiceAdmin.Pages.Special
{
    public class SetupModel : BasePageModel
    {
        private ILogger<SetupModel> Logger { get; }

        public string Message { get; set; }

        [BindProperty]
        public string ConfirmCurrentDate { get; set; }

        public SetupModel(ILogger<SetupModel> logger, IConfiguration configuration, IUserSession userSession, IEadentUserIdentity eadentUserIdentity)
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
                        bool userExists = EadentUserIdentity.AdminDoesUserExist(CentralSecurityServiceAdminSensitiveSettings.Instance.AdminAccount.AdminEMailAddress);

                        if (userExists)
                        {
                            UserEntity userEntity = EadentUserIdentity.AdminForceUserPasswordChange(
                                CentralSecurityServiceAdminSensitiveSettings.Instance.AdminAccount.AdminEMailAddress,
                                Guid.Parse(CentralSecurityServiceAdminSensitiveSettings.Instance.AdminAccount.AdminUserGuid),
                                CentralSecurityServiceAdminSensitiveSettings.Instance.AdminAccount.AdminPassword,
                                HttpHelper.GetRemoteIpAddress(Request), googleReCaptchaScore);

                            if (userEntity != null)
                            {
                                actionResult = Redirect("/SignIn");
                            }
                            else
                            {
                                Message = "Failed to create the Global Administrator account. Please try again later.";
                            }
                        }
                        else
                        {
                            int createdByApplicationId = 0;
                            string userGuidString = null;
                            string eMailAddress = CentralSecurityServiceAdminSensitiveSettings.Instance.AdminAccount.AdminEMailAddress;
                            string password = CentralSecurityServiceAdminSensitiveSettings.Instance.AdminAccount.AdminPassword;
                            string mobilePhoneNumber = CentralSecurityServiceAdminSensitiveSettings.Instance.AdminAccount.AdminMobilePhoneNumber;

                            if (string.IsNullOrWhiteSpace(mobilePhoneNumber))
                            {
                                mobilePhoneNumber = null;
                            }

                            (RegisterUserStatus registerUserStatusId, UserEntity userEntity) = EadentUserIdentity.RegisterUser(createdByApplicationId, userGuidString, Role.GlobalAdministrator,
                                eMailAddress, eMailAddress, mobilePhoneNumber, password, HttpHelper.GetRemoteIpAddress(Request), googleReCaptchaScore);

                            if (registerUserStatusId == RegisterUserStatus.Success)
                            {
                                actionResult = Redirect("/SignIn");
                            }
                            else
                            {
                                Message = $"RegisterUserStatusId = {registerUserStatusId}";
                            }
                        }
                    }
                }
            }

            return actionResult;
        }
    }
}
