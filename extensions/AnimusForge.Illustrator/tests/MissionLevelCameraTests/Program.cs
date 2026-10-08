using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Drawing;

// Exercises the actual compiled geometry only; no game initialization, screenshots or HTTP.
internal static class Program
{
    private static int _checks;
    private static MethodInfo _build;
    private static Type _vec;
    private static object V(float x, float y, float z) => Activator.CreateInstance(_vec, new object[] { x, y, z, -1f });
    private static float C(object v, string field) => (float)_vec.GetField(field).GetValue(v);
    private static void Check(bool pass, string label) { if (!pass) throw new Exception(label); _checks++; }
    private static bool Near(float a, float b) => Math.Abs(a - b) < 0.0001f;
    private static Array Build(float x, float y, float z, float radius = 1.6f, float aspect = 1.777778f)
        => (Array)_build.Invoke(null, new[] { V(10, 20, 3), V(x, y, z), (object)radius, aspect });
    public static int Main(string[] args)
    {
        try
        {
            using (var resolver = new OfflineAssemblyResolver(args.Skip(1).Prepend(Path.GetDirectoryName(Path.GetFullPath(args[0]))).ToArray()))
            {
                var dll = Assembly.LoadFrom(Path.GetFullPath(args[0]));
                _build = dll.GetType("AnimusForge.Illustrator.Engine.MissionScreenshotCapture", true)
                    .GetMethod("BuildLevelPositions", BindingFlags.Static | BindingFlags.NonPublic);
                _vec = _build.GetParameters()[0].ParameterType;
                var visibility = dll.GetType("AnimusForge.Illustrator.Engine.MissionScreenshotImageCodec", true)
                    .GetMethod("ValidateVisibleContent", BindingFlags.Static | BindingFlags.NonPublic);
                bool Rejects(Image img) {
                    try { visibility.Invoke(null, new object[] { img }); return false; }
                    catch (TargetInvocationException ex) when (ex.InnerException is InvalidDataException) { return true; }
                }
                using (var black = new Bitmap(320, 180)) {
                    Check(Rejects(black), "black reference rejected before request");
                    using (var g = Graphics.FromImage(black)) g.Clear(Color.FromArgb(4, 3, 2));
                    Check(Rejects(black), "near-black export rejected");
                    using (var g = Graphics.FromImage(black)) g.FillRectangle(Brushes.DimGray, 0, 0, 80, 180);
                    Check(!Rejects(black), "readable dark scene passes brightness guard");
                }
                foreach (string sample in args.Skip(1).Where(p => p.EndsWith(".png", StringComparison.OrdinalIgnoreCase)))
                    using (var img = Image.FromFile(sample))
                        Check(Rejects(img) == Path.GetFileName(sample).Contains("black"), "recorded reference brightness: " + Path.GetFileName(sample));
                var pair = Build(0, 1, 0);
                Check(pair.Length == 2, "exactly two viewpoints");
                var front = pair.GetValue(0); var rear = pair.GetValue(1);
                Check(Near(C(front, "z"), 3) && Near(C(rear, "z"), 3), "both viewpoints are level with pivot");
                Check(Near(C(front, "x"), 10) && Near(C(rear, "x"), 10), "camera horizontal view defines axis");
                Check(C(front, "y") < 20 && C(rear, "y") > 20, "A looks along player camera yaw, B reverses");
                Check(Near(C(front, "y") + C(rear, "y"), 40), "opposite 180-degree viewpoints");
                var tilted = Build(0, 8, 99);
                Check(Near(C(tilted.GetValue(0), "y"), C(front, "y")) && Near(C(tilted.GetValue(0), "z"), 3), "pitch and vector magnitude do not tilt or zoom framing");
                var east = Build(1, 0, 0);
                Check(C(east.GetValue(0), "x") < 10 && Near(C(east.GetValue(0), "y"), 20), "camera yaw rotates rig");
                Check(C(Build(0, 1, 0, 6).GetValue(0), "y") < C(front, "y"), "larger interaction region increases framing distance");
                Check(C(Build(0, 1, 0, 1.6f, 0.6f).GetValue(0), "y") < C(front, "y"), "narrow screen retains horizontal coverage");
                foreach (var invalid in new[] { new[] { 0f, 0f, 1f, 1.6f, 1f }, new[] { float.NaN, 1f, 0f, 1.6f, 1f }, new[] { 0f, 1f, 0f, 1.6f, 0f } })
                {
                    bool rejected = false;
                    try { Build(invalid[0], invalid[1], invalid[2], invalid[3], invalid[4]); }
                    catch (TargetInvocationException ex) when (ex.InnerException is InvalidOperationException) { rejected = true; }
                    Check(rejected, "invalid framing rejected without camera fallback");
                }
            }
            Console.WriteLine("PASS " + _checks + " actual-DLL geometry checks; native capture NOT-RUN.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
