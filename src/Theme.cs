using System.Drawing;

namespace iOS6Clock
{
    internal sealed class ClockTheme
    {
        public bool Dark;
        public Color Top;
        public Color Bottom;
        public Color Border;
        public Color Text;
        public Color Link;
        public Color LinkHover;
        public Color Separator;
        public string Suffix;

        public static readonly ClockTheme Light = new ClockTheme
        {
            Dark = false,
            Top = Color.FromArgb(227, 227, 227),
            Bottom = Color.FromArgb(211, 211, 211),
            Border = Color.FromArgb(196, 196, 196),
            Text = Color.FromArgb(26, 26, 28),
            Link = Color.FromArgb(100, 100, 104),
            LinkHover = Color.FromArgb(45, 45, 49),
            Separator = Color.FromArgb(199, 199, 199),
            Suffix = ""
        };

        public static readonly ClockTheme DarkTheme = new ClockTheme
        {
            Dark = true,
            Top = Color.FromArgb(34, 34, 37),
            Bottom = Color.FromArgb(21, 21, 23),
            Border = Color.FromArgb(61, 61, 65),
            Text = Color.FromArgb(245, 245, 247),
            Link = Color.FromArgb(158, 158, 164),
            LinkHover = Color.FromArgb(225, 225, 230),
            Separator = Color.FromArgb(54, 54, 58),
            Suffix = "_dark"
        };

        public static ClockTheme Get(bool dark)
        {
            return dark ? DarkTheme : Light;
        }
    }
}
