using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics;

public class MissionCrimeHandler : MissionLogic
{
	protected override void OnEndMission()
	{
		if (Settlement.CurrentSettlement != null && Settlement.CurrentSettlement.IsFortification)
		{
			IFaction mapFaction = Settlement.CurrentSettlement.MapFaction;
			if (!Hero.MainHero.IsPrisoner && !Campaign.Current.IsMainHeroDisguised && !mapFaction.IsBanditFaction && Campaign.Current.Models.CrimeModel.IsPlayerCrimeRatingSevere(mapFaction.MapFaction))
			{
				Campaign.Current.GameMenuManager.SetNextMenu("fortification_crime_rating");
			}
		}
	}
}
