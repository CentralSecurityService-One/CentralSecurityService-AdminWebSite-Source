using CentralSecurityService.Common.Configuration;
using CentralSecurityServiceAdmin.Configuration;
using CentralSecurityServiceAdmin.PagesAdditional;
using CentralSecurityServiceAdmin.Sessions;
using Eadent.Common.WebApi.Helpers;
using Eadent.Identity.Access;
using Eadent.Identity.Definitions;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Mvc;
using MimeKit;
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

        public string SuccessMessage { get; set; }

        public string ErrorMessage { get; set; }

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
                    await HandleUseEMailAddressAsync(HttpContext.RequestAborted);
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
                    await HandleUsePasswordResetCodeAsync(HttpContext.RequestAborted);
                }
                else if (action == "Request New Reset Code")
                {
                    await HandleRequestNewPasswordResetCodeAsync(HttpContext.RequestAborted);
                }
            }
            else if (State == ResetPasswordState.EnterNewPassword)
            {
                if (action == "Cancel")
                {
                    if (await GoogleReCaptchaScoreIsGoodAsync())
                    {
                        await EadentUserIdentity.RollBackUserPasswordResetAsync(EMailAddress, PasswordResetCode, HttpHelper.GetRemoteIpAddress(Request), GoogleReCaptchaScore, HttpContext.RequestAborted);

                        return LocalRedirect(Url.Content("~/SignIn"));
                    }
                }
                else if (action == "Set New Password")
                {
                    await HandleSetNewPasswordAsync(HttpContext.RequestAborted);
                }
            }

            return Page();
        }

        private async Task<bool> GoogleReCaptchaScoreIsGoodAsync()
        {
            (bool success, decimal googleReCaptchaScore) = await GoogleReCaptchaAsync();

            GoogleReCaptchaScore = googleReCaptchaScore;

            if ((!success) || (success && (googleReCaptchaScore < CentralSecurityServiceCommonSettings.Instance.GoogleReCaptcha.MinimumScore)))
            {
                ErrorMessage = "You may not proceed because of a poor Google ReCaptcha Score. Please Try Again.";

                success = false;
            }
            else
            {
                success = true;
            }

            return success;
        }

        private async Task<bool> SendEMailAsync(string displayName, string eMailAddress, string userPasswordResetCode)
        {
            bool eMailSent = false;

            var eMailSettings = CentralSecurityServiceAdminSensitiveSettings.Instance.EMail;

            var subject = "Central Security Service Administration Password Reset.";

            DateTime utcNow = DateTime.UtcNow;

            string htmlBody = $"<html>Admin.CentralSecurityService.one Password Reset.<br><br>" +
                $"Machine Name: <strong>{Environment.MachineName}</strong><br><br>" +
                $"Url: <strong>{Request.Scheme}://{Request.Host}{Request.Path}</strong><br><br>" +
                $"Date & Time (UTC): <strong>{utcNow:dddd, d-MMM-yyyy h:mm:ss tt}</strong><br><br>" +
                $"Date & Time (Local): <strong>{utcNow.ToLocalTime():dddd, d-MMM-yyyy h:mm:ss tt}</strong><br><br>";

            htmlBody += $"A Password Reset Request has been Received for your E-Mail Address.<br><br>" +
                        $"Display Name: <strong>{displayName}</strong><br><br>" +
                        $"E-Mail Address: <strong>{eMailAddress}</strong><br><br>" +
                        $"Your Password Reset Code is: <strong>{userPasswordResetCode}</strong><br><br>" +
                        $"If you did NOT Request a Password Reset, please ignore this E-Mail." +
                        $"</html>";

            var eMailMessage = new MimeMessage();
            eMailMessage.From.Add(new MailboxAddress("Central Security Service Administration", eMailSettings.FromEMailAddress));
            eMailMessage.To.Add(new MailboxAddress(displayName, eMailAddress));
            eMailMessage.Subject = subject;
            eMailMessage.Body = new TextPart("html")
            {
                Text = htmlBody
            };

            try
            {
                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(eMailSettings.HostName, eMailSettings.HostPort, true);
                await smtp.AuthenticateAsync(eMailSettings.SenderEMailAddress, eMailSettings.SenderPassword);
                await smtp.SendAsync(eMailMessage);
                await smtp.DisconnectAsync(true);

                eMailSent = true;
            }
            catch (Exception exception)
            {
                Logger.LogWarning(exception, "There was an Exception sending the Password Reset Code E-Mail for: {EMailAddress}.", EMailAddress);

                eMailSent = false;
            }

            return eMailSent;
        }

        private async Task HandleUseEMailAddressAsync(CancellationToken cancellationToken)
        {
            // TODO: Validate EMailAddress.
            if (string.IsNullOrWhiteSpace(EMailAddress))
            {
                ErrorMessage = "The E-Mail Address is required.";
            }
            else
            {
                if (await GoogleReCaptchaScoreIsGoodAsync())
                {
                    (UserPasswordResetStatus userPasswordResetStatusId, string displayName, string userPasswordResetCode) = await EadentUserIdentity.BeginUserPasswordResetAsync(EMailAddress, HttpHelper.GetRemoteIpAddress(Request), GoogleReCaptchaScore, cancellationToken);

                    bool bContinue = true;

                    if (userPasswordResetStatusId == UserPasswordResetStatus.NewRequest)
                    {
                        bContinue = await SendEMailAsync(displayName, EMailAddress, userPasswordResetCode);
                    }

                    if (bContinue)
                    {
                        SuccessMessage = "An E-Mail has been sent with a Password Reset Code if the E-Mail Address is recognised.";

                        State = ResetPasswordState.EnterResetCode;
                    }
                    else
                    {
                        ErrorMessage = "There was an error sending your Password Reset Code. Please try again later.";
                    }
                }
            }
        }

        private async Task HandleUsePasswordResetCodeAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(PasswordResetCode))
            {
                ErrorMessage = "The Password Reset Code is required.";
            }
            else
            {
                if (await GoogleReCaptchaScoreIsGoodAsync())
                {
                    UserPasswordResetStatus userPasswordResetStatusId = await EadentUserIdentity.TryUserPasswordResetCodeAsync(EMailAddress, PasswordResetCode, HttpHelper.GetRemoteIpAddress(Request), GoogleReCaptchaScore, cancellationToken);

                    if (userPasswordResetStatusId == UserPasswordResetStatus.LimitsReached)
                    {
                        ErrorMessage = "You have reached the maximum number of attempts to enter a Password Reset Code. Please try again later.";

                        EMailAddress = string.Empty;
                        ModelState.Remove(nameof(EMailAddress));
                        PasswordResetCode = string.Empty;
                        ModelState.Remove(nameof(PasswordResetCode));

                        State = ResetPasswordState.EnterEMailAddress;
                    }
                    else if (userPasswordResetStatusId == UserPasswordResetStatus.TimedOutExpired)
                    {
                        ErrorMessage = "The Password Reset has Timed Out and Expired. Please Restart the Password Request Process.";

                        EMailAddress = string.Empty;
                        ModelState.Remove(nameof(EMailAddress));
                        PasswordResetCode = string.Empty;
                        ModelState.Remove(nameof(PasswordResetCode));

                        State = ResetPasswordState.EnterEMailAddress;
                    }
                    else if (userPasswordResetStatusId == UserPasswordResetStatus.ValidResetCode)
                    {
                        SuccessMessage = "The Password Reset Code is valid. Please enter your new Password.";

                        State = ResetPasswordState.EnterNewPassword;
                    }
                    else // Default.
                    {
                        ErrorMessage = "The Password Reset Code is invalid.";
                        PasswordResetCode = string.Empty;
                        ModelState.Remove(nameof(PasswordResetCode));
                    }
                }
            }
        }

        private async Task HandleRequestNewPasswordResetCodeAsync(CancellationToken cancellationToken)
        {
            if (await GoogleReCaptchaScoreIsGoodAsync())
            {
                (UserPasswordResetStatus userPasswordResetStatusId, string displayName, string userPasswordResetCode) = await EadentUserIdentity.RequestNewUserPasswordResetCodeAsync(EMailAddress, HttpHelper.GetRemoteIpAddress(Request), GoogleReCaptchaScore, cancellationToken);

                if (userPasswordResetStatusId == UserPasswordResetStatus.LimitsReached)
                {
                    ErrorMessage = "You have reached the maximum number of attempts to Request a New Password Reset Code. Please try again later.";

                    EMailAddress = string.Empty;
                    ModelState.Remove(nameof(EMailAddress));
                    PasswordResetCode = string.Empty;
                    ModelState.Remove(nameof(PasswordResetCode));

                    State = ResetPasswordState.EnterEMailAddress;
                }
                else if (userPasswordResetStatusId == UserPasswordResetStatus.TimedOutExpired)
                {
                    ErrorMessage = "The Password Reset has Timed Out and Expired. Please Restart the Password Request Process.";

                    EMailAddress = string.Empty;
                    ModelState.Remove(nameof(EMailAddress));
                    PasswordResetCode = string.Empty;
                    ModelState.Remove(nameof(PasswordResetCode));

                    State = ResetPasswordState.EnterEMailAddress;
                }
                else if (userPasswordResetStatusId == UserPasswordResetStatus.NewRequest)
                {
                    PasswordResetCode = string.Empty;
                    ModelState.Remove(nameof(PasswordResetCode));

                    await SendEMailAsync(displayName, EMailAddress, userPasswordResetCode);
                }
            }
        }

        private async Task HandleSetNewPasswordAsync(CancellationToken cancellationToken)
        {
            if (await GoogleReCaptchaScoreIsGoodAsync())
            {
                if (string.IsNullOrWhiteSpace(NewPassword))
                {
                    ErrorMessage = "The New Password is required.";
                }
                else if (string.IsNullOrWhiteSpace(ConfirmNewPassword))
                {
                    ErrorMessage = "The Confirm New Password is required.";
                }
                else if (NewPassword.Length < 8)
                {
                    ErrorMessage = "The New Password must be at least 8 characters long.";
                }
                else if (!NewPassword.Any(char.IsUpper) || !NewPassword.Any(char.IsLower) || !NewPassword.Any(char.IsDigit))
                {
                    ErrorMessage = "The New Password must contain at least one uppercase letter, one lowercase letter, and one digit.";
                }
                else if (NewPassword != ConfirmNewPassword)
                {
                    ErrorMessage = "The New Password and Confirm New Password do not match.";
                }
                else
                {
                    UserPasswordResetStatus userPasswordResetStatusId = await EadentUserIdentity.CommitUserPasswordResetAsync(EMailAddress, PasswordResetCode, NewPassword, HttpHelper.GetRemoteIpAddress(Request), GoogleReCaptchaScore, cancellationToken);

                    if (userPasswordResetStatusId == UserPasswordResetStatus.TimedOutExpired)
                    {
                        ErrorMessage = "The Password Reset has Timed Out and Expired. Please Restart the Password Request Process.";

                        EMailAddress = string.Empty;
                        ModelState.Remove(nameof(EMailAddress));
                        PasswordResetCode = string.Empty;
                        ModelState.Remove(nameof(PasswordResetCode));

                        Logger.LogError("Password Reset failed for E-Mail Address {EMailAddress} at {DateTimeUtc}. UserPasswordResetStatusId: {UserPasswordResetStatusId}.", EMailAddress, DateTime.UtcNow, userPasswordResetStatusId);

                        State = ResetPasswordState.EnterEMailAddress;
                    }
                    else if (userPasswordResetStatusId != UserPasswordResetStatus.ValidResetCode)
                    {
                        ErrorMessage = "There was an error resetting your Password. Please try again later.";

                        Logger.LogError("Password Reset failed for E-Mail Address {EMailAddress} at {DateTimeUtc}. UserPasswordResetStatusId: {UserPasswordResetStatusId}.", EMailAddress, DateTime.UtcNow, userPasswordResetStatusId);
                    }
                    else
                    {
                        SuccessMessage = "Your Password has been Reset successfully. You can now Sign In with your new Password.";

                        State = ResetPasswordState.Completed;
                    }
                }
            }
        }
    }
}
