using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace AnimusForge.Illustrator.Engine
{
    // At most two bounded CPU projections after direction, on the generation worker. Input is
    // the already colour-corrected panorama, never an uncorrected native face.
    internal static class ScenePerspectiveProjection
    {
        internal static byte[] Project(byte[] panorama, double yawDegrees, double pitchDegrees,
            double horizontalFovDegrees, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            ValidateView(yawDegrees, pitchDegrees, horizontalFovDegrees);
            int width, height;
            byte[] source = DecodePanorama(panorama, token, out width, out height);
            return ProjectPixels(source, width, height, yawDegrees, pitchDegrees, horizontalFovDegrees, token);
        }

        internal static byte[][] ProjectPair(byte[] panorama, double yawDegrees, double pitchDegrees,
            double horizontalFovDegrees, double auxiliaryYawDegrees, double auxiliaryPitchDegrees,
            double auxiliaryHorizontalFovDegrees, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            ValidateView(yawDegrees, pitchDegrees, horizontalFovDegrees);
            ValidateView(auxiliaryYawDegrees, auxiliaryPitchDegrees, auxiliaryHorizontalFovDegrees);
            // The panorama is the largest allocation; decode it once and reuse the
            // pixels for both views. No extra capture, camera work or director call.
            int width, height;
            byte[] source = DecodePanorama(panorama, token, out width, out height);
            return new[]
            {
                ProjectPixels(source, width, height, yawDegrees, pitchDegrees, horizontalFovDegrees, token),
                ProjectPixels(source, width, height, auxiliaryYawDegrees, auxiliaryPitchDegrees, auxiliaryHorizontalFovDegrees, token)
            };
        }

        private static void ValidateView(double yawDegrees, double pitchDegrees, double horizontalFovDegrees)
        {
            if (!Finite(yawDegrees) || yawDegrees < -180 || yawDegrees > 180 ||
                !Finite(pitchDegrees) || pitchDegrees < -60 || pitchDegrees > 60 ||
                !Finite(horizontalFovDegrees) || horizontalFovDegrees < 45 || horizontalFovDegrees > 100)
                throw new ArgumentOutOfRangeException(nameof(yawDegrees), "Invalid scene reference view.");
        }

        private static byte[] DecodePanorama(byte[] panorama, CancellationToken token, out int width, out int height)
        {
            if (panorama == null || panorama.Length < 33 || panorama.Length > 16 * 1024 * 1024 ||
                panorama[0] != 137 || panorama[1] != 80 || panorama[2] != 78 || panorama[3] != 71 ||
                panorama[4] != 13 || panorama[5] != 10 || panorama[6] != 26 || panorama[7] != 10 ||
                panorama[12] != 73 || panorama[13] != 72 || panorama[14] != 68 || panorama[15] != 82)
                throw new InvalidDataException("Scene perspective requires a bounded panorama PNG.");
            uint widthHeader = ReadBigEndian(panorama, 16), heightHeader = ReadBigEndian(panorama, 20);
            if (heightHeader < 2 || heightHeader > 2048 || widthHeader != heightHeader * 2)
                throw new InvalidDataException("Scene panorama must be 2:1 and at most 4096 x 2048.");
            width = (int)widthHeader;
            height = (int)heightHeader;
            byte[] source = new byte[checked(width * height * 4)];
            using (var input = new MemoryStream(panorama, false))
            using (var bitmap = new Bitmap(input))
            {
                if (bitmap.Width != width || bitmap.Height != height)
                    throw new InvalidDataException("Scene panorama dimensions disagree with its header.");
                var data = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                try
                {
                    for (int row = 0; row < height; row++)
                    {
                        token.ThrowIfCancellationRequested();
                        Marshal.Copy(IntPtr.Add(data.Scan0, row * data.Stride), source, row * width * 4, width * 4);
                    }
                }
                finally { bitmap.UnlockBits(data); }
            }

            return source;
        }

        private static byte[] ProjectPixels(byte[] source, int width, int height, double yawDegrees,
            double pitchDegrees, double horizontalFovDegrees, CancellationToken token)
        {
            const int size = 768;
            token.ThrowIfCancellationRequested();
            double yaw = yawDegrees * Math.PI / 180, pitch = pitchDegrees * Math.PI / 180;
            double sinYaw = Math.Sin(yaw), cosYaw = Math.Cos(yaw);
            double sinPitch = Math.Sin(pitch), cosPitch = Math.Cos(pitch);
            double tangent = Math.Tan(horizontalFovDegrees * Math.PI / 360);
            var offsets = new double[size];
            for (int x = 0; x < size; x++) offsets[x] = (2 * (x + 0.5) / size - 1) * tangent;
            var pixels = new byte[size * size * 4];
            for (int y = 0; y < size; y++)
            {
                token.ThrowIfCancellationRequested();
                double sy = -offsets[y];
                // Local axes: horizontal right, original forward, world up.
                double baseRight = sinYaw * (cosPitch - sy * sinPitch);
                double baseForward = cosYaw * (cosPitch - sy * sinPitch);
                double vertical = sinPitch + sy * cosPitch;
                for (int x = 0; x < size; x++)
                {
                    double right = baseRight + offsets[x] * cosYaw;
                    double forward = baseForward - offsets[x] * sinYaw;
                    double longitude = Math.Atan2(right, forward);
                    double latitude = Math.Atan2(vertical, Math.Sqrt(right * right + forward * forward));
                    Sample(source, width, height, (longitude / (2 * Math.PI) + 0.5) * width - 0.5,
                        (0.5 - latitude / Math.PI) * height - 0.5, pixels, (y * size + x) * 4);
                }
            }
            using (var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb))
            {
                var data = bitmap.LockBits(new Rectangle(0, 0, size, size), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                try
                {
                    for (int row = 0; row < size; row++)
                    {
                        token.ThrowIfCancellationRequested();
                        Marshal.Copy(pixels, row * size * 4, IntPtr.Add(data.Scan0, row * data.Stride), size * 4);
                    }
                }
                finally { bitmap.UnlockBits(data); }
                using (var output = new MemoryStream())
                {
                    bitmap.Save(output, ImageFormat.Png);
                    token.ThrowIfCancellationRequested();
                    return output.ToArray();
                }
            }
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        private static uint ReadBigEndian(byte[] bytes, int offset) =>
            ((uint)bytes[offset] << 24) | ((uint)bytes[offset + 1] << 16) | ((uint)bytes[offset + 2] << 8) | bytes[offset + 3];

        private static void Sample(byte[] source, int width, int height, double x, double y, byte[] output, int target)
        {
            // Longitude wraps across the back seam; latitude clamps at the poles.
            int floorX = (int)Math.Floor(x), x0 = (floorX + width) % width, x1 = (x0 + 1) % width;
            y = Math.Max(0, Math.Min(height - 1, y));
            int y0 = (int)y, y1 = Math.Min(y0 + 1, height - 1);
            double dx = x - floorX, dy = y - y0;
            int a = (y0 * width + x0) * 4, b = (y0 * width + x1) * 4;
            int c = (y1 * width + x0) * 4, d = (y1 * width + x1) * 4;
            double aa = (1 - dx) * (1 - dy) * source[a + 3], ab = dx * (1 - dy) * source[b + 3];
            double ac = (1 - dx) * dy * source[c + 3], ad = dx * dy * source[d + 3];
            double alpha = aa + ab + ac + ad;
            output[target + 3] = (byte)Math.Round(alpha);
            if (alpha == 0) return;
            for (int channel = 0; channel < 3; channel++)
                output[target + channel] = (byte)Math.Round((source[a + channel] * aa + source[b + channel] * ab +
                    source[c + channel] * ac + source[d + channel] * ad) / alpha);
        }
    }
}
