using System;
using System.Collections.Generic;
using System.Linq;
using Bannerlord.UIExtenderEx.Attributes;
using AnimusForge.Refactor.Modules;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace AnimusForge;

// Identity actions of the faction tab. Every button is quoted by the civil-war owner (same rules as
// execution); clicking always re-quotes and confirms, so a dim button only shows why it is unavailable.
public sealed partial class KingdomFactionPanelVM
{
	private static string L(string id, string fallback) => new TextObject("{=" + id + "}" + fallback).ToString();
	private static string L(string id, string fallback, string variable, object value)
	{
		return DeveloperUiText.Format(id, fallback, (variable, value));
	}
	private static string L(string id, string fallback, params (string Name, object Value)[] variables)
	{
		return DeveloperUiText.Format(id, fallback, variables);
	}

	private enum Act { Suppress, Negotiate, Concede, Dissolve, JoinCrown, JoinFaction, Leave, Found, Detonate, Accept, Refuse, DissolveOwn, ChangeDemand }
	private const string TintPrimary = "#6E523380", TintDanger = "#5E2A2280", TintGold = "#4A3C2280", TintNeutral = "#3A2E2266";
	private bool _open;
	private int _generation;

	internal void Open() { if (_open) return; _open = true; _generation++; TeamModuleServices.CivilWar.StateChanged += OnPoliticalChange; }
	internal void Close() { if (!_open) return; _open = false; _generation++; TeamModuleServices.CivilWar.StateChanged -= OnPoliticalChange; }
	public override void OnFinalize() { Close(); base.OnFinalize(); }
	private void OnPoliticalChange() { if (_open && _panel.Revision != TeamModuleServices.CivilWar.Revision) Refresh(); }

	private CivilWarPanelFaction Own => _panel.Factions.FirstOrDefault(x => x.Id == _panel.PlayerFactionId);

	private void RebuildActions()
	{
		Actions.Clear();
		CivilWarPanelFaction own = Own;
		ActionTitle = TitleFor(own);
		if (!HasEntry) return;
		switch (_panel.Role)
		{
			case CivilWarPanelRole.King:
				Add(Act.Suppress, L("AF_DIP_UI_CivilWar_ActionSuppress", "Suppress"), TintNeutral);
				Add(Act.Negotiate, L("AF_DIP_UI_CivilWar_ActionNegotiate", "Negotiate"), TintPrimary);
				Add(Act.Concede, L("AF_DIP_UI_CivilWar_ActionConcede", "Concede"), TintNeutral);
				Add(Act.Dissolve, L("AF_DIP_UI_CivilWar_ActionDissolve", "Force dissolve"), TintDanger);
				break;
			case CivilWarPanelRole.Crown:
				Add(Act.Leave, L("AF_DIP_UI_CivilWar_ActionLeaveCrown", "Leave the crown faction"), TintNeutral);
				Add(Act.JoinFaction, L("AF_DIP_UI_CivilWar_ActionJoinFaction", "Join selected faction"), TintDanger);
				Add(Act.Found, L("AF_DIP_UI_CivilWar_ActionFound", "Found a faction"), TintNeutral);
				break;
			case CivilWarPanelRole.Middle:
				Add(Act.JoinCrown, L("AF_DIP_UI_CivilWar_ActionJoinCrown", "Join the crown"), TintGold);
				Add(Act.JoinFaction, L("AF_DIP_UI_CivilWar_ActionJoinFaction", "Join selected faction"), TintDanger);
				Add(Act.Found, L("AF_DIP_UI_CivilWar_ActionFound", "Found a faction"), TintNeutral);
				break;
			case CivilWarPanelRole.Member:
				Add(Act.Detonate, L("AF_DIP_UI_CivilWar_ActionProposeRebellion", "Propose rebellion"), TintDanger);
				Add(Act.Leave, L("AF_DIP_UI_CivilWar_ActionLeaveFaction", "Leave faction"), TintNeutral);
				Add(Act.JoinCrown, L("AF_DIP_UI_CivilWar_ActionJoinCrown", "Join the crown"), TintGold);
				Add(Act.Found, L("AF_DIP_UI_CivilWar_ActionFound", "Found a faction"), TintNeutral);
				break;
			case CivilWarPanelRole.Leader:
				if (own?.HasPendingResponse == true)
				{
					Add(Act.Accept, own.PendingDissolve ? L("AF_DIP_UI_CivilWar_ActionAcceptDissolve", "Accept dissolution") : L("AF_DIP_UI_CivilWar_ActionAcceptCompensation", "Accept compensation"), TintGold);
					Add(Act.Refuse, own.PendingDissolve ? L("AF_DIP_UI_CivilWar_ActionRefuseDissolve", "Refuse and rebel") : L("AF_DIP_UI_CivilWar_ActionRefuseCompensation", "Refuse compensation"), own.PendingDissolve ? TintDanger : TintNeutral);
				}
				if (own?.PendingDissolve != true) Add(Act.Detonate, L("AF_DIP_UI_CivilWar_ActionImmediateRebellion", "Rebel now"), TintDanger);
				Add(Act.Leave, L("AF_DIP_UI_CivilWar_ActionLeaveFaction", "Leave faction"), TintNeutral);
				Add(Act.DissolveOwn, L("AF_DIP_UI_CivilWar_ActionDissolveOwn", "Dissolve faction"), TintDanger);
				Add(Act.ChangeDemand, L("AF_DIP_UI_CivilWar_ActionChangeDemand", "Change demand"), TintNeutral);
				break;
		}
	}

