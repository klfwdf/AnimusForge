using Helpers;
using NavalDLC.CharacterDevelopment;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace NavalDLC.GameComponents;

public class NavalDLCInventoryCapacityModel : InventoryCapacityModel
{
	private static readonly TextObject _textTroopMounts = new TextObject("{=GIlU4NXm}Troops' Mounts");

	private static readonly TextObject _textMountsAndPackAnimals = new TextObject("{=Sb1MKbvP}Mounts and Pack Animals");

	private static readonly TextObject _textLiveStocksAnimals = new TextObject("{=KxUgSAKi}Live Stock Animals");

	private static readonly TextObject _textItems = new TextObject("{=U7er3V9s}Items");

	private static readonly TextObject _textBaseNavalCapacity = new TextObject("{=7Q8ufo5X}Ships");

	private const float CustomMountWeight = 50f;

	private const float CustomPackAnimalWeight = 30f;

	private const float CustomLiveStockWeight = 20f;

	public override ExplainedNumber CalculateInventoryCapacity(MobileParty mobileParty, bool isCurrentlyAtSea, bool includeDescriptions = false, int additionalManOnFoot = 0, int additionalSpareMounts = 0, int additionalPackAnimals = 0, bool includeFollowers = false)
	{
		ExplainedNumber result = base.BaseModel.CalculateInventoryCapacity(mobileParty, isCurrentlyAtSea, includeDescriptions, additionalManOnFoot, additionalSpareMounts, additionalPackAnimals, includeFollowers);
		if (isCurrentlyAtSea)
		{
			float num = 0f;
			foreach (Ship ship in mobileParty.Ships)
			{
				num += ship.InventoryCapacity;
			}
			foreach (MobileParty attachedParty in mobileParty.AttachedParties)
			{
				foreach (Ship ship2 in attachedParty.Ships)
				{
					num += ship2.InventoryCapacity;
				}
			}
			result.Add(num, _textBaseNavalCapacity);
		}
		return result;
	}

	public override int GetItemAverageWeight()
	{
		return base.BaseModel.GetItemAverageWeight();
	}

	public override ExplainedNumber CalculateTotalWeightCarried(MobileParty mobileParty, bool isCurrentlyAtSea, bool includeDescriptions = false)
	{
		ExplainedNumber result = base.BaseModel.CalculateTotalWeightCarried(mobileParty, isCurrentlyAtSea, includeDescriptions);
		if (isCurrentlyAtSea)
		{
			foreach (TroopRosterElement item in mobileParty.MemberRoster.GetTroopRoster())
			{
				float num = 0f;
				if (!item.Character.IsHero && !item.Character.Equipment.Horse.IsEmpty)
				{
					num += 50f * (float)item.Number;
				}
				result.Add(num, _textTroopMounts);
			}
		}
		return result;
	}

	public override float GetItemEffectiveWeight(EquipmentElement equipmentElement, MobileParty mobileParty, bool isCurrentlyAtSea, out TextObject description)
	{
		if (isCurrentlyAtSea)
		{
			ItemObject item = equipmentElement.Item;
			ExplainedNumber stat;
			if (item.HasHorseComponent)
			{
				if (item.HorseComponent.IsMount)
				{
					stat = new ExplainedNumber(50f);
					description = _textMountsAndPackAnimals;
					PerkHelper.AddPerkBonusForParty(NavalPerks.Boatswain.NavalHorde, mobileParty, isPrimaryBonus: false, ref stat);
				}
				else if (item.HorseComponent.IsPackAnimal)
				{
					stat = new ExplainedNumber(30f);
					description = _textMountsAndPackAnimals;
					PerkHelper.AddPerkBonusForParty(NavalPerks.Boatswain.Optimization, mobileParty, isPrimaryBonus: false, ref stat);
				}
				else if (item.HorseComponent.IsLiveStock)
				{
					stat = new ExplainedNumber(20f);
					description = _textLiveStocksAnimals;
					PerkHelper.AddPerkBonusForParty(NavalPerks.Boatswain.Optimization, mobileParty, isPrimaryBonus: false, ref stat);
				}
				else
				{
					stat = new ExplainedNumber(equipmentElement.GetEquipmentElementWeight());
					description = _textItems;
				}
			}
			else
			{
				stat = new ExplainedNumber(equipmentElement.GetEquipmentElementWeight());
				description = _textItems;
			}
			if (item.IsTradeGood)
			{
				PerkHelper.AddPerkBonusForParty(NavalPerks.Boatswain.GildedPurse, mobileParty, isPrimaryBonus: false, ref stat);
			}
			return stat.ResultNumber;
		}
		return base.BaseModel.GetItemEffectiveWeight(equipmentElement, mobileParty, isCurrentlyAtSea, out description);
	}
}
