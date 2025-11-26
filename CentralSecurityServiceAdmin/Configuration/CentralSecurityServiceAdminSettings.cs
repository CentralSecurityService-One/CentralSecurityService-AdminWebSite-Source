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
            public string DevelopmentFolder { get; set; }

            public string ProductionFolder { get; set; }
        }

        public class ReferencesSettings
        {
            public string SourceReferenceFilesFolder { get; set; }

            public string DevelopmentReferenceFilesFolder { get; set; }

            public string ProductionReferenceFilesFolder { get; set; }
        }

        public SensitiveSettings Sensitive { get; set; }

        public ReferencesSettings References { get; set; }
    }
}
