// ImageLoader.cs
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Threading.Tasks;

namespace imagePain.Helpers
{
	public static class ImageLoader
	{
		public static async Task<(Image<TPixel> Original, Image<TPixel> Current, Image<TPixel> Mask)> LoadAsync<TPixel>(string path)
			where TPixel : unmanaged, IPixel<TPixel>
		{
			var image = await Image.LoadAsync<TPixel>(path);
			var mask = new Image<TPixel>(image.Width, image.Height);
			mask.Mutate(x => x.BackgroundColor(Color.Black));

			System.Diagnostics.Debug.WriteLine($"Preview size: {image.Width}x{image.Height}");
			return (image.Clone(), image.Clone(), mask);
		}
	}
}