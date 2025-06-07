namespace CentralSecurityServiceAdmin.Configuration
{
    public class CentralSecurityServiceAdminSensitiveSettings
    {
        public const string SectionName = "CentralSecurityServiceAdminSensitive";

        public static CentralSecurityServiceAdminSensitiveSettings Instance { get; private set; }

        public CentralSecurityServiceAdminSensitiveSettings()
        {
            Instance = this;
        }

        public class AdminAccountSettings
        {
            public string AdminEMailAddress { get; set; }

            public string AdminUserGuid { get; set; }

            public string AdminPassword { get; set; }

            public string AdminMobilePhoneNumber { get; set; }
        }

        public AdminAccountSettings AdminAccount { get; set; }
    }
}
