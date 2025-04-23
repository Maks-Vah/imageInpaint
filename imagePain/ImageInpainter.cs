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
        var options = new SessionOptions
        {
            GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL
        };

        try { options.AppendExecutionProvider_CUDA(); }
        catch { options.AppendExecutionProvider_CPU(); }

        _session = new InferenceSession(modelPath, options);
    }

    public Image<Rgba32> Inpaint(Image<Rgba32> image, Image<Rgba32> mask)
    {
        var (preparedImage, preparedMask) = PrepareInputs(image, mask);
        var (inputTensor, maskTensor) = CreateTensors(preparedImage, preparedMask);

        // Сохраняем промежуточные данные для отладки
        preparedImage.SaveAsPng("debug_input.png");
        preparedMask.SaveAsPng("debug_mask.png");

        using var results = _session.Run(new[]
        {
            NamedOnnxValue.CreateFromTensor("image", inputTensor),
            NamedOnnxValue.CreateFromTensor("mask", maskTensor)
        });

        return ProcessOutput(results[0].AsTensor<float>(), image, mask);
    }

    private (Image<Rgba32>, Image<Rgba32>) PrepareInputs(Image<Rgba32> image, Image<Rgba32> mask)
    {
        // 1. Подготовка изображения с паддингом
        var processedImage = image.Clone();
        processedImage.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(TargetSize),
            Mode = ResizeMode.Pad,
            PadColor = Color.Black,
            Sampler = KnownResamplers.Lanczos3
        }));

        // 2. Подготовка маски (бинаризация + ресайз)
        var processedMask = mask.Clone();
        processedMask.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < row.Length; x++)
                {
                    row[x] = row[x].R > 128 ? Color.White : Color.Black;
                }
            }
        });

        processedMask.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(TargetSize),
            Mode = ResizeMode.Pad,
            PadColor = Color.Black,
            Sampler = KnownResamplers.NearestNeighbor
        }));

        return (processedImage, processedMask);
    }

    private (DenseTensor<float>, DenseTensor<float>) CreateTensors(Image<Rgba32> image, Image<Rgba32> mask)
    {
        var imageTensor = new DenseTensor<float>(new[] { 1, 3, TargetSize, TargetSize });
        var maskTensor = new DenseTensor<float>(new[] { 1, 1, TargetSize, TargetSize });

        // Заполнение тензора изображения (RGB порядок, нормализация [0, 1])
        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < TargetSize; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < TargetSize; x++)
                {
                    imageTensor[0, 0, y, x] = row[x].R / 255f; // R
                    imageTensor[0, 1, y, x] = row[x].G / 255f; // G
                    imageTensor[0, 2, y, x] = row[x].B / 255f; // B
                }
            }
        });

        // Заполнение тензора маски (1.0 для областей восстановления)
        mask.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < TargetSize; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < TargetSize; x++)
                {
                    maskTensor[0, 0, y, x] = row[x].R > 128 ? 1.0f : 0.0f;
                }
            }
        });

        return (imageTensor, maskTensor);
    }

    private Image<Rgba32> ProcessOutput(Tensor<float> tensor, Image<Rgba32> original, Image<Rgba32> mask)
    {
        var output = new Image<Rgba32>(TargetSize, TargetSize);

        // Конвертация тензора в изображение
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

        // Возврат к исходному размеру
        output.Mutate(x => x.Resize(original.Width, original.Height));

        // Смешивание с оригиналом через маску
        var result = original.Clone();
        result.Mutate(ctx => ctx.DrawImage(output, new Point(0, 0), new GraphicsOptions
        {
            AlphaCompositionMode = PixelAlphaCompositionMode.SrcOver,
            ColorBlendingMode = PixelColorBlendingMode.Normal
        }));

        result.SaveAsPng("debug_final.png");
        return result;
    }

    public void Dispose() => _session?.Dispose();
}