using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using TaleWorlds.Library;

namespace AnimusForge.Illustrator.Engine
{
    public sealed class CachedIllustrationItem
    {
        public string Key { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string Prompt { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedTime { get; set; }
        [JsonIgnore]
        public byte[] ImageData { get; set; }
    }

    public static class DiskImageCacheManager
    {
        private static readonly string CacheBaseDir;

        static DiskImageCacheManager()
        {
            try
            {
                string docsDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                CacheBaseDir = Path.Combine(docsDir, "Mount and Blade II Bannerlord", "AnimusForge", "IllustratorCache");
                if (!Directory.Exists(CacheBaseDir))
                {
                    Directory.CreateDirectory(CacheBaseDir);
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"[Illustrator] Failed to init cache dir: {ex.Message}");
                CacheBaseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "IllustratorCache");
            }
        }

        public static bool TryGetCachedImage(string key, out byte[] imageBytes, out string filePath)
        {
            imageBytes = null;
            filePath = GetImagePath(key);

            if (File.Exists(filePath))
            {
                try
                {
                    imageBytes = File.ReadAllBytes(filePath);
                    return imageBytes != null && imageBytes.Length > 0;
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        public static CachedIllustrationItem LoadImage(string key, string category = null)
        {
            try
            {
                string[] categories = !string.IsNullOrEmpty(category)
                    ? new[] { category }
                    : new[] { "encyclopedia", "conversation", "weekly_report" };

                foreach (var cat in categories)
                {
                    string filePath = GetImagePath(key, cat);
                    if (File.Exists(filePath))
                    {
                        byte[] bytes = File.ReadAllBytes(filePath);
                        string metaPath = Path.ChangeExtension(filePath, ".json");
                        string prompt = string.Empty;
                        string title = string.Empty;
                        DateTime created = File.GetCreationTime(filePath);

                        if (File.Exists(metaPath))
                        {
                            try
                            {
                                string metaJson = File.ReadAllText(metaPath, Encoding.UTF8);
                                var meta = JsonConvert.DeserializeObject<CachedIllustrationItem>(metaJson);
                                if (meta != null)
                                {
                                    prompt = meta.Prompt;
                                    title = meta.Title;
                                    if (meta.CreatedTime != default)
                                    {
                                        created = meta.CreatedTime;
                                    }
                                }
                            }
                            catch { }
                        }

                        return new CachedIllustrationItem
                        {
                            Key = key,
                            Category = cat,
                            FilePath = filePath,
                            Prompt = prompt,
                            Title = title,
                            CreatedTime = created,
                            ImageData = bytes
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"[Illustrator] Failed to load image cache: {ex.Message}");
            }
            return null;
        }

        public static void SaveImage(string key, byte[] bytes, string prompt, string title, string category = "weekly_report")
        {
            if (bytes == null || bytes.Length == 0)
            {
                return;
            }

            try
            {
                string categoryDir = Path.Combine(CacheBaseDir, category);
                if (!Directory.Exists(categoryDir))
                {
                    Directory.CreateDirectory(categoryDir);
                }

                string filePath = Path.Combine(categoryDir, ComputeHash(key) + ".png");
                File.WriteAllBytes(filePath, bytes);

                string metaPath = Path.ChangeExtension(filePath, ".json");
                var item = new CachedIllustrationItem
                {
                    Key = key,
                    Category = category,
                    FilePath = filePath,
                    Prompt = prompt ?? string.Empty,
                    Title = title ?? string.Empty,
                    CreatedTime = DateTime.UtcNow
                };

                File.WriteAllText(metaPath, JsonConvert.SerializeObject(item, Formatting.Indented), Encoding.UTF8);
                InvalidateCache();
            }
            catch (Exception ex)
            {
                Debug.Print($"[Illustrator] Failed to save image cache: {ex.Message}");
            }
        }

        private static List<CachedIllustrationItem> _cachedIllustrations;
        private static DateTime _lastScanTime = DateTime.MinValue;
        private static readonly object _cacheLock = new object();

        public static void InvalidateCache()
        {
            lock (_cacheLock)
            {
                _cachedIllustrations = null;
            }
        }

        public static List<CachedIllustrationItem> GetAllCachedIllustrations(bool forceRefresh = false)
        {
            lock (_cacheLock)
            {
                if (!forceRefresh && _cachedIllustrations != null && (DateTime.UtcNow - _lastScanTime).TotalSeconds < 10)
                {
                    return new List<CachedIllustrationItem>(_cachedIllustrations);
                }

                var list = new List<CachedIllustrationItem>();
                try
                {
                if (!Directory.Exists(CacheBaseDir))
                {
                    return list;
                }

                var visitedPngs = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);

                string[] jsonFiles = Directory.GetFiles(CacheBaseDir, "*.json", SearchOption.AllDirectories);
                foreach (string jsonFile in jsonFiles)
                {
                    try
                    {
                        string content = File.ReadAllText(jsonFile, Encoding.UTF8);
                        var item = JsonConvert.DeserializeObject<CachedIllustrationItem>(content);
                        if (item != null)
                        {
                            string localPng = Path.ChangeExtension(jsonFile, ".png");
                            if (File.Exists(localPng))
                            {
                                item.FilePath = localPng;
                                visitedPngs.Add(localPng);
                                list.Add(item);
                            }
                            else if (!string.IsNullOrEmpty(item.FilePath) && File.Exists(item.FilePath))
                            {
                                visitedPngs.Add(item.FilePath);
                                list.Add(item);
                            }
                        }
                    }
                    catch
                    {
                    }
                }

                // 兜底扫描任何未绑定的 PNG 文件
                string[] pngFiles = Directory.GetFiles(CacheBaseDir, "*.png", SearchOption.AllDirectories);
                foreach (string png in pngFiles)
                {
                    if (!visitedPngs.Contains(png))
                    {
                        string fileName = Path.GetFileNameWithoutExtension(png);
                        list.Add(new CachedIllustrationItem
                        {
                            Key = fileName,
                            Category = "general",
                            FilePath = png,
                            Title = "卡拉迪亚历史画卷",
                            Prompt = "（本地已留存历史画卷）",
                            CreatedTime = File.GetCreationTimeUtc(png)
                        });
                    }
                }

                list.Sort((a, b) => b.CreatedTime.CompareTo(a.CreatedTime));
                _cachedIllustrations = list;
                _lastScanTime = DateTime.UtcNow;
                return new List<CachedIllustrationItem>(_cachedIllustrations);
            }
            catch
            {
                return list;
            }
            }
        }

        public static string GetImagePath(string key, string category = "weekly_report")
        {
            string categoryDir = Path.Combine(CacheBaseDir, category);
            return Path.Combine(categoryDir, ComputeHash(key) + ".png");
        }

        public static string ComputeHash(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return "empty_" + Guid.NewGuid().ToString("N");
            }
            using (var sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
                var sb = new StringBuilder();
                for (int i = 0; i < 16; i++) // 32 字符 hex
                {
                    sb.Append(bytes[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }
    }
}
