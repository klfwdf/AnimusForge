using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.Refactor.Modules;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace AnimusForge;

internal sealed partial class KingdomCivilWarOwner
{
	private long _revision;
	private bool _hasPoliticalResponse;
	internal event Action StateChanged;
	internal long Revision { get => _revision; private set { _revision = value; StateChanged?.Invoke(); } }
	private static bool PoliticalClan(Clan c, Kingdom k) => k != null && CivilWarWorld.IsPoliticalClan(c)
        && c.Kingdom == k && c.Leader != null && c.Leader.IsAlive;
	private static bool IsGovernance(CivilWarAction a) => a == CivilWarAction.Suppress || a == CivilWarAction.Negotiate || a == CivilWarAction.Concede || a == CivilWarAction.ForceDissolve;
	private static KingdomCivilWarFactionState ActionFaction(CivilWarActionRequest r, KingdomCivilWarKingdomState s, Clan actor)
		=> r.Action == CivilWarAction.Leave ? FactionOfClan(s, actor) : r.Action == CivilWarAction.JoinCrown || r.Action == CivilWarAction.Found ? null : s?.Factions.FirstOrDefault(x => x.Id == r.FactionId);
	internal bool TryTakePoliticalResponse(out CivilWarActionRequest request, out string text)
	{
		request = null; text = "";
		if (!_hasPoliticalResponse || !DuelSettings.IsCivilWarFactionsEnabled()) return false;
		foreach (var s in _storage.Kingdoms.Values) foreach (var f in s.Factions)
		{
			var p = f.PendingResponse;
			if (p == null || p.Prompted || p.Accepted || f.LeaderClanId != Clan.PlayerClan?.StringId || f.ResolutionNeedsReview) continue;
			p.Prompted = true;
			request = new CivilWarActionRequest { OperationId = Guid.NewGuid().ToString("N"), KingdomId = s.KingdomId, FactionId = f.Id, Action = CivilWarAction.Respond, Version = f.Version };
			text = p.Dissolve ? CivilWarUiText.T("AF_KINGDOM_CW_Action_ResponseDissolve", "The crown orders the faction dissolved. Obey and it dissolves; defy and it rises in rebellion.")
				: CivilWarUiText.T("AF_KINGDOM_CW_Action_ResponseOffer", "The crown offers {A} in exchange for withdrawing the demand; accepting dissolves the faction.",
					("A", p.Gold > 0
						? CivilWarUiText.T("AF_KINGDOM_CW_Action_ResponseOfferGold", "{A} gold", ("A", p.Gold.ToString()))
						: CivilWarUiText.T("AF_KINGDOM_CW_Action_ResponseOfferInfluence", "{A} influence", ("A", p.Influence.ToString()))));
			text += CivilWarUiText.T("AF_KINGDOM_CW_Action_ResponseDeadline", "\nReply deadline: {A}; missing it counts as {B}.", ("A", CivilWarUiText.Date(p.DeadlineDay)),
				("B", p.Dissolve ? CivilWarUiText.T("AF_KINGDOM_CW_Action_ResponseDeadlineDefy", "defiance") : CivilWarUiText.T("AF_KINGDOM_CW_Action_ResponseDeadlineRefuse", "refusal")));
			return true;
		}
		_hasPoliticalResponse = false; return false;
	}

	internal List<CivilWarFoundingOption> GetFoundingOptions(string kingdomId, Clan actor)
	{
		var result = new List<CivilWarFoundingOption>();
		Kingdom kingdom = CivilWarWorld.FindKingdom(kingdomId);
		if (!PoliticalClan(actor, kingdom) || actor == kingdom.RulingClan) return result;
		var state = Find(kingdom);
		foreach (var demand in CivilWarCatalog.ValidDemands)
		{
			if (CivilWarWorld.FortificationCount(actor) < demand.MinFortifications || actor != Clan.PlayerClan && state?.Factions.Any(x => x.DemandId == demand.Id) == true) continue;
			if (demand.Target == CivilWarDemandTarget.None) result.Add(new CivilWarFoundingOption { DemandId = demand.Id, Text = CivilWarUiText.FoundingOptionText(demand, "", demand.Text) });
			else if (demand.Target == CivilWarDemandTarget.ImposedPolicy)
			{
				foreach (var policy in kingdom.ActivePolicies) result.Add(new CivilWarFoundingOption { DemandId = demand.Id, TargetId = policy.StringId, Text = CivilWarUiText.FoundingOptionText(demand, policy.Name?.ToString(), FormatDemand(demand, policy.Name.ToString())) });
			}
			else foreach (var target in kingdom.FactionsAtWarWith.OfType<Kingdom>().Where(x => !x.IsEliminated && !IsCivilWarPair(kingdom, x)))
				result.Add(new CivilWarFoundingOption { DemandId = demand.Id, TargetId = target.StringId, Text = CivilWarUiText.FoundingOptionText(demand, CivilWarWorld.KingdomName(target), FormatDemand(demand, CivilWarWorld.KingdomName(target))) });
		}
		return result;
	}

