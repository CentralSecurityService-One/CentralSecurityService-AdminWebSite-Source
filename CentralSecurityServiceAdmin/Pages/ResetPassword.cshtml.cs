using CentralSecurityService.Common.DataAccess.CentralSecurityService.Repositories;
using CentralSecurityServiceAdmin.PagesAdditional;
using CentralSecurityServiceAdmin.Sessions;
using Eadent.Identity.Access;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace CentralSecurityServiceAdmin.Pages
{
    public class ResetPasswordModel : BasePageModel
    {
        public enum ResetPasswordState
        {
            EnterEMailAddress,
            EnterResetCode,
            EnterNewPassword,
            Completed
        }

        private const string StateSessionKey = "ResetPassword.State";
        
        private ILogger<ResetPasswordModel> Logger { get; }

        public string Message { get; set; }

        public ResetPasswordState State
        {
            get
            {
                var value = HttpContext.Session.GetInt32(StateSessionKey);

                return value.HasValue
                    ? (ResetPasswordState)value.Value
                    : ResetPasswordState.EnterEMailAddress; // Default State.
            }
            set
            {
                HttpContext.Session.SetInt32(StateSessionKey, (int)value);
            }
        }

        [BindProperty]
        [EmailAddress(ErrorMessage = "Invalid E-Mail Address.")]
        [Required(ErrorMessage = "E-Mail Address is required.")]
        public string EMailAddress { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Password Reset Code is required.")]
        public string PasswordResetCode { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "New Password is required.")]
        public string NewPassword { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Confirm New Password is required.")]
        public string ConfirmNewPassword { get; set; }

        public ResetPasswordModel(ILogger<ResetPasswordModel> logger, IConfiguration configuration, IUserSession userSession, IEadentUserIdentity eadentUserIdentity)
            : base(logger, configuration, userSession, eadentUserIdentity)
        {
            Logger = logger;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (UserSession.IsSignedIn)
            {
                Logger.LogInformation("User is already Signed In. Redirecting to Index page at {DateTimeUtc}.", DateTime.UtcNow);

                return LocalRedirect(Url.Content("~/"));
            }

            State = ResetPasswordState.EnterEMailAddress;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string action)
        {
            // TODO: Validate User Input.
            // TODO: REVIEW: Consider using a more secure way to handle Password Reset Codes? May already be sufficient when Database is added (e.g. Time-Limited codes).
            // TODO: Implement E-Mail sending functionality.
            // TODO: Implement Password Reset Code validation.
            // TODO: Implement New Password setting functionality.
            // TODO: Implement proper error handling and logging.
            // TODO: REVIEW: Google ReCaptcha validation.
            if (UserSession.IsSignedIn)
            {
                Logger.LogInformation("User is already Signed In. Redirecting to Index page at {DateTimeUtc}.", DateTime.UtcNow);

                return LocalRedirect(Url.Content("~/"));
            }

            if (State == ResetPasswordState.EnterEMailAddress)
            {
                if (action == "Cancel")
                {
                    return LocalRedirect(Url.Content("~/SignIn"));
                }
                else if (action == "Use E-Mail Address")
                {
                    // TODO: Validate EMailAddress.
                    if (string.IsNullOrWhiteSpace(EMailAddress))
                    {
                        Message = "The E-Mail Address is required.";
                    }
                    else
                    {
                        Message = "An E-Mail has been sent with a Password Reset Code if the E-Mail Address is recognised.";

                        State = ResetPasswordState.EnterResetCode;
                    }
                }
            }
            else if (State == ResetPasswordState.EnterResetCode)
            {
                if (action == "Cancel")
                {
                    return LocalRedirect(Url.Content("~/SignIn"));
                }
                else if (action == "Use Reset Code")
                {
                    if (string.IsNullOrWhiteSpace(PasswordResetCode))
                    {
                        Message = "The Password Reset Code is required.";
                    }
                    else
                    {
                        if (PasswordResetCode != "123456") // TODO: Replace with actual validation logic.
                        {
                            Message = "The Password Reset Code is invalid.";
                            PasswordResetCode = string.Empty;
                            ModelState.Remove(nameof(PasswordResetCode));
                        }
                        else
                        {
                            Message = "The Password Reset Code is valid. Please enter your new Password.";
                            State = ResetPasswordState.EnterNewPassword;
                        }
                    }
                }
                else if (action == "Request New Reset Code")
                {
                    PasswordResetCode = string.Empty;
                    ModelState.Remove(nameof(PasswordResetCode));
                }
            }
            else if (State == ResetPasswordState.EnterNewPassword)
            {
                if (action == "Cancel")
                {
                    return LocalRedirect(Url.Content("~/SignIn"));
                }
                else if (action == "Set New Password")
                {
                    if (string.IsNullOrWhiteSpace(NewPassword))
                    {
                        Message = "The New Password is required.";
                    }
                    else if (string.IsNullOrWhiteSpace(ConfirmNewPassword))
                    {
                        Message = "The Confirm New Password is required.";
                    }
                    else if (NewPassword.Length < 8)
                    {
                        Message = "The New Password must be at least 8 characters long.";
                    }
                    else if (!NewPassword.Any(char.IsUpper) || !NewPassword.Any(char.IsLower) || !NewPassword.Any(char.IsDigit))
                    {
                        Message = "The New Password must contain at least one uppercase letter, one lowercase letter, and one digit.";
                    }
                    else if (NewPassword != ConfirmNewPassword)
                    {
                        Message = "The New Password and Confirm New Password do not match.";
                    }
                    else
                    {
                        Message = "Your Password has been Reset successfully. You can now Sign In with your new Password.";

                        State = ResetPasswordState.Completed;
                    }
                }
            }

            return Page();
        }
    }
}
