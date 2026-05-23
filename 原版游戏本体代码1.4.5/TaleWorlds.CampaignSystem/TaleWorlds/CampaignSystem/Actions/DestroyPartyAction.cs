using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.Actions;

public static class DestroyPartyAction
{
	private static void ApplyInternal(PartyBase destroyerParty, MobileParty destroyedParty)
	{
		if (destroyedParty != MobileParty.MainParty)
		{
			if (!destroyedParty.IsActive)
			{
				Debug.Print("Trying to destroy an inactive party with id: " + destroyedParty.StringId);
				Debug.FailedAssert("destroyedParty.IsActive", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Actions\\DestroyPartyAction.cs", "ApplyInternal", 17);
			}
			if (destroyedParty.IsCaravan && destroyedParty.Party.Owner != null && destroyedParty.Party.Owner.GetPerkValue(DefaultPerks.Trade.InsurancePlans))
			{
				GiveGoldAction.ApplyBetweenCharacters(null, destroyedParty.Party.Owner, (int)DefaultPerks.Trade.InsurancePlans.PrimaryBonus);
			}
			CampaignEventDispatcher.Instance.OnMobilePartyDestroyed(destroyedParty, destroyerParty);
			CampaignEventDispatcher.Instance.OnMapInteractableDestroyed(destroyedParty.Party);
			destroyedParty.RemoveParty();
		}
	}

	public static void Apply(PartyBase destroyerParty, MobileParty destroyedParty)
	{
		ApplyInternal(destroyerParty, destroyedParty);
	}

	public static void ApplyForDisbanding(MobileParty disbandedParty, Settlement relatedSettlement)
	{
		if (disbandedParty.CurrentSettlement != null)
		{
			LeaveSettlementAction.ApplyForParty(disbandedParty);
		}
		CampaignEventDispatcher.Instance.OnPartyDisbanded(disbandedParty, relatedSettlement);
		ApplyInternal(null, disbandedParty);
	}
}
