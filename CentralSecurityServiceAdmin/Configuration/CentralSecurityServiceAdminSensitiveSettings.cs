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
            public string AdminDisplayName { get; set; }

            public string AdminEMailAddress { get; set; }

            public string AdminUserGuid { get; set; }

            public string AdminPassword { get; set; }

            public string AdminMobilePhoneNumber { get; set; }
        }

        public class EMailSettings
        {
            public string HostName { get; set; }

            public int HostPort { get; set; }

            public string SenderEMailAddress { get; set; }

            public string SenderPassword { get; set; }

            public string FromEMailName { get; set; }

            public string FromEMailAddress { get; set; }
        }

        public AdminAccountSettings AdminAccount { get; set; }

        public EMailSettings EMail { get; set; }
    }
}
