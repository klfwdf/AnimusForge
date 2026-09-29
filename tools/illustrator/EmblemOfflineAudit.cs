using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;

// Calls the compiled production methods. Expected masks and geometric landmarks
// are built independently; these are NOT native shader golden images.
public static class EmblemOfflineAudit
{
    public sealed class Result
    {
        public string Name;
        public bool Passed;
        public string Evidence;
    }

    private static Type composer;
    private static readonly BindingFlags PrivateStatic = BindingFlags.NonPublic | BindingFlags.Static;
    private static readonly List<Result> results = new List<Result>();
    private static readonly List<Bitmap> rows = new List<Bitmap>();
    private static string output;

    private static object Call(string method, params object[] args)
    {
        return composer.GetMethod(method, PrivateStatic).Invoke(null, args);
    }

    private static void Check(string name, bool passed, string evidence)
    {
        results.Add(new Result { Name = name, Passed = passed, Evidence = evidence });
    }

    private static Bitmap Mask(int inset, bool inverse, bool redAlso)
    {
        var bitmap = new Bitmap(64, 64, PixelFormat.Format32bppArgb);
        for (int y = 0; y < 64; y++)
        for (int x = 0; x < 64; x++)
        {
            bool inside = x >= inset && x < 64 - inset && y >= inset && y < 64 - inset;
            bitmap.SetPixel(x, y, Color.FromArgb(inside != inverse ? 255 : 0,
                inside && redAlso ? 255 : 0, inside ? 255 : 0, 0));
        }
        return bitmap;
    }

