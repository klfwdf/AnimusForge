using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Illustrator.Engine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AnimusForge.Illustrator.Core
{
    // Opt-in by generation scope, not global HTTP logging. Headers/credentials are never serialized.
    // At most 12 request directories, 15 MiB references + 512 KiB metadata + 2 MiB
    // each optional scene inventory/resource supplement (and atomic temp files).
    internal sealed class GenerationDiagnostics : IDisposable
    {
        internal const int MaxRecords = 12;
        internal const int MaxReferenceBytes = 15 * 1024 * 1024;
        internal const int MaxMetadataBytes = 512 * 1024;
        internal const int MaxEvents = 96;
        private const int MaxSceneInventoryBytes = 2 * 1024 * 1024;
        private const int Utf8BomBytes = 3;
        private static readonly Encoding DocumentEncoding = new UTF8Encoding(true);
        private static readonly AsyncLocal<GenerationDiagnostics> Ambient = new AsyncLocal<GenerationDiagnostics>();
        private static readonly object StorageLock = new object();
        // New: conversation_1003-222530_3f9a. Legacy (still pruned): 20261003T142530_<32 hex>.
        // Not RegexOptions.Compiled: it runs at most twice per generation, and compiling would cost more on first use.
        private static readonly Regex RecordName = new Regex(
            @"^((encyclopedia|conversation|weekly_report)_\d{4}-\d{6}_[a-f0-9]{4}|\d{8}T\d{6}_[a-f0-9]{32})$");
        private static readonly HashSet<string> Active = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly object _gate = new object();
        private readonly GenerationDiagnostics _previous;
        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private readonly List<string> _secrets = new List<string>();
        private readonly Dictionary<string, JObject> _references = new Dictionary<string, JObject>();
        private readonly JObject _document;
        private readonly string _directory;
        private int _referenceBytes;
        private int _stepLogBytes;
        private const int MaxStepLogBytes = 128 * 1024;
        private int _captureEvidenceBytes;
        private bool _disposed;
        private readonly JArray _events = new JArray();
        internal static GenerationDiagnostics Current => Ambient.Value;
        internal string Id { get; }

        private GenerationDiagnostics(string campaign, string category, string root)
        {
            // The name must match RecordName, otherwise retention would never prune it.
            if (category != "encyclopedia" && category != "conversation" && category != "weekly_report")
                throw new ArgumentException("Unsupported diagnostic category.", nameof(category));
            // Short readable name: category + local MMdd-HHmmss + 4-hex suffix, e.g. conversation_1003-222530_3f9a.
            // Year is omitted on purpose; retention is 12 records and pruning orders by creation time, not by name.
            string prefix = category + "_" + DateTime.Now.ToString("MMdd-HHmmss", CultureInfo.InvariantCulture) + "_";
            string fullRoot = Path.GetFullPath(root);
            string id, directory;
            lock (StorageLock)
            {
                Directory.CreateDirectory(root);
                Prune(root, MaxRecords - 1);
                if (new DirectoryInfo(root).GetDirectories().Count(d => RecordName.IsMatch(d.Name)) >= MaxRecords)
                    throw new IOException("Diagnostic retention is full.");
                // 4 hex chars can collide within one second; pick a free name under the lock so two records never share a directory.
                do
                {
                    id = prefix + Guid.NewGuid().ToString("N").Substring(0, 4);
                    directory = Path.Combine(fullRoot, id);
                } while (Directory.Exists(directory) || Active.Contains(directory));
                Directory.CreateDirectory(directory);
                Active.Add(directory);
            }
            Id = id;
            _directory = directory;
            _previous = Ambient.Value;
            _document = new JObject { ["schema"] = 2, ["id"] = Id, ["campaign"] = CleanText(campaign), ["category"] = CleanText(category),
                ["startedUtc"] = DateTime.UtcNow, ["outcome"] = "running", ["events"] = _events,
                ["assemblyVersion"] = typeof(GenerationDiagnostics).Assembly.GetName().Version.ToString(),
                ["moduleVersionId"] = typeof(GenerationDiagnostics).Assembly.ManifestModule.ModuleVersionId.ToString("D") };
            try { Flush(); }
            catch { lock (StorageLock) Active.Remove(_directory); throw; }
            Ambient.Value = this;
            RecordStage("pipeline_begin", new JObject { ["category"] = category, ["diagnosticDirectory"] = _directory });
            RecordStage("native_export_environment", Engine.NativeExportDiagnostics.DescribeEnvironment());
        }

        internal static GenerationDiagnostics Begin(string campaign, string category)
        {
            if (category != "encyclopedia" && category != "conversation" && category != "weekly_report") return null;
            try { return new GenerationDiagnostics(campaign, category, Path.Combine(DiskImageCacheManager.CacheRoot, "Diagnostics")); }
            catch (Exception ex) { WriteDelivery(null, "diagnostics_unavailable", ex.GetType().Name); return null; }
        }

        internal void RegisterSecret(string value)
        { if (!string.IsNullOrEmpty(value)) lock (_gate) if (!_secrets.Contains(value)) _secrets.Add(value); }

        internal void SetSubject(string subject)
        { Safe(() => { _document["subject"] = CleanText(subject); Flush(); }); }

        internal void RecordStage(string stage, JObject details = null)
        { Safe(() => AddEvent(stage, details == null ? new JObject() : (JObject)Sanitize(details, false))); }

        internal void RecordDirectorRequest(string endpoint, string model, JObject payload, IReadOnlyList<IllustrationReferenceImage> refs)
        {
            Safe(() => AddEvent("director_request", new JObject { ["endpoint"] = CleanText(SafeUrl(endpoint)), ["model"] = CleanText(model),
                ["payload"] = Sanitize(payload, true), ["referenceCount"] = refs?.Count ?? 0 }));
        }

        internal void RecordPanoramaFace(int face, byte[] nativePng)
        {
            Safe(() =>
            {
                if (nativePng == null || _captureEvidenceBytes + (long)nativePng.Length > 8 * 1024 * 1024)
                { AddEvent("panorama_native_evidence", new JObject { ["face"] = face, ["omitted"] = "8 MiB capture evidence budget" }); return; }
                var evidence = StoreReference(nativePng, "native_face_" + face + "_before_rgb_conversion.png");
                _captureEvidenceBytes += nativePng.Length;
                AddEvent("panorama_native_evidence", new JObject { ["face"] = face, ["reference"] = evidence,
                    ["purpose"] = "Raw native export before producer-specific RGB correction; not a model reference." });
            });
        }

        internal void RecordSceneInventory(PanoramaSnapshotInventory inventory, PanoramaSceneSnapshot snapshot, bool complete)
        {
            if (inventory == null || snapshot == null) return;
            Safe(() =>
            {
                var document = (JObject)Sanitize(inventory.ToDocument(snapshot, complete), false);
                var entries = (JArray)document["entries"];
                int droppedForBytes = 0;
                byte[] bytes;
                while (true)
                {
                    document["omittedForByteBudget"] = droppedForBytes;
                    bytes = Encoding.UTF8.GetBytes(document.ToString(Formatting.None));
                    if (bytes.Length + Utf8BomBytes <= MaxSceneInventoryBytes) break;
                    if (entries.Count == 0) throw new IOException("Scene inventory exceeds metadata budget.");
                    int remove = Math.Max(1, entries.Count / 4);
                    for (int i = 0; i < remove; i++) entries.RemoveAt(entries.Count - 1);
                    droppedForBytes += remove;
                }
                string path = Path.Combine(_directory, "scene-inventory.json");
                string temporary = path + ".tmp";
                try
                {
                    WriteUtf8Document(temporary, bytes);
                    if (File.Exists(path)) File.Replace(temporary, path, null);
                    else File.Move(temporary, path);
                }
                finally { if (File.Exists(temporary)) File.Delete(temporary); }
                AddEvent("scene_inventory", new JObject
                {
                    ["file"] = "scene-inventory.json", ["bytes"] = bytes.Length + Utf8BomBytes, ["complete"] = complete,
                    ["recordedEntries"] = entries.Count, ["omittedEntries"] = document["omittedEntries"],
                    ["omittedForByteBudget"] = droppedForBytes
                });
            });
        }

        internal void RecordIsolatedSceneProbeImage(byte[] previewPng)
        {
            Safe(() => AddEvent("isolated_scene_probe_image", new JObject
            {
                ["preview"] = StoreReference(previewPng, "isolated_panorama_preview.png"),
                ["views"] = 6, ["horizontalDegrees"] = 360, ["verticalDegrees"] = 180, ["sentToModels"] = false,
                ["purpose"] = "Manual private-scene spherical panorama preview; no view renders the live mission scene."
            }));
        }

        internal void RecordSceneResourceSupplement(JObject details)
        {
            if (details == null) return;
            Safe(() =>
            {
                var document = (JObject)Sanitize(details, false);
                var records = document["records"] as JArray ?? new JArray();
                int dropped = 0;
                byte[] bytes;
                while (true)
                {
                    document["omittedForByteBudget"] = dropped;
                    bytes = Encoding.UTF8.GetBytes(document.ToString(Formatting.None));
                    if (bytes.Length + Utf8BomBytes <= MaxSceneInventoryBytes) break;
                    if (records.Count == 0) throw new IOException("Resource inventory exceeds metadata budget.");
                    int remove = Math.Max(1, records.Count / 4);
                    for (int i = 0; i < remove; i++) records.RemoveAt(records.Count - 1);
                    dropped += remove;
                }
                string path = Path.Combine(_directory, "scene-resource-supplement.json");
                string temporary = path + ".tmp";
                try
                {
                    WriteUtf8Document(temporary, bytes);
                    if (File.Exists(path)) File.Replace(temporary, path, null); else File.Move(temporary, path);
                }
                finally { if (File.Exists(temporary)) File.Delete(temporary); }
                AddEvent("scene_resource_supplement", new JObject
                {
                    ["file"] = "scene-resource-supplement.json", ["bytes"] = bytes.Length + Utf8BomBytes,
                    ["copied"] = document["copied"], ["complete"] = document["complete"],
                    ["missingResources"] = document["missingResources"], ["omittedForByteBudget"] = dropped
                });
            });
        }

        internal void RecordDirectorResponse(string rawResponse, string finishReason, int? httpStatus = null)
        { RecordResponse("director_response", rawResponse, httpStatus, finishReason); }

        internal void RecordDirection(IllustrationDirection direction)
        {
            Safe(() => AddEvent("direction", new JObject { ["status"] = CleanText(direction.DirectionStatus), ["message"] = CleanText(direction.StatusText),
                ["reason"] = CleanText(direction.FallbackReason), ["finishReason"] = CleanText(direction.FinishReason), ["prompt"] = CleanText(direction.Prompt),
                ["sceneYawDegrees"] = direction.SceneYawDegrees, ["scenePitchDegrees"] = direction.ScenePitchDegrees,
                ["sceneHorizontalFovDegrees"] = direction.SceneHorizontalFovDegrees,
                ["auxiliarySceneYawDegrees"] = direction.AuxiliarySceneYawDegrees,
                ["auxiliaryScenePitchDegrees"] = direction.AuxiliaryScenePitchDegrees,
                ["auxiliarySceneHorizontalFovDegrees"] = direction.AuxiliarySceneHorizontalFovDegrees }));
        }

        internal async Task RecordImageRequestAsync(HttpRequestMessage request, string protocol)
        {
            try
            {
                var data = new JObject();
                var multipart = request.Content as MultipartFormDataContent;
                if (multipart != null)
                {
                    var files = new JArray();
                    foreach (var part in multipart)
                    {
                        var disposition = part.Headers.ContentDisposition;
                        if (disposition?.FileName != null)
                        {
                            byte[] bytes = await part.ReadAsByteArrayAsync().ConfigureAwait(false);
                            lock (_gate) files.Add(StoreReference(bytes, disposition.FileName.Trim('"')));
                        }
                        else if (disposition?.Name != null)
                            data[disposition.Name.Trim('"')] = CleanText(await part.ReadAsStringAsync().ConfigureAwait(false));
                    }
                    data["images"] = files;
                }
                else
                {
                    var parsed = JToken.Parse(await request.Content.ReadAsStringAsync().ConfigureAwait(false));
                    lock (_gate) data["payload"] = Sanitize(parsed, true);
                }
                data["endpoint"] = CleanText(SafeUrl(request.RequestUri.ToString()));
                data["protocol"] = CleanText(protocol);
                Safe(() => AddEvent("image_request", data));
            }
            catch (Exception ex) { RecordStage("diagnostic_request_omitted", new JObject { ["reason"] = ex.GetType().Name }); }
        }

        internal void RecordImageResponse(string body, int httpStatus)
        { RecordResponse("image_response", body, httpStatus, null); }

        private void RecordResponse(string stage, string body, int? httpStatus, string finishReason)
        {
            Safe(() =>
            {
                JToken parsed;
                try { parsed = Sanitize(JToken.Parse(body ?? ""), false); }
                catch { parsed = CleanText(body); }
                AddEvent(stage, new JObject { ["httpStatus"] = httpStatus, ["finishReason"] = CleanText(finishReason), ["body"] = parsed });
            });
        }

        internal void RecordImageResult(ImageGenerationResult result)
        {
            Safe(() =>
            {
                _document["outcome"] = result.Success ? "success" : "failed";
                if (result.Success) { _document.Remove("failedStage"); _document.Remove("failureCode"); }
                if (!result.Success && _document["failedStage"] == null) _document["failedStage"] = _document["lastStage"]?.DeepClone();
                AddEvent("image_result", new JObject { ["success"] = result.Success, ["error"] = CleanText(result.ErrorMessage),
                    ["elapsedMs"] = result.ElapsedMilliseconds, ["sentPrompt"] = CleanText(result.ResolvedPrompt), ["imageBytes"] = result.ImageBytes?.Length ?? 0 });
            });
        }

        internal void Finish(string outcome, string error = null)
        {
            Safe(() =>
            {
                if ((string)_document["outcome"] == "running" || outcome == "cancelled" || outcome == "failed") _document["outcome"] = outcome;
                _document["elapsedMs"] = _clock.ElapsedMilliseconds;
                if (!string.IsNullOrWhiteSpace(error)) _document["error"] = CleanText(error);
                if ((outcome == "failed" || outcome == "cancelled") && _document["failedStage"] == null)
                    _document["failedStage"] = _document["lastStage"]?.DeepClone();
                AddEvent("pipeline_finish", new JObject { ["outcome"] = _document["outcome"]?.DeepClone(),
                    ["error"] = CleanText(error), ["failedStage"] = _document["failedStage"]?.DeepClone(), ["elapsedMs"] = _clock.ElapsedMilliseconds });
            });
        }

        private JToken Sanitize(JToken token, bool saveReferences)
        {
            if (token is JObject obj)
            {
                var copy = new JObject();
                foreach (var property in obj.Properties())
                {
                    string name = property.Name.ToLowerInvariant();
                    if (name == "authorization" || name == "api_key" || name == "apikey" || name == "access_token" || name == "secret" || name == "player2-game-key") copy[property.Name] = "[redacted]";
                    else if (name == "b64_json" || name == "image" && property.Value.Type == JTokenType.String || name == "b64" || (name == "data" && property.Value.Type == JTokenType.String && property.Value.ToString().Length > 1024))
                        copy[property.Name] = "[image data omitted, " + property.Value.ToString().Length + " chars]";
                    else copy[property.Name] = Sanitize(property.Value, saveReferences);
                }
                return copy;
            }
            if (token is JArray array) return new JArray(array.Select(x => Sanitize(x, saveReferences)));
            if (token.Type != JTokenType.String) return token.DeepClone();
            string value = token.Value<string>() ?? "";
            if (value.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
            {
                if (!saveReferences) return "[returned image data omitted]";
                int comma = value.IndexOf(',');
                if (comma < 0 || value.Length - comma > ImagePayload.MaxBytes * 4L / 3 + 8) return "[oversize/invalid reference omitted]";
                try { return StoreReference(Convert.FromBase64String(value.Substring(comma + 1)), "image"); }
                catch { return "[invalid reference omitted]"; }
            }
            return CleanText(value);
        }

        private JObject StoreReference(byte[] bytes, string name)
        {
            string hash;
            using (var sha = SHA256.Create()) hash = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "");
            if (_references.TryGetValue(hash, out var existing)) return (JObject)existing.DeepClone();
            var info = new JObject { ["name"] = CleanText(name), ["sha256"] = hash, ["bytes"] = bytes.Length };
            if (_disposed) { info["omitted"] = "diagnostic scope disposed"; return info; }
            if (bytes.Length > ImagePayload.MaxBytes || _referenceBytes + bytes.Length > MaxReferenceBytes)
            { info["omitted"] = "reference storage budget"; return info; }
            // Decode only for metadata; persist exact transmitted encoded bytes with no channel conversion.
            using (var stream = new MemoryStream(bytes))
            using (var image = Image.FromStream(stream, false, true))
            {
                if ((long)image.Width * image.Height > 16777216) { info["omitted"] = "dimensions"; return info; }
                info["width"] = image.Width; info["height"] = image.Height;
            }
            string filename = DiskImageCacheManager.ReadableFileLabel(CleanText(name)) + "_" + (_references.Count + 1)
                + (bytes.Length > 2 && bytes[0] == 255 && bytes[1] == 216 ? ".jpg" : ".png");
            File.WriteAllBytes(Path.Combine(_directory, filename), bytes);
            _referenceBytes += bytes.Length;
            info["file"] = filename;
            _references[hash] = info;
            return (JObject)info.DeepClone();
        }

        private static void WriteUtf8Document(string path, byte[] bytes)
        {
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                byte[] bom = DocumentEncoding.GetPreamble();
                stream.Write(bom, 0, bom.Length);
                stream.Write(bytes, 0, bytes.Length);
            }
        }

        private void AddEvent(string stage, JObject data)
        {
            if (_events.Count >= MaxEvents)
            {
                _events.RemoveAt(0); // Always retain the newest error/result, not just the first capture events.
                _document["eventsOmitted"] = true;
                _document["eventsOmittedCount"] = ((int?)_document["eventsOmittedCount"] ?? 0) + 1;
            }
            data["stage"] = CleanText(stage);
            data["requestElapsedMs"] = _clock.ElapsedMilliseconds;
            _document["lastStage"] = CleanText(stage);
            if (_document["failedStage"] == null && data["failureCode"] != null && !string.IsNullOrWhiteSpace((string)data["failureCode"]))
            {
                _document["failedStage"] = CleanText(stage);
                _document["failureCode"] = data["failureCode"].DeepClone();
            }
            if (data["httpStatus"] != null) _document["lastHttpStatus"] = data["httpStatus"].DeepClone();
            _events.Add(data);
            Flush();
            WriteStepSummary(stage, data);
        }

        // Small summaries only: request bodies/prompts/base64 stay in bounded private trace.json.
        // No tick polling or extra file scan; one line per accepted diagnostic event.
        private void WriteStepSummary(string stage, JObject data)
        {
            if (stage.StartsWith("panorama_face", StringComparison.Ordinal) || stage == "panorama_native_evidence") return;
            var summary = new JObject();
            foreach (string key in new[] { "category", "status", "outcome", "failedStage", "failureCode", "reason", "error", "endpoint", "protocol", "model", "httpStatus", "referenceCount", "requestedRefs", "actualRefs", "success", "elapsedMs", "timeoutMs", "applicationTicks", "saveRequested", "delivered", "bytes", "view", "mode", "portrait", "directorRuleSource", "customStyleActive", "customNegativeActive", "paintObservationAvailable", "paintCallbacks", "paintCallbacksAfterSave", "renderTargetChangedAfterSave", "exportDirectory", "exportPath", "fileProbeCount", "fileSeen", "fileError", "buildApi", "tempDirectory", "nonAsciiPath", "face", "totalTimeoutMs" })
                if (data[key] != null) summary[key] = data[key].DeepClone();
            string line = "id=" + Id + " step=" + CleanText(stage) + " elapsedMs=" + _clock.ElapsedMilliseconds + " " + CleanText(summary.ToString(Formatting.None));
            if (line.Length > 2048) line = line.Substring(0, 2048) + " [truncated]";
            try
            {
                string path = Path.Combine(_directory, "steps.log");
                string entry = line + Environment.NewLine;
                int bytes = Encoding.UTF8.GetByteCount(entry);
                if (_stepLogBytes + bytes > MaxStepLogBytes)
                {
                    File.Copy(path, Path.Combine(_directory, "steps.previous.log"), true);
                    File.WriteAllText(path, entry, DocumentEncoding);
                    _stepLogBytes = bytes + Utf8BomBytes;
                }
                else { File.AppendAllText(path, entry, DocumentEncoding); _stepLogBytes += bytes + (_stepLogBytes == 0 ? Utf8BomBytes : 0); }
            }
            catch { /* trace errors never break generation */ }
            WriteDelivery(Id, stage, line);
        }

        internal static void WriteDelivery(string id, string stage, string message)
        {
            string safe = SensitiveLogText.Redact(message);
            if (safe.Length > 2048) safe = safe.Substring(0, 2048) + " [truncated]";
            try { if (IllustratorRuntime.IsHostRunning) global::AnimusForge.Logger.Log("Illustrator", "id=" + (id ?? "unavailable") + " step=" + stage + " " + safe); } catch { }
            TaleWorlds.Library.Debug.Print("[Illustrator] id=" + (id ?? "unavailable") + " step=" + stage + " " + safe);
        }

        private string CleanText(string value)
        {
            value = value ?? string.Empty;
            lock (_gate)
                foreach (string secret in _secrets) value = SensitiveLogText.Redact(value, secret);
            value = SensitiveLogText.Redact(value);
            return value.Length > 65536 ? value.Substring(0, 65536) + " [truncated]" : value;
        }

        private static string SafeUrl(string value)
        {
            return SensitiveLogText.SafeUrl(value);
        }

        private void Flush()
        {
            // Finish/SetSubject can add data outside AddEvent. Enforce the budget at the single
            // write boundary, retaining request/outcome identity even when an event is too large.
            BoundMetadataField("campaign", 512);
            BoundMetadataField("category", 64);
            BoundMetadataField("subject", 1024);
            BoundMetadataField("error", 4096);
            string json = _document.ToString(Formatting.None);
            if (Encoding.UTF8.GetByteCount(json) + Utf8BomBytes > MaxMetadataBytes)
            {
                _document["metadataTruncated"] = true;
                for (int i = 0; i < _events.Count && Encoding.UTF8.GetByteCount(json) + Utf8BomBytes > MaxMetadataBytes; i++)
                {
                    var summary = new JObject { ["omitted"] = "metadata storage budget" };
                    foreach (string key in new[] { "stage", "elapsedMs", "requestElapsedMs", "endpoint", "protocol", "model", "status", "httpStatus", "success", "failureCode", "failedStage", "error", "outcome" })
                    {
                        JToken value = _events[i][key];
                        if (value == null) continue;
                        summary[key] = value.Type == JTokenType.String && value.Value<string>().Length > 256
                            ? (JToken)(value.Value<string>().Substring(0, 256) + " [truncated]") : value.DeepClone();
                    }
                    _events[i] = summary;
                    json = _document.ToString(Formatting.None);
                }
            }
            if (Encoding.UTF8.GetByteCount(json) + Utf8BomBytes > MaxMetadataBytes)
                throw new IOException("Diagnostic metadata exceeded its bounded storage budget.");
            string path = Path.Combine(_directory, "trace.json");
            string temporary = path + ".tmp";
            File.WriteAllText(temporary, json, DocumentEncoding);
            if (File.Exists(path)) File.Replace(temporary, path, null); else File.Move(temporary, path);
        }

        private void BoundMetadataField(string name, int maxChars)
        {
            string value = (string)_document[name];
            if (value == null || value.Length <= maxChars) return;
            _document[name] = value.Substring(0, maxChars) + " [truncated]";
            _document["metadataTruncated"] = true;
        }

        private void Safe(Action action)
        {
            try { lock (_gate) if (!_disposed) action(); }
            catch (Exception ex) { TaleWorlds.Library.Debug.Print("[Illustrator] Diagnostic write skipped: " + ex.GetType().Name); }
        }

        private static void Prune(string root, int keep)
        {
            string resolvedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var dirs = new DirectoryInfo(root).GetDirectories().Where(d =>
                RecordName.IsMatch(d.Name) &&
                (d.Attributes & FileAttributes.ReparsePoint) == 0 &&
                d.FullName.StartsWith(resolvedRoot, StringComparison.OrdinalIgnoreCase)).OrderByDescending(d => d.CreationTimeUtc).ThenByDescending(d => d.Name).ToList();
            for (int i = dirs.Count - 1; dirs.Count > keep && i >= 0; i--)
            {
                if (Active.Contains(dirs[i].FullName)) continue;
                try { Directory.Delete(dirs[i].FullName, true); dirs.RemoveAt(i); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            }
        }

        public void Dispose()
        {
            lock (_gate)
            {
                if (_disposed) return;
                Finish("completed");
                _disposed = true;
            }
            if (ReferenceEquals(Ambient.Value, this)) Ambient.Value = _previous;
            lock (StorageLock) { Active.Remove(_directory); }
        }
    }
}
