using CentralSecurityService.Common.DataAccess.CentralSecurityService.Databases;
using CentralSecurityService.Common.DataAccess.CentralSecurityService.Entities;
using CentralSecurityService.Common.DataAccess.CentralSecurityService.Repositories;
using CentralSecurityService.Common.Definitions;
using CentralSecurityServiceAdmin.Configuration;
using CentralSecurityServiceAdmin.Helpers;
using CentralSecurityServiceAdmin.PagesAdditional;
using CentralSecurityServiceAdmin.Sessions;
using Eadent.Identity.Access;
using Eadent.Identity.Definitions;
using Microsoft.AspNetCore.Mvc;

namespace CentralSecurityServiceAdmin.Pages
{
    public class AddDataModel : BasePageModel
    {
        private IWebHostEnvironment WebHostEnvironment { get; }

        private ICentralSecurityServiceDatabase CentralSecurityServiceDatabase { get; set; }

        private IReferencesRepository ReferencesRepository { get; set; }

        public string Message { get; set; }

        public AddDataModel(ILogger<AddDataModel> logger, IWebHostEnvironment webHostEnvironment, IConfiguration configuration, IUserSession userSession, IEadentUserIdentity eadentUserIdentity, ICentralSecurityServiceDatabase centralSecurityServiceDatabase, IReferencesRepository referencesRepository)
            : base(logger, configuration, userSession, eadentUserIdentity)
        {
            WebHostEnvironment = webHostEnvironment;
            CentralSecurityServiceDatabase = centralSecurityServiceDatabase;
            ReferencesRepository = referencesRepository;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            IActionResult actionResult = EnsureUserIsSignedIn();

            if (actionResult != null)
            {
                return actionResult; // Redirect to SignIn if user is not Signed In.
            }

            (bool hasRole, IUserSession.IRole role) = UserSession.HasRole(Role.GlobalAdministrator);

            if (!hasRole)
            {
                return LocalRedirect("/"); // User does not have the required Role.
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string action)
        {
            if (UserSession.IsSignedIn)
            {
                (bool hasRole, IUserSession.IRole role) = UserSession.HasRole(Role.GlobalAdministrator);

                if (!hasRole)
                {
                    return LocalRedirect("/"); // User does not have the required Role.
                }
                else
                {
                    if (action == "Add Data")
                    {
                        AddData();
                    }
                }
            }

            return Page();
        }

        private void AddReference(ReferenceType referenceType, string sourceReferenceName, string thumbnailFileName, string description, string categorisations)
        {
            long uniqueReferenceId = CentralSecurityServiceDatabase.GetNextUniqueReferenceId();

            string imageSourceFilePathAndName = null;

            string imageDestinationFilePathAndName = null;

            string thumbnailDestinationFileName = null;

            string thumbnailDestinationFilePathAndName = null;

            string referenceFileName = sourceReferenceName;

            string referenceFilesFolder = null;

            if (WebHostEnvironment.IsDevelopment())
            {
                referenceFilesFolder = CentralSecurityServiceAdminSettings.Instance.References.DevelopmentReferenceFilesFolder;
            }
            else
            {
                referenceFilesFolder = CentralSecurityServiceAdminSettings.Instance.References.ProductionReferenceFilesFolder;
            }

            if (referenceType == ReferenceType.Image)
            {
                referenceFileName = $"{uniqueReferenceId:R000_000_000}_000-{sourceReferenceName}";

                // TODO: Make path fully configurable or use a safer method to construct paths.
                imageSourceFilePathAndName = Path.Combine(referenceFilesFolder, "Source", sourceReferenceName);

                imageDestinationFilePathAndName = Path.Combine(referenceFilesFolder, referenceFileName);

                System.IO.File.Copy(imageSourceFilePathAndName, imageDestinationFilePathAndName, true);
            }

            if (!string.IsNullOrWhiteSpace(thumbnailFileName))
            {
                string thumbnailSourceFilePathAndName = Path.Combine(referenceFilesFolder, "Source", thumbnailFileName);

                thumbnailDestinationFileName = $"{uniqueReferenceId:R000_000_000}_000-{thumbnailFileName}";

                thumbnailDestinationFilePathAndName = Path.Combine(referenceFilesFolder, thumbnailDestinationFileName);

                System.IO.File.Copy(thumbnailSourceFilePathAndName, thumbnailDestinationFilePathAndName, true);
            }

            if ((referenceType == ReferenceType.Image) && string.IsNullOrWhiteSpace(thumbnailFileName))
            {
                thumbnailDestinationFileName = $"{uniqueReferenceId:R000_000_000}_000-Thumbnail_Width_125-{Path.GetFileNameWithoutExtension(sourceReferenceName)}.jpg";

                thumbnailDestinationFilePathAndName = Path.Combine(referenceFilesFolder, thumbnailDestinationFileName);

                ImageHelper.SaveThumbnailAsJpeg(imageSourceFilePathAndName, thumbnailDestinationFilePathAndName, 125);
            }

            var referenceEntity = new ReferenceEntity()
            {
                UniqueReferenceId = uniqueReferenceId,
                SubReferenceId = 0,
                ReferenceTypeId = referenceType,
                ThumbnailFileName = thumbnailDestinationFileName,
                ReferenceName = referenceFileName,
                Description = description,
                Categorisations = categorisations,
                CreatedDateTimeUtc = DateTime.UtcNow,
            };

            ReferencesRepository.Create(referenceEntity);
            ReferencesRepository.SaveChanges();
        }

        private void AddData()
        {
            AddReference(ReferenceType.Image, "00-Elizabeth_Lydia_Manningham_Buller-www.Wikipedia.org-2022_08_14.jpg", "Thumbnail_Width_125-00-Elizabeth_Lydia_Manningham_Buller-www.Wikipedia.org-2022_08_14.jpg", "Elizabeth Lydia Manningham-Buller", "MI5, \"Bad\" Freemason.");
            AddReference(ReferenceType.Image, "01-Maria_Duffy-Bad_CIA-MI5-Bad_Medical-And-Bad_Freemason-Edited_From_Original_Image_Using_IrfanView_64_bit-20140216_201258_1x1.jpg", thumbnailFileName: null, "Maria Duffy", "\"Bad\" CIA, MI5, \"Bad\" Medical, \"Bad\" Freemason.");
            AddReference(ReferenceType.Image, "02-Galina_Carroll-Bad_CIA-MI6-And-Bad_Freemason.jpg", thumbnailFileName: null, "Galina Carroll", "\"Bad\" CIA, MI6, \"Bad\" Freemason.");
            AddReference(ReferenceType.Image, "03-Mary_Siney_Duffy-Bad_CIA-Bad_Medical-And-Bad_Freemason-www.Facebook.com-2025_06_01-46482842_10157032199681454_8007284602844479488_n.jpg", thumbnailFileName: null, "Mary Siney Duffy", "\"Bad\" CIA, MI5, \"Bad\" Medical, \"Bad\" Freemason.");
            AddReference(ReferenceType.Image, "04-Dr._Yolande_Ferguson-Bad_CIA-MI5-Bad_Medical-And-Bad_Freemason-www.Facebook.com-12274309_10153100560791036_6947388060168958277_n.jpg", thumbnailFileName: null, "Dr. Yolande Ferguson", "\"Bad\" CIA, MI5, \"Bad\" Medical, \"Bad\" Freemason.");
            AddReference(ReferenceType.Image, "05-Sinead_Frain-Bad_CIA-MI5-Bad_Medical-And-Bad_Freemason-www.LinkedIn.com-1551534145048.jpg", thumbnailFileName: null, "Sinead Frain", "\"Bad\" CIA, MI5, \"Bad\" Medical, \"Bad\" Freemason.");
            AddReference(ReferenceType.Image, "06-Manu_Gambhir-Bad_Freemason-At_Least-r225x225-www.PocketGamer.biz-2022_11_25.jpg", thumbnailFileName: null, "Manu Gambhir", "\"Bad\" Freemason At Least.");
            AddReference(ReferenceType.Image, "07-Dr._Galal_Badrawy-Bad_CIA-BSS-Bad_Medical-Bad_Freemason-www.Facebook.com-2022_11_28-18222387_364846557250351_8970463873198105780_n.jpg", thumbnailFileName: null, "Dr. Galal Badrawy", "\"Bad\" CIA, BSS, \"Bad\" Medical, \"Bad\" Freemason.");
            AddReference(ReferenceType.Image, "08-Dr._Raju_Bangaru-Bad_Medical-At_Least-HealthService.hse.ie-May_2022_latest_cho_dncc_magazine-2022_11_30.png", thumbnailFileName: null, "Dr, Raju Bangaru", "\"Bad\" Medical At Least.");
            AddReference(ReferenceType.Image, "09-Andrew_Caras_Altas-Bad_Freemason-At_Least-www.Facebook.com-2022_12_01-420280_10151766083634768_314611671_n.jpg", thumbnailFileName: null, "Andrew Caras-Altas", "\"Bad\" Freemason At Least.");
            AddReference(ReferenceType.Image, "10-Jasmine_Fletcher-MI6-At_Least-1-7766-0-0.jpg", thumbnailFileName: null, "Jasmine Fletcher", "MI6 At Least.");
            AddReference(ReferenceType.Image, "11-Stephen_Hadfield-BSS-At_Least-1_0_1_1-0.jpg", thumbnailFileName: null, "Stephen Hadfield", "BSS At Least.");
            AddReference(ReferenceType.Image, "12-Martin_Simpkins-MI5-Bad_Freemason-20140131_174756.jpg", thumbnailFileName: null, "Martin Simpkins", "MI5, \"Bad\" Freemason.");

            AddReference(ReferenceType.VideoUrl, "https://youtu.be/SXIj-ps1Vg0", thumbnailFileName: null, "2025_06_01_0 - The Square, Tallaght, Dublin, Ireland - (S25+).", "Various.");
            AddReference(ReferenceType.VideoUrl, "https://youtu.be/CM7IULLHv9U", thumbnailFileName: null, "2025_06_03_0 - Liffey Valley, Dublin, Ireland - (S25+).", "Various.");
            AddReference(ReferenceType.VideoUrl, "https://youtu.be/65snrvUBdrw", thumbnailFileName: null, "2025_06_04_0 - Stillorgan, Dublin, Ireland - Rotated 180 Degrees - (S25+ And Microsoft Clipchamp).", "Various.");
            AddReference(ReferenceType.VideoUrl, "https://youtu.be/4veAudVmrlk", thumbnailFileName: null, "2025_06_04_1 - Stillorgan, Dublin, Ireland - (S25+).", "Various.");
            AddReference(ReferenceType.VideoUrl, "https://youtu.be/u8gaUxAoAXg", thumbnailFileName: null, "2025_06_06_0 - Tesco, Liffey Valley, Dublin, Ireland - (S25+).", "Various.");

            AddReference(ReferenceType.Image, "13-Éamonn_Anthony_Duffy-A_1-Gimp-Rotated_90_Degrees_Clockwise-Slice_20_80-20210924_204416-0-1.jpg", "Thumbnail_Width_125-13-Éamonn_Anthony_Duffy-A_1-Gimp-Rotated_90_Degrees_Clockwise-Slice_20_80-20210924_204416-0-1.jpg", "Eamonn/Éamonn Anthony Duffy.", "None - He is a \"1\".");
            AddReference(ReferenceType.Image, "14-Walter_Maguire-Gimp-Slice_66.6-Horizontal_10_50-www.Facebook.com-2025_06_08-468281894_10161023495633207_5359474878369017671_n-1-0.jpg", thumbnailFileName: null, "Walter Maguire.", "\"Bad\" CIA, \"Bad\" Freemason At Least.");

            Message = "Test data added successfully.";
        }
    }
}
