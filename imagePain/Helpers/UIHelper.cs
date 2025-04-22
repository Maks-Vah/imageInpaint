//Helpers\UIHelper
using System;
using System.Drawing;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;

namespace imagePain.Helpers
{
    public static class UIHelper
    {
        public static Button CreateToolButton(string text, Action onClick, bool isToggleButton = false)
        {
            var btn = new Button
            {
                Text = text,
                Width = 160,
                Height = 45,
                Margin = new Padding(10, 10, 10, 5),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 70, 70),
                ForeColor = Color.White,
                Tag = false // Для хранения состояния
            };
            btn.Click += (_, _) => {
                if (isToggleButton)
                {
                    btn.Tag = !(bool)btn.Tag;
                    btn.BackColor = (bool)btn.Tag
                        ? Color.FromArgb(90, 90, 140)
                        : Color.FromArgb(70, 70, 70);
                }
                onClick();
            };
            return btn;
        }

        public static TrackBar CreateBrushSlider(int initialValue, Action<int> onChange)
        {
            var slider = new TrackBar
            {
                Minimum = 5,
                Maximum = 100,
                Value = initialValue,
                Width = 160,
                Margin = new Padding(10, 0, 10, 20)
            };
            slider.ValueChanged += (_, _) => onChange(slider.Value);
            return slider;
        }

        public static FlowLayoutPanel CreateToolPanel(
            Action onLoad, Action onDrawToggle, Action<int> onBrushChange, Action onInpaint, Action onSave)
        {
            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                Width = 180,
                BackColor = Color.FromArgb(60, 60, 60),
                FlowDirection = FlowDirection.TopDown,
                AutoScroll = true
            };

            panel.Controls.Add(CreateToolButton("🖼 Загрузить", onLoad));
            panel.Controls.Add(CreateToolButton("✏️ Рисование", onDrawToggle));

            panel.Controls.Add(new Label
            {
                Text = "Размер кисти:",
                ForeColor = Color.White,
                Margin = new Padding(10, 20, 5, 5),
                Width = 160
            });

            panel.Controls.Add(CreateBrushSlider(20, onBrushChange));
            panel.Controls.Add(CreateToolButton("✨ Заливка", onInpaint));
            panel.Controls.Add(CreateToolButton("💾 Сохранить", onSave));

            return panel;
        }
    }
}
