using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AnimusForge.Refactor.Runtime;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using static AnimusForge.MyBehavior;
namespace AnimusForge;
internal sealed class KingdomStabilityLabController
{
 private long _confirmationTicket;
 private readonly KingdomStabilityLabPort _port;
 internal KingdomStabilityLabController(KingdomStabilityLabPort port) { _port = port; }
	internal void OpenDevKingdomStabilityLabMenu()
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		List<Kingdom> devEditableKingdoms = EventEditorProjection.GetDevEditableKingdoms();
		if (devEditableKingdoms.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("当前没有可测试的王国。"));
			_port.OpenDevEventEditorMenu();
			return;
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("back", "返回", null));
		foreach (Kingdom item in devEditableKingdoms)
		{
			list.Add(new InquiryElement(item.StringId ?? "", BuildDevKingdomStabilityLabel(item), null));
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("这里用于实验王国稳定度与带城反出逻辑。");
		stringBuilder.AppendLine("默认所有王国稳定度都是 50（一般），不会自动叛乱。");
		stringBuilder.AppendLine("现在周报里的 STAB_* 评级标签也会改变王国稳定度；手动调整与周报评级都会影响后续周结算。");
		stringBuilder.AppendLine(" ");
		stringBuilder.AppendLine("请选择一个王国进入详情。");
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("王国稳定度与叛乱实验", stringBuilder.ToString().TrimEnd(), list, isExitShown: true, 0, 1, "进入", "返回", delegate(List<InquiryElement> selected)
		{
    if (!_port.IsCurrent(generation)) return;
			if (selected == null || selected.Count == 0)
			{
				_port.OpenDevEventEditorMenu();
				return;
			}
			if (selected[0].Identifier is string text && text == "back")
			{
				_port.OpenDevEventEditorMenu();
				return;
			}
			string kingdomId = selected[0].Identifier as string;
			Kingdom kingdom = _port.FindKingdomById(kingdomId);
			if (kingdom == null)
			{
				InformationManager.DisplayMessage(new InformationMessage("找不到对应的王国。"));
				OpenDevKingdomStabilityLabMenu();
			}
			else
			{
				OpenDevKingdomStabilityDetailMenu(kingdom);
			}
		}, delegate
		{
    if (!_port.IsCurrent(generation)) return;
			_port.OpenDevEventEditorMenu();
		}, "", isSeachAvailable: true);
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	internal string BuildDevKingdomStabilityLabel(Kingdom kingdom)
	{
		if (kingdom == null)
		{
			return "未知王国";
		}
		int kingdomStabilityValue = _port.GetKingdomStabilityValue(kingdom);
		bool protectedByImmunity = PlayerKingdomRebellionImmunity.ShouldProtectKingdom(kingdom);
		string immunityText = protectedByImmunity ? "（玩家王国稳定度叛乱免疫）" : "";
		return _port.GetKingdomDisplayName(kingdom, "王国") + " [" + _port.GetKingdomStabilityTierText(kingdomStabilityValue) + "/" + kingdomStabilityValue + "] 叛乱周概率 " + _port.FormatKingdomRebellionChance(protectedByImmunity ? 0f : _port.GetKingdomRebellionWeeklyChance(kingdomStabilityValue)) + immunityText;
	}

	internal string BuildDevKingdomStabilityDetailText(Kingdom kingdom)
	{
		if (kingdom == null)
		{
			return "找不到对应的王国。";
		}
		int kingdomStabilityValue = _port.GetKingdomStabilityValue(kingdom);
		bool protectedByImmunity = PlayerKingdomRebellionImmunity.ShouldProtectKingdom(kingdom);
		float kingdomRebellionWeeklyChance = _port.GetKingdomRebellionWeeklyChance(kingdomStabilityValue);
		int kingdomStabilityRelationTargetOffset = _port.GetKingdomStabilityRelationTargetOffset(kingdomStabilityValue);
		int kingdomStabilityWeeklyBalancingDelta = _port.GetKingdomStabilityWeeklyBalancingDelta(kingdomStabilityValue);
		int lowClanCountRuleClanCount = _port.CountActiveKingdomClansForLowClanCountRule(kingdom);
		int lowClanCountRoyalDomainLoyaltyAdjustment = _port.GetLowClanCountRoyalDomainLoyaltyAdjustment(kingdomStabilityValue, lowClanCountRuleClanCount);
		if (protectedByImmunity)
		{
			kingdomRebellionWeeklyChance = 0f;
			kingdomStabilityRelationTargetOffset = 0;
			lowClanCountRoyalDomainLoyaltyAdjustment = 0;
		}
		List<KingdomRebellionCandidateInfo> list = protectedByImmunity ? new List<KingdomRebellionCandidateInfo>() : _port.EvaluateKingdomRebellionCandidates(kingdom, forceTrigger: false);
		List<KingdomRebellionCandidateInfo> list2 = list.Where((KingdomRebellionCandidateInfo x) => x != null && x.Eligible).Take(5).ToList();
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("王国：" + _port.GetKingdomDisplayName(kingdom, "某王国"));
		stringBuilder.AppendLine("当前稳定度：" + kingdomStabilityValue + "（" + _port.GetKingdomStabilityTierText(kingdomStabilityValue) + "）");
		stringBuilder.AppendLine("动态平衡周修正：" + _port.FormatKingdomStabilityRelationOffsetText(kingdomStabilityWeeklyBalancingDelta));
		stringBuilder.AppendLine("当前非王族关系修正：" + _port.FormatKingdomStabilityRelationOffsetText(kingdomStabilityRelationTargetOffset) + "（作用于国王与本国非王族家族成年成员）");
		stringBuilder.AppendLine("非王族有效家族数：" + lowClanCountRuleClanCount + "；王室直辖地忠诚日修正：" + _port.FormatKingdomStabilityRelationOffsetText(lowClanCountRoyalDomainLoyaltyAdjustment));
		if (protectedByImmunity)
		{
			stringBuilder.AppendLine("本周叛乱概率：0%（玩家王国稳定度叛乱免疫已开启）");
		}
		else
		{
			stringBuilder.AppendLine("本周叛乱概率：" + _port.FormatKingdomRebellionChance(kingdomRebellionWeeklyChance));
		}
		stringBuilder.AppendLine("当前国王：" + _port.GetHeroDisplayName(kingdom.Leader));
		stringBuilder.AppendLine("执政家族：" + _port.GetClanDisplayName(kingdom.RulingClan));
		stringBuilder.AppendLine(" ");
		stringBuilder.AppendLine(" ");
		if (protectedByImmunity)
		{
			stringBuilder.AppendLine("当前可自动叛乱候选：免疫状态下不执行本模组稳定度叛乱判定。");
		}
		else if (list2.Count > 0)
		{
			stringBuilder.AppendLine("当前可自动叛乱候选：");
			foreach (KingdomRebellionCandidateInfo item in list2)
			{
				stringBuilder.AppendLine("- " + item.ClanName + " | 关系 " + item.RelationToKing + " | 城镇 " + item.TownCount + " | 城堡 " + item.CastleCount + " | 评分 " + item.Score.ToString("0.0"));
			}
		}
		else
		{
			stringBuilder.AppendLine("当前没有满足自动叛乱条件的家族。");
		}
		return stringBuilder.ToString().TrimEnd();
	}

	internal void OpenDevKingdomStabilityDetailMenu(Kingdom kingdom)
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		if (kingdom == null)
		{
			OpenDevKingdomStabilityLabMenu();
			return;
		}
		List<InquiryElement> list = new List<InquiryElement>
		{
			new InquiryElement("set_value", "调整稳定度", null),
			new InquiryElement("test_roll", "测试本周叛乱判定", null),
			new InquiryElement("force_rebellion", "强制触发该王国叛乱", null),
			new InquiryElement("back", "返回王国列表", null)
		};
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("王国稳定度详情 - " + _port.GetKingdomDisplayName(kingdom, "王国"), BuildDevKingdomStabilityDetailText(kingdom), list, isExitShown: true, 0, 1, "执行", "返回", delegate(List<InquiryElement> selected)
		{
    if (!_port.IsCurrent(generation)) return;
			if (selected == null || selected.Count == 0)
			{
				OpenDevKingdomStabilityLabMenu();
				return;
			}
			switch (selected[0].Identifier as string)
			{
			case "set_value":
				OpenDevEditKingdomStability(kingdom);
				break;
			case "test_roll":
				RunDevKingdomRebellionTest(kingdom);
				break;
			case "force_rebellion":
				ConfirmForceDevKingdomRebellion(kingdom);
				break;
			default:
				OpenDevKingdomStabilityLabMenu();
				break;
			}
		}, delegate
		{
    if (!_port.IsCurrent(generation)) return;
			OpenDevKingdomStabilityLabMenu();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	internal void OpenDevEditKingdomStability(Kingdom kingdom)
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		if (kingdom == null)
		{
			OpenDevKingdomStabilityLabMenu();
			return;
		}
		int kingdomStabilityValue = _port.GetKingdomStabilityValue(kingdom);
		string text = "请输入 0-100 之间的稳定度数值。\n\n当前值：" + kingdomStabilityValue + "（" + _port.GetKingdomStabilityTierText(kingdomStabilityValue) + "）\n默认值：50（一般）\n\n档位：\n90-100 极高\n75-89 高\n60-74 较高\n40-59 一般\n25-39 较差\n10-24 很差\n0-9 极差";
		InformationManager.ShowTextInquiry(new TextInquiryData("调整稳定度 - " + _port.GetKingdomDisplayName(kingdom, "王国"), text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, "保存", "返回", delegate(string input)
		{
    if (!_port.IsCurrent(generation)) return;
			if (!int.TryParse((input ?? "").Trim(), out var result))
			{
				InformationManager.DisplayMessage(new InformationMessage("请输入 0-100 的整数。"));
				OpenDevEditKingdomStability(kingdom);
				return;
			}
			result = _port.ClampKingdomStabilityValue(result);
			_port.SetKingdomStabilityValue(kingdom, result);
			InformationManager.DisplayMessage(new InformationMessage(_port.GetKingdomDisplayName(kingdom, "王国") + " 的稳定度已更新为 " + result + "（" + _port.GetKingdomStabilityTierText(result) + "）。"));
			OpenDevKingdomStabilityDetailMenu(kingdom);
		}, delegate
		{
    if (!_port.IsCurrent(generation)) return;
			OpenDevKingdomStabilityDetailMenu(kingdom);
		}));
	}

	internal void RunDevKingdomRebellionTest(Kingdom kingdom)
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		if (kingdom == null)
		{
			OpenDevKingdomStabilityLabMenu();
			return;
		}
		int num = Math.Max(1, _port.GetCurrentGameDayIndexSafe() / 7);
		KingdomRebellionResolutionResult result = _port.ResolveKingdomRebellion(kingdom, num, executeAction: false, forceTrigger: false);
		InformationManager.ShowInquiry(new InquiryData("叛乱测试结果 - " + _port.GetKingdomDisplayName(kingdom, "王国"), EventEditorProjection.BuildKingdomRebellionResolutionText(_port, result), isAffirmativeOptionShown: true, isNegativeOptionShown: false, "返回详情", "", delegate
		{
    if (!_port.IsCurrent(generation)) return;
			OpenDevKingdomStabilityDetailMenu(kingdom);
		}, null));
	}

	internal void ConfirmForceDevKingdomRebellion(Kingdom kingdom)
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		if (kingdom == null)
		{
			OpenDevKingdomStabilityLabMenu();
			return;
		}
		long ticket = ++_confirmationTicket;
		bool completed = false;
		InformationManager.ShowInquiry(new InquiryData("强制触发王国叛乱", "这会跳过稳定度概率，但不会放宽关系条件；系统仍只会从当前满足自动叛乱条件的家族中，选择一个最适合带城反出的家族执行。\n\n如果当前没有满足关系条件的候选家族，本次将不会发生叛乱。\n\n该操作会真实改动存档中的王国格局，是否继续？", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "强制执行", "取消", delegate
		{
    if (completed || ticket != _confirmationTicket || !_port.IsCurrent(generation)) return;
    completed = true;
			int num = Math.Max(1, _port.GetCurrentGameDayIndexSafe() / 7);
			KingdomRebellionResolutionResult kingdomRebellionResolutionResult = _port.ResolveKingdomRebellion(kingdom, num, executeAction: false, forceTrigger: true);
			Clan selectedClan = kingdomRebellionResolutionResult?.SelectedClan;
			KingdomRebellionCandidateInfo kingdomRebellionCandidateInfo = kingdomRebellionResolutionResult?.Candidates?.FirstOrDefault((KingdomRebellionCandidateInfo x) => x != null && x.Clan == selectedClan);
			if (selectedClan == null || kingdomRebellionCandidateInfo == null)
			{
				InformationManager.ShowInquiry(new InquiryData("强制叛乱结果 - " + _port.GetKingdomDisplayName(kingdom, "王国"), EventEditorProjection.BuildKingdomRebellionResolutionText(_port, kingdomRebellionResolutionResult), isAffirmativeOptionShown: true, isNegativeOptionShown: false, "返回详情", "", delegate
				{
    if (!_port.IsCurrent(generation)) return;
					OpenDevKingdomStabilityDetailMenu(kingdom);
				}, null));
				return;
			}
			_port.StartDevForcedKingdomRebellionAsync(kingdom, selectedClan, num, kingdomRebellionCandidateInfo.RelationToKing, kingdomRebellionCandidateInfo.TownCount, kingdomRebellionCandidateInfo.CastleCount, kingdomRebellionResolutionResult.SelectedFollowerClans);
		}, delegate
		{
    if (completed || ticket != _confirmationTicket || !_port.IsCurrent(generation)) return;
    completed = true;
			OpenDevKingdomStabilityDetailMenu(kingdom);
		}));
	}
}
