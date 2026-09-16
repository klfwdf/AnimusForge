using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AnimusForge.Illustrator.Engine
{
    internal static class ImagePayload
    {
        internal const int MaxBytes = 24 * 1024 * 1024;
        internal const int MaxResponseBytes = 36 * 1024 * 1024;
        internal static byte[] Normalize(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 24 || bytes.Length > MaxBytes) throw new InvalidDataException("图片为空或超过24 MiB限制。");
            bool png = bytes[0] == 137 && bytes[1] == 80 && bytes[2] == 78 && bytes[3] == 71;
            bool jpeg = bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255;
            if (!png && !jpeg) throw new InvalidDataException("仅接受可解码的PNG或JPEG图片。");
            if (png)
            {
                long width = ((long)bytes[16] << 24) | ((long)bytes[17] << 16) | ((long)bytes[18] << 8) | bytes[19];
                long height = ((long)bytes[20] << 24) | ((long)bytes[21] << 16) | ((long)bytes[22] << 8) | bytes[23];
                ValidateDimensions(width, height);
            }
            else ValidateJpegHeader(bytes);
            using (var input = new MemoryStream(bytes, false))
            using (var image = Image.FromStream(input, false, true))
            {
                ValidateDimensions(image.Width, image.Height);
                using (var bitmap = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppArgb))
                using (var graphics = Graphics.FromImage(bitmap))
                using (var output = new MemoryStream())
                {
                    graphics.DrawImageUnscaled(image, 0, 0);
                    if (png) return bytes; // Already fully decoded; avoid repeated PNG recompression.
                    bitmap.Save(output, ImageFormat.Png);
                    if (output.Length > MaxBytes) throw new InvalidDataException("规范化图片超过24 MiB限制。");
                    return output.ToArray();
                }
            }
        }

        private static void ValidateJpegHeader(byte[] bytes)
        {
            int p = 2;
            while (p + 3 < bytes.Length)
            {
                if (bytes[p++] != 255) throw new InvalidDataException("Invalid JPEG marker.");
                while (p < bytes.Length && bytes[p] == 255) p++;
                if (p >= bytes.Length) break;
                int marker = bytes[p++];
                if (marker == 0xD9 || marker == 0xDA) break;
                if (marker == 0x01 || (marker >= 0xD0 && marker <= 0xD7)) continue;
                if (p + 1 >= bytes.Length) break;
                int length = (bytes[p] << 8) | bytes[p + 1];
                if (length < 2 || p + length > bytes.Length) throw new InvalidDataException("Truncated JPEG.");
                if (marker >= 0xC0 && marker <= 0xCF && marker != 0xC4 && marker != 0xC8 && marker != 0xCC)
                {
                    if (length < 8) throw new InvalidDataException("Invalid JPEG dimensions.");
                    ValidateDimensions((bytes[p + 5] << 8) | bytes[p + 6], (bytes[p + 3] << 8) | bytes[p + 4]);
                    return;
                }
                p += length;
            }
            throw new InvalidDataException("JPEG dimensions unavailable.");
        }

        internal static byte[] ReadFile(string path)
        {
            using (var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (input.Length > MaxBytes) throw new InvalidDataException("缓存图片超过大小限制。");
                byte[] bytes = new byte[(int)input.Length];
                int offset = 0;
                while (offset < bytes.Length)
                {
                    int read = input.Read(bytes, offset, bytes.Length - offset);
                    if (read == 0) throw new EndOfStreamException();
                    offset += read;
                }
                return Normalize(bytes);
            }
        }

        private static void ValidateDimensions(long width, long height)
        {
            if (width <= 0 || height <= 0 || width > 8192 || height > 8192 || width * height > 16777216)
                throw new InvalidDataException("图片尺寸超过8192边长或1600万像素限制。");
        }

        internal static async Task<byte[]> ReadBoundedAsync(HttpContent content, int limit, CancellationToken token)
        {
            if (content.Headers.ContentLength > limit) throw new InvalidDataException("服务端响应超过大小限制。");
            using (var stream = await content.ReadAsStreamAsync().ConfigureAwait(false))
            using (var output = new MemoryStream())
            {
                byte[] buffer = new byte[16384];
                int count;
                while ((count = await stream.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false)) != 0)
                {
                    if (output.Length + count > limit) throw new InvalidDataException("服务端响应超过大小限制。");
                    output.Write(buffer, 0, count);
                }
                token.ThrowIfCancellationRequested();
                return output.ToArray();
            }
        }
    }
}
