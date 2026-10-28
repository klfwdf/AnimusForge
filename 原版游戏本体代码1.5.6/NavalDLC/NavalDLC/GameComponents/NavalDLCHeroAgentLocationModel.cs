using NavalDLC.Storyline;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;

namespace NavalDLC.GameComponents;

public class NavalDLCHeroAgentLocationModel : HeroAgentLocationModel
{
	public override Location GetLocationForHero(Hero hero, Settlement settlement, out HeroLocationDetail heroSpawnDetail)
	{
		if (NavalStorylineData.IsNavalStorylineHero(hero))
		{
			if (NavalStorylineData.HasCompletedLast(NavalStorylineData.NavalStorylineStage.Act3Quest5) && hero == NavalStorylineData.Gunnar && settlement.IsVillage && hero.Occupation == Occupation.Special)
			{
				heroSpawnDetail = HeroLocationDetail.Notable;
				return settlement.LocationComplex.GetLocationWithId("village_center");
			}
			heroSpawnDetail = HeroLocationDetail.NobleBelongingToNoParty;
			if (NavalStorylineData.HasCompletedLast(NavalStorylineData.NavalStorylineStage.None) && hero == NavalStorylineData.Purig && !hero.IsDead)
			{
				return settlement.LocationComplex.GetLocationWithId("tavern");
			}
			if (hero == NavalStorylineData.Gunnar && NavalStorylineData.HasCompletedLast(NavalStorylineData.NavalStorylineStage.None))
			{
				return null;
			}
			return settlement.LocationComplex.GetLocationWithId("port");
		}
		if (NavalStorylineData.IsNavalStoryLineActive())
		{
			heroSpawnDetail = HeroLocationDetail.None;
			return null;
		}
		return base.BaseModel.GetLocationForHero(hero, settlement, out heroSpawnDetail);
	}

	public override bool WillBeListedInOverlay(LocationCharacter locationCharacter)
	{
		if (NavalStorylineData.IsNavalStoryLineActive() && locationCharacter.Character.IsHero && NavalStorylineData.IsNavalStorylineHero(locationCharacter.Character.HeroObject))
		{
			return true;
		}
		return base.BaseModel.WillBeListedInOverlay(locationCharacter);
	}
}
