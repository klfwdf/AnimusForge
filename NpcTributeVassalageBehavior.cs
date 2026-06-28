using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using HarmonyLib;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace AnimusForge;

internal readonly struct AfTributePowerContext
{
	public AfTributePowerContext(
		float scorePayer,
		float scoreReceiver,
		float receiverDecisionThreshold,
		float settlementValue,
		float payerWarProgress,
		float receiverWarProgress,
		float warProgressDifference,
		float rawTributeRatio,
		float appliedTributeRatio,
		float payerFiefProsperity,
		int calculatedTribute)
	{
		ScorePayer = scorePayer;
		ScoreReceiver = scoreReceiver;
		ReceiverDecisionThreshold = receiverDecisionThreshold;
		SettlementValue = settlementValue;
		PayerWarProgress = payerWarProgress;
		ReceiverWarProgress = receiverWarProgress;
		WarProgressDifference = warProgressDifference;
		RawTributeRatio = rawTributeRatio;
		AppliedTributeRatio = appliedTributeRatio;
		PayerFiefProsperity = payerFiefProsperity;
		CalculatedTribute = calculatedTribute;
	}

	public float ScorePayer { get; }
	public float ScoreReceiver { get; }
	public float ReceiverDecisionThreshold { get; }
	public float SettlementValue { get; }
	public float PayerWarProgress { get; }
	public float ReceiverWarProgress { get; }
	public float WarProgressDifference { get; }
	public float RawTributeRatio { get; }
	public float AppliedTributeRatio { get; }
	public float PayerFiefProsperity { get; }
	public int CalculatedTribute { get; }

	public float ScoreDelta => ScoreReceiver - ScorePayer;
}

internal sealed class NpcTributeVassalageBehavior : CampaignBehaviorBase
{
	private const string LogCategory = "NpcTributeVassalage";
	private const float MinimumStrengthRatio = 1.35f;
	private const float MinimumPeaceScoreGap = 50f;
	private const int MinimumDailyTribute = 10;
	private const float BaseChance = 0.12f;
	private const float MaximumChance = 0.65f;

	public static NpcTributeVassalageBehavior Instance { get; private set; }

