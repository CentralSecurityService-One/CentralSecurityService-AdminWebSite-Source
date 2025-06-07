using Eadent.Identity.DataAccess.EadentUserIdentity.Entities;
using Eadent.Identity.Definitions;

namespace CentralSecurityServiceAdmin.Sessions
{
    public interface IUserSession
    {
        public interface IRole
        {
            Role RoleId { get; }

            short RoleLevel { get; }

            string RoleName { get; }
        }

        string SessionToken { get; }

        Guid SessionGuid { get; }

        bool IsSignedIn { get; }

        bool IsPrivileged { get; }

        string EMailAddress { get; }

        string DisplayName { get; }

        List<IRole> Roles { get; }

        void SignIn(UserSessionEntity userSessionEntity);

        void SignOut();

        void Clear();

        (bool hasRole, IRole role) HasRole(Role roleId);

        string GetRoleIdsAsString();

        string GetRoleNamesAsString();
    }
}
