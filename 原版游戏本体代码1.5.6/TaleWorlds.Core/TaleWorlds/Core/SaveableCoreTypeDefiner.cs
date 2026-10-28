using System;
using System.Collections.Generic;
using TaleWorlds.Core.SaveCompability;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.Core;

public class SaveableCoreTypeDefiner : SaveableTypeDefiner
{
	public SaveableCoreTypeDefiner()
		: base(10000)
	{
	}

	protected override void DefineClassTypes()
	{
		AddClassDefinition(typeof(ArmorComponent), 2);
		AddClassDefinition(typeof(Banner), 3);
		AddClassDefinition(typeof(BannerData), 4);
		AddClassDefinition(typeof(BasicCharacterObject), 5);
		AddClassDefinition(typeof(CharacterAttribute), 6);
		AddClassDefinition(typeof(WeaponDesign), 9);
		AddClassDefinition(typeof(CraftingPiece), 10);
		AddClassDefinition(typeof(CraftingTemplate), 11);
		AddClassDefinition(typeof(EntitySystem<>), 15);
		AddClassDefinition(typeof(Equipment), 16);
		AddClassDefinition(typeof(TradeItemComponent), 18);
		AddClassDefinition(typeof(GameType), 26);
		AddClassDefinition(typeof(HorseComponent), 27);
		AddClassDefinition(typeof(ItemCategory), 28);
		AddClassDefinition(typeof(ItemComponent), 29);
		AddClassDefinition(typeof(ItemModifier), 30);
		AddClassDefinition(typeof(ItemModifierGroup), 31);
		AddClassDefinition(typeof(ItemObject), 32);
		AddClassDefinition(typeof(MissionResult), 36);
		AddClassDefinition(typeof(PropertyObject), 38);
		AddClassDefinition(typeof(SkillObject), 39);
		AddClassDefinition(typeof(PropertyOwner<>), 40);
		AddClassDefinition(typeof(PropertyOwnerF<>), 41);
		AddClassDefinition(typeof(SiegeEngineType), 42);
		AddClassDefinition(typeof(WeaponDesignElement), 44);
		AddClassDefinition(typeof(WeaponComponent), 45);
		AddClassDefinition(typeof(WeaponComponentData), 46);
		AddClassDefinition(typeof(InformationData), 50);
		AddClassDefinition(typeof(MBFastRandom), 52);
		AddClassDefinition(typeof(BannerComponent), 53);
		AddClassDefinition(typeof(ShipHull), 54);
		AddClassDefinition(typeof(ShipUpgradePiece), 55);
	}

	protected override void DefineStructTypes()
	{
		AddStructDefinition(typeof(ItemRosterElement), 1004);
		AddStructDefinition(typeof(UniqueTroopDescriptor), 1006);
		AddStructDefinition(typeof(StaticBodyProperties), 1009);
		AddStructDefinition(typeof(EquipmentElement), 1011);
		AddStructDefinition(typeof(AgentSaveData), 1012);
	}

	protected override void DefineEnumTypes()
	{
		AddEnumDefinition(typeof(BattleSideEnum), 2001);
		AddEnumDefinition(typeof(Equipment.EquipmentType), 2006);
		AddEnumDefinition(typeof(WeaponFlags), 2007);
		AddEnumDefinition(typeof(FormationClass), 2008);
		AddEnumDefinition(typeof(BattleState), 2009);
	}

	protected override void DefineInterfaceTypes()
	{
	}

	protected override void DefineConflictResolvers()
	{
		AddConflictResolver(8, new CharacterSkillsResolver());
	}

	protected override void DefineRootClassTypes()
	{
		AddRootClassDefinition(typeof(Game), 4001);
	}

	protected override void DefineGenericClassDefinitions()
	{
		ConstructGenericClassDefinition(typeof(Tuple<int, int>));
		ConstructGenericClassDefinition(typeof(PropertyOwner<SkillObject>));
		ConstructGenericClassDefinition(typeof(PropertyOwner<CharacterAttribute>));
	}

	protected override void DefineGenericStructDefinitions()
	{
	}

