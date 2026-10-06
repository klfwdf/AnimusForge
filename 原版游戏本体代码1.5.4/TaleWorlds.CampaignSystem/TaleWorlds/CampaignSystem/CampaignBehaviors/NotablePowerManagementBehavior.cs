using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class NotablePowerManagementBehavior : CampaignBehaviorBase
{
	private const int GoldLimitForNotablesToStartGainingPower = 10000;

	private const int GoldLimitForNotablesToStartLosingPower = 5000;

	private const int GoldNeededToGainOnePower = 500;

	public override void RegisterEvents()
	{
		CampaignEvents.HeroCreated.AddNonSerializedListener(this, OnHeroCreated);
		CampaignEvents.DailyTickHeroEvent.AddNonSerializedListener(this, DailyTickHero);
		CampaignEvents.RaidCompletedEvent.AddNonSerializedListener(this, OnRaidCompleted);
	}

	private void OnHeroCreated(Hero hero, bool isMaternal)
	{
		if (hero.IsNotable)
		{
			hero.AddPower(Campaign.Current.Models.NotablePowerModel.GetInitialPower(hero));
		}
	}

	private void DailyTickHero(Hero hero)
	{
		if (hero.IsAlive && hero.IsNotable)
		{
			hero.AddPower(Campaign.Current.Models.NotablePowerModel.CalculateDailyPowerChangeForHero(hero).ResultNumber);
			BalanceGoldAndPowerOfNotable(hero);
		}
	}

	private void OnRaidCompleted(BattleSideEnum winnerSide, RaidEventComponent mapEvent)
	{
		foreach (Hero notable in mapEvent.MapEventSettlement.Notables)
		{
			notable.AddPower(-5f);
		}
	}

	private void BalanceGoldAndPowerOfNotable(Hero notable)
	{
		if (notable.Gold > 10500)
		{
			int num = (notable.Gold - 10000) / 500;
			GiveGoldAction.ApplyBetweenCharacters(notable, null, num * 500, disableNotification: true);
			notable.AddPower(num);
		}
		else if (notable.Gold < 4500 && notable.Power > 0f)
		{
			int num2 = (5000 - notable.Gold) / 500;
			GiveGoldAction.ApplyBetweenCharacters(null, notable, num2 * 500, disableNotification: true);
			notable.AddPower(-num2);
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
	}
}
