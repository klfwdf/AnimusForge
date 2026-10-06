using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json.Linq;
using TaleWorlds.Engine;
using Path = System.IO.Path;
using TaleWorlds.MountAndBlade.View.Tableaus;

namespace AnimusForge.Illustrator.Engine
{
    // Bounded managed snapshots on the request worker, never called by a paint callback.
    // No directory scan, write probe, native object access or GPU-completion claim.
    internal static class NativeExportDiagnostics
    {
        internal static string DescribeError(Exception error)
        {
            if (error == null) return null;
            string text = error.GetType().Name + ": " + error.Message;
            return text.Length > 512 ? text.Substring(0, 512) : text;
        }

        internal static JObject DescribeFile(string path)
        {
            var result = new JObject { ["path"] = path, ["pathLength"] = path?.Length ?? 0,
                ["nonAsciiPath"] = HasNonAscii(path), ["exists"] = false };
            if (string.IsNullOrEmpty(path)) { result["status"] = "not_requested"; return result; }
            try
            {
                // File.Exists hides access errors. GetAttributes lets diagnostics distinguish them.
                var attributes = File.GetAttributes(path);
                result["exists"] = true;
                result["status"] = (attributes & FileAttributes.Directory) != 0 ? "directory" : "file";
                if ((attributes & FileAttributes.Directory) == 0) result["bytes"] = new FileInfo(path).Length;
            }
            catch (FileNotFoundException) { result["status"] = "missing"; }
            catch (DirectoryNotFoundException) { result["status"] = "directory_missing"; }
            catch (Exception ex) { result["status"] = "error"; result["fileError"] = DescribeError(ex); }
            return result;
        }

        internal static JObject DescribeEnvironment()
        {
            var result = new JObject { ["is64BitProcess"] = Environment.Is64BitProcess,
                ["osVersion"] = Environment.OSVersion.ToString(), ["clrVersion"] = Environment.Version.ToString(),
                ["gpuCompletionVerified"] = false,
#if BANNERLORD_1_4_OR_GREATER
                ["buildApi"] = "1.4"
#else
                ["buildApi"] = "1.3"
#endif
            };
            try
            {
                string temp = IllustratorStoragePaths.TempDirectory;
                result["tempDirectory"] = temp;
                result["tempPathLength"] = temp.Length;
                result["nonAsciiPath"] = HasNonAscii(temp);
                result["engineAssembly"] = DescribeAssembly(typeof(View).Assembly);
                result["tableauAssembly"] = DescribeAssembly(typeof(CharacterTableau).Assembly);
            }
            catch (Exception ex) { result["environmentError"] = DescribeError(ex); }
            return result;
        }

        private static JObject DescribeAssembly(Assembly assembly) => new JObject
        {
            ["name"] = assembly.GetName().Name, ["version"] = assembly.GetName().Version.ToString(),
            ["moduleVersionId"] = assembly.ManifestModule.ModuleVersionId.ToString("D")
        };

        private static bool HasNonAscii(string value)
        {
            if (value != null) foreach (char c in value) if (c > 127) return true;
            return false;
        }
    }
}