	private string TitleFor(CivilWarPanelFaction own)
	{
		string tag = own?.Tag ?? "";
		switch (_panel.Role)
		{
			case CivilWarPanelRole.King: return _faction != null ? L("AF_DIP_UI_CivilWar_TitleKingSelected", "Kingdom governance · {FACTION}", "FACTION", _faction.ShortName) : _panel.Factions.Count > 0 ? L("AF_DIP_UI_CivilWar_TitleKingChoose", "Kingdom governance · select a faction") : L("AF_DIP_UI_CivilWar_TitleKingEmpty", "Kingdom governance · no faction to govern");
			case CivilWarPanelRole.Crown: return L("AF_DIP_UI_CivilWar_TitleCrown", "Crown vassal · clan grievance {GRIEVANCE} · relation with king {RELATION}", ("GRIEVANCE", _panel.PlayerGrievance), ("RELATION", Signed(_panel.PlayerRelationToKing)));
			case CivilWarPanelRole.Member: return L("AF_DIP_UI_CivilWar_TitleMember", "{TAG} member · clan grievance {GRIEVANCE} · relation with leader {RELATION}", ("TAG", tag), ("GRIEVANCE", _panel.PlayerGrievance), ("RELATION", Signed(_panel.PlayerRelationToLeader)));
			case CivilWarPanelRole.Leader: return L("AF_DIP_UI_CivilWar_TitleLeader", "{TAG} leader", "TAG", tag) + (own?.HasPendingResponse == true ? "  ·  " + L("AF_DIP_UI_CivilWar_PendingResponse", "pending response: {RESPONSE}", "RESPONSE", own.PendingDissolve ? L("AF_DIP_UI_CivilWar_DissolutionOrder", "the king's dissolution order") : L("AF_DIP_UI_CivilWar_NegotiationCompensation", "the king's negotiated compensation")) : "  ·  " + L("AF_DIP_UI_CivilWar_Grievance", "clan grievance {GRIEVANCE}", "GRIEVANCE", _panel.PlayerGrievance));
			case CivilWarPanelRole.Middle: return L("AF_DIP_UI_CivilWar_TitleMiddle", "Uncommitted vassal · clan grievance {GRIEVANCE} · relation with king {RELATION}", ("GRIEVANCE", _panel.PlayerGrievance), ("RELATION", Signed(_panel.PlayerRelationToKing)));
			default: return "";
		}
	}

