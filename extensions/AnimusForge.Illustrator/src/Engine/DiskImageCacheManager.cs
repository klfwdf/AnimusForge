using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using TaleWorlds.Library;

namespace AnimusForge.Illustrator.Engine
{
    public sealed class CachedIllustrationItem
    {
        public string Key { get; set; } = string.Empty;
        public string SubjectKey { get; set; } = string.Empty;
        public string CampaignKey { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string Prompt { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Theme { get; set; } = string.Empty;
        public string ActionSummary { get; set; } = string.Empty;
        public string DiagnosticId { get; set; } = string.Empty;
        public string DirectorStatus { get; set; } = string.Empty;
        public string DirectorStatusText { get; set; } = string.Empty;
        public string DirectorFallbackReason { get; set; } = string.Empty;
        // Fingerprint of the selected image style/custom and negative prompt. A cache hit
        // with a different fingerprint must be regenerated instead of being presented as
        // if it followed the current style settings.
        public string StyleFingerprint { get; set; } = string.Empty;
        public string SourceImageKey { get; set; }
        public string EditInstruction { get; set; }
        public string GenerationMode { get; set; }
        [JsonIgnore]
        public string ThemeText => string.IsNullOrWhiteSpace(Theme) ? "纪事画卷" : "主题：" + Theme;
        [JsonIgnore]
        public string DisplayStatusText => ThemeText +
            (!string.IsNullOrWhiteSpace(DirectorStatus) && !string.Equals(DirectorStatus, "complete", StringComparison.Ordinal) &&
             !string.IsNullOrWhiteSpace(DirectorStatusText) ? "\n" + DirectorStatusText : string.Empty);
        public bool IsDefault { get; set; }
        // Player-pinned in the gallery; favorites are never evicted by the cache limit.
        public bool IsFavorite { get; set; }
        public bool Deleted { get; set; }
        public DateTime CreatedTime { get; set; }
        [JsonIgnore]
        public byte[] ImageData { get; set; }

        internal CachedIllustrationItem CopyMetadata()
        {
            var copy = (CachedIllustrationItem)MemberwiseClone();
            copy.ImageData = null;
            return copy;
        }
    }

    public static class DiskImageCacheManager
    {
        private static readonly string CacheBaseDir;
        private static readonly object CacheLock = new object();
        private static List<CachedIllustrationItem> _cachedIllustrations;
        private static string _cachedCampaignKey;
        // One campaign in memory. Each category indexes both subject ids and individual image
        // ids; repeated card openings never enumerate unrelated metadata or keep decoded images.
        private static Dictionary<string, Dictionary<string, List<CachedIllustrationItem>>> _cachedLookup;
        private static long[] _cachedDirectoryStamps;
        private static readonly string[] Categories = { "encyclopedia", "conversation", "weekly_report", "general" };

        static DiskImageCacheManager()
        {
            try
            {
                // Image generation is a host feature: cache and diagnostics live under the AF module.
                string path = IllustratorStoragePaths.EnsureDirectory(IllustratorStoragePaths.ImageDirectory);
                CacheBaseDir = path;
            }
            catch (Exception ex)
            {
                Debug.Print($"[Illustrator] Failed to init cache dir: {ex.Message}");
                CacheBaseDir = string.Empty;
            }
        }

        public static string CacheRoot => string.IsNullOrWhiteSpace(CacheBaseDir)
            ? throw new IOException("No writable illustration cache root is available.") : CacheBaseDir;

        public static string SanitizeKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "unknown";
            var invalid = Path.GetInvalidFileNameChars();
            var chars = value.Trim().ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (Array.IndexOf(invalid, chars[i]) >= 0 || char.IsControl(chars[i])) chars[i] = '_';
            }
            string key = new string(chars);
            return key == "." || key == ".." ? "unknown" : key;
        }

        private static string CampaignDirectory(string campaignKey)
        {
            return SafeDirectory(Path.Combine(CacheBaseDir, SanitizeKey(campaignKey)));
        }

        private static string TrashDirectory(string campaignKey)
        {
            return SafeDirectory(Path.Combine(CacheBaseDir, "_trash", SanitizeKey(campaignKey)));
        }

        public static bool TryGetCachedImage(string key, string campaignKey, out byte[] imageBytes, out string filePath, string category = null)
        {
            imageBytes = null;
            filePath = null;
            var item = LoadImage(key, campaignKey, category);
            if (item == null) return false;
            imageBytes = item.ImageData;
            filePath = item.FilePath;
            return imageBytes != null && imageBytes.Length > 0;
        }

