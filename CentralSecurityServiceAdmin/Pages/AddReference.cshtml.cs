using CentralSecurityService.Common.Configuration;
using CentralSecurityService.Common.DataAccess.CentralSecurityService.Databases;
using CentralSecurityService.Common.DataAccess.CentralSecurityService.Entities;
using CentralSecurityService.Common.DataAccess.CentralSecurityService.Repositories;
using CentralSecurityService.Common.Definitions;
using CentralSecurityServiceAdmin.Configuration;
using CentralSecurityServiceAdmin.PagesAdditional;
using CentralSecurityServiceAdmin.Sessions;
using Eadent.Identity.Access;
using Eadent.Identity.Definitions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkiaSharp;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using static System.Net.Mime.MediaTypeNames;

namespace CentralSecurityServiceAdmin.Pages
{
    public class AddReferenceModel : BasePageModel
    {
        private ILogger<AddReferenceModel> Logger { get; }

        private ICentralSecurityServiceDatabase CentralSecurityServiceDatabase { get; set; }

        private IReferencesRepository ReferencesRepository { get; set; }

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

        public AddReferenceModel(ILogger<AddReferenceModel> logger, IConfiguration configuration, IUserSession userSession, IEadentUserIdentity eadentUserIdentity, ICentralSecurityServiceDatabase centralSecurityServiceDatabase, IReferencesRepository referencesRepository)
            : base(logger, configuration, userSession, eadentUserIdentity)
        {
            Logger = logger;
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
                        await AddImageReferenceAsync();
                    }
                    else if (ReferenceTypeId == ReferenceType.VideoUrl)
                    {
                        await AddVideoUrlReferenceAsync();
                    }
                    else
                    {
                        Logger.LogWarning("Unsupported Reference Type: {ReferenceTypeId}.", ReferenceTypeId);
                    }
                }
                catch (Exception exception)
                {
                    Logger.LogError(exception, "An Exception occurred.");
                }
            }
        }

        // Courtesy of www.ChatGpt.com (Modified).
        private static void SaveThumbnailAsJpeg(string imageFilePathAndName, string thumbnailFilePathAndName, int targetWidth)
        {
            // Load the image.
            using var inputStream = System.IO.File.OpenRead(imageFilePathAndName);

            using var original = SKBitmap.Decode(inputStream);

            if (original.Width < targetWidth)
            {
                // If the original image is smaller than the target width, just copy it as is.
                System.IO.File.Copy(imageFilePathAndName, thumbnailFilePathAndName, true);
            }
            else
            {
                int targetHeight = original.Height * targetWidth / original.Width;

                // Resize/Get Thumbnail.
                using var thumbnail = original.Resize(new SKImageInfo(targetWidth, targetHeight), new SKSamplingOptions(new SKCubicResampler()));

                if (thumbnail == null)
                    throw new Exception("Failed to resize image / Get Thumbnail.");

                using var image = SKImage.FromBitmap(thumbnail);

                using var outputStream = System.IO.File.OpenWrite(thumbnailFilePathAndName);

                // Encode to JPEG (or use .Encode(SKEncodedImageFormat.Png, 100) for PNG).
                image.Encode(SKEncodedImageFormat.Jpeg, 90).SaveTo(outputStream);
            }
        }

        private void AddReference(long uniqueReferenceId, ReferenceType referenceTypeId, string referenceName, string thumbnailFileName, string description, string categorisations)
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

            ReferencesRepository.Create(referenceEntity);
            ReferencesRepository.SaveChanges();

            Logger.LogInformation("Image Reference added successfully with UniqueReferenceId: {UniqueReferenceId}.", uniqueReferenceId);
        }

        private async Task AddImageReferenceAsync()
        {
            long uniqueReferenceId = 0;

            string imageFileName = null;

            string thumbnailFileName = null;

            string imageFilePathAndName = null;

            string thumbnailFilePathAndName = null;

            if (ImageFileToUpload == null || ImageFileToUpload.Length == 0)
            {
                Logger.LogWarning("No Image File was uploaded.");
                return;
            }
            else
            {
                uniqueReferenceId = CentralSecurityServiceDatabase.GetNextUniqueReferenceId();

                imageFileName = $"{uniqueReferenceId:R000_000_000}_000-{ImageFileToUpload.FileName}";
                imageFilePathAndName = Path.Combine(CentralSecurityServiceAdminSettings.Instance.References.ReferenceFilesFolder, imageFileName);

                using (var fileStream = new FileStream(imageFilePathAndName, FileMode.Create))
                {
                    await ImageFileToUpload.CopyToAsync(fileStream);
                }
            }

            if (ThumbnailFileToUpload == null || ThumbnailFileToUpload.Length == 0)
            {
                thumbnailFileName = $"{uniqueReferenceId:R000_000_000}_000-Thumbnail_Width_125-{Path.GetFileNameWithoutExtension(imageFileName)}.jpg";
                thumbnailFilePathAndName = Path.Combine(CentralSecurityServiceAdminSettings.Instance.References.ReferenceFilesFolder, thumbnailFileName);

                SaveThumbnailAsJpeg(imageFilePathAndName, thumbnailFilePathAndName, 125);
            }
            else
            {
                // TODO: Look to reduce Duplicate Code with AddVideoUrlReferenceAsync.
                thumbnailFileName = $"{uniqueReferenceId:R000_000_000}_000-Thumbnail_Width_125-{Path.GetFileNameWithoutExtension(ThumbnailFileToUpload.FileName)}.jpg";
                thumbnailFilePathAndName = Path.Combine(CentralSecurityServiceAdminSettings.Instance.References.ReferenceFilesFolder, thumbnailFileName);

                using (var fileStream = new FileStream(thumbnailFilePathAndName, FileMode.Create))
                {
                    await ThumbnailFileToUpload.CopyToAsync(fileStream);
                }
            }

            AddReference(uniqueReferenceId, ReferenceType.Image, imageFileName, thumbnailFileName, Description, Categorisations);
        }

        private async Task AddVideoUrlReferenceAsync()
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
                thumbnailFilePathAndName = Path.Combine(CentralSecurityServiceAdminSettings.Instance.References.ReferenceFilesFolder, thumbnailFileName);

                using (var fileStream = new FileStream(thumbnailFilePathAndName, FileMode.Create))
                {
                    await ThumbnailFileToUpload.CopyToAsync(fileStream);
                }
            }

            AddReference(uniqueReferenceId, ReferenceType.VideoUrl, referenceName, thumbnailFileName, Description, Categorisations);
        }
    }
}