	protected override void DefineContainerDefinitions()
	{
		ConstructContainerDefinition(typeof(ItemRosterElement[]));
		ConstructContainerDefinition(typeof(EquipmentElement[]));
		ConstructContainerDefinition(typeof(Equipment[]));
		ConstructContainerDefinition(typeof(WeaponDesignElement[]));
		ConstructContainerDefinition(typeof(List<ItemObject>));
		ConstructContainerDefinition(typeof(List<ItemComponent>));
		ConstructContainerDefinition(typeof(List<ItemModifier>));
		ConstructContainerDefinition(typeof(List<ItemModifierGroup>));
		ConstructContainerDefinition(typeof(List<CharacterAttribute>));
		ConstructContainerDefinition(typeof(List<SkillObject>));
		ConstructContainerDefinition(typeof(List<ItemCategory>));
		ConstructContainerDefinition(typeof(List<CraftingPiece>));
		ConstructContainerDefinition(typeof(List<CraftingTemplate>));
		ConstructContainerDefinition(typeof(List<SiegeEngineType>));
		ConstructContainerDefinition(typeof(List<PropertyObject>));
		ConstructContainerDefinition(typeof(List<UniqueTroopDescriptor>));
		ConstructContainerDefinition(typeof(List<Equipment>));
		ConstructContainerDefinition(typeof(List<BannerData>));
		ConstructContainerDefinition(typeof(List<EquipmentElement>));
		ConstructContainerDefinition(typeof(List<WeaponDesign>));
		ConstructContainerDefinition(typeof(List<ItemRosterElement>));
		ConstructContainerDefinition(typeof(List<InformationData>));
		ConstructContainerDefinition(typeof(List<AgentSaveData>));
		ConstructContainerDefinition(typeof(List<BattleSideEnum>));
		ConstructContainerDefinition(typeof(List<ShipUpgradePiece>));
		ConstructContainerDefinition(typeof(List<ShipHull>));
		ConstructContainerDefinition(typeof(Dictionary<string, ItemCategory>));
		ConstructContainerDefinition(typeof(Dictionary<string, CraftingPiece>));
		ConstructContainerDefinition(typeof(Dictionary<string, CraftingTemplate>));
		ConstructContainerDefinition(typeof(Dictionary<string, SiegeEngineType>));
		ConstructContainerDefinition(typeof(Dictionary<string, PropertyObject>));
		ConstructContainerDefinition(typeof(Dictionary<string, SkillObject>));
		ConstructContainerDefinition(typeof(Dictionary<string, CharacterAttribute>));
		ConstructContainerDefinition(typeof(Dictionary<string, ItemModifierGroup>));
		ConstructContainerDefinition(typeof(Dictionary<string, ItemComponent>));
		ConstructContainerDefinition(typeof(Dictionary<string, ItemObject>));
		ConstructContainerDefinition(typeof(Dictionary<string, ItemModifier>));
		ConstructContainerDefinition(typeof(Dictionary<MBGUID, ItemCategory>));
		ConstructContainerDefinition(typeof(Dictionary<MBGUID, CraftingPiece>));
		ConstructContainerDefinition(typeof(Dictionary<MBGUID, CraftingTemplate>));
		ConstructContainerDefinition(typeof(Dictionary<MBGUID, SiegeEngineType>));
		ConstructContainerDefinition(typeof(Dictionary<MBGUID, PropertyObject>));
		ConstructContainerDefinition(typeof(Dictionary<MBGUID, SkillObject>));
		ConstructContainerDefinition(typeof(Dictionary<MBGUID, CharacterAttribute>));
		ConstructContainerDefinition(typeof(Dictionary<MBGUID, ItemModifierGroup>));
		ConstructContainerDefinition(typeof(Dictionary<MBGUID, ItemObject>));
		ConstructContainerDefinition(typeof(Dictionary<MBGUID, ItemComponent>));
		ConstructContainerDefinition(typeof(Dictionary<MBGUID, ItemModifier>));
		ConstructContainerDefinition(typeof(Dictionary<ItemCategory, float>));
		ConstructContainerDefinition(typeof(Dictionary<ItemCategory, int>));
		ConstructContainerDefinition(typeof(Dictionary<SiegeEngineType, int>));
		ConstructContainerDefinition(typeof(Dictionary<SkillObject, int>));
		ConstructContainerDefinition(typeof(Dictionary<PropertyObject, int>));
		ConstructContainerDefinition(typeof(Dictionary<PropertyObject, float>));
		ConstructContainerDefinition(typeof(Dictionary<ItemObject, int>));
		ConstructContainerDefinition(typeof(Dictionary<ItemObject, float>));
		ConstructContainerDefinition(typeof(Dictionary<CharacterAttribute, int>));
		ConstructContainerDefinition(typeof(Dictionary<CraftingTemplate, List<CraftingPiece>>));
		ConstructContainerDefinition(typeof(Dictionary<CraftingTemplate, float>));
		ConstructContainerDefinition(typeof(Dictionary<long, Dictionary<long, int>>));
		ConstructContainerDefinition(typeof(Dictionary<int, Tuple<int, int>>));
		ConstructContainerDefinition(typeof(Dictionary<EquipmentElement, int>));
		ConstructContainerDefinition(typeof(Dictionary<string, ShipUpgradePiece>));
	}
}
