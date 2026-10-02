using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.ML.OnnxRuntime;

namespace AnimusForge
{
    internal static class BenchmarkHooks
    {
        internal static string ModuleRoot;
        internal static string Backend;
        internal static string ProfilePrefix;
        internal static SessionOptions Options;
        internal static readonly List<string> Logs = new List<string>();
        internal static int SingleMetrics;
        internal static void Configure(SessionOptions options)
        {
            Options = options;
            if (Backend == "cuda")
            {
                var cuda = new OrtCUDAProviderOptions();
                cuda.UpdateOptions(new Dictionary<string, string> { { "device_id", "0" }, { "use_tf32", "0" } });
                options.AppendExecutionProvider_CUDA(cuda);
                cuda.Dispose();
            }
            if (!string.IsNullOrEmpty(ProfilePrefix))
            {
                options.EnableProfiling = true;
                options.ProfileOutputPathPrefix = ProfilePrefix;
            }
        }
    }
    internal static class AnimusForgeModulePaths
    {
        internal static string GetCurrentModuleRoot() => BenchmarkHooks.ModuleRoot;
    }
    internal static class Logger
    {
        public static void Log(string category, string text) { BenchmarkHooks.Logs.Add(category + ": " + text); }
        public static void Obs(string category, string kind, Dictionary<string, object> fields) { }
        public static void Metric(string name, bool ok, double milliseconds)
        {
            if (name == "onnx.rerank") BenchmarkHooks.SingleMetrics++;
            if (!ok) BenchmarkHooks.Logs.Add("METRIC_FAILURE: " + name);
        }
    }
    internal static class Program
    {
        private static Dictionary<string, string> Parse(string[] args)
        {
            var result = new Dictionary<string, string>();
            for (int i = 0; i < args.Length; i += 2) result.Add(args[i], args[i + 1]);
            return result;
        }
        private static JObject TimedCall(OnnxCrossEncoderReranker engine, string query, List<string> documents, Dictionary<string, float> cache, bool miss)
        {
            if (miss) cache.Clear(); // Outside measured interval; no production cache behavior is changed.
            int before = BenchmarkHooks.SingleMetrics;
            var watch = Stopwatch.StartNew();
            bool ok = engine.TryScoreBatch(query, documents, out List<float> scores);
            watch.Stop();
            if (!ok || scores.Count != documents.Count || scores.Any(x => float.IsNaN(x) || float.IsInfinity(x)))
                throw new InvalidOperationException("Batch failed or returned invalid scores.");
            int fallback = BenchmarkHooks.SingleMetrics - before;
            if (fallback != 0) throw new InvalidOperationException("Unexpected batch-to-single fallback; do not treat it as a batch benchmark.");
            return new JObject { ["ms"] = watch.Elapsed.TotalMilliseconds, ["scores"] = JArray.FromObject(scores), ["singleFallbackCalls"] = fallback };
        }
        private static int[] GetTokenLengths(object tokenizer, string query, List<string> docs)
        {
            var method = tokenizer.GetType().GetMethod("EncodePair", BindingFlags.Instance | BindingFlags.Public);
            return docs.Select(doc =>
            {
                object[] values = { query, doc, 512, null };
                return ((List<long>)method.Invoke(tokenizer, values)).Count;
            }).ToArray();
        }
        public static int Main(string[] args)
        {
            Dictionary<string, string> p = null;
            OnnxCrossEncoderReranker engine = null;
            InferenceSession session = null;
            var output = new JObject();
            try
            {
                p = Parse(args);
                BenchmarkHooks.ModuleRoot = p["--module-root"];
                BenchmarkHooks.Backend = p["--backend"];
                BenchmarkHooks.ProfilePrefix = p.ContainsKey("--profile-prefix") ? p["--profile-prefix"] : null;
                output["backend"] = BenchmarkHooks.Backend;
                output["pid"] = Process.GetCurrentProcess().Id;
                output["framework"] = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
                output["runtimeAssembly"] = typeof(InferenceSession).Assembly.FullName;
                output["runtimeAssemblyPath"] = typeof(InferenceSession).Assembly.Location;
                output["tf32"] = BenchmarkHooks.Backend == "cuda" ? (JToken)false : JValue.CreateNull();
                output["startedUtc"] = DateTime.UtcNow.ToString("o");
                var scenarios = JArray.Parse(File.ReadAllText(p["--dataset"]));
                var init = Stopwatch.StartNew();
                engine = OnnxCrossEncoderReranker.Instance;
                bool available = engine.IsAvailable;
                init.Stop();
                output["initializationMs"] = init.Elapsed.TotalMilliseconds;
                if (!available) throw new InvalidOperationException(engine.LastError);
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var type = engine.GetType();
                session = (InferenceSession)type.GetField("_session", flags).GetValue(engine);
                var cache = (Dictionary<string, float>)type.GetField("_scoreCache", flags).GetValue(engine);
                var tokenizer = type.GetField("_tokenizer", flags).GetValue(engine);
                output["modelPath"] = AnimusForgeModelStore.ResolveReranker().ModelPath;
                output["inputMetadata"] = JObject.FromObject(session.InputMetadata.ToDictionary(x => x.Key, x => new { elementType = x.Value.ElementType.FullName, dimensions = x.Value.Dimensions }));
                var results = new JArray();
                int repetitions = int.Parse(p["--repetitions"]);
                foreach (JObject item in scenarios)
                {
                    string id = (string)item["id"], query = (string)item["query"];
                    var docs = item["documents"].ToObject<List<string>>();
                    Console.WriteLine("CASE " + BenchmarkHooks.Backend + " " + id);
                    var result = new JObject { ["id"] = id, ["batchSize"] = docs.Count, ["tokenLengths"] = JArray.FromObject(GetTokenLengths(tokenizer, query, docs)) };
                    result["firstShapeMiss"] = TimedCall(engine, query, docs, cache, true);
                    for (int i = 0; i < 3; i++) TimedCall(engine, query, docs, cache, true);
                    var misses = new JArray();
                    for (int i = 0; i < repetitions; i++) misses.Add(TimedCall(engine, query, docs, cache, true));
                    result["cacheMiss"] = misses;
                    var hits = new JArray();
                    for (int i = 0; i < repetitions; i++) hits.Add(TimedCall(engine, query, docs, cache, false));
                    result["cacheHit"] = hits;
                    results.Add(result);
                }
                output["scenarios"] = results;
                output["processWorkingSetBytes"] = Process.GetCurrentProcess().WorkingSet64;
                if (BenchmarkHooks.ProfilePrefix != null) output["profilePath"] = session.EndProfiling();
                output["ok"] = true;
                return 0;
            }
            catch (Exception ex)
            {
                output["ok"] = false;
                output["error"] = ex.ToString();
                Console.Error.WriteLine(ex);
                return 1;
            }
            finally
            {
                if (session != null) session.Dispose();
                if (BenchmarkHooks.Options != null) BenchmarkHooks.Options.Dispose();
                output["logs"] = JArray.FromObject(BenchmarkHooks.Logs);
                output["endedUtc"] = DateTime.UtcNow.ToString("o");
                if (p != null && p.ContainsKey("--result")) File.WriteAllText(p["--result"], output.ToString(Formatting.Indented));
            }
        }
    }
}

// Production contains an unused using; this namespace supplies no game API.
namespace TaleWorlds.Engine { internal static class BenchmarkNamespaceMarker { } }
