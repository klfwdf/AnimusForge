using System;
using System.IO;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

internal static class SettlementBalanceSettings
{
	internal const string FileName = "SettlementBalanceSettings.json";
	private static SettlementBalanceSettingsStore _store;
	internal static SettlementBalanceSnapshot Current => Volatile.Read(ref _store)?.Current ?? SettlementBalanceSnapshot.Default;

	// Called on campaign/UI initialization, never by a model getter or a daily write.
	internal static void Initialize()
	{
		if (_store != null) return;
		try
		{
			string directory = DuelSettings.GetCustomPromptTextStoreDirectoryForPolicyPrompts();
			if (!string.IsNullOrWhiteSpace(directory))
				Interlocked.CompareExchange(ref _store, new SettlementBalanceSettingsStore(Path.Combine(directory, FileName)), null);
		}
		catch (Exception ex) { PolicySystemLog.Failure("Balance", "settings-path-failed", ex.Message, "Using cached defaults; no file written."); }
	}

	internal static bool TrySave(SettlementBalanceSnapshot snapshot, out string error)
	{
		Initialize();
		if (_store == null) { error = "无法定位政策设置目录。"; return false; }
		return _store.TrySave(snapshot, out error);
	}

	internal static string Encode(SettlementBalanceSnapshot snapshot)
	{
		var limits = new JObject();
		foreach (var definition in SettlementBalanceRules.Definitions)
		{
			var limit = snapshot.Get(definition.Metric);
			limits[definition.Id] = new JObject { ["Enabled"] = limit.Enabled, ["Value"] = limit.Value };
		}
		return new JObject { ["Version"] = 1, ["Limits"] = limits }.ToString(Formatting.Indented);
	}

	internal static bool TryDecode(string json, out SettlementBalanceSnapshot snapshot, out string error)
	{
		snapshot = SettlementBalanceSnapshot.Default;
		error = string.Empty;
		try
		{
			var document = JObject.Parse(json);
			if (document["Version"]?.Type != JTokenType.Integer || document["Version"].Value<int>() != 1 || !(document["Limits"] is JObject limits))
				throw new FormatException("总量上限配置版本或结构无效。");
			var candidate = snapshot;
			foreach (var definition in SettlementBalanceRules.Definitions)
			{
				JToken token = limits[definition.Id];
				if (token == null) continue;
				if (!(token is JObject row) || row["Enabled"]?.Type != JTokenType.Boolean || row["Value"]?.Type != JTokenType.Integer)
					throw new FormatException(definition.Name + "配置无效。");
				candidate = candidate.With(definition.Metric, row["Enabled"].Value<bool>(), row["Value"].Value<int>());
			}
			if (!candidate.TryValidate(out error)) return false;
			snapshot = candidate;
			return true;
		}
		catch (Exception ex) { error = ex.Message; return false; }
	}
}

internal sealed class SettlementBalanceSettingsStore
{
	private readonly string _path;
	private SettlementBalanceSnapshot _snapshot;
	internal SettlementBalanceSnapshot Current => Volatile.Read(ref _snapshot);
	internal SettlementBalanceSettingsStore(string path)
	{
		_path = path;
		_snapshot = SettlementBalanceSnapshot.Default;
		try
		{
			if (!File.Exists(path)) return;
			if (new FileInfo(path).Length > 65536) throw new FormatException("总量上限配置文件过大。");
			if (!SettlementBalanceSettings.TryDecode(File.ReadAllText(path, new UTF8Encoding(false, true)), out var loaded, out string error))
				throw new FormatException(error);
			_snapshot = loaded;
		}
		catch (Exception ex) { PolicySystemLog.Failure("Balance", "settings-load-failed", ex.Message, "Using defaults; original file retained."); }
	}

	internal bool TrySave(SettlementBalanceSnapshot snapshot, out string error)
	{
		error = string.Empty;
		if (snapshot == null) { error = "配置为空。"; return false; }
		if (!snapshot.TryValidate(out error)) return false;
		string temp = _path + ".tmp-" + Guid.NewGuid().ToString("N");
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(_path));
			using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
			using (var writer = new StreamWriter(stream, new UTF8Encoding(false), 4096, leaveOpen: true))
			{
				writer.Write(SettlementBalanceSettings.Encode(snapshot));
				writer.Flush();
				stream.Flush(flushToDisk: true);
			}
			if (!SettlementBalanceSettings.TryDecode(File.ReadAllText(temp), out _, out error)) return false;
			if (File.Exists(_path)) File.Replace(temp, _path, null);
			else File.Move(temp, _path);
			Volatile.Write(ref _snapshot, snapshot);
			return true;
		}
		catch (Exception ex) { error = ex.Message; return false; }
		finally { if (File.Exists(temp)) { try { File.Delete(temp); } catch { } } }
	}
}