	private static string Signed(int value) => value > 0 ? "+" + value : value < 0 ? "−" + (-value) : "0";

	// Which faction an action is aimed at: the selected tab for governance / joining, the player's own otherwise.
	private string TargetFaction(Act act)
	{
		if (act == Act.Suppress || act == Act.Negotiate || act == Act.Concede || act == Act.Dissolve || act == Act.JoinFaction) return _faction?.Id ?? "";
		if (act == Act.JoinCrown || act == Act.Found) return "";
		return _panel.PlayerFactionId;
	}

	private static CivilWarAction ToAction(Act act)
	{
		switch (act)
		{
			case Act.Suppress: return CivilWarAction.Suppress;
			case Act.Negotiate: return CivilWarAction.Negotiate;
			case Act.Concede: return CivilWarAction.Concede;
			case Act.Dissolve: return CivilWarAction.ForceDissolve;
			case Act.JoinCrown: return CivilWarAction.JoinCrown;
			case Act.JoinFaction: return CivilWarAction.JoinOpposition;
			case Act.Leave: return CivilWarAction.Leave;
			case Act.Found: return CivilWarAction.Found;
			case Act.Detonate: return CivilWarAction.Detonate;
			case Act.DissolveOwn: return CivilWarAction.DissolveOwn;
			case Act.ChangeDemand: return CivilWarAction.ChangeDemand;
			default: return CivilWarAction.Respond;
		}
	}

	private CivilWarActionRequest Request(Act act, string factionId) => new CivilWarActionRequest
	{
		OperationId = Guid.NewGuid().ToString("N"), KingdomId = _panel.KingdomId, FactionId = factionId ?? "", Action = ToAction(act), Accept = act == Act.Accept
	};

	private void Add(Act act, string label, string tint)
	{
		string factionId = TargetFaction(act);
		bool governance = act == Act.Suppress || act == Act.Negotiate || act == Act.Concede || act == Act.Dissolve;
		if (governance && _faction == null)
		{
			Actions.Add(new KingdomFactionActionVM(label, _panel.Factions.Count > 0 ? L("AF_DIP_UI_CivilWar_SelectFactionFirst", "Select a faction in the list first") : L("AF_DIP_UI_CivilWar_NoOpposition", "No opposition faction"), true, tint, () => RunAction(act)));
			return;
		}
		CivilWarActionQuote quote;
		if (act == Act.Negotiate) quote = BestOffer(factionId);
		else if (act == Act.Found || act == Act.ChangeDemand)
		{
			CivilWarFoundingOption first = DemandOptions(act).FirstOrDefault();
			if (first == null) { Actions.Add(new KingdomFactionActionVM(label, L("AF_DIP_UI_CivilWar_NoDemand", "No demand is available"), true, tint, () => RunAction(act))); return; }
			CivilWarActionRequest probe = Request(act, factionId); probe.DemandId = first.DemandId; probe.TargetId = first.TargetId;
			quote = TeamModuleServices.CivilWar.Quote(probe);
		}
		else quote = TeamModuleServices.CivilWar.Quote(Request(act, factionId));
		string sub = quote.Allowed ? AllowedSub(act, quote) : Brief(quote);
		Actions.Add(new KingdomFactionActionVM(label, sub, !quote.Allowed, tint, () => RunAction(act)));
	}

	// Negotiation is allowed when any compensation tier is affordable; tier 1 gold or influence is the cheapest.
	private CivilWarActionQuote BestOffer(string factionId)
	{
		CivilWarActionQuote last = null;
		foreach (bool influence in new[] { false, true })
		{
			CivilWarActionRequest r = Request(Act.Negotiate, factionId); r.OfferTier = 1; r.OfferInfluence = influence;
			last = TeamModuleServices.CivilWar.Quote(r);
			if (last.Allowed) return last;
		}
		return last;
	}

