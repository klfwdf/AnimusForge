namespace TaleWorlds.CampaignSystem.Actions;

public static class AdoptHeroAction
{
	private static void ApplyInternal(Hero adoptedHero)
	{
		if (Hero.MainHero.IsFemale)
		{
			adoptedHero.Mother = Hero.MainHero;
		}
		else
		{
			adoptedHero.Father = Hero.MainHero;
		}
		adoptedHero.Clan = Clan.PlayerClan;
	}

	public static void Apply(Hero adoptedHero)
	{
		ApplyInternal(adoptedHero);
	}
}