	internal CivilWarActionQuote Quote(CivilWarActionRequest r, Clan actor)
	{
		var q = new CivilWarActionQuote();
		if (r == null) { q.Reason = CivilWarUiText.T("AF_KINGDOM_CW_Action_NoOperation", "No such operation."); return q; }
		Kingdom k = CivilWarWorld.FindKingdom(r.KingdomId);
		var s = Find(k);
		var f = ActionFaction(r, s, actor);
		q.Version = f?.Version ?? 0;
		if (!DuelSettings.IsCivilWarFactionsEnabled() || !CivilWarWorld.IsAlive(k)) q.Reason = CivilWarUiText.T("AF_KINGDOM_CW_Action_Disabled", "Civil war factions are disabled, or the kingdom is no longer valid.");
		else if (CivilWarWorld.IsPlayerRuled(k) && !DuelSettings.IsCivilWarPlayerKingdomFactionsAllowed()) q.Reason = CivilWarUiText.T("AF_KINGDOM_CW_Action_PlayerKingdomDisabled", "Player-kingdom factions are disabled in settings.");
		else if (actor?.Leader == null || !actor.Leader.IsAlive) q.Reason = CivilWarUiText.T("AF_KINGDOM_CW_Action_ClanLeaderIneligible", "The clan leader is not eligible.");
		else if (r.Version >= 0 && r.Version != q.Version) q.Reason = CivilWarUiText.T("AF_KINGDOM_CW_Action_StateChanged", "The faction state changed; please confirm again.");
		else if (f?.ResolutionNeedsReview == true) q.Reason = CivilWarUiText.T("AF_KINGDOM_CW_Action_PreviousOperationPendingReview", "A previous operation is still awaiting review; no further operation is possible yet.");
		else if (r.Action == CivilWarAction.Respond)
		{
			if (f?.PendingResponse == null || f.LeaderClanId != actor.StringId || f.PendingResponse.LeaderClanId != actor.StringId) q.Reason = CivilWarUiText.T("AF_KINGDOM_CW_Action_NothingToRespond", "There is nothing for this clan to respond to.");
			else if (IsPreWar(f) && !PoliticalClan(actor, k) || f.Stage == KingdomCivilWarStage.OpenWar && actor.Kingdom?.StringId != f.RebelKingdomId) q.Reason = CivilWarUiText.T("AF_KINGDOM_CW_Action_ClanAllegianceChanged", "The clan's allegiance changed; the original agreement can no longer be answered.");
			else if (f.PendingResponse.Accepted) q.Reason = CivilWarUiText.T("AF_KINGDOM_CW_Action_AgreementAlreadyAccepted", "The agreement was accepted and is settling; it cannot be answered again or withdrawn. Wait for the safe retry.");
			else if (r.Accept == false && f.PendingResponse.Dissolve && actor == Clan.PlayerClan && r.OperationId != f.PendingResponse.OperationId + ":timeout" && !PlayerDetonationAllowed(k, actor, out string responseReason)) q.Reason = responseReason;
			else if (CivilWarWorld.CurrentDay() > f.PendingResponse.DeadlineDay && (r.Accept || r.OperationId != f.PendingResponse.OperationId + ":timeout")) q.Reason = CivilWarUiText.T("AF_KINGDOM_CW_Action_ResponseDeadlinePassed", "The response deadline has passed.");
			q.Consequence = f?.PendingResponse?.Dissolve == true ? CivilWarUiText.T("AF_KINGDOM_CW_Action_RespondDissolveConsequence", "Obedience dissolves the faction; defiance starts a rebellion.") : CivilWarUiText.T("AF_KINGDOM_CW_Action_RespondAcceptConsequence", "Accepting compensation withdraws the demand and dissolves the faction.");
		}
		else if (IsGovernance(r.Action))
		{
			if (actor != k.RulingClan || actor.Kingdom != k) q.Reason = CivilWarUiText.T("AF_KINGDOM_CW_Action_RulingClanOnly", "Only the ruling clan of the original kingdom can do this.");
			else if (f == null) q.Reason = CivilWarUiText.T("AF_KINGDOM_CW_Action_SelectExistingFaction", "Select a faction that still exists.");
			else if (CivilWarWorld.FindClan(f.LeaderClanId)?.Leader?.IsAlive != true) q.Reason = CivilWarUiText.T("AF_KINGDOM_CW_Action_FactionLeaderChanged", "The faction leader changed; wait for the succession to resolve.");
			else if (f.PendingResponse != null) q.Reason = CivilWarUiText.T("AF_KINGDOM_CW_Action_AwaitingFactionResponse", "Waiting for the faction to respond.");
			else if (f.Stage == KingdomCivilWarStage.OpenWar && (r.Action == CivilWarAction.Suppress || r.Action == CivilWarAction.ForceDissolve)) q.Reason = CivilWarUiText.T("AF_KINGDOM_CW_Action_NoSuppressAfterFounding", "Once the kingdom-founding request has started, the faction cannot be suppressed or administratively dissolved.");
			else if (f.Stage == KingdomCivilWarStage.OpenWar && string.IsNullOrWhiteSpace(f.RebelKingdomId)) q.Reason = CivilWarUiText.T("AF_KINGDOM_CW_Action_RebelsFoundingKingdom", "The rebels are founding their kingdom; wait for it to finish.");
			else if (r.Action != CivilWarAction.Concede && CivilWarWorld.CurrentDay() < f.GovernanceUntilDay) { q.CooldownUntilDay = f.GovernanceUntilDay; q.Reason = CivilWarUiText.T("AF_KINGDOM_CW_Action_GovernanceCooldown", "Political actions are on cooldown until {A}.", ("A", CivilWarUiText.Date(f.GovernanceUntilDay))); }
			if (r.Action == CivilWarAction.Suppress) { q.Influence = CivilWarPoliticalRules.SuppressCost; q.Consequence = CivilWarUiText.T("AF_KINGDOM_CW_Action_SuppressConsequence", "Members lose up to 20 influence; the leader takes +7.5 grievance and the rest +4; −5 relation with the king. The first suppression delays the deadline by 7 days."); }
			if (r.Action == CivilWarAction.ForceDissolve)
			{
				q.Influence = CivilWarPoliticalRules.DissolveCost; q.Consequence = CivilWarUiText.T("AF_KINGDOM_CW_Action_ForceDissolveConsequence", "Members take +7.5 grievance and −10 relation with the king; obedience dissolves the faction, defiance starts a rebellion.");
				if (q.Reason.Length == 0 && !CanStartPoliticalWar(k, s, f, IsPlayerLed(f))) q.Reason = CivilWarUiText.T("AF_KINGDOM_CW_Action_NoDissolveWarBlocked", "The domestic/world war cap or player-kingdom immunity blocks a defiant rebellion, so the dissolution order cannot be issued.");
			}
			if (r.Action == CivilWarAction.Negotiate)
			{
				if (r.OfferTier < 1 || r.OfferTier > 3) q.Reason = CivilWarUiText.T("AF_KINGDOM_CW_Action_InvalidOfferTier", "Invalid compensation tier.");
				else if (r.OfferInfluence) q.Influence = CompensationOffer(k, s, f, r.OfferTier, true);
				else q.Gold = CompensationOffer(k, s, f, r.OfferTier, false);
				q.Consequence = CivilWarUiText.T("AF_KINGDOM_CW_Action_NegotiateConsequence", "Accepting transfers the compensation, withdraws the demand, and dissolves the faction; refusing costs nothing but consumes the 7-day cooldown. During a war, make peace first and return home.");
			}
			if (r.Action == CivilWarAction.Concede) q.Consequence = CivilWarUiText.T("AF_KINGDOM_CW_Action_ConcedeConsequence", "The current demand is honored in full; a claim to the throne means abdication, and a war of independence means recognizing independence.");
			if (q.Reason.Length == 0 && (actor.Influence < q.Influence || actor.Leader.Gold < q.Gold)) q.Reason = CivilWarUiText.T("AF_KINGDOM_CW_Action_RulingClanResources", "The ruling clan lacks the resources.");
		}
		else
		{
			KingdomCivilWarClanState member = null;
			s?.Clans.TryGetValue(actor?.StringId ?? "", out member);
			var own = FactionOfClan(s, actor);
			bool same = r.Action == CivilWarAction.JoinCrown && member?.Side == KingdomCivilWarSide.Crown
				|| r.Action == CivilWarAction.JoinOpposition && own != null && own == f;
			if (!PoliticalClan(actor, k)) q.Reason = CivilWarUiText.T("AF_KINGDOM_CW_Action_VassalOnly", "You must be a full vassal of this kingdom.");
			else if (actor == k.RulingClan) q.Reason = CivilWarUiText.T("AF_KINGDOM_CW_Action_KingCannotJoin", "The king cannot join, leave, or found an opposition faction on their own.");
			else if (own?.Stage == KingdomCivilWarStage.OpenWar || (r.Action != CivilWarAction.Detonate && s?.Factions.Any(x => x.Stage == KingdomCivilWarStage.OpenWar) == true)) q.Reason = CivilWarUiText.T("AF_KINGDOM_CW_Action_MembershipLocked", "Membership management is locked during a founding request or civil war.");
			else if (own?.ResolutionNeedsReview == true) q.Reason = CivilWarUiText.T("AF_KINGDOM_CW_Action_MembershipPendingReview", "A faction operation result is still awaiting review; membership and factions cannot be changed yet.");
			else if (r.Action == CivilWarAction.ChangeDemand)
			{
				if (actor != Clan.PlayerClan || f == null || f != own || f.LeaderClanId != actor.StringId || !IsPreWar(f)) q.Reason = CivilWarUiText.T("AF_KINGDOM_CW_Action_ChangeOwnDemandOnly", "You can only change the demand of a pre-war faction you lead.");
				else if (f.PendingResponse != null || !string.IsNullOrEmpty(f.PendingConcessionOperationId)) q.Reason = CivilWarUiText.T("AF_KINGDOM_CW_Action_DemandChangeLocked", "A royal negotiation or agreement settlement is in progress; the demand cannot be changed yet.");
				else if (f.DemandId == r.DemandId && f.TargetId == r.TargetId) q.Reason = CivilWarUiText.T("AF_KINGDOM_CW_Action_DemandUnchanged", "The demand and target are unchanged; members do not need to be reassessed.");
				else if (!GetFoundingOptions(k.StringId, actor).Any(x => x.DemandId == r.DemandId && x.TargetId == r.TargetId)) q.Reason = CivilWarUiText.T("AF_KINGDOM_CW_Action_NewDemandInvalid", "The new demand or target is not valid, or the required holdings are missing.");
				q.Consequence = CivilWarUiText.T("AF_KINGDOM_CW_Action_ChangeDemandConsequence", "Change your faction's demand and immediately reassess whether every other member stays involved; those who leave return to uncommitted, with no extra relation loss, no outside clan recruitment, and no rebellion.");
			}
			else if (r.Action == CivilWarAction.DissolveOwn)
			{
				if (actor != Clan.PlayerClan || f == null || f != own || f.LeaderClanId != actor.StringId || !IsPreWar(f)) q.Reason = CivilWarUiText.T("AF_KINGDOM_CW_Action_DissolveOwnOnly", "You can only dissolve a pre-war faction you lead.");
				else if (f.PendingResponse?.Accepted == true || !string.IsNullOrEmpty(f.PendingConcessionOperationId)) q.Reason = CivilWarUiText.T("AF_KINGDOM_CW_Action_AcceptedAgreementSettling", "An accepted agreement is still settling; the faction cannot be dissolved.");
				q.Consequence = CivilWarUiText.T("AF_KINGDOM_CW_Action_DissolveOwnConsequence", "Dissolve the faction you lead: −10 relation with every other member clan. Grievance is kept and any existing kingdom cooldown is not extended.");
			}
			else if (r.Action == CivilWarAction.Found && actor == Clan.PlayerClan)
			{
				if (own?.PendingResponse?.Accepted == true || !string.IsNullOrEmpty(own?.PendingConcessionOperationId)) q.Reason = CivilWarUiText.T("AF_KINGDOM_CW_Action_AgreementSettling", "An agreement is still settling; the faction cannot be refounded.");
				else if (!GetFoundingOptions(k.StringId, actor).Any(x => x.DemandId == r.DemandId && x.TargetId == r.TargetId)) q.Reason = CivilWarUiText.T("AF_KINGDOM_CW_Action_DemandInvalid", "The demand or target is not valid, or the required holdings are missing.");
				q.Consequence = CivilWarUiText.T("AF_KINGDOM_CW_Action_FoundConsequence", "Found a faction: −10 relation with the ruler. If you are already in a camp, you leave it first and bear the member relation loss, and your old faction dissolves. A player leader only starts a rebellion manually.");
			}
			else if (r.Action == CivilWarAction.Leave)
			{
				if (member == null || member.Side == KingdomCivilWarSide.Middle) q.Reason = CivilWarUiText.T("AF_KINGDOM_CW_Action_AlreadyNeutral", "You are already uncommitted.");
				q.Consequence = CivilWarUiText.T("AF_KINGDOM_CW_Action_LeaveConsequence", "−10 relation with the former member leader and −20 with the faction leader (not cumulative); you cannot join a faction for 7 days. The player may still found a faction.");
			}
			else if (r.Action == CivilWarAction.Detonate)
			{
				if (f == null || own != f || !IsPreWar(f)) q.Reason = CivilWarUiText.T("AF_KINGDOM_CW_Action_DetonateOwnOnly", "You can only call your own pre-war faction to rise.");
				else if (!CanStartPoliticalWar(k, s, f, actor == Clan.PlayerClan)) q.Reason = CivilWarUiText.T("AF_KINGDOM_CW_Action_RisingBlocked", "The domestic/world war cap or rebellion immunity prevents a rebellion.");
				else if (actor == Clan.PlayerClan && !PlayerDetonationAllowed(k, actor, out string strengthReason)) q.Reason = strengthReason;
				else if (f.LeaderClanId != actor.StringId && CivilWarWorld.CurrentDay() < f.ProposalUntilDay) q.Reason = CivilWarUiText.T("AF_KINGDOM_CW_Action_ProposalRefusedCooldown", "After a proposal to rise is refused, you must wait 7 days.");
				q.Consequence = f?.LeaderClanId == actor.StringId ? CivilWarUiText.T("AF_KINGDOM_CW_Action_DetonateLeaderConsequence", "Enter the rebellion and kingdom-founding process immediately.") : CivilWarUiText.T("AF_KINGDOM_CW_Action_DetonateProposalConsequence", "Propose it to the leader; after a refusal you cannot propose again for 7 days.");
			}
			else if (!same)
			{
				if (member != null && member.Side != KingdomCivilWarSide.Middle) q.Reason = CivilWarUiText.T("AF_KINGDOM_CW_Action_MustLeaveCampFirst", "You must leave your current camp first, bearing the relation loss and the 7-day cooldown.");
				else if (_storage.ClanExitUntilDay.TryGetValue(actor.StringId, out int until) && CivilWarWorld.CurrentDay() < until) { q.CooldownUntilDay = until; q.Reason = CivilWarUiText.T("AF_KINGDOM_CW_Action_ExitCooldown", "Exit cooldown runs until {A}; changing kingdoms does not bypass it.", ("A", CivilWarUiText.Date(until))); }
				else if (r.Action == CivilWarAction.JoinOpposition && !IsPreWar(f)) q.Reason = CivilWarUiText.T("AF_KINGDOM_CW_Action_SelectJoinableFaction", "Select a pre-war faction you can join.");
				else if (r.Action == CivilWarAction.Found)
				{
					if (!CivilWarFactionRules.CanFormFaction(s?.Factions.Count ?? 0, CivilWarWorld.CurrentWeek(), s?.CooldownUntilWeek ?? 0, DuelSettings.BuildCivilWarTuning()) || (s?.CooldownUntilDay ?? 0) > CivilWarWorld.CurrentDay()) q.Reason = CivilWarUiText.T("AF_KINGDOM_CW_Action_FactionSlotsFull", "Faction slots are full, or the kingdom is still on cooldown.");
					else if (!GetFoundingOptions(k.StringId, actor).Any(x => x.DemandId == r.DemandId && x.TargetId == r.TargetId)) q.Reason = CivilWarUiText.T("AF_KINGDOM_CW_Action_DemandInvalid", "The demand or target is not valid, or the required holdings are missing.");
				}
				else if (r.Action != CivilWarAction.JoinCrown && r.Action != CivilWarAction.JoinOpposition) q.Reason = CivilWarUiText.T("AF_KINGDOM_CW_Action_Unsupported", "This operation is not supported.");
			}
		}
		q.Allowed = q.Reason.Length == 0;
		return q;
	}