        public static CachedIllustrationItem LoadImage(string subjectKey, string campaignKey, string category = null)
        {
            if (string.IsNullOrWhiteSpace(subjectKey) || string.IsNullOrWhiteSpace(campaignKey)) return null;
            try
            {
                List<CachedIllustrationItem> candidatesToRead;
                lock (CacheLock)
                {
                    EnsureMetadataIndex(campaignKey, false);
                    string[] categories = !string.IsNullOrEmpty(category) ? new[] { ValidCategory(category) } : Categories;
                    candidatesToRead = new List<CachedIllustrationItem>();
                    foreach (var cat in categories)
                    {
                        if (!_cachedLookup.TryGetValue(cat, out var subjects) || !subjects.TryGetValue(subjectKey, out var candidates)) continue;
                        // Sort only the indexed matches inside this category; retain category priority.
                        foreach (var item in candidates.OrderByDescending(item => item.IsDefault).ThenByDescending(item => item.CreatedTime))
                        {
                            if (File.Exists(item.FilePath)) candidatesToRead.Add(item.CopyMetadata());
                        }
                    }
                    // A corrupt default still falls through to later files, then the next category.
                }
                // Do not hold the metadata lock while reading or decoding up to 24 MiB.
                foreach (var candidate in candidatesToRead)
                {
                    try
                    {
                        candidate.ImageData = ImagePayload.ReadFile(candidate.FilePath);
                        if (candidate.ImageData != null && candidate.ImageData.Length > 0) return candidate;
                    }
                    catch (Exception ex)
                    {
                        Debug.Print($"[Illustrator] Skipping unreadable cached image '{candidate.FilePath}': {ex.Message}");
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                Debug.Print($"[Illustrator] Failed to load image cache: {ex.Message}");
                return null;
            }
        }

        public static bool IsStyleCompatible(CachedIllustrationItem item, string styleFingerprint)
        {
            // Missing legacy metadata is unknown, not evidence of a style mismatch.
            return item != null && (string.IsNullOrWhiteSpace(item.StyleFingerprint) ||
                string.IsNullOrWhiteSpace(styleFingerprint) ||
                string.Equals(item.StyleFingerprint, styleFingerprint, StringComparison.Ordinal));
        }

        public static string CachedDisplayStatus(CachedIllustrationItem item, string styleFingerprint)
        {
            return item.DisplayStatusText + (IsStyleCompatible(item, styleFingerprint)
                ? string.Empty : "\n画风设置已更改；点击重新绘制后应用新画风。");
        }

        public static CachedIllustrationItem SaveImage(string subjectKey, byte[] bytes, string prompt, string title, string category, string campaignKey, int maxCacheCount, bool makeDefault = false, bool allowImplicitDefault = true, string theme = null, string actionSummary = null, string diagnosticId = null, string directorStatus = null, string directorStatusText = null, string directorFallbackReason = null, string styleFingerprint = null)
        {
            return SaveImageWithEditMetadata(subjectKey, bytes, prompt, title, category, campaignKey, maxCacheCount, makeDefault, allowImplicitDefault, theme, actionSummary, diagnosticId, directorStatus, directorStatusText, directorFallbackReason, styleFingerprint);
        }

        public static CachedIllustrationItem SaveImageWithEditMetadata(string subjectKey, byte[] bytes, string prompt, string title, string category, string campaignKey, int maxCacheCount, bool makeDefault = false, bool allowImplicitDefault = true, string theme = null, string actionSummary = null, string diagnosticId = null, string directorStatus = null, string directorStatusText = null, string directorFallbackReason = null, string styleFingerprint = null, string sourceImageKey = null, string editInstruction = null, string generationMode = null)
        {
            try { bytes = ImagePayload.Normalize(bytes); }
            catch (Exception ex) { Core.GenerationDiagnostics.Current?.RecordStage("cache_image_invalid", new Newtonsoft.Json.Linq.JObject { ["failureCode"] = "cache.invalid_image", ["error"] = ex.Message }); Core.GenerationDiagnostics.Current?.Finish("failed", ex.Message); return null; }
            lock (CacheLock)
            {
            if (string.IsNullOrWhiteSpace(subjectKey) || string.IsNullOrWhiteSpace(campaignKey) || bytes == null || bytes.Length == 0)
            { Core.GenerationDiagnostics.Current?.RecordStage("cache_save_rejected", new Newtonsoft.Json.Linq.JObject { ["failureCode"] = "cache.missing_identity_or_image" }); Core.GenerationDiagnostics.Current?.Finish("failed", "cache identity/image missing"); return null; }
            Core.GenerationDiagnostics.Current?.RecordStage("cache_save_begin", new Newtonsoft.Json.Linq.JObject { ["bytes"] = bytes.Length, ["category"] = category });
            try
            {
                string categoryDir = SafeDirectory(Path.Combine(CampaignDirectory(campaignKey), ValidCategory(category)));
                Directory.CreateDirectory(categoryDir);
                bool isDefault = makeDefault || (allowImplicitDefault && !HasIndexedSubject(campaignKey, ValidCategory(category), subjectKey));
                string imageId = $"{ComputeHash(subjectKey)}_{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid().ToString("N").Substring(0, 6)}";
                string fileName = ReadableFileLabel(title) + "_" + imageId.Substring(imageId.IndexOf('_') + 1);
                string filePath = Path.Combine(categoryDir, fileName + ".png");
                File.WriteAllBytes(filePath, bytes);

                var item = new CachedIllustrationItem
                {
                    Key = imageId,
                    SubjectKey = subjectKey,
                    CampaignKey = campaignKey,
                    Category = ValidCategory(category),
                    FilePath = filePath,
                    Prompt = prompt ?? string.Empty,
                    Title = title ?? string.Empty,
                    Theme = theme ?? string.Empty,
                    ActionSummary = actionSummary ?? string.Empty,
                    DiagnosticId = diagnosticId ?? string.Empty,
                    DirectorStatus = directorStatus ?? string.Empty,
                    DirectorStatusText = directorStatusText ?? string.Empty,
                    DirectorFallbackReason = directorFallbackReason ?? string.Empty,
                    StyleFingerprint = styleFingerprint ?? string.Empty,
                    SourceImageKey = sourceImageKey, EditInstruction = editInstruction, GenerationMode = generationMode,
                    CreatedTime = DateTime.UtcNow,
                    IsDefault = isDefault
                };
                AtomicWrite(Path.ChangeExtension(filePath, ".json"), JsonConvert.SerializeObject(item, Formatting.Indented));
                InvalidateCache();
                if (item.IsDefault) SetDefault(item, campaignKey);
                EnforceLimit(campaignKey, maxCacheCount, sourceImageKey);
                Core.GenerationDiagnostics.Current?.RecordStage("cache_save_complete", new Newtonsoft.Json.Linq.JObject { ["bytes"] = bytes.Length, ["fileName"] = Path.GetFileName(filePath) });
                return item;
            }
            catch (Exception ex)
            {
                Core.GenerationDiagnostics.Current?.RecordStage("cache_save_failed", new Newtonsoft.Json.Linq.JObject { ["failureCode"] = "cache.write_failed", ["error"] = ex.GetType().Name + ": " + ex.Message });
                Core.GenerationDiagnostics.Current?.Finish("failed", ex.Message);
                Debug.Print($"[Illustrator] Failed to save image cache: {ex.Message}");
                return null;
            }

            }
        }

        public static void InvalidateCache()
        {
            lock (CacheLock)
            {
                _cachedIllustrations = null;
                _cachedCampaignKey = null;
                _cachedLookup = null;
                _cachedDirectoryStamps = null;
            }
        }

        internal static string ReadableFileLabel(string title)
        {
            string label = SanitizeKey(string.IsNullOrWhiteSpace(title) ? "illustration" : title).TrimEnd(' ', '.');
            if (label.Length > 48) label = label.Substring(0, 48);
            if (label.Length > 0 && char.IsHighSurrogate(label[label.Length - 1])) label = label.Substring(0, label.Length - 1);
            return string.IsNullOrWhiteSpace(label) ? "illustration" : label;
        }

        public static List<CachedIllustrationItem> GetAllCachedIllustrations(string campaignKey, bool forceRefresh = false)
        {
            lock (CacheLock)
            {
                EnsureMetadataIndex(campaignKey, forceRefresh);
                return _cachedIllustrations.Select(item => item.CopyMetadata()).ToList();
            }
        }

        private static void EnsureMetadataIndex(string campaignKey, bool forceRefresh)
        {
                string normalizedCampaign = SanitizeKey(campaignKey);
                if (!forceRefresh && _cachedIllustrations != null && _cachedLookup != null &&
                    string.Equals(_cachedCampaignKey, normalizedCampaign, StringComparison.Ordinal) && DirectoryStampsMatch(campaignKey)) return;
                var list = new List<CachedIllustrationItem>();
                bool complete = true;
                var stamps = new long[Categories.Length + 1];
                try
                {
                    // Capture before enumeration: a concurrent external addition must cause the
                    // next lookup to retry, not be hidden behind a post-enumeration timestamp.
                    for (int i = 0; i < stamps.Length; i++) stamps[i] = DirectoryStamp(campaignKey, i);
                    string campaignDir = CampaignDirectory(campaignKey);
                    if (Directory.Exists(campaignDir))
                    {
                        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        foreach (var jsonFile in EnumerateCampaignFiles(campaignKey, "*.json"))
                        {
                            var item = ReadMetadata(jsonFile);
                            if (item == null) continue;
                            visited.Add(item.FilePath);
                            if (!item.Deleted && File.Exists(item.FilePath)) list.Add(item);
                        }
                        foreach (var png in EnumerateCampaignFiles(campaignKey, "*.png"))
                        {
                            if (visited.Contains(png)) continue;
                            list.Add(new CachedIllustrationItem
                            {
                                Key = Path.GetFileNameWithoutExtension(png),
                                SubjectKey = Path.GetFileNameWithoutExtension(png),
                                CampaignKey = normalizedCampaign,
                                Category = new DirectoryInfo(Path.GetDirectoryName(png)).Name,
                                FilePath = png,
                                Title = "卡拉迪亚历史画卷",
                                Prompt = "（本地已留存历史画卷）",
                                CreatedTime = File.GetCreationTimeUtc(png)
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    complete = false;
                    Debug.Print("[Illustrator] Failed to refresh image metadata: " + ex.Message);
                }
                list.Sort((a, b) => b.CreatedTime.CompareTo(a.CreatedTime));
                var lookup = new Dictionary<string, Dictionary<string, List<CachedIllustrationItem>>>(StringComparer.Ordinal);
                foreach (var item in list)
                {
                    if (!lookup.TryGetValue(item.Category, out var subjects))
                        lookup[item.Category] = subjects = new Dictionary<string, List<CachedIllustrationItem>>(StringComparer.Ordinal);
                    AddIndexItem(subjects, item.SubjectKey, item);
                    if (!string.Equals(item.Key, item.SubjectKey, StringComparison.Ordinal)) AddIndexItem(subjects, item.Key, item);
                }
                _cachedIllustrations = list;
                // A transient failed enumeration must not freeze a partial index for the session.
                _cachedCampaignKey = complete ? normalizedCampaign : null;
                _cachedLookup = lookup;
                _cachedDirectoryStamps = stamps;
        }

        private static bool DirectoryStampsMatch(string campaignKey)
        {
            if (_cachedDirectoryStamps == null) return false;
            try
            {
                // Five directory stats per query, independent of gallery size. No file-system
                // watcher or per-frame polling; manual additions/restores invalidate the index.
                for (int i = 0; i < _cachedDirectoryStamps.Length; i++)
                    if (_cachedDirectoryStamps[i] != DirectoryStamp(campaignKey, i)) return false;
                return true;
            }
            catch { return false; }
        }

        private static long DirectoryStamp(string campaignKey, int index)
        {
            string directory = SafeDirectory(Path.Combine(CampaignDirectory(campaignKey), index < Categories.Length ? Categories[index] : "_defaults"));
            return Directory.GetLastWriteTimeUtc(directory).Ticks;
        }

        private static bool HasIndexedSubject(string campaignKey, string category, string subject)
        {
            EnsureMetadataIndex(campaignKey, false);
            return _cachedLookup.TryGetValue(category, out var subjects) && subjects.TryGetValue(subject, out var items) &&
                items.Any(item => string.Equals(item.SubjectKey, subject, StringComparison.Ordinal) && File.Exists(item.FilePath));
        }

        private static void AddIndexItem(Dictionary<string, List<CachedIllustrationItem>> subjects, string key, CachedIllustrationItem item)
        {
            if (string.IsNullOrEmpty(key)) return;
            if (!subjects.TryGetValue(key, out var values)) subjects[key] = values = new List<CachedIllustrationItem>();
            values.Add(item);
        }

        private static CachedIllustrationItem ReadMetadata(string jsonFile)
        {
            try
            {
                if (!IsSafePath(jsonFile, CacheBaseDir)) return null;
                var item = JsonConvert.DeserializeObject<CachedIllustrationItem>(File.ReadAllText(jsonFile, Encoding.UTF8));
                if (item == null) return null;
                string localPng = Path.ChangeExtension(jsonFile, ".png");
                if (!File.Exists(localPng) || !IsSafePath(localPng, CacheBaseDir)) return null;
                item.FilePath = localPng;
                string category = new DirectoryInfo(Path.GetDirectoryName(jsonFile)).Name;
                string campaign = new DirectoryInfo(Path.GetDirectoryName(Path.GetDirectoryName(jsonFile))).Name;
                if (!Categories.Contains(category)) return null;
                item.Category = category;
                item.CampaignKey = campaign;
                if (string.IsNullOrEmpty(item.Key)) item.Key = Path.GetFileNameWithoutExtension(jsonFile);
                if (string.IsNullOrEmpty(item.SubjectKey)) item.SubjectKey = item.Key;
                string pointer = DefaultPath(campaign, item.Category, item.SubjectKey);
                if (File.Exists(pointer)) item.IsDefault = File.ReadAllText(pointer, Encoding.UTF8) == item.Key;
                return item;
            }
            catch { return null; }
        }

        public static bool SetDefault(CachedIllustrationItem target, string campaignKey)
        {
            lock (CacheLock)
            {
                if (target == null || string.IsNullOrWhiteSpace(target.SubjectKey) ||
                    !IsSafePath(target.FilePath, CampaignDirectory(campaignKey)) || !File.Exists(target.FilePath)) return false;
                var actual = ReadMetadata(Path.ChangeExtension(target.FilePath, ".json"));
                if (actual == null || actual.Key != target.Key || actual.SubjectKey != target.SubjectKey || actual.Category != target.Category) return false;
                string pointer = DefaultPath(campaignKey, actual.Category, actual.SubjectKey);
                Directory.CreateDirectory(Path.GetDirectoryName(pointer));
                AtomicWrite(pointer, actual.Key);
                InvalidateCache();
                return true;
            }
        }

        public static bool PromoteDefaultIfNewest(CachedIllustrationItem target, string campaignKey)
        {
            lock (CacheLock)
            {
                if (target == null || string.IsNullOrWhiteSpace(target.SubjectKey) ||
                    !IsSafePath(target.FilePath, CampaignDirectory(campaignKey)) || !File.Exists(target.FilePath)) return false;
                var actual = ReadMetadata(Path.ChangeExtension(target.FilePath, ".json"));
                if (actual == null || actual.Key != target.Key || actual.SubjectKey != target.SubjectKey || actual.Category != target.Category) return false;
                EnsureMetadataIndex(campaignKey, false);
                if (_cachedLookup.TryGetValue(actual.Category, out var subjects) && subjects.TryGetValue(actual.SubjectKey, out var candidates))
                {
                    var current = candidates.FirstOrDefault(item => item.IsDefault);
                    if (current != null && current.CreatedTime > actual.CreatedTime) return false;
                }
                string pointer = DefaultPath(campaignKey, actual.Category, actual.SubjectKey);
                Directory.CreateDirectory(Path.GetDirectoryName(pointer));
                AtomicWrite(pointer, actual.Key);
                InvalidateCache();
                return true;
            }
        }

        public static bool SetFavorite(CachedIllustrationItem target, string campaignKey, bool favorite)
        {
            lock (CacheLock)
            {
                try
                {
                    if (target == null || !IsSafePath(target.FilePath, CampaignDirectory(campaignKey)) || !File.Exists(target.FilePath)) return false;
                    string metaPath = Path.ChangeExtension(target.FilePath, ".json");
                    if (!IsSafePath(metaPath, CampaignDirectory(campaignKey))) return false;
                    // Legacy images without metadata get a sidecar so the flag survives a refresh.
                    var actual = File.Exists(metaPath) ? ReadMetadata(metaPath) : target.CopyMetadata();
                    if (actual == null || actual.Key != target.Key || !Categories.Contains(actual.Category)) return false;
                    actual.IsFavorite = favorite;
                    AtomicWrite(metaPath, JsonConvert.SerializeObject(actual, Formatting.Indented));
                    InvalidateCache();
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.Print($"[Illustrator] Failed to update favorite flag: {ex.Message}");
                    return false;
                }
            }
        }

        public static bool DeleteItem(CachedIllustrationItem item, string campaignKey)
        {
            lock (CacheLock)
            {
            if (item == null || string.IsNullOrWhiteSpace(item.FilePath)) return false;
            try
            {
                if (!IsSafePath(item.FilePath, CampaignDirectory(campaignKey))) return false;
                string trash = TrashDirectory(campaignKey);
                Directory.CreateDirectory(trash);
                string imageTarget = Path.Combine(trash, Path.GetFileName(item.FilePath));
                string metaSource = Path.ChangeExtension(item.FilePath, ".json");
                string metaTarget = Path.Combine(trash, Path.GetFileName(metaSource));
                if (!IsSafePath(metaSource, CampaignDirectory(campaignKey))) return false;
                if (File.Exists(imageTarget) || File.Exists(metaTarget)) return false;
                if (File.Exists(item.FilePath)) File.Move(item.FilePath, imageTarget);
                try { if (File.Exists(metaSource)) File.Move(metaSource, metaTarget); }
                catch { if (File.Exists(imageTarget)) File.Move(imageTarget, item.FilePath); throw; }
                InvalidateCache();
                return true;
            }
            catch (Exception ex)
            {
                Debug.Print($"[Illustrator] Failed to move illustration to recycle area: {ex.Message}");
                return false;
            }

            }
        }

        private static void EnforceLimit(string campaignKey, int maxCacheCount, string preserveImageKey = null)
        {
            var items = GetAllCachedIllustrations(campaignKey);
            int limit = Math.Max(20, Math.Min(1000, maxCacheCount));
            if (items.Count <= limit) return;
            int remaining = items.Count;
            // Keep the newest generated histories; otherwise an unpromoted new image is
            // immediately evicted when all older subjects already have a default.
            foreach (var item in items.Where(i => !i.IsFavorite && i.Key != preserveImageKey).OrderBy(i => i.CreatedTime))
            {
                if (remaining <= limit) break;
                if (DeleteItem(item, campaignKey)) remaining--;
            }
        }

        public static string GetImagePath(string imageId, string campaignKey, string category = "weekly_report")
        {
            return Path.Combine(SafeDirectory(Path.Combine(CampaignDirectory(campaignKey), ValidCategory(category))), ComputeHash(imageId) + ".png");
        }

        private static string ValidCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category)) return "general";
            if (!Categories.Contains(category)) throw new ArgumentException("Invalid illustration category.");
            return category;
        }

        internal static bool IsSafePath(string path, string root)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path)) return false;
                string full = Path.GetFullPath(path);
                string boundary = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                if (!full.StartsWith(boundary, StringComparison.OrdinalIgnoreCase)) return false;
                // Reject junctions/symlinks on every existing ancestor, including the cache root.
                for (string current = full; !string.IsNullOrEmpty(current); current = Path.GetDirectoryName(current))
                    if ((File.Exists(current) || Directory.Exists(current)) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) return false;
                return true;
            }
            catch { return false; }
        }

