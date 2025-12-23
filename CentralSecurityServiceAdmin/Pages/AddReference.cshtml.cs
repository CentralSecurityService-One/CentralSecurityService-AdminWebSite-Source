using CentralSecurityService.Common.Configuration;
using CentralSecurityService.Common.DataAccess.CentralSecurityService.Databases;
using CentralSecurityService.Common.DataAccess.CentralSecurityService.Entities;
using CentralSecurityService.Common.DataAccess.CentralSecurityService.Repositories;
using CentralSecurityService.Common.Definitions;
using CentralSecurityServiceAdmin.Configuration;
using CentralSecurityServiceAdmin.Helpers;
using CentralSecurityServiceAdmin.PagesAdditional;
using CentralSecurityServiceAdmin.Sessions;
using Eadent.Identity.Access;
using Microsoft.AspNetCore.Mvc;

namespace CentralSecurityServiceAdmin.Pages
{
    public class AddReferenceModel : BasePageModel
    {
        private ILogger<AddReferenceModel> Logger { get; }

        private IWebHostEnvironment WebHostEnvironment { get; }

        private ICentralSecurityServiceDatabase CentralSecurityServiceDatabase { get; set; }

        private IReferencesRepository ReferencesRepository { get; set; }

        public string Message { get; set; }

        [BindProperty]
        public ReferenceType ReferenceTypeId { get; set; }

        [BindProperty]
        public string Description { get; set; }

        [BindProperty]
        public string Categorisations { get; set; }

        [BindProperty]
        public IFormFile ImageFileToUpload { get; set; }

        [BindProperty]
        public IFormFile ThumbnailFileToUpload { get; set; }

        [BindProperty]
        public string VideoUrl { get; set; }

        [BindProperty]
        public string ReferenceUrl { get; set; }

        public AddReferenceModel(ILogger<AddReferenceModel> logger, IWebHostEnvironment webHostEnvironment, IConfiguration configuration, IUserSession userSession, IEadentUserIdentity eadentUserIdentity, ICentralSecurityServiceDatabase centralSecurityServiceDatabase, IReferencesRepository referencesRepository)
            : base(logger, configuration, userSession, eadentUserIdentity)
        {
            Logger = logger;
            WebHostEnvironment = webHostEnvironment;
            CentralSecurityServiceDatabase = centralSecurityServiceDatabase;
            ReferencesRepository = referencesRepository;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            Logger.LogInformation("Add Reference page accessed.");

            IActionResult actionResult = EnsureUserIsSignedIn();

            if (actionResult != null)
            {
                return actionResult; // Redirect to SignIn if user is not Signed In.
            }

            return Page();
        }

        public async Task OnPostAsync(string action)
        {
            if (UserSession.IsSignedIn)
            {
                (bool success, decimal googleReCaptchaScore) = await GoogleReCaptchaAsync();

                GoogleReCaptchaScore = googleReCaptchaScore;

                if (googleReCaptchaScore < CentralSecurityServiceCommonSettings.Instance.GoogleReCaptcha.MinimumScore)
                {
                    Logger.LogWarning("You are unable to Add A Reference because of a poor Google ReCaptcha Score {GoogleReCaptchaScore}.", googleReCaptchaScore);
                }
                else if (action == "Add Reference")
                {
                    try
                    {
                        if (ReferenceTypeId == ReferenceType.Image)
                        {
                            await AddImageReferenceAsync(HttpContext.RequestAborted);

                            Message = "Image Reference added successfully.";
                        }
                        else if (ReferenceTypeId == ReferenceType.VideoUrl)
                        {
                            await AddVideoUrlReferenceAsync(HttpContext.RequestAborted);

                            Message = "Video Url Reference added successfully.";
                        }
                        else if (ReferenceTypeId == ReferenceType.Url)
                        {
                            await AddUrlReferenceAsync(HttpContext.RequestAborted);

                            Message = "Url Reference added successfully.";
                        }
                        else
                        {
                            Logger.LogWarning("Unsupported Reference Type: {ReferenceTypeId}.", ReferenceTypeId);

                            Message = "Unsupported Reference Type. Please try again.";
                        }
                    }
                    catch (Exception exception)
                    {
                        Logger.LogError(exception, "An Exception occurred.");

                        Message = "An error occurred while adding the Reference. Please try again later.";
                    }
                }
            }
        }

