using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using TaleWorlds.Library;

namespace AnimusForge.Illustrator.Engine
{
    /// <summary>
    /// Pure CPU conversion of six square, 90-degree native SceneView exports to one
    /// 360 x 180-degree equirectangular image. No camera, scene or UI side effects.
    /// The producer is the native IView final-image exporter: its verified R/B correction
    /// belongs here only, never to encoded provider images or the UI loader.
    /// </summary>
    internal static class PanoramaProjection
    {
        internal const int FaceCount = 6;
        internal const int FrontBackViewCount = 2;
        internal const int FrontBackHeaderHeight = 32;
        internal const int MaximumFaceDimension = 2048;
        private const long MaximumInputPixels = 16L * 1024 * 1024;
        private const int MaximumFaceBytes = 16 * 1024 * 1024;
        private const long MaximumInputBytes = 64L * 1024 * 1024;

        // Fixed face order: front, right, back, left, up, down. All frames have
        // the same origin. Camera forward=-u, right=s, up=f; world up is +Z.
        // The caller must set every camera to HFOV=pi/2 and aspect=1.
        internal static MatrixFrame[] BuildCameraFrames(MatrixFrame original)
        {
            Vec3 forward = new Vec3(-original.rotation.u.x, -original.rotation.u.y, 0f);
            float length = (float)Math.Sqrt(forward.x * forward.x + forward.y * forward.y);
            if (!Finite(original.origin.x) || !Finite(original.origin.y) || !Finite(original.origin.z))
                throw new ArgumentException("Panorama camera origin is invalid.", nameof(original));
            if (!Finite(length) || length < 0.0001f)
            {
                // Looking straight up/down still has a horizontal camera-right.
                forward = new Vec3(-original.rotation.s.y, original.rotation.s.x, 0f);
                length = (float)Math.Sqrt(forward.x * forward.x + forward.y * forward.y);
                if (!Finite(length) || length < 0.0001f)
                    throw new ArgumentException("Panorama camera has no horizontal orientation.", nameof(original));
            }
            forward *= 1f / length;
            Vec3 right = new Vec3(forward.y, -forward.x, 0f);
            Vec3 up = new Vec3(0f, 0f, 1f);
            return new[]
            {
                Frame(original.origin, right, up, forward),
                Frame(original.origin, -forward, up, right),
                Frame(original.origin, -right, up, -forward),
                Frame(original.origin, forward, up, -right),
                Frame(original.origin, right, -forward, up),
                Frame(original.origin, right, forward, -up)
            };
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private static MatrixFrame Frame(Vec3 origin, Vec3 right, Vec3 up, Vec3 forward)
        {
            MatrixFrame result = MatrixFrame.Identity;
            result.origin = origin;
            result.rotation.s = right;
            result.rotation.f = up;
            result.rotation.u = -forward;
            return result;
        }

        /// <summary>
        /// Map a direction expressed in the horizontal front camera's right,
        /// forward, up basis to a face and top-left-origin texture coordinates.
        /// This orientation is shared with BuildCameraFrames (including poles).
        /// </summary>
        internal static void MapDirection(double right, double forward, double up,
            out int face, out double u, out double v)
        {
            double ar = Math.Abs(right), af = Math.Abs(forward), au = Math.Abs(up);
            double largest = Math.Max(ar, Math.Max(af, au));
            if (double.IsNaN(largest) || double.IsInfinity(largest) || largest == 0)
                throw new ArgumentException("Panorama direction must be finite and nonzero.");
            double x, y;
            if (af >= ar && af >= au)
            {
                face = forward >= 0 ? 0 : 2;
                x = forward >= 0 ? right / af : -right / af;
                y = up / af;
            }
            else if (ar >= au)
            {
                face = right >= 0 ? 1 : 3;
                x = right >= 0 ? -forward / ar : forward / ar;
                y = up / ar;
            }
            else
            {
                face = up >= 0 ? 4 : 5;
                x = right / au;
                y = up >= 0 ? -forward / au : forward / au;
            }
            u = (x + 1) * 0.5;
            v = (1 - y) * 0.5;
        }

        /// <summary>
        /// Input must be six uncorrected native face PNGs. R/B are corrected
        /// exactly once while decoding this producer; alpha is preserved.
        /// Runs once per requested capture, on a worker thread. It owns no
        /// persistent cache, and all temporary buffers have bounded dimensions.
        /// </summary>
        internal static byte[] Compose(IReadOnlyList<byte[]> nativeFacePngs, int width = 2048, int height = 1024)
            => ComposeWithCancellation(nativeFacePngs, width, height, CancellationToken.None);

        internal static byte[] ComposeWithCancellation(IReadOnlyList<byte[]> nativeFacePngs, int width, int height, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (nativeFacePngs == null || nativeFacePngs.Count != FaceCount)
                throw new ArgumentException("A panorama requires exactly six native faces.", nameof(nativeFacePngs));
            if (width < 4 || height < 2 || width > 4096 || height > 2048 || width != height * 2)
                throw new ArgumentOutOfRangeException(nameof(width), "Panorama output must be 2:1 and at most 4096 x 2048.");

            int size = 0;
            long totalBytes = 0, totalPixels = 0;
            // Read PNG dimensions before GDI+ allocation, including all faces
            // before decoding the first one. Do not trust compressed byte size.
            for (int i = 0; i < FaceCount; i++)
            {
                byte[] png = nativeFacePngs[i];
                int dimension = ReadSquarePngDimension(png);
                totalBytes += png.Length;
                totalPixels += (long)dimension * dimension;
                if (totalBytes > MaximumInputBytes || totalPixels > MaximumInputPixels)
                    throw new InvalidDataException("Panorama input exceeds the image budget.");
                if (i == 0) size = dimension;
                else if (size != dimension)
                    throw new InvalidDataException("Panorama faces must have identical square dimensions.");
            }
            byte[][] faces = new byte[FaceCount][];
            for (int i = 0; i < FaceCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                faces[i] = DecodeNativeFace(nativeFacePngs[i], size);
            }
            RejectRepeatedFaces(faces);

            byte[] output = new byte[checked(width * height * 4)];
            // Precompute the horizontal trig terms once, rather than millions
            // of calls per panorama. Only two vertical trig calls per row.
            double[] longitudeSin = new double[width], longitudeCos = new double[width];
            for (int x = 0; x < width; x++)
            {
                double longitude = ((x + 0.5) / width - 0.5) * Math.PI * 2;
                longitudeSin[x] = Math.Sin(longitude);
                longitudeCos[x] = Math.Cos(longitude);
            }
            for (int y = 0; y < height; y++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                double latitude = (0.5 - (y + 0.5) / height) * Math.PI;
                double horizontal = Math.Cos(latitude), vertical = Math.Sin(latitude);
                for (int x = 0; x < width; x++)
                {
                    MapDirection(longitudeSin[x] * horizontal, longitudeCos[x] * horizontal, vertical,
                        out int face, out double u, out double v);
                    Sample(faces[face], size, u, v, output, (y * width + x) * 4);
                }
            }
            using (var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb))
            {
                var data = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                try
                {
                    for (int y = 0; y < height; y++)
                        Marshal.Copy(output, y * width * 4, IntPtr.Add(data.Scan0, y * data.Stride), width * 4);
                }
                finally { bitmap.UnlockBits(data); }
                using (var stream = new MemoryStream())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    bitmap.Save(stream, ImageFormat.Png);
                    return stream.ToArray();
                }
            }
        }

