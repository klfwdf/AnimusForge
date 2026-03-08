using System;
using System.Collections.Generic;
using TaleWorlds.Core.SaveCompability;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.Core
{
	// Token: 0x020000C9 RID: 201
	public class SaveableCoreTypeDefiner : SaveableTypeDefiner
	{
		// Token: 0x06000AD3 RID: 2771 RVA: 0x00022B74 File Offset: 0x00020D74
		public SaveableCoreTypeDefiner()
			: base(10000)
		{
		}

		// Token: 0x06000AD4 RID: 2772 RVA: 0x00022B84 File Offset: 0x00020D84
		protected override void DefineClassTypes()
		{
			base.AddClassDefinition(typeof(ArmorComponent), 2, null);
			base.AddClassDefinition(typeof(Banner), 3, null);
			base.AddClassDefinition(typeof(BannerData), 4, null);
			base.AddClassDefinition(typeof(BasicCharacterObject), 5, null);
			base.AddClassDefinition(typeof(CharacterAttribute), 6, null);
			base.AddClassDefinition(typeof(WeaponDesign), 9, null);
			base.AddClassDefinition(typeof(CraftingPiece), 10, null);
			base.AddClassDefinition(typeof(CraftingTemplate), 11, null);
			base.AddClassDefinition(typeof(EntitySystem<>), 15, null);
			base.AddClassDefinition(typeof(Equipment), 16, null);
			base.AddClassDefinition(typeof(TradeItemComponent), 18, null);
			base.AddClassDefinition(typeof(GameType), 26, null);
			base.AddClassDefinition(typeof(HorseComponent), 27, null);
			base.AddClassDefinition(typeof(ItemCategory), 28, null);
			base.AddClassDefinition(typeof(ItemComponent), 29, null);
			base.AddClassDefinition(typeof(ItemModifier), 30, null);
			base.AddClassDefinition(typeof(ItemModifierGroup), 31, null);
			base.AddClassDefinition(typeof(ItemObject), 32, null);
			base.AddClassDefinition(typeof(MissionResult), 36, null);
			base.AddClassDefinition(typeof(PropertyObject), 38, null);
			base.AddClassDefinition(typeof(SkillObject), 39, null);
			base.AddClassDefinition(typeof(PropertyOwner<>), 40, null);
			base.AddClassDefinition(typeof(PropertyOwnerF<>), 41, null);
			base.AddClassDefinition(typeof(SiegeEngineType), 42, null);
			base.AddClassDefinition(typeof(WeaponDesignElement), 44, null);
			base.AddClassDefinition(typeof(WeaponComponent), 45, null);
			base.AddClassDefinition(typeof(WeaponComponentData), 46, null);
			base.AddClassDefinition(typeof(InformationData), 50, null);
			base.AddClassDefinition(typeof(MBFastRandom), 52, null);
			base.AddClassDefinition(typeof(BannerComponent), 53, null);
			base.AddClassDefinition(typeof(ShipHull), 54, null);
			base.AddClassDefinition(typeof(ShipUpgradePiece), 55, null);
		}

		// Token: 0x06000AD5 RID: 2773 RVA: 0x00022DEC File Offset: 0x00020FEC
		protected override void DefineStructTypes()
		{
			base.AddStructDefinition(typeof(ItemRosterElement), 1004, null);
			base.AddStructDefinition(typeof(UniqueTroopDescriptor), 1006, null);
			base.AddStructDefinition(typeof(StaticBodyProperties), 1009, null);
			base.AddStructDefinition(typeof(EquipmentElement), 1011, null);
			base.AddStructDefinition(typeof(AgentSaveData), 1012, null);
		}

		// Token: 0x06000AD6 RID: 2774 RVA: 0x00022E68 File Offset: 0x00021068
		protected override void DefineEnumTypes()
		{
			base.AddEnumDefinition(typeof(BattleSideEnum), 2001, null);
			base.AddEnumDefinition(typeof(Equipment.EquipmentType), 2006, null);
			base.AddEnumDefinition(typeof(WeaponFlags), 2007, null);
			base.AddEnumDefinition(typeof(FormationClass), 2008, null);
			base.AddEnumDefinition(typeof(BattleState), 2009, null);
		}

		// Token: 0x06000AD7 RID: 2775 RVA: 0x00022EE3 File Offset: 0x000210E3
		protected override void DefineInterfaceTypes()
		{
		}

		// Token: 0x06000AD8 RID: 2776 RVA: 0x00022EE5 File Offset: 0x000210E5
		protected override void DefineConflictResolvers()
		{
			base.AddConflictResolver(8, new CharacterSkillsResolver());
		}

		// Token: 0x06000AD9 RID: 2777 RVA: 0x00022EF3 File Offset: 0x000210F3
		protected override void DefineRootClassTypes()
		{
			base.AddRootClassDefinition(typeof(Game), 4001, null);
		}

		// Token: 0x06000ADA RID: 2778 RVA: 0x00022F0B File Offset: 0x0002110B
		protected override void DefineGenericClassDefinitions()
		{
			base.ConstructGenericClassDefinition(typeof(Tuple<int, int>));
			base.ConstructGenericClassDefinition(typeof(PropertyOwner<SkillObject>));
			base.ConstructGenericClassDefinition(typeof(PropertyOwner<CharacterAttribute>));
		}

		// Token: 0x06000ADB RID: 2779 RVA: 0x00022F3D File Offset: 0x0002113D
		protected override void DefineGenericStructDefinitions()
		{
		}

		// Token: 0x06000ADC RID: 2780 RVA: 0x00022F40 File Offset: 0x00021140
		protected override void DefineContainerDefinitions()
		{
			base.ConstructContainerDefinition(typeof(ItemRosterElement[]));
			base.ConstructContainerDefinition(typeof(EquipmentElement[]));
			base.ConstructContainerDefinition(typeof(Equipment[]));
			base.ConstructContainerDefinition(typeof(WeaponDesignElement[]));
			base.ConstructContainerDefinition(typeof(List<ItemObject>));
			base.ConstructContainerDefinition(typeof(List<ItemComponent>));
			base.ConstructContainerDefinition(typeof(List<ItemModifier>));
			base.ConstructContainerDefinition(typeof(List<ItemModifierGroup>));
			base.ConstructContainerDefinition(typeof(List<CharacterAttribute>));
			base.ConstructContainerDefinition(typeof(List<SkillObject>));
			base.ConstructContainerDefinition(typeof(List<ItemCategory>));
			base.ConstructContainerDefinition(typeof(List<CraftingPiece>));
			base.ConstructContainerDefinition(typeof(List<CraftingTemplate>));
			base.ConstructContainerDefinition(typeof(List<SiegeEngineType>));
			base.ConstructContainerDefinition(typeof(List<PropertyObject>));
			base.ConstructContainerDefinition(typeof(List<UniqueTroopDescriptor>));
			base.ConstructContainerDefinition(typeof(List<Equipment>));
			base.ConstructContainerDefinition(typeof(List<BannerData>));
			base.ConstructContainerDefinition(typeof(List<EquipmentElement>));
			base.ConstructContainerDefinition(typeof(List<WeaponDesign>));
			base.ConstructContainerDefinition(typeof(List<ItemRosterElement>));
			base.ConstructContainerDefinition(typeof(List<InformationData>));
			base.ConstructContainerDefinition(typeof(List<AgentSaveData>));
			base.ConstructContainerDefinition(typeof(List<BattleSideEnum>));
			base.ConstructContainerDefinition(typeof(List<ShipUpgradePiece>));
			base.ConstructContainerDefinition(typeof(List<ShipHull>));
			base.ConstructContainerDefinition(typeof(Dictionary<string, ItemCategory>));
			base.ConstructContainerDefinition(typeof(Dictionary<string, CraftingPiece>));
			base.ConstructContainerDefinition(typeof(Dictionary<string, CraftingTemplate>));
			base.ConstructContainerDefinition(typeof(Dictionary<string, SiegeEngineType>));
			base.ConstructContainerDefinition(typeof(Dictionary<string, PropertyObject>));
			base.ConstructContainerDefinition(typeof(Dictionary<string, SkillObject>));
			base.ConstructContainerDefinition(typeof(Dictionary<string, CharacterAttribute>));
			base.ConstructContainerDefinition(typeof(Dictionary<string, ItemModifierGroup>));
			base.ConstructContainerDefinition(typeof(Dictionary<string, ItemComponent>));
			base.ConstructContainerDefinition(typeof(Dictionary<string, ItemObject>));
			base.ConstructContainerDefinition(typeof(Dictionary<string, ItemModifier>));
			base.ConstructContainerDefinition(typeof(Dictionary<MBGUID, ItemCategory>));
			base.ConstructContainerDefinition(typeof(Dictionary<MBGUID, CraftingPiece>));
			base.ConstructContainerDefinition(typeof(Dictionary<MBGUID, CraftingTemplate>));
			base.ConstructContainerDefinition(typeof(Dictionary<MBGUID, SiegeEngineType>));
			base.ConstructContainerDefinition(typeof(Dictionary<MBGUID, PropertyObject>));
			base.ConstructContainerDefinition(typeof(Dictionary<MBGUID, SkillObject>));
			base.ConstructContainerDefinition(typeof(Dictionary<MBGUID, CharacterAttribute>));
			base.ConstructContainerDefinition(typeof(Dictionary<MBGUID, ItemModifierGroup>));
			base.ConstructContainerDefinition(typeof(Dictionary<MBGUID, ItemObject>));
			base.ConstructContainerDefinition(typeof(Dictionary<MBGUID, ItemComponent>));
			base.ConstructContainerDefinition(typeof(Dictionary<MBGUID, ItemModifier>));
			base.ConstructContainerDefinition(typeof(Dictionary<ItemCategory, float>));
			base.ConstructContainerDefinition(typeof(Dictionary<ItemCategory, int>));
			base.ConstructContainerDefinition(typeof(Dictionary<SiegeEngineType, int>));
			base.ConstructContainerDefinition(typeof(Dictionary<SkillObject, int>));
			base.ConstructContainerDefinition(typeof(Dictionary<PropertyObject, int>));
			base.ConstructContainerDefinition(typeof(Dictionary<PropertyObject, float>));
			base.ConstructContainerDefinition(typeof(Dictionary<ItemObject, int>));
			base.ConstructContainerDefinition(typeof(Dictionary<ItemObject, float>));
			base.ConstructContainerDefinition(typeof(Dictionary<CharacterAttribute, int>));
			base.ConstructContainerDefinition(typeof(Dictionary<CraftingTemplate, List<CraftingPiece>>));
			base.ConstructContainerDefinition(typeof(Dictionary<CraftingTemplate, float>));
			base.ConstructContainerDefinition(typeof(Dictionary<long, Dictionary<long, int>>));
			base.ConstructContainerDefinition(typeof(Dictionary<int, Tuple<int, int>>));
			base.ConstructContainerDefinition(typeof(Dictionary<EquipmentElement, int>));
			base.ConstructContainerDefinition(typeof(Dictionary<string, ShipUpgradePiece>));
		}
	}
}
