using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace AnimusForge.Illustrator.Engine
{
    // Normalize an already rendered whole banner: no atlas, recoloring or geometry.
    internal static class NativeBannerImage
    {
        internal static string Encode(byte[] png, int maxDimension)
        {
            if (png == null || png.Length < 24 || png.Length > 16 * 1024 * 1024 ||
                png[0] != 137 || png[1] != 80 || png[2] != 78 || png[3] != 71) return null;
            try
            {
                using (var input = new MemoryStream(png, false))
                using (var source = new Bitmap(input))
                {
                    if (source.Width < 64 || source.Width != source.Height || source.Width > 4096) return null;
                    using (var bitmap = source.Clone(new Rectangle(0, 0, source.Width, source.Height), PixelFormat.Format32bppArgb))
                    {
                        if (!NormalizeAndValidate(bitmap)) return null;
                        int size = Math.Min(bitmap.Width, Math.Max(128, Math.Min(1024, maxDimension)));
                        using (var output = new MemoryStream())
                        {
                            if (size == bitmap.Width) bitmap.Save(output, ImageFormat.Png);
                            else
                            {
                                using (var resized = new Bitmap(size, size, PixelFormat.Format32bppArgb))
                                using (var g = Graphics.FromImage(resized))
                                {
                                    g.CompositingMode = CompositingMode.SourceCopy;
                                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                                    g.DrawImage(bitmap, 0, 0, size, size);
                                    resized.Save(output, ImageFormat.Png);
                                }
                            }
                            return Convert.ToBase64String(output.ToArray());
                        }
                    }
                }
            }
            catch (ArgumentException) { return null; }
            catch (ExternalException) { return null; }
        }

        private static bool NormalizeAndValidate(Bitmap bitmap)
        {
            var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            try
            {
                int length = data.Stride * bitmap.Height;
                byte[] pixels = new byte[length];
                Marshal.Copy(data.Scan0, pixels, 0, length);
                int opaque = 0, distinct = 0;
                int firstR = -1, firstG = 0, firstB = 0;
                for (int i = 0; i < length; i += 4)
                {
                    // Existing native TableauView exporter convention: swap once.
                    byte temp = pixels[i]; pixels[i] = pixels[i + 2]; pixels[i + 2] = temp;
                    if (pixels[i + 3] < 128) continue;
                    opaque++;
                    if (firstR < 0) { firstR = pixels[i + 2]; firstG = pixels[i + 1]; firstB = pixels[i]; }
                    else if (Math.Abs(pixels[i + 2] - firstR) + Math.Abs(pixels[i + 1] - firstG) + Math.Abs(pixels[i] - firstB) > 24) distinct++;
                }
                // Conservative gate: intentionally uniform flags are also omitted.
                if (opaque < bitmap.Width * bitmap.Height / 8 || distinct < Math.Max(16, bitmap.Width * bitmap.Height / 200)) return false;
                Marshal.Copy(pixels, 0, data.Scan0, length);
                return true;
            }
            finally { bitmap.UnlockBits(data); }
        }
    }
}