        internal static MatrixFrame[] BuildFrontBackCameraFrames(MatrixFrame original)
        {
            var directions = BuildCameraFrames(original);
            return new[] { directions[0], directions[2] };
        }

        // Two opposite perspective views are a reference sheet, not a fabricated 360 panorama.
        // Reuse only the verified native-producer color adapter, without spherical reprojection.
        internal static byte[] ComposeFrontBack(IReadOnlyList<byte[]> nativeViews)
        {
            if (nativeViews == null || nativeViews.Count != FrontBackViewCount)
                throw new ArgumentException("Front/back reference requires exactly two views.", nameof(nativeViews));
            int size = ReadSquarePngDimension(nativeViews[0]);
            if (ReadSquarePngDimension(nativeViews[1]) != size ||
                (long)nativeViews[0].Length + nativeViews[1].Length > MaximumInputBytes ||
                2L * size * size > MaximumInputPixels)
                throw new InvalidDataException("Front/back reference dimensions or byte budget are invalid.");
            byte[] front = DecodeNativeFace(nativeViews[0], size);
            byte[] back = DecodeNativeFace(nativeViews[1], size);
            if (NearlyIdentical(front, back))
                throw new InvalidDataException("前后两张环境图近乎相同，未确认取得不同方向，已停止生成。");
            int width = size * 2, height = size + FrontBackHeaderHeight;
            using (var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb))
            {
                var data = bitmap.LockBits(new Rectangle(0, FrontBackHeaderHeight, width, size), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                try
                {
                    for (int y = 0; y < size; y++)
                    {
                        IntPtr row = IntPtr.Add(data.Scan0, y * data.Stride);
                        Marshal.Copy(front, y * size * 4, row, size * 4);
                        Marshal.Copy(back, y * size * 4, IntPtr.Add(row, size * 4), size * 4);
                    }
                }
                finally { bitmap.UnlockBits(data); }
                using (var graphics = Graphics.FromImage(bitmap))
                using (var font = new Font(FontFamily.GenericSansSerif, 16f, FontStyle.Bold, GraphicsUnit.Pixel))
                {
                    graphics.FillRectangle(Brushes.DimGray, 0, 0, width, FrontBackHeaderHeight);
                    graphics.DrawString("FRONT", font, Brushes.White, 10, 6);
                    graphics.DrawString("BACK", font, Brushes.White, size + 10, 6);
                }
                using (var output = new MemoryStream())
                {
                    bitmap.Save(output, ImageFormat.Png);
                    return output.ToArray();
                }
            }
        }

