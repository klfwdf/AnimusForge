using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace AnimusForge;

internal sealed partial class KingdomCivilWarOwner
{
	private static string FulfillmentText(KingdomCivilWarFactionState faction)
	{
		if (faction.ResolutionNeedsReview) return CivilWarUiText.T("AF_KINGDOM_CW_Panel_StageSettlementError", "Settlement error  ·  state kept; check the log");
		if (faction.Fulfillment.Status == "failed" || faction.Fulfillment.Status == "rejected") return CivilWarUiText.T("AF_KINGDOM_CW_Fulfillment_RetryManual", "Fulfillment failed or the vote was rejected; the faction remains. The ruler may try a new concession after the cooldown.");
		if (faction.Fulfillment.Kind == "independence" && faction.Fulfillment.Choice.Length == 0) return CivilWarUiText.T("AF_KINGDOM_CW_Fulfillment_Choice", "Accepted · choose departure or LLM kingdom founding in the popup");
		return faction.Fulfillment.Kind == "independence"
			? CivilWarUiText.T("AF_KINGDOM_CW_Fulfillment_Founding", "Accepted · awaiting LLM naming and confirmed kingdom independence")
			: CivilWarUiText.T("AF_KINGDOM_CW_Fulfillment_Voting", "Accepted · awaiting both kingdoms' peace votes; peace is not yet confirmed");
	}

	private CivilWarPendingFulfillment BeginFulfillment(CivilWarEffectContext ctx, string kind)
	{
		var f = ctx.Faction;
		string operation = !string.IsNullOrEmpty(f.PendingConcessionOperationId) ? f.PendingConcessionOperationId : f.Id + ":war:fulfillment:" + f.ResolutionOutcomeId;
		if (f.Fulfillment != null)
		{
			if (f.Fulfillment.OperationId != operation || f.Fulfillment.Kind != kind)
				throw new InvalidOperationException("Faction fulfillment identity changed.");
			return f.Fulfillment;
		}
		var pending = new CivilWarPendingFulfillment
		{
			Kind = kind, OperationId = operation, RulerClanId = ctx.Kingdom.RulingClan?.StringId ?? "",
			LeaderClanId = f.LeaderClanId, TargetId = f.TargetId,
			ClanIds = (ctx.Faction.Stage == KingdomCivilWarStage.OpenWar ? ctx.WarClans() : ctx.FactionClans()).Where(x => x != ctx.Kingdom.RulingClan)
				.Select(x => x.StringId).Concat(new[] { f.LeaderClanId }).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
			StructuralWarIds = kind == "independence" && !CivilWarWorld.IsAlive(ctx.RebelKingdom)
				? ctx.Kingdom.FactionsAtWarWith.OfType<Kingdom>().Where(x => x != ctx.Kingdom && !ctx.LeaderClan.FactionsAtWarWith.Contains(x)).Select(x => x.StringId).ToList()
				: new List<string>()
		};
		if (kind == "independence" && ctx.Kingdom.RulingClan != Clan.PlayerClan && !pending.ClanIds.Contains(Clan.PlayerClan?.StringId ?? "")) pending.Choice = "found";
		f.Fulfillment = pending;
		f.DemandLocked = true;
		f.PlayerAnswerPending = false;
		f.WaitingForOtherWar = f.EscalationPending = false;
		return pending;
	}

