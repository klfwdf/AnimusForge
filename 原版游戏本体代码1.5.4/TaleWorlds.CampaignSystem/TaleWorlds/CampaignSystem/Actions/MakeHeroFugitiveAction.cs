namespace TaleWorlds.CampaignSystem.Actions;

public static class MakeHeroFugitiveAction
{
	private static void ApplyInternal(Hero fugitive, bool showNotification)
	{
		if (!fugitive.IsAlive)
		{
			return;
		}
		if (fugitive.PartyBelongedTo != null)
		{
			if (fugitive.PartyBelongedTo.LeaderHero == fugitive)
			{
				DestroyPartyAction.Apply(null, fugitive.PartyBelongedTo);
			}
			else
			{
				fugitive.PartyBelongedTo.MemberRoster.RemoveTroop(fugitive.CharacterObject);
			}
		}
		if (fugitive.CurrentSettlement != null)
		{
			LeaveSettlementAction.ApplyForCharacterOnly(fugitive);
		}
		fugitive.ChangeState(Hero.CharacterStates.Fugitive);
		CampaignEventDispatcher.Instance.OnCharacterBecameFugitive(fugitive, showNotification);
	}

	public static void Apply(Hero fugitive, bool showNotification = false)
	{
		ApplyInternal(fugitive, showNotification);
	}
}
