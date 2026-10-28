namespace TaleWorlds.CampaignSystem.Actions;

public static class MakePregnantAction
{
	private static void ApplyInternal(Hero mother)
	{
		mother.IsPregnant = true;
		CampaignEventDispatcher.Instance.OnChildConceived(mother);
	}

	public static void Apply(Hero mother)
	{
		ApplyInternal(mother);
	}
}
