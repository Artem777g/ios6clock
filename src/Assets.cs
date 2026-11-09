using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;

namespace iOS6Clock
{
    internal static class Assets
    {
        static readonly Dictionary<string, Image> Originals = new Dictionary<string, Image>();
        static readonly Dictionary<string, Bitmap> Scaled = new Dictionary<string, Bitmap>();

        public static Image Get(string name)
        {
            lock (Originals)
            {
                Image img;
                if (Originals.TryGetValue(name, out img)) return img;

                Assembly asm = Assembly.GetExecutingAssembly();
                using (Stream s = asm.GetManifestResourceStream(name))
                {
                    if (s == null) throw new InvalidOperationException("resource " + name + " not found");
                    using (var ms = new MemoryStream())
                    {
                        s.CopyTo(ms);
                        ms.Position = 0;
                        using (var tmp = Image.FromStream(ms))
                            img = new Bitmap(tmp);
                    }
                }
                Originals[name] = img;
                return img;
            }
        }

        public static Bitmap Get(string name, int size)
        {
            string key = name + "@" + size;
            lock (Scaled)
            {
                Bitmap bmp;
                if (Scaled.TryGetValue(key, out bmp)) return bmp;

                Image src = Get(name);
                bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.CompositingMode = CompositingMode.SourceOver;
                    g.CompositingQuality = CompositingQuality.HighQuality;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.DrawImage(src, new Rectangle(0, 0, size, size));
                }
                Scaled[key] = bmp;
                return bmp;
            }
        }

        public static void ClearScaled()
        {
            lock (Scaled)
            {
                foreach (Bitmap b in Scaled.Values) b.Dispose();
                Scaled.Clear();
            }
        }
    }

    internal static class Fonts
    {
        public static bool HasLight { get; private set; }

        static Fonts()
        {
            using (var f = new Font("Segoe UI Light", 10f))
                HasLight = f.Name == "Segoe UI Light";
        }

        public static Font Light(float pt)
        {
            return HasLight ? new Font("Segoe UI Light", pt, FontStyle.Regular, GraphicsUnit.Point)
                            : new Font("Segoe UI", pt, FontStyle.Regular, GraphicsUnit.Point);
        }

        public static Font Regular(float pt)
        {
            return new Font("Segoe UI", pt, FontStyle.Regular, GraphicsUnit.Point);
        }
    }

    internal static class IconFactory
    {
        public static Icon Create(int size)
        {
            using (var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb))
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.Clear(Color.Transparent);

                    float m = size * 0.045f;
                    var r = new RectangleF(m, m, size - 2 * m, size - 2 * m);
                    using (var b = new SolidBrush(Color.White)) g.FillEllipse(b, r);
                    using (var p = new Pen(Color.FromArgb(24, 24, 24), Math.Max(1f, size * 0.05f)))
                        g.DrawEllipse(p, r);

                    var center = new PointF(size / 2f, size / 2f);
                    float radius = size * 0.5f - m;
                    using (var pen = new Pen(Color.FromArgb(20, 20, 20), Math.Max(1.2f, size * 0.075f)))
                    {
                        for (int i = 0; i < 12; i++)
                        {
                            double a = i * Math.PI / 6.0;
                            var p1 = new PointF(center.X + (float)(Math.Sin(a) * radius * 0.72), center.Y - (float)(Math.Cos(a) * radius * 0.72));
                            var p2 = new PointF(center.X + (float)(Math.Sin(a) * radius * 0.90), center.Y - (float)(Math.Cos(a) * radius * 0.90));
                            g.DrawLine(pen, p1, p2);
                        }
                    }

                    using (var pen = new Pen(Color.FromArgb(24, 24, 24), Math.Max(1.5f, size * 0.09f)))
                    {
                        pen.StartCap = LineCap.Round;
                        pen.EndCap = LineCap.Round;
                        g.DrawLine(pen, center, PointOn(center, radius * 0.55f, 300.0));
                        g.DrawLine(pen, center, PointOn(center, radius * 0.78f, 60.0));
                    }
                    using (var pen = new Pen(Color.FromArgb(210, 30, 30), Math.Max(1f, size * 0.05f)))
                        g.DrawLine(pen, center, PointOn(center, radius * 0.66f, 210.0));
                }

                IntPtr h = bmp.GetHicon();
                try
                {
                    using (var tmp = Icon.FromHandle(h))
                        return (Icon)tmp.Clone();
                }
                finally
                {
                    Native.DestroyIcon(h);
                }
            }
        }

        static PointF PointOn(PointF c, float r, double deg)
        {
            double a = deg * Math.PI / 180.0;
            return new PointF(c.X + (float)(Math.Sin(a) * r), c.Y - (float)(Math.Cos(a) * r));
        }
    }
}
