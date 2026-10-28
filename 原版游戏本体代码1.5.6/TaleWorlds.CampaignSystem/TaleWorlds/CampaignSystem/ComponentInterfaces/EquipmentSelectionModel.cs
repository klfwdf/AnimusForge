using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class EquipmentSelectionModel : MBGameModel<EquipmentSelectionModel>
{
	public abstract Equipment GetEquipmentForHeroComeOfAge(Hero hero, Equipment.EquipmentType equipmentType);

	public abstract Equipment GetEquipmentForHeroReachesTeenAge(Hero hero);

	public abstract Equipment GetEquipmentForInitialChildrenGeneration(Hero hero);

	public abstract Equipment GetEquipmentForDeliveredOffspring(Hero hero);

	public abstract (Equipment, Equipment) GetEquipmentsForChangingRuler(Hero newRuler, Hero oldRuler, Equipment.EquipmentType equipmentType);

	public abstract Equipment GetEquipmentForCompanionWhenTurningToLord(Hero companionHero, Equipment.EquipmentType equipmentType);
}
