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
    private static AnimusForge.Illustrator.UI.Overlays.MissionPhotoVM Photo =>
        (AnimusForge.Illustrator.UI.Overlays.MissionPhotoVM)_screen.Layers.OfType<TaleWorlds.Engine.GauntletUI.GauntletLayer>().Last().VM;
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
        _screen.CombatCamera.Frame = new MatrixFrame { origin = new Vec3(0, -4, 1.3f), rotation = new Mat3 { u = new Vec3(0, -1, 0) } };
        ScreenManager.TopScreen = _screen;ScreenManager.FocusedLayer=null;
        MBDebug.DisableAllUI = false;
        TaleWorlds.InputSystem.Input.Keys.Clear();TaleWorlds.InputSystem.Input.MouseMoveX=0;TaleWorlds.InputSystem.Input.MouseMoveY=0;TaleWorlds.InputSystem.Input.DeltaMouseScroll=0;
        int shots=0;Utilities.Export = path => File.WriteAllBytes(path, BitmapBytes(++shots == 1 ? Color.Red : Color.Blue));
    }
    private static void Key(MissionScreenshotCapture capture,TaleWorlds.InputSystem.InputKey key)
    {
        TaleWorlds.InputSystem.Input.Keys.Clear();capture.Tick();
        TaleWorlds.InputSystem.Input.Keys.Add(key);capture.Tick();
        TaleWorlds.InputSystem.Input.Keys.Clear();capture.Tick();
    }
    private static void Shoot(MissionScreenshotCapture capture)
    {
        Key(capture,TaleWorlds.InputSystem.InputKey.Enter);
        for(int i=0;i<500&&!Photo.ChoicesVisible&&!capture.Completion.IsCompleted;i++)
        {IllustratorRuntime.ApplicationFrame++;capture.Tick();Thread.Sleep(3);}
        Check(Photo.ChoicesVisible,"native export completes to explicit decision");
    }
    private static void Restored(bool paused,string label)
    {
        Check(_screen.CustomCamera==null&&_screen.CombatCamera.Position.y==-4,label+": camera restored");
        Check(!MBDebug.DisableAllUI,label+": UI restored");
        Check(_screen.LayerWatchers==0&&_screen.Layers.Count==0,label+": overlay/subscription removed");
        Check(MissionState.Current.Paused==paused&&_mission.Scene.TimeSpeed==(paused?0:0.75f),label+": original pause retained");
        Check(_mission.RequestCount==0&&Game.Current.GameStateManager.Count==(paused?1:0),label+": prior owners preserved");
        Check(Camera.Created==Camera.ReleasedCount,label+": native cameras released once");
    }
    private static void CaptureCases()
    {
        foreach(bool paused in new[]{false,true})
        {
            Setup(paused);int shots=0;Utilities.Export=path=>{shots++;Check(MBDebug.DisableAllUI,"hint/UI hidden during native export");File.WriteAllBytes(path,BitmapBytes(Color.Red));};
            var capture=new MissionScreenshotCapture(_mission,true);
            Check(_mission.Scene.TimeSpeed==0&&MissionState.Current.Paused,"freeze on entry");
            Check(Photo.Hint.Contains("Enter")&&!Photo.ChoicesVisible,"only Enter hint before screenshot");
            Reject(()=>new MissionScreenshotCapture(_mission,true),"capture gate serializes native resources");
            for(int i=0;i<10;i++){IllustratorRuntime.ApplicationFrame++;capture.Tick();}
            Check(shots==0&&!capture.Completion.IsCompleted,"no automatic capture or submit");
            Shoot(capture);Check(shots==1&&!capture.Completion.IsCompleted,"first shot still awaits confirmation");
            var submitVm=Photo;submitVm.ExecuteFirst();submitVm.ExecuteFirst();
            Check(capture.Completion.GetAwaiter().GetResult().Count==1,"one shot submits once without second reference");
            Restored(paused,"single submit");capture.Cancel("late");Restored(paused,"idempotent");
        }
        Setup();var pair=new MissionScreenshotCapture(_mission,true);Shoot(pair);Photo.ExecuteSecond();
        Check(!Photo.ChoicesVisible&&!pair.Completion.IsCompleted,"continue returns to free camera");
        TaleWorlds.InputSystem.Input.MouseMoveX=30;TaleWorlds.InputSystem.Input.MouseMoveY=20;TaleWorlds.InputSystem.Input.DeltaMouseScroll=120;
        Key(pair,TaleWorlds.InputSystem.InputKey.W);TaleWorlds.InputSystem.Input.MouseMoveX=0;TaleWorlds.InputSystem.Input.MouseMoveY=0;TaleWorlds.InputSystem.Input.DeltaMouseScroll=0;
        Check(_screen.CustomCamera.Frame.rotation.u.x!=0&&_screen.CustomCamera.Frame.rotation.u.z!=0,"player changes yaw and pitch freely");
        Check(_screen.CustomCamera.Fov!=0.7f,"scroll zoom applies only to photo camera");
        Shoot(pair);Check(Photo.SecondText=="重新截取","second shot cannot add a third");
        Photo.ExecuteFirst();var data=pair.Completion.GetAwaiter().GetResult();
        Check(data.Count==2&&!MissionScreenshotImageCodec.SameImage(data.Current,data.Reverse),"both player shots retained");Restored(false,"two submit");
        Setup();var retake=new MissionScreenshotCapture(_mission,true);Shoot(retake);Photo.ExecuteSecond();
        Key(retake,TaleWorlds.InputSystem.InputKey.Escape);
        Check(Photo.FirstText=="重新截取"&&Photo.SecondText=="取消"&&!retake.Completion.IsCompleted,"Esc presents restart/cancel rather than silently submitting");
        Photo.ExecuteFirst();Shoot(retake);Photo.ExecuteFirst();
        Check(retake.Completion.GetAwaiter().GetResult().Count==1,"retake starts again from first, discarding previous reference");Restored(false,"retake");
        Setup();var cancel=new MissionScreenshotCapture(_mission,true);Key(cancel,TaleWorlds.InputSystem.InputKey.Escape);Photo.ExecuteSecond();
        Check(cancel.Completion.IsCanceled,"explicit cancel yields no generation");Restored(false,"cancel");
        Setup();var exportCancel=new MissionScreenshotCapture(_mission,true);Key(exportCancel,TaleWorlds.InputSystem.InputKey.Enter);Key(exportCancel,TaleWorlds.InputSystem.InputKey.Escape);Photo.ExecuteSecond();
        Check(exportCancel.Completion.IsCanceled,"Esc works during export warmup");Restored(false,"export cancel");
        Setup();var oldUi=new TaleWorlds.Engine.GauntletUI.GauntletLayer();var hiddenUi=new TaleWorlds.Engine.GauntletUI.GauntletLayer();hiddenUi.UIContext.Root.IsVisible=false;
        _screen.AddLayer(oldUi);_screen.AddLayer(hiddenUi);ScreenManager.TrySetFocus(oldUi);
        var visibility=new MissionScreenshotCapture(_mission,true);
        Check(!oldUi.UIContext.Root.IsVisible&&!hiddenUi.UIContext.Root.IsVisible,"prior UI hidden without losing prior visibility");
        oldUi.UIContext.Root.IsVisible=true;visibility.Tick();
        Check(!oldUi.UIContext.Root.IsVisible,"later UI updates remain hidden during aiming");
        var addedUi=new TaleWorlds.Engine.GauntletUI.GauntletLayer();_screen.AddLayer(addedUi);
        Check(!addedUi.UIContext.Root.IsVisible,"layers added during capture are hidden");
        _screen.RemoveLayer(oldUi);oldUi.IsFinalized=false;_screen.AddLayer(oldUi);
        visibility.Cancel("test");Check(oldUi.UIContext.Root.IsVisible&&!hiddenUi.UIContext.Root.IsVisible&&ScreenManager.FocusedLayer==oldUi,"exact prior UI/focus restored");
        Check(addedUi.UIContext.Root.IsVisible,"newly added UI regains original visibility");
        Setup();var originUi=new TaleWorlds.Engine.GauntletUI.GauntletLayer();_screen.AddLayer(originUi);ScreenManager.TrySetFocus(originUi);
        var replacedScreen=new MissionScreenshotCapture(_mission,true);
        var nextScreen=new ScreenBase();var nextFocus=new TaleWorlds.Engine.GauntletUI.GauntletLayer();nextScreen.AddLayer(nextFocus);
        ScreenManager.TopScreen=nextScreen;ScreenManager.TrySetFocus(nextFocus);replacedScreen.Tick();
        Check(replacedScreen.Completion.IsFaulted&&ScreenManager.FocusedLayer==nextFocus,"leaving does not restore focus over the replacement screen");
        Check(originUi.UIContext.Root.IsVisible&&_screen.LayerWatchers==0&&_screen.Layers.Count==1,"leaving still restores origin UI and releases overlay");
        Setup();var beforeFocus=new TaleWorlds.Engine.GauntletUI.GauntletLayer();_screen.AddLayer(beforeFocus);ScreenManager.TrySetFocus(beforeFocus);
        var focusOwner=new MissionScreenshotCapture(_mission,true);var otherFocus=new TaleWorlds.Engine.GauntletUI.GauntletLayer();_screen.AddLayer(otherFocus);ScreenManager.TrySetFocus(otherFocus);
        focusOwner.Cancel("test");Check(ScreenManager.FocusedLayer==otherFocus,"another UI focus owner is not overwritten on cancel");
        Setup();var leaving=new MissionScreenshotCapture(_mission,true);Mission.Current=new Mission();leaving.Tick();
        Check(leaving.Completion.IsFaulted&&Mission.Current.Scene.TimeSpeed==1,"leaving aborts and leaves replacement scene alone");
        Check(_mission.RequestCount==0&&!MBDebug.DisableAllUI,"leaving releases own pause/UI");
        Setup();var takeover=new MissionScreenshotCapture(_mission,true);var other=_screen.CustomCamera=new Camera();takeover.Tick();
        Check(takeover.Completion.IsFaulted&&_screen.CustomCamera==other&&!other.Released,"camera takeover not overwritten");
        Setup();Utilities.Export=path=>{throw new IOException("export failed");};var failure=new MissionScreenshotCapture(_mission,true);
        Key(failure,TaleWorlds.InputSystem.InputKey.Enter);
        for(int i=0;i<150&&!failure.Completion.IsCompleted;i++){IllustratorRuntime.ApplicationFrame++;failure.Tick();Thread.Sleep(3);}
        Check(failure.Completion.IsFaulted,"export failure explicit");Restored(false,"export failure");
        Setup();Utilities.Export=path=>{};var timeout=new MissionScreenshotCapture(_mission,true);
        Key(timeout,TaleWorlds.InputSystem.InputKey.Enter);Thread.Sleep(8100);timeout.Tick();
        Check(timeout.Completion.IsFaulted,"shot deadline finite");Restored(false,"timeout");
        Setup();_screen.Rendered=false;Reject(()=>new MissionScreenshotCapture(_mission,true),"unrendered mission rejects before mutation");
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
        MissionScreenshotRules.RequireReferences(new[] { refs[0] });
        Check(true, "single reference accepted");
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
