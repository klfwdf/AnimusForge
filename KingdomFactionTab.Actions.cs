using System;
using Bannerlord.UIExtenderEx.Attributes;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.Refactor.Modules;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AnimusForge;

public sealed partial class KingdomFactionPanelVM
{
	private string _selectedFaction = "";
	private bool _open;
	private int _generation;
	[DataSourceProperty] public string IdentityText => _panel.Identity + " · " + (_panel.Factions.FirstOrDefault(x => x.Id == _selectedFaction)?.Name ?? "未选择反对派");
	internal void Open() { if (_open) return; _open = true; _generation++; TeamModuleServices.CivilWar.StateChanged += OnPoliticalChange; }
	internal void Close() { if (!_open) return; _open = false; _generation++; TeamModuleServices.CivilWar.StateChanged -= OnPoliticalChange; }
	public override void OnFinalize() { Close(); base.OnFinalize(); }
	private void OnPoliticalChange() { if (_open && _panel.Revision != TeamModuleServices.CivilWar.Revision) Refresh(); }
	private void SelectFaction(string id) { if (!_open) return; _selectedFaction = id; Refresh(); }
	private CivilWarActionRequest Request(CivilWarAction action) => new CivilWarActionRequest { OperationId = Guid.NewGuid().ToString("N"), KingdomId = _panel.KingdomId, FactionId = _selectedFaction, Action = action };
	[DataSourceMethod] public void ExecuteJoinCrown() => Confirm(Request(CivilWarAction.JoinCrown));
	[DataSourceMethod] public void ExecuteJoinFaction() => Confirm(Request(CivilWarAction.JoinOpposition));
	[DataSourceMethod] public void ExecuteLeave() => Confirm(Request(CivilWarAction.Leave));
	[DataSourceMethod] public void ExecuteDetonate() => Confirm(Request(CivilWarAction.Detonate));
	[DataSourceMethod] public void ExecuteFound()
	{
		var options = TeamModuleServices.CivilWar.GetFoundingOptions(_panel.KingdomId);
		Select("建立派系", "选择诉求及合法目标。无需达到不满或对国王关系门槛。", options.Select(x => new InquiryElement(x, x.Text, null, true, "由本家族领导")).ToList(), selected =>
		{
			var option = selected as CivilWarFoundingOption; if (option == null) return;
			var r = Request(CivilWarAction.Found); r.DemandId = option.DemandId; r.TargetId = option.TargetId; Confirm(r);
		});
	}
	[DataSourceMethod] public void ExecuteGovern()
	{
		var items = new List<InquiryElement>();
		foreach (var pair in new[] { Tuple.Create(CivilWarAction.Suppress, "压制"), Tuple.Create(CivilWarAction.Negotiate, "谈判"), Tuple.Create(CivilWarAction.Concede, "妥协"), Tuple.Create(CivilWarAction.ForceDissolve, "强制解散") })
		{
			var r = Request(pair.Item1); var q = TeamModuleServices.CivilWar.Quote(r);
			// A different compensation currency may still be affordable; all six offers are quoted next.
			items.Add(new InquiryElement(pair.Item1, pair.Item2 + " · " + Cost(q), null, pair.Item1 == CivilWarAction.Negotiate || q.Allowed, q.Allowed ? q.Consequence : q.Reason));
		}
		Select("国王治理", "压制、谈判、强制解散共用七天冷却；妥协不受该冷却限制。", items, selected =>
		{
			var action = (CivilWarAction)selected;
			if (action != CivilWarAction.Negotiate) { Confirm(Request(action)); return; }
			var offers = new List<InquiryElement>();
			for (int tier = 1; tier <= 3; tier++) for (int currency = 0; currency < 2; currency++)
			{
				var r = Request(action); r.OfferTier = tier; r.OfferInfluence = currency == 1; var q = TeamModuleServices.CivilWar.Quote(r);
				offers.Add(new InquiryElement(r, Cost(q), null, q.Allowed, q.Allowed ? q.Consequence : q.Reason));
			}
			Select("谈判补偿", "只有对方接受后才转移补偿；拒绝仍消耗行动冷却。", offers, chosen => Confirm((CivilWarActionRequest)chosen));
		});
	}
	[DataSourceMethod] public void ExecuteRespond()
	{
		var r = Request(CivilWarAction.Respond); var q = TeamModuleServices.CivilWar.Quote(r);
		if (!q.Allowed) { InformationManager.DisplayMessage(new InformationMessage(q.Reason)); return; }
		Select("回应王室", q.Consequence, new List<InquiryElement> { new InquiryElement(true, "接受／服从", null, true, "按条款执行"), new InquiryElement(false, "拒绝／抗命", null, true, "拒绝解散将起兵") }, selected => { r.Accept = (bool)selected; Confirm(r); });
	}
	private void Select(string title, string text, List<InquiryElement> items, Action<object> apply)
	{
		if (!_open) return;
		if (items.Count == 0) { InformationManager.DisplayMessage(new InformationMessage("当前没有合法选项。")); return; }
		int generation = _generation;
		MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(title, text, items, true, 1, 1, "继续", "返回", selected =>
		{ if (!_open || _generation != generation) return; var item = selected?.FirstOrDefault(); if (item != null) apply(item.Identifier); }, null));
	}
	private static string Cost(CivilWarActionQuote q) => q.Gold > 0 ? q.Gold + " 金币" : q.Influence > 0 ? q.Influence + " 影响力" : "无固定费用";
	private void Confirm(CivilWarActionRequest r)
	{
		if (!_open) return;
		var q = TeamModuleServices.CivilWar.Quote(r);
		if (!q.Allowed) { InformationManager.DisplayMessage(new InformationMessage(q.Reason)); Refresh(); return; }
		r.Version = q.Version;
		int generation = _generation;
		InformationManager.ShowInquiry(new InquiryData("确认派系操作", Cost(q) + "\n" + q.Consequence, true, true, "确认", "取消", () =>
		{
			if (!_open || generation != _generation) return;
			var result = TeamModuleServices.CivilWar.Execute(r);
			InformationManager.DisplayMessage(new InformationMessage(result.Message));
			if (_open && generation == _generation) Refresh();
		}, null));
	}
}
