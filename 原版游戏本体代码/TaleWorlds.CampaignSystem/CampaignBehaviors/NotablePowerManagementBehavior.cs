using System;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x0200041B RID: 1051
	public class NotablePowerManagementBehavior : CampaignBehaviorBase
	{
		// Token: 0x0600429D RID: 17053 RVA: 0x00141030 File Offset: 0x0013F230
		public override void RegisterEvents()
		{
			CampaignEvents.HeroCreated.AddNonSerializedListener(this, new Action<Hero, bool>(this.OnHeroCreated));
			CampaignEvents.DailyTickHeroEvent.AddNonSerializedListener(this, new Action<Hero>(this.DailyTickHero));
			CampaignEvents.RaidCompletedEvent.AddNonSerializedListener(this, new Action<BattleSideEnum, RaidEventComponent>(this.OnRaidCompleted));
		}

		// Token: 0x0600429E RID: 17054 RVA: 0x00141082 File Offset: 0x0013F282
		private void OnHeroCreated(Hero hero, bool isMaternal)
		{
			if (hero.IsNotable)
			{
				hero.AddPower((float)Campaign.Current.Models.NotablePowerModel.GetInitialPower(hero));
			}
		}

		// Token: 0x0600429F RID: 17055 RVA: 0x001410A8 File Offset: 0x0013F2A8
		private void DailyTickHero(Hero hero)
		{
			if (hero.IsAlive && hero.IsNotable)
			{
				hero.AddPower(Campaign.Current.Models.NotablePowerModel.CalculateDailyPowerChangeForHero(hero, false).ResultNumber);
				this.BalanceGoldAndPowerOfNotable(hero);
			}
		}

		// Token: 0x060042A0 RID: 17056 RVA: 0x001410F0 File Offset: 0x0013F2F0
		private void OnRaidCompleted(BattleSideEnum winnerSide, RaidEventComponent mapEvent)
		{
			foreach (Hero hero in mapEvent.MapEventSettlement.Notables)
			{
				hero.AddPower(-5f);
			}
		}

		// Token: 0x060042A1 RID: 17057 RVA: 0x0014114C File Offset: 0x0013F34C
		private void BalanceGoldAndPowerOfNotable(Hero notable)
		{
			if (notable.Gold > 10500)
			{
				int num = (notable.Gold - 10000) / 500;
				GiveGoldAction.ApplyBetweenCharacters(notable, null, num * 500, true);
				notable.AddPower((float)num);
				return;
			}
			if (notable.Gold < 4500 && notable.Power > 0f)
			{
				int num2 = (5000 - notable.Gold) / 500;
				GiveGoldAction.ApplyBetweenCharacters(null, notable, num2 * 500, true);
				notable.AddPower((float)(-(float)num2));
			}
		}

		// Token: 0x060042A2 RID: 17058 RVA: 0x001411D6 File Offset: 0x0013F3D6
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x04001305 RID: 4869
		private const int GoldLimitForNotablesToStartGainingPower = 10000;

		// Token: 0x04001306 RID: 4870
		private const int GoldLimitForNotablesToStartLosingPower = 5000;

		// Token: 0x04001307 RID: 4871
		private const int GoldNeededToGainOnePower = 500;
	}
}
