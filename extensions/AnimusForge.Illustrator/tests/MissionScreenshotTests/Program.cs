using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using AnimusForge.Illustrator.Core;
using AnimusForge.Illustrator.Engine;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;
using Camera = TaleWorlds.Engine.Camera;
using Utilities = TaleWorlds.Engine.Utilities;

internal static class Program
{
    private static int _assertions;
    private static string _root;
    private static Mission _mission;
    private static MissionScreen _screen;
    private static readonly Vec3 Pivot = new Vec3(0, 0, 1.3f);
    private static byte[] BitmapBytes(Color color, int width = 8, int height = 6, float dpi = 96)
    {
        using (var image = new Bitmap(width, height, PixelFormat.Format24bppRgb))
        using (var stream = new MemoryStream())
        {
            image.SetResolution(dpi, dpi);
            using (var graphics = Graphics.FromImage(image)) graphics.Clear(color);
            image.Save(stream, ImageFormat.Bmp); return stream.ToArray();
        }
    }
    private static void Check(bool passed, string name)
    { if (!passed) throw new Exception(name); _assertions++; }
    private static void Reject(Action action, string name)
    { try { action(); } catch (Exception) { _assertions++; return; } throw new Exception("Expected rejection: " + name); }
    private static void Setup(bool paused = false)
    {
        IllustratorRuntime.OwnerThread = Thread.CurrentThread.ManagedThreadId;
        IllustratorRuntime.ApplicationFrame = 0;
        _mission = Mission.Current = new Mission();
        _mission.Scene.TimeSpeed = paused ? 0 : 0.75f;
        MissionState.Current = new MissionState { Paused = paused };
        Game.Current = new Game();
        if (paused) Game.Current.GameStateManager.RegisterActiveStateDisableRequest("prior_ui");
        _screen = new MissionScreen { Mission = _mission };
        _screen.CombatCamera.Frame = new MatrixFrame { origin = new Vec3(0, -4, 1.3f) };
        ScreenManager.TopScreen = _screen;
        MBDebug.DisableAllUI = false;
        int shots = 0;
        Utilities.Export = path => File.WriteAllBytes(path, BitmapBytes(++shots == 1 ? Color.Red : Color.Blue));
    }
    private static void Pump(MissionScreenshotCapture capture)
    {
        for (int i = 0; i < 500 && !capture.Completion.IsCompleted; i++)
        { IllustratorRuntime.ApplicationFrame++; capture.Tick(); Thread.Sleep(3); }
        Check(capture.Completion.IsCompleted, "finite capture completion");
    }
    private static void Restored(bool paused, string caseName, bool checkTime = true)
    {
        Check(_screen.CustomCamera == null, caseName + ": original custom camera");
        Check(_screen.CombatCamera.Position.y == -4, caseName + ": original combat frame");
        Check(!MBDebug.DisableAllUI, caseName + ": UI restored");
        Check(_screen.LayerWatchers == 0, caseName + ": layer subscription removed");
        Check(MissionState.Current.Paused == paused, caseName + ": original pause retained");
        if (checkTime) Check(_mission.Scene.TimeSpeed == (paused ? 0 : 0.75f), caseName + ": original time speed");
        Check(_mission.RequestCount == 0, caseName + ": own pause removed");
        Check(Game.Current.GameStateManager.Count == (paused ? 1 : 0), caseName + ": prior disable owner retained");
        Check(Camera.Created == Camera.ReleasedCount, caseName + ": cameras released once");
    }
    private static void CaptureCases()
    {
        Setup();
        var ui = new TaleWorlds.Engine.GauntletUI.GauntletLayer(); _screen.AddLayer(ui);
        int earlyShots = 0;
        Utilities.Export = path => { earlyShots++; File.WriteAllBytes(path, BitmapBytes(Color.Red)); };
        var warming = new MissionScreenshotCapture(_mission, Pivot, new Vec3(0, 1, 0), 1.6f, true);
        Check(ui.TwoDimensionView.Clears == 1 && ui.TwoDimensionPlatform.Clears == 1 && ui.IsActive, "clear cached UI without changing layer activation");
        IllustratorRuntime.ApplicationFrame = 1; warming.Tick();
        Check(earlyShots == 0, "first application frame cannot export old UI frame");
        Thread.Sleep(270); warming.Tick();
        Check(earlyShots == 0, "elapsed time alone does not skip frame warmup");
        IllustratorRuntime.ApplicationFrame = 4; warming.Tick();
        Check(earlyShots == 1, "export only after both frame and time warmup");
        warming.Cancel("fixture end"); Restored(false, "warmup");
        Setup();
        var changedUi = new MissionScreenshotCapture(_mission, Pivot, new Vec3(0, 1, 0), 1.6f, true);
        _screen.AddLayer(new TaleWorlds.Engine.GauntletUI.GauntletLayer()); changedUi.Tick();
        Check(changedUi.Completion.IsFaulted, "new UI during capture rejects request"); Restored(false, "new UI");

        foreach (bool paused in new[] { false, true })
        {
            Setup(paused);
            int shots = 0;
            Utilities.Export = path => {
                Check(MBDebug.DisableAllUI && MissionState.Current.Paused && _mission.Scene.TimeSpeed == 0, "freeze before each export");
                Check(++shots == 1 ? _screen.CombatCamera.Position.y > 4 : _screen.CombatCamera.Position.y < -4, "independent front then rear level camera");
                File.WriteAllBytes(path, BitmapBytes(shots == 1 ? Color.Red : Color.Blue));
            };
            var capture = new MissionScreenshotCapture(_mission, Pivot, new Vec3(0, 1, 0), 1.6f, true);
            Reject(() => new MissionScreenshotCapture(_mission, Pivot, new Vec3(0, 1, 0), 1.6f, true), "native stage prevents duplicate capture");
            Pump(capture);
            var pair = capture.Completion.GetAwaiter().GetResult();
            Check(!MissionScreenshotImageCodec.SameImage(pair.Current, pair.Reverse), "two distinct raw references");
            Check(shots == 2, "exactly two exports");
            Check(_mission.Scene.LastExclude == (TaleWorlds.Engine.BodyFlags.CameraCollisionRayCastExludeFlags | TaleWorlds.Engine.BodyFlags.DontCollideWithCamera), "use original game camera collision filters, not player body/AI barriers");
            Restored(paused, "success");
            capture.Cancel("late cancel"); Restored(paused, "idempotent completion");
        }
        Setup();
        var cancel = new MissionScreenshotCapture(_mission, Pivot, new Vec3(0, 1, 0), 1.6f, true);
        cancel.Cancel("explicit cancellation");
        Reject(() => cancel.Completion.GetAwaiter().GetResult(), "cancel surfaces failure"); Restored(false, "cancel");
        Setup();
        var leaving = new MissionScreenshotCapture(_mission, Pivot, new Vec3(0, 1, 0), 1.6f, true);
        Mission.Current = new Mission(); leaving.Tick();
        Reject(() => leaving.Completion.GetAwaiter().GetResult(), "leave before completion cancels");
        Check(Mission.Current.Scene.TimeSpeed == 1, "leave never mutates replacement scene");
        Mission.Current = _mission; Restored(false, "leave", checkTime: false);
        Setup();
        var takeover = new MissionScreenshotCapture(_mission, Pivot, new Vec3(0, 1, 0), 1.6f, true);
        var other = _screen.CustomCamera = new Camera { Frame = new MatrixFrame { origin = new Vec3(20, 30, 40) } };
        _screen.CombatCamera.FillParametersFrom(other); takeover.Tick();
        Reject(() => takeover.Completion.GetAwaiter().GetResult(), "other camera takeover stops capture");
        Check(_screen.CustomCamera == other && _screen.CombatCamera.Position.y == 30 && !other.Released, "never overwrite or release other's camera");
        Check(!MBDebug.DisableAllUI && _mission.RequestCount == 0, "takeover still releases UI/time");
        Setup();
        Utilities.Export = path => { throw new IOException("native export failed"); };
        var exportFailure = new MissionScreenshotCapture(_mission, Pivot, new Vec3(0, 1, 0), 1.6f, true); Pump(exportFailure);
        Reject(() => exportFailure.Completion.GetAwaiter().GetResult(), "export failure explicit"); Restored(false, "export failure");
        Setup(); _mission.Scene.HitDistance = 0.2f;
        Reject(() => new MissionScreenshotCapture(_mission, Pivot, new Vec3(0, 1, 0), 1.6f, true), "both camera paths checked before capture"); Restored(false, "blocked view");
        Setup(); _screen.Rendered = false;
        Reject(() => new MissionScreenshotCapture(_mission, Pivot, new Vec3(0, 1, 0), 1.6f, true), "not rendered, reject before mutation"); Restored(false, "not rendered");
        Setup();
        var unknown = new MissionScreenshotCapture(_mission, Pivot, new Vec3(0, 1, 0), 1.6f, true);
        IllustratorRuntime.ApplicationFrame++; unknown.Tick();
        _screen.IsFinalized = true; unknown.Tick();
        Reject(() => unknown.Completion.GetAwaiter().GetResult(), "finalized screen retires capture");
        Check(!MBDebug.DisableAllUI && _mission.RequestCount == 0, "finalized native handles untouched; reversible globals released");
        Setup(true);
        var priorCamera = _screen.CustomCamera = new Camera { Frame = _screen.CombatCamera.Frame };
        MBDebug.DisableAllUI = true;
        var alreadyHidden = new MissionScreenshotCapture(_mission, Pivot, new Vec3(0, 1, 0), 1.6f, true); Pump(alreadyHidden);
        Check(!alreadyHidden.Completion.IsFaulted && _screen.CustomCamera == priorCamera && !priorCamera.Released, "prior custom camera preserved");
        Check(MBDebug.DisableAllUI && MissionState.Current.Paused && Game.Current.GameStateManager.Count == 1, "prior hidden UI and pause kept");
        Setup();
        Utilities.Export = path => { /* Simulate native export never producing a file. */ };
        var timeout = new MissionScreenshotCapture(_mission, Pivot, new Vec3(0, 1, 0), 1.6f, true);
        IllustratorRuntime.ApplicationFrame++; timeout.Tick(); Thread.Sleep(8100); timeout.Tick();
        Check(timeout.Completion.IsFaulted && timeout.Completion.Exception.InnerException.Message.Contains("超时"), "8-second total capture deadline, no export retry");
        Restored(false, "deadline");
    }
    private static void CodecCases()
    {
        foreach (float dpi in new[] { 72f, 96f, 240f, 300f })
            foreach (Color color in new[] { Color.Red, Color.Blue, Color.Gold, Color.FromArgb(210, 160, 135), Color.Purple })
            {
                byte[] bmp = BitmapBytes(color, dpi: dpi);
                MissionScreenshotImageCodec.ValidateBitmap(bmp);
                byte[] png = MissionScreenshotImageCodec.ToPng(bmp, CancellationToken.None);
                using (var stream = new MemoryStream(png)) using (var decoded = new Bitmap(stream))
                {
                    Check(decoded.Width == 8 && decoded.Height == 6, "DPI-independent pixel dimensions");
                    Check(decoded.GetPixel(0, 0).ToArgb() == color.ToArgb() && decoded.GetPixel(7, 5).ToArgb() == color.ToArgb(), "exact RGBA, no R/B swap or DPI margins");
                }
                Check(png[25] == 6, "standard RGBA8 PNG");
            }
        byte[] large = BitmapBytes(Color.Blue, 4096, 2160);
        Check(large.Length > ImagePayload.MaxBytes, "4K raw BMP exceeds normal encoded-image budget");
        MissionScreenshotImageCodec.ValidateBitmap(large);
        byte[] resized = MissionScreenshotImageCodec.ToPng(large, CancellationToken.None);
        using (var stream = new MemoryStream(resized)) using (var bitmap = new Bitmap(stream))
            Check(bitmap.Width == 2048 && bitmap.Height == 1080, "bounded resize preserves aspect ratio");
        byte[] valid = BitmapBytes(Color.Red);
        Reject(() => MissionScreenshotImageCodec.ValidateBitmap(valid.Take(valid.Length - 1).ToArray()), "truncated pixel payload");
        byte[] broken = (byte[])valid.Clone(); broken[0] = 0;
        Reject(() => MissionScreenshotImageCodec.ValidateBitmap(broken), "bad signature");
        broken = (byte[])valid.Clone(); Array.Copy(BitConverter.GetBytes(50000), 0, broken, 18, 4);
        Array.Copy(BitConverter.GetBytes(50000), 0, broken, 22, 4);
        Reject(() => MissionScreenshotImageCodec.ValidateBitmap(broken), "pixel bomb");
        using (var cts = new CancellationTokenSource())
        { cts.Cancel(); Reject(() => MissionScreenshotImageCodec.ToPng(valid, cts.Token), "cancel before decode"); }
        string file = Path.Combine(_root, "writer.bmp");
        using (var writer = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.None))
        using (var cts = new CancellationTokenSource(500))
        {
            var read = MissionScreenshotImageCodec.ReadCompleteAsync(file, cts.Token);
            writer.Write(valid, 0, valid.Length); writer.Flush(); Thread.Sleep(60);
            Check(!read.IsCompleted, "do not accept open native writer");
            writer.Dispose(); Check(read.GetAwaiter().GetResult().SequenceEqual(valid), "accept complete exclusive BMP only");
        }
        using (var cts = new CancellationTokenSource(70))
            Reject(() => MissionScreenshotImageCodec.ReadCompleteAsync(Path.Combine(_root, "absent.bmp"), cts.Token).GetAwaiter().GetResult(), "missing file has bounded cancellation");
        Check(MissionScreenshotImageCodec.SameImage(valid, valid), "duplicate image detected");
        var refs = new[] { new IllustrationReferenceImage("encodedA", "A", IllustrationReferenceKind.MissionScreenshot), new IllustrationReferenceImage("encodedB", "B", IllustrationReferenceKind.MissionScreenshot) };
        MissionScreenshotRules.RequireReferences(refs);
        Reject(() => MissionScreenshotRules.RequireReferences(null), "screenshots mandatory");
        Reject(() => MissionScreenshotRules.RequireReferences(new[] { refs[0] }), "one screenshot rejected");
        Reject(() => MissionScreenshotRules.RequireReferences(new[] { refs[0], refs[1], refs[0] }), "third reference rejected");
        Reject(() => MissionScreenshotRules.RequireReferences(new[] { refs[0], new IllustrationReferenceImage("x", "portrait", IllustrationReferenceKind.Character) }), "portrait/panorama cannot replace screenshot");
        Check(MissionScreenshotRules.IsMode(MissionScreenshotRules.BattleMode) && !MissionScreenshotRules.IsMode("百科肖像"), "mode isolation");
    }
    public static int Main()
    {
        _root = Path.GetFullPath(Path.Combine("artifacts", "mission-screenshots-20261008", "fixture-" + Guid.NewGuid().ToString("N")));
        IllustratorStoragePaths.TempDirectory = _root; Directory.CreateDirectory(_root);
        try
        {
            CodecCases(); CaptureCases();
            Console.WriteLine("PASS " + _assertions + " assertions; production capture/codec/rules, fake native camera/export only. GPU, HUD and real frame correspondence NOT-RUN."); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        finally { Directory.Delete(_root, true); }
    }
}