	private string AllowedSub(Act act, CivilWarActionQuote q)
	{
		CivilWarPanelFaction own = Own;
		switch (act)
		{
			case Act.Suppress:
			case Act.Dissolve: return Cost(q);
			case Act.Negotiate: return L("AF_DIP_UI_CivilWar_SelectCompensationTier", "Select a compensation tier");
			case Act.Concede: return L("AF_DIP_UI_CivilWar_ConcedeDemand", "Honor {DEMAND}", "DEMAND", _faction?.DemandTag ?? L("AF_DIP_UI_CivilWar_Demand", "the demand"));
			case Act.JoinCrown: return L("AF_DIP_UI_CivilWar_StandWithKing", "Stand with the king");
			case Act.ChangeDemand: return L("AF_DIP_UI_CivilWar_ReevaluateMembers", "Other members reassess participation");
			case Act.DissolveOwn: return L("AF_DIP_UI_CivilWar_DissolveRelationLoss", "Relations with other members −10");
			case Act.JoinFaction: return L("AF_DIP_UI_CivilWar_JoinFactionSub", "Join {FACTION}", "FACTION", Clip(_faction?.ShortName ?? L("AF_DIP_UI_CivilWar_SelectedFaction", "the selected faction"), 8));
			case Act.Leave: return _panel.Role == CivilWarPanelRole.Leader ? L("AF_DIP_UI_CivilWar_LeadershipTransfer", "Leadership passes to a successor") : L("AF_DIP_UI_CivilWar_LeaveNeutral", "{DAYS} days of neutrality · relations suffer", "DAYS", CivilWarPoliticalRules.ExitDays);
			case Act.Found: return L("AF_DIP_UI_CivilWar_FoundSlots", "Select a demand · slots {USED} / {MAX}", ("USED", _panel.Factions.Count), ("MAX", _panel.MaxFactions));
			case Act.Detonate: return _panel.Role == CivilWarPanelRole.Leader ? L("AF_DIP_UI_CivilWar_FoundingProcess", "Enter the kingdom-founding process") : L("AF_DIP_UI_CivilWar_LeaderDecision", "Let the leader decide");
			case Act.Accept: return own?.PendingDissolve == true ? L("AF_DIP_UI_CivilWar_FactionDissolved", "Faction dissolved") : L("AF_DIP_UI_CivilWar_AcceptOffer", "Take {OFFER} · dissolve", "OFFER", OfferText(own));
			case Act.Refuse: return own?.PendingDissolve == true ? L("AF_DIP_UI_CivilWar_RebellionProcess", "Enter the rebellion process") : L("AF_DIP_UI_CivilWar_FactionRetained", "Faction retained · no cost");
			default: return "";
		}
	}

	private static string OfferText(CivilWarPanelFaction f) => f == null ? L("AF_DIP_UI_CivilWar_Compensation", "compensation") : f.PendingGold > 0 ? L("AF_DIP_UI_CivilWar_Gold", "{AMOUNT} gold", "AMOUNT", f.PendingGold) : L("AF_DIP_UI_CivilWar_Influence", "{AMOUNT} influence", "AMOUNT", f.PendingInfluence);

	private static string Cost(CivilWarActionQuote q) => q.Gold > 0 ? L("AF_DIP_UI_CivilWar_Gold", "{AMOUNT} gold", "AMOUNT", q.Gold) : q.Influence > 0 ? L("AF_DIP_UI_CivilWar_Influence", "{AMOUNT} influence", "AMOUNT", q.Influence) : L("AF_DIP_UI_CivilWar_NoFixedCost", "No fixed cost");

