using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics
{
	// Token: 0x02000078 RID: 120
	public class MissionCrimeHandler : MissionLogic
	{
		// Token: 0x060004CE RID: 1230 RVA: 0x0001EBF8 File Offset: 0x0001CDF8
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
}