	private bool CanStartPoliticalWar(Kingdom k, KingdomCivilWarKingdomState s, KingdomCivilWarFactionState f, bool playerAuthorized = false)
	{
		CivilWarTuning tuning = DuelSettings.BuildCivilWarTuning();
		return !PlayerKingdomRebellionImmunity.ShouldProtectKingdom(k)
			&& CivilWarFactionRules.CanOpenWar(s != null && OtherFactionAtWar(s, f), tuning)
			&& CivilWarFactionRules.HasWorldWarSlot(WorldOpenWarCount, tuning, playerAuthorized);
	}
	// Quoted on demand (panel open / AI governance), never per tick.
	private static int CompensationOffer(Kingdom k, KingdomCivilWarKingdomState s, KingdomCivilWarFactionState f, int tier, bool influence)
	{
		int leaderTier = CivilWarWorld.FindClan(f.LeaderClanId)?.Tier ?? 0;
		float power = IsPreWar(f) ? FactionPower(k, s, f) : f.LastFactionPower;
		float grievance = CivilWarRules.Clamp(f.Grievance / 100f, 0f, 1f);
		string key = f.Id + ":" + CivilWarWorld.CurrentWeek() + ":" + (influence ? "i" : "g");
		return influence ? CivilWarPoliticalRules.InfluenceOffer(tier, leaderTier, power, grievance, key) : CivilWarPoliticalRules.GoldOffer(tier, leaderTier, power, grievance, key);
	}
	private static string Fingerprint(CivilWarActionRequest r, Clan actor) => string.Join("|", actor?.StringId, r.KingdomId, r.FactionId, r.Action, r.DemandId, r.TargetId, r.OfferTier, r.OfferInfluence, r.Accept);
	internal CivilWarActionResult Execute(CivilWarActionRequest r, Clan actor, bool leaderAgreed = false)
	{
		if (r == null || string.IsNullOrWhiteSpace(r.OperationId)) return new CivilWarActionResult { Status = CivilWarActionStatus.Rejected, Message = KingdomCivilWarTexts.Raw("KW_Raw_076"), MessageProjectionKey = "ReceiptNoIdentity" };
		string fingerprint = Fingerprint(r, actor);
		if (_storage.Operations.TryGetValue(r.OperationId, out var previous))
			return new CivilWarActionResult { FactionId = previous.FactionId, Status = previous.Fingerprint != fingerprint || previous.InProgress ? CivilWarActionStatus.PartialFailure : (CivilWarActionStatus)previous.Status,
				Message = previous.Fingerprint != fingerprint ? KingdomCivilWarTexts.Raw("KW_Raw_077") : previous.InProgress ? KingdomCivilWarTexts.Raw("KW_Raw_078") : previous.Message,
				MessageProjectionKey = previous.Fingerprint != fingerprint ? "ReceiptIdentityConflict" : previous.InProgress ? "ReceiptInterrupted" : "" };
		var quote = Quote(r, actor);
		if (!quote.Allowed) return new CivilWarActionResult { Status = CivilWarActionStatus.Rejected, Message = quote.Reason };
		var receipt = new CivilWarOperation { Fingerprint = fingerprint, FactionId = r.FactionId, InProgress = true };
		_storage.Operations.Add(r.OperationId, receipt);
		Kingdom k = CivilWarWorld.FindKingdom(r.KingdomId);
		var s = GetOrCreate(k, CivilWarWorld.CurrentWeek());
		var f = ActionFaction(r, s, actor);
		// Only Found can leave/dissolve an old side before creating a different faction. This list is action-local,
		// filled by the existing relation loops; it never becomes persistent faction membership or tick work.
		List<Hero> affectedParticipants = r.Action == CivilWarAction.Found ? new List<Hero>() : null;
		CivilWarActionResult result;
		try
		{
			result = ExecutePoliticalAction(r, actor, k, s, f, quote, leaderAgreed, affectedParticipants);
			receipt.Status = (int)result.Status; receipt.Message = result.Message; receipt.FactionId = result.FactionId;
			receipt.InProgress = false;
			if (result.Status == CivilWarActionStatus.Applied)
			{
				// Display path: every Applied result carries a projection key, so the chronicle entry renders the
				// localized sentence while the raw receipt text keeps feeding material and facts.
				PublishPoliticalResult(k, s, f, actor, r.OperationId, result.Message, affectedParticipants, CivilWarUiText.ResultNotice(result));
				receipt.FactsWritten = true;
			}
		}
		catch (CivilWarRetryableEffectException ex)
		{
			if (f != null) { f.ResolutionError = ex.Message; f.ResolutionNeedsReview = false; }
			receipt.InProgress = false; receipt.Status = (int)CivilWarActionStatus.PartialFailure;
			receipt.Message = KingdomCivilWarTexts.Raw("KW_Raw_079") + ex.Message;
			result = new CivilWarActionResult { Status = CivilWarActionStatus.PartialFailure, Message = receipt.Message, FactionId = f?.Id ?? "" };
			string retryDetail = CivilWarUiText.FailureText(ex.Code, ex.Arg, ex.Arg2);
			if (retryDetail != null) Project(result, "RetrySafeSteps", retryDetail);
		}
		catch (Exception ex)
		{
			if (f != null) { f.ResolutionNeedsReview = true; f.ResolutionError = ex.Message; }
			receipt.Message = KingdomCivilWarTexts.Raw("KW_Raw_080") + ex.Message;
			result = new CivilWarActionResult { Status = CivilWarActionStatus.PartialFailure, Message = receipt.Message, FactionId = f?.Id ?? "" };
			var described = ex as CivilWarReviewRequiredException;
			string reviewDetail = CivilWarUiText.FailureText(described?.Code ?? "", described?.Arg ?? "", described?.Arg2 ?? "");
			if (reviewDetail != null) Project(result, "IncompleteReview", reviewDetail);
			Logger.Log("KingdomCivilWar", receipt.Message);
		}
		if (f != null) f.Version++;
		SaveSummary(s);
		if (r.Action == CivilWarAction.Suppress) NotifyPoliticalChange(k, "royal_suppression");
		if (result.Status == CivilWarActionStatus.Applied && (r.Action == CivilWarAction.JoinCrown || r.Action == CivilWarAction.JoinOpposition || r.Action == CivilWarAction.Leave || r.Action == CivilWarAction.Found || r.Action == CivilWarAction.DissolveOwn || r.Action == CivilWarAction.ChangeDemand)) NotifyPoliticalChange(k, "membership");
		Revision++;
		return result;
	}

