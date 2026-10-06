using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultCompanionHiringPriceCalculationModel : CompanionHiringPriceCalculationModel
{
	public override int GetCompanionHiringPrice(Hero companion)
	{
		ExplainedNumber stat = new ExplainedNumber(0f, includeDescriptions: false, null);
		Town town = companion.CurrentSettlement?.Town;
		if (town == null)
		{
			town = SettlementHelper.FindNearestTownToMobileParty(MobileParty.MainParty, MobileParty.NavigationType.All);
		}
		float num = 0f;
		for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumEquipmentSetSlots; equipmentIndex++)
		{
			EquipmentElement itemRosterElement = companion.CharacterObject.Equipment[equipmentIndex];
			if (itemRosterElement.Item != null)
			{
				num += (float)town.GetItemPrice(itemRosterElement);
			}
		}
		for (EquipmentIndex equipmentIndex2 = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex2 < EquipmentIndex.NumEquipmentSetSlots; equipmentIndex2++)
		{
			EquipmentElement itemRosterElement2 = companion.CharacterObject.FirstCivilianEquipment[equipmentIndex2];
			if (itemRosterElement2.Item != null)
			{
				num += (float)town.GetItemPrice(itemRosterElement2);
			}
		}
		stat.Add(num / 2f);
		stat.Add(companion.CharacterObject.Level * 10);
		if (Hero.MainHero.IsPartyLeader && Hero.MainHero.GetPerkValue(DefaultPerks.Steward.PaidInPromise))
		{
			stat.AddFactor(DefaultPerks.Steward.PaidInPromise.PrimaryBonus);
		}
		if (Hero.MainHero.PartyBelongedTo != null)
		{
			PerkHelper.AddPerkBonusForParty(DefaultPerks.Trade.GreatInvestor, Hero.MainHero.PartyBelongedTo, isPrimaryBonus: false, ref stat);
		}
		return (int)stat.ResultNumber;
	}
}
