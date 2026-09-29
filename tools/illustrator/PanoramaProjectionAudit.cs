using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using TaleWorlds.Library;
using Color = System.Drawing.Color;

// Tests production DLL CPU conversion only. These synthetic fixtures establish
// mapping/color/data contracts, NOT real SceneView camera/GPU/actor safety.
public static class PanoramaProjectionAudit
{
    private const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private static Type projection;
    private static int checks;
    private static void Check(bool value, string name)
    {
        if (!value) throw new Exception("FAIL " + name);
        checks++;
        Console.WriteLine("PASS " + name);
    }
    private static object Call(string name, params object[] args)
    {
        try { return projection.GetMethod(name, Static).Invoke(null, args); }
        catch (TargetInvocationException ex) { throw ex.InnerException; }
    }
    private static byte[] Compose(IReadOnlyList<byte[]> pngs, int width, int height)
    { return (byte[])Call("Compose", pngs, width, height); }
    private static void Reject(Action action, string name)
    {
        bool rejected = false;
        try { action(); }
        catch (ArgumentException) { rejected = true; }
        catch (InvalidDataException) { rejected = true; }
        Check(rejected, name);
    }
    private static void Map(double x, double y, double z, out int face, out double u, out double v)
    {
        object[] args = { x, y, z, 0, 0d, 0d };
        Call("MapDirection", args);
        face = (int)args[3]; u = (double)args[4]; v = (double)args[5];
    }
    private static bool Close(double a, double b) { return Math.Abs(a - b) < 0.00001; }
    private static bool Vector(Vec3 v, double x, double y, double z)
    { return Close(v.x, x) && Close(v.y, y) && Close(v.z, z); }

    public static void Run(string assemblyPath)
    {
        checks = 0;
        projection = Assembly.LoadFrom(assemblyPath).GetType("AnimusForge.Illustrator.Engine.PanoramaProjection", true);
        Check(projection != null, "production panorama converter loaded");
        TestFrames();
        TestDirections();
        TestProjection();
        TestValidation();
        Console.WriteLine("PanoramaProjectionAudit: " + checks + " PASS / 0 FAIL (CPU only; no GPU acceptance)");
    }

    private static void TestFrames()
    {
        double[] pitches = { 0, 0.47, -0.63, Math.PI / 2, -Math.PI / 2 };
        double[] yaws = { 0, 0.35, Math.PI / 2, -2.4 };
        foreach (double yaw in yaws)
        foreach (double pitch in pitches)
        {
            double sx = Math.Sin(yaw), cy = Math.Cos(yaw), cp = Math.Cos(pitch), sp = Math.Sin(pitch);
            MatrixFrame original = MatrixFrame.Identity;
            original.origin = new Vec3(37f, -13f, 6.5f);
            original.rotation.s = new Vec3((float)cy, (float)-sx, 0);
            original.rotation.f = new Vec3((float)(-sx * sp), (float)(-cy * sp), (float)cp);
            original.rotation.u = new Vec3((float)(-sx * cp), (float)(-cy * cp), (float)-sp);
            MatrixFrame[] frames = (MatrixFrame[])Call("BuildCameraFrames", original);
            bool good = frames.Length == 6;
            double[][] forwards = { new[] { sx, cy, 0d }, new[] { cy, -sx, 0d },
                new[] { -sx, -cy, 0d }, new[] { -cy, sx, 0d }, new[] { 0d, 0d, 1d }, new[] { 0d, 0d, -1d } };
            double[][] rights = { new[] { cy, -sx, 0d }, new[] { -sx, -cy, 0d },
                new[] { -cy, sx, 0d }, new[] { sx, cy, 0d }, new[] { cy, -sx, 0d }, new[] { cy, -sx, 0d } };
            double[][] ups = { new[] { 0d, 0d, 1d }, new[] { 0d, 0d, 1d },
                new[] { 0d, 0d, 1d }, new[] { 0d, 0d, 1d }, new[] { -sx, -cy, 0d }, new[] { sx, cy, 0d } };
            for (int i = 0; i < frames.Length; i++)
            {
                good &= Vector(frames[i].origin, 37, -13, 6.5);
                good &= Vector(-frames[i].rotation.u, forwards[i][0], forwards[i][1], forwards[i][2]);
                good &= Vector(frames[i].rotation.s, rights[i][0], rights[i][1], rights[i][2]);
                good &= Vector(frames[i].rotation.f, ups[i][0], ups[i][1], ups[i][2]);
            }
            Check(good, "six camera forward/right/up bases and common origin yaw=" + yaw + " pitch=" + pitch);
        }
        MatrixFrame invalid = MatrixFrame.Identity;
        invalid.origin.x = float.NaN;
        Reject(delegate { Call("BuildCameraFrames", invalid); }, "invalid origin rejected");
        invalid = MatrixFrame.Identity;
        invalid.rotation.u = Vec3.Zero; invalid.rotation.s = Vec3.Zero;
        Reject(delegate { Call("BuildCameraFrames", invalid); }, "degenerate orientation rejected");
    }