	// Records a display projection key alongside the raw Message. The notification sites render the
	// projection when present and otherwise fall back to Message, so an old saved receipt or an outcome
	// this build does not describe still shows its real text instead of a guessed summary.
	private static void Project(CivilWarActionResult result, string key, string arg1 = "", string arg2 = "")
	{
		if (result == null) return;
		result.MessageProjectionKey = key ?? "";
		result.MessageProjectionArg = arg1 ?? "";
		result.MessageProjectionArg2 = arg2 ?? "";
	}

	private CivilWarActionResult ExecutePoliticalAction(CivilWarActionRequest r, Clan actor, Kingdom k, KingdomCivilWarKingdomState s, KingdomCivilWarFactionState f, CivilWarActionQuote q, bool leaderAgreed, ICollection<Hero> affectedParticipants = null)
	{
		int day = CivilWarWorld.CurrentDay(), week = CivilWarWorld.CurrentWeek();		var tuning = DuelSettings.BuildCivilWarTuning();
		var member = GetOrCreateClan(s, actor, week);
		var result = new CivilWarActionResult { Status = CivilWarActionStatus.Applied, FactionId = f?.Id ?? "" };
		if (r.Action == CivilWarAction.JoinCrown || r.Action == CivilWarAction.JoinOpposition)
		{
			var side = r.Action == CivilWarAction.JoinCrown ? KingdomCivilWarSide.Crown : KingdomCivilWarSide.Opposition;
			if (member.Side == side && (side == KingdomCivilWarSide.Crown || member.FactionId == f?.Id)) { result.Status = CivilWarActionStatus.Rejected; result.Message = KingdomCivilWarTexts.Raw("KW_Raw_081"); Project(result, "AlreadyInCamp"); return result; }
			member.Side = side; member.FactionId = side == KingdomCivilWarSide.Crown ? "" : f.Id; member.SideSinceWeek = week; member.SideSinceDay = CivilWarWorld.CurrentDay();
			if (actor == Clan.PlayerClan) s.PlayerSide = side == KingdomCivilWarSide.Crown ? "crown" : "opposition";
			result.Message = CivilWarWorld.ClanName(actor) + KingdomCivilWarTexts.Raw("KW_Raw_082") + (side == KingdomCivilWarSide.Crown ? KingdomCivilWarTexts.Raw("KW_Raw_083") : FactionName(f, CivilWarCatalog.FindDemand(f.DemandId))) + "。";
			Project(result, "Joined", CivilWarWorld.ClanName(actor), side == KingdomCivilWarSide.Crown ? CivilWarUiText.T("AF_KINGDOM_CW_CampCrown", "the crown camp") : CivilWarUiText.DemandTag(CivilWarCatalog.FindDemand(f?.DemandId)));
		}
		else if (r.Action == CivilWarAction.Leave)
		{
			DetachPoliticalSide(k, s, actor, member, week, day);
			result.Message = CivilWarWorld.ClanName(actor) + KingdomCivilWarTexts.Raw("KW_Raw_084");
			Project(result, "LeftCamp", CivilWarWorld.ClanName(actor));
		}
		else if (r.Action == CivilWarAction.Found)
		{
			var previous = FactionOfClan(s, actor);
			string departure = "";
			if (previous?.LeaderClanId == actor.StringId && actor == Clan.PlayerClan)
			{
				departure = KingdomCivilWarTexts.Raw("KW_Raw_085") + FactionName(previous, CivilWarCatalog.FindDemand(previous.DemandId)) + KingdomCivilWarTexts.Raw("KW_Raw_086");
				DissolvePlayerFaction(k, s, previous, actor, week, tuning, affectedParticipants);
			}
			else if (member.Side != KingdomCivilWarSide.Middle)
			{
				departure = KingdomCivilWarTexts.Raw("KW_Raw_087") + (previous == null ? KingdomCivilWarTexts.Raw("KW_Raw_083") : FactionName(previous, CivilWarCatalog.FindDemand(previous.DemandId))) + KingdomCivilWarTexts.Raw("KW_Raw_086");
				DetachPoliticalSide(k, s, actor, member, week, day, affectedParticipants);
			}
			var demand = CivilWarCatalog.FindDemand(r.DemandId);
			f = new KingdomCivilWarFactionState { Id = FactionPrefix + k.StringId + "-" + (++s.FactionSerial), DemandId = demand.Id, TargetId = r.TargetId,
				TargetName = demand.Target == CivilWarDemandTarget.EnemyKingdom ? CivilWarWorld.KingdomName(CivilWarWorld.FindKingdom(r.TargetId)) : k.ActivePolicies.FirstOrDefault(x => x.StringId == r.TargetId)?.Name?.ToString() ?? "",
				LeaderClanId = actor.StringId, PlayerFounded = actor == Clan.PlayerClan, CreatedWeek = week, StageWeek = week, UltimatumWeek = week + tuning.UltimatumDelayWeeks,
				UltimatumDay = day + tuning.UltimatumDelayWeeks * 7, LastDemandDay = day, WarGoal = (int)CivilWarCatalog.EffectiveWarGoal(demand), Grievance = TotalGrievance(member) };
			s.Factions.Add(f); member.Side = KingdomCivilWarSide.Opposition; member.FactionId = f.Id; member.SideSinceWeek = week; member.SideSinceDay = CivilWarWorld.CurrentDay();
			if (actor == Clan.PlayerClan) s.PlayerSide = "opposition";
			ApplyFoundingRelationLoss(k, actor);
			result.FactionId = f.Id; result.Message = CivilWarWorld.ClanName(actor) + departure + KingdomCivilWarTexts.Raw("KW_Raw_088") + FormatDemand(demand, f.TargetName) + "。";
			Project(result, "Founded", CivilWarWorld.ClanName(actor), CivilWarUiText.DemandDisplayText(demand, ResolveDemandTargetDisplay(demand, f, k)));
		}
		else if (r.Action == CivilWarAction.ChangeDemand)
		{
			result.Message = ChangePlayerFactionDemand(k, s, f, r, week, tuning, out string changedDemand, out string changedRetained);
			Project(result, "DemandChanged", changedDemand, changedRetained);
		}
		else if (r.Action == CivilWarAction.DissolveOwn)
		{
			DissolvePlayerFaction(k, s, f, actor, week, tuning);
			result.Message = KingdomCivilWarTexts.Raw("KW_Raw_089");
			Project(result, "DissolvedOwn");
		}
		else if (r.Action == CivilWarAction.Detonate)
		{
			var leader = CivilWarWorld.FindClan(f.LeaderClanId);
			if (leader != actor && !leaderAgreed && RandomFloat() >= EscalationChance(k, s, f))
			{
				f.ProposalUntilDay = day + CivilWarPoliticalRules.ActionDays; result.Status = CivilWarActionStatus.Rejected; result.Message = KingdomCivilWarTexts.Raw("KW_Raw_090"); Project(result, "LeaderRefusedRising"); return result;
			}
			f.RebellionOperationId = r.OperationId;
			OpenWar(k, s, f, leader, week, tuning, HostStability, playerAuthorized: actor == Clan.PlayerClan);
			result.Status = CivilWarActionStatus.AwaitingKingdom; result.Message = KingdomCivilWarTexts.Raw("KW_Raw_091");
			Project(result, "RisingAccepted");
		}
		else if (r.Action == CivilWarAction.Suppress)
		{
			ChangeClanInfluenceAction.Apply(actor, -q.Influence); f.GovernanceUntilDay = day + CivilWarPoliticalRules.ActionDays;
			foreach (var record in Members(s, f).ToList())
			{
				var clan = CivilWarWorld.FindClan(record.ClanId); if (clan == null) continue;
				ChangeClanInfluenceAction.Apply(clan, -Math.Min(CivilWarPoliticalRules.SuppressInfluenceLoss, Math.Max(0, clan.Influence)));
				AddPoints(record, "royal_suppression", clan.StringId == f.LeaderClanId ? CivilWarPoliticalRules.SuppressLeaderGrievance : CivilWarPoliticalRules.SuppressMemberGrievance);
				ChangeRelationAction.ApplyRelationChangeBetweenHeroes(clan.Leader, actor.Leader, -CivilWarPoliticalRules.SuppressRelationLoss, false);
			}
			var candidate = Members(s, f).Where(x => x.ClanId != f.LeaderClanId && x.ClanId != Clan.PlayerClan?.StringId)
				.OrderBy(TotalGrievance).ThenBy(x => x.ClanId, StringComparer.Ordinal).FirstOrDefault();
			if (candidate != null && CivilWarRules.Roll("suppression_leave", CivilWarCatalog.LeaveOpposition,
				BuildFeatures(k, s, f, CivilWarWorld.FindClan(candidate.ClanId), Host.GetStability(k)), CivilWarPoliticalRules.SuppressLeaveMultiplier, tuning, RandomFloat).Passed)
			{ candidate.Side = KingdomCivilWarSide.Middle; candidate.FactionId = ""; candidate.SideSinceWeek = week; candidate.SideSinceDay = CivilWarWorld.CurrentDay(); }
			if (!f.SuppressionExtended) { f.SuppressionExtended = true; f.UltimatumDay = Math.Max(day, f.UltimatumDay < 0 ? f.UltimatumWeek * 7 : f.UltimatumDay) + 7; if (f.PlayerAnswerPending) f.AnswerDeadlineDay = Math.Max(day, f.AnswerDeadlineDay < 0 ? f.PlayerAnswerDeadlineWeek * 7 : f.AnswerDeadlineDay) + 7; }
			result.Message = KingdomCivilWarTexts.Raw("KW_Raw_092") + FactionName(f, CivilWarCatalog.FindDemand(f.DemandId)) + KingdomCivilWarTexts.Raw("KW_Raw_093");
			Project(result, "Suppressed", CivilWarUiText.DemandTag(CivilWarCatalog.FindDemand(f?.DemandId)));
		}
		else if (r.Action == CivilWarAction.Respond)
		{
			var pending = f.PendingResponse;
			pending.ResponseOperationId = r.OperationId;
			if (k.RulingClan?.StringId != pending.RulerClanId) { f.PendingResponse = null; result.Status = CivilWarActionStatus.Rejected; result.Message = KingdomCivilWarTexts.Raw("KW_Raw_094"); Project(result, "RulingClanReplaced"); return result; }
			return CompletePoliticalResponse(k, s, f, pending, r.Accept, playerAuthorized: actor == Clan.PlayerClan && r.OperationId != pending.OperationId + ":timeout");
		}
		else if (r.Action == CivilWarAction.Negotiate || r.Action == CivilWarAction.ForceDissolve)
		{
			bool dissolve = r.Action == CivilWarAction.ForceDissolve;
			f.GovernanceUntilDay = day + CivilWarPoliticalRules.ActionDays; f.DemandLocked = true;
			if (dissolve)
			{
				ChangeClanInfluenceAction.Apply(actor, -q.Influence);
				foreach (var record in Members(s, f)) { AddPoints(record, "royal_suppression", CivilWarPoliticalRules.DissolveGrievance); var clan = CivilWarWorld.FindClan(record.ClanId); if (clan?.Leader != null) ChangeRelationAction.ApplyRelationChangeBetweenHeroes(clan.Leader, actor.Leader, -CivilWarPoliticalRules.DissolveRelationLoss, false); }
			}
			var pending = new CivilWarPendingResponse { OperationId = r.OperationId, RulerClanId = actor.StringId, LeaderClanId = f.LeaderClanId, Dissolve = dissolve, Gold = q.Gold, Influence = dissolve ? 0 : q.Influence, DeadlineDay = day + CivilWarPoliticalRules.ReplyDays };
			f.PendingResponse = pending;
			if (f.LeaderClanId == Clan.PlayerClan?.StringId)
			{ _hasPoliticalResponse = true; result.Status = CivilWarActionStatus.AwaitingPlayer; result.Message = dissolve ? KingdomCivilWarTexts.Raw("KW_Raw_095") : KingdomCivilWarTexts.Raw("KW_Raw_096"); Project(result, dissolve ? "DissolveSentToPlayer" : "OfferSentToPlayer"); return result; }
			bool accept = dissolve ? RandomFloat() >= EscalationChance(k, s, f) : RandomFloat() < CivilWarPoliticalRules.NegotiationChance(BuildFeatures(k, s, f, CivilWarWorld.FindClan(f.LeaderClanId), Host.GetStability(k)), r.OfferTier, tuning);
			return CompletePoliticalResponse(k, s, f, pending, accept);
		}
		else if (r.Action == CivilWarAction.Concede)
		{
			if (string.IsNullOrEmpty(f.PendingConcessionOperationId)) { f.PendingConcessionRulerId = actor.StringId; f.PendingConcessionLeaderId = f.LeaderClanId; }
			if (f.PendingConcessionRulerId != actor.StringId || f.PendingConcessionLeaderId != f.LeaderClanId) throw new InvalidOperationException(KingdomCivilWarTexts.Raw("KW_Raw_097"));
			f.PendingConcessionOperationId = r.OperationId;
			var demand = CivilWarCatalog.FindDemand(f.DemandId);
			var ctx = BuildContext(k, s, f, demand, CivilWarWorld.FindClan(f.LeaderClanId), week);
			string effect = demand.AcceptEffectId;
			if (f.Stage == KingdomCivilWarStage.OpenWar)
			{
				if (CivilWarCatalog.EffectiveWarGoal(demand) == CivilWarWarGoal.Secede) effect = CivilWarEffectIds.Secede;
				else if (CivilWarCatalog.EffectiveWarGoal(demand) == CivilWarWarGoal.Usurp) effect = CivilWarEffectIds.UsurpThrone;
				else CivilWarEffects.ReturnRebels(ctx, ctx.WarClans(), 0, true);
			}
			if (!CivilWarEffects.TryApply(effect, ctx, out string reason)) { f.ResolutionError = reason; f.ResolutionNeedsReview = !ctx.RetryableFailure; result.Status = CivilWarActionStatus.PartialFailure; result.Message = KingdomCivilWarTexts.Raw("KW_Raw_098") + reason; string failure = CivilWarUiText.FailureText(ctx.FailureCode, ctx.FailureArg, ctx.FailureArg2); if (failure != null) Project(result, "ConcessionPending", failure); return result; }
			result.Message = KingdomCivilWarTexts.Raw("KW_Raw_099") + FactionName(f, demand) + KingdomCivilWarTexts.Raw("KW_Raw_100") + string.Join("；", ctx.Notes);
			Project(result, "Conceded", CivilWarUiText.DemandTag(demand), CivilWarUiText.NoteText(ctx.Notes, ctx.NoteProjections));
			f.PendingConcessionOperationId = "";
			FinishFaction(k, s, f, week, tuning, HostStability, 4);
		}
		return result;
	}

