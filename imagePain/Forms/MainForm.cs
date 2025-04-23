// MainForm.cs
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using imagePain.Canvas;
using imagePain.Helpers;
using System;
using System.Drawing;
using System.Numerics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using imagePain.Utils;
using Color = SixLabors.ImageSharp.Color;
using PointF = SixLabors.ImageSharp.PointF;
using WinFormsPoint = System.Drawing.Point;
using SixLaborsColor = SixLabors.ImageSharp.Color;
using SixLaborsImage = SixLabors.ImageSharp.Image;

namespace imagePain.Forms
{
    public partial class MainForm : Form
    {
        private ImageCanvas _canvas;
        private BrushTool _brushTool;
        private ImageInpainter _inpainter;

        public MainForm()
        {
            InitializeComponents();
            BuildUI();
        }

        private void InitializeComponents()
        {
            _canvas = new ImageCanvas
            {
                Dock = DockStyle.Fill,
                BackColor = System.Drawing.Color.Black
            };
            _brushTool = new BrushTool();
            _inpainter = new ImageInpainter("Models\\lama_fp32.onnx");
        }

        private void BuildUI()
        {
            Text = "Image Inpainting Tool";
            Size = new System.Drawing.Size(1920, 1080);
            BackColor = System.Drawing.Color.FromArgb(45, 45, 45);
            ForeColor = System.Drawing.Color.White;


            _brushTool.BrushSizeChanged += size => _canvas.BrushSize = size;

            var toolPanel = UIHelper.CreateToolPanel(
                onLoad: LoadImage,
                onDrawToggle: () => _canvas.Cursor = _canvas.Cursor == Cursors.Cross ? Cursors.Default : Cursors.Cross,
                onBrushChange: size => _brushTool.BrushSize = size,
                onInpaint: ProcessImageAsync,
                onSave: SaveImage
            );

            Controls.Add(toolPanel);
            Controls.Add(_canvas);
        }


private async void LoadImage()
{
    using var dialog = new OpenFileDialog
    {
        Filter = "Images|*.jpg;*.jpeg;*.png;*.webp|All files|*.*"
    };

    if (dialog.ShowDialog() == DialogResult.OK)
    {
        try
        {
            var (original, current, mask) = await ImageLoader.LoadAsync<Rgba32>(dialog.FileName);
            _canvas.SetImages(original, current, mask);
            
            // Принудительное обновление
            _canvas.Refresh();
            Application.DoEvents();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}



        private async void ProcessImageAsync()
        {
            if (_canvas?.OriginalImage == null || _canvas?.Mask == null)
            {
                MessageBox.Show("Please load an image and create a mask first!",
                    "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                Cursor = Cursors.WaitCursor;

                var result = await Task.Run(() =>
                    _inpainter.Inpaint(
                        _canvas.OriginalImage.Clone(),
                        _canvas.Mask.Clone()
                    ));

                if (result != null)
                {
                    _canvas.UpdateCurrentImage(result);
                    MessageBox.Show("Inpainting completed successfully!",
                        "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Processing error: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void SaveImage()
        {
            if (!_canvas.HasImage) return;

            using var dialog = new SaveFileDialog
            {
                Filter = "PNG Image|*.png|JPEG Image|*.jpg|WebP Image|*.webp"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    _canvas.CurrentImage.Save(dialog.FileName);
                    MessageBox.Show("Image saved successfully!", "Info",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Save error: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            _canvas?.Dispose();
            _inpainter?.Dispose();
            base.Dispose(disposing);
        }
    }
}