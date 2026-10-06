using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace StoryMode.GameComponents;

public class StoryModeKingdomDecisionPermissionModel : KingdomDecisionPermissionModel
{
	public override bool IsPolicyDecisionAllowed(PolicyObject policy)
	{
		return base.BaseModel.IsPolicyDecisionAllowed(policy);
	}

	public override bool IsAnnexationDecisionAllowed(Settlement annexedSettlement)
	{
		return base.BaseModel.IsAnnexationDecisionAllowed(annexedSettlement);
	}

	public override bool IsExpulsionDecisionAllowed(Clan expelledClan)
	{
		return base.BaseModel.IsExpulsionDecisionAllowed(expelledClan);
	}

	public override bool IsKingSelectionDecisionAllowed(Kingdom kingdom)
	{
		return base.BaseModel.IsKingSelectionDecisionAllowed(kingdom);
	}

	public override bool IsWarDecisionAllowedBetweenKingdoms(Kingdom kingdom1, Kingdom kingdom2, out TextObject reason)
	{
		if (StoryModeManager.Current.MainStoryLine.ThirdPhase != null)
		{
			MBReadOnlyList<Kingdom> oppositionKingdoms = StoryModeManager.Current.MainStoryLine.ThirdPhase.OppositionKingdoms;
			if (oppositionKingdoms.IndexOf(kingdom1) >= 0 && oppositionKingdoms.IndexOf(kingdom2) >= 0)
			{
				reason = GameTexts.FindText("str_kingdom_diplomacy_war_truce_disabled_reason_story");
				return false;
			}
		}
		return base.BaseModel.IsWarDecisionAllowedBetweenKingdoms(kingdom1, kingdom2, out reason);
	}

	public override bool IsPeaceDecisionAllowedBetweenKingdoms(Kingdom kingdom1, Kingdom kingdom2, out TextObject reason)
	{
		if (StoryModeManager.Current.MainStoryLine.ThirdPhase != null)
		{
			MBReadOnlyList<Kingdom> oppositionKingdoms = StoryModeManager.Current.MainStoryLine.ThirdPhase.OppositionKingdoms;
			MBReadOnlyList<Kingdom> allyKingdoms = StoryModeManager.Current.MainStoryLine.ThirdPhase.AllyKingdoms;
			if ((oppositionKingdoms.IndexOf(kingdom1) >= 0 && allyKingdoms.IndexOf(kingdom2) >= 0) || (oppositionKingdoms.IndexOf(kingdom2) >= 0 && allyKingdoms.IndexOf(kingdom1) >= 0))
			{
				reason = GameTexts.FindText("str_kingdom_diplomacy_war_truce_disabled_reason_story");
				return false;
			}
		}
		return base.BaseModel.IsPeaceDecisionAllowedBetweenKingdoms(kingdom1, kingdom2, out reason);
	}

	public override bool IsStartAllianceDecisionAllowedBetweenKingdoms(Kingdom kingdom1, Kingdom kingdom2, out TextObject reason)
	{
		return base.BaseModel.IsStartAllianceDecisionAllowedBetweenKingdoms(kingdom1, kingdom2, out reason);
	}
}