	private float EscalationChance(Kingdom k, KingdomCivilWarKingdomState s, KingdomCivilWarFactionState f)
	{
		var demand = CivilWarCatalog.FindDemand(f.DemandId);
		return CivilWarRules.Shape((demand?.Escalate?.Raw(BuildFeatures(k, s, f, CivilWarWorld.FindClan(f.LeaderClanId), Host.GetStability(k))) ?? 0)
			* (demand?.EscalationScale ?? 1) * CivilWarAftermathRules.EscalationFactor(CurrentAftermath(s, CivilWarWorld.CurrentWeek())), DuelSettings.BuildCivilWarTuning());
	}

	private CivilWarActionResult CompletePoliticalResponse(Kingdom k, KingdomCivilWarKingdomState s, KingdomCivilWarFactionState f, CivilWarPendingResponse p, bool accept, bool playerAuthorized = false)
	{
		var result = new CivilWarActionResult { FactionId = f.Id, Status = CivilWarActionStatus.Applied };
		var crown = CivilWarWorld.FindClan(p.RulerClanId); var leader = CivilWarWorld.FindClan(p.LeaderClanId);
		if (crown != k.RulingClan || p.LeaderClanId != f.LeaderClanId) throw new InvalidOperationException(KingdomCivilWarTexts.Raw("KW_Raw_101"));
		if (!accept)
		{
			f.PendingResponse = null;
			if (p.Dissolve && !CanStartPoliticalWar(k, s, f, playerAuthorized)) { result.Status = CivilWarActionStatus.Rejected; result.Message = KingdomCivilWarTexts.Raw("KW_Raw_102"); Project(result, "WarEligibilityChanged"); }
			else if (p.Dissolve && IsPlayerLed(f) && !playerAuthorized) { RefuseAndReschedule(f, CivilWarWorld.CurrentWeek(), DuelSettings.BuildCivilWarTuning()); result.Status = CivilWarActionStatus.Rejected; result.Message = KingdomCivilWarTexts.Raw("KW_Raw_103"); Project(result, "DissolveOrderExpired"); }
			else if (p.Dissolve) { OpenWar(k, s, f, leader, CivilWarWorld.CurrentWeek(), DuelSettings.BuildCivilWarTuning(), HostStability, playerAuthorized); result.Status = CivilWarActionStatus.AwaitingKingdom; result.Message = KingdomCivilWarTexts.Raw("KW_Raw_104"); Project(result, "FactionDefied"); }
			else { result.Status = CivilWarActionStatus.Rejected; result.Message = KingdomCivilWarTexts.Raw("KW_Raw_105"); Project(result, "OfferRefused"); }
			return result;
		}
		if (!p.Dissolve)
		{
			if (crown?.Leader == null || leader?.Leader == null || crown.Leader.Gold < p.Gold || crown.Influence < p.Influence)
			{ f.PendingResponse = null; result.Status = CivilWarActionStatus.Rejected; result.Message = KingdomCivilWarTexts.Raw("KW_Raw_106"); Project(result, "PayerCannotPay"); return result; }
			p.Accepted = true;
			if (f.Stage == KingdomCivilWarStage.OpenWar) CivilWarEffects.ReturnRebels(BuildContext(k, s, f, CivilWarCatalog.FindDemand(f.DemandId), leader, CivilWarWorld.CurrentWeek()), f.WarClanIds.Select(CivilWarWorld.FindClan).Where(x => x != null), 0, true);
			if (p.Gold > 0) GiveGoldAction.ApplyBetweenCharacters(crown.Leader, leader.Leader, p.Gold, disableNotification: true);
			if (p.Influence > 0) { ChangeClanInfluenceAction.Apply(crown, -p.Influence); ChangeClanInfluenceAction.Apply(leader, p.Influence); }
		}
		f.PendingResponse = null;
		result.Message = p.Dissolve ? KingdomCivilWarTexts.Raw("KW_Raw_107") : KingdomCivilWarTexts.Raw("KW_Raw_108");
		Project(result, p.Dissolve ? "ObeyedDissolve" : "AcceptedOffer");
		FinishFaction(k, s, f, CivilWarWorld.CurrentWeek(), DuelSettings.BuildCivilWarTuning(), HostStability, 0);
		foreach (string id in new[] { p.OperationId, p.ResponseOperationId })
			if (_storage.Operations.TryGetValue(id ?? "", out var receipt)) { receipt.Status = (int)result.Status; receipt.Message = result.Message; receipt.InProgress = false; }
		return result;
	}

