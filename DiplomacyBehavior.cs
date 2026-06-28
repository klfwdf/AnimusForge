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
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace AnimusForge
{
	public class DiplomacyBehavior : CampaignBehaviorBase
	{
		public static DiplomacyBehavior Instance { get; private set; }

		private static bool s_globalPatchesApplied;

		[ModuleInitializer]
		internal static void ModuleInit()
		{
			ApplyGlobalPatchesOnce();
		}

		private static void ApplyGlobalPatchesOnce()
		{
			if (s_globalPatchesApplied) return;
			s_globalPatchesApplied = true;
			try
			{
				Harmony harmony = new Harmony("com.AnimusForge.diplomacy");
				harmony.Patch(
					typeof(MyBehavior).GetMethod("BuildShoutPromptContextForExternal",
						BindingFlags.Public | BindingFlags.Static),
					postfix: new HarmonyMethod(typeof(DiplomacyBehavior), nameof(Patch_BuildDiplomacyContext_Postfix)));
				Logger.Log("DiplomacyBehavior", "[Harmony] Diplomacy context injection applied.");
			}
			catch (Exception ex)
			{
				Logger.Log("DiplomacyBehavior", $"[Harmony Error] {ex.Message}");
			}
		}

		public override void RegisterEvents()
		{
			ApplyGlobalPatchesOnce();
			Instance = this;
			Logger.Log("DiplomacyBehavior", "[Lifecycle] Registered.");
		}

		public override void SyncData(IDataStore dataStore)
		{
		}

		public static void ProcessDiplomacyTagsDispatch(Hero npc, ref string text)
		{
			if (npc == null || string.IsNullOrEmpty(text)) return;
			if (text.IndexOf("DIPLOMACY", StringComparison.OrdinalIgnoreCase) < 0) return;

			DiplomacyBehavior behavior = Instance
				?? Campaign.Current?.GetCampaignBehavior<DiplomacyBehavior>();
			if (behavior == null)
			{
				Logger.Log("DiplomacyBehavior", "[Dispatch] Instance is null, abort.");
				return;
			}
			behavior.ProcessDiplomacyTags(npc, ref text);
		}

		private static readonly Regex DiplomacyTagRegex = new Regex(
			@"\[ACTION:DIPLOMACY:([A-Z_]+):([^\]]*)\]",
			RegexOptions.IgnoreCase | RegexOptions.Compiled);

		private void ProcessDiplomacyTags(Hero npc, ref string responseText)
		{
			int matchCount = 0;
			responseText = DiplomacyTagRegex.Replace(responseText, match =>
			{
				matchCount++;
				return ProcessSingleDiplomacyTag(npc, match.Groups[1].Value, match.Groups[2].Value);
			});
			if (matchCount > 0)
			{
				responseText = DiplomacyTagRegex.Replace(responseText, "");
				responseText = responseText.Trim();
			}
		}

		private string ProcessSingleDiplomacyTag(Hero npc, string action, string payload)
		{
			try
			{
				Logger.Log("DiplomacyBehavior", $"[Tag] action={action} payload={payload} npc={npc.StringId}");
				switch (action.ToUpperInvariant())
				{
					case "DECLARE_WAR":    return TryExecuteDeclareWar(npc, payload);
					case "MAKE_PEACE":     return TryExecuteMakePeace(npc, payload);
					case "FORM_ALLIANCE":  return TryExecuteFormAlliance(npc, payload);
					case "BREAK_ALLIANCE": return TryExecuteBreakAlliance(npc, payload);
					case "MAKE_TRADE":     return TryExecuteMakeTrade(npc, payload);
					case "CANCEL_TRADE":   return TryExecuteCancelTrade(npc, payload);
					default:
						Logger.Log("DiplomacyBehavior", $"[Tag] Unknown action: {action}");
						return "";
				}
			}
			catch (Exception ex)
			{
				Logger.Log("DiplomacyBehavior", $"[Tag Error] action={action}: {ex.Message}");
				return "";
			}
		}

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
				if (!string.Equals(id1, playerKingdom.StringId, StringComparison.OrdinalIgnoreCase)) { Logger.Log("DiplomacyBehavior", $"[DeclareWar] Declarer {id1} != player kingdom"); return ""; }
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
			{ allianceBeh.EndAlliance(declarer, target); Logger.Log("DiplomacyBehavior", $"[DeclareWar] Broke alliance"); }

			MeetingBattleRuntime.UnlockDiplomaticSideEffects("diplomacy_declare_war");
			DeclareWarAction.ApplyByKingdomDecision(declarer, target);
			Logger.Log("DiplomacyBehavior", $"[DeclareWar] {declarer.StringId} -> {target.StringId}");
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

			MeetingBattleRuntime.UnlockDiplomaticSideEffects("diplomacy_make_peace");
			MakePeaceAction.ApplyByKingdomDecision(payer, receiver, tributeAmount, durationDays);
			Logger.Log("DiplomacyBehavior", $"[MakePeace] {payer.StringId}->{receiver.StringId} tribute={tributeAmount} days={durationDays}");
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

			MeetingBattleRuntime.UnlockDiplomaticSideEffects("diplomacy_form_alliance");
			allianceBeh.StartAlliance(playerKingdom, npcKingdom);
			Logger.Log("DiplomacyBehavior", $"[FormAlliance] {playerKingdom.StringId} <-> {npcKingdom.StringId}");
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

			MeetingBattleRuntime.UnlockDiplomaticSideEffects("diplomacy_break_alliance");
			allianceBeh.EndAlliance(playerKingdom, npcKingdom);
			Logger.Log("DiplomacyBehavior", $"[BreakAlliance] {playerKingdom.StringId} <-> {npcKingdom.StringId}");
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

			MeetingBattleRuntime.UnlockDiplomaticSideEffects("diplomacy_make_trade");
			tradeBeh.MakeTradeAgreement(playerKingdom, npcKingdom, duration);
			Logger.Log("DiplomacyBehavior", $"[MakeTrade] {playerKingdom.StringId} <-> {npcKingdom.StringId} days={(int)duration.ToDays}");
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

			MeetingBattleRuntime.UnlockDiplomaticSideEffects("diplomacy_cancel_trade");
			tradeBeh.EndTradeAgreement(playerKingdom, npcKingdom);
			Logger.Log("DiplomacyBehavior", $"[CancelTrade] {playerKingdom.StringId} <-> {npcKingdom.StringId}");
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
			if (amountStr == "0" || string.IsNullOrEmpty(amountStr)) return 0;
			if (amountStr.Equals("auto", StringComparison.OrdinalIgnoreCase)) return CalculateTribute(payer, receiver);
			if (int.TryParse(amountStr, out int parsed))
			{
				int max = (int)(payer.Fiefs.Sum(x => x.Prosperity) * 0.15f * 0.35f);
				return (MBMath.ClampInt(parsed, 0, max) / 10) * 10;
			}
			return -1;
		}

		private static int ParseDurationDays(string daysStr, bool hasTribute)
		{
			if (daysStr.Equals("default", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(daysStr) || daysStr == "0")
				return hasTribute ? 100 : 0;
			if (int.TryParse(daysStr, out int parsed))
				return MBMath.ClampInt(parsed, 1, 252);
			return hasTribute ? 100 : 0;
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

		private static bool IsNpcKing(Hero npc, Kingdom npcKingdom)
		{
			return npc != null && npcKingdom != null && npc == npcKingdom.RulingClan?.Leader;
		}

		private static string GetKingdomDisplayName(Kingdom k)
		{ return k?.Name?.ToString() ?? k?.StringId ?? "未知王国"; }

		// ════════════════════════════════════════════════════════ LLM context

		internal static string BuildDiplomacyInstructionContext(Hero npc)
		{
			try
			{
				if (npc == null) return "";
				Kingdom npcKingdom = npc.Clan?.Kingdom;
				if (npcKingdom == null) return "";
				bool npcIsKing = npc == npcKingdom.RulingClan?.Leader;
				bool playerIsKing = IsPlayerKing();
				if (!npcIsKing && !playerIsKing) return "";
				Kingdom playerKingdom = Clan.PlayerClan?.Kingdom;
				if (playerKingdom == null || playerKingdom.IsEliminated) return "";

				StringBuilder sb = new StringBuilder();
				sb.AppendLine();
				sb.AppendLine("【国王外交规则】");
				sb.AppendLine("重要：游戏内84天=一年，21天=一季度，没有月和周的概念。谈论时间请用季度或年。");

				if (!playerIsKing)
				{
					sb.AppendLine($"【身份限制】玩家只是{GetKingdomDisplayName(playerKingdom)}的领主，不是国王。");
					sb.AppendLine("只有双方国王才能谈论议和、结盟、贸易等外交事务。");
					sb.AppendLine("但玩家即使不是国王也可以通过挑衅对你方宣战——宣战是单方行为，你阻止不了。");
				}
				else
				{ sb.AppendLine("你和玩家都是国王，可以讨论宣战、议和、结盟、贸易等所有外交事务。"); }

				List<Kingdom> warTargets = new List<Kingdom>();
				foreach (Kingdom k in Kingdom.All)
				{ if (!k.IsEliminated && k != npcKingdom && FactionManager.IsAtWarAgainstFaction(npcKingdom, k)) warTargets.Add(k); }
				if (playerKingdom != npcKingdom && !playerKingdom.IsEliminated && FactionManager.IsAtWarAgainstFaction(npcKingdom, playerKingdom) && !warTargets.Contains(playerKingdom))
					warTargets.Add(playerKingdom);
				foreach (Kingdom enemy in warTargets) AppendWarStatsBlock(sb, npcKingdom, enemy);

				if (playerKingdom != npcKingdom && !playerKingdom.IsEliminated && !FactionManager.IsAtWarAgainstFaction(npcKingdom, playerKingdom))
				{ sb.AppendLine(); sb.AppendLine($"【与{GetKingdomDisplayName(playerKingdom)}的和平状态】双方目前处于和平状态。"); }

				return sb.ToString().TrimEnd();
			}
			catch (Exception ex) { Logger.Log("DiplomacyBehavior", $"[BuildInstruction Error] {ex.Message}"); return ""; }
		}

		private static void AppendWarStatsBlock(StringBuilder sb, Kingdom myKingdom, Kingdom enemy)
		{
			var dm = Campaign.Current.Models.DiplomacyModel;
			StanceLink stance = myKingdom.GetStanceWith(enemy);
			sb.AppendLine(); sb.AppendLine($"【与{GetKingdomDisplayName(enemy)}的战争局势】（仅供判断谈判立场，勿在正文逐条朗读）");
			sb.AppendLine($"- 战争已持续：{(int)(stance.WarStartDate.ElapsedDaysUntilNow)} 天");
			sb.AppendLine($"- 你方战争进展分：{dm.GetWarProgressScore(myKingdom, enemy).ResultNumber:F0} / 750");
			sb.AppendLine($"- 敌方战争进展分：{dm.GetWarProgressScore(enemy, myKingdom).ResultNumber:F0} / 750");
			sb.AppendLine($"  你方击杀：{stance.GetCasualties(enemy)}，敌方击杀：{stance.GetCasualties(myKingdom)}");
			sb.AppendLine($"  你方占城：{stance.GetSuccessfulTownSieges(myKingdom)}城{stance.GetSuccessfulSieges(myKingdom)-stance.GetSuccessfulTownSieges(myKingdom)}堡");
			sb.AppendLine($"- 你方总战力：{myKingdom.CurrentTotalStrength:F0}，敌方总战力：{enemy.CurrentTotalStrength:F0}");

			float enemyProsperity = enemy.Fiefs.Sum(x => x.Prosperity);
			sb.AppendLine($"- 敌方繁荣度：{enemyProsperity:F0}，合理贡金范围 0 ~ {(int)(enemyProsperity*0.15f*0.35f)} 第纳尔/天");

			int myOtherEnemies = 0; float myOtherStr = 0;
			foreach (Kingdom k in Kingdom.All)
			{ if (k != enemy && !k.IsEliminated && k != myKingdom && FactionManager.IsAtWarAgainstFaction(myKingdom, k)) { myOtherEnemies++; myOtherStr += k.CurrentTotalStrength; } }
			if (myOtherEnemies > 0) sb.AppendLine($"- 多线作战：同时与 {myOtherEnemies} 个敌人交战（总战力 {myOtherStr:F0}）");

			float diff = dm.GetWarProgressScore(myKingdom, enemy).ResultNumber - dm.GetWarProgressScore(enemy, myKingdom).ResultNumber;
			if (diff > 100) sb.AppendLine($"- 【谈判立场】你方明显占优");
			else if (diff < -100) sb.AppendLine($"- 【谈判立场】你方明显劣势");
			else sb.AppendLine($"- 【谈判立场】双方大体持平");
		}

		internal static string BuildDiplomacyPostprocessContext(Hero npc)
		{
			try
			{
				if (npc == null) return "";
				Kingdom npcKingdom = npc.Clan?.Kingdom;
				if (npcKingdom == null) return "";
				if (npc != npcKingdom.RulingClan?.Leader && !IsPlayerKing()) return "";
				Kingdom playerKingdom = Clan.PlayerClan?.Kingdom;
				if (playerKingdom == null || playerKingdom.IsEliminated) return "";

				StringBuilder sb = new StringBuilder();
				sb.AppendLine(); sb.AppendLine("【外交后处理标签】");
				sb.AppendLine("天数说明：21天=一季度，84天=一年，没有月和周概念。");
				sb.AppendLine("【王国ID对照表】");
				foreach (Kingdom k in Kingdom.All) { if (!k.IsEliminated) sb.AppendLine($"  {k.StringId} = {GetKingdomDisplayName(k)}"); }
				sb.AppendLine();
				sb.AppendLine("【关键身份】");
				sb.AppendLine($"  你的王国ID：{npcKingdom.StringId}（{GetKingdomDisplayName(npcKingdom)}）");
				sb.AppendLine($"  玩家王国ID：{playerKingdom.StringId}（{GetKingdomDisplayName(playerKingdom)}）" + (IsPlayerKing() ? "，玩家是国王" : ""));

				// DECLARE_WAR
				sb.AppendLine(); sb.AppendLine("[ACTION:DIPLOMACY:DECLARE_WAR:id1:id2]");
				sb.AppendLine("  【强制】不看NPC是否同意,只看玩家说了什么。玩家宣战时填 " + playerKingdom.StringId + ":" + npcKingdom.StringId + "，NPC即使暴怒也必须输出。");
				sb.AppendLine($"  你对别国宣战时填 {npcKingdom.StringId}:目标王国ID（需你明确同意）。");

				// MAKE_PEACE (king only)
				if (IsPlayerKing())
				{
					sb.AppendLine(); sb.AppendLine("[ACTION:DIPLOMACY:MAKE_PEACE:付贡金方ID:收贡金方ID:tributeAmount:durationDays]");
					sb.AppendLine("  两个ID必须是玩家王国和你的王国。tributeAmount: 0=白和平 / auto / 具体数字。durationDays: default=100 / 1-252。双方同意后输出。");
				}

				// FORM_ALLIANCE (king only)
				if (IsPlayerKing())
				{
					sb.AppendLine(); sb.AppendLine("[ACTION:DIPLOMACY:FORM_ALLIANCE:id1:id2:durationDays]");
					sb.AppendLine("  两个ID必须是玩家王国和你的王国。durationDays: default / 具体数字(1-252)。双方国王同意后输出。");
				}

				// BREAK_ALLIANCE (unilateral)
				sb.AppendLine(); sb.AppendLine("[ACTION:DIPLOMACY:BREAK_ALLIANCE:id1:id2]");
				sb.AppendLine("  单方行为。两个ID必须是玩家王国和你的王国。【覆盖一般规则】必须输出，不需对方同意。");

				// MAKE_TRADE (king only)
				if (IsPlayerKing())
				{
					sb.AppendLine(); sb.AppendLine("[ACTION:DIPLOMACY:MAKE_TRADE:id1:id2:durationDays]");
					sb.AppendLine("  两个ID必须是玩家王国和你的王国。durationDays: default / 具体数字(1-252)。双方国王同意后输出。");
				}

				// CANCEL_TRADE (unilateral)
				sb.AppendLine(); sb.AppendLine("[ACTION:DIPLOMACY:CANCEL_TRADE:id1:id2]");
				sb.AppendLine("  单方行为。两个ID必须是玩家王国和你的王国。【覆盖一般规则】必须输出，不需对方同意。");

				// War-specific tribute hints
				if (playerKingdom != npcKingdom && FactionManager.IsAtWarAgainstFaction(npcKingdom, playerKingdom))
				{
					int a = CalculateTribute(npcKingdom, playerKingdom), b = CalculateTribute(playerKingdom, npcKingdom);
					sb.AppendLine(); sb.AppendLine($"auto贡金：{npcKingdom.StringId}付{a}/天，{playerKingdom.StringId}付{b}/天");
				}
				return sb.ToString().TrimEnd();
			}
			catch (Exception ex) { Logger.Log("DiplomacyBehavior", $"[BuildPostprocess Error] {ex.Message}"); return ""; }
		}

		// ════════════════════════════════════════════════════════ Harmony

		private static void Patch_BuildDiplomacyContext_Postfix(
			Hero targetHero, string input, string extraFact, string cultureIdOverride, bool hasAnyHero,
			CharacterObject targetCharacter, string kingdomIdOverride, int targetAgentIndex,
			bool suppressDynamicRuleAndLore, bool usePrefetchedLoreContext, string prefetchedLoreContext,
			ref MyBehavior.ShoutPromptContext __result)
		{
			try
			{
				if (__result == null) return;
				if ((__result.Extras ?? "").IndexOf("【附加规则:diplomacy】", StringComparison.OrdinalIgnoreCase) < 0) return;
				Hero ctx = targetHero ?? targetCharacter?.HeroObject;
				if (ctx == null) return;
				string ins = BuildDiplomacyInstructionContext(ctx);
				if (!string.IsNullOrWhiteSpace(ins)) __result.Extras = (__result.Extras ?? "") + "\n" + ins;
				string trustIns = BuildDiplomacyRuntimeInstruction(ctx);
				if (!string.IsNullOrWhiteSpace(trustIns)) __result.Extras = (__result.Extras ?? "") + "\n" + trustIns;
			}
			catch (Exception ex) { Logger.Log("DiplomacyBehavior", $"[PatchContext Error] {ex.Message}"); }
		}

		private static string BuildDiplomacyRuntimeInstruction(Hero npc)
		{
			try
			{
				if (npc == null) return "";
				Clan clan = npc.Clan;
				Kingdom kingdom = clan?.Kingdom;
				string playerName = MyBehavior.BuildPlayerPublicDisplayNameForExternal();
				if (string.IsNullOrWhiteSpace(playerName)) playerName = "玩家";
				var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["playerName"] = playerName };

				// Determine state
				string stateKey = "";
				if (kingdom == null) { stateKey = "no_kingdom"; }
				else if (clan.IsUnderMercenaryService) { stateKey = "mercenary"; }
				else if (npc != kingdom.RulingClan?.Leader) { stateKey = "not_king"; }
				else if (!IsPlayerKing()) { stateKey = "player_not_king"; }

				if (!string.IsNullOrWhiteSpace(stateKey))
				{
					string stateTemplate = AIConfigHandler.ResolveRuleRuntimeText("diplomacy", stateKey, forConstraint: false, tokens);
					if (!string.IsNullOrWhiteSpace(stateTemplate)) return stateTemplate;
					if (stateKey == "no_kingdom" || stateKey == "mercenary" || stateKey == "not_king" || stateKey == "player_not_king")
						return "";
				}

				// Trust level - only for alliance/trade negotiations
				if (kingdom != null && !clan.IsUnderMercenaryService && npc == kingdom.RulingClan?.Leader)
				{
					int trust = RewardSystemBehavior.Instance?.GetEffectiveTrust(npc) ?? 0;
					int trustLevelIndex = RewardSystemBehavior.GetTrustLevelIndex(trust);
					string trustTemplate = AIConfigHandler.ResolveRuleRuntimeText("diplomacy", "level_" + trustLevelIndex, forConstraint: false, tokens);
					if (!string.IsNullOrWhiteSpace(trustTemplate)) return trustTemplate;
				}

				return "";
			}
			catch (Exception ex) { Logger.Log("DiplomacyBehavior", $"[RuntimeInstruction Error] {ex.Message}"); return ""; }
		}
	}
}
