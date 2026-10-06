using System;
using System.Collections.Generic;
using System.Linq;
using Bannerlord.UIExtenderEx.Attributes;
using AnimusForge.Refactor.Modules;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AnimusForge;

// Identity actions of the faction tab. Every button is quoted by the civil-war owner (same rules as
// execution); clicking always re-quotes and confirms, so a dim button only shows why it is unavailable.
public sealed partial class KingdomFactionPanelVM
{
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
				Add(Act.Suppress, "压制", TintNeutral);
				Add(Act.Negotiate, "谈判", TintPrimary);
				Add(Act.Concede, "妥协", TintNeutral);
				Add(Act.Dissolve, "强制解散", TintDanger);
				break;
			case CivilWarPanelRole.Crown:
				Add(Act.Leave, "退出王室阵营", TintNeutral);
				Add(Act.JoinFaction, "加入所选派系", TintDanger);
				Add(Act.Found, "建立派系", TintNeutral);
				break;
			case CivilWarPanelRole.Middle:
				Add(Act.JoinCrown, "加入王室", TintGold);
				Add(Act.JoinFaction, "加入所选派系", TintDanger);
				Add(Act.Found, "建立派系", TintNeutral);
				break;
			case CivilWarPanelRole.Member:
				Add(Act.Detonate, "提议起兵", TintDanger);
				Add(Act.Leave, "退出派系", TintNeutral);
				Add(Act.JoinCrown, "加入王室", TintGold);
				Add(Act.Found, "建立派系", TintNeutral);
				break;
			case CivilWarPanelRole.Leader:
				if (own?.HasPendingResponse == true)
				{
					Add(Act.Accept, own.PendingDissolve ? "服从解散" : "接受补偿", TintGold);
					Add(Act.Refuse, own.PendingDissolve ? "抗命起兵" : "拒绝补偿", own.PendingDissolve ? TintDanger : TintNeutral);
				}
				if (own?.PendingDissolve != true) Add(Act.Detonate, "立即起兵", TintDanger);
				Add(Act.Leave, "退出派系", TintNeutral);
				Add(Act.DissolveOwn, "解散派系", TintDanger);
				Add(Act.ChangeDemand, "更改诉求", TintNeutral);
				break;
		}
	}

	private string TitleFor(CivilWarPanelFaction own)
	{
		string tag = own?.Tag ?? "";
		switch (_panel.Role)
		{
			case CivilWarPanelRole.King: return _faction != null ? "国王治理  ·  对 " + _faction.ShortName : _panel.Factions.Count > 0 ? "国王治理  ·  请选择一个派系" : "国王治理  ·  尚无可治理派系";
			case CivilWarPanelRole.Crown: return "王室附庸  ·  你的家族不满 " + _panel.PlayerGrievance + "  ·  与国王 " + Signed(_panel.PlayerRelationToKing);
			case CivilWarPanelRole.Member: return tag + "成员  ·  你的家族不满 " + _panel.PlayerGrievance + "  ·  与领袖 " + Signed(_panel.PlayerRelationToLeader);
			case CivilWarPanelRole.Leader: return tag + "领袖" + (own?.HasPendingResponse == true ? "  ·  待答复：" + (own.PendingDissolve ? "国王的解散令" : "国王的谈判补偿") : "  ·  你的家族不满 " + _panel.PlayerGrievance);
			case CivilWarPanelRole.Middle: return "未表态封臣  ·  你的家族不满 " + _panel.PlayerGrievance + "  ·  与国王 " + Signed(_panel.PlayerRelationToKing);
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
			Actions.Add(new KingdomFactionActionVM(label, _panel.Factions.Count > 0 ? "请先在目录中选择派系" : "尚无反对派", true, tint, () => RunAction(act)));
			return;
		}
		CivilWarActionQuote quote;
		if (act == Act.Negotiate) quote = BestOffer(factionId);
		else if (act == Act.Found || act == Act.ChangeDemand)
		{
			CivilWarFoundingOption first = DemandOptions(act).FirstOrDefault();
			if (first == null) { Actions.Add(new KingdomFactionActionVM(label, "没有可选诉求", true, tint, () => RunAction(act))); return; }
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
			case Act.Negotiate: return "选择补偿档位";
			case Act.Concede: return "兑现" + (_faction?.DemandTag ?? "诉求");
			case Act.JoinCrown: return "站在国王一边";
			case Act.ChangeDemand: return "其他成员重评是否参与";
			case Act.DissolveOwn: return "与其他成员关系 −10";
			case Act.JoinFaction: return "加入" + Clip(_faction?.ShortName ?? "所选派系", 8);
			case Act.Leave: return _panel.Role == CivilWarPanelRole.Leader ? "领导权移交继任者" : CivilWarPoliticalRules.ExitDays + " 天中立 · 关系受损";
			case Act.Found: return "选择诉求 · 名额 " + _panel.Factions.Count + " / " + _panel.MaxFactions;
			case Act.Detonate: return _panel.Role == CivilWarPanelRole.Leader ? "进入建国流程" : "交由领袖判定";
			case Act.Accept: return own?.PendingDissolve == true ? "派系解散" : "收下 " + OfferText(own) + " · 解散";
			case Act.Refuse: return own?.PendingDissolve == true ? "进入起兵流程" : "派系保留 · 不收费";
			default: return "";
		}
	}

	private static string OfferText(CivilWarPanelFaction f) => f == null ? "补偿" : f.PendingGold > 0 ? f.PendingGold + " 金币" : f.PendingInfluence + " 影响力";

	private static string Cost(CivilWarActionQuote q) => q.Gold > 0 ? q.Gold + " 金币" : q.Influence > 0 ? q.Influence + " 影响力" : "无固定费用";

	// The first clause of the owner's reason fits under a button; the full text is shown on click.
	private static string Brief(CivilWarActionQuote q)
	{
		if (q.CooldownUntilDay > 0) return "冷却至第 " + q.CooldownUntilDay + " 天";
		string first = (q.Reason ?? "").Split(new[] { '，', '。', '；' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim() ?? "";
		return first.Length == 0 || first.Length > 14 ? "暂不可用 · 点击查看" : first;
	}

	private void RunAction(Act act)
	{
		if (!_open) return;
		try
		{
			bool governance = act == Act.Suppress || act == Act.Negotiate || act == Act.Concede || act == Act.Dissolve;
			if (governance && _faction == null) { InformationManager.DisplayMessage(new InformationMessage(_panel.Factions.Count > 0 ? "请先在目录中选择要治理的派系。" : "王国内尚无反对派。")); return; }
			if (act == Act.Found || act == Act.ChangeDemand) { RunDemandSelection(act); return; }
			if (act == Act.Negotiate) { RunNegotiate(); return; }
			if (act == Act.Accept || act == Act.Refuse)
			{
				CivilWarPanelFaction own = Own;
				string body = own?.PendingDissolve == true
					? (act == Act.Accept ? "服从解散令：派系解散，不满仍然保留。" : "抗命：拒绝解散并进入起兵流程。")
					: (act == Act.Accept ? "接受补偿：收下 " + OfferText(own) + "，撤回诉求并解散派系。" : "拒绝补偿：派系保留，不收补偿。");
				Confirm(Request(act, TargetFaction(act)), body);
				return;
			}
			Confirm(Request(act, TargetFaction(act)), null);
		}
		catch (Exception ex)
		{
			Logger.Log("CivilWar", "[KingdomFactionTab] action failed: " + act + " " + ex.Message);
			InformationManager.DisplayMessage(new InformationMessage("派系操作失败，详见日志。"));
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
		Select(act == Act.ChangeDemand ? "更改派系诉求" : "建立派系",
			act == Act.ChangeDemand ? "更改后保留派系身份，立即重评其他成员是否继续参与；不会自动起兵。" : "无需达到不满或关系门槛，但仍受战后冷却；创建时与统治者关系−10，改建时承担旧阵营关系损失。",
			options.Select(x => new InquiryElement(x, x.Text, null, true, "由本家族领导")).ToList(), selected =>
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
		Select("谈判补偿", "只有对方接受后才转移补偿；拒绝仍消耗行动冷却。", offers, chosen => Confirm((CivilWarActionRequest)chosen, null));
	}

	private void Select(string title, string text, List<InquiryElement> items, Action<object> apply)
	{
		if (!_open) return;
		if (items.Count == 0) { InformationManager.DisplayMessage(new InformationMessage("当前没有合法选项。")); return; }
		int generation = _generation;
		MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(title, text, items, true, 1, 1, "继续", "返回", selected =>
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
		InformationManager.ShowInquiry(new InquiryData("确认派系操作", cost + (body ?? q.Consequence), true, true, "确认", "取消", () =>
		{
			if (!_open || generation != _generation) return;
			var result = TeamModuleServices.CivilWar.Execute(r);
			InformationManager.DisplayMessage(new InformationMessage(result.Message));
			if (_open && generation == _generation) Refresh();
		}, null));
	}
}