	private void EnsurePoliticalLeader(Kingdom k, KingdomCivilWarKingdomState s, KingdomCivilWarFactionState f, bool startCooldown = true)
	{
		if (!IsPreWar(f)) return;
		var leader = CivilWarWorld.FindClan(f.LeaderClanId);
		if (PoliticalClan(leader, k) && leader != k.RulingClan && FactionOfClan(s, leader) == f) return;
		var next = Members(s, f).Select(x => CivilWarWorld.FindClan(x.ClanId)).Where(x => PoliticalClan(x, k) && x != k.RulingClan)
			.OrderByDescending(CivilWarWorld.Strength).ThenBy(x => x.StringId, StringComparer.Ordinal).FirstOrDefault();
		if (next == null) Dissolve(k, s, f, CivilWarWorld.CurrentWeek(), DuelSettings.BuildCivilWarTuning(), CivilWarUiText.T("AF_KINGDOM_CW_Action_NoSuccessor", "The faction has no eligible successor and is dissolved."), startCooldown);
		else { f.LeaderClanId = next.StringId; f.Version++; AddHistory(s, CivilWarWorld.CurrentWeek(), CivilWarUiText.T("AF_KINGDOM_CW_Action_LeaderSuccession", "{A} succeeds as faction leader.", ("A", CivilWarWorld.ClanName(next)))); }
	}

