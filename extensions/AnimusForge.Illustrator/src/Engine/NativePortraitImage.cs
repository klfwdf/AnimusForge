using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;

namespace AnimusForge.Illustrator.Engine
{
    // Only for CharacterTableau's IView.SetSaveFinalResultToDisk PNG producer.
    // The 2026-09-25 field-conversation capture shows reversed R/B in both
    // full-body and head exports (brown/gold clothing became blue/cyan).
    // This is the same native exporter used by PanoramaProjection; ordinary
    // screenshots, banner images, HTTP results and UI/cache PNGs do not enter here.
    internal static class NativePortraitImage
    {
        internal static Bitmap DecodeFinalRenderPng(byte[] png, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (png == null || png.Length < 33 || png.Length > 16 * 1024 * 1024 ||
                png[0] != 137 || png[1] != 80 || png[2] != 78 || png[3] != 71 ||
                png[4] != 13 || png[5] != 10 || png[6] != 26 || png[7] != 10 ||
                png[8] != 0 || png[9] != 0 || png[10] != 0 || png[11] != 13 ||
                png[12] != 73 || png[13] != 72 || png[14] != 68 || png[15] != 82)
                throw new InvalidDataException("Native portrait export is not a bounded PNG.");
            uint width = ReadDimension(png, 16), height = ReadDimension(png, 20);
            if (width == 0 || height == 0 || width > 2048 || height > 2048)
                throw new InvalidDataException("Native portrait export dimensions exceed 2048 pixels per side.");

            Bitmap result;
            using (var stream = new MemoryStream(png, false))
            using (var decoded = new Bitmap(stream))
            {
                if (decoded.Width != width || decoded.Height != height)
                    throw new InvalidDataException("Native portrait dimensions disagree with its header.");
                result = decoded.Clone(new Rectangle(0, 0, decoded.Width, decoded.Height), PixelFormat.Format32bppArgb);
            }
            try
            {
                // One linear pass per requested portrait on the existing worker path.
                // Row-based addressing supports padding and either stride direction.
                int pixelWidth = result.Width, pixelHeight = result.Height;
                var data = result.LockBits(new Rectangle(0, 0, pixelWidth, pixelHeight),
                    ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                try
                {
                    unsafe
                    {
                        for (int y = 0; y < pixelHeight; y++)
                        {
                            if ((y & 31) == 0) token.ThrowIfCancellationRequested();
                            byte* row = (byte*)data.Scan0 + y * data.Stride;
                            for (int x = 0; x < pixelWidth; x++)
                            {
                                byte* pixel = row + x * 4;
                                byte channel = pixel[0];
                                pixel[0] = pixel[2];
                                pixel[2] = channel;
                            }
                        }
                    }
                }
                finally { result.UnlockBits(data); }
                token.ThrowIfCancellationRequested();
                return result;
            }
            catch { result.Dispose(); throw; }
        }

        private static uint ReadDimension(byte[] bytes, int offset) =>
            ((uint)bytes[offset] << 24) | ((uint)bytes[offset + 1] << 16) |
            ((uint)bytes[offset + 2] << 8) | bytes[offset + 3];
    }
}
