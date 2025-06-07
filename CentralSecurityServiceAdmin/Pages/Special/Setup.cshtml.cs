using CentralSecurityService.Common.Configuration;
using CentralSecurityServiceAdmin.Configuration;
using CentralSecurityServiceAdmin.Sessions;
using Eadent.Common.WebApi.ApiClient;
using Eadent.Common.WebApi.DataTransferObjects.Google;
using Eadent.Common.WebApi.Helpers;
using Eadent.Identity.Access;
using Eadent.Identity.DataAccess.EadentUserIdentity.Entities;
using Eadent.Identity.Definitions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Mail;

namespace CentralSecurityServiceAdmin.Pages.Special
{
    public class SetupModel : PageModel
    {
        private ILogger<SetupModel> Logger { get; }

        protected IEadentUserIdentity EadentUserIdentity { get; }

        public string Message { get; set; }

        [BindProperty]
        public string ConfirmCurrentDate { get; set; }

        public string GoogleReCaptchaSiteKey => CentralSecurityServiceCommonSettings.Instance.GoogleReCaptcha.SiteKey;

        public decimal GoogleReCaptchaScore { get; set; }

        [BindProperty]
        public string GoogleReCaptchaValue { get; set; }

        public SetupModel(ILogger<SetupModel> logger, IEadentUserIdentity eadentUserIdentity)
        {
            Logger = logger;
            EadentUserIdentity = eadentUserIdentity;
        }

        public async Task<IActionResult> OnGetAsayc()
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