	private bool FulfillPeaceAgenda(CivilWarEffectContext ctx)
	{
		var p = BeginFulfillment(ctx, "peace");
		if (p.Status == "rejected" || p.Status == "failed")
			throw new CivilWarRetryableEffectException(CivilWarUiText.T("AF_KINGDOM_CW_Fulfillment_PeaceRejected", "The peace agenda was rejected or cancelled; the faction remains."));
		if (p.RulerClanId != ctx.Kingdom.RulingClan?.StringId || p.LeaderClanId != ctx.Faction.LeaderClanId || p.TargetId != ctx.Faction.TargetId)
			throw new InvalidOperationException("Faction peace parties changed.");
		var target = CivilWarWorld.FindKingdom(p.TargetId);
		p.Status = VoteDealBehavior.GetOrSubmitFactionPeaceAgenda(p.OperationId, ctx.Faction.Id, ctx.Kingdom, target, ctx.Kingdom.RulingClan, out string reason);
		if (p.Status == "completed" && CivilWarWorld.IsAlive(target) && !ctx.Kingdom.IsAtWarWith(target)) return true;
		if (p.Status != "pending") ctx.Faction.GovernanceUntilDay = CivilWarWorld.CurrentDay() + CivilWarPoliticalRules.ActionDays;
		if (p.Status != "pending")
			throw new CivilWarRetryableEffectException(CivilWarUiText.T("AF_KINGDOM_CW_Fulfillment_PeaceRejected", "The peace agenda was rejected or cancelled; the faction remains.") + " " + reason);
		return false;
	}

	private bool FulfillIndependence(CivilWarEffectContext ctx)
	{
		var p = BeginFulfillment(ctx, "independence");
		if (p.Status == "failed") throw new CivilWarRetryableEffectException(ctx.Faction.ResolutionError);
		if (p.LeaderClanId != ctx.Faction.LeaderClanId || p.RulerClanId != ctx.Kingdom.RulingClan?.StringId)
			throw new InvalidOperationException("Faction independence parties changed.");
		if (string.IsNullOrEmpty(p.KingdomId) && CivilWarWorld.IsAlive(ctx.RebelKingdom)) { p.KingdomId = ctx.RebelKingdom.StringId; p.Choice = "found"; }
		if (p.Choice == "leave")
		{
			foreach (string id in p.ClanIds)
			{
				var clan = CivilWarWorld.FindClan(id);
				if (clan == null || clan.IsEliminated) throw new InvalidOperationException("Independence participant no longer exists: " + id);
				if (clan.Kingdom == null) continue;
				if (clan.Kingdom != ctx.Kingdom || clan == ctx.Kingdom.RulingClan) throw new InvalidOperationException("Independence participant allegiance changed: " + id);
				try { Host.LeaveKingdomKeepingFiefs(clan); }
				catch (Exception ex)
				{
					Logger.Log("KingdomCivilWar", "[WARN] authorized departure interrupted clan=" + id + " error=" + ex.Message);
				}
				if (clan.Kingdom != null) throw new CivilWarRetryableEffectException("Granted clan departure is not confirmed: " + id);
			}
			Host.MakeClansPeaceful(p.ClanIds.Select(CivilWarWorld.FindClan), "civil_war_granted_departure");
			if (p.ClanIds.Select(CivilWarWorld.FindClan).Any(c => c != null && c.FactionsAtWarWith.Contains(ctx.Kingdom)))
				throw new CivilWarRetryableEffectException("Granted clan departure peace is not confirmed.");
			p.Status = "completed";
			return true;
		}
		Kingdom independent = CivilWarWorld.FindKingdom(p.KingdomId);
		if (!string.IsNullOrEmpty(p.KingdomId) && !CivilWarWorld.IsAlive(independent))
			throw new InvalidOperationException("The authorized independent kingdom no longer exists.");
		if (independent == null)
		{
			if (!IsIndependenceRequestCurrent(ctx.Faction.Id, p.OperationId, ctx.Kingdom.StringId, p.LeaderClanId))
				throw new InvalidOperationException("Faction independence authorization is no longer valid.");
			if (_activeNamingRequests.Add(ctx.Faction.Id))
			{
				Host.QueueRebellion(ctx.Kingdom, ctx.LeaderClan, p.ClanIds.Select(CivilWarWorld.FindClan).Where(x => x != null && x != ctx.LeaderClan).ToList(), ctx.Faction.Id, true);
			}
			return false;
		}
		ctx.RebelKingdom = independent;
		ctx.Faction.RebelKingdomId = independent.StringId;
		foreach (string id in p.ClanIds)
		{
			Clan clan = CivilWarWorld.FindClan(id);
			if (clan == null || clan.IsEliminated || clan.Leader?.IsAlive != true)
				throw new InvalidOperationException("An authorized independence participant is no longer eligible: " + id);
			if (clan.Kingdom == independent) continue;
			if (clan.Kingdom != ctx.Kingdom || clan == ctx.Kingdom.RulingClan || !CivilWarWorld.IsPoliticalClan(clan))
				throw new InvalidOperationException("An authorized independence participant changed allegiance: " + id);
			ChangeKingdomAction.ApplyByJoinToKingdomByDefection(clan, ctx.Kingdom, independent, default(CampaignTime), showNotification: true);
			if (clan.Kingdom != independent) throw new CivilWarRetryableEffectException("Independence participant transfer is not confirmed: " + id);
		}
		foreach (string id in p.StructuralWarIds)
		{
			var inheritedEnemy = CivilWarWorld.FindKingdom(id);
			if (CivilWarWorld.IsAlive(inheritedEnemy)) CivilWarEffects.RequirePeace(independent, inheritedEnemy, "civil_war_independence_inherited_war");
		}
		CivilWarEffects.RequirePeace(ctx.Kingdom, independent, "civil_war_authorized_independence");
		p.Status = "completed";
		return true;
	}

