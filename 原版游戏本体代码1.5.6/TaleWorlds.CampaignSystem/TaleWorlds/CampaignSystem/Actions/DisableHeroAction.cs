namespace TaleWorlds.CampaignSystem.Actions;

public static class DisableHeroAction
{
	private static void ApplyInternal(Hero hero)
	{
		if (!hero.IsAlive)
		{
			return;
		}
		if (hero.PartyBelongedTo != null)
		{
			if (hero.PartyBelongedTo.LeaderHero == hero)
			{
				DestroyPartyAction.Apply(null, hero.PartyBelongedTo);
			}
			else
			{
				hero.PartyBelongedTo.MemberRoster.RemoveTroop(hero.CharacterObject);
			}
		}
		if (hero.StayingInSettlement != null)
		{
			hero.ChangeState(Hero.CharacterStates.Disabled);
			hero.StayingInSettlement = null;
		}
		if (hero.CurrentSettlement != null)
		{
			LeaveSettlementAction.ApplyForCharacterOnly(hero);
		}
		if (hero.IsPrisoner)
		{
			EndCaptivityAction.ApplyByEscape(hero);
		}
		hero.ChangeState(Hero.CharacterStates.Disabled);
	}

	public static void Apply(Hero hero)
	{
		ApplyInternal(hero);
	}
}
