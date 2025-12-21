using Eadent.Common.WebApi.Helpers;
using Eadent.Identity.Access;
using Eadent.Identity.DataAccess.EadentUserIdentity.Entities;
using Eadent.Identity.Definitions;
using Eadent.Identity.Helpers;
using Serilog;
using Serilog.Context;

namespace CentralSecurityServiceAdmin.Sessions
{
    public class UserSession : IUserSession
    {
        private const string SignedInTokenName = "CentralSecurityService.Identity.UserSession.SignedIn.Token";

        private const string SignedOutGuidName = "CentralSecurityService.Identity.UserSession.SignedOut.Guid";

        public class RoleDetails : IUserSession.IRole
        {
            public Role RoleId { get; }

            public short RoleLevel { get; }

            public string RoleName { get; }

            public RoleDetails(RoleEntity roleEntity)
            {
                RoleId = roleEntity.RoleId;
                RoleLevel = roleEntity.RoleLevel;
                RoleName = roleEntity.RoleName;
            }
        }

        private ILogger<UserSession> Logger { get; }

        private HttpContext? HttpContext { get; }

        public string SessionToken { get; private set; }

        public Guid SessionGuid { get; private set; }

        public bool IsSignedIn { get; private set; }

        public bool IsPrivileged { get; private set; }

        public string EMailAddress { get; private set; }

        public string DisplayName { get; private set; }

        public List<IUserSession.IRole> Roles { get; private set; }

        private void SetSessionTokenAndLoggingGuid()
        {
            HttpContext?.Session.SetString(SignedInTokenName, SessionToken);

            LogContext.PushProperty("SessionGuid", SessionGuid);

            // The following line is commented out to avoid Log Files being filled with SessionToken and SessionGuid information for every Page invoked.
            //Logger.LogInformation($"UserSessionToken = {SessionToken} : UserSessionGuid = {SessionGuid}");
        }

        private void HandleSignedInSession(UserSessionEntity userSessionEntity)
        {
            if ((userSessionEntity.UserSessionToken != null) &&
                (userSessionEntity.UserSessionStatusId == UserSessionStatus.SignedIn))
            {
                IsSignedIn = true;
                IsPrivileged = UserRoleHelper.IsPrivileged(userSessionEntity.User.UserRoles);

                EMailAddress = userSessionEntity.EMailAddress;
                DisplayName = userSessionEntity.User.DisplayName;

                Roles = new List<IUserSession.IRole>();

                foreach (var userRoleEntity in userSessionEntity.User.UserRoles)
                {
                    var role = new RoleDetails(userRoleEntity.Role);

                    Roles.Add(role);
                }
            }
        }

        private void HandleSignedOutGuid()
        {
            Guid signedOutGuid = Guid.Empty;

            var signedOutGuidString = HttpContext?.Session.GetString(SignedOutGuidName);

            if (signedOutGuidString != null)
            {
                Guid.TryParse(signedOutGuidString, out signedOutGuid);
            }

            if (signedOutGuid == Guid.Empty)
            {
                signedOutGuid = Guid.NewGuid();
                HttpContext?.Session.SetString(SignedOutGuidName, signedOutGuid.ToString());
            }

            SessionGuid = signedOutGuid;

            LogContext.PushProperty("SessionGuid", SessionGuid);
        }

        public UserSession(ILogger<UserSession> logger, IHttpContextAccessor httpContextAccessor, IEadentUserIdentity eadentUserIdentity)
        {
            Logger = logger;
            HttpContext = httpContextAccessor.HttpContext;

            try
            {
                SessionStatus sessionStatusId = SessionStatus.Error;

                UserSessionEntity userSessionEntity = null;

                var userSessionToken = HttpContext?.Session.GetString(SignedInTokenName);

                if (userSessionToken != null)
                {
                    (sessionStatusId, userSessionEntity) = eadentUserIdentity.CheckAndUpdateUserSession(userSessionToken, HttpHelper.GetRemoteIpAddress(HttpContext?.Request));

                    if (userSessionEntity != null)
                    {
                        SessionToken = userSessionEntity.UserSessionToken;
                        SessionGuid = userSessionEntity.UserSessionGuid;
                    }

                    if ((sessionStatusId == SessionStatus.SignedIn) && (userSessionEntity != null))
                    {
                        HandleSignedInSession(userSessionEntity);
                    }

                    SetSessionTokenAndLoggingGuid();
                }

                if (userSessionEntity == null)
                {
                    HandleSignedOutGuid();
                }
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "An Exception has occurred.");
            }
        }

        public void SignIn(UserSessionEntity userSessionEntity)
        {
            if (userSessionEntity != null)
            {
                SessionToken = userSessionEntity.UserSessionToken;
                SessionGuid = userSessionEntity.UserSessionGuid;

                HandleSignedInSession(userSessionEntity);
            }

            SetSessionTokenAndLoggingGuid();
        }

        public void SignOut()
        {
            Clear();
        }

        public void Clear()
        {
            SessionToken = null;
            SessionGuid = Guid.Empty;

            IsSignedIn = false;
            IsPrivileged = false;

            EMailAddress = null;
            DisplayName = null;

            Roles = null;

            HttpContext?.Session.Remove(SignedInTokenName);
            HandleSignedOutGuid();
        }

        public (bool hasRole, IUserSession.IRole role) HasRole(Role roleId)
        {
            bool hasRole = false;

            IUserSession.IRole role = null;

            if (Roles != null)
            {
                role = Roles.Find(roleItem => roleItem.RoleId == roleId);

                if (role != null)
                {
                    hasRole = true;
                }
            }

            return (hasRole, role);
        }

        public string GetRoleIdsAsString()
        {
            string roleIds = null;

            if (Roles != null)
            {
                foreach (var role in Roles)
                {
                    if (roleIds == null)
                    {
                        roleIds = $"{(short)role.RoleId}";
                    }
                    else
                    {
                        roleIds += $", {(short)role.RoleId}";
                    }
                }
            }

            return roleIds;
        }

        public string GetRoleNamesAsString()
        {
            string roleNames = null;

            if (Roles != null)
            {
                foreach (var role in Roles)
                {
                    if (roleNames == null)
                    {
                        roleNames = $"{role.RoleName}";
                    }
                    else
                    {
                        roleNames += $", {role.RoleName}";
                    }
                }
            }

            return roleNames;
        }
    }
}