	// `displayText` is an optional panel-only projection of the same result. The raw `text` stays the value
	// that material, saved receipts and AFEF facts consume; only the chronicle entry uses the projection, and
	// a caller without one keeps passing `text` alone.
	private void PublishPoliticalResult(Kingdom k, KingdomCivilWarKingdomState s, KingdomCivilWarFactionState f, Clan actor, string operationId, string text, IEnumerable<Hero> affectedParticipants = null, string displayText = null)
	{
		AddHistory(s, CivilWarWorld.CurrentWeek(), displayText ?? text);
		MyBehavior.RecordCivilWarPoliticalResult(k, "civil_war:result:" + operationId, text, f != null || operationId.Contains(":war:"),
			actor?.Leader, CivilWarWorld.FindClan(f?.LeaderClanId)?.Leader);
		var heroes = new HashSet<Hero>();
		if (affectedParticipants != null)
			foreach (Hero hero in affectedParticipants) if (hero != null) heroes.Add(hero);
		if (actor?.Leader != null) heroes.Add(actor.Leader);
		if (k.Leader != null) heroes.Add(k.Leader);
		var leader = CivilWarWorld.FindClan(f?.LeaderClanId); if (leader?.Leader != null) heroes.Add(leader.Leader);
		if (f != null) foreach (var id in f.WarClanIds.Concat(f.LastParticipantIds).Concat(Members(s, f).Select(x => x.ClanId))) { var clan = CivilWarWorld.FindClan(id); if (clan?.Leader != null) heroes.Add(clan.Leader); }
		foreach (var hero in heroes) WriteFact(k, hero, "civil_war:action:" + operationId + ":" + hero.StringId, text);
	}
}
