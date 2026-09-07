using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Kbg.NppPluginNET.PluginInfrastructure;

namespace NppTranslatePanel.Utils
{
    /// <summary>
    /// Owns the native image handles used by the Notepad++ toolbar. The simple
    /// A-and-arrow glyph stays recognizable in standard, Fluent, and dark modes.
    /// </summary>
    internal sealed class ToolbarIconSet : IDisposable
    {
        private IntPtr bitmapHandle;
        private IntPtr lightIconHandle;
        private IntPtr darkIconHandle;
        private bool disposed;

        public ToolbarIconSet()
        {
            using (Bitmap standard = DrawGlyph(
                Color.FromArgb(0, 102, 204), Color.FromArgb(0, 140, 80),
                Color.FromArgb(192, 192, 192)))
            {
                bitmapHandle = standard.GetHbitmap();
            }

            lightIconHandle = CreateIconHandle(Color.Black);
            darkIconHandle = CreateIconHandle(Color.White);
        }

        public toolbarIconsWithDarkMode Handles => new toolbarIconsWithDarkMode
        {
            hToolbarBmp = bitmapHandle,
            hToolbarIcon = lightIconHandle,
            hToolbarIconDarkMode = darkIconHandle
        };

        private static IntPtr CreateIconHandle(Color foreground)
        {
            using (Bitmap bitmap = DrawGlyph(foreground, foreground, Color.Transparent))
                return bitmap.GetHicon();
        }

        private static Bitmap DrawGlyph(Color letterColor, Color arrowColor, Color background)
        {
            var bitmap = new Bitmap(16, 16, PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            using (var letterPen = new Pen(letterColor, 1.7f))
            using (var arrowPen = new Pen(arrowColor, 1.7f))
            {
                graphics.Clear(background);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                letterPen.StartCap = LineCap.Round;
                letterPen.EndCap = LineCap.Round;
                arrowPen.StartCap = LineCap.Round;
                arrowPen.EndCap = LineCap.Round;
                arrowPen.LineJoin = LineJoin.Round;

                // A
                graphics.DrawLine(letterPen, 1.5f, 13.5f, 5.0f, 2.5f);
                graphics.DrawLine(letterPen, 5.0f, 2.5f, 8.0f, 13.5f);
                graphics.DrawLine(letterPen, 2.8f, 9.2f, 6.8f, 9.2f);

                // Right arrow
                graphics.DrawLine(arrowPen, 8.5f, 8.0f, 14.5f, 8.0f);
                graphics.DrawLine(arrowPen, 11.5f, 5.0f, 14.5f, 8.0f);
                graphics.DrawLine(arrowPen, 14.5f, 8.0f, 11.5f, 11.0f);
            }
            return bitmap;
        }

        public void Dispose()
        {
            if (disposed)
                return;

            if (bitmapHandle != IntPtr.Zero)
                DeleteObject(bitmapHandle);
            if (lightIconHandle != IntPtr.Zero)
                DestroyIcon(lightIconHandle);
            if (darkIconHandle != IntPtr.Zero)
                DestroyIcon(darkIconHandle);

            bitmapHandle = IntPtr.Zero;
            lightIconHandle = IntPtr.Zero;
            darkIconHandle = IntPtr.Zero;
            disposed = true;
        }

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr objectHandle);

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr iconHandle);
    }
}