        private async Task AddReferenceAsync(long uniqueReferenceId, ReferenceType referenceTypeId, string referenceName, string thumbnailFileName, string description, string categorisations, CancellationToken cancellationToken = default)
        {
            var referenceEntity = new ReferenceEntity()
            {
                UniqueReferenceId = uniqueReferenceId,
                SubReferenceId = 0,
                ReferenceTypeId = referenceTypeId,
                ThumbnailFileName = thumbnailFileName,
                ReferenceName = referenceName,
                Description = description,
                Categorisations = categorisations,
                CreatedDateTimeUtc = DateTime.UtcNow,
            };

            await ReferencesRepository.CreateAsync(referenceEntity, cancellationToken);
            await ReferencesRepository.SaveChangesAsync(cancellationToken);

            Logger.LogInformation("Image Reference added successfully with UniqueReferenceId: {UniqueReferenceId}.", uniqueReferenceId);
        }

        private async Task AddImageReferenceAsync(CancellationToken cancellationToken = default)
        {
            long uniqueReferenceId = 0;

            string imageFileName = null;

            string thumbnailFileName = null;

            string imageFilePathAndName = null;

            string thumbnailFilePathAndName = null;

            string referenceFilesFolder = null;

            if (WebHostEnvironment.IsDevelopment())
            {
                referenceFilesFolder = CentralSecurityServiceAdminSettings.Instance.References.DevelopmentReferenceFilesFolder;
            }
            else
            {
                referenceFilesFolder = CentralSecurityServiceAdminSettings.Instance.References.ProductionReferenceFilesFolder;
            }

            if (ImageFileToUpload == null || ImageFileToUpload.Length == 0)
            {
                Logger.LogWarning("No Image File was uploaded.");
                return;
            }
            else
            {
                uniqueReferenceId = CentralSecurityServiceDatabase.GetNextUniqueReferenceId();

                imageFileName = $"{uniqueReferenceId:R000_000_000}_000-{ImageFileToUpload.FileName}";
                imageFilePathAndName = Path.Combine(referenceFilesFolder, imageFileName);

                using (var fileStream = new FileStream(imageFilePathAndName, FileMode.Create))
                {
                    await ImageFileToUpload.CopyToAsync(fileStream);
                }
            }

            if (ThumbnailFileToUpload == null || ThumbnailFileToUpload.Length == 0)
            {
                thumbnailFileName = $"{uniqueReferenceId:R000_000_000}_000-Thumbnail_Width_125-{Path.GetFileNameWithoutExtension(imageFileName)}.jpg";
                thumbnailFilePathAndName = Path.Combine(referenceFilesFolder, thumbnailFileName);

                ImageHelper.SaveThumbnailAsJpeg(imageFilePathAndName, thumbnailFilePathAndName, 125);
            }
            else
            {
                // TODO: Look to reduce Duplicate Code with AddVideoUrlReferenceAsync.
                thumbnailFileName = $"{uniqueReferenceId:R000_000_000}_000-Thumbnail_Width_125-{Path.GetFileNameWithoutExtension(ThumbnailFileToUpload.FileName)}.jpg";
                thumbnailFilePathAndName = Path.Combine(referenceFilesFolder, thumbnailFileName);

                using (var fileStream = new FileStream(thumbnailFilePathAndName, FileMode.Create))
                {
                    await ThumbnailFileToUpload.CopyToAsync(fileStream);
                }
            }

            await AddReferenceAsync(uniqueReferenceId, ReferenceType.Image, imageFileName, thumbnailFileName, Description, Categorisations, cancellationToken);
        }

