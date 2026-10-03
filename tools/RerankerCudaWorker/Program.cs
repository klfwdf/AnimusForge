using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ML.OnnxRuntime;

namespace TaleWorlds.Engine { internal static class WorkerNamespacePlaceholder { } }

namespace AnimusForge
{
    internal static class AnimusForgeModulePaths
    {
        internal static string Root;
        internal static string GetCurrentModuleRoot() => Root;
    }

    // Worker assembly links production tokenizer, padding, scoring and model resolver verbatim.
    internal static class RerankerDeviceRuntime
    {
        internal static IRerankerEncodedBackend CreateBackend() => null;
        internal static InferenceSession CreateLocalSession(string modelPath, SessionOptions options)
        {
            if (!OrtEnv.Instance().GetAvailableProviders().Contains("CUDAExecutionProvider"))
                throw new InvalidOperationException("CUDA provider unavailable.");
            using (var cuda = new OrtCUDAProviderOptions())
            {
                cuda.UpdateOptions(new Dictionary<string, string> { { "device_id", "0" }, { "use_tf32", "0" } });
                options.AppendExecutionProvider_CUDA(cuda);
                // Optional offline profiling; never enabled by the game launcher.
                string profile = Environment.GetEnvironmentVariable("AF_RERANKER_WORKER_PROFILE");
                if (!string.IsNullOrEmpty(profile)) { options.EnableProfiling = true; options.ProfileOutputPathPrefix = profile; }
                Session = new InferenceSession(modelPath, options);
                return Session;
            }
        }
        internal static InferenceSession Session;
    }

    internal static class Logger
    {
        public static void Log(string category, string message) { Console.Error.WriteLine(category + ": " + message); }
        public static void Metric(string name, bool ok, double milliseconds) { }
        public static void Obs(string category, string kind, Dictionary<string, object> values) { }
    }

    internal static class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                if (args.Length != 1 || !int.TryParse(args[0], out int parentId)) return 2;
                var parent = Process.GetProcessById(parentId);
                // Blocking kernel wait, not a polling timer. Prevents an orphan GPU allocation
                // even when Bannerlord crashes while a native inference call is stuck.
                Task.Run(() => { using (parent) { parent.WaitForExit(); } Environment.Exit(0); });
                using (var input = new BinaryReader(Console.OpenStandardInput(), Encoding.UTF8))
                using (var output = new BinaryWriter(Console.OpenStandardOutput(), Encoding.UTF8))
                {
                    if (input.ReadInt32() != RerankerWire.Version) return 3;
                    AnimusForgeModulePaths.Root = input.ReadString();
                    var engine = OnnxCrossEncoderReranker.Instance;
                    if (!engine.IsAvailable) return 4;
                    output.Write(RerankerWire.Version); output.Write("CUDA_FP32"); output.Flush();
                    while (true)
                    {
                        RerankerWire.ReadRequest(input, out var rows, out var masks);
                        if (!engine.TryRunBatchEncoded(rows, masks, out var scores) || scores.Count != rows.Count) return 5;
                        output.Write(scores.Count);
                        foreach (float score in scores) output.Write(score);
                        output.Flush();
                    }
                }
            }
            catch (EndOfStreamException) { return 0; }
            catch (Exception ex) { Console.Error.WriteLine("worker_failure=" + ex.GetType().Name); return 6; }
            finally
            {
                if (RerankerDeviceRuntime.Session != null)
                {
                    if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AF_RERANKER_WORKER_PROFILE")))
                        RerankerDeviceRuntime.Session.EndProfiling();
                    RerankerDeviceRuntime.Session.Dispose();
                }
            }
        }
    }
}
