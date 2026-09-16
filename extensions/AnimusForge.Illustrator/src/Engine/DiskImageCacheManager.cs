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
        public bool IsDefault { get; set; }
        public bool Deleted { get; set; }
        public DateTime CreatedTime { get; set; }
        [JsonIgnore]
        public byte[] ImageData { get; set; }
    }

    public static class DiskImageCacheManager
    {
        private static readonly string CacheBaseDir;
        private static readonly object CacheLock = new object();
        private static List<CachedIllustrationItem> _cachedIllustrations;
        private static string _cachedCampaignKey;
        private static readonly string[] Categories = { "encyclopedia", "conversation", "weekly_report", "general" };

        static DiskImageCacheManager()
        {
            try
            {
                string docsDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                CacheBaseDir = Path.Combine(docsDir, "Mount and Blade II Bannerlord", "AnimusForge", "IllustratorCache");
                Directory.CreateDirectory(CacheBaseDir);
            }
            catch (Exception ex)
            {
                Debug.Print($"[Illustrator] Failed to init cache dir: {ex.Message}");
                CacheBaseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "IllustratorCache");
            }
        }

        public static string CacheRoot => CacheBaseDir;

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
            lock (CacheLock)
            {
            if (string.IsNullOrWhiteSpace(subjectKey) || string.IsNullOrWhiteSpace(campaignKey)) return null;
            try
            {
                string[] categories = !string.IsNullOrEmpty(category) ? new[] { ValidCategory(category) } : Categories;
                CachedIllustrationItem fallback = null;
                foreach (var cat in categories)
                {
                    string dir = SafeDirectory(Path.Combine(CampaignDirectory(campaignKey), cat));
                    if (!Directory.Exists(dir)) continue;
                    foreach (var metaPath in Directory.GetFiles(dir, "*.json", SearchOption.TopDirectoryOnly))
                    {
                        var item = ReadMetadata(metaPath);
                        if (item == null || item.Deleted) continue;
                        if (!string.Equals(item.SubjectKey, subjectKey, StringComparison.Ordinal) &&
                            !string.Equals(item.Key, subjectKey, StringComparison.Ordinal)) continue;
                        if (!File.Exists(item.FilePath)) continue;
                        if (item.IsDefault)
                        {
                            item.ImageData = ImagePayload.ReadFile(item.FilePath);
                            return item;
                        }
                        if (fallback == null || item.CreatedTime > fallback.CreatedTime) fallback = item;
                    }
                }
                if (fallback != null) fallback.ImageData = ImagePayload.ReadFile(fallback.FilePath);
                return fallback;
            }
            catch (Exception ex)
            {
                Debug.Print($"[Illustrator] Failed to load image cache: {ex.Message}");
                return null;
            }

            }
        }

        public static CachedIllustrationItem SaveImage(string subjectKey, byte[] bytes, string prompt, string title, string category, string campaignKey, int maxCacheCount, bool makeDefault = false, bool allowImplicitDefault = true)
        {
            try { bytes = ImagePayload.Normalize(bytes); }
            catch (Exception ex) { Debug.Print("[Illustrator] Rejected cache image: " + ex.Message); return null; }
            lock (CacheLock)
            {
            if (string.IsNullOrWhiteSpace(subjectKey) || string.IsNullOrWhiteSpace(campaignKey) || bytes == null || bytes.Length == 0) return null;
            try
            {
                string categoryDir = SafeDirectory(Path.Combine(CampaignDirectory(campaignKey), ValidCategory(category)));
                Directory.CreateDirectory(categoryDir);
                string imageId = $"{ComputeHash(subjectKey)}_{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid().ToString("N").Substring(0, 6)}";
                string filePath = Path.Combine(categoryDir, imageId + ".png");
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
                    CreatedTime = DateTime.UtcNow,
                    IsDefault = makeDefault || (allowImplicitDefault && !GetAllCachedIllustrations(campaignKey, true).Any(existing => existing.SubjectKey == subjectKey && existing.Category == ValidCategory(category)))
                };
                AtomicWrite(Path.ChangeExtension(filePath, ".json"), JsonConvert.SerializeObject(item, Formatting.Indented));
                if (item.IsDefault) SetDefault(item, campaignKey);
                EnforceLimit(campaignKey, maxCacheCount);
                InvalidateCache();
                return item;
            }
            catch (Exception ex)
            {
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
            }
        }

        public static List<CachedIllustrationItem> GetAllCachedIllustrations(string campaignKey, bool forceRefresh = false)
        {
            lock (CacheLock)
            {
                if (!forceRefresh && _cachedIllustrations != null && string.Equals(_cachedCampaignKey, campaignKey, StringComparison.Ordinal))
                {
                    return new List<CachedIllustrationItem>(_cachedIllustrations);
                }

                var list = new List<CachedIllustrationItem>();
                try
                {
                    string campaignDir = CampaignDirectory(campaignKey);
                    if (Directory.Exists(campaignDir))
                    {
                        foreach (var jsonFile in EnumerateCampaignFiles(campaignKey, "*.json"))
                        {
                            var item = ReadMetadata(jsonFile);
                            if (item != null && !item.Deleted && File.Exists(item.FilePath)) list.Add(item);
                        }
                        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        foreach (var item in list) visited.Add(item.FilePath);
                        foreach (var png in EnumerateCampaignFiles(campaignKey, "*.png"))
                        {
                            if (visited.Contains(png)) continue;
                            list.Add(new CachedIllustrationItem
                            {
                                Key = Path.GetFileNameWithoutExtension(png),
                                SubjectKey = Path.GetFileNameWithoutExtension(png),
                                CampaignKey = campaignKey,
                                Category = new DirectoryInfo(Path.GetDirectoryName(png)).Name,
                                FilePath = png,
                                Title = "卡拉迪亚历史画卷",
                                Prompt = "（本地已留存历史画卷）",
                                CreatedTime = File.GetCreationTimeUtc(png)
                            });
                        }
                    }
                    list.Sort((a, b) => b.CreatedTime.CompareTo(a.CreatedTime));
                    _cachedIllustrations = list;
                    _cachedCampaignKey = campaignKey;
                    return new List<CachedIllustrationItem>(list);
                }
                catch
                {
                    return list;
                }
            }
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

        private static void EnforceLimit(string campaignKey, int maxCacheCount)
        {
            var items = GetAllCachedIllustrations(campaignKey, true);
            int limit = Math.Max(20, Math.Min(1000, maxCacheCount));
            if (items.Count <= limit) return;
            int remaining = items.Count;
            // Keep the newest generated histories; otherwise an unpromoted new image is
            // immediately evicted when all older subjects already have a default.
            foreach (var item in items.OrderBy(i => i.CreatedTime))
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
