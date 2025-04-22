using System;
using System.Numerics;
using System.Windows.Forms;

namespace imagePain.Canvas
{
    public class BrushTool
    {
        public event Action DrawingStarted;
        public event Action DrawingEnded;
        public event Action<int> BrushSizeChanged;
        public event Action<Vector2> DrawingContinued;
        private int _brushSize = 20;

        public int BrushSize
        {
            get => _brushSize;
            set
            {
                _brushSize = value;
                BrushSizeChanged?.Invoke(_brushSize);
            }
        }

        public void HandleMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                DrawingStarted?.Invoke();
        }

        public void HandleMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                DrawingEnded?.Invoke();
        }
    }
}