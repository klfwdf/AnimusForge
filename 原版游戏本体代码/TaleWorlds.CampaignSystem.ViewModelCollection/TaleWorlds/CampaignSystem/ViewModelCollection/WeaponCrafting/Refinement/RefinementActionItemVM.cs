using System;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.Refinement
{
	// Token: 0x02000114 RID: 276
	public class RefinementActionItemVM : ViewModel
	{
		// Token: 0x17000872 RID: 2162
		// (get) Token: 0x0600192F RID: 6447 RVA: 0x0005F6B4 File Offset: 0x0005D8B4
		public Crafting.RefiningFormula RefineFormula { get; }

		// Token: 0x06001930 RID: 6448 RVA: 0x0005F6BC File Offset: 0x0005D8BC
		public RefinementActionItemVM(Crafting.RefiningFormula refineFormula, Action<RefinementActionItemVM> onSelect)
		{
			this._onSelect = onSelect;
			this.RefineFormula = refineFormula;
			this.InputMaterials = new MBBindingList<CraftingResourceItemVM>();
			this.OutputMaterials = new MBBindingList<CraftingResourceItemVM>();
			SmithingModel smithingModel = Campaign.Current.Models.SmithingModel;
			if (this.RefineFormula.Input1Count > 0)
			{
				this.InputMaterials.Add(new CraftingResourceItemVM(this.RefineFormula.Input1, this.RefineFormula.Input1Count, 0));
			}
			if (this.RefineFormula.Input2Count > 0)
			{
				this.InputMaterials.Add(new CraftingResourceItemVM(this.RefineFormula.Input2, this.RefineFormula.Input2Count, 0));
			}
			if (this.RefineFormula.OutputCount > 0)
			{
				this.OutputMaterials.Add(new CraftingResourceItemVM(this.RefineFormula.Output, this.RefineFormula.OutputCount, 0));
			}
			if (this.RefineFormula.Output2Count > 0)
			{
				this.OutputMaterials.Add(new CraftingResourceItemVM(this.RefineFormula.Output2, this.RefineFormula.Output2Count, 0));
			}
			this.RefreshDynamicProperties();
			this.RefreshValues();
		}

		// Token: 0x06001931 RID: 6449 RVA: 0x0005F7E4 File Offset: 0x0005D9E4
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.InputMaterials.ApplyActionOnAllItems(delegate(CraftingResourceItemVM m)
			{
				m.RefreshValues();
			});
			this.OutputMaterials.ApplyActionOnAllItems(delegate(CraftingResourceItemVM m)
			{
				m.RefreshValues();
			});
		}

		// Token: 0x06001932 RID: 6450 RVA: 0x0005F84B File Offset: 0x0005DA4B
		public void RefreshDynamicProperties()
		{
			this.IsEnabled = this.UpdateInputAvailabilities();
		}

		// Token: 0x06001933 RID: 6451 RVA: 0x0005F85C File Offset: 0x0005DA5C
		private bool UpdateInputAvailabilities()
		{
			bool result = true;
			ItemRoster itemRoster = MobileParty.MainParty.ItemRoster;
			foreach (CraftingResourceItemVM craftingResourceItemVM in this.InputMaterials)
			{
				if (itemRoster.GetItemNumber(craftingResourceItemVM.ResourceItem) < craftingResourceItemVM.ResourceAmount)
				{
					result = false;
					craftingResourceItemVM.IsResourceAvailable = false;
				}
				else
				{
					craftingResourceItemVM.IsResourceAvailable = true;
				}
			}
			return result;
		}

		// Token: 0x06001934 RID: 6452 RVA: 0x0005F8D8 File Offset: 0x0005DAD8
		public void ExecuteSelectAction()
		{
			this._onSelect(this);
		}

		// Token: 0x17000873 RID: 2163
		// (get) Token: 0x06001935 RID: 6453 RVA: 0x0005F8E6 File Offset: 0x0005DAE6
		// (set) Token: 0x06001936 RID: 6454 RVA: 0x0005F8EE File Offset: 0x0005DAEE
		[DataSourceProperty]
		public MBBindingList<CraftingResourceItemVM> InputMaterials
		{
			get
			{
				return this._inputMaterials;
			}
			set
			{
				if (value != this._inputMaterials)
				{
					this._inputMaterials = value;
					base.OnPropertyChangedWithValue<MBBindingList<CraftingResourceItemVM>>(value, "InputMaterials");
				}
			}
		}

		// Token: 0x17000874 RID: 2164
		// (get) Token: 0x06001937 RID: 6455 RVA: 0x0005F90C File Offset: 0x0005DB0C
		// (set) Token: 0x06001938 RID: 6456 RVA: 0x0005F914 File Offset: 0x0005DB14
		[DataSourceProperty]
		public MBBindingList<CraftingResourceItemVM> OutputMaterials
		{
			get
			{
				return this._outputMaterials;
			}
			set
			{
				if (value != this._outputMaterials)
				{
					this._outputMaterials = value;
					base.OnPropertyChangedWithValue<MBBindingList<CraftingResourceItemVM>>(value, "OutputMaterials");
				}
			}
		}

		// Token: 0x17000875 RID: 2165
		// (get) Token: 0x06001939 RID: 6457 RVA: 0x0005F932 File Offset: 0x0005DB32
		// (set) Token: 0x0600193A RID: 6458 RVA: 0x0005F93A File Offset: 0x0005DB3A
		[DataSourceProperty]
		public bool IsSelected
		{
			get
			{
				return this._isSelected;
			}
			set
			{
				if (value != this._isSelected)
				{
					this._isSelected = value;
					base.OnPropertyChangedWithValue(value, "IsSelected");
				}
			}
		}

		// Token: 0x17000876 RID: 2166
		// (get) Token: 0x0600193B RID: 6459 RVA: 0x0005F958 File Offset: 0x0005DB58
		// (set) Token: 0x0600193C RID: 6460 RVA: 0x0005F960 File Offset: 0x0005DB60
		[DataSourceProperty]
		public bool IsEnabled
		{
			get
			{
				return this._isEnabled;
			}
			set
			{
				if (value != this._isEnabled)
				{
					this._isEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsEnabled");
				}
			}
		}

		// Token: 0x04000B96 RID: 2966
		private readonly Action<RefinementActionItemVM> _onSelect;

		// Token: 0x04000B98 RID: 2968
		private MBBindingList<CraftingResourceItemVM> _inputMaterials;

		// Token: 0x04000B99 RID: 2969
		private MBBindingList<CraftingResourceItemVM> _outputMaterials;

		// Token: 0x04000B9A RID: 2970
		private bool _isSelected;

		// Token: 0x04000B9B RID: 2971
		private bool _isEnabled;
	}
}
