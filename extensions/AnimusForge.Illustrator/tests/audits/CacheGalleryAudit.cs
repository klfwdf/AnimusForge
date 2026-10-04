using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

// Exercises the built implementation with a private on-disk cache and queued UI completions.
// No campaign, player cache, native texture, network or GPU is used.
public static class CacheGalleryAudit
{
    private const BindingFlags AllStatic = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
    private const BindingFlags AllInstance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    private static int _checks;
    private static Type _cache, _texture, _prepared;
    private static string _samplePath;
    private static readonly ConcurrentQueue<Action> Completions = new ConcurrentQueue<Action>();
    private static readonly ConcurrentQueue<string> Reads = new ConcurrentQueue<string>();
    private static readonly List<string> Displayed = new List<string>();
    private static readonly List<string> Errors = new List<string>();
    private static ManualResetEventSlim _started, _release;
    private static string _blockedPath;
    private static int _readers, _maxReaders;
    private static int _completedPosts;
    private static bool _current;

    private static void Check(bool passed, string description)
    {
        if (!passed) throw new Exception("FAIL " + description);
        _checks++;
        Console.WriteLine("PASS " + description);
    }

    private static object Call(Type type, string name, params object[] arguments)
    {
        MethodInfo method = type.GetMethod(name, AllStatic);
        ParameterInfo[] parameters = method.GetParameters();
        object[] complete = new object[parameters.Length];
        for (int i = 0; i < complete.Length; i++) complete[i] = i < arguments.Length ? arguments[i] : parameters[i].DefaultValue;
        return method.Invoke(null, complete);
    }

    private static object Property(object instance, string name)
    { return instance.GetType().GetProperty(name, AllInstance).GetValue(instance, null); }
    private static string Key(object instance) { return (string)Property(instance, "Key"); }
    private static string PathOf(object instance) { return (string)Property(instance, "FilePath"); }
    private static object Save(string subject, byte[] bytes, string category, string campaign, bool makeDefault)
    { return Call(_cache, "SaveImage", subject, bytes, "image prompt", "original title", category, campaign, 100, makeDefault, false, "theme", "action", "diag-id", "truncated", "导演输出截断，使用本地构图", "length"); }
    private static object Load(string subject, string category, string campaign)
    { return Call(_cache, "LoadImage", subject, campaign, category); }
    private static IList List(string campaign, bool refresh)
    { return (IList)Call(_cache, "GetAllCachedIllustrations", campaign, refresh); }

    private static byte[] MakeColors(bool rgb)
    {
        Color[] colors = { Color.Red, Color.Blue, Color.Gold, Color.FromArgb(235, 181, 146), Color.Purple, Color.FromArgb(rgb ? 255 : 128, 10, 50, 190) };
        using (var bitmap = new Bitmap(colors.Length, 1, rgb ? PixelFormat.Format24bppRgb : PixelFormat.Format32bppArgb))
        using (var output = new MemoryStream())
        {
            for (int i = 0; i < colors.Length; i++) bitmap.SetPixel(i, 0, colors[i]);
            bitmap.Save(output, ImageFormat.Png);
            return output.ToArray();
        }
    }

    private static void ComparePixels(byte[] original, byte[] actual, string label)
    {
        using (var input = new MemoryStream(original))
        using (var output = new MemoryStream(actual))
        using (var before = new Bitmap(input))
        using (var after = new Bitmap(output))
            for (int x = 0; x < before.Width; x++)
                Check(before.GetPixel(x, 0).ToArgb() == after.GetPixel(x, 0).ToArgb(), label + " preserves exact RGBA swatch " + x);
    }

    private static byte[] Encoded(object prepared)
    { return (byte[])_prepared.GetField("_encoded", AllInstance).GetValue(prepared); }

