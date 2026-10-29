using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

// Compile separately for .NET Framework and run against each built implementation.
// Exercises the real diplomacy upload preparation without invoking native rendering.
internal static class TextureColorProbe
{
    private static long _pixels;
    private static int Main(string[] args)
    {
        if (args.Length < 4) return 2;
        AppDomain.CurrentDomain.AssemblyResolve += delegate(object sender, ResolveEventArgs request)
        {
            string name = new AssemblyName(request.Name).Name;
            if (name.EndsWith(".resources", StringComparison.OrdinalIgnoreCase)) return null;
            for (int i = 3; i < args.Length; i++)
            {
                string path = Path.Combine(args[i], name + ".dll");
                if (File.Exists(path)) return Assembly.LoadFrom(path);
            }
            return null;
        };
        try
        {
            Assembly assembly = Assembly.LoadFrom(Path.GetFullPath(args[0]));
            MethodInfo read = assembly.GetType("AnimusForge.DiplomacyEventSprites", true)
                .GetMethod("ReadTextureBytes", BindingFlags.Static | BindingFlags.NonPublic);
            if (read == null) throw new Exception("Production upload preparation entry missing.");
            string[] files = Directory.GetFiles(args[1], "*.png");
            if (files.Length != 19) throw new Exception("Expected all 19 event textures.");
            foreach (string path in files) Check(read, path);
            Directory.CreateDirectory(args[2]);
            string fixture = Path.Combine(args[2], "rgba-alpha.png");
            using (var bitmap = new Bitmap(7, 3, PixelFormat.Format32bppArgb))
            {
                bitmap.SetResolution(240, 120);
                bitmap.SetPixel(0, 0, Color.FromArgb(123, 210, 51, 17));
                bitmap.SetPixel(6, 2, Color.FromArgb(255, 9, 61, 239));
                bitmap.Save(fixture, ImageFormat.Png);
            }
            try { Check(read, fixture); }
            finally { File.Delete(fixture); }
            Console.WriteLine("PASS actual diplomacy texture reader; 19 bundled textures + RGBA alpha/DPI fixture; exact pixels=" + _pixels + "; MVID=" + assembly.ManifestModule.ModuleVersionId + "; native rendering not exercised.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }

    private static void Check(MethodInfo read, string path)
    {
        byte[] input = File.ReadAllBytes(path);
        byte[] output = (byte[])read.Invoke(null, new object[] { path });
        if (output.Length < 29 || output[24] != 8 || output[25] != 6 || output[28] != 0)
            throw new Exception("Output is not non-interlaced RGBA8: " + path);
        using (var a = new MemoryStream(input))
        using (var b = new MemoryStream(output))
        using (var original = new Bitmap(a))
        using (var prepared = new Bitmap(b))
        {
            if (original.Size != prepared.Size) throw new Exception("Dimensions changed: " + path);
            var area = new Rectangle(0, 0, original.Width, original.Height);
            BitmapData first = original.LockBits(area, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                BitmapData second = prepared.LockBits(area, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                try
                {
                    byte[] left = new byte[original.Width * 4], right = new byte[left.Length];
                    for (int y = 0; y < original.Height; y++)
                    {
                        Marshal.Copy(IntPtr.Add(first.Scan0, y * first.Stride), left, 0, left.Length);
                        Marshal.Copy(IntPtr.Add(second.Scan0, y * second.Stride), right, 0, right.Length);
                        for (int x = 0; x < left.Length; x++)
                            if (left[x] != right[x]) throw new Exception("Pixel/channel changed: " + path + " row=" + y + " byte=" + x);
                    }
                    _pixels += (long)original.Width * original.Height;
                }
                finally { prepared.UnlockBits(second); }
            }
            finally { original.UnlockBits(first); }
        }
    }
}
