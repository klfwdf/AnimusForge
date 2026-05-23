using System;
using System.Linq;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.Refinement
{
	// Token: 0x02000115 RID: 277
	public class RefinementVM : ViewModel
	{
		// Token: 0x0600193D RID: 6461 RVA: 0x0005F980 File Offset: 0x0005DB80
		public RefinementVM(Action onRefinementSelectionChange, Func<CraftingAvailableHeroItemVM> getCurrentHero)
		{
			this._onRefinementSelectionChange = onRefinementSelectionChange;
			this._craftingBehavior = Campaign.Current.GetCampaignBehavior<ICraftingCampaignBehavior>();
			this._getCurrentHero = getCurrentHero;
			this.AvailableRefinementActions = new MBBindingList<RefinementActionItemVM>();
			this.SetupRefinementActionsList(this._getCurrentHero().Hero);
		}

		// Token: 0x0600193E RID: 6462 RVA: 0x0005F9D2 File Offset: 0x0005DBD2
		private void SetupRefinementActionsList(Hero craftingHero)
		{
			this.UpdateRefinementFormulas(craftingHero);
			this.RefreshRefinementActionsList(craftingHero);
		}

		// Token: 0x0600193F RID: 6463 RVA: 0x0005F9E2 File Offset: 0x0005DBE2
		internal void OnCraftingHeroChanged(CraftingAvailableHeroItemVM newHero)
		{
			this.SetupRefinementActionsList(this._getCurrentHero().Hero);
			this.SelectDefaultAction();
		}

		// Token: 0x06001940 RID: 6464 RVA: 0x0005FA00 File Offset: 0x0005DC00
		private void UpdateRefinementFormulas(Hero hero)
		{
			this.AvailableRefinementActions.Clear();
			foreach (Crafting.RefiningFormula refineFormula in Campaign.Current.Models.SmithingModel.GetRefiningFormulas(hero))
			{
				this.AvailableRefinementActions.Add(new RefinementActionItemVM(refineFormula, new Action<RefinementActionItemVM>(this.OnSelectAction)));
			}
		}

		// Token: 0x06001941 RID: 6465 RVA: 0x0005FA80 File Offset: 0x0005DC80
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.RefinementText = new TextObject("{=p7raHA9x}Refinement", null).ToString();
			this.AvailableRefinementActions.ApplyActionOnAllItems(delegate(RefinementActionItemVM x)
			{
				x.RefreshValues();
			});
			RefinementActionItemVM currentSelectedAction = this.CurrentSelectedAction;
			if (currentSelectedAction == null)
			{
				return;
			}
			currentSelectedAction.RefreshValues();
		}

		// Token: 0x06001942 RID: 6466 RVA: 0x0005FAE4 File Offset: 0x0005DCE4
		public void ExecuteSelectedRefinement(Hero currentCraftingHero)
		{
			if (this.CurrentSelectedAction != null)
			{
				ICraftingCampaignBehavior craftingBehavior = this._craftingBehavior;
				if (craftingBehavior != null)
				{
					craftingBehavior.DoRefinement(currentCraftingHero, this.CurrentSelectedAction.RefineFormula);
				}
				this.RefreshRefinementActionsList(currentCraftingHero);
				if (!this.CurrentSelectedAction.IsEnabled)
				{
					this.OnSelectAction(null);
				}
			}
		}

		// Token: 0x06001943 RID: 6467 RVA: 0x0005FB34 File Offset: 0x0005DD34
		public void RefreshRefinementActionsList(Hero craftingHero)
		{
			foreach (RefinementActionItemVM refinementActionItemVM in this.AvailableRefinementActions)
			{
				refinementActionItemVM.RefreshDynamicProperties();
			}
			if (this.CurrentSelectedAction == null)
			{
				this.SelectDefaultAction();
			}
		}

		// Token: 0x06001944 RID: 6468 RVA: 0x0005FB8C File Offset: 0x0005DD8C
		private void SelectDefaultAction()
		{
			RefinementActionItemVM refinementActionItemVM = this.AvailableRefinementActions.FirstOrDefault((RefinementActionItemVM a) => a.IsEnabled);
			if (refinementActionItemVM != null)
			{
				this.OnSelectAction(refinementActionItemVM);
			}
		}

		// Token: 0x06001945 RID: 6469 RVA: 0x0005FBCE File Offset: 0x0005DDCE
		private void OnSelectAction(RefinementActionItemVM selectedAction)
		{
			if (this.CurrentSelectedAction != null)
			{
				this.CurrentSelectedAction.IsSelected = false;
			}
			this.CurrentSelectedAction = selectedAction;
			this._onRefinementSelectionChange();
			if (this.CurrentSelectedAction != null)
			{
				this.CurrentSelectedAction.IsSelected = true;
			}
		}

		// Token: 0x17000877 RID: 2167
		// (get) Token: 0x06001946 RID: 6470 RVA: 0x0005FC0A File Offset: 0x0005DE0A
		// (set) Token: 0x06001947 RID: 6471 RVA: 0x0005FC12 File Offset: 0x0005DE12
		[DataSourceProperty]
		public RefinementActionItemVM CurrentSelectedAction
		{
			get
			{
				return this._currentSelectedAction;
			}
			set
			{
				if (value != this._currentSelectedAction)
				{
					this._currentSelectedAction = value;
					base.OnPropertyChangedWithValue<RefinementActionItemVM>(value, "CurrentSelectedAction");
					this.IsValidRefinementActionSelected = value != null;
				}
			}
		}

		// Token: 0x17000878 RID: 2168
		// (get) Token: 0x06001948 RID: 6472 RVA: 0x0005FC3A File Offset: 0x0005DE3A
		// (set) Token: 0x06001949 RID: 6473 RVA: 0x0005FC42 File Offset: 0x0005DE42
		[DataSourceProperty]
		public bool IsValidRefinementActionSelected
		{
			get
			{
				return this._isValidRefinementActionSelected;
			}
			set
			{
				if (value != this._isValidRefinementActionSelected)
				{
					this._isValidRefinementActionSelected = value;
					base.OnPropertyChangedWithValue(value, "IsValidRefinementActionSelected");
				}
			}
		}

		// Token: 0x17000879 RID: 2169
		// (get) Token: 0x0600194A RID: 6474 RVA: 0x0005FC60 File Offset: 0x0005DE60
		// (set) Token: 0x0600194B RID: 6475 RVA: 0x0005FC68 File Offset: 0x0005DE68
		[DataSourceProperty]
		public MBBindingList<RefinementActionItemVM> AvailableRefinementActions
		{
			get
			{
				return this._availableRefinementActions;
			}
			set
			{
				if (value != this._availableRefinementActions)
				{
					this._availableRefinementActions = value;
					base.OnPropertyChangedWithValue<MBBindingList<RefinementActionItemVM>>(value, "AvailableRefinementActions");
				}
			}
		}

		// Token: 0x1700087A RID: 2170
		// (get) Token: 0x0600194C RID: 6476 RVA: 0x0005FC86 File Offset: 0x0005DE86
		// (set) Token: 0x0600194D RID: 6477 RVA: 0x0005FC8E File Offset: 0x0005DE8E
		[DataSourceProperty]
		public string RefinementText
		{
			get
			{
				return this._refinementText;
			}
			set
			{
				if (value != this._refinementText)
				{
					this._refinementText = value;
					base.OnPropertyChangedWithValue<string>(value, "RefinementText");
				}
			}
		}

		// Token: 0x04000B9C RID: 2972
		private readonly Action _onRefinementSelectionChange;

		// Token: 0x04000B9D RID: 2973
		private readonly ICraftingCampaignBehavior _craftingBehavior;

		// Token: 0x04000B9E RID: 2974
		private readonly Func<CraftingAvailableHeroItemVM> _getCurrentHero;

		// Token: 0x04000B9F RID: 2975
		private RefinementActionItemVM _currentSelectedAction;

		// Token: 0x04000BA0 RID: 2976
		private bool _isValidRefinementActionSelected;

		// Token: 0x04000BA1 RID: 2977
		private MBBindingList<RefinementActionItemVM> _availableRefinementActions;

		// Token: 0x04000BA2 RID: 2978
		private string _refinementText;
	}
}