	internal string GetIndependenceChoice(string factionId, string operationId)
	{
		var p = FindFaction(factionId, out _)?.Fulfillment;
		return p?.Kind == "independence" && p.OperationId == operationId ? p.Choice : "";
	}

	internal bool AnswerIndependenceChoice(string factionId, string operationId, bool found)
	{
		var f = FindFaction(factionId, out var s); var p = f?.Fulfillment;
		if (p == null || p.Choice.Length > 0 || !IsIndependenceRequestCurrent(factionId, operationId, s.KingdomId, f.LeaderClanId)) return false;
		p.Choice = found ? "found" : "leave";
		if (!found) _activeNamingRequests.Remove(factionId);
		NotifyPoliticalChange(CivilWarWorld.FindKingdom(s.KingdomId), "independence_choice"); Revision++;
		return true;
	}

	internal bool IsPeacefulIndependenceRequest(string factionId)
	{
		var f = FindFaction(factionId, out _);
		return DuelSettings.IsCivilWarFactionsEnabled() && f?.Fulfillment?.Kind == "independence" && f.Fulfillment.Status != "failed" && !f.ResolutionNeedsReview;
	}

	internal string GetIndependenceOperationId(string factionId)
		=> IsPeacefulIndependenceRequest(factionId) ? FindFaction(factionId, out _).Fulfillment.OperationId : "";

	internal bool IsIndependenceRequestCurrent(string factionId, string operationId, string kingdomId, string leaderClanId)
	{
		var f = FindFaction(factionId, out var s);
		var p = f?.Fulfillment;
		if (!IsPeacefulIndependenceRequest(factionId) || p.OperationId != operationId || p.Choice == "leave" || string.IsNullOrEmpty(operationId)
			|| s.KingdomId != kingdomId || p.LeaderClanId != leaderClanId || f.LeaderClanId != leaderClanId || !string.IsNullOrEmpty(p.KingdomId)) return false;
		var home = CivilWarWorld.FindKingdom(kingdomId);
		var leader = CivilWarWorld.FindClan(leaderClanId);
		if (!CivilWarWorld.IsAlive(home) || home.RulingClan?.StringId != p.RulerClanId || leader == null || leader.IsEliminated || leader.Leader?.IsAlive != true || leader == home.RulingClan) return false;
		// A saved, validated name allows resuming a structural leave that completed before an event threw.
		if (leader.Kingdom != home && !(leader.Kingdom == null && !string.IsNullOrEmpty(p.FormalName))) return false;
		return p.ClanIds != null && p.ClanIds.All(id =>
		{
			var c = CivilWarWorld.FindClan(id);
			return c != null && !c.IsEliminated && c.Leader?.IsAlive == true && CivilWarWorld.IsPoliticalClan(c)
				&& (c.Kingdom == home || c == leader && c.Kingdom == null && !string.IsNullOrEmpty(p.FormalName));
		});
	}