    private static Bitmap ExpectedMask(int inset, Color color)
    {
        var bitmap = new Bitmap(64, 64, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bitmap))
        using (var brush = new SolidBrush(color))
        {
            g.Clear(Color.Transparent);
            g.FillRectangle(brush, inset, inset, 64 - 2 * inset, 64 - 2 * inset);
        }
        return bitmap;
    }

    private static int AlphaPixels(Bitmap bitmap)
    {
        int count = 0;
        for (int y = 0; y < bitmap.Height; y++)
        for (int x = 0; x < bitmap.Width; x++) if (bitmap.GetPixel(x, y).A >= 128) count++;
        return count;
    }

    private static int PixelDifferences(Bitmap a, Bitmap b)
    {
        int count = 0;
        for (int y = 0; y < a.Height; y++)
        for (int x = 0; x < a.Width; x++)
        {
            Color c = a.GetPixel(x, y), d = b.GetPixel(x, y);
            if (c.A == 0 && d.A == 0) continue;
            if (c.ToArgb() != d.ToArgb()) count++;
        }
        return count;
    }

    private static void AddRow(string name, Bitmap input, Bitmap actual, Bitmap expected)
    {
        var row = new Bitmap(660, 240);
        using (var g = Graphics.FromImage(row))
        using (var font = new Font("Arial", 12))
        {
            g.Clear(Color.FromArgb(245, 245, 245));
            g.DrawString(name, font, Brushes.Black, 8, 4);
            Bitmap[] images = { input, actual, expected };
            string[] labels = { "Input / mask", "Production output", "Independent expectation" };
            for (int i = 0; i < 3; i++)
            {
                g.DrawString(labels[i], font, Brushes.Black, 8 + i * 220, 30);
                for (int cy = 0; cy < 16; cy++)
                for (int cx = 0; cx < 16; cx++)
                    g.FillRectangle((cx + cy) % 2 == 0 ? Brushes.LightGray : Brushes.White,
                        8 + i * 220 + cx * 10, 60 + cy * 10, 10, 10);
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.DrawImage(images[i], new Rectangle(8 + i * 220, 60, 160, 160));
            }
        }
        rows.Add(row);
        actual.Save(Path.Combine(output, name + ".png"), ImageFormat.Png);
    }

    private static Bitmap Draw(Bitmap icon, float angle, bool mirror)
    {
        Type pieceType = composer.GetNestedType("PieceJob", BindingFlags.NonPublic);
        object piece = Activator.CreateInstance(pieceType, true);
        foreach (string field in new[] { "Cx", "Cy" }) pieceType.GetField(field).SetValue(piece, 48f);
        foreach (string field in new[] { "W", "H" }) pieceType.GetField(field).SetValue(piece, 64f);
        pieceType.GetField("Deg").SetValue(piece, angle);
        pieceType.GetField("Mirror").SetValue(piece, mirror);
        var canvas = new Bitmap(96, 96);
        using (var g = Graphics.FromImage(canvas)) Call("DrawPiece", g, icon, piece);
        return canvas;
    }

    private static PointF RedCenter(Bitmap bitmap)
    {
        float sx = 0, sy = 0, n = 0;
        for (int y = 0; y < bitmap.Height; y++)
        for (int x = 0; x < bitmap.Width; x++)
        {
            Color c = bitmap.GetPixel(x, y);
            if (c.A > 200 && c.R > 200 && c.G < 50 && c.B < 50) { sx += x; sy += y; n++; }
        }
        return n > 0 ? new PointF(sx / n, sy / n) : new PointF(-1000, -1000);
    }

    public static Result[] Run(string assemblyPath, string outputDirectory, string sampleDirectory)
    {
        output = outputDirectory;
        Directory.CreateDirectory(output);
        composer = Assembly.LoadFrom(assemblyPath).GetType("AnimusForge.Illustrator.Engine.BannerEmblemComposer", true);
        results.Clear(); rows.Clear();

        // 16 distinct cells validate managed indexing, not the unverified GPU export orientation.
        using (var atlas = new Bitmap(64, 64))
        {
            for (int index = 0; index < 16; index++)
            using (var g = Graphics.FromImage(atlas))
            using (var brush = new SolidBrush(Color.FromArgb(255, 10 + index * 12, 50, 200)))
                g.FillRectangle(brush, index % 4 * 16, index / 4 * 16, 16, 16);
            for (int index = 0; index < 16; index++)
            using (var cell = (Bitmap)Call("ExtractAtlasCell", atlas, index))
                Check("atlas_cell_" + index, cell.GetPixel(8, 8).R == 10 + index * 12, "4x4 CPU PNG indexing");
        }

        foreach (int inset in new[] { 16, 6 })
        using (var mask = Mask(inset, false, false))
        using (var actual = (Bitmap)Call("TintIconCell", mask, Color.Gold, Color.Blue, false))
        using (var expected = ExpectedMask(inset, Color.Gold))
        {
            string name = inset == 16 ? "small_alpha_mask" : "large_alpha_mask";
            int differences = PixelDifferences(actual, expected);
            Check(name, differences == 0, "different pixels=" + differences + "/4096; actual center alpha=" + actual.GetPixel(32, 32).A);
            AddRow(name, mask, actual, expected);
        }

        using (var mask = Mask(16, true, false))
        using (var actual = (Bitmap)Call("TintIconCell", mask, Color.Gold, Color.Blue, false))
        using (var expected = ExpectedMask(16, Color.Gold))
        {
            int differences = PixelDifferences(actual, expected);
            Check("inverse_alpha_mask", differences == 0, "different pixels=" + differences + "; inverse mask fixture");
            AddRow("inverse_alpha_mask", mask, actual, expected);
        }

        using (var mask = Mask(16, false, true))
        using (var actual = (Bitmap)Call("TintIconCell", mask, Color.Gold, Color.Blue, false))
        using (var expected = ExpectedMask(16, Color.Blue))
        {
            int differences = PixelDifferences(actual, expected);
            Check("documented_red_priority", differences == 0, "documented R-priority contract; actual center=" + actual.GetPixel(32, 32));
            AddRow("documented_red_priority", mask, actual, expected);
        }

        using (var mask = Mask(16, false, false))
        using (var noStroke = (Bitmap)Call("TintIconCell", mask, Color.Gold, Color.Blue, false))
        using (var stroke = (Bitmap)Call("TintIconCell", mask, Color.Gold, Color.Blue, true))
        using (var expected = new Bitmap(64, 64))
        {
            using (var g = Graphics.FromImage(expected))
            {
                g.FillRectangle(Brushes.Blue, 15, 15, 34, 34);
                g.FillRectangle(Brushes.Gold, 16, 16, 32, 32);
            }
            Check("stroke_adds_coverage", AlphaPixels(stroke) > AlphaPixels(noStroke),
                "without=" + AlphaPixels(noStroke) + "; with=" + AlphaPixels(stroke) + "; synthetic dilation, not native shader golden");
            AddRow("stroke_adds_coverage", mask, stroke, expected);
        }

        using (var icon = new Bitmap(64, 64))
        {
            using (var g = Graphics.FromImage(icon))
            {
                g.FillRectangle(Brushes.Red, 44, 28, 8, 8);
                g.FillRectangle(Brushes.Blue, 28, 10, 8, 8);
            }
            foreach (int angle in new[] { 0, 90, 180, 270 })
            foreach (bool mirror in new[] { false, true })
            using (var actual = Draw(icon, angle, mirror))
            {
                // BannerVisual: RotateAboutUp(+a), then mirror local X. Native camera
                // has +Y up; PNG has +Y down. Thus +90 moves a right landmark UP.
                double a = angle * Math.PI / 180;
                double x = (mirror ? -1 : 1) * 16;
                double ex = 47.5 + Math.Cos(a) * x;
                double ey = 47.5 - Math.Sin(a) * x;
                PointF observed = RedCenter(actual);
                double distance = Math.Sqrt(Math.Pow(observed.X - ex, 2) + Math.Pow(observed.Y - ey, 2));
                Check("native_rotation_" + angle + "_mirror_" + mirror, distance < 1.5,
                    "red landmark distance=" + distance.ToString("0.00") + "; expected=" + ex.ToString("0.0") + "," + ey.ToString("0.0") + "; actual=" + observed);
                if (angle == 90 && !mirror)
                using (var expected = (Bitmap)icon.Clone())
                {
                    expected.RotateFlip(RotateFlipType.Rotate270FlipNone);
                    using (var expectedCanvas = Draw(expected, 0, false)) AddRow("native_rotation_90", icon, actual, expectedCanvas);
                }
            }
        }

        // Do not claim a corrupt old extraction is a valid native golden sample.
        if (!string.IsNullOrEmpty(sampleDirectory) && Directory.Exists(sampleDirectory))
        {
            foreach (string file in Directory.GetFiles(sampleDirectory, "cell_*_raw.png"))
            using (var raw = new Bitmap(file))
            using (var tinted = (Bitmap)Call("TintIconCell", raw, Color.Gold, Color.Blue, true))
            {
                string name = Path.GetFileNameWithoutExtension(file);
                tinted.Save(Path.Combine(output, "replay_" + name + ".png"), ImageFormat.Png);
                bool visible = (bool)Call("HasVisibleContent", tinted, Color.Transparent);
                // Empty known fault sample is a negative control. Other samples are observations only.
                if (name == "cell_162_raw") Check("historical_empty_cell_162_rejected", !visible, "visible=" + visible);
            }
        }

        using (var board = new Bitmap(660, rows.Count * 240))
        using (var g = Graphics.FromImage(board))
        {
            for (int i = 0; i < rows.Count; i++) { g.DrawImageUnscaled(rows[i], 0, i * 240); rows[i].Dispose(); }
            board.Save(Path.Combine(output, "comparison.png"), ImageFormat.Png);
        }
        rows.Clear();
        return results.ToArray();
    }
}