        private static int ReadSquarePngDimension(byte[] png)
        {
            if (png == null || png.Length < 33 || png.Length > MaximumFaceBytes ||
                png[0] != 137 || png[1] != 80 || png[2] != 78 || png[3] != 71 ||
                png[4] != 13 || png[5] != 10 || png[6] != 26 || png[7] != 10 ||
                png[8] != 0 || png[9] != 0 || png[10] != 0 || png[11] != 13 ||
                png[12] != 73 || png[13] != 72 || png[14] != 68 || png[15] != 82)
                throw new InvalidDataException("Panorama face is not a bounded PNG.");
            uint width = ReadBigEndian(png, 16), height = ReadBigEndian(png, 20);
            if (width < 2 || width > MaximumFaceDimension || height != width)
                throw new InvalidDataException("Panorama face must be square and at most 2048 pixels per side.");
            return (int)width;
        }

        private static uint ReadBigEndian(byte[] bytes, int offset) =>
            ((uint)bytes[offset] << 24) | ((uint)bytes[offset + 1] << 16) | ((uint)bytes[offset + 2] << 8) | bytes[offset + 3];

        private static byte[] DecodeNativeFace(byte[] png, int size)
        {
            using (var stream = new MemoryStream(png, false))
            using (var bitmap = new Bitmap(stream))
            {
                if (bitmap.Width != size || bitmap.Height != size)
                    throw new InvalidDataException("Decoded panorama face dimensions disagree with its header.");
                byte[] pixels = new byte[checked(size * size * 4)];
                var data = bitmap.LockBits(new Rectangle(0, 0, size, size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                try
                {
                    for (int y = 0; y < size; y++)
                        Marshal.Copy(IntPtr.Add(data.Scan0, y * data.Stride), pixels, y * size * 4, size * 4);
                }
                finally { bitmap.UnlockBits(data); }
                for (int i = 0; i < pixels.Length; i += 4)
                {
                    byte blue = pixels[i];
                    pixels[i] = pixels[i + 2];
                    pixels[i + 2] = blue;
                }
                return pixels;
            }
        }

        private static void RejectRepeatedFaces(byte[][] faces)
        {
            // Only reject when ALL six faces are virtually the same raster.
            // A symmetric pair is insufficient evidence. A featureless room
            // cannot establish coverage, so report it as unavailable rather
            // than manufacturing a claimed panorama from one repeated view.
            for (int i = 1; i < FaceCount; i++)
                if (!NearlyIdentical(faces[0], faces[i])) return;
            throw new InvalidDataException("All six scene views are identical or nearly identical; directional panorama coverage is unverified.");
        }

        private static bool NearlyIdentical(byte[] first, byte[] second)
        {
            long difference = 0;
            int changedPixels = 0, pixels = first.Length / 4;
            int allowedChanged = Math.Max(1, pixels / 200); // at most 0.5% outliers
            for (int i = 0; i < first.Length; i += 4)
            {
                int largest = 0;
                for (int channel = 0; channel < 4; channel++)
                {
                    int delta = Math.Abs(first[i + channel] - second[i + channel]);
                    difference += delta;
                    largest = Math.Max(largest, delta);
                }
                if (largest > 2 && ++changedPixels > allowedChanged) return false;
            }
            return difference <= first.Length * 0.75;
        }

        private static void Sample(byte[] source, int size, double u, double v, byte[] output, int target)
        {
            double x = Math.Max(0, Math.Min(size - 1, u * size - 0.5));
            double y = Math.Max(0, Math.Min(size - 1, v * size - 0.5));
            int x0 = (int)x, y0 = (int)y, x1 = Math.Min(x0 + 1, size - 1), y1 = Math.Min(y0 + 1, size - 1);
            double dx = x - x0, dy = y - y0;
            int a = (y0 * size + x0) * 4, b = (y0 * size + x1) * 4;
            int c = (y1 * size + x0) * 4, d = (y1 * size + x1) * 4;
            double wa = (1 - dx) * (1 - dy), wb = dx * (1 - dy), wc = (1 - dx) * dy, wd = dx * dy;
            double alpha = source[a + 3] * wa + source[b + 3] * wb + source[c + 3] * wc + source[d + 3] * wd;
            output[target + 3] = (byte)Math.Round(alpha);
            if (alpha == 0) return;
            // Interpolate premultiplied RGB so transparent edges do not acquire
            // dark fringes; output remains ordinary straight-alpha RGBA PNG.
            double aa = wa * source[a + 3], ab = wb * source[b + 3], ac = wc * source[c + 3], ad = wd * source[d + 3];
            for (int channel = 0; channel < 3; channel++)
                output[target + channel] = (byte)Math.Round((source[a + channel] * aa + source[b + channel] * ab +
                    source[c + channel] * ac + source[d + channel] * ad) / alpha);
        }
    }
}
