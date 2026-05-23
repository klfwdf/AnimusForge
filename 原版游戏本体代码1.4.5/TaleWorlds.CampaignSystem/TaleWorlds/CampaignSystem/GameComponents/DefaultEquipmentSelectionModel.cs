using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultEquipmentSelectionModel : EquipmentSelectionModel
{
	public override Equipment GetEquipmentForHeroComeOfAge(Hero hero, Equipment.EquipmentType equipmentType)
	{
		EquipmentCategories customFlags = EquipmentCategories.IsLordTemplate;
		return GetSuitableEquipmentSet(hero, customFlags, equipmentType);
	}

	public override Equipment GetEquipmentForHeroReachesTeenAge(Hero hero)
	{
		EquipmentCategories customFlags = EquipmentCategories.IsLordTemplate | EquipmentCategories.IsTeenagerEquipmentTemplate;
		return GetSuitableEquipmentSet(hero, customFlags, Equipment.EquipmentType.Civilian);
	}

	public override Equipment GetEquipmentForDeliveredOffspring(Hero hero)
	{
		EquipmentCategories customFlags = EquipmentCategories.IsLordTemplate | EquipmentCategories.IsChildEquipmentTemplate;
		return GetSuitableEquipmentSet(hero, customFlags, Equipment.EquipmentType.Civilian);
	}

	public override Equipment GetEquipmentForCompanionWhenTurningToLord(Hero companionHero, Equipment.EquipmentType equipmentType)
	{
		EquipmentCategories customFlags = EquipmentCategories.IsLordTemplate;
		return GetSuitableEquipmentSet(companionHero, customFlags, equipmentType);
	}

	public override Equipment GetEquipmentForInitialChildrenGeneration(Hero hero)
	{
		bool num = hero.Age < (float)Campaign.Current.Models.AgeModel.BecomeTeenagerAge;
		EquipmentCategories equipmentCategories = EquipmentCategories.IsLordTemplate;
		equipmentCategories = ((!num) ? (equipmentCategories | EquipmentCategories.IsTeenagerEquipmentTemplate) : (equipmentCategories | EquipmentCategories.IsChildEquipmentTemplate));
		return GetSuitableEquipmentSet(hero, equipmentCategories, Equipment.EquipmentType.Civilian);
	}

	public override (Equipment, Equipment) GetEquipmentsForChangingRuler(Hero newRuler, Hero oldRuler, Equipment.EquipmentType equipmentType)
	{
		Equipment item = null;
		Equipment item2 = null;
		if (newRuler != Hero.MainHero)
		{
			item = GetSuitableEquipmentSet(newRuler, EquipmentCategories.IsKingdomRulerTemplate, equipmentType);
		}
		if (oldRuler != null && oldRuler.IsActive && oldRuler != Hero.MainHero)
		{
			item2 = GetSuitableEquipmentSet(oldRuler, EquipmentCategories.IsLordTemplate, equipmentType);
		}
		return (item, item2);
	}

	private Equipment GetSuitableEquipmentSet(Hero hero, EquipmentCategories customFlags, Equipment.EquipmentType equipmentType)
	{
		MBList<Equipment> mBList = new MBList<Equipment>();
		if (hero.IsFemale)
		{
			customFlags |= EquipmentCategories.IsFemaleTemplate;
		}
		foreach (MBEquipmentRoster item in MBEquipmentRosterExtensions.All)
		{
			if (!IsRosterAppropriateForHeroAsTemplate(item, hero.Culture, customFlags))
			{
				continue;
			}
			foreach (Equipment allEquipment in item.AllEquipments)
			{
				if (allEquipment.ItemEquipmentType == equipmentType)
				{
					mBList.Add(allEquipment);
				}
			}
		}
		return mBList.GetRandomElement();
	}

	private bool IsRosterAppropriateForHeroAsTemplate(MBEquipmentRoster equipmentRoster, CultureObject culture, EquipmentCategories customFlags)
	{
		bool result = false;
		if (equipmentRoster.EquipmentCulture == culture && equipmentRoster.EquipmentCategories == customFlags)
		{
			result = true;
		}
		return result;
	}
}