	internal void NoteIndependenceNamingReady(string factionId, string operationId, string formal, string shortName, string lore)
	{
		var f = FindFaction(factionId, out var s);
		if (f == null || f.Fulfillment?.Choice != "found" || !IsIndependenceRequestCurrent(factionId, operationId, s.KingdomId, f.LeaderClanId)) return;
		f.Fulfillment.FormalName = formal; f.Fulfillment.ShortName = shortName; f.Fulfillment.Lore = lore;
		Revision++;
	}

	internal bool GetIndependenceNaming(string factionId, string operationId, out string formal, out string shortName, out string lore)
	{
		var p = FindFaction(factionId, out _)?.Fulfillment;
		formal = p?.FormalName ?? ""; shortName = p?.ShortName ?? ""; lore = p?.Lore ?? "";
		return p?.Kind == "independence" && p.OperationId == operationId && formal.Length > 0 && shortName.Length > 0 && lore.Length > 0;
	}

	internal void NoteIndependentKingdomCreated(string factionId, string operationId, Kingdom kingdom)
	{
		var f = FindFaction(factionId, out var s); var p = f?.Fulfillment;
		if (p?.Kind != "independence" || p.Choice != "found" || p.OperationId != operationId || !CivilWarWorld.IsAlive(kingdom)
			|| kingdom.StringId == s.KingdomId || CivilWarWorld.FindClan(p.LeaderClanId)?.Kingdom != kingdom) return;
		if (!string.IsNullOrEmpty(p.KingdomId) && p.KingdomId != kingdom.StringId) return;
		p.KingdomId = kingdom.StringId; f.RebelKingdomId = kingdom.StringId;
		_activeNamingRequests.Remove(factionId);
		NotifyPoliticalChange(CivilWarWorld.FindKingdom(s.KingdomId), "independence_created");
		Revision++;
	}

	private bool AdvancePendingFulfillment(Kingdom k, KingdomCivilWarKingdomState s, KingdomCivilWarFactionState f, int week, CivilWarTuning tuning)
	{
		if (f.Fulfillment == null) return false;
		if (f.Fulfillment.Status == "failed" || f.Fulfillment.Status == "rejected")
		{
			if (!CivilWarWorld.IsPlayerRuled(k) && CivilWarWorld.CurrentDay() >= f.GovernanceUntilDay)
			{
				// The AI ruler must explicitly choose a new operation, at most once per political cooldown.
				TryAiGovernance(k, s, f);
				f.GovernanceUntilDay = Math.Max(f.GovernanceUntilDay, CivilWarWorld.CurrentDay() + CivilWarPoliticalRules.ActionDays);
			}
			return true;
		}
		if (string.IsNullOrEmpty(f.PendingConcessionOperationId)) ResolveWar(k, s, f, week, tuning, HostStability);
		else
		{
			var r = new AnimusForge.Refactor.Modules.CivilWarActionRequest { OperationId = f.PendingConcessionOperationId, KingdomId = k.StringId, FactionId = f.Id, Action = AnimusForge.Refactor.Modules.CivilWarAction.Concede };
			var result = ExecutePoliticalAction(r, k.RulingClan, k, s, f, new AnimusForge.Refactor.Modules.CivilWarActionQuote { Allowed = true }, false);
			if (result.Status == AnimusForge.Refactor.Modules.CivilWarActionStatus.Applied)
			{
				PublishPoliticalResult(k, s, f, k.RulingClan, r.OperationId + ":settled", result.Message);
				if (_storage.Operations.TryGetValue(r.OperationId, out var receipt)) { receipt.Status = (int)result.Status; receipt.Message = result.Message; receipt.InProgress = false; receipt.FactsWritten = true; }
			}
		}
		return true;
	}
}
