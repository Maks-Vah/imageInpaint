using System;
using System.Drawing;
using System.Windows.Forms;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Drawing;
using System.Numerics;
using Color = SixLabors.ImageSharp.Color;
using PointF = SixLabors.ImageSharp.PointF;
using Point = System.Drawing.Point;
using DrawingImage = System.Drawing.Image;
using System.Diagnostics;
using System.IO;

namespace imagePain.Canvas
{
    public class ImageCanvas : PictureBox
    {
        private Timer _refreshTimer;
        private bool _refreshNeeded = false;
        private Image<Rgba32> _originalImage;
        private Image<Rgba32> _currentImage;
        private Image<Rgba32> _mask;
        private bool _isDrawing;
        private Vector2 _lastPoint;
        private int _brushSize = 20;
        private int _drawCounter = 0;
        public int BrushSize
        {
            get => _brushSize;
            set => _brushSize = Math.Clamp(value, 1, 100);
        }



public Image<Rgba32> OriginalImage => _originalImage?.Clone();
        public Image<Rgba32> CurrentImage => _currentImage?.Clone();
        public Image<Rgba32> Mask => _mask?.Clone();
        public bool HasImage => _originalImage != null;

        public ImageCanvas()
        {
            MouseDown += OnMouseDown;
            MouseMove += OnMouseMove;
            MouseUp += OnMouseUp;
            DoubleBuffered = true;
            Cursor = Cursors.Cross;
            _refreshTimer = new Timer();
            _refreshTimer.Interval = 20; // 50 FPS
            _refreshTimer.Tick += (s, e) =>
            {
                if (_refreshNeeded)
                {
                    _refreshNeeded = false;
                    UpdatePreview();
                }
            };
            _refreshTimer.Start();
        }

        public void SetImages(Image<Rgba32> original, Image<Rgba32> current, Image<Rgba32> mask)
        {
            // Освобождаем старые ресурсы
            _originalImage?.Dispose();
            _currentImage?.Dispose();
            _mask?.Dispose();

            // Клонируем новые изображения
            _originalImage = original.Clone();
            _currentImage = current.Clone();
            _mask = mask.Clone() ?? new Image<Rgba32>(original.Width, original.Height, Color.Black); ;

            // Устанавливаем размер контрола
            this.Size = new System.Drawing.Size(original.Width, original.Height);

            UpdatePreview();
        }

        public void UpdateCurrentImage(Image<Rgba32> image)
        {
            _currentImage?.Dispose();
            _currentImage = image.Clone();
            UpdatePreview();
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && _originalImage != null)
            {
                _isDrawing = true;
                _lastPoint = TranslatePoint(e.Location);
                DrawDot(_lastPoint);
            }
        }