	public override void RegisterEvents()
	{
		Instance = this;
		NpcTributeVassalageDiagnosticLog.Event("behavior.register_events", new Dictionary<string, object>
		{
			["logPath"] = NpcTributeVassalageDiagnosticLog.GetDiagnosticLogPath()
		});
		Logger.Log(LogCategory, "[Lifecycle] Registered.");
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	internal static void RegisterHarmonyPatches(Harmony harmony)
	{
		if (harmony == null)
		{
			return;
		}
		harmony.CreateClassProcessor(typeof(Patch_NpcTributeVassalage_MakePeaceAction)).Patch();
		NpcTributeVassalageDiagnosticLog.Event("harmony.patch_applied", new Dictionary<string, object>
		{
			["target"] = "MakePeaceAction.ApplyInternal"
		});
	}

	internal NpcTributeVassalagePeaceSnapshot CapturePeaceSnapshot(
		IFaction faction1,
		IFaction faction2,
		int dailyTributeFrom1To2,
		int dailyTributeDuration,
		MakePeaceAction.MakePeaceDetail detail)
	{
		try
		{
			Kingdom activeKingdom = ResolveFactionKingdom(faction1);
			Kingdom opponentKingdom = ResolveFactionKingdom(faction2);
			return new NpcTributeVassalagePeaceSnapshot
			{
				ActiveKingdom = activeKingdom,
				OpponentKingdom = opponentKingdom,
				DailyTributeFromActiveToOpponent = dailyTributeFrom1To2,
				DailyTributeDuration = dailyTributeDuration,
				Detail = detail,
				WasAtWar = IsAtWar(activeKingdom, opponentKingdom)
			};
		}
		catch (Exception ex)
		{
			LogError("capture_snapshot_error", ex);
			return null;
		}
	}

	internal void HandlePeaceApplied(NpcTributeVassalagePeaceSnapshot snapshot)
	{
		if (snapshot == null)
		{
			return;
		}
		try
		{
			Kingdom activeKingdom = snapshot.ActiveKingdom;
			Kingdom opponentKingdom = snapshot.OpponentKingdom;
			if (!snapshot.WasAtWar)
			{
				LogSkip("not_at_war_before_peace_action", snapshot);
				return;
			}
			if (snapshot.Detail != MakePeaceAction.MakePeaceDetail.ByKingdomDecision)
			{
				LogSkip("not_kingdom_decision", snapshot);
				return;
			}
			if (!IsValidNpcKingdom(activeKingdom) || !IsValidNpcKingdom(opponentKingdom) || activeKingdom == opponentKingdom)
			{
				LogSkip("invalid_or_player_involved_kingdom", snapshot);
				return;
			}
			if (snapshot.DailyTributeFromActiveToOpponent < MinimumDailyTribute || snapshot.DailyTributeDuration <= 0)
			{
				LogSkip("active_side_not_paying_positive_tribute", snapshot);
				return;
			}
			if (IsAtWar(activeKingdom, opponentKingdom))
			{
				LogSkip("peace_not_applied", snapshot);
				return;
			}
			if (!DiplomacyBehavior.TryBuildTributePowerContext(activeKingdom, opponentKingdom, out AfTributePowerContext tributeContext))
			{
				LogSkip("tribute_power_context_unavailable", snapshot);
				return;
			}
			float activeStrength = GetRefreshedKingdomStrength(activeKingdom);
			float opponentStrength = GetRefreshedKingdomStrength(opponentKingdom);
			float strengthRatio = CalculateStrengthRatio(opponentStrength, activeStrength);
			if (!IsWeakActivePeaceSide(snapshot, tributeContext, activeStrength, opponentStrength, strengthRatio))
			{
				LogCandidate("skip", "not_significant_weak_active_side_gap", snapshot, tributeContext, activeStrength, opponentStrength, strengthRatio, 0f, 0f, "");
				return;
			}
			float chance = CalculateVassalageChance(snapshot, tributeContext, strengthRatio);
			float roll = MBRandom.RandomFloat;
			LogCandidate("candidate", "eligible", snapshot, tributeContext, activeStrength, opponentStrength, strengthRatio, chance, roll, "");
			if (roll > chance)
			{
				LogCandidate("skip", "roll_failed", snapshot, tributeContext, activeStrength, opponentStrength, strengthRatio, chance, roll, "");
				return;
			}
			VassalageBehavior vassalageBehavior = VassalageBehavior.Instance;
			if (vassalageBehavior == null)
			{
				LogCandidate("skip", "vassalage_behavior_missing", snapshot, tributeContext, activeStrength, opponentStrength, strengthRatio, chance, roll, "");
				return;
			}
			if (vassalageBehavior.TryCreateNpcTributaryVassalage(
				opponentKingdom,
				activeKingdom,
				"npc_tribute_vassalage",
				out string statusText,
				out string agreementId))
			{
				Logger.Log(LogCategory, "Applied NPC tributary vassalage agreement=" + agreementId + " suzerain=" + (opponentKingdom.StringId ?? "") + " vassal=" + (activeKingdom.StringId ?? ""));
				LogCandidate("applied", "agreement_created", snapshot, tributeContext, activeStrength, opponentStrength, strengthRatio, chance, roll, agreementId, statusText);
				return;
			}
			LogCandidate("skip", "agreement_create_rejected", snapshot, tributeContext, activeStrength, opponentStrength, strengthRatio, chance, roll, agreementId, statusText);
		}
		catch (Exception ex)
		{
			LogError("handle_peace_applied_error", ex, snapshot);
		}
	}

	private static bool IsWeakActivePeaceSide(
		NpcTributeVassalagePeaceSnapshot snapshot,
		AfTributePowerContext tributeContext,
		float activeStrength,
		float opponentStrength,
		float strengthRatio)
	{
		bool positiveTribute = snapshot?.DailyTributeFromActiveToOpponent >= MinimumDailyTribute;
		bool weakerByPeaceScore = tributeContext.ScoreDelta >= MinimumPeaceScoreGap;
		bool weakerByStrength = strengthRatio >= MinimumStrengthRatio;
		bool fallbackStrengthGap = activeStrength <= 0f && opponentStrength > 0f;
		bool meaningfulTribute = Math.Max(snapshot?.DailyTributeFromActiveToOpponent ?? 0, tributeContext.CalculatedTribute) >= MinimumDailyTribute
			|| tributeContext.AppliedTributeRatio >= 0.05f;
		return positiveTribute && meaningfulTribute && (weakerByPeaceScore || weakerByStrength || fallbackStrengthGap);
	}

	private static float CalculateVassalageChance(
		NpcTributeVassalagePeaceSnapshot snapshot,
		AfTributePowerContext tributeContext,
		float strengthRatio)
	{
		float scoreBonus = Clamp01((tributeContext.ScoreDelta - MinimumPeaceScoreGap) / 250f) * 0.18f;
		float strengthBonus = Clamp01((strengthRatio - MinimumStrengthRatio) / 1.5f) * 0.2f;
		float tributeRatioBonus = Clamp01(tributeContext.AppliedTributeRatio / 0.15f) * 0.18f;
		float tributeAmountBonus = Clamp01((snapshot?.DailyTributeFromActiveToOpponent ?? 0) / 800f) * 0.07f;
		return Clamp(BaseChance + scoreBonus + strengthBonus + tributeRatioBonus + tributeAmountBonus, BaseChance, MaximumChance);
	}

	private static Kingdom ResolveFactionKingdom(IFaction faction)
	{
		if (faction == null)
		{
			return null;
		}
		if (faction is Kingdom kingdom)
		{
			return kingdom;
		}
		try
		{
			return faction.Leader?.Clan?.Kingdom ?? faction.MapFaction as Kingdom;
		}
		catch
		{
			return null;
		}
	}

	private static bool IsValidNpcKingdom(Kingdom kingdom)
	{
		if (!IsValidKingdom(kingdom))
		{
			return false;
		}
		Kingdom playerKingdom = Clan.PlayerClan?.Kingdom ?? Hero.MainHero?.Clan?.Kingdom;
		return playerKingdom == null || kingdom != playerKingdom;
	}

	private static bool IsValidKingdom(Kingdom kingdom)
	{
		try
		{
			return kingdom != null && !kingdom.IsEliminated && !string.IsNullOrWhiteSpace(kingdom.StringId);
		}
		catch
		{
			return kingdom != null && !string.IsNullOrWhiteSpace(kingdom.StringId);
		}
	}

	private static bool IsAtWar(Kingdom left, Kingdom right)
	{
		try
		{
			return left != null && right != null && left != right && FactionManager.IsAtWarAgainstFaction(left, right);
		}
		catch
		{
			return false;
		}
	}

	private static float GetRefreshedKingdomStrength(Kingdom kingdom)
	{
		if (!IsValidKingdom(kingdom))
		{
			return 0f;
		}
		try
		{
			if (kingdom.Clans != null)
			{
				foreach (Clan clan in kingdom.Clans)
				{
					clan?.UpdateCurrentStrength();
				}
			}
		}
		catch
		{
		}
		float strength = 0f;
		try
		{
			strength = kingdom.CurrentTotalStrength;
		}
		catch
		{
			return 0f;
		}
		if (float.IsNaN(strength) || float.IsInfinity(strength))
		{
			return 0f;
		}
		return Math.Max(0f, strength);
	}

	private static float CalculateStrengthRatio(float stronger, float weaker)
	{
		if (stronger <= 0f && weaker <= 0f)
		{
			return 1f;
		}
		if (weaker <= 0f)
		{
			return stronger > 0f ? 99f : 1f;
		}
		return Math.Max(0f, stronger) / Math.Max(1f, weaker);
	}

	private static float Clamp01(float value)
	{
		return Clamp(value, 0f, 1f);
	}

	private static float Clamp(float value, float min, float max)
	{
		if (float.IsNaN(value) || float.IsInfinity(value))
		{
			return min;
		}
		if (value < min)
		{
			return min;
		}
		if (value > max)
		{
			return max;
		}
		return value;
	}

	private static void LogSkip(string reason, NpcTributeVassalagePeaceSnapshot snapshot)
	{
		LogCandidate("skip", reason, snapshot, default, 0f, 0f, 0f, 0f, 0f, "");
	}

	private static void LogCandidate(
		string stage,
		string reason,
		NpcTributeVassalagePeaceSnapshot snapshot,
		AfTributePowerContext tributeContext,
		float activeStrength,
		float opponentStrength,
		float strengthRatio,
		float chance,
		float roll,
		string agreementId,
		string statusText = "")
	{
		NpcTributeVassalageDiagnosticLog.Event(stage, new Dictionary<string, object>
		{
			["reason"] = reason ?? "",
			["classificationBasis"] = "MakePeaceAction.ApplyInternal faction1 proposer plus positive dailyTributeFrom1To2",
			["activePeaceKingdom"] = NpcTributeVassalageDiagnosticLog.DescribeKingdom(snapshot?.ActiveKingdom),
			["opponentKingdom"] = NpcTributeVassalageDiagnosticLog.DescribeKingdom(snapshot?.OpponentKingdom),
			["proposedVassal"] = NpcTributeVassalageDiagnosticLog.DescribeKingdom(snapshot?.ActiveKingdom),
			["proposedSuzerain"] = NpcTributeVassalageDiagnosticLog.DescribeKingdom(snapshot?.OpponentKingdom),
			["dailyTributeFromActiveToOpponent"] = snapshot?.DailyTributeFromActiveToOpponent ?? 0,
			["dailyTributeDuration"] = snapshot?.DailyTributeDuration ?? 0,
			["detail"] = snapshot?.Detail.ToString() ?? "",
			["wasAtWar"] = snapshot?.WasAtWar ?? false,
			["scorePayer"] = tributeContext.ScorePayer,
			["scoreReceiver"] = tributeContext.ScoreReceiver,
			["scoreDelta"] = tributeContext.ScoreDelta,
			["receiverDecisionThreshold"] = tributeContext.ReceiverDecisionThreshold,
			["payerWarProgress"] = tributeContext.PayerWarProgress,
			["receiverWarProgress"] = tributeContext.ReceiverWarProgress,
			["warProgressDifference"] = tributeContext.WarProgressDifference,
			["rawTributeRatio"] = tributeContext.RawTributeRatio,
			["appliedTributeRatio"] = tributeContext.AppliedTributeRatio,
			["calculatedTribute"] = tributeContext.CalculatedTribute,
			["activeStrength"] = activeStrength,
			["opponentStrength"] = opponentStrength,
			["strengthRatio"] = strengthRatio,
			["chance"] = chance,
			["roll"] = roll,
			["agreementId"] = agreementId ?? "",
			["statusText"] = statusText ?? ""
		});
	}

	private static void LogError(string reason, Exception ex, NpcTributeVassalagePeaceSnapshot snapshot = null)
	{
		Logger.Log(LogCategory, "[ERROR] " + reason + ": " + ex);
		NpcTributeVassalageDiagnosticLog.Event("error", new Dictionary<string, object>
		{
			["reason"] = reason ?? "",
			["activePeaceKingdom"] = NpcTributeVassalageDiagnosticLog.DescribeKingdom(snapshot?.ActiveKingdom),
			["opponentKingdom"] = NpcTributeVassalageDiagnosticLog.DescribeKingdom(snapshot?.OpponentKingdom),
			["exception"] = ex?.ToString() ?? ""
		});
	}
}

internal sealed class NpcTributeVassalagePeaceSnapshot
{
	public Kingdom ActiveKingdom { get; set; }
	public Kingdom OpponentKingdom { get; set; }
	public int DailyTributeFromActiveToOpponent { get; set; }
	public int DailyTributeDuration { get; set; }
	public MakePeaceAction.MakePeaceDetail Detail { get; set; }
	public bool WasAtWar { get; set; }
}

[HarmonyPatch(typeof(MakePeaceAction), "ApplyInternal")]
internal static class Patch_NpcTributeVassalage_MakePeaceAction
{
	[ThreadStatic]
	private static NpcTributeVassalagePeaceSnapshot _snapshot;

