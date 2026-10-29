using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using EngineTextureResource = TaleWorlds.Engine.Texture;
using UiTexture = TaleWorlds.TwoDimension.Texture;
using UiSprite = TaleWorlds.TwoDimension.Sprite;

namespace AnimusForge;

// Main-thread cache ownership; one detached file/decoder job at a time, only on popup demand.
internal static class DiplomacyEventSprites
{
    private const string Category = "af_diplomacy_event";
    private const int Capacity = 5;
    private static readonly Dictionary<string, Cached> Cache = new Dictionary<string, Cached>(StringComparer.Ordinal);
    private static readonly HashSet<string> Failures = new HashSet<string>(StringComparer.Ordinal);
    private static long _use;
    private static Task<byte[]> _preparation;
    private static string _preparingId;

    // False means the caller must retain the announcement and try on a later tick.
    // Never wait on a worker, and never upload two native textures in the same attempt.
    internal static bool Prepare(string id)
    {
        if (id != "event-chrome" && !DiplomacyEventIllustrations.IsKnown(id)) return true;
        if (Cache.ContainsKey(id) || Failures.Contains(id)) return true;
        if (_preparation == null)
        {
            string path = Path.Combine(AnimusForgeModulePaths.GetCurrentModuleRoot(), "GUI", "SpriteParts", Category, id + ".png");
            _preparingId = id;
            _preparation = Task.Run(() => ReadTextureBytes(path));
            return false;
        }
        if (!_preparation.IsCompleted) return false;
        Task<byte[]> completed = _preparation;
        string preparedId = _preparingId;
        _preparation = null;
        _preparingId = null;
        try { Upload(preparedId, completed.GetAwaiter().GetResult()); }
        catch (Exception ex)
        {
            if (Failures.Add(preparedId)) Logger.Log("DiplomacyEventPopup", "Texture unavailable id=" + preparedId + " error=" + ex.Message);
        }
        return false;
    }

    internal static string Load(string id)
    {
        if (id != "event-chrome" && !DiplomacyEventIllustrations.IsKnown(id)) return "";
        string name = Category + "\\" + id;
        if (UIResourceManager.SpriteData == null) return "";
        if (Cache.TryGetValue(id, out Cached cached))
        {
            cached.LastUse = ++_use;
            UIResourceManager.SpriteData.Sprites[name] = cached.Sprite;
            return name;
        }
        return "";
    }

    private static void Upload(string id, byte[] bytes)
    {
        string name = Category + "\\" + id;
        EngineTextureResource texture = null;
        var watch = Stopwatch.StartNew();
        try
        {
            texture = EngineTextureResource.CreateFromMemory(bytes);
            if (texture == null) throw new InvalidDataException("Native texture loader returned null.");
            texture.Name = name;
            texture.SetTextureAsAlwaysValid();
            texture.PreloadTexture(true);
            var sprite = new EventSprite(name, new UiTexture(new EngineTexture(texture)),
                texture.Width > 0 ? texture.Width : 1024, texture.Height > 0 ? texture.Height : 1024);
            if (Cache.Count >= Capacity) EvictOldest();
            Cache.Add(id, new Cached(texture, sprite, ++_use));
            UIResourceManager.SpriteData.Sprites[name] = sprite;
        }
        catch (Exception ex)
        {
            try { texture?.Release(); } catch { }
            if (Failures.Add(id)) Logger.Log("DiplomacyEventPopup", "Texture unavailable id=" + id + " error=" + ex.Message);
        }
        finally { Logger.Log("DiplomacyEventPopup", "Texture upload id=" + id + " elapsedMs=" + watch.Elapsed.TotalMilliseconds.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)); }
    }

    internal static void Clear()
    {
        // Detached workers never publish. Observe a late failure without retaining a campaign or UI.
        Task<byte[]> retired = _preparation;
        _preparation = null;
        _preparingId = null;
        if (retired != null) retired.ContinueWith(t => { var ignored = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
        foreach (var pair in Cache)
        {
            string name = Category + "\\" + pair.Key;
            if (UIResourceManager.SpriteData != null && UIResourceManager.SpriteData.Sprites.TryGetValue(name, out UiSprite registered)
                && ReferenceEquals(registered, pair.Value.Sprite)) UIResourceManager.SpriteData.Sprites.Remove(name);
            try { pair.Value.Texture.ReleaseAfterNumberOfFrames(3); }
            catch (Exception ex) { Logger.Log("DiplomacyEventPopup", "Texture release failed id=" + pair.Key + " error=" + ex.Message); }
        }
        Cache.Clear();
        Failures.Clear();
        _use = 0;
    }

    internal static byte[] ReadTextureBytes(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("Bundled event illustration is missing.", path);
        if (new FileInfo(path).Length > 8 * 1024 * 1024) throw new InvalidDataException("Event texture exceeds the asset budget.");
        // RGB PNGs take the native decoder branch with the observed red/blue swap.
        // Reuse the pixel-preserving RGBA8 encoding contract; never tint or swap pixels.
        return Illustrator.Engine.ImagePayload.Normalize(File.ReadAllBytes(path));
    }

    private static void EvictOldest()
    {
        string oldest = null;
        long use = long.MaxValue;
        foreach (var pair in Cache)
            if (pair.Key != "event-chrome" && pair.Value.LastUse < use) { oldest = pair.Key; use = pair.Value.LastUse; }
        if (oldest == null) return;
        Cached victim = Cache[oldest];
        Cache.Remove(oldest);
        string name = Category + "\\" + oldest;
        if (UIResourceManager.SpriteData.Sprites.TryGetValue(name, out UiSprite registered) && ReferenceEquals(registered, victim.Sprite))
            UIResourceManager.SpriteData.Sprites.Remove(name);
        // Only called before opening a new movie, after the previous popup has released its movie.
        victim.Texture.ReleaseAfterNumberOfFrames(3);
    }

    private sealed class Cached
    {
        internal Cached(EngineTextureResource texture, UiSprite sprite, long use) { Texture = texture; Sprite = sprite; LastUse = use; }
        internal readonly EngineTextureResource Texture;
        internal readonly UiSprite Sprite;
        internal long LastUse;
    }
    private sealed class EventSprite : UiSprite
    {
        private readonly UiTexture _texture;
        internal EventSprite(string name, UiTexture texture, int width, int height)
            : base(name, width, height, TaleWorlds.TwoDimension.SpriteNinePatchParameters.Empty) { _texture = texture; }
        public override UiTexture Texture => _texture;
        public override Vec2 GetMinUvs() => Vec2.Zero;
        public override Vec2 GetMaxUvs() => Vec2.One;
    }
}
