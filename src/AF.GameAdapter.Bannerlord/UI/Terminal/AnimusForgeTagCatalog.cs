using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AnimusForge.SiegeAftermathIntervention;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

internal sealed class AnimusForgeTagCatalogSnapshot
{
	public List<AnimusForgeTagCatalogEntry> Entries { get; } = new List<AnimusForgeTagCatalogEntry>();

	public List<string> SourceRoots { get; } = new List<string>();

	public int ScannedFileCount { get; set; }

	public DateTime BuiltUtc { get; set; } = DateTime.UtcNow;
}

internal sealed class AnimusForgeTagCatalogEntry
{
	public string Id { get; set; } = "";

	public string Tag { get; set; } = "";

	public string Category { get; set; } = "";

	public string Description { get; set; } = "";

	public List<string> Sources { get; } = new List<string>();
}

internal static class AnimusForgeTagCatalog
{
	private const int MaxTextFileBytes = 5 * 1024 * 1024;

	private const int MaxAssemblyBytes = 80 * 1024 * 1024;

	private static readonly object CacheLock = new object();

	private static readonly Regex BracketTagRegex = new Regex("\\[(?:ACTION:[^\\]\\r\\n]{1,180}|A:(?:H_J_P_P_(?:[CL]|C[/&]L)|C_J_P_K|C_J_K:[^\\]\\r\\n]{1,120}|P_J_K_[MV]|P_L_K)|AD:[^\\]\\r\\n]{1,180}|ADP:[^\\]\\r\\n]{1,120}|ASS:[^\\]\\r\\n]{1,180}|GUI:[^\\]\\r\\n]{1,180}|ATT:[^\\]\\r\\n]{1,120}|ATP:[^\\]\\r\\n]{1,120}|FOL|STP|END|RELAY:[^\\]\\r\\n]{1,120}|AFEF[^\\]\\r\\n]{1,140}|AF_SCENE_SESSION:[^\\]\\r\\n]{1,80}|CONTENT)\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static AnimusForgeTagCatalogSnapshot _cachedSnapshot;

	private static DateTime _cachedAtUtc = DateTime.MinValue;

	public static AnimusForgeTagCatalogSnapshot BuildSnapshot(bool forceRefresh = false)
	{
		lock (CacheLock)
		{
			if (!forceRefresh && _cachedSnapshot != null && (DateTime.UtcNow - _cachedAtUtc).TotalSeconds < 15.0)
			{
				return _cachedSnapshot;
			}
			AnimusForgeTagCatalogSnapshot snapshot = BuildSnapshotCore();
			_cachedSnapshot = snapshot;
			_cachedAtUtc = DateTime.UtcNow;
			return snapshot;
		}
	}

	public static bool TryExportSnapshotToModuleTxt(AnimusForgeTagCatalogSnapshot snapshot, out string filePath, out string error)
	{
		filePath = "";
		error = "";
		try
		{
			snapshot ??= BuildSnapshot(forceRefresh: true);
			if (snapshot == null || snapshot.Entries.Count <= 0)
			{
				error = "当前没有可导出的标签。";
				return false;
			}
			string moduleRoot = ResolveExportModuleRoot(snapshot);
			if (string.IsNullOrWhiteSpace(moduleRoot) || !Directory.Exists(moduleRoot))
			{
				error = "没有找到可写入的 AnimusForge 模块目录。";
				return false;
			}
			string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
			string baseName = "AnimusForge_Tag_Catalog_" + timestamp;
			string candidate = Path.Combine(moduleRoot, baseName + ".txt");
			for (int i = 1; File.Exists(candidate) && i < 100; i++)
			{
				candidate = Path.Combine(moduleRoot, baseName + "_" + i.ToString(CultureInfo.InvariantCulture) + ".txt");
			}
			if (File.Exists(candidate))
			{
				error = "导出文件名冲突过多，请稍后再试。";
				return false;
			}
			File.WriteAllText(candidate, BuildExportText(snapshot, moduleRoot), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			filePath = candidate;
			return true;
		}
		catch (Exception ex)
		{
			error = ex.Message;
			Logger.Log("TagCatalog", "[ERROR] export failed: " + ex);
			return false;
		}
	}

	private static AnimusForgeTagCatalogSnapshot BuildSnapshotCore()
	{
		Dictionary<string, AnimusForgeTagCatalogEntry> entries = new Dictionary<string, AnimusForgeTagCatalogEntry>(StringComparer.OrdinalIgnoreCase);
		AnimusForgeTagCatalogSnapshot snapshot = new AnimusForgeTagCatalogSnapshot
		{
			BuiltUtc = DateTime.UtcNow
		};
		AddBuiltInEntries(entries);
		foreach (string root in ResolveScanRoots())
		{
			if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
			{
				continue;
			}
			AddSourceRoot(snapshot, root);
			ScanDirectory(root, entries, snapshot);
		}
		ScanCurrentAssembly(entries);
		AddSiegeFallbackRules(entries);
		AddVillageAftermathRules(entries);
		snapshot.Entries.AddRange(entries.Values.OrderBy((AnimusForgeTagCatalogEntry x) => CategoryOrder(x.Category)).ThenBy((AnimusForgeTagCatalogEntry x) => x.Tag, StringComparer.OrdinalIgnoreCase));
		for (int i = 0; i < snapshot.Entries.Count; i++)
		{
			snapshot.Entries[i].Id = "tag:" + i.ToString();
		}
		return snapshot;
	}

	private static IEnumerable<string> ResolveScanRoots()
	{
		List<string> roots = new List<string>();
		AddRoot(roots, AnimusForgeModulePaths.GetCurrentModuleRoot());
		try
		{
			string assemblyDir = Path.GetDirectoryName(typeof(AnimusForgeTagCatalog).Assembly.Location);
			string cursor = assemblyDir;
			for (int i = 0; i < 8 && !string.IsNullOrWhiteSpace(cursor); i++)
			{
				string nestedModule = Path.Combine(cursor, "AnimusForge");
				if (Directory.Exists(Path.Combine(nestedModule, "ModuleData")))
				{
					AddRoot(roots, nestedModule);
				}
				if (File.Exists(Path.Combine(cursor, "SubModule.xml")) && Directory.Exists(Path.Combine(cursor, "ModuleData")))
				{
					AddRoot(roots, cursor);
				}
				cursor = Directory.GetParent(cursor)?.FullName;
			}
		}
		catch
		{
		}
		try
		{
			string current = Directory.GetCurrentDirectory();
			AddRoot(roots, Path.Combine(current, "AnimusForge"));
			if (File.Exists(Path.Combine(current, "SubModule.xml")) && Directory.Exists(Path.Combine(current, "ModuleData")))
			{
				AddRoot(roots, current);
			}
		}
		catch
		{
		}
		return roots;
	}

	private static string ResolveExportModuleRoot(AnimusForgeTagCatalogSnapshot snapshot)
	{
		try
		{
			string currentRoot = AnimusForgeModulePaths.GetCurrentModuleRoot();
			if (IsAnimusForgeModuleRoot(currentRoot))
			{
				return Path.GetFullPath(currentRoot);
			}
		}
		catch
		{
		}
		try
		{
			foreach (string root in snapshot?.SourceRoots ?? new List<string>())
			{
				if (IsAnimusForgeModuleRoot(root))
				{
					return Path.GetFullPath(root);
				}
			}
		}
		catch
		{
		}
		return "";
	}

	private static bool IsAnimusForgeModuleRoot(string path)
	{
		try
		{
			return !string.IsNullOrWhiteSpace(path)
				&& Directory.Exists(path)
				&& File.Exists(Path.Combine(path, "SubModule.xml"))
				&& Directory.Exists(Path.Combine(path, "ModuleData"));
		}
		catch
		{
			return false;
		}
	}

	private static void AddRoot(List<string> roots, string root)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
			{
				return;
			}
			string full = Path.GetFullPath(root);
			if (!roots.Any((string x) => string.Equals(x, full, StringComparison.OrdinalIgnoreCase)))
			{
				roots.Add(full);
			}
		}
		catch
		{
		}
	}

