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

internal static class KingdomAnnexationDiagnosticLog
{
	private const int MaxStringLength = 300;
	private const int MaxListItems = 12;
	private const int MaxFieldsPerEvent = 40;
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
			string path = AnimusForgeModulePaths.GetLogFilePath("AF_KingdomAnnexation_Diagnostics.jsonl");
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
				["stage"] = Preview(stage, 160),
				["campaignDay"] = SafeGetCampaignDay(),
				["mainHero"] = DescribeHero(SafeGetMainHero()),
				["playerKingdomCurrent"] = DescribeKingdom(SafeGetPlayerKingdom())
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

	public static string GetDiagnosticLogPath()
	{
		try
		{
			return AnimusForgeModulePaths.GetLogFilePath("AF_KingdomAnnexation_Diagnostics.jsonl");
		}
		catch
		{
			return "AF_KingdomAnnexation_Diagnostics.jsonl";
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
		if (ContainsAny(lower, "error", "exception", "fail", "reject", "invalid", "anomaly"))
		{
			return true;
		}
		if (lower == "tag.parse"
			|| lower == "apply.start.before"
			|| lower == "post_transfer.check"
			|| lower == "destroy.finish.after"
			|| lower == "vassalage.cleanup.finish")
		{
			return true;
		}
		if (lower == "clan.transfer")
		{
			return FieldBool(fields, "anomaly") || !FieldBool(fields, "ok");
		}
		if (lower == "diplomacy.peace.after")
		{
			return FieldBool(fields, "actionAttempted") || !string.IsNullOrWhiteSpace(FieldString(fields, "error"));
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

	private static bool FieldBool(IDictionary<string, object> fields, string key)
	{
		if (fields == null || key == null || !fields.TryGetValue(key, out var value))
		{
			return false;
		}
		if (value is bool flag)
		{
			return flag;
		}
		bool parsed;
		return bool.TryParse(value?.ToString() ?? "", out parsed) && parsed;
	}

	private static string FieldString(IDictionary<string, object> fields, string key)
	{
		if (fields == null || key == null || !fields.TryGetValue(key, out var value))
		{
			return "";
		}
		return value?.ToString() ?? "";
	}

	private static bool ShouldKeepField(string key)
	{
		string lower = (key ?? "").Trim().ToLowerInvariant();
		if (string.IsNullOrWhiteSpace(lower))
		{
			return false;
		}
		if (ContainsAny(lower,
			"reason", "status", "error", "exception", "phase", "result",
			"id", "name", "kind", "targetkingdomid", "kingdom", "clan", "hero", "settlement", "leader",
			"count", "moved", "failed", "remaining", "residual", "still", "warstance",
			"peace", "vassalage", "broken", "ok", "anomaly", "attempt", "return", "atwar", "eliminated", "latency"))
		{
			return true;
		}
		return false;
	}

	public static Dictionary<string, object> BuildTagParseSnapshot(string targetKingdomId, Kingdom resolvedKingdom, Kingdom playerKingdom, Hero conversationHero)
	{
		return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
		{
			["targetKingdomId"] = Preview(targetKingdomId),
			["resolved"] = DescribeKingdom(resolvedKingdom),
			["playerKingdom"] = DescribeKingdom(playerKingdom),
			["conversationHero"] = DescribeHero(conversationHero)
		};
	}

	public static Dictionary<string, object> BuildRejectSnapshot(string reason, Kingdom playerKingdom, Kingdom targetKingdom, Hero conversationHero)
	{
		return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
		{
			["reason"] = Preview(reason),
			["playerKingdom"] = DescribeKingdom(playerKingdom),
			["targetKingdom"] = DescribeKingdom(targetKingdom),
			["conversationHero"] = DescribeHero(conversationHero)
		};
	}

	public static Dictionary<string, object> BuildApplySnapshot(Kingdom playerKingdom, Kingdom targetKingdom, Hero conversationHero, IEnumerable<Clan> targetClans, IEnumerable<Settlement> targetSettlements)
	{
		List<Clan> clans = SafeList(targetClans);
		List<Settlement> settlements = SafeList(targetSettlements);
		return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
		{
			["playerKingdom"] = DescribeKingdom(playerKingdom),
			["targetKingdom"] = DescribeKingdom(targetKingdom),
			["conversationHero"] = DescribeHero(conversationHero),
			["targetClansCount"] = clans.Count,
			["targetSettlementsCount"] = settlements.Count,
			["playerKingdomFactionsAtWarWith"] = DescribeFactionsAtWarWith(playerKingdom),
			["targetKingdomFactionsAtWarWith"] = DescribeFactionsAtWarWith(targetKingdom),
			["playerAtWarWithTarget"] = IsAtWar(playerKingdom, targetKingdom),
			["targetAtWarWithPlayer"] = IsAtWar(targetKingdom, playerKingdom)
		};
	}

	public static Dictionary<string, object> BuildPostTransferSnapshot(Kingdom playerKingdom, Kingdom targetKingdom, int movedCount, int failedCount, IEnumerable<Clan> remainingClans, IEnumerable<Settlement> residualSettlements, bool peaceApplied, int brokenVassalageAgreements)
	{
		List<Clan> clans = SafeList(remainingClans);
		List<Settlement> settlements = SafeList(residualSettlements);
		return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
		{
			["playerKingdom"] = DescribeKingdom(playerKingdom),
			["targetKingdom"] = DescribeKingdom(targetKingdom),
			["movedCount"] = movedCount,
			["failedCount"] = failedCount,
			["remainingClansCount"] = clans.Count,
			["remainingClans"] = clans.Select(DescribeClan).ToList(),
			["residualTargetSettlementsCount"] = settlements.Count,
			["residualTargetSettlements"] = settlements.Select(DescribeSettlement).ToList(),
			["peaceApplied"] = peaceApplied,
			["brokenVassalageAgreements"] = brokenVassalageAgreements,
			["playerAtWarWithTarget"] = IsAtWar(playerKingdom, targetKingdom),
			["targetAtWarWithPlayer"] = IsAtWar(targetKingdom, playerKingdom)
		};
	}

	public static Dictionary<string, object> BuildClanTransferSnapshot(Kingdom playerKingdom, Kingdom targetKingdom, Clan clan, Kingdom beforeKingdom, Kingdom afterKingdom, bool wasRulingClan, bool ok, bool anomaly, string result)
	{
		return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
		{
			["ok"] = ok,
			["anomaly"] = anomaly,
			["result"] = Preview(result),
			["clan"] = DescribeClan(clan),
			["leader"] = DescribeHero(SafeGetClanLeader(clan)),
			["beforeKingdom"] = DescribeKingdom(beforeKingdom),
			["afterKingdom"] = DescribeKingdom(afterKingdom),
			["playerKingdom"] = DescribeKingdom(playerKingdom),
			["targetKingdom"] = DescribeKingdom(targetKingdom),
			["wasRulingClan"] = wasRulingClan,
			["fiefCount"] = CountClanSettlements(clan),
			["warPartyCount"] = CountClanWarParties(clan),
			["clanFactionsAtWarWith"] = DescribeFactionsAtWarWith(clan),
			["playerKingdomFactionsAtWarWith"] = DescribeFactionsAtWarWith(playerKingdom),
			["targetKingdomFactionsAtWarWith"] = DescribeFactionsAtWarWith(targetKingdom)
		};
	}

	public static Dictionary<string, object> BuildDestroyFinishSnapshot(Kingdom playerKingdom, Kingdom targetKingdom, IEnumerable<Clan> originalTargetClans, IEnumerable<Settlement> originalTargetSettlements, bool destroyOk, string destroyResult, int movedCount, int failedCount, double latencyMs)
	{
		List<Clan> clansStillInTarget = FindClansStillInKingdom(targetKingdom);
		List<Settlement> settlementsStillInTarget = FindSettlementsStillInKingdom(targetKingdom);
		List<string> warStancesToTarget = FindWarStancesToTarget(targetKingdom);
		List<Clan> originalClans = SafeList(originalTargetClans);
		List<Settlement> originalSettlements = SafeList(originalTargetSettlements);
		return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
		{
			["ok"] = destroyOk,
			["result"] = Preview(destroyResult),
			["playerKingdom"] = DescribeKingdom(playerKingdom),
			["targetKingdom"] = DescribeKingdom(targetKingdom),
			["targetKingdomIsEliminated"] = SafeBool(() => targetKingdom != null && targetKingdom.IsEliminated),
			["targetKingdomClansCount"] = SafeInt(() => targetKingdom?.Clans?.Count ?? 0),
			["targetClansCount"] = originalClans.Count,
			["targetSettlementsCount"] = originalSettlements.Count,
			["originalTargetClansCount"] = originalClans.Count,
			["originalTargetSettlementsCount"] = originalSettlements.Count,
			["movedCount"] = movedCount,
			["failedCount"] = failedCount,
			["hasClanStillInTargetKingdom"] = clansStillInTarget.Count > 0,
			["clansStillInTargetKingdomCount"] = clansStillInTarget.Count,
			["clansStillInTargetKingdom"] = clansStillInTarget.Select(DescribeClan).Take(MaxListItems).ToList(),
			["hasSettlementStillInTargetMapFaction"] = settlementsStillInTarget.Count > 0,
			["settlementsStillInTargetMapFactionCount"] = settlementsStillInTarget.Count,
			["settlementsStillInTargetMapFaction"] = settlementsStillInTarget.Select(DescribeSettlement).Take(MaxListItems).ToList(),
			["hasWarStanceToTargetKingdom"] = warStancesToTarget.Count > 0,
			["warStancesToTargetKingdomCount"] = warStancesToTarget.Count,
			["warStancesToTargetKingdom"] = warStancesToTarget.Take(MaxListItems).ToList(),
			["playerAtWarWithTargetAfterDestroy"] = IsAtWar(playerKingdom, targetKingdom),
			["targetAtWarWithPlayerAfterDestroy"] = IsAtWar(targetKingdom, playerKingdom),
			["latencyMs"] = Math.Round(Math.Max(0.0, latencyMs), 2)
		};
	}

	public static Dictionary<string, object> BuildPeaceSnapshot(Kingdom playerKingdom, Kingdom targetKingdom, string phase, bool actionAttempted = false, bool actionReturned = false, string error = "")
	{
		return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
		{
			["phase"] = Preview(phase),
			["actionAttempted"] = actionAttempted,
			["actionReturned"] = actionReturned,
			["error"] = Preview(error),
			["playerKingdom"] = DescribeKingdom(playerKingdom),
			["targetKingdom"] = DescribeKingdom(targetKingdom),
			["playerKingdomFactionsAtWarWith"] = DescribeFactionsAtWarWith(playerKingdom),
			["targetKingdomFactionsAtWarWith"] = DescribeFactionsAtWarWith(targetKingdom),
			["playerAtWarWithTarget"] = IsAtWar(playerKingdom, targetKingdom),
			["targetAtWarWithPlayer"] = IsAtWar(targetKingdom, playerKingdom)
		};
	}

	public static Dictionary<string, object> BuildVassalageCleanupSnapshot(Kingdom targetKingdom, bool calledBreakAgreementsForAnnexedKingdom, int removedCount, bool targetWasPlayerVassal, bool targetWasPlayerSuzerain, string phase, string error = "")
	{
		return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
		{
			["phase"] = Preview(phase),
			["targetKingdom"] = DescribeKingdom(targetKingdom),
			["calledBreakAgreementsForAnnexedKingdom"] = calledBreakAgreementsForAnnexedKingdom,
			["removedCount"] = removedCount,
			["targetWasPlayerVassal"] = targetWasPlayerVassal,
			["targetWasPlayerSuzerain"] = targetWasPlayerSuzerain,
			["error"] = Preview(error)
		};
	}

	public static Dictionary<string, object> DescribeKingdom(Kingdom kingdom)
	{
		if (kingdom == null)
		{
			return NullObject("kingdom");
		}
		return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
		{
			["kind"] = "kingdom",
			["id"] = Preview(SafeString(() => kingdom.StringId)),
			["name"] = Preview(SafeString(() => kingdom.Name?.ToString())),
			["leader"] = Preview(SafeString(() => kingdom.Leader?.StringId)),
			["rulingClan"] = Preview(SafeString(() => kingdom.RulingClan?.StringId)),
			["clanCount"] = SafeInt(() => kingdom.Clans?.Count ?? 0)
		};
	}

	public static Dictionary<string, object> DescribeClan(Clan clan)
	{
		if (clan == null)
		{
			return NullObject("clan");
		}
		return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
		{
			["kind"] = "clan",
			["id"] = Preview(SafeString(() => clan.StringId)),
			["name"] = Preview(SafeString(() => clan.Name?.ToString())),
			["kingdom"] = Preview(SafeString(() => clan.Kingdom?.StringId)),
			["leader"] = Preview(SafeString(() => clan.Leader?.StringId)),
			["fiefCount"] = CountClanSettlements(clan),
			["warPartyCount"] = CountClanWarParties(clan)
		};
	}

	public static Dictionary<string, object> DescribeHero(Hero hero)
	{
		if (hero == null)
		{
			return NullObject("hero");
		}
		return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
		{
			["kind"] = "hero",
			["id"] = Preview(SafeString(() => hero.StringId)),
			["name"] = Preview(SafeString(() => hero.Name?.ToString())),
			["clan"] = Preview(SafeString(() => hero.Clan?.StringId)),
			["kingdom"] = Preview(SafeString(() => hero.Clan?.Kingdom?.StringId ?? (hero.MapFaction as Kingdom)?.StringId))
		};
	}

	public static Dictionary<string, object> DescribeSettlement(Settlement settlement)
	{
		if (settlement == null)
		{
			return NullObject("settlement");
		}
		Clan ownerClan = SafeGet(() => settlement.OwnerClan);
		IFaction mapFaction = SafeGet(() => settlement.MapFaction);
		object mapEvent = SafeGet<object>(() => settlement.Party?.MapEvent);
		return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
		{
			["kind"] = "settlement",
			["id"] = Preview(SafeString(() => settlement.StringId)),
			["name"] = Preview(SafeString(() => settlement.Name?.ToString())),
			["ownerClan"] = Preview(SafeString(() => ownerClan?.StringId)),
			["ownerClanKingdom"] = Preview(SafeString(() => ownerClan?.Kingdom?.StringId)),
			["mapFaction"] = DescribeFactionCompact(mapFaction),
			["isUnderSiege"] = SafeBool(() => settlement.IsUnderSiege),
			["hasMapEvent"] = mapEvent != null
		};
	}

	public static Kingdom SafeGetClanKingdom(Clan clan)
	{
		return SafeGet(() => clan?.Kingdom);
	}

	public static bool IsRulingClan(Kingdom kingdom, Clan clan)
	{
		return SafeBool(() => kingdom != null && clan != null && kingdom.RulingClan == clan);
	}

	private static Dictionary<string, object> NullObject(string kind)
	{
		return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
		{
			["kind"] = kind ?? "",
			["isNull"] = true
		};
	}

	private static List<string> DescribeFactionsAtWarWith(IFaction faction)
	{
		List<string> result = new List<string>();
		try
		{
			if (faction?.FactionsAtWarWith == null)
			{
				return result;
			}
			foreach (IFaction other in faction.FactionsAtWarWith)
			{
				if (other != null)
				{
					result.Add(DescribeFactionCompact(other));
					if (result.Count >= MaxListItems)
					{
						result.Add("<truncated>");
						break;
					}
				}
			}
		}
		catch
		{
		}
		return result;
	}

	private static string DescribeFactionCompact(IFaction faction)
	{
		if (faction == null)
		{
			return "null";
		}
		try
		{
			string kind = faction is Kingdom ? "kingdom" : (faction is Clan ? "clan" : faction.GetType().Name);
			return Preview(kind + ":" + (faction.StringId ?? "") + "|" + (faction.Name?.ToString() ?? ""));
		}
		catch
		{
			return "faction:<error>";
		}
	}

	private static bool IsAtWar(IFaction a, IFaction b)
	{
		try
		{
			return a != null && b != null && a.IsAtWarWith(b);
		}
		catch
		{
			return false;
		}
	}

	private static bool HasWarStance(IFaction faction, IFaction target)
	{
		try
		{
			if (faction == null || target == null || faction == target)
			{
				return false;
			}
			StanceLink stance = faction.GetStanceWith(target);
			if (stance != null)
			{
				return stance.IsAtWar;
			}
			return faction.IsAtWarWith(target);
		}
		catch
		{
			return false;
		}
	}

	private static List<Clan> FindClansStillInKingdom(Kingdom targetKingdom)
	{
		try
		{
			if (targetKingdom == null || Clan.All == null)
			{
				return new List<Clan>();
			}
			return Clan.All.Where((Clan x) => x != null && x.Kingdom == targetKingdom).Distinct().ToList();
		}
		catch
		{
			return new List<Clan>();
		}
	}

	private static List<Settlement> FindSettlementsStillInKingdom(Kingdom targetKingdom)
	{
		try
		{
			if (targetKingdom == null || Settlement.All == null)
			{
				return new List<Settlement>();
			}
			return Settlement.All.Where((Settlement x) => x != null && x.MapFaction == targetKingdom).Distinct().ToList();
		}
		catch
		{
			return new List<Settlement>();
		}
	}

	private static List<string> FindWarStancesToTarget(Kingdom targetKingdom)
	{
		List<string> result = new List<string>();
		try
		{
			if (targetKingdom == null)
			{
				return result;
			}
			if (Kingdom.All != null)
			{
				foreach (Kingdom kingdom in Kingdom.All)
				{
					if (kingdom != null && kingdom != targetKingdom && HasWarStance(kingdom, targetKingdom))
					{
						result.Add(DescribeFactionCompact(kingdom));
						if (result.Count >= MaxListItems)
						{
							result.Add("<truncated>");
							return result;
						}
					}
				}
			}
			if (Clan.All != null)
			{
				foreach (Clan clan in Clan.All)
				{
					if (clan != null && HasWarStance(clan, targetKingdom))
					{
						result.Add(DescribeFactionCompact(clan));
						if (result.Count >= MaxListItems)
						{
							result.Add("<truncated>");
							return result;
						}
					}
				}
			}
		}
		catch
		{
		}
		return result;
	}

	private static int CountClanSettlements(Clan clan)
	{
		try
		{
			return clan == null || Settlement.All == null ? 0 : Settlement.All.Count((Settlement x) => x != null && x.OwnerClan == clan);
		}
		catch
		{
			return 0;
		}
	}

	private static int CountClanWarParties(Clan clan)
	{
		try
		{
			return clan?.WarPartyComponents?.Count ?? 0;
		}
		catch
		{
			return 0;
		}
	}

	private static Hero SafeGetClanLeader(Clan clan)
	{
		return SafeGet(() => clan?.Leader);
	}

	private static List<T> SafeList<T>(IEnumerable<T> values)
	{
		try
		{
			return values == null ? new List<T>() : values.Where((T x) => x != null).Distinct().ToList();
		}
		catch
		{
			return new List<T>();
		}
	}

	private static T SafeGet<T>(Func<T> getter)
	{
		try
		{
			return getter == null ? default(T) : getter();
		}
		catch
		{
			return default(T);
		}
	}

	private static string SafeString(Func<string> getter)
	{
		try
		{
			return getter?.Invoke() ?? "";
		}
		catch
		{
			return "";
		}
	}

	private static bool SafeBool(Func<bool> getter)
	{
		try
		{
			return getter != null && getter();
		}
		catch
		{
			return false;
		}
	}

	private static int SafeInt(Func<int> getter)
	{
		try
		{
			return getter == null ? 0 : getter();
		}
		catch
		{
			return 0;
		}
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
		return SafeGet(() => Hero.MainHero);
	}

	private static Kingdom SafeGetPlayerKingdom()
	{
		return SafeGet(() => Clan.PlayerClan?.Kingdom ?? Hero.MainHero?.Clan?.Kingdom);
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
				string key = Preview(entry.Key?.ToString() ?? "", 200);
				if (!string.IsNullOrWhiteSpace(key))
				{
					normalized[key] = NormalizeValue(entry.Value);
				}
				if (normalized.Count >= MaxListItems)
				{
					normalized["<truncated>"] = true;
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

	public static string Preview(string text, int maxLength = MaxStringLength)
	{
		string value = (text ?? "").Replace("\r", "\\r").Replace("\n", "\\n");
		if (maxLength <= 0)
		{
			maxLength = MaxStringLength;
		}
		return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...<truncated>";
	}
}
