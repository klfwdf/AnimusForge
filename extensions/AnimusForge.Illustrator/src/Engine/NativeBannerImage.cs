using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace AnimusForge.Illustrator.Engine
{
    // Decode the native BannerTableau IView final-image export, never a normal
    // UI/cache PNG. The 2026-09-25 captured brown/orange banner arrived blue/cyan:
    // correct that producer's reversed R/B once, preserving all rendered geometry.
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
                    // Correct every pixel, including translucent edges. This is
                    // an observed native-export convention, not inferred from BGRA storage.
                    byte channel = pixels[i];
                    pixels[i] = pixels[i + 2];
                    pixels[i + 2] = channel;
                    if (pixels[i + 3] < 128) continue;
                    opaque++;
                    int red = pixels[i + 2];
                    int green = pixels[i + 1];
                    int blue = pixels[i];
                    if (firstR < 0) { firstR = red; firstG = green; firstB = blue; }
                    else if (Math.Abs(red - firstR) + Math.Abs(green - firstG) + Math.Abs(blue - firstB) > 24) distinct++;
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
