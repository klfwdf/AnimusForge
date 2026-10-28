namespace TaleWorlds.CampaignSystem.Actions;

public static class ChangeRomanticStateAction
{
	private static void ApplyInternal(Hero hero1, Hero hero2, Romance.RomanceLevelEnum toWhat)
	{
		Romance.SetRomanticState(hero1, hero2, toWhat);
		CampaignEventDispatcher.Instance.OnRomanticStateChanged(hero1, hero2, toWhat);
	}

	public static void Apply(Hero person1, Hero person2, Romance.RomanceLevelEnum toWhat)
	{
		ApplyInternal(person1, person2, toWhat);
	}
}
