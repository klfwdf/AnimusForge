using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x02000113 RID: 275
	public class DefaultEquipmentSelectionModel : EquipmentSelectionModel
	{
		// Token: 0x060017C1 RID: 6081 RVA: 0x00071334 File Offset: 0x0006F534
		public override MBList<MBEquipmentRoster> GetEquipmentRostersForHeroComeOfAge(Hero hero, bool isCivilian)
		{
			MBList<MBEquipmentRoster> mblist = new MBList<MBEquipmentRoster>();
			bool flag = !hero.IsNoncombatant;
			foreach (MBEquipmentRoster mbequipmentRoster in MBEquipmentRosterExtensions.All)
			{
				if (this.IsRosterAppropriateForHeroAsTemplate(mbequipmentRoster, hero, true, EquipmentFlags.IsNobleTemplate))
				{
					if (isCivilian)
					{
						if (flag)
						{
							if (mbequipmentRoster.HasEquipmentFlags(EquipmentFlags.IsCombatantTemplate | EquipmentFlags.IsCivilianTemplate))
							{
								mblist.Add(mbequipmentRoster);
							}
						}
						else if (mbequipmentRoster.HasEquipmentFlags(EquipmentFlags.IsNoncombatantTemplate))
						{
							mblist.Add(mbequipmentRoster);
						}
					}
					else if (mbequipmentRoster.HasEquipmentFlags(EquipmentFlags.IsMediumTemplate))
					{
						mblist.Add(mbequipmentRoster);
					}
				}
			}
			return mblist;
		}

		// Token: 0x060017C2 RID: 6082 RVA: 0x000713DC File Offset: 0x0006F5DC
		public override MBList<MBEquipmentRoster> GetEquipmentRostersForHeroReachesTeenAge(Hero hero)
		{
			EquipmentFlags suitableFlags = EquipmentFlags.IsNobleTemplate | EquipmentFlags.IsTeenagerEquipmentTemplate;
			MBList<MBEquipmentRoster> result = new MBList<MBEquipmentRoster>();
			this.AddEquipmentsToRoster(hero, suitableFlags, ref result, true);
			return result;
		}

		// Token: 0x060017C3 RID: 6083 RVA: 0x00071404 File Offset: 0x0006F604
		public override MBList<MBEquipmentRoster> GetEquipmentRostersForInitialChildrenGeneration(Hero hero)
		{
			bool flag = hero.Age < (float)Campaign.Current.Models.AgeModel.BecomeTeenagerAge;
			EquipmentFlags suitableFlags = EquipmentFlags.IsNobleTemplate | (flag ? EquipmentFlags.IsChildEquipmentTemplate : EquipmentFlags.IsTeenagerEquipmentTemplate);
			MBList<MBEquipmentRoster> result = new MBList<MBEquipmentRoster>();
			this.AddEquipmentsToRoster(hero, suitableFlags, ref result, true);
			return result;
		}

		// Token: 0x060017C4 RID: 6084 RVA: 0x00071454 File Offset: 0x0006F654
		public override MBList<MBEquipmentRoster> GetEquipmentRostersForDeliveredOffspring(Hero hero)
		{
			EquipmentFlags suitableFlags = EquipmentFlags.IsNobleTemplate | EquipmentFlags.IsChildEquipmentTemplate;
			MBList<MBEquipmentRoster> result = new MBList<MBEquipmentRoster>();
			this.AddEquipmentsToRoster(hero, suitableFlags, ref result, true);
			return result;
		}

		// Token: 0x060017C5 RID: 6085 RVA: 0x0007147C File Offset: 0x0006F67C
		public override MBList<MBEquipmentRoster> GetEquipmentRostersForCompanion(Hero hero, bool isCivilian)
		{
			EquipmentFlags suitableFlags = (isCivilian ? (EquipmentFlags.IsCivilianTemplate | EquipmentFlags.IsNobleTemplate) : (EquipmentFlags.IsNobleTemplate | EquipmentFlags.IsMediumTemplate));
			MBList<MBEquipmentRoster> result = new MBList<MBEquipmentRoster>();
			this.AddEquipmentsToRoster(hero, suitableFlags, ref result, isCivilian);
			return result;
		}

		// Token: 0x060017C6 RID: 6086 RVA: 0x000714A8 File Offset: 0x0006F6A8
		private bool IsRosterAppropriateForHeroAsTemplate(MBEquipmentRoster equipmentRoster, Hero hero, bool shouldMatchGender, EquipmentFlags customFlags = EquipmentFlags.None)
		{
			bool result = false;
			if (equipmentRoster.IsEquipmentTemplate() && (!shouldMatchGender || equipmentRoster.HasEquipmentFlags(EquipmentFlags.IsFemaleTemplate) == hero.IsFemale) && equipmentRoster.EquipmentCulture == hero.Culture && (customFlags == EquipmentFlags.None || equipmentRoster.HasEquipmentFlags(customFlags)))
			{
				bool flag = equipmentRoster.HasEquipmentFlags(EquipmentFlags.IsNomadTemplate) || equipmentRoster.HasEquipmentFlags(EquipmentFlags.IsWoodlandTemplate);
				bool flag2 = !hero.IsChild && (equipmentRoster.HasEquipmentFlags(EquipmentFlags.IsChildEquipmentTemplate) || equipmentRoster.HasEquipmentFlags(EquipmentFlags.IsTeenagerEquipmentTemplate));
				if (!flag && !flag2)
				{
					result = true;
				}
			}
			return result;
		}

		// Token: 0x060017C7 RID: 6087 RVA: 0x0007153C File Offset: 0x0006F73C
		private bool IsHeroCombatant(Hero hero)
		{
			return !hero.IsFemale || hero.Clan == Hero.MainHero.Clan || (hero.Mother != null && !hero.Mother.IsNoncombatant) || (hero.RandomIntWithSeed(17U, 0, 1) == 0 && hero.GetTraitLevel(DefaultTraits.Valor) == 1);
		}

		// Token: 0x060017C8 RID: 6088 RVA: 0x00071598 File Offset: 0x0006F798
		private void AddEquipmentsToRoster(Hero hero, EquipmentFlags suitableFlags, ref MBList<MBEquipmentRoster> roster, bool shouldMatchGender)
		{
			foreach (MBEquipmentRoster mbequipmentRoster in MBEquipmentRosterExtensions.All)
			{
				if (this.IsRosterAppropriateForHeroAsTemplate(mbequipmentRoster, hero, shouldMatchGender, suitableFlags))
				{
					roster.Add(mbequipmentRoster);
				}
			}
		}
	}
}
