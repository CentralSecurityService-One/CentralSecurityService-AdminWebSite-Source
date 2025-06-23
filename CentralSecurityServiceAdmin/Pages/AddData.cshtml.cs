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
        private ILogger<AddDataModel> Logger { get; }

        private IWebHostEnvironment WebHostEnvironment { get; }

        private ICentralSecurityServiceDatabase CentralSecurityServiceDatabase { get; set; }

        private IReferencesRepository ReferencesRepository { get; set; }

        public string Message { get; set; }

        [BindProperty]
        public bool OnlyAddIfUnique { get; set; } = true; // Default to checked.

        public AddDataModel(ILogger<AddDataModel> logger, IWebHostEnvironment webHostEnvironment, IConfiguration configuration, IUserSession userSession, IEadentUserIdentity eadentUserIdentity, ICentralSecurityServiceDatabase centralSecurityServiceDatabase, IReferencesRepository referencesRepository)
            : base(logger, configuration, userSession, eadentUserIdentity)
        {
            Logger = logger;
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
                        await AddDataAsync(HttpContext.RequestAborted);
                    }
                }
            }

            return Page();
        }

        private async Task<long> AddReferenceAsync(bool onlyAddIfUnique, ReferenceType referenceType, string sourceReferenceName, string thumbnailFileName, string description, string categorisations, CancellationToken cancellationToken = default)
        {
            long numReferencesAdded = 0;

            if (onlyAddIfUnique)
            {
                bool referenceExists = false;

                if (referenceType == ReferenceType.Image)
                {
                    referenceExists = await ReferencesRepository.ReferenceExistsIgnoringUniqueReferenceIdPrefixAsync(sourceReferenceName, cancellationToken);
                }
                else
                {
                    referenceExists = await ReferencesRepository.ReferenceExistsAsync(sourceReferenceName, cancellationToken);
                }

                if (referenceExists)
                {
                    Logger.LogInformation("ReferenceName '{SourceReferenceName}' already exists. Skipping addition.", sourceReferenceName);

                    return numReferencesAdded; // Exit if the reference already exists.
                }
            }

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

            ++numReferencesAdded;

            return numReferencesAdded;
        }

        private async Task AddDataAsync(CancellationToken cancellationToken)
        {
            long totalNumReferencesAdded = 0;

            totalNumReferencesAdded += await AddReferenceAsync(OnlyAddIfUnique, ReferenceType.Image, "00-Elizabeth_Lydia_Manningham_Buller-www.Wikipedia.org-2022_08_14.jpg", "Thumbnail_Width_125-00-Elizabeth_Lydia_Manningham_Buller-www.Wikipedia.org-2022_08_14.jpg", "Elizabeth Lydia Manningham-Buller.", "MI5, \"Bad\" Freemason.", cancellationToken);
            totalNumReferencesAdded += await AddReferenceAsync(OnlyAddIfUnique, ReferenceType.Image, "01-Maria_Duffy-Bad_CIA-MI5-Bad_Medical-And-Bad_Freemason-Edited_From_Original_Image_Using_IrfanView_64_bit-20140216_201258_1x1.jpg", thumbnailFileName: null, "Maria Duffy.", "\"Bad\" CIA, MI5, \"Bad\" Medical, \"Bad\" Freemason.", cancellationToken);
            totalNumReferencesAdded += await AddReferenceAsync(OnlyAddIfUnique, ReferenceType.Image, "02-Galina_Carroll-Bad_CIA-MI6-And-Bad_Freemason.jpg", thumbnailFileName: null, "Galina Carroll.", "\"Bad\" CIA, MI6, \"Bad\" Freemason.", cancellationToken);
            totalNumReferencesAdded += await AddReferenceAsync(OnlyAddIfUnique, ReferenceType.Image, "03-Mary_Siney_Duffy-Bad_CIA-Bad_Medical-And-Bad_Freemason-www.Facebook.com-2025_06_01-46482842_10157032199681454_8007284602844479488_n.jpg", thumbnailFileName: null, "Mary Siney Duffy.", "\"Bad\" CIA, MI5, \"Bad\" Medical, \"Bad\" Freemason.", cancellationToken);
            totalNumReferencesAdded += await AddReferenceAsync(OnlyAddIfUnique, ReferenceType.Image, "04-Dr._Yolande_Ferguson-Bad_CIA-MI5-Bad_Medical-And-Bad_Freemason-www.Facebook.com-12274309_10153100560791036_6947388060168958277_n.jpg", thumbnailFileName: null, "Dr. Yolande Ferguson.", "\"Bad\" CIA, MI5, \"Bad\" Medical, \"Bad\" Freemason.", cancellationToken);
            totalNumReferencesAdded += await AddReferenceAsync(OnlyAddIfUnique, ReferenceType.Image, "05-Sinead_Frain-Bad_CIA-MI5-Bad_Medical-And-Bad_Freemason-www.LinkedIn.com-1551534145048.jpg", thumbnailFileName: null, "Sinead Frain.", "\"Bad\" CIA, MI5, \"Bad\" Medical, \"Bad\" Freemason.", cancellationToken);
            totalNumReferencesAdded += await AddReferenceAsync(OnlyAddIfUnique, ReferenceType.Image, "06-Manu_Gambhir-Bad_Freemason-At_Least-r225x225-www.PocketGamer.biz-2022_11_25.jpg", thumbnailFileName: null, "Manu Gambhir.", "\"Bad\" Freemason, at least.", cancellationToken);
            totalNumReferencesAdded += await AddReferenceAsync(OnlyAddIfUnique, ReferenceType.Image, "07-Dr._Galal_Badrawy-Bad_CIA-BSS-Bad_Medical-Bad_Freemason-www.Facebook.com-2022_11_28-18222387_364846557250351_8970463873198105780_n.jpg", thumbnailFileName: null, "Dr. Galal Badrawy.", "\"Bad\" CIA, BSS, \"Bad\" Medical, \"Bad\" Freemason.", cancellationToken);
            totalNumReferencesAdded += await AddReferenceAsync(OnlyAddIfUnique, ReferenceType.Image, "08-Dr._Raju_Bangaru-Bad_Medical-At_Least-HealthService.hse.ie-May_2022_latest_cho_dncc_magazine-2022_11_30.png", thumbnailFileName: null, "Dr. Raju Bangaru.", "\"Bad\" Medical, at least.", cancellationToken);
            totalNumReferencesAdded += await AddReferenceAsync(OnlyAddIfUnique, ReferenceType.Image, "09-Andrew_Caras_Altas-Bad_Freemason-At_Least-www.Facebook.com-2022_12_01-420280_10151766083634768_314611671_n.jpg", thumbnailFileName: null, "Andrew Caras-Altas.", "\"Bad\" Freemason, at least.", cancellationToken);
            totalNumReferencesAdded += await AddReferenceAsync(OnlyAddIfUnique, ReferenceType.Image, "10-Jasmine_Fletcher-MI6-At_Least-1-7766-0-0.jpg", thumbnailFileName: null, "Jasmine Fletcher.", "MI6, at least.", cancellationToken);
            totalNumReferencesAdded += await AddReferenceAsync(OnlyAddIfUnique, ReferenceType.Image, "11-Stephen_Hadfield-BSS-At_Least-1_0_1_1-0.jpg", thumbnailFileName: null, "Stephen Hadfield.", "BSS, at least.", cancellationToken);
            totalNumReferencesAdded += await AddReferenceAsync(OnlyAddIfUnique, ReferenceType.Image, "12-Martin_Simpkins-MI5-Bad_Freemason-20140131_174756.jpg", thumbnailFileName: null, "Martin Simpkins.", "MI5, \"Bad\" Freemason.", cancellationToken);

            totalNumReferencesAdded += await AddReferenceAsync(OnlyAddIfUnique, ReferenceType.VideoUrl, "https://youtu.be/SXIj-ps1Vg0", thumbnailFileName: null, "2025_06_01_0 - The Square, Tallaght, Dublin, Ireland - (S25+).", "Various.", cancellationToken);
            totalNumReferencesAdded += await AddReferenceAsync(OnlyAddIfUnique, ReferenceType.VideoUrl, "https://youtu.be/CM7IULLHv9U", thumbnailFileName: null, "2025_06_03_0 - Liffey Valley, Dublin, Ireland - (S25+).", "Various.", cancellationToken);
            totalNumReferencesAdded += await AddReferenceAsync(OnlyAddIfUnique, ReferenceType.VideoUrl, "https://youtu.be/65snrvUBdrw", thumbnailFileName: null, "2025_06_04_0 - Stillorgan, Dublin, Ireland - Rotated 180 Degrees - (S25+ And Microsoft Clipchamp).", "Various.", cancellationToken);
            totalNumReferencesAdded += await AddReferenceAsync(OnlyAddIfUnique, ReferenceType.VideoUrl, "https://youtu.be/4veAudVmrlk", thumbnailFileName: null, "2025_06_04_1 - Stillorgan, Dublin, Ireland - (S25+).", "Various.", cancellationToken);
            totalNumReferencesAdded += await AddReferenceAsync(OnlyAddIfUnique, ReferenceType.VideoUrl, "https://youtu.be/u8gaUxAoAXg", thumbnailFileName: null, "2025_06_06_0 - Tesco, Liffey Valley, Dublin, Ireland - (S25+).", "Various.", cancellationToken);

            totalNumReferencesAdded += await AddReferenceAsync(OnlyAddIfUnique, ReferenceType.Image, "13-Éamonn_Anthony_Duffy-A_1-Gimp-Rotated_90_Degrees_Clockwise-Slice_20_80-20210924_204416-0-1.jpg", "Thumbnail_Width_125-13-Éamonn_Anthony_Duffy-A_1-Gimp-Rotated_90_Degrees_Clockwise-Slice_20_80-20210924_204416-0-1.jpg", "Eamonn Anthony Duffy.", "None - He is a \"1\".", cancellationToken);
            totalNumReferencesAdded += await AddReferenceAsync(OnlyAddIfUnique, ReferenceType.Image, "14-Walter_Maguire-Gimp-Slice_66.6-Horizontal_10_50-www.Facebook.com-2025_06_08-468281894_10161023495633207_5359474878369017671_n-1-0.jpg", thumbnailFileName: null, "Walter Maguire.", "\"Bad\" CIA, \"Bad\" Freemason, at least.", cancellationToken);

            totalNumReferencesAdded += await AddReferenceAsync(OnlyAddIfUnique, ReferenceType.VideoUrl, "https://youtu.be/jIqyQ9fT9dg", "Thumbnail_Width_125-2025_06_14-www.Ucc.ie-UCC-BP1-Photo-of-George-Boole-standing-267x420.png", "George Boole (In Memory).", "Unknown.", cancellationToken);

            totalNumReferencesAdded += await AddReferenceAsync(OnlyAddIfUnique, ReferenceType.Image, "20250613_115724.jpg", "Thumbnail_Width_125-20250613_115724.jpg", "Heuston Station, Dublin, Ireland.", "Suspected \"Bad\" Freemason, at least.", cancellationToken);
            totalNumReferencesAdded += await AddReferenceAsync(OnlyAddIfUnique, ReferenceType.Image, "20250613_175958.jpg", "Thumbnail_Width_125-20250613_175958.jpg", "Cork City, Ireland.", "Suspected \"Bad\" Freemasons, at least.", cancellationToken);
            totalNumReferencesAdded += await AddReferenceAsync(OnlyAddIfUnique, ReferenceType.Image, "20250613_181140.jpg", "Thumbnail_Width_125-20250613_181140.jpg", "Cork City, Ireland.", "Suspected \"Bad\" Freemasons, at least.", cancellationToken);
            totalNumReferencesAdded += await AddReferenceAsync(OnlyAddIfUnique, ReferenceType.Image, "20250613_182101.jpg", thumbnailFileName: null, "Cork City, Ireland.", "Suspected \"Bad\" Freemasons, at least.", cancellationToken);
            totalNumReferencesAdded += await AddReferenceAsync(OnlyAddIfUnique, ReferenceType.Image, "20250613_182828.jpg", thumbnailFileName: null, "Cork City, Ireland.", "Suspected \"Bad\" Freemasons, at least.", cancellationToken);
            totalNumReferencesAdded += await AddReferenceAsync(OnlyAddIfUnique, ReferenceType.Image, "20250613_183108.jpg", thumbnailFileName: null, "Cork City, Ireland.", "Suspected \"Bad\" Freemason, at least.", cancellationToken);
            totalNumReferencesAdded += await AddReferenceAsync(OnlyAddIfUnique, ReferenceType.Image, "20250613_190617.jpg", "Thumbnail_Width_125-20250613_190617.jpg", "Cork City, Ireland.", "Suspected \"Bad\" Freemasons, at least.", cancellationToken);
            totalNumReferencesAdded += await AddReferenceAsync(OnlyAddIfUnique, ReferenceType.Image, "20250614_195612.jpg", thumbnailFileName: null, "Cork City, Ireland.", "Suspected \"Bad\" Freemasons, at least.", cancellationToken);
            totalNumReferencesAdded += await AddReferenceAsync(OnlyAddIfUnique, ReferenceType.Image, "20250614_202752.jpg", thumbnailFileName: null, "Cork City, Ireland.", "Suspected \"Bad\" Freemasons, and Suspected \"Bad\" CIA, at least.", cancellationToken);

            totalNumReferencesAdded += await AddReferenceAsync(OnlyAddIfUnique, ReferenceType.Image, "20250616_190000.jpg", "Thumbnail_Width_125-20250616_190000.jpg", "A Dead Brown Bee, On A Car, Lucan, Co. Dublin, Ireland.", "None - Nature.", cancellationToken);

            totalNumReferencesAdded += await AddReferenceAsync(OnlyAddIfUnique, ReferenceType.Image, "15-Gary_Boyle-www.Facebook.com-2025_06_23-64803765_2099120116864965_2816587334504415232_n-0-0.png", thumbnailFileName: null, "Gary Boyle.", "\"Bad\" CIA, MI6, \"Bad\" Freemason.", cancellationToken);
            totalNumReferencesAdded += await AddReferenceAsync(OnlyAddIfUnique, ReferenceType.Image, "16-Emer_Boyle-www.Facebook.com-2025_06_23-505385803_10224272635683155_77870391914975521_n.jpg", thumbnailFileName: null, "Emer Boyle.", "\"Bad\" CIA, MI5, \"Bad\" Medical, \"Bad\" Freemason.", cancellationToken);
            totalNumReferencesAdded += await AddReferenceAsync(OnlyAddIfUnique, ReferenceType.Image, "17-Daire-Boyle-M24-Daire Boyle-Boyle_2.jpg", thumbnailFileName: null, "Daire Boyle.", "\"Bad\" CIA, \"Bad\" Freemason, at least.", cancellationToken);
            totalNumReferencesAdded += await AddReferenceAsync(OnlyAddIfUnique, ReferenceType.Image, "18-Bronwyn_Boyle-76648989_2536357966586983_6900569765157273600_n.jpg", thumbnailFileName: null, "Bronwyn Boyle.", "\"Bad\" CIA, \"Bad\" Freemason, at least.", cancellationToken);

            Message = $"{totalNumReferencesAdded} Reference(s) Added.";
        }
    }
}
