using StoryMode;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace NavalDLC.GameComponents;

public class NavalDLCBanditDensityModel : BanditDensityModel
{
	private const float GetNavalSafeZoneRadiusForFortificationPort = 15f;

	private const float GetNavalSafeZoneRadiusForVillagePort = 7f;

	private Clan _deserterClan;

	private Clan DeserterClan
	{
		get
		{
			if (_deserterClan == null)
			{
				_deserterClan = Clan.FindFirst((Clan x) => x.StringId == "deserters");
			}
			return _deserterClan;
		}
	}

	public override int NumberOfMinimumBanditPartiesInAHideoutToInfestIt => base.BaseModel.NumberOfMinimumBanditPartiesInAHideoutToInfestIt;

	public override int NumberOfMaximumBanditPartiesInEachHideout => base.BaseModel.NumberOfMaximumBanditPartiesInEachHideout;

	public override int NumberOfMaximumBanditPartiesAroundEachHideout => base.BaseModel.NumberOfMaximumBanditPartiesAroundEachHideout;

	public override int NumberOfMinimumBanditTroopsInHideoutMission => base.BaseModel.NumberOfMinimumBanditTroopsInHideoutMission;

	public override int NumberOfInitialHideoutsAtEachBanditFaction
	{
		get
		{
			StoryModeManager current = StoryModeManager.Current;
			if (current != null && current.MainStoryLine?.IsPlayerInteractionRestricted == true)
			{
				return 0;
			}
			return base.BaseModel.NumberOfInitialHideoutsAtEachBanditFaction + 1;
		}
	}

	public override int NumberOfMaximumHideoutsAtEachBanditFaction
	{
		get
		{
			StoryModeManager current = StoryModeManager.Current;
			if (current != null && current.MainStoryLine?.IsPlayerInteractionRestricted == true)
			{
				return 0;
			}
			return base.BaseModel.NumberOfMaximumHideoutsAtEachBanditFaction + 1;
		}
	}

	public override int NumberOfMaximumTroopCountForFirstFightInHideout => base.BaseModel.NumberOfMaximumTroopCountForFirstFightInHideout;

	public override int NumberOfMaximumTroopCountForBossFightInHideout => base.BaseModel.NumberOfMaximumTroopCountForBossFightInHideout;

	public override float SpawnPercentageForFirstFightInHideoutMission => base.BaseModel.SpawnPercentageForFirstFightInHideoutMission;

	public override int GetMaximumTroopCountForHideoutMission(MobileParty party, bool isAssault)
	{
		return base.BaseModel.GetMaximumTroopCountForHideoutMission(party, isAssault);
	}

	public override bool IsPositionInsideNavalSafeZone(CampaignVec2 position)
	{
		if (position.IsValid() && !position.IsOnLand)
		{
			Settlement item = Campaign.Current.Models.MapDistanceModel.GetClosestEntranceToFace(position.Face, MobileParty.NavigationType.Naval).Item1;
			float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(item, in position, isFromPort: true, MobileParty.NavigationType.Naval);
			float num = (item.IsVillage ? 7f : 15f);
			if (distance < num)
			{
				return true;
			}
		}
		return false;
	}

	public override int GetMaxSupportedNumberOfLootersForClan(Clan clan)
	{
		StoryModeManager current = StoryModeManager.Current;
		if (current != null && current.MainStoryLine?.IsPlayerInteractionRestricted == true)
		{
			return 0;
		}
		if (clan.HasNavalNavigationCapability)
		{
			return NavalDLCManager.Instance.NavalMapSceneWrapper.GetSpawnPoints(clan.StringId).Count;
		}
		if (clan.StringId == "looters")
		{
			return 300 - ((DeserterClan != null) ? DeserterClan.WarPartyComponents.Count : 0);
		}
		return base.BaseModel.GetMaxSupportedNumberOfLootersForClan(clan);
	}

	public override int GetMinimumTroopCountForHideoutMission(MobileParty party, bool isAssault)
	{
		return base.BaseModel.GetMinimumTroopCountForHideoutMission(party, isAssault);
	}
}
