using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using AnimusForge;

namespace TaleWorlds.Engine { internal static class TestNamespacePlaceholder { } }
namespace AnimusForge
{
    internal sealed class TestDropdown { internal int SelectedIndex; }
    internal sealed class DuelSettings
    {
        internal static DuelSettings Instance = new DuelSettings();
        internal TestDropdown RerankerDeviceDropdown = new TestDropdown();
    }
    internal static class AnimusForgeModulePaths
    {
        internal static string Root;
        internal static string GetCurrentModuleRoot() => Root;
    }
    internal static class Logger
    {
        internal static readonly List<string> Logs = new List<string>();
        public static void Log(string category, string message) { lock (Logs) Logs.Add(category + ": " + message); }
        public static void Metric(string name, bool ok, double milliseconds) { }
        public static void Obs(string category, string kind, Dictionary<string, object> fields) { }
    }
}

internal static class Program
{
    private static int _checks;
    private static void Check(bool ok, string label) { if (!ok) throw new Exception(label); _checks++; }
    private static object Field(object value, string name) => value.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(value);
    private static void ClearCache(OnnxCrossEncoderReranker engine) => ((Dictionary<string, float>)Field(engine, "_scoreCache")).Clear();

    private static int Main(string[] args)
    {
        // The production client always passes its parent PID. This executable doubles
        // as a deterministic broken-provider fixture, exercising real pipe/timeouts.
        if (args.Length == 1 && int.TryParse(args[0], out _)) return FakeWorker();
        try
        {
            if (args[0] == "faults") { Faults(); return 0; }
            string mode = args[0]; AnimusForgeModulePaths.Root = args[1];
            DuelSettings.Instance.RerankerDeviceDropdown.SelectedIndex = mode == "cpu" ? 0 : mode == "invalid" ? 9 : 1;
            var engine = OnnxCrossEncoderReranker.Instance;
            var initializers = Enumerable.Range(0, 4).Select(_ => Task.Run(() => engine.IsAvailable)).ToArray();
            Task.WaitAll(initializers);
            Check(initializers.All(t => t.Result), "concurrent initialization publishes only complete backend");
            bool gpu = mode == "gpu" || mode == "kill";
            Check((Field(engine, "_encodedBackend") != null) == gpu, "actual backend selection");
            if (gpu) Check(Field(engine, "_session") == null, "GPU success does not allocate a CPU model");
            var docs = new List<string> { "商队向城市运送粮食和木材。", "南部帝国的女皇统治着她的领地。", "村民在田地里收获小麦。", "骑兵可以快速到达战场。" };
            string query = "商队出售什么货物";
            var timer = Stopwatch.StartNew();
            Check(engine.TryScoreBatch(query, docs, out var scores) && scores.Count == docs.Count, "real production batch");
            timer.Stop();
            Check(engine.TryScore(query, docs[0], out var cached) && cached == scores[0], "batch cache reused by single-score path");
            Check(!engine.TryScore("", docs[0], out _) && !engine.TryScoreBatch(query, new string[0], out _), "empty input contract");
            // Capture source-tokenized rows for a separate graceful profiling process.
            var tokenizer = Field(engine, "_tokenizer"); var encode = tokenizer.GetType().GetMethod("EncodePair");
            object[] call = { query, docs[0], 512, null };
            var encoded = (List<long>)encode.Invoke(tokenizer, call);
            File.WriteAllText(args[2] + ".encoded.json", JsonConvert.SerializeObject(new { rows = new[] { encoded }, masks = new[] { (int[])call[3] } }));
            ClearCache(engine);
            var single = new List<float>();
            foreach (string doc in docs) { Check(engine.TryScore(query, doc, out float score), "real single-score path"); single.Add(score); }
            Check(scores.Zip(single, (a,b) => Math.Abs(a-b)).Max() <= 1e-5, "batch/single FP32 equivalence");
            if (gpu)
            {
                // Changed preference cannot replace an in-flight process or silently change cache semantics.
                DuelSettings.Instance.RerankerDeviceDropdown.SelectedIndex = 0;
                ClearCache(engine);
                var work = Enumerable.Range(0, 4).Select(i => Task.Run(() =>
                {
                    if (!engine.TryScore(query + i, docs[i], out float score) || float.IsNaN(score)) throw new Exception("concurrent score");
                })).ToArray();
                Task.WaitAll(work); Check(Field(engine, "_session") == null, "parallel CUDA calls remain GPU; restart selection frozen");
            }
            if (mode == "kill")
            {
                var client = Field(engine, "_encodedBackend"); var process = (Process)Field(client, "_process");
                process.Kill(); process.WaitForExit(5000); ClearCache(engine);
                Check(engine.TryScoreBatch(query, docs, out var fallback), "killed worker falls back on same request");
                Check(Field(engine, "_session") != null, "fallback creates real CPU session lazily");
                Check(scores.Zip(fallback, (a,b) => Math.Abs(a-b)).Max() <= 1e-5, "GPU/CPU fallback scores consistent");
                ClearCache(engine); Check(engine.TryScore(query, docs[0], out _), "subsequent requests stay usable on CPU");
                Check(Logger.Logs.Count(s => s.Contains("fallback=worker_inference_")) == 1, "failure logged once; no restart loop");
            }
            var warmMs = new List<double>();
            for (int i = 0; i < 5; i++)
            {
                ClearCache(engine); var sw = Stopwatch.StartNew();
                Check(engine.TryScoreBatch(query, docs, out _), "warm uncached request");
                warmMs.Add(sw.Elapsed.TotalMilliseconds);
            }
            var longDocs = docs.Select(d => string.Concat(Enumerable.Repeat(d, 180))).ToList();
            Check(engine.TryScoreBatch(query, longDocs, out var longScores), "512-token truncated production fixture");
            Check(engine.TryScoreBatch(query, new[] { "", docs[0] }, out var mixed) && mixed[0] == 0f, "empty candidate keeps original zero score and ordering");
            File.WriteAllText(args[2], JsonConvert.SerializeObject(new { mode, checks = _checks, scores, single, longScores, warmMs, elapsedMs = timer.Elapsed.TotalMilliseconds, logs = Logger.Logs }, Formatting.Indented));
            (Field(engine, "_encodedBackend") as IDisposable)?.Dispose();
            Console.WriteLine("PASS mode=" + mode + " checks=" + _checks); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }

    private static void Faults()
    {
        var rows = new List<List<long>> { new List<long> { 0, 2 } }; var masks = new List<int[]> { new[] { 1, 1 } };
        using (var stream = new MemoryStream())
        {
            var writer = new BinaryWriter(stream); RerankerWire.WriteRequest(writer, rows, masks); stream.Position = 0;
            RerankerWire.ReadRequest(new BinaryReader(stream), out var r, out var m);
            Check(r[0].SequenceEqual(rows[0]) && m[0].SequenceEqual(masks[0]), "wire roundtrip");
        }
        foreach (int count in new[] { -1, 0, 1025, int.MaxValue })
        {
            using (var stream = new MemoryStream(BitConverter.GetBytes(count)))
            { bool rejected = false; try { RerankerWire.ReadRequest(new BinaryReader(stream), out _, out _); } catch (InvalidDataException) { rejected = true; } Check(rejected, "bounded wire allocation"); }
        }
        string self = Assembly.GetExecutingAssembly().Location;
        foreach (string mode in new[] { "startup-crash", "startup-timeout", "wrong-version" })
        {
            Environment.SetEnvironmentVariable("AF_TEST_WORKER_CASE", mode);
            bool rejected = false; try { using (RerankerCudaProcess.Start(self, "fixture", _ => { }, 1000, 1000)) { } } catch { rejected = true; }
            Check(rejected, mode);
        }
        foreach (string mode in new[] { "crash", "timeout", "nan", "count" })
        {
            Environment.SetEnvironmentVariable("AF_TEST_WORKER_CASE", mode);
            var logs = new List<string>();
            using (var client = RerankerCudaProcess.Start(self, "fixture", logs.Add, 5000, 500))
            {
                Check(!client.TryScore(rows, masks, out _), "reject " + mode);
                Check(!client.TryScore(rows, masks, out _), "latched CPU fallback " + mode);
                Check(logs.Count(x => x.Contains("fallback=")) == 1, "bounded log " + mode);
            }
        }
        Environment.SetEnvironmentVariable("AF_TEST_WORKER_CASE", "good");
        using (var client = RerankerCudaProcess.Start(self, "fixture", _ => { }, 5000, 1000))
        {
            var calls = Enumerable.Range(0, 8).Select(_ => Task.Run(() => { if (!client.TryScore(rows, masks, out var s) || s[0] != .25f) throw new Exception("interleaved pipe frames"); })).ToArray();
            Task.WaitAll(calls); Check(true, "concurrent pipe transactions serialized");
            client.Dispose(); Check(!client.TryScore(rows, masks, out _), "disposed backend fails closed");
        }
        Environment.SetEnvironmentVariable("AF_TEST_WORKER_CASE", null);
        Console.WriteLine("PASS fault checks=" + _checks);
    }

    private static int FakeWorker()
    {
        string mode = Environment.GetEnvironmentVariable("AF_TEST_WORKER_CASE");
        if (mode == "startup-crash") return 1;
        if (mode == "startup-timeout") { Thread.Sleep(30000); return 1; }
        using (var reader = new BinaryReader(Console.OpenStandardInput(), Encoding.UTF8))
        using (var writer = new BinaryWriter(Console.OpenStandardOutput(), Encoding.UTF8))
        {
            reader.ReadInt32(); reader.ReadString();
            writer.Write(mode == "wrong-version" ? 999 : RerankerWire.Version); writer.Write("CUDA_FP32"); writer.Flush();
            try
            {
                while (true)
                {
                    RerankerWire.ReadRequest(reader, out var rows, out _);
                    if (mode == "crash") return 1;
                    if (mode == "timeout") Thread.Sleep(30000);
                    writer.Write(mode == "count" ? rows.Count + 1 : rows.Count);
                    foreach (var row in rows) writer.Write(mode == "nan" ? float.NaN : .25f);
                    writer.Flush();
                }
            }
            catch (EndOfStreamException) { return 0; }
        }
    }
}
