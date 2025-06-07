namespace CentralSecurityServiceAdmin.Configuration
{
    public class CentralSecurityServiceAdminSettings
    {
        public const string SectionName = "CentralSecurityServiceAdmin";

        public static CentralSecurityServiceAdminSettings Instance { get; private set; }

        public CentralSecurityServiceAdminSettings()
        {
            Instance = this;
        }

        public class SensitiveSettings
        {
            public string Folder { get; set; }
        }

        public SensitiveSettings Sensitive { get; set; }
    }
}
