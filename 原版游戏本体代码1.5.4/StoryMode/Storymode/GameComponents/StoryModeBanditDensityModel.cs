using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;

namespace StoryMode.GameComponents;

public class StoryModeBanditDensityModel : BanditDensityModel
{
	public override int NumberOfMaximumBanditPartiesAroundEachHideout
	{
		get
		{
			if (StoryModeManager.Current.MainStoryLine.IsPlayerInteractionRestricted)
			{
				return 0;
			}
			return base.BaseModel.NumberOfMaximumBanditPartiesAroundEachHideout;
		}
	}

	public override int NumberOfMaximumBanditPartiesInEachHideout
	{
		get
		{
			if (StoryModeManager.Current.MainStoryLine.IsPlayerInteractionRestricted)
			{
				return 0;
			}
			return base.BaseModel.NumberOfMaximumBanditPartiesInEachHideout;
		}
	}

	public override int NumberOfMaximumHideoutsAtEachBanditFaction
	{
		get
		{
			if (StoryModeManager.Current.MainStoryLine.IsPlayerInteractionRestricted)
			{
				return 0;
			}
			return base.BaseModel.NumberOfMaximumHideoutsAtEachBanditFaction;
		}
	}

	public override int NumberOfInitialHideoutsAtEachBanditFaction
	{
		get
		{
			if (StoryModeManager.Current.MainStoryLine.IsPlayerInteractionRestricted)
			{
				return 0;
			}
			return base.BaseModel.NumberOfInitialHideoutsAtEachBanditFaction;
		}
	}

	public override int NumberOfMinimumBanditPartiesInAHideoutToInfestIt => base.BaseModel.NumberOfMinimumBanditPartiesInAHideoutToInfestIt;

	public override int NumberOfMinimumBanditTroopsInHideoutMission => base.BaseModel.NumberOfMinimumBanditTroopsInHideoutMission;

	public override int NumberOfMaximumTroopCountForFirstFightInHideout => base.BaseModel.NumberOfMaximumTroopCountForFirstFightInHideout;

	public override int NumberOfMaximumTroopCountForBossFightInHideout => base.BaseModel.NumberOfMaximumTroopCountForBossFightInHideout;

	public override float SpawnPercentageForFirstFightInHideoutMission => base.BaseModel.SpawnPercentageForFirstFightInHideoutMission;

	public override int GetMaximumTroopCountForHideoutMission(MobileParty party, bool isAssault)
	{
		return base.BaseModel.GetMaximumTroopCountForHideoutMission(party, isAssault);
	}

	public override bool IsPositionInsideNavalSafeZone(CampaignVec2 position)
	{
		return base.BaseModel.IsPositionInsideNavalSafeZone(position);
	}

	public override int GetMaxSupportedNumberOfLootersForClan(Clan clan)
	{
		if (StoryModeManager.Current.MainStoryLine.IsPlayerInteractionRestricted)
		{
			return 0;
		}
		return base.BaseModel.GetMaxSupportedNumberOfLootersForClan(clan);
	}

	public override int GetMinimumTroopCountForHideoutMission(MobileParty party, bool isAssault)
	{
		return base.BaseModel.GetMinimumTroopCountForHideoutMission(party, isAssault);
	}
}