    private static IEnumerable<MethodBase> Calls(MethodBase method)
    {
        byte[] code = method.GetMethodBody().GetILAsByteArray();
        var opcodes = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(OpCode)).Select(field => (OpCode)field.GetValue(null)).ToDictionary(op => op.Value);
        for (int offset = 0; offset < code.Length; )
        {
            short value = code[offset++];
            if (value == 0xfe) value = (short)(0xfe00 | code[offset++]);
            OpCode op = opcodes[value];
            int size;
            switch (op.OperandType)
            {
                case OperandType.InlineNone: size = 0; break;
                case OperandType.ShortInlineBrTarget:
                case OperandType.ShortInlineI:
                case OperandType.ShortInlineVar: size = 1; break;
                case OperandType.InlineVar: size = 2; break;
                case OperandType.InlineI8:
                case OperandType.InlineR: size = 8; break;
                case OperandType.InlineSwitch: size = 4 + BitConverter.ToInt32(code, offset) * 4; break;
                default: size = 4; break;
            }
            if (op.OperandType == OperandType.InlineMethod)
                yield return method.Module.ResolveMethod(BitConverter.ToInt32(code, offset));
            offset += size;
        }
    }

    public static object PrepareForTest(string path, CancellationToken token)
    {
        int readers = Interlocked.Increment(ref _readers);
        if (readers > _maxReaders) _maxReaders = readers;
        Reads.Enqueue(path);
        try
        {
            if (path == _blockedPath)
            {
                _started.Set();
                if (!_release.Wait(5000)) throw new TimeoutException("test gate");
            }
            // A decoder can finish after cancellation. Production must reject that result too.
            return Call(_texture, "ReadPreparedImageForUi", _samplePath, CancellationToken.None);
        }
        finally { Interlocked.Decrement(ref _readers); }
    }

    public static void Display(string label, object prepared) { Displayed.Add(label); }

    private static Delegate TypedReader(Type delegateType)
    {
        var path = Expression.Parameter(typeof(string), "path");
        var token = Expression.Parameter(typeof(CancellationToken), "token");
        var body = Expression.Convert(Expression.Call(typeof(CacheGalleryAudit).GetMethod("PrepareForTest"), path, token), _prepared);
        return Expression.Lambda(delegateType, body, path, token).Compile();
    }

    private static void Select(object loader, string label)
    {
        MethodInfo select = loader.GetType().GetMethod("Select", AllInstance);
        Type callbackType = select.GetParameters()[1].ParameterType;
        var image = Expression.Parameter(_prepared, "image");
        var callback = Expression.Lambda(callbackType,
            Expression.Call(typeof(CacheGalleryAudit).GetMethod("Display"), Expression.Constant(label), Expression.Convert(image, typeof(object))), image).Compile();
        select.Invoke(loader, new object[] { label, callback, new Action<string>(text => Errors.Add(text)) });
    }

    private static void PumpUntil(Func<bool> done)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(5);
        while (!done())
        {
            Action action;
            while (Completions.TryDequeue(out action)) action();
            if (done()) return;
            if (DateTime.UtcNow > deadline) throw new TimeoutException("UI completion did not arrive");
            Thread.Sleep(5);
        }
    }

    private static object NewLoader(Type loaderType)
    {
        ConstructorInfo constructor = loaderType.GetConstructors(AllInstance).Single();
        return constructor.Invoke(new object[] {
            new Func<bool>(() => _current), new Action<Action>(action => Completions.Enqueue(() => { try { action(); } finally { _completedPosts++; } })),
            TypedReader(constructor.GetParameters()[2].ParameterType)
        });
    }

    private static void Block(string label)
    {
        _blockedPath = label;
        _started = new ManualResetEventSlim(false);
        _release = new ManualResetEventSlim(false);
    }

    private static void Close(object loader)
    { loader.GetType().GetMethod("Dispose", AllInstance).Invoke(loader, null); }

    public static void Run(string dllPath)
    {
        _checks = 0;
        Assembly assembly = Assembly.LoadFrom(dllPath);
        _cache = assembly.GetType("AnimusForge.Illustrator.Engine.DiskImageCacheManager", true);
        _texture = assembly.GetType("AnimusForge.Illustrator.Engine.GauntletTextureLoader", true);
        _prepared = _texture.GetNestedType("PreparedImage", BindingFlags.NonPublic);
        string fixture = Path.Combine(Path.GetDirectoryName(dllPath), "cache-gallery-audit-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(fixture);
        _cache.GetField("CacheBaseDir", AllStatic).SetValue(null, fixture);
        Call(_cache, "InvalidateCache");

        byte[] rgba = MakeColors(false);
        byte[] rgb = MakeColors(true);
        object named = Call(_cache, "SaveImage", "readable", rgba, "提示词", "人物立绘：塔洛斯/会面", "encyclopedia", "campaign-readable", 100, true);
        Check(named != null && Path.GetFileName(PathOf(named)).StartsWith("人物立绘：塔洛斯_会面_"), "Chinese title becomes a readable safe filename");
        Check(!Path.GetFileNameWithoutExtension(PathOf(named)).StartsWith(Key(named)), "storage filename is independent from stable image key");
        Check(Key(Load(Key(named), "encyclopedia", "campaign-readable")) == Key(named), "readable filename remains retrievable by stable key");
        Check(((string)Call(_cache, "ReadableFileLabel", "../" + new string('字', 90))).Length == 48, "filename label is bounded and excludes directory separators");
        Check((string)Call(_cache, "ReadableFileLabel", "... ") == "illustration", "empty filename after sanitization gets a valid label");
        string legacyPath = Path.Combine(Path.GetDirectoryName(PathOf(named)), Key(named) + ".png");
        File.Move(PathOf(named), legacyPath);
        File.Move(Path.ChangeExtension(PathOf(named), ".json"), Path.ChangeExtension(legacyPath, ".json"));
        Call(_cache, "InvalidateCache");
        Check(PathOf(Load(Key(named), "encyclopedia", "campaign-readable")) == legacyPath, "old hash-named image and metadata load without migration");
        Type paths = assembly.GetType("AnimusForge.AnimusForgeModulePaths", true);
        string moduleFixture = Path.Combine(fixture, "Modules", "AnimusForge");
        Directory.CreateDirectory(Path.Combine(moduleFixture, "ModuleData"));
        File.WriteAllText(Path.Combine(moduleFixture, "SubModule.xml"), "<Module />");
        foreach (string api in new[] { "1.3", "1.4" })
            Check((string)Call(paths, "ResolveModuleRootFromAssemblyDir", Path.Combine(moduleFixture, "bin", "Win64_Shipping_Client", "versions", api)) == moduleFixture, "production paths resolve module root from version " + api);
        Check((string)Call(paths, "GetLogsDirectory") == Path.Combine((string)Call(paths, "GetCurrentModuleRoot"), "logs"), "production log entry is under module logs, not player data");
        Check(rgba[25] == 6 && rgb[25] == 2, "fixture covers both RGBA8 and RGB8 PNG producers");
        _samplePath = Path.Combine(fixture, "preview.png");
        File.WriteAllBytes(_samplePath, rgba);
        object first = Save("same-subject", rgba, "encyclopedia", "campaign-a", true);
        Check(first != null, "cache image saved in isolated fixture");
        Thread.Sleep(2);
        object second = Save("same-subject", rgba, "encyclopedia", "campaign-a", false);
        object conversation = Save("same-subject", rgba, "conversation", "campaign-a", true);
        object loaded = Load("same-subject", "encyclopedia", "campaign-a");
        Check(Key(loaded) == Key(first), "default beats newer unpromoted image in subject index");
        Check(Key(Load(Key(second), "encyclopedia", "campaign-a")) == Key(second), "individual image key lookup remains supported");
        Check(Key(Load("same-subject", "conversation", "campaign-a")) == Key(conversation), "same subject never crosses category boundary");
        Check(Key(Load("same-subject", null, "campaign-a")) == Key(first), "uncategorized lookup retains established category priority");
        Check((string)Property(loaded, "DiagnosticId") == "diag-id" && (string)Property(loaded, "DirectorFallbackReason") == "length", "diagnostic id and fallback reason survive actual cache save/load");
        Check(((string)Property(loaded, "DisplayStatusText")).Contains("输出截断"), "cached degraded direction is visible with theme");
        loaded.GetType().GetProperty("DirectorStatus").SetValue(loaded, "complete", null);
        Check((string)Property(loaded, "DisplayStatusText") == "主题：theme", "complete direction adds no degradation notice");

        string firstMetadata = Path.ChangeExtension(PathOf(first), ".json");
        File.WriteAllText(firstMetadata, File.ReadAllText(firstMetadata).Replace("original title", "external title"));
        Check((string)Property(Load("same-subject", "encyclopedia", "campaign-a"), "Title") == "original title", "repeated LoadImage uses in-memory metadata instead of rescanning JSON");
        List("campaign-a", true);
        Check((string)Property(Load("same-subject", "encyclopedia", "campaign-a"), "Title") == "external title", "explicit refresh rebuilds metadata index");
        IList copied = List("campaign-a", false);
        object changed = copied[0];
        changed.GetType().GetProperty("SubjectKey").SetValue(changed, "tampered", null);
        Check(Load("tampered", null, "campaign-a") == null, "returned metadata cannot mutate the shared index");
        Check(((IEnumerable)_cache.GetField("_cachedIllustrations", AllStatic).GetValue(null)).Cast<object>().All(item => Property(item, "ImageData") == null), "metadata index retains no decoded image bytes");

        object otherCampaign = Save("same-subject", rgba, "encyclopedia", "campaign-b", true);
        Check(Key(Load("same-subject", "encyclopedia", "campaign-b")) == Key(otherCampaign), "same subject in another save is isolated");
        Check(Key(Load("same-subject", "encyclopedia", "campaign-a")) == Key(first), "switching back rebuilds the original campaign index");
        Check((bool)Call(_cache, "SetDefault", second, "campaign-a"), "set-default persists pointer");
        Check(Key(Load("same-subject", "encyclopedia", "campaign-a")) == Key(second), "set-default invalidates cached selection");
        Check(!(bool)Call(_cache, "DeleteItem", second, "campaign-b"), "cross-campaign deletion is rejected");
        Check((bool)Call(_cache, "DeleteItem", second, "campaign-a"), "delete moves selected image into recycle area");
        Check(Key(Load("same-subject", "encyclopedia", "campaign-a")) == Key(first), "delete invalidates index and falls back to retained image");
        string trash = Path.Combine(fixture, "_trash", "campaign-a");
        File.Move(Path.Combine(trash, Path.GetFileName(PathOf(second))), PathOf(second));
        File.Move(Path.Combine(trash, Path.GetFileName(Path.ChangeExtension(PathOf(second), ".json"))), Path.ChangeExtension(PathOf(second), ".json"));
        Check(Key(Load("same-subject", "encyclopedia", "campaign-a")) == Key(second), "directory timestamp detects manual restore on next subject lookup without full-refresh request");
        string externalImage = Path.Combine(Path.GetDirectoryName(PathOf(second)), "external-image.png");
        File.WriteAllBytes(externalImage, rgba);
        Check(Load("external-image", "encyclopedia", "campaign-a") != null, "directory timestamp detects new external image on LoadImage");
        File.Delete(externalImage);
        Check(!List("campaign-a", false).Cast<object>().Any(item => Key(item) == "external-image"), "directory timestamp detects external deletion on gallery query");
        string secondMetadata = Path.ChangeExtension(PathOf(second), ".json");
        File.WriteAllText(secondMetadata, System.Text.RegularExpressions.Regex.Replace(File.ReadAllText(secondMetadata), @"\s*""(?:DiagnosticId|DirectorStatus|DirectorStatusText|DirectorFallbackReason)""\s*:\s*""[^""]*"",?", ""));
        Call(_cache, "InvalidateCache");
        Check((string)Property(Load("same-subject", "encyclopedia", "campaign-a"), "DirectorStatus") == string.Empty, "legacy cache without diagnostics remains readable");
        File.WriteAllText(firstMetadata, File.ReadAllText(firstMetadata).Replace("\"Deleted\": false", "\"Deleted\": true"));
        List("campaign-a", true);
        Check(Load(Key(first), "encyclopedia", "campaign-a") == null, "deleted metadata is not resurrected as an orphan PNG");

        Check(_prepared.GetConstructors(AllInstance).All(ctor => ctor.IsPrivate), "PreparedImage has no unchecked constructor available to callers");
        ConstructorInfo preparedConstructor = _prepared.GetConstructors(AllInstance).Single();
        Check(Calls(preparedConstructor).Count(method => method.Name == "PrepareEncodedImageForUi") == 1, "actual PreparedImage constructor calls fixed color preparation once");
        Check(Calls(_texture.GetMethod("LoadOrRegisterPngBytes", AllStatic)).Any(method => method.DeclaringType == _prepared && method.Name == "FromBytes"), "legacy public UI loader uses validated PreparedImage factory");
        Check(Calls(_texture.GetMethod("ReadPreparedImageForUi", AllStatic)).Any(method => method.DeclaringType == _prepared && method.Name == "FromFile"), "background file preparation uses validated factory");
        Check(Calls(_prepared.GetMethod("FromFile", AllStatic)).Any(method => method == preparedConstructor) && Calls(_prepared.GetMethod("FromFile", AllStatic)).Any(method => method.Name == "ReadEncodedFile"), "file preparation reads once and calls the same tested color constructor");
        Check(Calls(_texture.GetMethod("LoadOrRegisterPreparedImage", AllStatic)).Any(method => method.DeclaringType == _prepared && method.Name == "Register"), "gallery registration consumes only prepared images");
        Check(Calls(_prepared.GetMethod("Register", AllInstance)).Any(method => method.Name == "RegisterPreparedBytes"), "prepared image directly enters private texture registration");
        var registerCalls = Calls(_texture.GetMethod("RegisterPreparedBytes", AllStatic)).ToArray();
        Check(registerCalls.Any(method => method.Name == "CreateFromMemory") && !registerCalls.Any(method => method.Name == "Normalize" || method.Name == "PrepareEncodedImageForUi"), "GPU registration uses prepared bytes without a second GDI decode");
        foreach (bool isRgb in new[] { false, true })
        {
            byte[] original = isRgb ? rgb : rgba;
            object prepared = Call(_prepared, "FromBytes", original);
            byte[] encoded = Encoded(prepared);
            Check(encoded[24] == 8 && encoded[25] == 6 && encoded[28] == 0, "prepared format follows fixed RGBA8 contract: rgb=" + isRgb);
            ComparePixels(original, encoded, "first UI preparation rgb=" + isRgb);
            if (!isRgb) Check(original.SequenceEqual(encoded), "standard RGBA PNG bytes remain unchanged");
            string file = Path.Combine(fixture, isRgb ? "rgb.png" : "rgba.png");
            File.WriteAllBytes(file, original);
            object reopened = Call(_texture, "ReadPreparedImageForUi", file, CancellationToken.None);
            ComparePixels(original, Encoded(reopened), "cache reopen rgb=" + isRgb);
            Check(File.ReadAllBytes(file).SequenceEqual(original), "preparing old cache does not rewrite it: rgb=" + isRgb);
        }
        byte[] mutable = (byte[])rgba.Clone();
        object protectedImage = Call(_prepared, "FromBytes", mutable);
        mutable[0] = 0;
        Check(Encoded(protectedImage)[0] == 137, "caller cannot mutate a validated PreparedImage through its original array");
        using (var canceled = new CancellationTokenSource())
        {
            canceled.Cancel();
            bool rejected = false;
            try { Call(_texture, "ReadPreparedImageForUi", _samplePath, canceled.Token); }
            catch (TargetInvocationException ex) { rejected = ex.InnerException is OperationCanceledException; }
            Check(rejected, "canceled image preparation stops before reading");
        }

        Type loaderType = assembly.GetType("AnimusForge.Illustrator.Engine.GalleryPreviewLoader", true);
        _current = true;
        object previewLoader = NewLoader(loaderType);
        Block("first");
        Select(previewLoader, "first");
        Check(_started.Wait(5000), "preview read runs in worker");
        Select(previewLoader, "skipped");
        Select(previewLoader, "latest");
        _release.Set();
        PumpUntil(() => Displayed.Contains("latest"));
        Check(Reads.ToArray().SequenceEqual(new[] { "first", "latest" }), "rapid selection coalesces pending reads to latest image");
        Check(Displayed.SequenceEqual(new[] { "latest" }) && Errors.Count == 0, "late canceled image never reaches GPU callback or failure UI");
        Check(_maxReaders == 1, "each gallery has at most one image decode in flight");

        Block("close");
        Select(previewLoader, "close");
        Check(_started.Wait(5000), "close test reaches active image read");
        int beforeClose = _completedPosts;
        Close(previewLoader);
        _release.Set();
        PumpUntil(() => _completedPosts > beforeClose);
        Check(!Displayed.Contains("close") && Errors.Count == 0, "closing gallery rejects late image completion");

        object obsoleteLoader = NewLoader(loaderType);
        Block("old-campaign");
        Select(obsoleteLoader, "old-campaign");
        Check(_started.Wait(5000), "campaign change test reaches active read");
        int beforeCampaign = _completedPosts;
        _current = false;
        _release.Set();
        PumpUntil(() => _completedPosts > beforeCampaign);
        Check(!Displayed.Contains("old-campaign"), "invalid scope rejects old-campaign image before texture registration");
        Close(obsoleteLoader);

        Console.WriteLine("CACHE/GALLERY AUDIT: " + _checks + " PASS / 0 FAIL; fixture=" + fixture);
    }
}