    private static void TestDirections()
    {
        double[][] centers = { new[] { 0d, 1d, 0d }, new[] { 1d, 0d, 0d }, new[] { 0d, -1d, 0d },
            new[] { -1d, 0d, 0d }, new[] { 0d, 0d, 1d }, new[] { 0d, 0d, -1d } };
        for (int i = 0; i < 6; i++)
        {
            int face; double u, v;
            Map(centers[i][0], centers[i][1], centers[i][2], out face, out u, out v);
            Check(face == i && Close(u, .5) && Close(v, .5), "axis/pole maps to face center " + i);
        }
        for (int expectedFace = 0; expectedFace < 6; expectedFace++)
        {
            bool good = true;
            foreach (double expectedU in new[] { .13, .5, .87 })
            foreach (double expectedV in new[] { .13, .5, .87 })
            {
                double x, y, z;
                FaceDirection(expectedFace, expectedU, expectedV, out x, out y, out z);
                int face; double u, v;
                Map(x, y, z, out face, out u, out v);
                good &= face == expectedFace && Close(u, expectedU) && Close(v, expectedV);
            }
            Check(good, "face " + expectedFace + " asymmetric grid is not mirrored or flipped");
        }
        int[] coverage = new int[6];
        bool bounded = true;
        for (int lat = -89; lat <= 89; lat += 2)
        for (int lon = -180; lon < 180; lon += 2)
        {
            double p = lat * Math.PI / 180, a = lon * Math.PI / 180;
            int face; double u, v;
            Map(Math.Sin(a) * Math.Cos(p), Math.Cos(a) * Math.Cos(p), Math.Sin(p), out face, out u, out v);
            coverage[face]++;
            bounded &= u >= 0 && u <= 1 && v >= 0 && v <= 1;
        }
        Check(bounded && Array.TrueForAll(coverage, delegate(int hits) { return hits > 100; }), "full sphere has bounded coverage including ceiling and floor");
        Reject(delegate { int f; double u, v; Map(0, 0, 0, out f, out u, out v); }, "zero direction rejected");
        Reject(delegate { int f; double u, v; Map(double.NaN, 1, 0, out f, out u, out v); }, "NaN direction rejected");
    }