	// The first clause of the owner's reason fits under a button; the full text is shown on click.
	// The owner quotes its reason in the active language, so the split must cover the clause separators of
	// both scripts: splitting on the CJK separators alone leaves every localized English reason whole and
	// the brief permanently generic. A CJK clause says the same thing in roughly half the glyphs, hence the
	// two width budgets for the same button. The clause is shown verbatim - when the owner has no localized
	// wording yet, its real text is the honest thing to show rather than an invented summary.
	private static string Brief(CivilWarActionQuote q)
	{
		if (q.CooldownUntilDay > 0) return L("AF_DIP_UI_CivilWar_Cooldown", "Cooldown until day {DAY}", "DAY", q.CooldownUntilDay);
		string first = (q.Reason ?? "").Split(ClauseBreaks, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim() ?? "";
		int budget = IsMostlyCjk(first) ? 14 : 28;
		return first.Length == 0 || first.Length > budget ? L("AF_DIP_UI_CivilWar_Unavailable", "Unavailable · click to view") : first;
	}

	// Clause separators of both scripts the owner may quote in. Sentence-final periods are deliberately
	// absent: the owner substitutes numbers into its reasons, so splitting on '.' would cut "12.5" apart and
	// put a misleading half-number on the button. Only this brief split reads the list; the reason the owner
	// stores and displays in full is never rewritten.
	private static readonly char[] ClauseBreaks = { '，', '；', '。', ',', ';' };

	// True when the clause is mostly Han script, so the button budget is measured in that script's own glyph
	// width. Costs one pass over a short string and runs only when a button is rebuilt (selection change,
	// political state change), never per frame.
	private static bool IsMostlyCjk(string text)
	{
		int han = 0;
		for (int i = 0; i < text.Length; i++) if (text[i] >= '\u4E00' && text[i] <= '\u9FFF') han++;
		return han * 2 >= text.Length;
	}

	private void RunAction(Act act)
	{
		if (!_open) return;
		try
		{
			bool governance = act == Act.Suppress || act == Act.Negotiate || act == Act.Concede || act == Act.Dissolve;
			if (governance && _faction == null) { InformationManager.DisplayMessage(new InformationMessage(_panel.Factions.Count > 0 ? L("AF_DIP_UI_CivilWar_SelectFactionNotice", "Select the faction to govern in the list first.") : L("AF_DIP_UI_CivilWar_NoOppositionNotice", "There is no opposition faction in the kingdom."))); return; }
			if (act == Act.Found || act == Act.ChangeDemand) { RunDemandSelection(act); return; }
			if (act == Act.Negotiate) { RunNegotiate(); return; }
			if (act == Act.Accept || act == Act.Refuse)
			{
				CivilWarPanelFaction own = Own;
				string body = own?.PendingDissolve == true
					? (act == Act.Accept ? L("AF_DIP_UI_CivilWar_AcceptDissolveBody", "Accept the dissolution order: the faction will dissolve, but the grievance remains.") : L("AF_DIP_UI_CivilWar_RefuseDissolveBody", "Refuse the order: reject dissolution and enter the rebellion process."))
					: (act == Act.Accept ? L("AF_DIP_UI_CivilWar_AcceptCompensationBody", "Accept compensation: take {OFFER}, withdraw the demand, and dissolve the faction.", "OFFER", OfferText(own)) : L("AF_DIP_UI_CivilWar_RefuseCompensationBody", "Refuse compensation: the faction remains and no payment is taken."));
				Confirm(Request(act, TargetFaction(act)), body);
				return;
			}
			Confirm(Request(act, TargetFaction(act)), null);
		}
		catch (Exception ex)
		{
			Logger.Log("CivilWar", "[KingdomFactionTab] action failed: " + act + " " + ex.Message);
			InformationManager.DisplayMessage(new InformationMessage(L("AF_DIP_UI_CivilWar_ActionFailed", "Faction operation failed; see the log for details.")));
		}
	}

	private List<CivilWarFoundingOption> DemandOptions(Act act)
	{
		var options = TeamModuleServices.CivilWar.GetFoundingOptions(_panel.KingdomId);
		if (act == Act.ChangeDemand) options = options.Where(x => x.DemandId != Own?.DemandId || x.TargetId != Own?.TargetId).ToList();
		return options;
	}

	private CivilWarActionRequest DemandRequest(Act act, CivilWarFoundingOption option)
	{
		CivilWarActionRequest request = Request(act, TargetFaction(act)); request.DemandId = option.DemandId; request.TargetId = option.TargetId; return request;
	}

	private void RunDemandSelection(Act act)
	{
		var options = DemandOptions(act);
		Select(act == Act.ChangeDemand ? L("AF_DIP_UI_CivilWar_ChangeDemandTitle", "Change faction demand") : L("AF_DIP_UI_CivilWar_FoundTitle", "Found faction"),
			act == Act.ChangeDemand ? L("AF_DIP_UI_CivilWar_ChangeDemandDescription", "The faction identity remains; other members immediately reassess participation. This does not start a rebellion automatically.") : L("AF_DIP_UI_CivilWar_FoundDescription", "No grievance or relation threshold is required, but post-war cooldown still applies; founding costs −10 relation with the ruler, while refounding carries the old faction's relation loss."),
			options.Select(x => new InquiryElement(x, x.Text, null, true, L("AF_DIP_UI_CivilWar_LedByClan", "Led by this clan"))).ToList(), selected =>
			{
				var option = selected as CivilWarFoundingOption; if (option == null) return;
				Confirm(DemandRequest(act, option), null);
			});
	}

	private void RunNegotiate()
	{
		var offers = new List<InquiryElement>();
		for (int tier = 1; tier <= 3; tier++) for (int currency = 0; currency < 2; currency++)
		{
			CivilWarActionRequest r = Request(Act.Negotiate, _faction.Id); r.OfferTier = tier; r.OfferInfluence = currency == 1;
			CivilWarActionQuote q = TeamModuleServices.CivilWar.Quote(r);
			offers.Add(new InquiryElement(r, Cost(q), null, q.Allowed, q.Allowed ? q.Consequence : q.Reason));
		}
		Select(L("AF_DIP_UI_CivilWar_NegotiateTitle", "Negotiated compensation"), L("AF_DIP_UI_CivilWar_NegotiateDescription", "Compensation transfers only if the other side accepts; refusal still consumes the action cooldown."), offers, chosen => Confirm((CivilWarActionRequest)chosen, null));
	}

	private void Select(string title, string text, List<InquiryElement> items, Action<object> apply)
	{
		if (!_open) return;
		if (items.Count == 0) { InformationManager.DisplayMessage(new InformationMessage(L("AF_DIP_UI_CivilWar_NoLegalOptions", "There are no legal options right now."))); return; }
		int generation = _generation;
		MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(title, text, items, true, 1, 1, L("AF_DIP_UI_Common_Continue", "Continue"), L("AF_DIP_UI_Common_Back", "Back"), selected =>
		{ if (!_open || _generation != generation) return; var item = selected?.FirstOrDefault(); if (item != null) apply(item.Identifier); }, null));
	}

	// Quote again, show cost and consequence, execute on confirm. A rejected quote only reports the reason.
	private void Confirm(CivilWarActionRequest r, string body)
	{
		if (!_open) return;
		var q = TeamModuleServices.CivilWar.Quote(r);
		if (!q.Allowed) { InformationManager.DisplayMessage(new InformationMessage(q.Reason)); Refresh(); return; }
		r.Version = q.Version;
		int generation = _generation;
		string cost = q.Gold > 0 || q.Influence > 0 ? Cost(q) + "\n" : "";
		InformationManager.ShowInquiry(new InquiryData(L("AF_DIP_UI_CivilWar_ConfirmTitle", "Confirm faction operation"), cost + (body ?? q.Consequence), true, true, L("AF_DIP_UI_Common_Confirm", "Confirm"), L("AF_DIP_UI_Common_Cancel", "Cancel"), () =>
		{
			if (!_open || generation != _generation) return;
			var result = TeamModuleServices.CivilWar.Execute(r);
			InformationManager.DisplayMessage(new InformationMessage(CivilWarUiText.ResultNotice(result)));
			if (_open && generation == _generation) Refresh();
		}, null));
	}
}
