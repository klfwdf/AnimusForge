using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace AnimusForge
{
	public partial class DiplomacyBehavior : CampaignBehaviorBase
	{
		// ════════════════════════════════════════════════════════ DECLARE_WAR

		private string TryExecuteDeclareWar(Hero npc, string payload)
		{
			string[] parts = (payload ?? "").Split(':');
			if (parts.Length < 2) { Logger.Log("DiplomacyBehavior", "[DeclareWar] Bad format"); return ""; }

			string id1 = parts[0].Trim();
			string id2 = parts[1].Trim();
			if (string.IsNullOrWhiteSpace(id1) || string.IsNullOrWhiteSpace(id2)) { Logger.Log("DiplomacyBehavior", "[DeclareWar] Empty id(s)"); return ""; }

			Kingdom npcKingdom = npc.Clan?.Kingdom;
			if (npcKingdom == null) { Logger.Log("DiplomacyBehavior", "[DeclareWar] NPC has no kingdom"); return ""; }
			string npcKingdomId = npcKingdom.StringId;

			Kingdom declarer; Kingdom target;
			if (string.Equals(id2, npcKingdomId, StringComparison.OrdinalIgnoreCase))
			{
				Kingdom playerKingdom = Clan.PlayerClan?.Kingdom;
				if (playerKingdom == null || playerKingdom.IsEliminated) { Logger.Log("DiplomacyBehavior", "[DeclareWar] Player has no kingdom"); return ""; }
				bool payloadMatchesPlayerKingdom = string.Equals(id1, playerKingdom.StringId, StringComparison.OrdinalIgnoreCase);
				if (!DirectDiplomacyWarGuard.CanDeclareForPlayerKingdom(IsPlayerKing(), payloadMatchesPlayerKingdom))
				{
					Logger.Log("DiplomacyBehavior", !payloadMatchesPlayerKingdom
						? $"[DeclareWar] Declarer {id1} != player kingdom"
						: "[DeclareWar] Player not king");
					return "";
				}
				declarer = playerKingdom; target = npcKingdom;
			}
			else if (string.Equals(id1, npcKingdomId, StringComparison.OrdinalIgnoreCase))
			{
				if (npc != npcKingdom.RulingClan?.Leader) { Logger.Log("DiplomacyBehavior", $"[DeclareWar] NPC not king"); return ""; }
				declarer = npcKingdom; target = ResolveKingdom(id2);
				if (target == null || target.IsEliminated) { Logger.Log("DiplomacyBehavior", $"[DeclareWar] Target not found: {id2}"); return ""; }
			}
			else { Logger.Log("DiplomacyBehavior", $"[DeclareWar] Neither id matches NPC kingdom {npcKingdomId}"); return ""; }

			if (declarer == target) { Logger.Log("DiplomacyBehavior", "[DeclareWar] Same kingdom"); return ""; }
			if (FactionManager.IsAtWarAgainstFaction(declarer, target)) { Logger.Log("DiplomacyBehavior", "[DeclareWar] Already at war"); return ""; }

			IAllianceCampaignBehavior allianceBeh = Campaign.Current.GetCampaignBehavior<IAllianceCampaignBehavior>();
			if (allianceBeh != null && allianceBeh.IsAllyWithKingdom(declarer, target))
			{
				Logger.Log("DiplomacyBehavior", "[DeclareWar] Allied kingdoms must explicitly break the alliance before declaring war");
				return "";
			}

			MeetingBattleRuntime.RunWithDiplomaticSideEffectsUnlocked("diplomacy_declare_war", () =>
				DeclareWarAction.ApplyByKingdomDecision(declarer, target));
			if (!DirectDiplomacyWarGuard.DidDeclarationTakeEffect(FactionManager.IsAtWarAgainstFaction(declarer, target)))
			{
				Logger.Log("DiplomacyBehavior", $"[DeclareWar] Apply completed but kingdoms remain at peace: {declarer.StringId} -> {target.StringId}");
				return "";
			}
			Logger.Log("DiplomacyBehavior", $"[DeclareWar] {declarer.StringId} -> {target.StringId}");
			WorldDiplomacyBehavior.NotifyExternalDiplomacyResolved("declare_war", declarer, target, "面对面口头外交达成");
			return "";
		}

		private string TryExecuteIndependentClanPeace(Hero npc, string payload)
		{
			if (!string.IsNullOrWhiteSpace(payload))
			{
				Logger.Log("DiplomacyBehavior", "[IndependentClanPeace] Unexpected payload rejected");
				return "";
			}

			if (!TryResolveIndependentClanPeaceContext(npc, out Clan playerClan, out Kingdom targetKingdom))
			{
				Logger.Log("DiplomacyBehavior", "[IndependentClanPeace] Runtime eligibility no longer valid npc=" + (npc?.StringId ?? ""));
				return "";
			}

			MeetingBattleRuntime.RunWithDiplomaticSideEffectsUnlocked("diplomacy_independent_clan_peace", () =>
				MakePeaceAction.Apply(playerClan, targetKingdom));
			if (FactionManager.IsAtWarAgainstFaction(playerClan, targetKingdom))
			{
				Logger.Log("DiplomacyBehavior", "[IndependentClanPeace] Apply completed but factions remain hostile playerClan=" + playerClan.StringId + " targetKingdom=" + targetKingdom.StringId);
				return "";
			}

			DiplomacyRecentPeaceGuard.RegisterPeace(playerClan, targetKingdom, "diplomacy_independent_clan_peace");
			Logger.Log("DiplomacyBehavior", "[IndependentClanPeace] success playerClan=" + playerClan.StringId + " targetKingdom=" + targetKingdom.StringId + " king=" + npc.StringId);
			return "";
		}

		// ════════════════════════════════════════════════════════ MAKE_PEACE

		private string TryExecuteMakePeace(Hero npc, string payload)
		{
			string[] parts = (payload ?? "").Split(':');
			if (parts.Length < 3) { Logger.Log("DiplomacyBehavior", "[MakePeace] Bad format"); return ""; }
			string id1 = parts[0].Trim(), id2 = parts[1].Trim();
			string amountStr = parts.Length > 2 ? parts[2].Trim() : "0";
			string daysStr = parts.Length > 3 ? parts[3].Trim() : "default";
			if (string.IsNullOrWhiteSpace(id1) || string.IsNullOrWhiteSpace(id2)) { Logger.Log("DiplomacyBehavior", "[MakePeace] Empty id(s)"); return ""; }

			Kingdom playerKingdom = Clan.PlayerClan?.Kingdom;
			Kingdom npcKingdom = npc.Clan?.Kingdom;
			if (playerKingdom == null || playerKingdom.IsEliminated) { Logger.Log("DiplomacyBehavior", "[MakePeace] Player no kingdom"); return ""; }
			if (npcKingdom == null) { Logger.Log("DiplomacyBehavior", "[MakePeace] NPC no kingdom"); return ""; }
			if (!IsPlayerKing()) { Logger.Log("DiplomacyBehavior", "[MakePeace] Player not king"); return ""; }
			if (!IsNpcKing(npc, npcKingdom)) { Logger.Log("DiplomacyBehavior", "[MakePeace] NPC not king"); return ""; }

			Kingdom payer, receiver;
			if (string.Equals(id1, playerKingdom.StringId, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(id2, npcKingdom.StringId, StringComparison.OrdinalIgnoreCase))
			{ payer = playerKingdom; receiver = npcKingdom; }
			else if (string.Equals(id1, npcKingdom.StringId, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(id2, playerKingdom.StringId, StringComparison.OrdinalIgnoreCase))
			{ payer = npcKingdom; receiver = playerKingdom; }
			else { Logger.Log("DiplomacyBehavior", $"[MakePeace] IDs mismatch"); return ""; }

			if (!FactionManager.IsAtWarAgainstFaction(payer, receiver)) { Logger.Log("DiplomacyBehavior", "[MakePeace] Not at war"); return ""; }

			int tributeAmount = ParseTributeAmount(amountStr, payer, receiver);
			if (tributeAmount < 0) return "";
			int durationDays = ParseDurationDays(daysStr, tributeAmount > 0);

			if (!DiplomacyPeaceTermsService.TryApplyPeace(
				payer,
				receiver,
				tributeAmount,
				durationDays,
				"diplomacy_make_peace",
				out int appliedTribute,
				out int appliedDuration,
				out string failureReason))
			{
				Logger.Log("DiplomacyBehavior", "[MakePeace] execution rejected: " + failureReason);
				return "";
			}
			Logger.Log("DiplomacyBehavior", $"[MakePeace] {payer.StringId}->{receiver.StringId} tribute={appliedTribute} days={appliedDuration}");
			WorldDiplomacyBehavior.NotifyExternalDiplomacyResolved("accept_peace", payer, receiver, "面对面口头外交达成");
			return "";
		}

		// ════════════════════════════════════════════════════════ FORM_ALLIANCE

		private string TryExecuteFormAlliance(Hero npc, string payload)
		{
			// Format: FORM_ALLIANCE:id1:id2:durationDays  (id1,id2 = player+NPC kingdoms)
			string[] parts = (payload ?? "").Split(':');
			if (parts.Length < 2) { Logger.Log("DiplomacyBehavior", "[FormAlliance] Bad format"); return ""; }
			string id1 = parts[0].Trim(), id2 = parts[1].Trim();
			string daysStr = parts.Length > 2 ? parts[2].Trim() : "default";
			if (string.IsNullOrWhiteSpace(id1) || string.IsNullOrWhiteSpace(id2)) { Logger.Log("DiplomacyBehavior", "[FormAlliance] Empty id(s)"); return ""; }

			Kingdom playerKingdom = Clan.PlayerClan?.Kingdom;
			Kingdom npcKingdom = npc.Clan?.Kingdom;
			if (playerKingdom == null || playerKingdom.IsEliminated) { Logger.Log("DiplomacyBehavior", "[FormAlliance] Player no kingdom"); return ""; }
			if (npcKingdom == null) { Logger.Log("DiplomacyBehavior", "[FormAlliance] NPC no kingdom"); return ""; }
			if (!IsPlayerKing()) { Logger.Log("DiplomacyBehavior", "[FormAlliance] Player not king"); return ""; }
			if (!IsNpcKing(npc, npcKingdom)) { Logger.Log("DiplomacyBehavior", "[FormAlliance] NPC not king"); return ""; }

			// Validate: the two IDs must be player+NPC kingdoms
			if (!IsPlayerNpcPair(id1, id2, playerKingdom, npcKingdom)) { Logger.Log("DiplomacyBehavior", $"[FormAlliance] IDs mismatch"); return ""; }

			if (playerKingdom == npcKingdom) { Logger.Log("DiplomacyBehavior", "[FormAlliance] Same kingdom"); return ""; }

			IAllianceCampaignBehavior allianceBeh = Campaign.Current.GetCampaignBehavior<IAllianceCampaignBehavior>();
			if (allianceBeh == null) { Logger.Log("DiplomacyBehavior", "[FormAlliance] No Alliance behavior"); return ""; }
			if (allianceBeh.IsAllyWithKingdom(playerKingdom, npcKingdom)) { Logger.Log("DiplomacyBehavior", "[FormAlliance] Already allied"); return ""; }

			// Check alliance count (max 2 per kingdom)
			int allianceCount = 0;
			foreach (Kingdom k in Kingdom.All)
			{ if (!k.IsEliminated && k != playerKingdom && allianceBeh.IsAllyWithKingdom(playerKingdom, k)) allianceCount++; }
			if (allianceCount >= 2) { Logger.Log("DiplomacyBehavior", "[FormAlliance] Player at max alliances (2)"); return ""; }
			allianceCount = 0;
			foreach (Kingdom k in Kingdom.All)
			{ if (!k.IsEliminated && k != npcKingdom && allianceBeh.IsAllyWithKingdom(npcKingdom, k)) allianceCount++; }
			if (allianceCount >= 2) { Logger.Log("DiplomacyBehavior", "[FormAlliance] NPC at max alliances (2)"); return ""; }

			MeetingBattleRuntime.RunWithDiplomaticSideEffectsUnlocked("diplomacy_form_alliance", () =>
				allianceBeh.StartAlliance(playerKingdom, npcKingdom));
			Logger.Log("DiplomacyBehavior", $"[FormAlliance] {playerKingdom.StringId} <-> {npcKingdom.StringId}");
			WorldDiplomacyBehavior.NotifyExternalDiplomacyResolved("accept_alliance", playerKingdom, npcKingdom, "面对面口头外交达成");
			return "";
		}

		// ════════════════════════════════════════════════════════ BREAK_ALLIANCE

		private string TryExecuteBreakAlliance(Hero npc, string payload)
		{
			// Format: BREAK_ALLIANCE:id1:id2  (unilateral, id1,id2 = player+NPC kingdoms)
			string[] parts = (payload ?? "").Split(':');
			if (parts.Length < 2) { Logger.Log("DiplomacyBehavior", "[BreakAlliance] Bad format"); return ""; }
			string id1 = parts[0].Trim(), id2 = parts[1].Trim();
			if (string.IsNullOrWhiteSpace(id1) || string.IsNullOrWhiteSpace(id2)) { Logger.Log("DiplomacyBehavior", "[BreakAlliance] Empty id(s)"); return ""; }

			Kingdom playerKingdom = Clan.PlayerClan?.Kingdom;
			Kingdom npcKingdom = npc.Clan?.Kingdom;
			if (playerKingdom == null || playerKingdom.IsEliminated) { Logger.Log("DiplomacyBehavior", "[BreakAlliance] Player no kingdom"); return ""; }
			if (npcKingdom == null) { Logger.Log("DiplomacyBehavior", "[BreakAlliance] NPC no kingdom"); return ""; }
			if (!IsPlayerNpcPair(id1, id2, playerKingdom, npcKingdom)) { Logger.Log("DiplomacyBehavior", $"[BreakAlliance] IDs mismatch"); return ""; }

			IAllianceCampaignBehavior allianceBeh = Campaign.Current.GetCampaignBehavior<IAllianceCampaignBehavior>();
			if (allianceBeh == null) { Logger.Log("DiplomacyBehavior", "[BreakAlliance] No Alliance behavior"); return ""; }
			if (!allianceBeh.IsAllyWithKingdom(playerKingdom, npcKingdom)) { Logger.Log("DiplomacyBehavior", "[BreakAlliance] Not allied"); return ""; }

			MeetingBattleRuntime.RunWithDiplomaticSideEffectsUnlocked("diplomacy_break_alliance", () =>
				PermanentAllianceGuard.RunAuthorizedBreak("diplomacy_break_alliance",
					playerKingdom,
					npcKingdom,
					() => allianceBeh.EndAlliance(playerKingdom, npcKingdom)));
			Logger.Log("DiplomacyBehavior", $"[BreakAlliance] {playerKingdom.StringId} <-> {npcKingdom.StringId}");
			WorldDiplomacyBehavior.NotifyExternalDiplomacyResolved("break_alliance", playerKingdom, npcKingdom, "面对面口头外交达成");
			return "";
		}

		// ════════════════════════════════════════════════════════ MAKE_TRADE

		private string TryExecuteMakeTrade(Hero npc, string payload)
		{
			// Format: MAKE_TRADE:id1:id2:durationDays  (id1,id2 = player+NPC kingdoms)
			string[] parts = (payload ?? "").Split(':');
			if (parts.Length < 2) { Logger.Log("DiplomacyBehavior", "[MakeTrade] Bad format"); return ""; }
			string id1 = parts[0].Trim(), id2 = parts[1].Trim();
			string daysStr = parts.Length > 2 ? parts[2].Trim() : "default";
			if (string.IsNullOrWhiteSpace(id1) || string.IsNullOrWhiteSpace(id2)) { Logger.Log("DiplomacyBehavior", "[MakeTrade] Empty id(s)"); return ""; }

			Kingdom playerKingdom = Clan.PlayerClan?.Kingdom;
			Kingdom npcKingdom = npc.Clan?.Kingdom;
			if (playerKingdom == null || playerKingdom.IsEliminated) { Logger.Log("DiplomacyBehavior", "[MakeTrade] Player no kingdom"); return ""; }
			if (npcKingdom == null) { Logger.Log("DiplomacyBehavior", "[MakeTrade] NPC no kingdom"); return ""; }
			if (!IsPlayerKing()) { Logger.Log("DiplomacyBehavior", "[MakeTrade] Player not king"); return ""; }
			if (!IsNpcKing(npc, npcKingdom)) { Logger.Log("DiplomacyBehavior", "[MakeTrade] NPC not king"); return ""; }
			if (!IsPlayerNpcPair(id1, id2, playerKingdom, npcKingdom)) { Logger.Log("DiplomacyBehavior", $"[MakeTrade] IDs mismatch"); return ""; }

			ITradeAgreementsCampaignBehavior tradeBeh = Campaign.Current.GetCampaignBehavior<ITradeAgreementsCampaignBehavior>();
			if (tradeBeh == null) { Logger.Log("DiplomacyBehavior", "[MakeTrade] No Trade behavior"); return ""; }
			if (HasTradeAgreementCompat(tradeBeh, playerKingdom, npcKingdom)) { Logger.Log("DiplomacyBehavior", "[MakeTrade] Already trading"); return ""; }

			CampaignTime duration;
			if (daysStr.Equals("default", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(daysStr) || daysStr == "0")
				duration = Campaign.Current.Models.TradeAgreementModel.GetTradeAgreementDurationInYears(playerKingdom, npcKingdom);
			else if (int.TryParse(daysStr, out int parsedDays))
				duration = CampaignTime.Days(Math.Max(1, MBMath.ClampInt(parsedDays, 1, 252)));
			else
				duration = Campaign.Current.Models.TradeAgreementModel.GetTradeAgreementDurationInYears(playerKingdom, npcKingdom);

			MeetingBattleRuntime.RunWithDiplomaticSideEffectsUnlocked("diplomacy_make_trade", () =>
				tradeBeh.MakeTradeAgreement(playerKingdom, npcKingdom, duration));
			Logger.Log("DiplomacyBehavior", $"[MakeTrade] {playerKingdom.StringId} <-> {npcKingdom.StringId} days={(int)duration.ToDays}");
			WorldDiplomacyBehavior.NotifyExternalDiplomacyResolved("accept_trade", playerKingdom, npcKingdom, "面对面口头外交达成");
			return "";
		}

		// ════════════════════════════════════════════════════════ CANCEL_TRADE

		private string TryExecuteCancelTrade(Hero npc, string payload)
		{
			// Format: CANCEL_TRADE:id1:id2  (unilateral, id1,id2 = player+NPC kingdoms)
			string[] parts = (payload ?? "").Split(':');
			if (parts.Length < 2) { Logger.Log("DiplomacyBehavior", "[CancelTrade] Bad format"); return ""; }
			string id1 = parts[0].Trim(), id2 = parts[1].Trim();
			if (string.IsNullOrWhiteSpace(id1) || string.IsNullOrWhiteSpace(id2)) { Logger.Log("DiplomacyBehavior", "[CancelTrade] Empty id(s)"); return ""; }

			Kingdom playerKingdom = Clan.PlayerClan?.Kingdom;
			Kingdom npcKingdom = npc.Clan?.Kingdom;
			if (playerKingdom == null || playerKingdom.IsEliminated) { Logger.Log("DiplomacyBehavior", "[CancelTrade] Player no kingdom"); return ""; }
			if (npcKingdom == null) { Logger.Log("DiplomacyBehavior", "[CancelTrade] NPC no kingdom"); return ""; }
			if (!IsPlayerNpcPair(id1, id2, playerKingdom, npcKingdom)) { Logger.Log("DiplomacyBehavior", $"[CancelTrade] IDs mismatch"); return ""; }

			ITradeAgreementsCampaignBehavior tradeBeh = Campaign.Current.GetCampaignBehavior<ITradeAgreementsCampaignBehavior>();
			if (tradeBeh == null) { Logger.Log("DiplomacyBehavior", "[CancelTrade] No Trade behavior"); return ""; }
			if (!HasTradeAgreementCompat(tradeBeh, playerKingdom, npcKingdom)) { Logger.Log("DiplomacyBehavior", "[CancelTrade] No trade agreement"); return ""; }

			MeetingBattleRuntime.RunWithDiplomaticSideEffectsUnlocked("diplomacy_cancel_trade", () =>
				tradeBeh.EndTradeAgreement(playerKingdom, npcKingdom));
			Logger.Log("DiplomacyBehavior", $"[CancelTrade] {playerKingdom.StringId} <-> {npcKingdom.StringId}");
			WorldDiplomacyBehavior.NotifyExternalDiplomacyResolved("cancel_trade", playerKingdom, npcKingdom, "面对面口头外交达成");
			return "";
		}

		// ════════════════════════════════════════════════════════════════════
		//  Shared helpers
		// ════════════════════════════════════════════════════════════════════

		private static bool IsPlayerNpcPair(string id1, string id2, Kingdom playerKingdom, Kingdom npcKingdom)
		{
			return (string.Equals(id1, playerKingdom.StringId, StringComparison.OrdinalIgnoreCase)
					&& string.Equals(id2, npcKingdom.StringId, StringComparison.OrdinalIgnoreCase))
				|| (string.Equals(id1, npcKingdom.StringId, StringComparison.OrdinalIgnoreCase)
					&& string.Equals(id2, playerKingdom.StringId, StringComparison.OrdinalIgnoreCase));
		}

		private static bool HasTradeAgreementCompat(ITradeAgreementsCampaignBehavior tradeBeh, Kingdom kingdom, Kingdom other)
		{
			return BannerlordApiCompat.HasTradeAgreement(tradeBeh, kingdom, other);
		}

		private static int ParseTributeAmount(string amountStr, Kingdom payer, Kingdom receiver)
		{
			return DiplomacyPeaceTermsService.ResolveTributeAmount(amountStr, payer, receiver);
		}

		private static int ParseDurationDays(string daysStr, bool hasTribute)
		{
			return DiplomacyPeaceTermsService.ResolveDurationDays(daysStr, hasTribute);
		}

		internal static bool TryBuildTributePowerContext(Kingdom payer, Kingdom receiver, out AfTributePowerContext context)
		{
			context = default;
			try
			{
				if (payer == null || receiver == null || payer == receiver || Campaign.Current?.Models?.DiplomacyModel == null)
				{
					return false;
				}
				var dm = Campaign.Current.Models.DiplomacyModel;
				float scorePayer = dm.GetScoreOfDeclaringPeace(payer, receiver);
				float scoreReceiver = dm.GetScoreOfDeclaringPeace(receiver, payer);
				float settlementValue = dm.GetValueOfSettlementsForFaction(payer);
				float receiverDecisionThreshold = dm.GetDecisionMakingThreshold(receiver);
				float num = scoreReceiver > 0f ? scoreReceiver - scorePayer : receiverDecisionThreshold - scoreReceiver;
				float payerWarProgress = dm.GetWarProgressScore(payer, receiver).ResultNumber;
				float receiverWarProgress = dm.GetWarProgressScore(receiver, payer).ResultNumber;
				float warDiff = MathF.Abs(payerWarProgress - receiverWarProgress);
				float rawRatio = num / (settlementValue + 1f);
				float ratio = rawRatio;
				if (warDiff < 75f)
				{
					ratio = 0.05f;
				}
				else
				{
					ratio /= 2f;
					if (ratio < 0.05f)
					{
						ratio = 0f;
					}
					else if (ratio < 0.10f)
					{
						ratio = 0.05f;
					}
					else if (ratio < 0.15f)
					{
						ratio = 0.10f;
					}
					else
					{
						ratio = 0.15f;
					}
				}
				float payerFiefProsperity = payer.Fiefs.Sum(x => x.Prosperity);
				int calculatedTribute = (int)(ratio * payerFiefProsperity * 0.35f) / 10 * 10;
				context = new AfTributePowerContext(
					scorePayer,
					scoreReceiver,
					receiverDecisionThreshold,
					settlementValue,
					payerWarProgress,
					receiverWarProgress,
					warDiff,
					rawRatio,
					ratio,
					payerFiefProsperity,
					calculatedTribute);
				return true;
			}
			catch (Exception ex)
			{
				Logger.Log("DiplomacyBehavior", "[TributePower] context failed: " + ex.Message);
				return false;
			}
		}

		private static int CalculateTribute(Kingdom payer, Kingdom receiver)
		{
			return TryBuildTributePowerContext(payer, receiver, out AfTributePowerContext context)
				? context.CalculatedTribute
				: 0;
		}

		private static Kingdom ResolveKingdom(string id)
		{
			if (string.IsNullOrWhiteSpace(id)) return null;
			foreach (Kingdom k in Kingdom.All)
			{ if (!k.IsEliminated && (string.Equals(k.StringId, id, StringComparison.OrdinalIgnoreCase) || string.Equals(k.Name?.ToString() ?? "", id, StringComparison.OrdinalIgnoreCase))) return k; }
			return null;
		}

		private static bool IsPlayerKing()
		{
			Kingdom pk = Clan.PlayerClan?.Kingdom;
			return pk != null && Hero.MainHero == pk.RulingClan?.Leader;
		}

		private static bool IsNegotiableWar(IFaction playerFaction, IFaction targetFaction)
		{
			return playerFaction != null
				&& targetFaction != null
				&& playerFaction != targetFaction
				&& !playerFaction.IsEliminated
				&& !targetFaction.IsEliminated
				&& FactionManager.IsAtWarAgainstFaction(playerFaction, targetFaction)
				&& !FactionManager.IsAtConstantWarAgainstFaction(playerFaction, targetFaction);
		}

		private static bool TryResolveIndependentClanPeaceContext(Hero npc, out Clan playerClan, out Kingdom targetKingdom)
		{
			playerClan = null;
			targetKingdom = null;
			try
			{
				Clan candidatePlayerClan = Clan.PlayerClan ?? Hero.MainHero?.Clan;
				if (Campaign.Current == null
					|| Hero.MainHero == null
					|| candidatePlayerClan == null
					|| candidatePlayerClan.IsEliminated
					|| candidatePlayerClan.Kingdom != null
					|| candidatePlayerClan.IsUnderMercenaryService
					|| (candidatePlayerClan.Leader != null && candidatePlayerClan.Leader != Hero.MainHero))
				{
					return false;
				}

				Clan targetClan = npc?.Clan;
				if (npc == null
					|| npc == Hero.MainHero
					|| npc.IsDead
					|| targetClan == null
					|| targetClan == candidatePlayerClan
					|| targetClan.IsEliminated
					|| targetClan.IsBanditFaction
					|| targetClan.IsOutlaw)
				{
					return false;
				}

				// Runs on every eligible prompt/postprocess turn. Resolve only the speaking hero's kingdom;
				// requiring its current ruler avoids world scans and prevents ordinary lords from negotiating.
				Kingdom candidateTargetKingdom = targetClan.Kingdom ?? npc.MapFaction as Kingdom;
				if (candidateTargetKingdom?.RulingClan?.Leader == npc
					&& IsNegotiableWar(candidatePlayerClan, candidateTargetKingdom))
				{
					playerClan = candidatePlayerClan;
					targetKingdom = candidateTargetKingdom;
					return true;
				}
			}
			catch
			{
			}
			return false;
		}

		internal static bool CanUseIndependentClanPeaceForExternal(Hero targetHero, CharacterObject targetCharacter = null)
		{
			return TryResolveIndependentClanPeaceContext(targetHero ?? targetCharacter?.HeroObject, out _, out _);
		}

		internal static bool IsIndependentClanPeacePostprocessTag(string tag)
		{
			return string.Equals((tag ?? "").Trim(), IndependentClanPeaceTag, StringComparison.OrdinalIgnoreCase);
		}

		private static bool IsPlayerIndependentSettlementClan()
		{
			try
			{
				Clan playerClan = Clan.PlayerClan ?? Hero.MainHero?.Clan;
				if (playerClan == null || Hero.MainHero == null || playerClan.Kingdom != null || playerClan.IsUnderMercenaryService)
				{
					return false;
				}
				if (playerClan.Leader != null && playerClan.Leader != Hero.MainHero)
				{
					return false;
				}
				return (playerClan.Settlements ?? Enumerable.Empty<Settlement>()).Any((Settlement x) => x != null && (x.IsTown || x.IsCastle));
			}
			catch
			{
				return false;
			}
		}

		private static bool IsNpcKing(Hero npc, Kingdom npcKingdom)
		{
			return npc != null && npcKingdom != null && npc == npcKingdom.RulingClan?.Leader;
		}

		internal static bool CanInjectDiplomacyRuleForExternal(Hero targetHero, CharacterObject targetCharacter = null)
		{
			try
			{
				Hero npc = targetHero ?? targetCharacter?.HeroObject;
				if (npc == null || npc == Hero.MainHero || npc.IsDead)
				{
					return false;
				}
				Kingdom npcKingdom = npc.Clan?.Kingdom;
				return npcKingdom != null && !npcKingdom.IsEliminated && IsNpcKing(npc, npcKingdom);
			}
			catch
			{
				return false;
			}
		}

		internal static bool CanUseDiplomacyActionPostprocessForExternal(Hero targetHero, CharacterObject targetCharacter = null)
		{
			return CanUseFullDiplomacyActionPostprocessForExternal(targetHero, targetCharacter)
				|| CanUseNpcSovereignDeclareWarPostprocessForExternal(targetHero, targetCharacter);
		}

		internal static bool CanUseFullDiplomacyActionPostprocessForExternal(Hero targetHero, CharacterObject targetCharacter = null)
		{
			try
			{
				Hero npc = targetHero ?? targetCharacter?.HeroObject;
				Kingdom playerKingdom = Clan.PlayerClan?.Kingdom;
				Kingdom npcKingdom = npc?.Clan?.Kingdom;
				return npc != null
					&& playerKingdom != null
					&& npcKingdom != null
					&& !playerKingdom.IsEliminated
					&& !npcKingdom.IsEliminated
					&& playerKingdom != npcKingdom
					&& IsPlayerKing()
					&& IsNpcKing(npc, npcKingdom);
			}
			catch
			{
				return false;
			}
		}

		internal static bool CanUseNpcSovereignDeclareWarPostprocessForExternal(Hero targetHero, CharacterObject targetCharacter = null)
		{
			try
			{
				Hero npc = targetHero ?? targetCharacter?.HeroObject;
				Kingdom npcKingdom = npc?.Clan?.Kingdom;
				return npc != null
					&& npc != Hero.MainHero
					&& !npc.IsDead
					&& npcKingdom != null
					&& !npcKingdom.IsEliminated
					&& IsNpcKing(npc, npcKingdom);
			}
			catch
			{
				return false;
			}
		}

		private static string GetKingdomDisplayName(Kingdom k)
		{ return k?.Name?.ToString() ?? k?.StringId ?? "未知王国"; }
	}
}
