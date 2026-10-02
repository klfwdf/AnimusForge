using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using AnimusForge.Illustrator.Engine;

internal static class Program
{
    private static int Main()
    {
        try
        {
            foreach (float dpi in new[] { 72f, 96f, 120f, 240f, 300f })
            {
                CheckConversion(dpi, dpi, ImageFormat.Png);
                CheckConversion(dpi, dpi, ImageFormat.Jpeg);
            }
            CheckConversion(240f, 120f, ImageFormat.Png);
            CheckRgbaPassthrough();
            Console.WriteLine("PASS: 12 cases (11 pixel-preserving conversions + RGBA passthrough).");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }

    private static void CheckConversion(float dpiX, float dpiY, ImageFormat format)
    {
        const int width = 320, height = 180;
        byte[] encoded;
        using (var source = new Bitmap(width, height, PixelFormat.Format24bppRgb))
        {
            source.SetResolution(dpiX, dpiY);
            using (var g = Graphics.FromImage(source))
            {
                g.Clear(Color.FromArgb(210, 45, 20));
                g.FillRectangle(Brushes.Green, width / 2, 0, width / 2, height / 2);
                g.FillRectangle(Brushes.Blue, 0, height / 2, width / 2, height / 2);
                g.FillRectangle(Brushes.Gold, width / 2, height / 2, width / 2, height / 2);
            }
            using (var stream = new MemoryStream())
            { source.Save(stream, format); encoded = stream.ToArray(); }
        }
        if (format.Equals(ImageFormat.Png))
            Require(encoded[25] == 2, "RGB fixture must exercise PNG re-encoding, not RGBA fast path");
        byte[] normalized = ImagePayload.Normalize(encoded);
        Require(normalized[24] == 8 && normalized[25] == 6 && normalized[28] == 0, "RGBA8 output contract");
        using (var input = new MemoryStream(encoded))
        using (var original = new Bitmap(input))
        using (var output = new MemoryStream(normalized))
        using (var actual = new Bitmap(output))
        {
            Require(actual.Width == width && actual.Height == height, "pixel dimensions changed");
            // Full pixel oracle detects DPI shrink/crop, transparent margins and channel swaps.
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    Require(actual.GetPixel(x, y).ToArgb() == original.GetPixel(x, y).ToArgb(),
                        "pixel mismatch at " + x + "," + y + " format=" + format + " DPI=" + dpiX + "x" + dpiY);
        }
        byte[] again = ImagePayload.Normalize(normalized);
        Require(ReferenceEquals(normalized, again), "normalized RGBA should be passed through unchanged");
        Console.WriteLine("PASS " + format + " DPI=" + dpiX + "x" + dpiY);
    }

    private static void CheckRgbaPassthrough()
    {
        using (var bitmap = new Bitmap(32, 16, PixelFormat.Format32bppArgb))
        using (var stream = new MemoryStream())
        {
            bitmap.SetResolution(240, 240);
            bitmap.SetPixel(0, 0, Color.FromArgb(128, 200, 30, 10));
            bitmap.SetPixel(31, 15, Color.Blue);
            bitmap.Save(stream, ImageFormat.Png);
            byte[] encoded = stream.ToArray();
            Require(encoded[25] == 6, "RGBA fixture");
            Require(ReferenceEquals(encoded, ImagePayload.Normalize(encoded)), "RGBA bytes/transparency must remain unchanged");
        }
        Console.WriteLine("PASS RGBA passthrough (including intentional transparency)");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
