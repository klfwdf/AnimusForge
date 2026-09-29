using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;

public static class PassiveScreenshotAudit
{
    private static int _passed;
    private static Type _maskType;
    private static MethodInfo _prepare;
    private static MethodInfo _bounds;
    private static void Check(bool value, string name)
    {
        if (!value) throw new InvalidOperationException("FAIL: " + name);
        _passed++;
        Console.WriteLine("PASS: " + name);
    }
    private static Bitmap Prepare(Bitmap source, Rectangle[] masks, bool unknown = false)
    {
        return (Bitmap)_prepare.Invoke(null, new object[] { source, masks, unknown });
    }
    private static bool SamePixels(Bitmap first, Bitmap second)
    {
        if (first == null || second == null || first.Size != second.Size) return false;
        for (int y = 0; y < first.Height; y++)
            for (int x = 0; x < first.Width; x++)
                if (first.GetPixel(x, y).ToArgb() != second.GetPixel(x, y).ToArgb()) return false;
        return true;
    }
    private static bool Bounds(float x, float y, float width, float height, float padding, out Rectangle rectangle)
    {
        object[] args = { x, y, width, height, padding, Rectangle.Empty };
        bool result = (bool)_bounds.Invoke(null, args);
        rectangle = (Rectangle)args[5];
        return result;
    }
    public static void Run(string assemblyPath)
    {
        _passed = 0;
        _maskType = Assembly.LoadFrom(assemblyPath).GetType("AnimusForge.Illustrator.Engine.SceneScreenshotMask", true);
        _prepare = _maskType.GetMethod("Prepare", BindingFlags.Static | BindingFlags.NonPublic);
        _bounds = _maskType.GetMethod("TryCreatePixelBounds", BindingFlags.Static | BindingFlags.NonPublic);
        Color mask = (Color)_maskType.GetField("MaskColor", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        Check(mask.A == 255 && mask.R == mask.G && mask.G == mask.B, "mask is opaque neutral grey");
        using (var source = new Bitmap(100, 60, PixelFormat.Format32bppArgb))
        {
            Color[] colors = { Color.Red, Color.Blue, Color.Gold, Color.FromArgb(255, 214, 163, 131), Color.Purple, Color.FromArgb(87, 23, 99, 211) };
            for (int y = 0; y < source.Height; y++)
                for (int x = 0; x < source.Width; x++) source.SetPixel(x, y, colors[(x + y) % colors.Length]);
            using (var pristine = (Bitmap)source.Clone())
            {
                using (var result = Prepare(source, new Rectangle[0]))
                {
                    Check(result != null && !ReferenceEquals(source, result), "empty masks still produce an independent copy");
                    Check(SamePixels(source, result), "red blue gold skin purple and translucent RGBA preserved exactly");
                }
                var rectangle = new Rectangle(75, 5, 25, 50);
                using (var result = Prepare(source, new[] { rectangle }))
                {
                    bool correct = result != null;
                    for (int y = 0; y < source.Height && correct; y++)
                        for (int x = 0; x < source.Width; x++)
                            if (result.GetPixel(x, y).ToArgb() != (rectangle.Contains(x, y) ? mask : source.GetPixel(x, y)).ToArgb()) { correct = false; break; }
                    Check(correct, "only the requested UI rectangle is replaced; untouched pixels retain exact RGBA");
                    using (var stream = new MemoryStream())
                    {
                        result.Save(stream, ImageFormat.Png);
                        stream.Position = 0;
                        using (var decoded = new Bitmap(stream)) Check(SamePixels(result, decoded), "PNG round trip preserves scene and mask pixels");
                    }
                }
                Check(SamePixels(source, pristine), "masking never changes caller bitmap");
                using (var result = Prepare(source, new[] { new Rectangle(0, 0, 45, 60), new Rectangle(0, 0, 45, 60) }))
                    Check(result != null, "overlapping UI masks count their union once");
                using (var result = Prepare(source, new[] { new Rectangle(0, 0, 30, 60), new Rectangle(70, 0, 30, 60) }))
                    Check(result == null, "disjoint panels obscuring over half the view reject capture");
                using (var result = Prepare(source, new[] { new Rectangle(0, 0, 50, 60) }))
                    Check(result != null, "exactly half of the view remains usable");
                using (var result = Prepare(source, new[] { new Rectangle(0, 0, 100, 60) }))
                    Check(result == null, "full screen gallery is never sent as a scene reference");
                using (var result = Prepare(source, new Rectangle[0], true))
                    Check(result == null, "unknown overlay bounds conservatively reject capture");
                using (var result = Prepare(source, null)) Check(result == null, "missing mask inventory rejects capture");
                using (var result = Prepare(source, new[] { Rectangle.Empty })) Check(result == null, "unmeasured UI panel rejects capture");
                using (var result = Prepare(source, new[] { new Rectangle(150, 0, 20, 20) }))
                    Check(SamePixels(source, result), "fully offscreen panel leaves scene pixels intact");
                using (var result = Prepare(source, new[] { new Rectangle(-5, 0, 10, 10) }))
                    Check(result.GetPixel(0, 0).ToArgb() == mask.ToArgb() && result.GetPixel(5, 0).ToArgb() == source.GetPixel(5, 0).ToArgb(), "partly offscreen panel clips without shifting");
                Check(SamePixels(source, pristine), "all rejection and clipping paths leave source unchanged");
            }
        }
        Rectangle bounds;
        Check(Bounds(100, 50, 200, 300, 24, out bounds) && bounds == new Rectangle(76, 26, 248, 348), "measured pixel position and size are used without double UI scaling");
        Check(Bounds(100.25f, 50.5f, 200, 300, 0, out bounds) && bounds == new Rectangle(100, 50, 201, 301), "fractional edges round outward so UI cannot leak through");
        Check(!Bounds(float.NaN, 0, 1, 1, 0, out bounds), "NaN panel position rejected");
        Check(!Bounds(0, 0, float.PositiveInfinity, 1, 0, out bounds), "infinite panel width rejected");
        Check(!Bounds(0, 0, 0, 1, 0, out bounds), "zero-sized panel rejected");
        Check(!Bounds(float.MaxValue, 0, 1, 1, 0, out bounds), "out-of-range coordinates rejected");
        Console.WriteLine("Passive screenshot audit: " + _passed + " PASS, 0 FAIL");
    }
}
