using SkiaSharp;

namespace CentralSecurityServiceAdmin.Helpers
{
    public class ImageHelper
    {
        // Courtesy of www.ChatGpt.com (Modified).
        public static void SaveThumbnailAsJpeg(string imageFilePathAndName, string thumbnailFilePathAndName, int targetWidth)
        {
            // Load the image.
            using var inputStream = File.OpenRead(imageFilePathAndName);

            using var original = SKBitmap.Decode(inputStream);

            if (original.Width <= targetWidth)
            {
                // If the original image is smaller than the target width, just copy it as is.
                File.Copy(imageFilePathAndName, thumbnailFilePathAndName, true);
            }
            else
            {
                int targetHeight = original.Height * targetWidth / original.Width;

                // Resize/Get Thumbnail.
                using var thumbnail = original.Resize(new SKImageInfo(targetWidth, targetHeight), new SKSamplingOptions(new SKCubicResampler()));

                if (thumbnail == null)
                    throw new Exception("Failed to resize image / Get Thumbnail.");

                using var image = SKImage.FromBitmap(thumbnail);

                using var outputStream = File.OpenWrite(thumbnailFilePathAndName);

                // Encode to JPEG (or use .Encode(SKEncodedImageFormat.Png, 100) for PNG).
                image.Encode(SKEncodedImageFormat.Jpeg, 90).SaveTo(outputStream);
            }
        }
    }
}