        // TODO: Look to reduce Duplicate Code with AddUrlReferenceAsync.
        private async Task AddVideoUrlReferenceAsync(CancellationToken cancellationToken = default)
        {
            string referenceName = null;

            string thumbnailFileName = null;

            string thumbnailFilePathAndName = null;

            if (string.IsNullOrWhiteSpace(VideoUrl))
            {
                Logger.LogWarning("No Video Url was specified.");
                return;
            }
            else
            {
                referenceName = VideoUrl.Trim();
            }

            string referenceFilesFolder = null;

            if (WebHostEnvironment.IsDevelopment())
            {
                referenceFilesFolder = CentralSecurityServiceAdminSettings.Instance.References.DevelopmentReferenceFilesFolder;
            }
            else
            {
                referenceFilesFolder = CentralSecurityServiceAdminSettings.Instance.References.ProductionReferenceFilesFolder;
            }
            long uniqueReferenceId = CentralSecurityServiceDatabase.GetNextUniqueReferenceId();

            if (ThumbnailFileToUpload == null || ThumbnailFileToUpload.Length == 0)
            {
                thumbnailFileName = null;
                thumbnailFilePathAndName = null;
            }
            else
            {
                // TODO: Look to reduce Duplicate Code with AddImageReferenceAsync.
                thumbnailFileName = $"{uniqueReferenceId:R000_000_000}_000-Thumbnail_Width_125-{Path.GetFileNameWithoutExtension(ThumbnailFileToUpload.FileName)}.jpg";
                thumbnailFilePathAndName = Path.Combine(referenceFilesFolder, thumbnailFileName);

                using (var fileStream = new FileStream(thumbnailFilePathAndName, FileMode.Create))
                {
                    await ThumbnailFileToUpload.CopyToAsync(fileStream);
                }
            }

            await AddReferenceAsync(uniqueReferenceId, ReferenceType.VideoUrl, referenceName, thumbnailFileName, Description, Categorisations, cancellationToken);
        }

        // TODO: Look to reduce Duplicate Code with AddVideoUrlReferenceAsync.
        private async Task AddUrlReferenceAsync(CancellationToken cancellationToken = default)
        {
            string referenceName = null;

            string thumbnailFileName = null;

            string thumbnailFilePathAndName = null;

            string referenceFilesFolder = null;

            if (WebHostEnvironment.IsDevelopment())
            {
                referenceFilesFolder = CentralSecurityServiceAdminSettings.Instance.References.DevelopmentReferenceFilesFolder;
            }
            else
            {
                referenceFilesFolder = CentralSecurityServiceAdminSettings.Instance.References.ProductionReferenceFilesFolder;
            }

            if (string.IsNullOrWhiteSpace(ReferenceUrl))
            {
                Logger.LogWarning("No Url was specified.");
                return;
            }
            else
            {
                referenceName = ReferenceUrl.Trim();
            }

            long uniqueReferenceId = CentralSecurityServiceDatabase.GetNextUniqueReferenceId();

            if (ThumbnailFileToUpload == null || ThumbnailFileToUpload.Length == 0)
            {
                thumbnailFileName = null;
                thumbnailFilePathAndName = null;
            }
            else
            {
                // TODO: Look to reduce Duplicate Code with AddImageReferenceAsync.
                thumbnailFileName = $"{uniqueReferenceId:R000_000_000}_000-Thumbnail_Width_125-{Path.GetFileNameWithoutExtension(ThumbnailFileToUpload.FileName)}.jpg";
                thumbnailFilePathAndName = Path.Combine(referenceFilesFolder, thumbnailFileName);

                using (var fileStream = new FileStream(thumbnailFilePathAndName, FileMode.Create))
                {
                    await ThumbnailFileToUpload.CopyToAsync(fileStream);
                }
            }

            await AddReferenceAsync(uniqueReferenceId, ReferenceType.Url, referenceName, thumbnailFileName, Description, Categorisations, cancellationToken);
        }
    }
}
