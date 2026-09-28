using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Domain;
using AnimusForge.Refactor.Modules;
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
		private string TryExecuteDeclareWar(Hero npc, string payload)
		{
			var source = new DiplomacyOralDeclareWarSource(npc);
			return DiplomacyOralDeclareWarApplication.Execute(ref source, payload);
		}

		private string TryExecuteIndependentClanPeace(Hero npc, string payload)
		{
			var source = new DiplomacyOralIndependentPeaceSource(npc);
			return DiplomacyOralIndependentPeaceApplication.Execute(ref source, payload);
		}

		// ════════════════════════════════════════════════════════ MAKE_PEACE

		private string TryExecuteMakePeace(Hero npc, string payload)
		{
			var source = new DiplomacyOralMakePeaceSource(npc);
			return DiplomacyOralMakePeaceApplication.Execute(ref source, payload);
		}

		// ════════════════════════════════════════════════════════ FORM_ALLIANCE

		private string TryExecuteFormAlliance(Hero npc, string payload)
		{
			Kingdom playerKingdom = Clan.PlayerClan?.Kingdom;
			Kingdom npcKingdom = npc.Clan?.Kingdom;
			WorldDiplomacyOralFormAllianceResolution resolution = WorldDiplomacyOralFormAllianceRules.ResolveCommand(
				payload,
				playerKingdomExists: playerKingdom != null,
				playerKingdomId: playerKingdom?.StringId,
				playerKingdomIsEliminated: playerKingdom?.IsEliminated == true,
				playerIsRuler: IsPlayerKing(),
				npcKingdomExists: npcKingdom != null,
				npcKingdomId: npcKingdom?.StringId,
				npcSpeakerHeroId: npc?.StringId,
				npcSpeakerIsRuler: IsNpcKing(npc, npcKingdom));
			if (!resolution.IsReady)
			{
				Logger.Log("DiplomacyBehavior", $"[FormAlliance] Rejected status={resolution.Status}");
				return "";
			}

			WorldDiplomacyFormAllianceExecutionReceipt receipt =
				FormAllianceCommandFacade.Execute(resolution.Command);
			if (!receipt.IsApplied)
			{
				Logger.Log("DiplomacyBehavior", $"[FormAlliance] Rejected status={receipt.Status} code={receipt.ErrorCode}");
				return "";
			}

			playerKingdom = ResolveKingdom(receipt.PlayerKingdomId);
			npcKingdom = ResolveKingdom(receipt.NpcKingdomId);
			if (playerKingdom == null || npcKingdom == null)
			{
				Logger.Log("DiplomacyBehavior", "[FormAlliance] Applied but receipt endpoints are unavailable");
				return "";
			}
			Logger.Log("DiplomacyBehavior", $"[FormAlliance] {playerKingdom.StringId} <-> {npcKingdom.StringId}");
			WorldDiplomacyBehavior.NotifyExternalDiplomacyResolved("accept_alliance", playerKingdom, npcKingdom, "面对面口头外交达成");
			return "";
		}

		// ════════════════════════════════════════════════════════ BREAK_ALLIANCE

		private string TryExecuteBreakAlliance(Hero npc, string payload)
		{
			Kingdom playerKingdom = Clan.PlayerClan?.Kingdom;
			Kingdom npcKingdom = npc.Clan?.Kingdom;
			WorldDiplomacyOralBreakAllianceResolution resolution =
				WorldDiplomacyOralBreakAllianceRules.ResolveCommand(
					payload,
					playerKingdomExists: playerKingdom != null,
					playerKingdomId: playerKingdom?.StringId,
					playerKingdomIsEliminated: playerKingdom?.IsEliminated == true,
					npcKingdomExists: npcKingdom != null,
					npcKingdomId: npcKingdom?.StringId,
					npcSpeakerHeroId: npc?.StringId);
			if (!resolution.IsReady)
			{
				Logger.Log("DiplomacyBehavior", $"[BreakAlliance] Rejected status={resolution.Status}");
				return "";
			}

			WorldDiplomacyBreakAllianceExecutionReceipt receipt =
				BreakAllianceCommandFacade.Execute(resolution.Command);
			if (!receipt.IsApplied)
			{
				Logger.Log("DiplomacyBehavior", $"[BreakAlliance] Rejected status={receipt.Status} code={receipt.ErrorCode}");
				return "";
			}

			playerKingdom = ResolveKingdom(receipt.PlayerKingdomId);
			npcKingdom = ResolveKingdom(receipt.NpcKingdomId);
			if (playerKingdom == null || npcKingdom == null)
			{
				Logger.Log("DiplomacyBehavior", "[BreakAlliance] Applied but receipt endpoints are unavailable");
				return "";
			}
			Logger.Log("DiplomacyBehavior", $"[BreakAlliance] {playerKingdom.StringId} <-> {npcKingdom.StringId}");
			WorldDiplomacyBehavior.NotifyExternalDiplomacyResolved("break_alliance", playerKingdom, npcKingdom, "面对面口头外交达成");
			return "";
		}

		// ════════════════════════════════════════════════════════ MAKE_TRADE

		private string TryExecuteMakeTrade(Hero npc, string payload)
		{
			Kingdom playerKingdom = Clan.PlayerClan?.Kingdom;
			Kingdom npcKingdom = npc.Clan?.Kingdom;
			WorldDiplomacyOralMakeTradeResolution resolution =
				WorldDiplomacyOralMakeTradeRules.ResolveCommand(
					payload,
					playerKingdomExists: playerKingdom != null,
					playerKingdomId: playerKingdom?.StringId,
					playerKingdomIsEliminated: playerKingdom?.IsEliminated == true,
					playerIsRuler: IsPlayerKing(),
					npcKingdomExists: npcKingdom != null,
					npcKingdomId: npcKingdom?.StringId,
					npcSpeakerHeroId: npc?.StringId,
					npcSpeakerIsRuler: IsNpcKing(npc, npcKingdom));
			if (!resolution.IsReady)
			{
				Logger.Log("DiplomacyBehavior", "[MakeTrade] Rejected status=" + resolution.Status);
				return "";
			}

			WorldDiplomacyMakeTradeExecutionReceipt receipt =
				MakeTradeCommandFacade.Execute(resolution.Command);
			if (!receipt.IsApplied)
			{
				Logger.Log("DiplomacyBehavior", "[MakeTrade] Rejected status=" + receipt.Status
					+ " code=" + receipt.ErrorCode);
				return "";
			}

			playerKingdom = ResolveKingdom(receipt.PlayerKingdomId);
			npcKingdom = ResolveKingdom(receipt.NpcKingdomId);
			if (playerKingdom == null || npcKingdom == null)
			{
				Logger.Log("DiplomacyBehavior", "[MakeTrade] Applied but receipt endpoints are unavailable");
				return "";
			}
			Logger.Log("DiplomacyBehavior", $"[MakeTrade] {playerKingdom.StringId} <-> {npcKingdom.StringId} days={receipt.AppliedDurationDays}");
			WorldDiplomacyBehavior.NotifyExternalDiplomacyResolved("accept_trade", playerKingdom, npcKingdom, "面对面口头外交达成");
			return "";
		}

		// ════════════════════════════════════════════════════════ CANCEL_TRADE

		private string TryExecuteCancelTrade(Hero npc, string payload)
		{
			Kingdom playerKingdom = Clan.PlayerClan?.Kingdom;
			Kingdom npcKingdom = npc.Clan?.Kingdom;
			WorldDiplomacyOralCancelTradeResolution resolution =
				WorldDiplomacyOralCancelTradeRules.ResolveCommand(
					payload,
					playerKingdomExists: playerKingdom != null,
					playerKingdomId: playerKingdom?.StringId,
					playerKingdomIsEliminated: playerKingdom?.IsEliminated == true,
					npcKingdomExists: npcKingdom != null,
					npcKingdomId: npcKingdom?.StringId,
					npcSpeakerHeroId: npc?.StringId);
			if (!resolution.IsReady)
			{
				Logger.Log("DiplomacyBehavior", "[CancelTrade] Rejected status=" + resolution.Status);
				return "";
			}

			WorldDiplomacyCancelTradeExecutionReceipt receipt =
				CancelTradeCommandFacade.Execute(resolution.Command);
			if (!receipt.IsApplied)
			{
				Logger.Log("DiplomacyBehavior", "[CancelTrade] Rejected status=" + receipt.Status
					+ " code=" + receipt.ErrorCode);
				return "";
			}

			playerKingdom = ResolveKingdom(receipt.PlayerKingdomId, includeEliminated: true);
			npcKingdom = ResolveKingdom(receipt.NpcKingdomId, includeEliminated: true);
			if (playerKingdom == null || npcKingdom == null)
			{
				Logger.Log("DiplomacyBehavior", "[CancelTrade] Applied but receipt endpoints are unavailable");
				return "";
			}
			Logger.Log("DiplomacyBehavior", $"[CancelTrade] {playerKingdom.StringId} <-> {npcKingdom.StringId}");
			WorldDiplomacyBehavior.NotifyExternalDiplomacyResolved("cancel_trade", playerKingdom, npcKingdom, "面对面口头外交达成");
			return "";
		}

		// ════════════════════════════════════════════════════════════════════
		//  Shared helpers
		// ════════════════════════════════════════════════════════════════════

		internal static bool TryBuildTributePowerContext(Kingdom payer, Kingdom receiver, out AfTributePowerContext context)
		{
			var source = new DiplomacyTributePowerSource(payer, receiver);
			return DiplomacyTributePowerApplication.TryBuild(ref source, out context);
		}

		private static int CalculateTribute(Kingdom payer, Kingdom receiver)
		{
			return TryBuildTributePowerContext(payer, receiver, out AfTributePowerContext context)
				? context.CalculatedTribute
				: 0;
		}

		internal static Kingdom ResolveKingdom(string id, bool includeEliminated = false)
		{
			if (string.IsNullOrWhiteSpace(id)) return null;
			foreach (Kingdom k in Kingdom.All)
			{ if ((includeEliminated || !k.IsEliminated) && (string.Equals(k.StringId, id, StringComparison.OrdinalIgnoreCase) || string.Equals(k.Name?.ToString() ?? "", id, StringComparison.OrdinalIgnoreCase))) return k; }
			return null;
		}

		internal static bool IsPlayerKing()
		{
			Kingdom pk = Clan.PlayerClan?.Kingdom;
			return pk != null && Hero.MainHero == pk.RulingClan?.Leader;
		}

		private static bool TryResolveIndependentClanPeaceContext(Hero npc, out Clan playerClan, out Kingdom targetKingdom)
		{
			playerClan = null;
			targetKingdom = null;
			var source = new DiplomacyIndependentPeaceSource(npc);
			if (!DiplomacyIndependentPeaceApplication.CanUse(ref source)) return false;
			playerClan = source.PlayerClan;
			targetKingdom = source.TargetKingdom;
			return true;
		}

		internal static bool CanUseIndependentClanPeaceForExternal(Hero targetHero, CharacterObject targetCharacter = null)
		{
			var source = new DiplomacyIndependentPeaceSource(targetHero ?? targetCharacter?.HeroObject);
			return DiplomacyIndependentPeaceApplication.CanUse(ref source);
		}

		internal static bool IsIndependentClanPeacePostprocessTag(string tag)
		{
			return DiplomacyConversationEligibilityApplication.IsIndependentClanPeaceTag(tag);
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

		internal static bool IsNpcKing(Hero npc, Kingdom npcKingdom)
		{
			return npc != null && npcKingdom != null && npc == npcKingdom.RulingClan?.Leader;
		}

		internal static bool CanInjectDiplomacyRuleForExternal(Hero targetHero, CharacterObject targetCharacter = null)
		{
			try
			{
				return DiplomacyConversationEligibilityApplication.CanInject(
					CaptureEligibilitySnapshot(targetHero ?? targetCharacter?.HeroObject));
			}
			catch
			{
				return false;
			}
		}

		internal static bool CanUseDiplomacyActionPostprocessForExternal(Hero targetHero, CharacterObject targetCharacter = null)
		{
			try
			{
				return DiplomacyConversationEligibilityApplication.CanUseAction(
					CaptureEligibilitySnapshot(targetHero ?? targetCharacter?.HeroObject));
			}
			catch { return false; }
		}

		internal static bool CanUseFullDiplomacyActionPostprocessForExternal(Hero targetHero, CharacterObject targetCharacter = null)
		{
			try
			{
				return DiplomacyConversationEligibilityApplication.CanUseFull(
					CaptureEligibilitySnapshot(targetHero ?? targetCharacter?.HeroObject));
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
				return DiplomacyConversationEligibilityApplication.CanUseNpcDeclareWar(
					CaptureEligibilitySnapshot(targetHero ?? targetCharacter?.HeroObject));
			}
			catch
			{
				return false;
			}
		}

		internal static DiplomacyConversationEligibilitySnapshot CaptureEligibilitySnapshot(Hero npc)
		{
			Kingdom npcKingdom = npc?.Clan?.Kingdom;
			bool npcIsDead;
			try { npcIsDead = npc?.IsDead == true; }
			catch { npcIsDead = true; }
			Kingdom playerKingdom = null;
			bool playerEliminated = false;
			bool playerIsRuler = false;
			// Player context must not veto NPC-only rule and war gates.
			try
			{
				playerKingdom = Clan.PlayerClan?.Kingdom;
				playerEliminated = playerKingdom?.IsEliminated == true;
				playerIsRuler = IsPlayerKing();
			}
			catch { playerKingdom = null; }
			return new DiplomacyConversationEligibilitySnapshot(
				npc != null, npc != null && npc == Hero.MainHero, npcIsDead,
				npcKingdom != null, npcKingdom?.IsEliminated == true, IsNpcKing(npc, npcKingdom),
				playerKingdom != null, playerEliminated,
				playerKingdom != npcKingdom, playerIsRuler);
		}

		private static string GetKingdomDisplayName(Kingdom k)
		{ return k?.Name?.ToString() ?? k?.StringId ?? "未知王国"; }
	}
}
