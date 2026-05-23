using TaleWorlds.CampaignSystem.Party;

namespace TaleWorlds.CampaignSystem.Actions;

public static class TransferPrisonerAction
{
	private static void ApplyInternal(CharacterObject prisonerTroop, PartyBase prisonerOwnerParty, PartyBase newParty)
	{
		if (prisonerTroop.HeroObject == Hero.MainHero)
		{
			PlayerCaptivity.CaptorParty = newParty;
			return;
		}
		prisonerOwnerParty.PrisonRoster.AddToCounts(prisonerTroop, -1);
		newParty.AddPrisoner(prisonerTroop, 1);
	}

	public static void Apply(CharacterObject prisonerTroop, PartyBase prisonerOwnerParty, PartyBase newParty)
	{
		ApplyInternal(prisonerTroop, prisonerOwnerParty, newParty);
	}
}