        private void DrawDot(Vector2 point)
        {
            if (_mask == null || _originalImage == null) return;

            try
            {
                // Проверяем и корректируем координаты
                float x = Math.Clamp(point.X, _brushSize / 2f, _originalImage.Width - _brushSize / 2f);
                float y = Math.Clamp(point.Y, _brushSize / 2f, _originalImage.Height - _brushSize / 2f);
                float radius = Math.Max(1, _brushSize / 2f); // Минимальный радиус 1

                using (var image = _mask.Clone())
                {
                    image.Mutate(ctx => ctx
                        .Fill(
                            SixLabors.ImageSharp.Color.White,
                            new EllipsePolygon(
                                new PointF(x, y), // Используем скорректированные координаты
                                radius
                            )
                        ));

                    // Безопасное присваивание с проверкой размеров
                    if (image.Width == _originalImage.Width && image.Height == _originalImage.Height)
                    {
                        _mask?.Dispose();
                        _mask = image.Clone();
                    }
                    else
                    {
                        Debug.WriteLine("Ошибка: размеры маски не совпадают с оригинальным изображением");
                    }
                }

                UpdatePreview();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка в DrawDot: {ex.Message}");
                // Восстанавливаем маску в случае ошибки
                _mask = new Image<Rgba32>(_originalImage.Width, _originalImage.Height);
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDrawing && e.Button == MouseButtons.Left)
            {
                var currentPoint = TranslatePoint(e.Location);
                DrawToMask(currentPoint);
                _lastPoint = currentPoint;
            }
        }

        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            if (_isDrawing)
            {
                _isDrawing = false;

                // Финализируем последние изменения
                if (_tempMask != null)
                {
                    _mask?.Dispose();
                    _mask = _tempMask.Clone();
                    _tempMask.Dispose();
                    _tempMask = null;
                    UpdatePreview();
                }
            }
        }

        private Vector2 TranslatePoint(Point point)
        {
            if (Image == null || _originalImage == null)
                return Vector2.Zero;

            float scaleX = (float)_originalImage.Width / Image.Width;
            float scaleY = (float)_originalImage.Height / Image.Height;

            return new Vector2(
                point.X * scaleX,
                point.Y * scaleY
            );
        }

        private Image<Rgba32> _tempMask;

        private void DrawToMask(Vector2 point)
        {
            if (_mask == null || _originalImage == null) return;

            try
            {

                var start = new PointF(Math.Clamp(_lastPoint.X, 0, _originalImage.Width - 1),
        Math.Clamp(_lastPoint.Y, 0, _originalImage.Height - 1));
            var end = new PointF(Math.Clamp(point.X, 0, _originalImage.Width - 1),
        Math.Clamp(point.Y, 0, _originalImage.Height - 1));

            // Используем временный буфер для рисования
            if (_tempMask == null)
            {
                _tempMask = _mask.Clone();
            }

            _tempMask.Mutate(ctx => ctx
                .DrawLine(SixLabors.ImageSharp.Color.White,
                _brushSize,
                start,
                end));

            // Обновляем основную маску каждые 10 операций
            if (_drawCounter % 5 == 0)
            {
                _mask?.Dispose();
                _mask = _tempMask.Clone();
                _tempMask.Dispose();
                _tempMask = null;
                UpdatePreview();
            }
            _lastPoint = point;
                _refreshNeeded = true; // Флаг для отложенного обновления
            }
    catch (Exception ex)
    {
        Debug.WriteLine($"Ошибка рисования: {ex}");
        _isDrawing = false;
        
        // Восстановление состояния при ошибке
        _tempMask?.Dispose();
        _tempMask = null;
    }
}

        private void UpdatePreview()
        {
            if (_currentImage == null || _mask == null) return;

            try
            {
                using var combinedImage = _currentImage.Clone();
                combinedImage.Mutate(ctx => ctx.DrawImage(_mask, 0.5f));

                var finalImage = ConvertImageToBitmap(combinedImage);

                this.Invoke((MethodInvoker)(() =>
                {
                    if (this.Image != null)
                    {
                        this.Image.Dispose();
                    }
                    this.Image = finalImage;
                }));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка в UpdatePreview: {ex}");
            }
        }

        private DrawingImage ConvertToDrawingImage(Image<Rgba32> image)
        {
            using var ms = new System.IO.MemoryStream();
            image.SaveAsPng(ms);
            ms.Position = 0;
            return DrawingImage.FromStream(ms);
        }

        private Bitmap ConvertImageToBitmap(Image<Rgba32> image)
        {
            var bitmap = new Bitmap(image.Width, image.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            try
            {
                var rect = new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height);
                var bitmapData = bitmap.LockBits(rect, System.Drawing.Imaging.ImageLockMode.WriteOnly, bitmap.PixelFormat);

                image.ProcessPixelRows(accessor =>
                {
                    for (int y = 0; y < accessor.Height; y++)
                    {
                        var row = accessor.GetRowSpan(y);
                        unsafe
                        {
                            byte* destPtr = (byte*)bitmapData.Scan0 + y * bitmapData.Stride;
                            for (int x = 0; x < accessor.Width; x++)
                            {
                                // Правильный порядок: ARGB (System.Drawing) <- RGBA (ImageSharp)
                                destPtr[x * 4 + 0] = row[x].B;     // Синий
                                destPtr[x * 4 + 1] = row[x].G;     // Зеленый
                                destPtr[x * 4 + 2] = row[x].R;     // Красный
                                destPtr[x * 4 + 3] = row[x].A;     // Альфа
                            }
                        }
                    }
                });

                bitmap.UnlockBits(bitmapData);
                return bitmap;
            }
            catch
            {
                bitmap.Dispose();
                throw;
            }
        }

        public void Clear()
        {
            _originalImage?.Dispose();
            _currentImage?.Dispose();
            _mask?.Dispose();

            _originalImage = null;
            _currentImage = null;
            _mask = null;

            this.Image?.Dispose();
            this.Image = null;
            this.Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            _originalImage?.Dispose();
            _currentImage?.Dispose();
            _mask?.Dispose();
            base.Dispose(disposing);
        }
    }
}