	public static void Prefix(
		IFaction faction1,
		IFaction faction2,
		int dailyTributeFrom1To2,
		int dailyTributeDuration,
		MakePeaceAction.MakePeaceDetail detail)
	{
		if (VassalageBehavior.IsApplyingVassalageDiplomacy)
		{
			_snapshot = null;
			return;
		}
		_snapshot = NpcTributeVassalageBehavior.Instance?.CapturePeaceSnapshot(
			faction1,
			faction2,
			dailyTributeFrom1To2,
			dailyTributeDuration,
			detail);
	}

	public static void Postfix()
	{
		NpcTributeVassalagePeaceSnapshot snapshot = _snapshot;
		_snapshot = null;
		if (snapshot == null || VassalageBehavior.IsApplyingVassalageDiplomacy)
		{
			return;
		}
		NpcTributeVassalageBehavior.Instance?.HandlePeaceApplied(snapshot);
	}
}

internal static class NpcTributeVassalageDiagnosticLog
{
	private const int MaxStringLength = 300;
	private const int MaxFieldsPerEvent = 48;
	private static readonly object FileLock = new object();
	private static readonly string SessionId = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
	private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
	private static long _sequence;

	public static void Event(string stage, IDictionary<string, object> fields = null)
	{
		try
		{
			string path = GetDiagnosticLogPath();
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
				["playerKingdom"] = DescribeKingdom(SafeGetPlayerKingdom())
			};
			if (fields != null)
			{
				foreach (KeyValuePair<string, object> field in fields)
				{
					string key = (field.Key ?? "").Trim();
					if (string.IsNullOrWhiteSpace(key))
					{
						continue;
					}
					entry[key] = NormalizeValue(field.Value);
					if (entry.Count >= MaxFieldsPerEvent)
					{
						entry["fieldsTruncated"] = true;
						break;
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
			return AnimusForgeModulePaths.GetLogFilePath("AF_NpcTributeVassalage_Diagnostics.jsonl");
		}
		catch
		{
			return "AF_NpcTributeVassalage_Diagnostics.jsonl";
		}
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

	private static int SafeGetCampaignDay()
	{
		try
		{
			return Math.Max(0, (int)Math.Floor(CampaignTime.Now.ToDays));
		}
		catch
		{
			return 0;
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
			return null;
		}
		if (value is string text)
		{
			return Preview(text, MaxStringLength);
		}
		if (value is Enum)
		{
			return value.ToString();
		}
		if (value is float f)
		{
			return Math.Round(f, 4);
		}
		if (value is double d)
		{
			return Math.Round(d, 4);
		}
		if (value is Kingdom kingdom)
		{
			return DescribeKingdom(kingdom);
		}
		if (value is Settlement settlement)
		{
			return "settlement=" + (settlement.StringId ?? "") + ";name=" + Preview(settlement.Name?.ToString() ?? "", 80);
		}
		if (value is System.Collections.IEnumerable enumerable && !(value is string))
		{
			List<object> items = new List<object>();
			int count = 0;
			foreach (object item in enumerable)
			{
				if (count >= 12)
				{
					items.Add("...");
					break;
				}
				items.Add(NormalizeValue(item));
				count++;
			}
			return items;
		}
		return value;
	}

	private static string Preview(string text, int maxLength)
	{
		if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
		{
			return text ?? "";
		}
		return text.Substring(0, maxLength) + "...";
	}
}
