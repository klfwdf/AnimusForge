using AnimusForge;
using AnimusForge.Illustrator.Engine;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;

internal static class SpriteLifetimeReplay
{
    internal static void Run(Action<bool, string> check)
    {
        string directory = Path.Combine(Path.GetTempPath(), "af-sprite-test-" + Guid.NewGuid().ToString("N"));
        AnimusForgeModulePaths.Root = directory;
        string images = Path.Combine(directory, "GUI", "SpriteParts", "af_diplomacy_event");
        Directory.CreateDirectory(images);
        foreach (string id in DiplomacyEventIllustrations.Entries.Select(x => x.Id).Append("event-chrome"))
            File.WriteAllBytes(Path.Combine(images, id + ".png"), new byte[32]);
        int mainThread = Environment.CurrentManagedThreadId;
        try
        {
            ImagePayload.Gate.Reset();
            check(!DiplomacyEventSprites.Prepare("event-chrome"), "First demand must return pending.");
            check(ImagePayload.Entered.Wait(5000), "Worker did not start.");
            for (int i = 0; i < 100; i++) check(!DiplomacyEventSprites.Prepare("event-chrome"), "Pending worker blocked or uploaded.");
            check(ImagePayload.ThreadId != mainThread && Texture.Created == 0, "Decoder must run off-thread; native upload must wait.");
            ImagePayload.Gate.Set();
            Ready("event-chrome");
            check(Texture.LastThread == mainThread && Texture.Created == 1, "Native upload must run once on caller thread.");
            check(DiplomacyEventSprites.Load("event-chrome").Length > 0, "Prepared sprite not registered.");
            foreach (string id in DiplomacyEventIllustrations.Entries.Take(7).Select(x => x.Id)) Ready(id);
            check(UIResourceManager.SpriteData.Sprites.Count == 5 && Texture.Released == 3, "LRU must retain only chrome plus four illustrations.");
            DiplomacyEventSprites.Clear();
            check(UIResourceManager.SpriteData.Sprites.Count == 0 && Texture.Released == Texture.Created, "End must remove and release owned sprites.");
            ImagePayload.Entered.Reset(); ImagePayload.Gate.Reset();
            DiplomacyEventSprites.Prepare("event-chrome");
            check(ImagePayload.Entered.Wait(5000), "Late worker did not start.");
            int count = Texture.Created;
            DiplomacyEventSprites.Clear();
            ImagePayload.Gate.Set();
            check(ImagePayload.Finished.Wait(5000), "Late worker did not finish.");
            check(Texture.Created == count && UIResourceManager.SpriteData.Sprites.Count == 0, "Retired worker republished native resources.");
        }
        finally { ImagePayload.Gate.Set(); DiplomacyEventSprites.Clear(); Directory.Delete(directory, true); }
    }
    private static void Ready(string id)
    {
        if (!SpinWait.SpinUntil(() => DiplomacyEventSprites.Prepare(id), 5000)) throw new Exception("Sprite readiness timeout: " + id);
    }
}

// Native rendering is intentionally faked; the production cache/worker/retirement code is linked above.
namespace AnimusForge
{
    internal static class AnimusForgeModulePaths { internal static string Root; internal static string GetCurrentModuleRoot() => Root; }
    internal static class Logger { internal static void Log(string category, string message) { } }
}
namespace AnimusForge.Illustrator.Engine
{
    internal static class ImagePayload
    {
        internal static readonly ManualResetEventSlim Gate = new(true), Entered = new(false), Finished = new(false);
        internal static int ThreadId;
        internal static byte[] Normalize(byte[] bytes)
        {
            Finished.Reset(); ThreadId = Environment.CurrentManagedThreadId; Entered.Set(); Gate.Wait(); Finished.Set(); return bytes;
        }
    }
}
namespace TaleWorlds.Library { public struct Vec2 { public static Vec2 Zero => default; public static Vec2 One => default; } }
namespace TaleWorlds.Engine
{
    public class Texture
    {
        public static int Created, Released, LastThread;
        public string Name; public int Width => 16; public int Height => 16;
        public static Texture CreateFromMemory(byte[] bytes) { Created++; LastThread = Environment.CurrentManagedThreadId; return new(); }
        public void SetTextureAsAlwaysValid() { } public void PreloadTexture(bool enabled) { }
        public void Release() { Released++; } public void ReleaseAfterNumberOfFrames(int frames) { Released++; }
    }
}
namespace TaleWorlds.Engine.GauntletUI
{
    public class EngineTexture { public EngineTexture(TaleWorlds.Engine.Texture texture) { } }
    public static class UIResourceManager { public static SpriteData SpriteData = new(); }
    public class SpriteData { public Dictionary<string, TaleWorlds.TwoDimension.Sprite> Sprites = new(); }
}
namespace TaleWorlds.TwoDimension
{
    public class Texture { public Texture(TaleWorlds.Engine.GauntletUI.EngineTexture texture) { } }
    public class SpriteNinePatchParameters { public static SpriteNinePatchParameters Empty = new(); }
    public abstract class Sprite
    {
        protected Sprite(string name, int width, int height, SpriteNinePatchParameters parameters) { }
        public abstract Texture Texture { get; }
        public abstract TaleWorlds.Library.Vec2 GetMinUvs(); public abstract TaleWorlds.Library.Vec2 GetMaxUvs();
    }
}
