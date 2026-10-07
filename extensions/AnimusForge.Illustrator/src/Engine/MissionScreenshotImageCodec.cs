using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace AnimusForge.Illustrator.Engine
{
    internal static class MissionScreenshotImageCodec
    {
        internal const int MaximumRawBytes = 96 * 1024 * 1024;
        internal const int MaximumPixels = 24 * 1024 * 1024;
        internal static async Task<byte[]> ReadCompleteAsync(string path, CancellationToken token)
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();
                try
                {
                    // Exclusive read waits for the native writer to close, not merely for file creation.
                    using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        if (file.Length > MaximumRawBytes) throw new InvalidDataException("原生截图超过96 MiB采集上限。");
                        if (file.Length >= 54)
                        {
                            byte[] bytes = new byte[checked((int)file.Length)];
                            int offset = 0;
                            while (offset < bytes.Length)
                            {
                                token.ThrowIfCancellationRequested();
                                int read = await file.ReadAsync(bytes, offset, bytes.Length - offset, token).ConfigureAwait(false);
                                if (read == 0) throw new EndOfStreamException();
                                offset += read;
                            }
                            ValidateBitmap(bytes);
                            return bytes;
                        }
                    }
                }
                catch (FileNotFoundException) { }
                catch (IOException) { }
                await Task.Delay(40, token).ConfigureAwait(false);
            }
        }

        internal static void ValidateBitmap(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 54 || bytes.Length > MaximumRawBytes || bytes[0] != 'B' || bytes[1] != 'M')
                throw new InvalidDataException("原生截图不是完整有效的 BMP。");
            int declared = BitConverter.ToInt32(bytes, 2), pixelOffset = BitConverter.ToInt32(bytes, 10);
            int header = BitConverter.ToInt32(bytes, 14), width = BitConverter.ToInt32(bytes, 18), height = BitConverter.ToInt32(bytes, 22);
            int bits = BitConverter.ToUInt16(bytes, 28), compression = BitConverter.ToInt32(bytes, 30);
            if (height == int.MinValue) throw new InvalidDataException("截图高度无效。");
            ValidateDimensions(width, Math.Abs(height));
            long row = ((long)width * bits + 31) / 32 * 4;
            if (declared != bytes.Length || header < 40 || pixelOffset < 14L + header || pixelOffset > bytes.Length
                || BitConverter.ToUInt16(bytes, 26) != 1 || (bits != 24 && bits != 32) || (compression != 0 && compression != 3)
                || pixelOffset + row * Math.Abs(height) > bytes.Length)
                throw new InvalidDataException("原生 BMP 尚不完整或像素格式不受支持。");
        }

        private static void ValidateDimensions(int width, int height)
        {
            if (width <= 0 || height <= 0 || (long)width * height > MaximumPixels)
                throw new InvalidDataException("截图像素尺寸无效或超过24百万像素上限。");
        }

        internal static byte[] ToPng(byte[] bytes, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            ValidateBitmap(bytes);
            using (var input = new MemoryStream(bytes, false))
            using (var original = Image.FromStream(input, false, true))
            {
                ValidateVisibleContent(original);
                double scale = Math.Min(1.0, 2048.0 / Math.Max(original.Width, original.Height));
                using (var bitmap = new Bitmap(Math.Max(1, (int)Math.Round(original.Width * scale)), Math.Max(1, (int)Math.Round(original.Height * scale)), PixelFormat.Format32bppArgb))
                using (var graphics = Graphics.FromImage(bitmap))
                using (var output = new MemoryStream())
                {
                    graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                    graphics.DrawImage(original, new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                        0, 0, original.Width, original.Height, GraphicsUnit.Pixel);
                    token.ThrowIfCancellationRequested();
                    bitmap.Save(output, ImageFormat.Png);
                    return ImagePayload.Normalize(output.ToArray());
                }
            }
        }

        internal static bool SameImage(byte[] first, byte[] second)
        {
            using (var hash = SHA256.Create())
                return Convert.ToBase64String(hash.ComputeHash(first)) == Convert.ToBase64String(hash.ComputeHash(second));
        }

        // Fixed 96x54 sample, worker only. Reject near-black exports without brightening them.
        internal static void ValidateVisibleContent(Image image)
        {
            using (var sample = new Bitmap(96, 54, PixelFormat.Format24bppRgb))
            using (var graphics = Graphics.FromImage(sample))
            {
                graphics.DrawImage(image, new Rectangle(0, 0, sample.Width, sample.Height));
                int visible = 0;
                for (int y = 0; y < sample.Height; y++)
                    for (int x = 0; x < sample.Width; x++)
                    {
                        Color color = sample.GetPixel(x, y);
                        if (Math.Max(color.R, Math.Max(color.G, color.B)) > 8) visible++;
                    }
                Core.GenerationDiagnostics.Current?.RecordStage("mission_screenshot_visibility", new Newtonsoft.Json.Linq.JObject {
                    ["samplePixels"] = sample.Width * sample.Height, ["visiblePixels"] = visible,
                    ["minimumVisiblePercent"] = 1, ["gpu_frame_identity_verified"] = false });
                if (visible * 100 < sample.Width * sample.Height)
                    throw new InvalidDataException("现场截图几乎全黑，无法识别人物与环境；已停止生成，未发送导演或生图请求。");
            }
        }
    }
}
