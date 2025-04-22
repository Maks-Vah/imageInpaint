//Utils\ImageUtils
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using System.IO;

namespace imagePain.Utils
{
    public static class ImageUtils
    {
        public static Image<Rgba32> CreateOverlay(Image<Rgba32> mask)
        {
            var overlay = new Image<Rgba32>(mask.Width, mask.Height);
            overlay.Mutate(ctx => ctx
                .BackgroundColor(new Rgba32(255, 50, 50, 128))
                .DrawImage(mask, PixelColorBlendingMode.Normal, 0.7f));

            return overlay;
        }

        public static System.Drawing.Image ConvertToWinFormsImage(Image<Rgba32> image)
        {
            using var ms = new MemoryStream();
            image.SaveAsPng(ms);
            ms.Position = 0;
            return System.Drawing.Image.FromStream(ms);
        }
    }
}
