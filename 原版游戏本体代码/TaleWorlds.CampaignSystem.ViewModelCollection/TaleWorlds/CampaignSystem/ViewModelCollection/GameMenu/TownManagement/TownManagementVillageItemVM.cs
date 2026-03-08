using System;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.TownManagement
{
	// Token: 0x020000AB RID: 171
	public class TownManagementVillageItemVM : ViewModel
	{
		// Token: 0x0600104E RID: 4174 RVA: 0x0004247C File Offset: 0x0004067C
		public TownManagementVillageItemVM(Village village)
		{
			this._village = village;
			this.Background = village.Settlement.SettlementComponent.BackgroundMeshName + "_t";
			this.VillageType = (int)this.DetermineVillageType(village.VillageType);
			this.RefreshValues();
		}

		// Token: 0x0600104F RID: 4175 RVA: 0x000424CE File Offset: 0x000406CE
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.Name = this._village.Name.ToString();
			this.ProductionName = this._village.VillageType.PrimaryProduction.Name.ToString();
		}

		// Token: 0x06001050 RID: 4176 RVA: 0x0004250C File Offset: 0x0004070C
		public void ExecuteShowTooltip()
		{
			InformationManager.ShowTooltip(typeof(Settlement), new object[] { this._village.Settlement });
		}

		// Token: 0x06001051 RID: 4177 RVA: 0x00042531 File Offset: 0x00040731
		public void ExecuteHideTooltip()
		{
			MBInformationManager.HideInformations();
		}

		// Token: 0x06001052 RID: 4178 RVA: 0x00042538 File Offset: 0x00040738
		private TownManagementVillageItemVM.VillageTypes DetermineVillageType(VillageType village)
		{
			if (village == DefaultVillageTypes.EuropeHorseRanch)
			{
				return TownManagementVillageItemVM.VillageTypes.EuropeHorseRanch;
			}
			if (village == DefaultVillageTypes.BattanianHorseRanch)
			{
				return TownManagementVillageItemVM.VillageTypes.BattanianHorseRanch;
			}
			if (village == DefaultVillageTypes.SteppeHorseRanch)
			{
				return TownManagementVillageItemVM.VillageTypes.SteppeHorseRanch;
			}
			if (village == DefaultVillageTypes.DesertHorseRanch)
			{
				return TownManagementVillageItemVM.VillageTypes.DesertHorseRanch;
			}
			if (village == DefaultVillageTypes.WheatFarm)
			{
				return TownManagementVillageItemVM.VillageTypes.WheatFarm;
			}
			if (village == DefaultVillageTypes.Lumberjack)
			{
				return TownManagementVillageItemVM.VillageTypes.Lumberjack;
			}
			if (village == DefaultVillageTypes.ClayMine)
			{
				return TownManagementVillageItemVM.VillageTypes.ClayMine;
			}
			if (village == DefaultVillageTypes.SaltMine)
			{
				return TownManagementVillageItemVM.VillageTypes.SaltMine;
			}
			if (village == DefaultVillageTypes.IronMine)
			{
				return TownManagementVillageItemVM.VillageTypes.IronMine;
			}
			if (village == DefaultVillageTypes.Fisherman)
			{
				return TownManagementVillageItemVM.VillageTypes.Fisherman;
			}
			if (village == DefaultVillageTypes.CattleRange)
			{
				return TownManagementVillageItemVM.VillageTypes.CattleRange;
			}
			if (village == DefaultVillageTypes.SheepFarm)
			{
				return TownManagementVillageItemVM.VillageTypes.SheepFarm;
			}
			if (village == DefaultVillageTypes.VineYard)
			{
				return TownManagementVillageItemVM.VillageTypes.VineYard;
			}
			if (village == DefaultVillageTypes.FlaxPlant)
			{
				return TownManagementVillageItemVM.VillageTypes.FlaxPlant;
			}
			if (village == DefaultVillageTypes.DateFarm)
			{
				return TownManagementVillageItemVM.VillageTypes.DateFarm;
			}
			if (village == DefaultVillageTypes.OliveTrees)
			{
				return TownManagementVillageItemVM.VillageTypes.OliveTrees;
			}
			if (village == DefaultVillageTypes.SilkPlant)
			{
				return TownManagementVillageItemVM.VillageTypes.SilkPlant;
			}
			if (village == DefaultVillageTypes.SilverMine)
			{
				return TownManagementVillageItemVM.VillageTypes.SilverMine;
			}
			return TownManagementVillageItemVM.VillageTypes.None;
		}

		// Token: 0x17000546 RID: 1350
		// (get) Token: 0x06001053 RID: 4179 RVA: 0x00042604 File Offset: 0x00040804
		// (set) Token: 0x06001054 RID: 4180 RVA: 0x0004260C File Offset: 0x0004080C
		[DataSourceProperty]
		public string Name
		{
			get
			{
				return this._name;
			}
			set
			{
				if (value != this._name)
				{
					this._name = value;
					base.OnPropertyChangedWithValue<string>(value, "Name");
				}
			}
		}

		// Token: 0x17000547 RID: 1351
		// (get) Token: 0x06001055 RID: 4181 RVA: 0x0004262F File Offset: 0x0004082F
		// (set) Token: 0x06001056 RID: 4182 RVA: 0x00042637 File Offset: 0x00040837
		[DataSourceProperty]
		public string ProductionName
		{
			get
			{
				return this._productionName;
			}
			set
			{
				if (value != this._productionName)
				{
					this._productionName = value;
					base.OnPropertyChangedWithValue<string>(value, "ProductionName");
				}
			}
		}

		// Token: 0x17000548 RID: 1352
		// (get) Token: 0x06001057 RID: 4183 RVA: 0x0004265A File Offset: 0x0004085A
		// (set) Token: 0x06001058 RID: 4184 RVA: 0x00042662 File Offset: 0x00040862
		[DataSourceProperty]
		public string Background
		{
			get
			{
				return this._background;
			}
			set
			{
				if (value != this._background)
				{
					this._background = value;
					base.OnPropertyChangedWithValue<string>(value, "Background");
				}
			}
		}

		// Token: 0x17000549 RID: 1353
		// (get) Token: 0x06001059 RID: 4185 RVA: 0x00042685 File Offset: 0x00040885
		// (set) Token: 0x0600105A RID: 4186 RVA: 0x0004268D File Offset: 0x0004088D
		[DataSourceProperty]
		public int VillageType
		{
			get
			{
				return this._villageType;
			}
			set
			{
				if (value != this._villageType)
				{
					this._villageType = value;
					base.OnPropertyChangedWithValue(value, "VillageType");
				}
			}
		}

		// Token: 0x04000773 RID: 1907
		private readonly Village _village;

		// Token: 0x04000774 RID: 1908
		private string _name;

		// Token: 0x04000775 RID: 1909
		private string _background;

		// Token: 0x04000776 RID: 1910
		private string _productionName;

		// Token: 0x04000777 RID: 1911
		private int _villageType;

		// Token: 0x0200021A RID: 538
		private enum VillageTypes
		{
			// Token: 0x040011BE RID: 4542
			None,
			// Token: 0x040011BF RID: 4543
			EuropeHorseRanch,
			// Token: 0x040011C0 RID: 4544
			BattanianHorseRanch,
			// Token: 0x040011C1 RID: 4545
			SteppeHorseRanch,
			// Token: 0x040011C2 RID: 4546
			DesertHorseRanch,
			// Token: 0x040011C3 RID: 4547
			WheatFarm,
			// Token: 0x040011C4 RID: 4548
			Lumberjack,
			// Token: 0x040011C5 RID: 4549
			ClayMine,
			// Token: 0x040011C6 RID: 4550
			SaltMine,
			// Token: 0x040011C7 RID: 4551
			IronMine,
			// Token: 0x040011C8 RID: 4552
			Fisherman,
			// Token: 0x040011C9 RID: 4553
			CattleRange,
			// Token: 0x040011CA RID: 4554
			SheepFarm,
			// Token: 0x040011CB RID: 4555
			VineYard,
			// Token: 0x040011CC RID: 4556
			FlaxPlant,
			// Token: 0x040011CD RID: 4557
			DateFarm,
			// Token: 0x040011CE RID: 4558
			OliveTrees,
			// Token: 0x040011CF RID: 4559
			SilkPlant,
			// Token: 0x040011D0 RID: 4560
			SilverMine
		}
	}
}