	private static void ScanDirectory(string root, Dictionary<string, AnimusForgeTagCatalogEntry> entries, AnimusForgeTagCatalogSnapshot snapshot)
	{
		Stack<string> pending = new Stack<string>();
		pending.Push(root);
		while (pending.Count > 0)
		{
			string dir = pending.Pop();
			string[] files = Array.Empty<string>();
			try
			{
				files = Directory.GetFiles(dir);
			}
			catch
			{
			}
			foreach (string file in files)
			{
				if (ShouldScanTextFile(file))
				{
					ScanTextFile(file, entries, snapshot);
				}
			}
			string[] dirs = Array.Empty<string>();
			try
			{
				dirs = Directory.GetDirectories(dir);
			}
			catch
			{
			}
			foreach (string child in dirs)
			{
				if (!ShouldSkipDirectory(child))
				{
					pending.Push(child);
				}
			}
		}
	}

	private static bool ShouldSkipDirectory(string path)
	{
		string name = Path.GetFileName(path ?? "") ?? "";
		return name.Equals(".git", StringComparison.OrdinalIgnoreCase)
			|| name.Equals("bin", StringComparison.OrdinalIgnoreCase)
			|| name.Equals("obj", StringComparison.OrdinalIgnoreCase)
			|| name.Equals("Logs", StringComparison.OrdinalIgnoreCase)
			|| name.Equals("ONNX", StringComparison.OrdinalIgnoreCase)
			|| name.StartsWith("原版游戏本体代码", StringComparison.OrdinalIgnoreCase);
	}

