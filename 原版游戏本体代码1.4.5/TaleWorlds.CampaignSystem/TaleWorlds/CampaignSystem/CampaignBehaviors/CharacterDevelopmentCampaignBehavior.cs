namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class CharacterDevelopmentCampaignBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.DailyTickHeroEvent.AddNonSerializedListener(this, DailyTickHero);
		CampaignEvents.OnCharacterCreationIsOverEvent.AddNonSerializedListener(this, OnCharacterCreationIsOver);
		CampaignEvents.OnHeroActivatedEvent.AddNonSerializedListener(this, OnHeroActivated);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void DailyTickHero(Hero hero)
	{
		if (ShouldDevelopCharacterStats(hero))
		{
			hero.HeroDeveloper.DevelopCharacterStats();
		}
	}

	private void OnCharacterCreationIsOver()
	{
		if (!CampaignOptions.AutoAllocateClanMemberPerks)
		{
			return;
		}
		foreach (Hero aliveHero in Campaign.Current.AliveHeroes)
		{
			if (!aliveHero.IsChild && aliveHero.Clan == Clan.PlayerClan && aliveHero != Hero.MainHero)
			{
				aliveHero.HeroDeveloper.DevelopCharacterStats();
			}
		}
	}

	private void OnHeroActivated(Hero hero, Hero.CharacterStates previousState)
	{
		if (ShouldDevelopCharacterStats(hero))
		{
			hero.HeroDeveloper.DevelopCharacterStats();
		}
	}

	private bool ShouldDevelopCharacterStats(Hero hero)
	{
		if (!hero.IsChild && hero.IsAlive && (hero.Clan != Clan.PlayerClan || (hero != Hero.MainHero && CampaignOptions.AutoAllocateClanMemberPerks)))
		{
			return hero.PartyBelongedTo?.MapEvent == null;
		}
		return false;
	}
}
