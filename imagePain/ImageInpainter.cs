using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;

namespace imagePain;

public sealed class ImageInpainter : IDisposable
{
    private const int TargetSize = 512;
    private readonly InferenceSession _session;

    public ImageInpainter(string modelPath)
    {
        var options = new SessionOptions { GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL };

        try { options.AppendExecutionProvider_CUDA(); }
        catch { options.AppendExecutionProvider_CPU(); }

        _session = new InferenceSession(modelPath, options);
    }

    public Image<Rgba32> Inpaint(Image<Rgba32> image, Image<Rgba32> mask)
    {
        var (img, msk, crop) = PrepareInputs(image, mask);
        var (imgTensor, maskTensor) = CreateTensors(img, msk);

        using var results = _session.Run(new[]
        {
            NamedOnnxValue.CreateFromTensor("image", imgTensor),
            NamedOnnxValue.CreateFromTensor("mask", maskTensor)
        });

        return PasteResult(results[0].AsTensor<float>(), image, crop);
    }

    private (Image<Rgba32>, Image<Rgba32>, Rectangle) PrepareInputs(Image<Rgba32> image, Image<Rgba32> mask)
    {
        var bounds = GetMaskBounds(mask);
        var cropRect = GetCropRect(bounds, image.Width, image.Height);

        var croppedImage = image.Clone(x => x.Crop(cropRect).Resize(TargetSize, TargetSize));
        var croppedMask = mask.Clone(x => x.Crop(cropRect).Resize(TargetSize, TargetSize));

        // Бинаризация маски
        croppedMask.Mutate(x => x.BinaryThreshold(0.5f)); // Чётко, вместо ручного пиксельного перебора

        return (croppedImage, croppedMask, cropRect);
    }

    private Rectangle GetMaskBounds(Image<Rgba32> mask)
    {
        int minX = mask.Width, minY = mask.Height, maxX = 0, maxY = 0;

        mask.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < row.Length; x++)
                {
                    if (row[x].R > 128)
                    {
                        minX = Math.Min(minX, x);
                        minY = Math.Min(minY, y);
                        maxX = Math.Max(maxX, x);
                        maxY = Math.Max(maxY, y);
                    }
                }
            }
        });

        return minX > maxX || minY > maxY
            ? new Rectangle(0, 0, 1, 1)
            : Rectangle.FromLTRB(minX, minY, maxX + 1, maxY + 1);
    }

    private Rectangle GetCropRect(Rectangle bounds, int width, int height)
    {
        int centerX = bounds.X + bounds.Width / 2;
        int centerY = bounds.Y + bounds.Height / 2;

        int x = Math.Clamp(centerX - TargetSize / 2, 0, width - TargetSize);
        int y = Math.Clamp(centerY - TargetSize / 2, 0, height - TargetSize);

        return new Rectangle(x, y, TargetSize, TargetSize);
    }

    private (DenseTensor<float>, DenseTensor<float>) CreateTensors(Image<Rgba32> image, Image<Rgba32> mask)
    {
        var imgTensor = new DenseTensor<float>(new[] { 1, 3, TargetSize, TargetSize });
        var maskTensor = new DenseTensor<float>(new[] { 1, 1, TargetSize, TargetSize });

        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < TargetSize; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < TargetSize; x++)
                {
                    imgTensor[0, 0, y, x] = row[x].R / 255f;
                    imgTensor[0, 1, y, x] = row[x].G / 255f;
                    imgTensor[0, 2, y, x] = row[x].B / 255f;
                }
            }
        });

        mask.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < TargetSize; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < TargetSize; x++)
                {
                    maskTensor[0, 0, y, x] = row[x].R > 128 ? 1f : 0f;
                }
            }
        });

        return (imgTensor, maskTensor);
    }

    private Image<Rgba32> PasteResult(Tensor<float> tensor, Image<Rgba32> original, Rectangle crop)
    {
        var output = new Image<Rgba32>(TargetSize, TargetSize);

        for (int y = 0; y < TargetSize; y++)
        {
            for (int x = 0; x < TargetSize; x++)
            {
                output[x, y] = new Rgba32(
                    (byte)Math.Clamp(tensor[0, 0, y, x], 0, 255),
                    (byte)Math.Clamp(tensor[0, 1, y, x], 0, 255),
                    (byte)Math.Clamp(tensor[0, 2, y, x], 0, 255));
            }
        }

        var result = original.Clone();
        result.Mutate(x => x.DrawImage(output, crop.Location, 1f));

        return result;
    }

    public void Dispose() => _session?.Dispose();
}