	private static bool ShouldScanTextFile(string path)
	{
		try
		{
			string ext = Path.GetExtension(path ?? "");
			if (!ext.Equals(".json", StringComparison.OrdinalIgnoreCase)
				&& !ext.Equals(".xml", StringComparison.OrdinalIgnoreCase)
				&& !ext.Equals(".xsl", StringComparison.OrdinalIgnoreCase)
				&& !ext.Equals(".txt", StringComparison.OrdinalIgnoreCase)
				&& !ext.Equals(".cs", StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
			return new FileInfo(path).Length <= MaxTextFileBytes;
		}
		catch
		{
			return false;
		}
	}

	private static void ScanTextFile(string path, Dictionary<string, AnimusForgeTagCatalogEntry> entries, AnimusForgeTagCatalogSnapshot snapshot)
	{
		try
		{
			string text = File.ReadAllText(path, Encoding.UTF8);
			snapshot.ScannedFileCount++;
			string source = BuildSourceLabel(path);
			if (Path.GetExtension(path).Equals(".json", StringComparison.OrdinalIgnoreCase))
			{
				ExtractJsonTagFields(text, source, entries);
			}
			ExtractTagsFromText(text, source, entries, "");
		}
		catch (Exception ex)
		{
			Logger.Log("TagCatalog", "[WARN] scan file failed path=" + path + " error=" + ex.Message);
		}
	}

	private static void ExtractJsonTagFields(string text, string source, Dictionary<string, AnimusForgeTagCatalogEntry> entries)
	{
		try
		{
			JToken root = JToken.Parse(text);
			ExtractJsonTagFields(root, source, entries, "");
		}
		catch
		{
		}
	}

	private static void ExtractJsonTagFields(JToken token, string source, Dictionary<string, AnimusForgeTagCatalogEntry> entries, string context)
	{
		if (token == null)
		{
			return;
		}
		if (token is JObject obj)
		{
			string tag = ReadJsonString(obj, "Tag");
			if (!string.IsNullOrWhiteSpace(tag))
			{
				string description = ReadJsonString(obj, "Description");
				AddTag(entries, tag, ClassifyTag(tag, explicitJsonRule: true), description, source, context);
			}
			string label = FirstNonEmpty(ReadJsonString(obj, "TopicLabel"), ReadJsonString(obj, "Id"), ReadJsonString(obj, "Code"));
			string nextContext = CombineContext(context, label);
			foreach (JProperty property in obj.Properties())
			{
				if (string.Equals(property.Name, "Tag", StringComparison.OrdinalIgnoreCase) || string.Equals(property.Name, "Description", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				string childContext = nextContext;
				if (string.IsNullOrWhiteSpace(label) && property.Value is JObject)
				{
					childContext = CombineContext(context, property.Name);
				}
				else if (string.Equals(property.Name, "PostprocessRules", StringComparison.OrdinalIgnoreCase))
				{
					childContext = CombineContext(nextContext, "PostprocessRules");
				}
				ExtractJsonTagFields(property.Value, source, entries, childContext);
			}
			return;
		}
		if (token is JArray array)
		{
			foreach (JToken child in array)
			{
				ExtractJsonTagFields(child, source, entries, context);
			}
		}
	}

	private static string ReadJsonString(JObject obj, string propertyName)
	{
		try
		{
			return obj.TryGetValue(propertyName, StringComparison.OrdinalIgnoreCase, out var token) ? (token?.ToString() ?? "").Trim() : "";
		}
		catch
		{
			return "";
		}
	}

	private static void ExtractTagsFromText(string text, string source, Dictionary<string, AnimusForgeTagCatalogEntry> entries, string context)
	{
		if (string.IsNullOrEmpty(text))
		{
			return;
		}
		try
		{
			foreach (Match match in BracketTagRegex.Matches(text))
			{
				string tag = match.Value;
				if (IsLikelyConcreteTag(tag))
				{
					AddTag(entries, tag, ClassifyTag(tag, explicitJsonRule: false), "", source, context);
				}
			}
		}
		catch
		{
		}
	}

	private static void ScanCurrentAssembly(Dictionary<string, AnimusForgeTagCatalogEntry> entries)
	{
		try
		{
			string assemblyPath = typeof(AnimusForgeTagCatalog).Assembly.Location;
			if (string.IsNullOrWhiteSpace(assemblyPath) || !File.Exists(assemblyPath))
			{
				return;
			}
			FileInfo info = new FileInfo(assemblyPath);
			if (info.Length <= 0 || info.Length > MaxAssemblyBytes)
			{
				return;
			}
			byte[] bytes = File.ReadAllBytes(assemblyPath);
			string source = "程序集:" + Path.GetFileName(assemblyPath);
			ExtractTagsFromText(Encoding.UTF8.GetString(bytes), source, entries, "compiled-utf8");
			ExtractTagsFromText(Encoding.Unicode.GetString(bytes), source, entries, "compiled-utf16");
		}
		catch (Exception ex)
		{
			Logger.Log("TagCatalog", "[WARN] scan assembly failed: " + ex.Message);
		}
	}

	private static void AddSiegeFallbackRules(Dictionary<string, AnimusForgeTagCatalogEntry> entries)
	{
		try
		{
			foreach (SiegePostprocessRuleDefinition rule in SiegePostprocessRuleCatalog.GetFallbackRules())
			{
				AddTag(entries, rule.Tag, "后处理/GCCZ", rule.Description, "SiegePostprocessRuleCatalog", SiegePostprocessRuleCatalog.RuleId);
			}
		}
		catch
		{
		}
	}

	private static void AddVillageAftermathRules(Dictionary<string, AnimusForgeTagCatalogEntry> entries)
	{
		foreach (VillageAftermathActionKind action in VillageAftermathActionTagCatalog.GetCanonicalOrder())
		{
			if (VillageAftermathActionTagCatalog.TryGetCanonicalTag(action, out string tag))
			{
				AddTag(entries, tag, "后处理/GCCZ村庄", "仅在有权限的普通村庄场景中，由玩家明确命令触发：" + action, "VillageAftermathActionTagCatalog", VillageAftermathRuntimePromptProfile.RuleId);
			}
		}
	}

	private static void AddBuiltInEntries(Dictionary<string, AnimusForgeTagCatalogEntry> entries)
	{
		AddTag(entries, "正文（自然语言）", "正文", "不是动作标签。标签测试输入框里除动作标签外的文本会作为 NPC 可见正文显示或写入历史。", "内置说明", "");
		AddTag(entries, "[CONTENT]", "正文/历史", "内部正文分隔标记。通常不是玩家手写动作标签。", "内置说明", "");
		AddTag(entries, "[AFEF玩家行为补充]", "正文/事实", "玩家行为事实写入标记，用于告诉 LLM 已实际发生的玩家行为。", "内置说明", "");
		AddTag(entries, "[AFEF NPC行为补充]", "正文/事实", "NPC行为事实写入标记，用于告诉 LLM 已实际发生的 NPC 行为。", "内置说明", "");
	}

	private static void AddTag(Dictionary<string, AnimusForgeTagCatalogEntry> entries, string rawTag, string category, string description, string source, string context)
	{
		string tag = NormalizeTag(rawTag);
		if (string.IsNullOrWhiteSpace(tag) || !IsLikelyConcreteTag(tag))
		{
			return;
		}
		string key = tag.ToUpperInvariant();
		if (!entries.TryGetValue(key, out var entry))
		{
			entry = new AnimusForgeTagCatalogEntry
			{
				Tag = tag,
				Category = string.IsNullOrWhiteSpace(category) ? "标签" : category.Trim(),
				Description = (description ?? "").Trim()
			};
			entries[key] = entry;
		}
		else
		{
			if (CategoryOrder(category) < CategoryOrder(entry.Category))
			{
				entry.Category = category;
			}
			string cleanDescription = (description ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(cleanDescription) && (string.IsNullOrWhiteSpace(entry.Description) || cleanDescription.Length > entry.Description.Length))
			{
				entry.Description = cleanDescription;
			}
		}
		string sourceText = CombineContext(source, context);
		if (!string.IsNullOrWhiteSpace(sourceText) && !entry.Sources.Any((string x) => string.Equals(x, sourceText, StringComparison.OrdinalIgnoreCase)))
		{
			entry.Sources.Add(sourceText);
		}
	}

	private static string NormalizeTag(string rawTag)
	{
		string text = (rawTag ?? "").Replace("\r", "").Replace("\n", " ").Trim();
		while (text.Contains("  "))
		{
			text = text.Replace("  ", " ");
		}
		return text;
	}

	private static bool IsLikelyConcreteTag(string tag)
	{
		string text = (tag ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		if (string.Equals(text, "正文（自然语言）", StringComparison.Ordinal))
		{
			return true;
		}
		if (!text.StartsWith("[", StringComparison.Ordinal) || !text.EndsWith("]", StringComparison.Ordinal))
		{
			return false;
		}
		if (text.IndexOf('\\') >= 0 || text.IndexOf('"') >= 0 || text.IndexOf("(?", StringComparison.Ordinal) >= 0 || text.IndexOf("[^", StringComparison.Ordinal) >= 0 || text.IndexOf("StringComparison", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf(".Length", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return false;
		}
		return text.StartsWith("[ACTION:", StringComparison.OrdinalIgnoreCase)
			|| text.Equals("[A:H_J_P_P_C/L]", StringComparison.OrdinalIgnoreCase)
			|| text.Equals("[A:H_J_P_P_C&L]", StringComparison.OrdinalIgnoreCase)
			|| text.Equals("[A:H_J_P_P_C]", StringComparison.OrdinalIgnoreCase)
			|| text.Equals("[A:H_J_P_P_L]", StringComparison.OrdinalIgnoreCase)
			|| text.Equals("[A:C_J_P_K]", StringComparison.OrdinalIgnoreCase)
			|| text.StartsWith("[A:C_J_K:", StringComparison.OrdinalIgnoreCase)
			|| text.Equals("[A:P_J_K_M]", StringComparison.OrdinalIgnoreCase)
			|| text.Equals("[A:P_J_K_V]", StringComparison.OrdinalIgnoreCase)
			|| text.Equals("[A:P_L_K]", StringComparison.OrdinalIgnoreCase)
			|| text.StartsWith("[AD:", StringComparison.OrdinalIgnoreCase)
			|| text.StartsWith("[ADP:", StringComparison.OrdinalIgnoreCase)
			|| text.StartsWith("[ASS:", StringComparison.OrdinalIgnoreCase)
			|| text.StartsWith("[GUI:", StringComparison.OrdinalIgnoreCase)
			|| text.StartsWith("[ATT:", StringComparison.OrdinalIgnoreCase)
			|| text.StartsWith("[ATP:", StringComparison.OrdinalIgnoreCase)
			|| text.Equals("[FOL]", StringComparison.OrdinalIgnoreCase)
			|| text.Equals("[STP]", StringComparison.OrdinalIgnoreCase)
			|| text.Equals("[END]", StringComparison.OrdinalIgnoreCase)
			|| text.StartsWith("[RELAY:", StringComparison.OrdinalIgnoreCase)
			|| text.StartsWith("[AFEF", StringComparison.OrdinalIgnoreCase)
			|| text.StartsWith("[AF_SCENE_SESSION:", StringComparison.OrdinalIgnoreCase)
			|| text.Equals("[CONTENT]", StringComparison.OrdinalIgnoreCase);
	}

	private static string ClassifyTag(string tag, bool explicitJsonRule)
	{
		string text = (tag ?? "").Trim();
		if (string.Equals(text, "正文（自然语言）", StringComparison.Ordinal))
		{
			return "正文";
		}
		if (text.StartsWith("[AFEF", StringComparison.OrdinalIgnoreCase) || text.StartsWith("[AF_SCENE_SESSION:", StringComparison.OrdinalIgnoreCase) || text.Equals("[CONTENT]", StringComparison.OrdinalIgnoreCase))
		{
			return "正文/历史";
		}
		if (text.StartsWith("[RELAY:", StringComparison.OrdinalIgnoreCase))
		{
			return "后处理/接力";
		}
		if (SiegeActionTagCatalog.ContainsRecognizedTag(text))
		{
			return "后处理/GCCZ";
		}
		if (text.StartsWith("[VILLAGE_ACTION:", StringComparison.OrdinalIgnoreCase))
		{
			return "后处理/GCCZ村庄";
		}
		if (text.StartsWith("[ACTION:", StringComparison.OrdinalIgnoreCase)
			|| text.Equals("[A:H_J_P_P_C/L]", StringComparison.OrdinalIgnoreCase)
			|| text.Equals("[A:H_J_P_P_C&L]", StringComparison.OrdinalIgnoreCase)
			|| text.Equals("[A:H_J_P_P_C]", StringComparison.OrdinalIgnoreCase)
			|| text.Equals("[A:H_J_P_P_L]", StringComparison.OrdinalIgnoreCase)
			|| text.Equals("[A:C_J_P_K]", StringComparison.OrdinalIgnoreCase)
			|| text.StartsWith("[A:C_J_K:", StringComparison.OrdinalIgnoreCase)
			|| text.Equals("[A:P_J_K_M]", StringComparison.OrdinalIgnoreCase)
			|| text.Equals("[A:P_J_K_V]", StringComparison.OrdinalIgnoreCase)
			|| text.Equals("[A:P_L_K]", StringComparison.OrdinalIgnoreCase)
			|| text.StartsWith("[AD", StringComparison.OrdinalIgnoreCase)
			|| text.StartsWith("[ATT", StringComparison.OrdinalIgnoreCase)
			|| text.StartsWith("[ATP", StringComparison.OrdinalIgnoreCase)
			|| text.StartsWith("[ASS", StringComparison.OrdinalIgnoreCase)
			|| text.StartsWith("[GUI", StringComparison.OrdinalIgnoreCase)
			|| text.Equals("[FOL]", StringComparison.OrdinalIgnoreCase)
			|| text.Equals("[STP]", StringComparison.OrdinalIgnoreCase)
			|| text.Equals("[END]", StringComparison.OrdinalIgnoreCase))
		{
			return explicitJsonRule ? "后处理/规则表" : "后处理";
		}
		return "标签";
	}

	private static string BuildExportText(AnimusForgeTagCatalogSnapshot snapshot, string moduleRoot)
	{
		StringBuilder stringBuilder = new StringBuilder();
		int bodyCount = snapshot.Entries.Count((AnimusForgeTagCatalogEntry x) => (x.Category ?? "").StartsWith("正文", StringComparison.Ordinal));
		int postprocessCount = snapshot.Entries.Count((AnimusForgeTagCatalogEntry x) => (x.Category ?? "").StartsWith("后处理", StringComparison.Ordinal));
		stringBuilder.AppendLine("AnimusForge 标签列表");
		stringBuilder.AppendLine("导出时间：" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
		stringBuilder.AppendLine("模块目录：" + (moduleRoot ?? ""));
		stringBuilder.AppendLine("标签总数：" + snapshot.Entries.Count.ToString(CultureInfo.InvariantCulture));
		stringBuilder.AppendLine("正文/历史：" + bodyCount.ToString(CultureInfo.InvariantCulture));
		stringBuilder.AppendLine("后处理：" + postprocessCount.ToString(CultureInfo.InvariantCulture));
		stringBuilder.AppendLine("扫描文件：" + snapshot.ScannedFileCount.ToString(CultureInfo.InvariantCulture));
		if (snapshot.SourceRoots.Count > 0)
		{
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("扫描来源：");
			foreach (string root in snapshot.SourceRoots)
			{
				stringBuilder.AppendLine("- " + root);
			}
		}
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("说明：此文件由游戏内 U 键 AnimusForge 终端的一键导出生成。列表来自当前模块文件、当前程序集字符串和内置运行时规则。");
		foreach (IGrouping<string, AnimusForgeTagCatalogEntry> group in snapshot.Entries.GroupBy((AnimusForgeTagCatalogEntry x) => string.IsNullOrWhiteSpace(x.Category) ? "标签" : x.Category).OrderBy((IGrouping<string, AnimusForgeTagCatalogEntry> x) => CategoryOrder(x.Key)).ThenBy((IGrouping<string, AnimusForgeTagCatalogEntry> x) => x.Key, StringComparer.OrdinalIgnoreCase))
		{
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("==== " + group.Key + " ====");
			foreach (AnimusForgeTagCatalogEntry entry in group.OrderBy((AnimusForgeTagCatalogEntry x) => x.Tag, StringComparer.OrdinalIgnoreCase))
			{
				stringBuilder.AppendLine();
				stringBuilder.AppendLine(entry.Tag ?? "");
				if (!string.IsNullOrWhiteSpace(entry.Description))
				{
					stringBuilder.AppendLine("说明：" + entry.Description.Trim());
				}
				if (entry.Sources.Count > 0)
				{
					stringBuilder.AppendLine("来源：");
					foreach (string source in entry.Sources)
					{
						stringBuilder.AppendLine("- " + source);
					}
				}
			}
		}
		return stringBuilder.ToString().TrimEnd() + Environment.NewLine;
	}

	private static int CategoryOrder(string category)
	{
		string text = category ?? "";
		if (text.StartsWith("正文", StringComparison.Ordinal))
		{
			return 0;
		}
		if (text.StartsWith("后处理/规则表", StringComparison.Ordinal))
		{
			return 1;
		}
		if (text.StartsWith("后处理/GCCZ", StringComparison.Ordinal))
		{
			return 2;
		}
		if (text.StartsWith("后处理", StringComparison.Ordinal))
		{
			return 3;
		}
		return 9;
	}

	private static string BuildSourceLabel(string path)
	{
		try
		{
			string fileName = Path.GetFileName(path);
			string dirName = Path.GetFileName(Path.GetDirectoryName(path) ?? "");
			return string.IsNullOrWhiteSpace(dirName) ? fileName : dirName + "/" + fileName;
		}
		catch
		{
			return path ?? "";
		}
	}

	private static void AddSourceRoot(AnimusForgeTagCatalogSnapshot snapshot, string root)
	{
		try
		{
			string full = Path.GetFullPath(root);
			if (!snapshot.SourceRoots.Any((string x) => string.Equals(x, full, StringComparison.OrdinalIgnoreCase)))
			{
				snapshot.SourceRoots.Add(full);
			}
		}
		catch
		{
		}
	}

	private static string CombineContext(string left, string right)
	{
		left = (left ?? "").Trim();
		right = (right ?? "").Trim();
		if (string.IsNullOrWhiteSpace(left))
		{
			return right;
		}
		if (string.IsNullOrWhiteSpace(right))
		{
			return left;
		}
		return left + " / " + right;
	}

	private static string FirstNonEmpty(params string[] values)
	{
		foreach (string value in values ?? Array.Empty<string>())
		{
			if (!string.IsNullOrWhiteSpace(value))
			{
				return value.Trim();
			}
		}
		return "";
	}
}
