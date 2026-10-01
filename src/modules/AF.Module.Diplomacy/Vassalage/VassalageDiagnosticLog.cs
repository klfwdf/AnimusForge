using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace AnimusForge;

internal static class VassalageDiagnosticLog
{
	private const int MaxStringLength = 300;
	private const int MaxListItems = 12;
	private const int MaxFieldsPerEvent = 36;
	private static readonly object FileLock = new object();
	private static readonly string SessionId = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
	private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
	private static long _sequence;

	public static void Event(string stage, IDictionary<string, object> fields = null)
	{
		try
		{
			if (!ShouldWriteEvent(stage, fields))
			{
				return;
			}
			string path = AnimusForgeModulePaths.GetLogFilePath("AF_Vassalage_Diagnostics.jsonl");
			string directory = Path.GetDirectoryName(path);
			if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}
			Dictionary<string, object> entry = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
			{
				["tsUtc"] = DateTime.UtcNow.ToString("o"),
				["session"] = SessionId,
				["seq"] = Interlocked.Increment(ref _sequence),
				["stage"] = Preview(stage ?? "", 160),
				["campaignDay"] = SafeGetCampaignDay(),
				["mainHero"] = DescribeHero(SafeGetMainHero()),
				["playerKingdom"] = DescribeKingdom(SafeGetPlayerKingdom())
			};
			AddIfNotEmpty(entry, "traceId", SafeGetTraceId());
			AddIfNotEmpty(entry, "traceChannel", SafeGetTraceChannel());
			if (fields != null)
			{
				foreach (KeyValuePair<string, object> field in fields)
				{
					string key = (field.Key ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(key) && ShouldKeepField(key))
					{
						entry[key] = NormalizeValue(field.Value);
						if (entry.Count >= MaxFieldsPerEvent)
						{
							entry["fieldsTruncated"] = true;
							break;
						}
					}
				}
			}
			string line = JsonConvert.SerializeObject(entry, Formatting.None) + Environment.NewLine;
			lock (FileLock)
			{
				File.AppendAllText(path, line, Utf8NoBom);
			}
		}
		catch
		{
		}
	}

	private static void AddIfNotEmpty(Dictionary<string, object> entry, string key, string value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			entry[key] = Preview(value, 120);
		}
	}

	private static bool ShouldWriteEvent(string stage, IDictionary<string, object> fields)
	{
		string text = (stage ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		string lower = text.ToLowerInvariant();
		if (ContainsAny(lower, "error", "exception", "fail", "reject", "invalid", "block", "drop", "break", "collapsed", "unsupported"))
		{
			return true;
		}
		if (lower == "behavior.session_launched" || lower == "behavior.game_load_finished")
		{
			return true;
		}
		if (lower == "action.apply.done"
			|| lower == "agreement.create.success"
			|| lower == "agreement.revise.success"
			|| lower == "agreement.create.sync_wars"
			|| lower == "tributary_payment.settled"
			|| lower == "protection.apply"
			|| lower == "obedience.adjust"
			|| lower == "obedience.protection_success"
			|| lower == "war_declared.queue_protection"
			|| lower == "war_declared.tributary_autonomous_war"
			|| lower == "war_declared.tributary_controlled_subject"
			|| lower == "war_declared.tributary_tributary_notice"
			|| lower == "make_peace.sync_protected_subject"
			|| lower == "protected_subject_war.record"
			|| lower == "pending_diplomacy.done"
			|| lower == "diplomacy.declare_war.apply.done"
			|| lower == "diplomacy.make_peace.apply.done")
		{
			return true;
		}
		if (lower.StartsWith("notice.queue_", StringComparison.OrdinalIgnoreCase)
			|| lower.EndsWith("_protection_accepted", StringComparison.OrdinalIgnoreCase)
			|| lower.EndsWith("_protection_refused", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		return false;
	}

	private static bool ContainsAny(string text, params string[] tokens)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		foreach (string token in tokens)
		{
			if (!string.IsNullOrWhiteSpace(token) && text.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		return false;
	}

	private static bool ShouldKeepField(string key)
	{
		string lower = (key ?? "").Trim().ToLowerInvariant();
		if (string.IsNullOrWhiteSpace(lower))
		{
			return false;
		}
		if (ContainsAny(lower,
			"reason", "status", "error", "exception", "message", "phase", "result",
			"id", "name", "kind", "agreementid", "noticeid", "protectedkey", "kingdomid", "clanid",
			"type", "action", "token", "campaigndate",
			"playerkingdom", "targetkingdom", "suzerain", "vassal", "enemy", "declaring", "target", "subject", "hero", "settlement",
			"count", "day", "before", "after", "delta", "ok", "can", "is", "was", "atwar", "already", "attempt", "return", "removed", "synced", "queued"))
		{
			return true;
		}
		return false;
	}

	public static string GetDiagnosticLogPath()
	{
		try
		{
			return AnimusForgeModulePaths.GetLogFilePath("AF_Vassalage_Diagnostics.jsonl");
		}
		catch
		{
			return "AF_Vassalage_Diagnostics.jsonl";
		}
	}

	public static string DescribeHero(Hero hero)
	{
		if (hero == null)
		{
			return "null";
		}
		string name = "";
		try
		{
			name = hero.Name?.ToString() ?? "";
		}
		catch
		{
		}
		string kingdomId = "";
		try
		{
			kingdomId = hero.Clan?.Kingdom?.StringId ?? (hero.MapFaction as Kingdom)?.StringId ?? "";
		}
		catch
		{
		}
		return "hero=" + (hero.StringId ?? "") + ";name=" + name + ";kingdom=" + kingdomId;
	}

	public static string DescribeKingdom(Kingdom kingdom)
	{
		if (kingdom == null)
		{
			return "null";
		}
		string name = "";
		string leader = "";
		try
		{
			name = kingdom.Name?.ToString() ?? "";
			leader = kingdom.Leader?.StringId ?? "";
		}
		catch
		{
		}
		return "kingdom=" + (kingdom.StringId ?? "") + ";name=" + Preview(name, 80) + ";leader=" + leader;
	}

	public static string DescribeSettlement(Settlement settlement)
	{
		if (settlement == null)
		{
			return "null";
		}
		string name = "";
		string type = "";
		string mapFaction = "";
		string ownerClan = "";
		try
		{
			name = settlement.Name?.ToString() ?? "";
			if (settlement.IsTown)
			{
				type = "town";
			}
			else if (settlement.IsCastle)
			{
				type = "castle";
			}
			else if (settlement.IsVillage)
			{
				type = "village";
			}
			else
			{
				type = "other";
			}
			mapFaction = (settlement.MapFaction as Kingdom)?.StringId ?? settlement.MapFaction?.StringId ?? "";
			ownerClan = settlement.OwnerClan?.StringId ?? "";
		}
		catch
		{
		}
		return "settlement=" + (settlement.StringId ?? "")
			+ ";name=" + Preview(name, 80)
			+ ";type=" + type
			+ ";mapFaction=" + mapFaction
			+ ";ownerClan=" + ownerClan;
	}

	public static string Preview(string text, int maxLength = MaxStringLength)
	{
		string value = (text ?? "").Replace("\r", "\\r").Replace("\n", "\\n");
		if (maxLength <= 0)
		{
			maxLength = MaxStringLength;
		}
		return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...<truncated>";
	}

	private static string SafeGetTraceId()
	{
		try
		{
			return Logger.CurrentTraceId ?? "";
		}
		catch
		{
			return "";
		}
	}

	private static string SafeGetTraceChannel()
	{
		try
		{
			return Logger.CurrentChannel ?? "";
		}
		catch
		{
			return "";
		}
	}

	private static int SafeGetCampaignDay()
	{
		try
		{
			return Math.Max(0, (int)Math.Floor(CampaignTime.Now.ToDays));
		}
		catch
		{
			return -1;
		}
	}

	private static Hero SafeGetMainHero()
	{
		try
		{
			return Hero.MainHero;
		}
		catch
		{
			return null;
		}
	}

	private static Kingdom SafeGetPlayerKingdom()
	{
		try
		{
			return Clan.PlayerClan?.Kingdom ?? Hero.MainHero?.Clan?.Kingdom;
		}
		catch
		{
			return null;
		}
	}

	private static object NormalizeValue(object value)
	{
		if (value == null)
		{
			return "";
		}
		if (value is string text)
		{
			return Preview(text);
		}
		if (value is bool || value is int || value is long || value is float || value is double || value is decimal)
		{
			return value;
		}
		if (value is Enum)
		{
			return value.ToString();
		}
		if (value is IDictionary dictionary)
		{
			Dictionary<string, object> normalized = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
			foreach (DictionaryEntry entry in dictionary)
			{
				string key = Preview(entry.Key?.ToString() ?? "", 120);
				if (!string.IsNullOrWhiteSpace(key) && ShouldKeepField(key))
				{
					normalized[key] = NormalizeValue(entry.Value);
				}
				if (normalized.Count >= MaxListItems)
				{
					normalized["truncated"] = true;
					break;
				}
			}
			return normalized;
		}
		if (value is IEnumerable enumerable)
		{
			List<object> list = new List<object>();
			foreach (object item in enumerable)
			{
				list.Add(NormalizeValue(item));
				if (list.Count >= MaxListItems)
				{
					list.Add("<truncated>");
					break;
				}
			}
			return list;
		}
		try
		{
			return Preview(value.ToString());
		}
		catch
		{
			return "";
		}
	}
}
