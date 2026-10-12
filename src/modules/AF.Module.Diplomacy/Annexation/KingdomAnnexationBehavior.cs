using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;

namespace AnimusForge;

public sealed class KingdomAnnexationBehavior : CampaignBehaviorBase
{
	private readonly HashSet<string> _annexationsInProgress = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	public static KingdomAnnexationBehavior Instance { get; private set; }

	// Lets the bulletin skip per-clan defection facts while one merger is being applied.
	internal static bool IsAnnexationInProgress => Instance != null && Instance._annexationsInProgress.Count > 0;

	public KingdomAnnexationBehavior()
	{
		Instance = this;
	}

	public override void RegisterEvents()
	{
		Instance = this;
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	public bool TryApplyKingdomAnnexation(Hero conversationHero, string targetKingdomId, out string statusText)
	{
		statusText = "";
		string kingdomToken = (targetKingdomId ?? "").Trim();
		Kingdom playerKingdom = GetPlayerKingdom();
		Kingdom targetKingdom = ResolveKingdomById(kingdomToken) ?? ResolveHeroKingdom(conversationHero);
		Dictionary<string, object> tagParseSnapshot = KingdomAnnexationDiagnosticLog.BuildTagParseSnapshot(kingdomToken, targetKingdom, playerKingdom, conversationHero);
		LogEvent("tag.parse", tagParseSnapshot);
		KingdomAnnexationDiagnosticLog.Event("tag.parse", tagParseSnapshot);
		return TryAnnexKingdom(playerKingdom, targetKingdom, conversationHero, out statusText);
	}

	public bool TryAnnexKingdom(Kingdom playerKingdom, Kingdom targetKingdom, Hero conversationHero, out string statusText)
	{
		statusText = WorldDiplomacyBehavior.SubmitOralDiplomaticCommitment(conversationHero,
			"action=Annexation;move=NewMatter;target=" + playerKingdom?.StringId + ";receiving=" + playerKingdom?.StringId + ";joining=" + targetKingdom?.StringId, out bool registered);
		return registered;
	}

	internal bool TryExecuteFormalAnnexation(Kingdom receiving, Kingdom joining, out string statusText)
	{
		return ExecuteFormalAnnexationCore(receiving, joining, joining?.RulingClan?.Leader, out statusText);
	}

	private bool ExecuteFormalAnnexationCore(Kingdom playerKingdom, Kingdom targetKingdom, Hero conversationHero, out string statusText)
	{
		statusText = "";
		Stopwatch stopwatch = Stopwatch.StartNew();
		if (!ValidateAnnexationRequest(playerKingdom, targetKingdom, conversationHero, out statusText))
		{
			LogApplyReject(statusText, playerKingdom, targetKingdom, conversationHero);
			return false;
		}

		string targetId = (targetKingdom.StringId ?? "").Trim();
		if (!_annexationsInProgress.Add(targetId))
		{
			statusText = DiplomacyUiLocalization.Format(
				"AF_DIP_UI_Annexation_Processing",
				"Annexation paused: {TARGET} integration is already being processed.",
				("TARGET", GetKingdomDisplayName(targetKingdom, "the target kingdom")));
			LogApplyReject("duplicate_in_progress", playerKingdom, targetKingdom, conversationHero);
			return false;
		}

		try
		{
			List<Clan> targetClans = SnapshotTargetClans(targetKingdom);
			List<Settlement> targetSettlements = SnapshotKingdomSettlements(targetKingdom);
			Hero formerRuler = targetKingdom.Leader;
			Dictionary<string, object> applySnapshot = KingdomAnnexationDiagnosticLog.BuildApplySnapshot(playerKingdom, targetKingdom, conversationHero, targetClans, targetSettlements);
			KingdomAnnexationDiagnosticLog.Event("apply.start.before", applySnapshot);
			LogEvent("apply.start", applySnapshot);
			KingdomAnnexationDiagnosticLog.Event("apply.start.after", applySnapshot);

			if (targetClans.Count == 0)
			{
				statusText = DiplomacyUiLocalization.Format(
					"AF_DIP_UI_Annexation_NoClans",
					"Annexation aborted: {TARGET} has no valid clans left to swear allegiance.",
					("TARGET", GetKingdomDisplayName(targetKingdom, "the target kingdom")));
				LogApplyReject("no_target_clans", playerKingdom, targetKingdom, conversationHero);
				return false;
			}

			int movedCount = 0;
			int failedCount = 0;
			foreach (Clan clan in OrderClansForAnnexation(targetClans, targetKingdom))
			{
				if (clan == null || clan.IsEliminated)
				{
					continue;
				}
				bool ok = false;
				Kingdom beforeKingdom = KingdomAnnexationDiagnosticLog.SafeGetClanKingdom(clan);
				bool wasRulingClan = KingdomAnnexationDiagnosticLog.IsRulingClan(targetKingdom, clan);
				string result = TransferClanToPlayerKingdom(playerKingdom, targetKingdom, clan, out ok);
				if (ok)
				{
					movedCount++;
				}
				else
				{
					failedCount++;
				}
				Kingdom afterKingdom = KingdomAnnexationDiagnosticLog.SafeGetClanKingdom(clan);
				bool anomaly = !ok || (ok && afterKingdom != playerKingdom);
				Dictionary<string, object> clanTransferSnapshot = KingdomAnnexationDiagnosticLog.BuildClanTransferSnapshot(playerKingdom, targetKingdom, clan, beforeKingdom, afterKingdom, wasRulingClan, ok, anomaly, result);
				LogEvent("clan.transfer", clanTransferSnapshot);
				KingdomAnnexationDiagnosticLog.Event("clan.transfer", clanTransferSnapshot);
			}

			int brokenVassalageAgreements = BreakVassalageAgreementsForAnnexedKingdom(targetKingdom);
			bool peaceApplied = TryMakePeace(playerKingdom, targetKingdom);
			List<Settlement> residualSettlements = targetSettlements.Where((Settlement x) => x != null && x.MapFaction == targetKingdom).ToList();
			List<Clan> remainingClans = SnapshotRemainingClans(targetKingdom);
			Dictionary<string, object> postTransferSnapshot = KingdomAnnexationDiagnosticLog.BuildPostTransferSnapshot(playerKingdom, targetKingdom, movedCount, failedCount, remainingClans, residualSettlements, peaceApplied, brokenVassalageAgreements);
			LogEvent("post_transfer.check", postTransferSnapshot);
			KingdomAnnexationDiagnosticLog.Event("post_transfer.check", postTransferSnapshot);

			if (residualSettlements.Count > 0)
			{
				statusText = DiplomacyUiLocalization.Format(
					"AF_DIP_UI_Annexation_SettlementsPending",
					"Annexation aborted: {TARGET} still has settlements awaiting transfer. The former kingdom was not dissolved to prevent ownership errors.",
					("TARGET", GetKingdomDisplayName(targetKingdom, "the target kingdom")));
				return false;
			}
			if (remainingClans.Count > 0)
			{
				statusText = DiplomacyUiLocalization.Format(
					"AF_DIP_UI_Annexation_ClansPending",
					"Annexation aborted: {TARGET} still has clans that have not completed their allegiance. The former kingdom was not dissolved to prevent clan ownership errors.",
					("TARGET", GetKingdomDisplayName(targetKingdom, "the target kingdom")));
				return false;
			}

			bool destroyOk = false;
			string destroyResult = "";
			try
			{
				DestroyKingdomAction.Apply(targetKingdom);
				destroyOk = targetKingdom.IsEliminated;
				destroyResult = destroyOk ? "destroyed_by_vanilla_action" : "destroy_action_returned_but_not_eliminated";
			}
			catch (Exception ex)
			{
				destroyResult = "exception: " + ex.GetType().Name + " - " + ex.Message;
			}

			stopwatch.Stop();
			Dictionary<string, object> destroyFinishSnapshot = KingdomAnnexationDiagnosticLog.BuildDestroyFinishSnapshot(playerKingdom, targetKingdom, targetClans, targetSettlements, destroyOk, destroyResult, movedCount, failedCount, stopwatch.Elapsed.TotalMilliseconds);
			LogEvent("destroy.finish", destroyFinishSnapshot);
			KingdomAnnexationDiagnosticLog.Event("destroy.finish.after", destroyFinishSnapshot);
			if (!destroyOk)
			{
				statusText = DiplomacyUiLocalization.Format(
					"AF_DIP_UI_Annexation_DestroyFailed",
					"Annexation aborted: all clans of {TARGET} have sworn allegiance to {PLAYER}, but the former kingdom could not be dissolved.",
					("TARGET", GetKingdomDisplayName(targetKingdom, "the target kingdom")),
					("PLAYER", GetKingdomDisplayName(playerKingdom, "the player kingdom")));
				return false;
			}
			MyBehavior.Instance?.CaptureWorldBulletinAnnexation(playerKingdom, targetKingdom, formerRuler, movedCount);
			statusText = DiplomacyUiLocalization.Format(
				"AF_DIP_UI_Annexation_Completed",
				"Annexation complete: {TARGET} has joined {PLAYER}. All clans of {TARGET} have sworn allegiance to {PLAYER}; {TARGET} has ceased to exist.",
				("TARGET", GetKingdomDisplayName(targetKingdom, "the target kingdom")),
				("PLAYER", GetKingdomDisplayName(playerKingdom, "the player kingdom")));
			return true;
		}
		finally
		{
			_annexationsInProgress.Remove(targetId);
		}
	}

	public static bool CanInjectAnnexationRuleForExternal(Hero targetHero, CharacterObject targetCharacter = null)
	{
		return TryBuildAnnexationRuntimeState(targetHero ?? targetCharacter?.HeroObject, out var _, out var _, out var _, out var _);
	}

	public static string BuildRuntimeAnnexationInstructionForExternal(Hero targetHero, CharacterObject targetCharacter = null)
	{
		if (!TryBuildAnnexationRuntimeState(targetHero ?? targetCharacter?.HeroObject, out var playerKingdom, out var targetKingdom, out var speaker, out var reason))
		{
			LogEvent("runtime.instruction.skip", new Dictionary<string, object>
			{
				["reason"] = reason,
				["targetHero"] = DescribeHero(targetHero),
				["targetCharacterId"] = targetCharacter?.StringId ?? ""
			});
			return "";
		}
		string targetKingdomId = targetKingdom.StringId ?? "";
		LogEvent("runtime.instruction", new Dictionary<string, object>
		{
			["playerKingdom"] = DescribeKingdom(playerKingdom),
			["targetKingdom"] = DescribeKingdom(targetKingdom),
			["speaker"] = DescribeHero(speaker)
		});
		string template = DiplomacyMiscPromptTexts.Get("Annexation_NegotiationFact", "[AF Kingdom Annexation Negotiation Facts]\nThe player is currently the ruler of {PLAYER_KINGDOM}.\n{SPEAKER} is currently the ruler of {TARGET_KINGDOM}.\nThe target kingdom ID must be written as: {TARGET_ID}.\nAnnexation terms with player as recipient: the target kingdom abdicates independent sovereignty, all clans join the player's kingdom, and former clans retain their fiefs as subjects of the player's kingdom.\nThe reply must negotiate naturally without action tags. Seeking peace, alliances, truces, reparations, tributes, vassalage, protectorates, or ordinary surrender are not kingdom annexations.\nAn agreement to publish forms ONLY when the other ruler explicitly and definitively agrees to merge the entire kingdom into the player's kingdom, abdicate sovereignty, and transfer all clans to the player's kingdom; propose formally first and await official acceptance, do not annex immediately. Other directions must specify receiving and joining kingdoms via the same formal declaration pipeline. Ongoing bargaining, demanding guarantees, hesitating, refusing, or merely showing respect do not constitute consent.");
		return PromptComposerTexts.FormatTemplate(template,
			("PLAYER_KINGDOM", GetKingdomDisplayName(playerKingdom, DiplomacyMiscPromptTexts.Get("Prompt_PlayerKingdomFallback", "player's kingdom"))),
			("SPEAKER", GetHeroDisplayName(speaker, DiplomacyMiscPromptTexts.Get("Prompt_SpeakerFallback", "speaker"))),
			("TARGET_KINGDOM", GetKingdomDisplayName(targetKingdom, DiplomacyMiscPromptTexts.Get("Prompt_TargetKingdomFallback", "target kingdom"))),
			("TARGET_ID", targetKingdomId ?? string.Empty));
	}

	public static string BuildRuntimeAnnexationConstraintHintForExternal(Hero targetHero, CharacterObject targetCharacter = null)
	{
		if (!TryBuildAnnexationRuntimeState(targetHero ?? targetCharacter?.HeroObject, out var _, out var targetKingdom, out var _, out var _))
		{
			return "";
		}
		string template = DiplomacyMiscPromptTexts.Get("Annexation_ConstraintHint", "The legacy KINGDOM_ANNEX tag applies to the player-recipient direction; an explicit commitment only submits a formal proposal. Other directions use DIPLOMACY:COMMIT specifying receiving/joining. The legacy tag target_kingdom_id must be {TARGET_ID}. Peace, alliance, truce, reparations, tribute, vassalage, or protectorates must not be treated as annexation.");
		return template.Replace("{TARGET_ID}", targetKingdom.StringId ?? string.Empty);
	}

	public static List<PostprocessRuleEntry> BuildRuntimeAnnexationPostprocessRulesForExternal(Hero targetHero, CharacterObject targetCharacter = null)
	{
		List<PostprocessRuleEntry> result = new List<PostprocessRuleEntry>();
		if (!TryBuildAnnexationRuntimeState(targetHero ?? targetCharacter?.HeroObject, out var playerKingdom, out var targetKingdom, out var speaker, out var reason))
		{
			LogEvent("postprocess.rules.skip", new Dictionary<string, object>
			{
				["reason"] = reason,
				["targetHero"] = DescribeHero(targetHero),
				["targetCharacterId"] = targetCharacter?.StringId ?? ""
			});
			return result;
		}
		string targetKingdomId = (targetKingdom.StringId ?? "").Trim();
		foreach (PostprocessRuleEntry rule in AIConfigHandler.GetGuardrailRulePostprocessRules("diplomacy") ?? new List<PostprocessRuleEntry>())
		{
			string tag = (rule?.Tag ?? "").Trim();
			if (string.IsNullOrWhiteSpace(tag) || !tag.StartsWith("[ACTION:KINGDOM_ANNEX:", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			tag = tag.Replace("{targetKingdomId}", targetKingdomId);
			result.Add(new PostprocessRuleEntry
			{
				Tag = tag,
				Description = (rule.Description ?? "").Replace("{targetKingdomId}", targetKingdomId)
			});
		}
		LogEvent("postprocess.rules", new Dictionary<string, object>
		{
			["playerKingdom"] = DescribeKingdom(playerKingdom),
			["targetKingdom"] = DescribeKingdom(targetKingdom),
			["speaker"] = DescribeHero(speaker),
			["rules"] = result.Select((PostprocessRuleEntry x) => x?.Tag ?? "").ToList()
		});
		return result;
	}

	private static bool ValidateAnnexationRequest(Kingdom playerKingdom, Kingdom targetKingdom, Hero conversationHero, out string statusText)
	{
		if (!IsValidKingdom(playerKingdom) || playerKingdom.RulingClan?.Leader?.IsAlive != true)
		{
			statusText = DiplomacyUiLocalization.Text("AF_DIP_UI_Annexation_NotPlayerRuler", "Annexation not established: you must be the ruler of your own kingdom.");
			return false;
		}
		if (!IsValidKingdom(targetKingdom))
		{
			statusText = DiplomacyUiLocalization.Text("AF_DIP_UI_Annexation_TargetInvalid", "Annexation not established: the target kingdom does not exist or has already been eliminated.");
			return false;
		}
		if (targetKingdom == playerKingdom)
		{
			statusText = DiplomacyUiLocalization.Text("AF_DIP_UI_Annexation_SameKingdom", "Annexation not established: you cannot annex your own kingdom.");
			return false;
		}
		if (!IsKingdomRuler(conversationHero, targetKingdom))
		{
			statusText = DiplomacyUiLocalization.Text("AF_DIP_UI_Annexation_TargetRulerRequired", "Annexation not established: the other party cannot renounce the target kingdom's sovereignty without being its ruler.");
			return false;
		}
		statusText = "";
		return true;
	}

	private static bool TryBuildAnnexationRuntimeState(Hero targetHero, out Kingdom playerKingdom, out Kingdom targetKingdom, out Hero speaker, out string reason)
	{
		playerKingdom = GetPlayerKingdom();
		targetKingdom = null;
		speaker = targetHero;
		reason = "";
		if (!IsValidKingdom(playerKingdom) || !IsPlayerRuler(playerKingdom))
		{
			reason = "player_not_ruler";
			return false;
		}
		if (speaker == null)
		{
			reason = "speaker_null";
			return false;
		}
		targetKingdom = ResolveHeroKingdom(speaker);
		if (!IsValidKingdom(targetKingdom))
		{
			reason = "invalid_target_kingdom";
			return false;
		}
		if (targetKingdom == playerKingdom)
		{
			reason = "same_kingdom";
			return false;
		}
		if (!IsKingdomRuler(speaker, targetKingdom))
		{
			reason = "speaker_not_target_ruler";
			return false;
		}
		return true;
	}

	private static List<Clan> SnapshotTargetClans(Kingdom targetKingdom)
	{
		try
		{
			return (targetKingdom?.Clans ?? Enumerable.Empty<Clan>()).Where((Clan x) => x != null && !x.IsEliminated).Distinct().ToList();
		}
		catch
		{
			return new List<Clan>();
		}
	}

	private static List<Clan> SnapshotRemainingClans(Kingdom targetKingdom)
	{
		try
		{
			return (targetKingdom?.Clans ?? Enumerable.Empty<Clan>()).Where((Clan x) => x != null).Distinct().ToList();
		}
		catch
		{
			return new List<Clan>();
		}
	}

	private static List<Clan> OrderClansForAnnexation(List<Clan> clans, Kingdom targetKingdom)
	{
		Clan rulingClan = null;
		try
		{
			rulingClan = targetKingdom?.RulingClan;
		}
		catch
		{
			rulingClan = null;
		}
		return (clans ?? new List<Clan>()).Where((Clan x) => x != null).OrderBy((Clan x) => x == rulingClan ? 1 : 0).ThenBy((Clan x) => x.StringId ?? "", StringComparer.OrdinalIgnoreCase).ToList();
	}

	private static string TransferClanToPlayerKingdom(Kingdom playerKingdom, Kingdom targetKingdom, Clan clan, out bool ok)
	{
		ok = false;
		try
		{
			if (clan.Kingdom == playerKingdom)
			{
				ok = true;
				return "already_in_player_kingdom";
			}
			if (clan.Kingdom != targetKingdom)
			{
				return "clan_not_in_target_kingdom";
			}

			ChangeKingdomAction.ApplyByJoinToKingdomByDefection(clan, targetKingdom, playerKingdom, default(CampaignTime), true);
			ok = clan.Kingdom == playerKingdom;
			return ok ? "joined_player_kingdom" : "join_action_did_not_update_clan_kingdom";
		}
		catch (Exception ex)
		{
			return "exception: " + ex.GetType().Name + " - " + ex.Message;
		}
	}

	private static List<Settlement> SnapshotKingdomSettlements(Kingdom kingdom)
	{
		try
		{
			return Settlement.All.Where((Settlement x) => x != null && x.MapFaction == kingdom).Distinct().ToList();
		}
		catch
		{
			return new List<Settlement>();
		}
	}

	private static int BreakVassalageAgreementsForAnnexedKingdom(Kingdom targetKingdom)
	{
		bool calledBreakAgreementsForAnnexedKingdom = VassalageBehavior.Instance != null;
		bool targetWasPlayerVassal = SafeBool(() => VassalageBehavior.Instance?.IsPlayerVassalKingdom(targetKingdom) == true);
		bool targetWasPlayerSuzerain = SafeBool(() => VassalageBehavior.Instance?.IsKingdomSuzerainOfPlayerForDiagnostics(targetKingdom) == true);
		KingdomAnnexationDiagnosticLog.Event("vassalage.cleanup.start", KingdomAnnexationDiagnosticLog.BuildVassalageCleanupSnapshot(targetKingdom, calledBreakAgreementsForAnnexedKingdom, 0, targetWasPlayerVassal, targetWasPlayerSuzerain, "before"));
		try
		{
			string targetName = GetKingdomDisplayName(targetKingdom, "the target kingdom");
			int removed = VassalageBehavior.Instance?.BreakAgreementsForAnnexedKingdom(targetKingdom, "kingdom_annexation", DiplomacyUiLocalization.Format("AF_DIP_UI_Annexation_VassalageEndedReason", "{TARGET} has joined your kingdom; its former vassalage treaties are now void.", ("TARGET", targetName))) ?? 0;
			KingdomAnnexationDiagnosticLog.Event("vassalage.cleanup.finish", KingdomAnnexationDiagnosticLog.BuildVassalageCleanupSnapshot(targetKingdom, calledBreakAgreementsForAnnexedKingdom, removed, targetWasPlayerVassal, targetWasPlayerSuzerain, "after"));
			return removed;
		}
		catch (Exception ex)
		{
			Logger.Log("KingdomAnnexation", "[WARN] vassalage cleanup failed: " + ex.Message);
			KingdomAnnexationDiagnosticLog.Event("vassalage.cleanup.finish", KingdomAnnexationDiagnosticLog.BuildVassalageCleanupSnapshot(targetKingdom, calledBreakAgreementsForAnnexedKingdom, 0, targetWasPlayerVassal, targetWasPlayerSuzerain, "error", ex.GetType().Name + ": " + ex.Message));
			return 0;
		}
	}

	private static bool TryMakePeace(Kingdom playerKingdom, Kingdom targetKingdom)
	{
		try
		{
			if (playerKingdom != null && targetKingdom != null && playerKingdom.IsAtWarWith(targetKingdom))
			{
				KingdomAnnexationDiagnosticLog.Event("diplomacy.peace.before", KingdomAnnexationDiagnosticLog.BuildPeaceSnapshot(playerKingdom, targetKingdom, "before_make_peace", actionAttempted: true));
				MakePeaceAction.Apply(playerKingdom, targetKingdom);
				KingdomAnnexationDiagnosticLog.Event("diplomacy.peace.after", KingdomAnnexationDiagnosticLog.BuildPeaceSnapshot(playerKingdom, targetKingdom, "after_make_peace", actionAttempted: true, actionReturned: true));
				LogEvent("diplomacy.peace", new Dictionary<string, object>
				{
					["playerKingdom"] = DescribeKingdom(playerKingdom),
					["targetKingdom"] = DescribeKingdom(targetKingdom),
					["ok"] = !playerKingdom.IsAtWarWith(targetKingdom)
				});
				return true;
			}
			KingdomAnnexationDiagnosticLog.Event("diplomacy.peace.skipped", KingdomAnnexationDiagnosticLog.BuildPeaceSnapshot(playerKingdom, targetKingdom, "not_at_war", actionAttempted: false));
		}
		catch (Exception ex)
		{
			KingdomAnnexationDiagnosticLog.Event("diplomacy.peace.after", KingdomAnnexationDiagnosticLog.BuildPeaceSnapshot(playerKingdom, targetKingdom, "error", actionAttempted: true, actionReturned: false, error: ex.GetType().Name + ": " + ex.Message));
			LogEvent("diplomacy.peace_error", new Dictionary<string, object>
			{
				["playerKingdom"] = DescribeKingdom(playerKingdom),
				["targetKingdom"] = DescribeKingdom(targetKingdom),
				["error"] = ex.GetType().Name + ": " + ex.Message
			});
		}
		return false;
	}

	private static Kingdom GetPlayerKingdom()
	{
		return Clan.PlayerClan?.Kingdom ?? Hero.MainHero?.Clan?.Kingdom;
	}

	private static Kingdom ResolveHeroKingdom(Hero hero)
	{
		try
		{
			return hero?.Clan?.Kingdom ?? hero?.MapFaction as Kingdom;
		}
		catch
		{
			return null;
		}
	}

	private static Kingdom ResolveKingdomById(string kingdomId)
	{
		string id = (kingdomId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(id))
		{
			return null;
		}
		try
		{
			return Kingdom.All?.FirstOrDefault((Kingdom x) => x != null && string.Equals((x.StringId ?? "").Trim(), id, StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			return null;
		}
	}

	private static bool IsValidKingdom(Kingdom kingdom)
	{
		try
		{
			return kingdom != null && !kingdom.IsEliminated && !string.IsNullOrWhiteSpace(kingdom.StringId);
		}
		catch
		{
			return false;
		}
	}

	private static bool IsPlayerRuler(Kingdom kingdom)
	{
		try
		{
			return kingdom != null && Clan.PlayerClan != null && (kingdom.RulingClan == Clan.PlayerClan || kingdom.Leader == Hero.MainHero);
		}
		catch
		{
			return false;
		}
	}

	private static bool IsKingdomRuler(Hero hero, Kingdom kingdom)
	{
		try
		{
			return hero != null && kingdom != null && (kingdom.Leader == hero || kingdom.RulingClan?.Leader == hero);
		}
		catch
		{
			return false;
		}
	}

	private static string GetKingdomDisplayName(Kingdom kingdom, string fallback)
	{
		try
		{
			string name = kingdom?.Name?.ToString();
			return string.IsNullOrWhiteSpace(name) ? (fallback ?? DiplomacyMiscPromptTexts.Get("Prompt_KingdomFallback", "kingdom")) : name;
		}
		catch
		{
			return fallback ?? DiplomacyMiscPromptTexts.Get("Prompt_KingdomFallback", "kingdom");
		}
	}

	private static string GetHeroDisplayName(Hero hero, string fallback)
	{
		try
		{
			string name = hero?.Name?.ToString();
			return string.IsNullOrWhiteSpace(name) ? (fallback ?? DiplomacyMiscPromptTexts.Get("Prompt_CounterpartFallback", "counterpart")) : name;
		}
		catch
		{
			return fallback ?? DiplomacyMiscPromptTexts.Get("Prompt_CounterpartFallback", "counterpart");
		}
	}

	private static string DescribeKingdom(Kingdom kingdom)
	{
		if (kingdom == null)
		{
			return "(null)";
		}
		return (kingdom.StringId ?? "") + "|" + GetKingdomDisplayName(kingdom, DiplomacyMiscPromptTexts.Get("Prompt_KingdomFallback", "kingdom")) + "|eliminated=" + SafeBool(() => kingdom.IsEliminated);
	}

	private static string DescribeClan(Clan clan)
	{
		if (clan == null)
		{
			return "(null)";
		}
		return (clan.StringId ?? "") + "|" + (clan.Name?.ToString() ?? DiplomacyMiscPromptTexts.Get("Prompt_ClanFallback", "clan")) + "|kingdom=" + (clan.Kingdom?.StringId ?? "") + "|eliminated=" + SafeBool(() => clan.IsEliminated);
	}

	private static string DescribeHero(Hero hero)
	{
		if (hero == null)
		{
			return "(null)";
		}
		return (hero.StringId ?? "") + "|" + GetHeroDisplayName(hero, DiplomacyMiscPromptTexts.Get("Prompt_HeroFallback", "hero")) + "|clan=" + (hero.Clan?.StringId ?? "") + "|kingdom=" + (hero.Clan?.Kingdom?.StringId ?? "");
	}

	private static string DescribeSettlement(Settlement settlement)
	{
		if (settlement == null)
		{
			return "(null)";
		}
		return (settlement.StringId ?? "") + "|" + (settlement.Name?.ToString() ?? "settlement") + "|ownerClan=" + (settlement.OwnerClan?.StringId ?? "") + "|mapFaction=" + (settlement.MapFaction?.StringId ?? "");
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

	private static void LogApplyReject(string reason, Kingdom playerKingdom, Kingdom targetKingdom, Hero conversationHero)
	{
		Dictionary<string, object> snapshot = KingdomAnnexationDiagnosticLog.BuildRejectSnapshot(reason, playerKingdom, targetKingdom, conversationHero);
		LogEvent("apply.reject", snapshot);
		KingdomAnnexationDiagnosticLog.Event("apply.reject", snapshot);
	}

	private static void LogEvent(string eventName, Dictionary<string, object> fields)
	{
		try
		{
			Logger.Obs("KingdomAnnexation", eventName, fields ?? new Dictionary<string, object>());
		}
		catch
		{
		}
		try
		{
			Logger.Log("KingdomAnnexation", eventName + " " + string.Join(" ", (fields ?? new Dictionary<string, object>()).Select((KeyValuePair<string, object> x) => x.Key + "=" + FormatLogValue(x.Value))));
		}
		catch
		{
		}
	}

	private static string FormatLogValue(object value)
	{
		if (value == null)
		{
			return "";
		}
		if (value is string text)
		{
			return text.Replace("\r", " ").Replace("\n", " ");
		}
		if (value is IEnumerable<string> strings)
		{
			return "[" + string.Join(",", strings) + "]";
		}
		if (value is IDictionary dictionary)
		{
			List<string> parts = new List<string>();
			foreach (DictionaryEntry entry in dictionary)
			{
				parts.Add((entry.Key?.ToString() ?? "") + "=" + FormatLogValue(entry.Value));
				if (parts.Count >= 20)
				{
					parts.Add("<truncated>");
					break;
				}
			}
			return "{" + string.Join(",", parts) + "}";
		}
		if (value is IEnumerable enumerable)
		{
			List<string> parts = new List<string>();
			foreach (object item in enumerable)
			{
				parts.Add(FormatLogValue(item));
				if (parts.Count >= 20)
				{
					parts.Add("<truncated>");
					break;
				}
			}
			return "[" + string.Join(",", parts) + "]";
		}
		return value.ToString();
	}
}