    private static void TestProjection()
    {
        Color[] colors = { Color.FromArgb(255, 241, 23, 37), Color.FromArgb(255, 18, 57, 223),
            Color.FromArgb(255, 219, 165, 42), Color.FromArgb(255, 218, 158, 123),
            Color.FromArgb(255, 139, 47, 185), Color.FromArgb(117, 229, 81, 46) };
        var inputs = new List<byte[]>();
        for (int i = 0; i < 6; i++)
        {
            Color c = colors[i];
            inputs.Add(NativePng(32, delegate(int x, int y) { return c; }));
        }
        byte[] result = Compose(inputs, 512, 256);
        Check(result[24] == 8 && result[25] == 6, "output is ordinary RGBA8 PNG");
        using (var stream = new MemoryStream(result))
        using (var image = new Bitmap(stream))
        {
            Check(image.Width == 512 && image.Height == 256, "output is a 2:1 equirectangular canvas");
            Point[] centers = { new Point(256, 128), new Point(384, 128), new Point(0, 128),
                new Point(128, 128), new Point(256, 0), new Point(256, 255) };
            for (int i = 0; i < 6; i++)
                Check(image.GetPixel(centers[i].X, centers[i].Y).ToArgb() == colors[i].ToArgb(),
                    "native R/B corrected once and exact RGBA preserved face " + i);
        }
        // Analytic continuous sphere: encode world direction as RGB. Any wrong
        // cube orientation/mirror, rotated pole or six-tile collage fails this.
        inputs.Clear();
        for (int i = 0; i < 6; i++)
        {
            int face = i;
            inputs.Add(NativePng(64, delegate(int x, int y)
            {
                double dx, dy, dz;
                FaceDirection(face, (x + .5) / 64, (y + .5) / 64, out dx, out dy, out dz);
                return DirectionColor(dx, dy, dz);
            }));
        }
        result = Compose(inputs, 512, 256);
        int maximumError = 0;
        using (var stream = new MemoryStream(result))
        using (var image = new Bitmap(stream))
        {
            for (int y = 0; y < 256; y += 3)
            for (int x = 0; x < 512; x += 5)
            {
                double lon = ((x + .5) / 512 - .5) * Math.PI * 2;
                double lat = (.5 - (y + .5) / 256) * Math.PI;
                Color expected = DirectionColor(Math.Sin(lon) * Math.Cos(lat), Math.Cos(lon) * Math.Cos(lat), Math.Sin(lat));
                Color actual = image.GetPixel(x, y);
                maximumError = Math.Max(maximumError, ColorError(expected, actual));
            }
            Check(maximumError <= 3, "analytic sphere matches all longitudes/latitudes, max channel error=" + maximumError);
            bool seam = true;
            foreach (int y in new[] { 0, 1, 64, 127, 192, 254, 255 })
                seam &= ColorError(image.GetPixel(0, y), image.GetPixel(511, y)) <= 3;
            Check(seam, "longitude wrap and polar seams remain continuous");
            Check(image.GetPixel(384, 128).R > 245 && image.GetPixel(128, 128).R < 10,
                "panorama horizontal handedness right vs left");
            Check(image.GetPixel(256, 0).B > 245 && image.GetPixel(256, 255).B < 10,
                "panorama ceiling and floor are not vertically inverted");
        }
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        byte[] fullResolution = Compose(inputs, 2048, 1024);
        elapsed.Stop();
        Check(fullResolution.Length > 33 && fullResolution[18] == 8 && fullResolution[22] == 4,
            "default 2048 x 1024 production resolution composes successfully");
        Console.WriteLine("CPU default-resolution composition: " + elapsed.ElapsedMilliseconds + " ms (informational; no GPU timing)");
        // A single symmetric pair does not prove native view failure.
        inputs[1] = inputs[0];
        Check(Compose(inputs, 64, 32).Length > 0, "one repeated face pair is not rejected as all-view failure");
    }

