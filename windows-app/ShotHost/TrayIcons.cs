using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace ShotHost
{
    /// Значки трея по состоянию, как в строке меню на Mac: обычный — слежу за папкой,
    /// бледный — пауза или слежение не идёт, бледная рамка с яркой стрелкой — идёт загрузка.
    static class TrayIcons
    {
        public static readonly Icon Watching;
        public static readonly Icon Idle;
        public static readonly Icon Uploading;

        static TrayIcons()
        {
            var size = SystemInformation.SmallIconSize;
            Watching = TrayApp.LoadIcon(size);
            using (var source = Watching.ToBitmap())
            {
                Idle = Draw(source, uploading: false);
                Uploading = Draw(source, uploading: true);
            }
        }

        static Icon Draw(Bitmap source, bool uploading)
        {
            // Значков всего три и живут они всё время работы, поэтому HICON не освобождаем.
            using (var bitmap = uploading ? DimFrame(source) : Faded(source))
                return Icon.FromHandle(bitmap.GetHicon());
        }

        static Bitmap Faded(Bitmap source)
        {
            var bitmap = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bitmap))
            using (var attributes = new ImageAttributes())
            {
                attributes.SetColorMatrix(new ColorMatrix { Matrix33 = 0.4f });
                g.DrawImage(source, new Rectangle(0, 0, source.Width, source.Height),
                    0, 0, source.Width, source.Height, GraphicsUnit.Pixel, attributes);
            }
            return bitmap;
        }

        /// Белая рамка уходит в синий фон иконки, стрелка в середине остаётся яркой.
        static Bitmap DimFrame(Bitmap source)
        {
            var bitmap = new Bitmap(source);
            var arrow = Rectangle.Round(new RectangleF(source.Width * 0.33f, source.Height * 0.3f,
                source.Width * 0.34f, source.Height * 0.42f));
            // Цвет фона между уголками рамки: синий у релиза, серый у сборки разработки.
            var blue = source.GetPixel((int)(source.Width * 0.2f), source.Height / 2);
            for (var y = 0; y < bitmap.Height; y++)
            {
                for (var x = 0; x < bitmap.Width; x++)
                {
                    if (arrow.Contains(x, y)) continue;
                    var c = bitmap.GetPixel(x, y);
                    var white = Math.Min(c.R, Math.Min(c.G, c.B)) / 255f;
                    if (white <= 0.5f) continue;
                    var k = 0.7f * (white - 0.5f) / 0.5f;
                    bitmap.SetPixel(x, y, Color.FromArgb(c.A,
                        (int)(c.R * (1 - k) + blue.R * k), (int)(c.G * (1 - k) + blue.G * k), (int)(c.B * (1 - k) + blue.B * k)));
                }
            }
            return bitmap;
        }
    }
}