        private static string SafeDirectory(string path)
        {
            if (!IsSafePath(path, CacheBaseDir)) throw new IOException("Cache path is outside its ownership boundary.");
            return path;
        }

        private static IEnumerable<string> EnumerateCampaignFiles(string campaignKey, string pattern)
        {
            foreach (string category in Categories)
            {
                string dir = SafeDirectory(Path.Combine(CampaignDirectory(campaignKey), category));
                if (!Directory.Exists(dir)) continue;
                foreach (string file in Directory.GetFiles(dir, pattern, SearchOption.TopDirectoryOnly))
                    if (IsSafePath(file, dir)) yield return file;
            }
        }

        private static string DefaultPath(string campaign, string category, string subject)
            => Path.Combine(SafeDirectory(Path.Combine(CampaignDirectory(campaign), "_defaults")), ComputeHash(category + "\n" + subject) + ".txt");

        private static void AtomicWrite(string path, string text)
        {
            if (!IsSafePath(path, CacheBaseDir)) throw new IOException("Unsafe cache write.");
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(temporary, text, Encoding.UTF8);
                if (File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }

        public static string ComputeHash(string input)
        {
            if (string.IsNullOrEmpty(input)) return "empty";
            using (var sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
                var sb = new StringBuilder();
                for (int i = 0; i < 16; i++) sb.Append(bytes[i].ToString("x2"));
                return sb.ToString();
            }
        }
    }
}