    private static void TestValidation()
    {
        byte[] original = NativePng(16, delegate(int x, int y) { return Color.FromArgb(255, 80 + x, 105 + y, 50); });
        var identical = new List<byte[]> { original, original, original, original, original, original };
        Reject(delegate { Compose(identical, 64, 32); }, "six identical native exports rejected instead of fake panorama");
        var nearlySame = new List<byte[]> { original };
        for (int i = 1; i < 6; i++)
            nearlySame.Add(NativePng(16, delegate(int x, int y) { return Color.FromArgb(255, 81 + x, 106 + y, 51); }));
        Reject(delegate { Compose(nearlySame, 64, 32); }, "six near-identical exports rejected with high confidence");
        Reject(delegate { Compose(null, 64, 32); }, "null faces rejected");
        Reject(delegate { Compose(new List<byte[]> { original }, 64, 32); }, "incomplete directional coverage rejected");
        Reject(delegate { Compose(identical, 64, 64); }, "non-equirectangular output rejected");
        Reject(delegate { Compose(identical, 8192, 4096); }, "oversized output rejected");
        var invalid = new List<byte[]>(identical);
        invalid[5] = null;
        Reject(delegate { Compose(invalid, 64, 32); }, "null face rejected before decode");
        invalid[5] = new byte[32];
        Reject(delegate { Compose(invalid, 64, 32); }, "malformed PNG header rejected");
        invalid[5] = NativePng(8, delegate(int x, int y) { return Color.Red; });
        Reject(delegate { Compose(invalid, 64, 32); }, "unequal face sizes rejected");
        byte[] oversized = (byte[])original.Clone();
        WriteDimension(oversized, 2049, 2049);
        invalid[5] = oversized;
        Reject(delegate { Compose(invalid, 64, 32); }, "oversized decoded dimensions rejected before allocation");
        byte[] rectangular = (byte[])original.Clone();
        WriteDimension(rectangular, 16, 15); invalid[5] = rectangular;
        Reject(delegate { Compose(invalid, 64, 32); }, "rectangular cube face rejected");
        byte[] budget = (byte[])original.Clone();
        WriteDimension(budget, 2048, 2048);
        var huge = new List<byte[]> { budget, budget, budget, budget, budget, budget };
        Reject(delegate { Compose(huge, 64, 32); }, "aggregate decoded pixel budget rejected before GDI decode");
        byte[] byteBudget = new byte[12 * 1024 * 1024];
        Array.Copy(original, byteBudget, original.Length);
        huge = new List<byte[]> { byteBudget, byteBudget, byteBudget, byteBudget, byteBudget, byteBudget };
        Reject(delegate { Compose(huge, 64, 32); }, "aggregate compressed byte budget rejected before GDI decode");
    }

    private static int ColorError(Color a, Color b)
    { return Math.Max(Math.Abs(a.R - b.R), Math.Max(Math.Abs(a.G - b.G), Math.Max(Math.Abs(a.B - b.B), Math.Abs(a.A - b.A)))); }
    private static void WriteDimension(byte[] png, int width, int height)
    {
        for (int i = 0; i < 4; i++)
        { png[16 + i] = (byte)(width >> ((3 - i) * 8)); png[20 + i] = (byte)(height >> ((3 - i) * 8)); }
    }
    // Independent inverse camera-basis fixture, with deliberately asymmetric
    // coordinates. It does not call the production mapping method.
    private static void FaceDirection(int face, double u, double v, out double x, out double y, out double z)
    {
        double h = 2 * u - 1, t = 1 - 2 * v;
        switch (face)
        {
            case 0: x = h; y = 1; z = t; break;
            case 1: x = 1; y = -h; z = t; break;
            case 2: x = -h; y = -1; z = t; break;
            case 3: x = -1; y = h; z = t; break;
            case 4: x = h; y = -t; z = 1; break;
            default: x = h; y = t; z = -1; break;
        }
    }
    private static Color DirectionColor(double x, double y, double z)
    {
        double length = Math.Sqrt(x * x + y * y + z * z);
        return Color.FromArgb(255, (int)Math.Round(127.5 + 127.5 * x / length),
            (int)Math.Round(127.5 + 127.5 * y / length), (int)Math.Round(127.5 + 127.5 * z / length));
    }
    private static byte[] NativePng(int size, Func<int, int, Color> pixel)
    {
        using (var image = new Bitmap(size, size, PixelFormat.Format32bppArgb))
        {
            byte[] data = new byte[size * size * 4];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                Color c = pixel(x, y); int offset = (y * size + x) * 4;
                // Native producer fixture has reversed R/B encoded values.
                data[offset] = c.R; data[offset + 1] = c.G; data[offset + 2] = c.B; data[offset + 3] = c.A;
            }
            BitmapData bits = image.LockBits(new Rectangle(0, 0, size, size), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            try
            {
                for (int y = 0; y < size; y++) Marshal.Copy(data, y * size * 4, IntPtr.Add(bits.Scan0, y * bits.Stride), size * 4);
            }
            finally { image.UnlockBits(bits); }
            using (var stream = new MemoryStream()) { image.Save(stream, ImageFormat.Png); return stream.ToArray(); }
        }
    }
}
