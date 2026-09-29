using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

// Tests only the identified native portrait producer and its actual read path.
// GPU export and final generated art still require live-game acceptance.
public static class NativePortraitColorAudit
{
    private const BindingFlags All = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static int checks;
    private static MethodInfo decode;
    private static void Check(bool value, string label)
    { if (!value) throw new Exception("FAIL " + label); checks++; Console.WriteLine("PASS " + label); }
    private static Bitmap Decode(byte[] png, CancellationToken token)
    { return (Bitmap)decode.Invoke(null, new object[] { png, token }); }
    private static bool Rejects<T>(Action action) where T : Exception
    {
        try { action(); return false; }
        catch (TargetInvocationException ex) { return ex.InnerException is T; }
    }
    private static byte[] Encode(Bitmap bitmap)
    {
        using (var stream = new MemoryStream()) { bitmap.Save(stream, ImageFormat.Png); return stream.ToArray(); }
    }
    private static int CountCalls(MethodInfo method, MethodInfo target)
    {
        var state = method.GetCustomAttribute<AsyncStateMachineAttribute>();
        if (state != null) method = state.StateMachineType.GetMethod("MoveNext", All);
        var instructions = (System.Collections.Generic.List<Tuple<System.Reflection.Emit.OpCode, object>>)
            typeof(PortraitNoDrawAudit).GetMethod("ReadIl", All).Invoke(null, new object[] { method });
        return instructions.Count(i => object.Equals(i.Item2, target));
    }
    public static void Run(string assemblyPath, string evidencePng, string correctedOutput)
    {
        checks = 0;
        var assembly = Assembly.LoadFrom(assemblyPath);
        var helper = assembly.GetType("AnimusForge.Illustrator.Engine.ScreenCaptureHelper", true);
        decode = assembly.GetType("AnimusForge.Illustrator.Engine.NativePortraitImage", true).GetMethod("DecodeFinalRenderPng", All);
        var reader = helper.GetMethod("ReadNativePortraitPngBase64", All);
        Check(CountCalls(reader, decode) == 1, "actual native file reader applies producer correction exactly once");
        Check(CountCalls(helper.GetMethod("ExtractAppearancePortraitAsync", All), reader) == 1,
            "shared full-body/head pipeline uses the corrected reader");
        Color[] expected = { Color.Red, Color.Blue, Color.Gold, Color.FromArgb(235,181,146), Color.Purple,
            Color.FromArgb(73,23,99,211), Color.FromArgb(255,116,76,56) };
        byte[] fixture;
        using (var source = new Bitmap(7, 3, PixelFormat.Format32bppArgb))
        {
            for (int y = 0; y < source.Height; y++)
                for (int x = 0; x < source.Width; x++)
                {
                    Color color = expected[(x+y) % expected.Length];
                    source.SetPixel(x, y, Color.FromArgb(color.A, color.B, color.G, color.R));
                }
            fixture = Encode(source);
        }
        byte[] original = (byte[])fixture.Clone();
        using (var corrected = Decode(fixture, CancellationToken.None))
        {
            Check(corrected.Width == 7 && corrected.Height == 3, "non-square odd-width portrait dimensions preserved");
            for (int y = 0; y < 3; y++)
                for (int x = 0; x < 7; x++)
                    Check(corrected.GetPixel(x, y).ToArgb() == expected[(x+y) % expected.Length].ToArgb(),
                        "exact red/blue/gold/skin/purple/translucent/brown pixel " + x + "," + y);
        }
        Check(fixture.SequenceEqual(original), "native input byte array is never mutated");
        Check(Rejects<InvalidDataException>(() => Decode(new byte[33], CancellationToken.None)), "invalid PNG rejected");
        byte[] oversized = (byte[])fixture.Clone(); oversized[16] = 0; oversized[17] = 0; oversized[18] = 8; oversized[19] = 1;
        Check(Rejects<InvalidDataException>(() => Decode(oversized, CancellationToken.None)), "oversized header rejected before decode");
        using (var canceled = new CancellationTokenSource())
        {
            canceled.Cancel();
            Check(Rejects<OperationCanceledException>(() => Decode(fixture, canceled.Token)), "canceled conversion propagates cancellation");
        }
        string temp = Path.Combine(Path.GetTempPath(), "af_portrait_color_audit_" + Guid.NewGuid().ToString("N") + ".png");
        try
        {
            // An opaque brown garment panel as it appears in the reversed native export.
            using (var source = new Bitmap(63, 65, PixelFormat.Format32bppArgb))
            using (var graphics = Graphics.FromImage(source))
            { graphics.Clear(Color.FromArgb(56, 76, 116)); File.WriteAllBytes(temp, Encode(source)); }
            string reference = ((Task<string>)reader.Invoke(null, new object[] { temp, 768, CancellationToken.None })).GetAwaiter().GetResult();
            using (var stream = new MemoryStream(Convert.FromBase64String(reference)))
            using (var corrected = new Bitmap(stream))
            {
                Color brown = corrected.GetPixel(31, 32);
                Check(Math.Abs(brown.R-116) <= 3 && Math.Abs(brown.G-76) <= 3 && Math.Abs(brown.B-56) <= 3,
                    "actual file-to-JPEG reference path returns brown clothing without a second swap");
            }
            Check(!File.Exists(temp), "successful reference read cleans up its own native file");
            File.WriteAllBytes(temp, fixture);
            using (var canceled = new CancellationTokenSource())
            {
                canceled.Cancel(); bool stopped = false;
                try { ((Task<string>)reader.Invoke(null, new object[] { temp, 768, canceled.Token })).GetAwaiter().GetResult(); }
                catch (OperationCanceledException) { stopped = true; }
                Check(stopped && !File.Exists(temp), "canceled reader propagates and cleans up its own file");
            }
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
        // Exercise both actual UI preparation entry points with an ordinary PNG.
        // It must remain untouched even though the native producer is corrected.
        var prepared = assembly.GetType("AnimusForge.Illustrator.Engine.GauntletTextureLoader", true)
            .GetNestedType("PreparedImage", BindingFlags.NonPublic);
        using (var source = new Bitmap(7, 1, PixelFormat.Format32bppArgb))
        {
            for (int x = 0; x < 7; x++) source.SetPixel(x, 0, expected[x]);
            byte[] normal = Encode(source);
            File.WriteAllBytes(temp, normal);
            try
            {
                foreach (string entry in new[] { "FromBytes", "FromFile" })
                {
                    object image = prepared.GetMethod(entry, All).Invoke(null, entry == "FromBytes"
                        ? new object[] { normal } : new object[] { temp, CancellationToken.None });
                    byte[] encoded = (byte[])prepared.GetField("_encoded", All).GetValue(image);
                    Check(encoded.SequenceEqual(normal), entry + " keeps ordinary RGBA PNG byte-for-byte unchanged");
                    using (var stream = new MemoryStream(encoded))
                    using (var decoded = new Bitmap(stream))
                        for (int x = 0; x < 7; x++)
                            Check(decoded.GetPixel(x, 0).ToArgb() == expected[x].ToArgb(), entry + " preserves ordinary UI color " + x);
                }
            }
            finally { File.Delete(temp); }
        }
        if (!string.IsNullOrEmpty(evidencePng))
        {
            using (var corrected = Decode(File.ReadAllBytes(evidencePng), CancellationToken.None))
                corrected.Save(correctedOutput, ImageFormat.Png);
            Check(File.Exists(correctedOutput), "captured user reference processed by the production adapter for visual comparison");
        }
        Console.WriteLine("NATIVE PORTRAIT COLOR AUDIT: " + checks + " PASS / 0 FAIL (no GPU or external API)");
    }
}
