using System;
using System.Linq;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.Refinement;

public class RefinementVM : ViewModel
{
	private readonly Action _onRefinementSelectionChange;

	private readonly ICraftingCampaignBehavior _craftingBehavior;

	private readonly Func<CraftingAvailableHeroItemVM> _getCurrentHero;

	private RefinementActionItemVM _currentSelectedAction;

	private bool _isValidRefinementActionSelected;

	private MBBindingList<RefinementActionItemVM> _availableRefinementActions;

	private string _refinementText;

	[DataSourceProperty]
	public RefinementActionItemVM CurrentSelectedAction
	{
		get
		{
			return _currentSelectedAction;
		}
		set
		{
			if (value != _currentSelectedAction)
			{
				_currentSelectedAction = value;
				OnPropertyChangedWithValue(value, "CurrentSelectedAction");
				IsValidRefinementActionSelected = value != null;
			}
		}
	}

	[DataSourceProperty]
	public bool IsValidRefinementActionSelected
	{
		get
		{
			return _isValidRefinementActionSelected;
		}
		set
		{
			if (value != _isValidRefinementActionSelected)
			{
				_isValidRefinementActionSelected = value;
				OnPropertyChangedWithValue(value, "IsValidRefinementActionSelected");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<RefinementActionItemVM> AvailableRefinementActions
	{
		get
		{
			return _availableRefinementActions;
		}
		set
		{
			if (value != _availableRefinementActions)
			{
				_availableRefinementActions = value;
				OnPropertyChangedWithValue(value, "AvailableRefinementActions");
			}
		}
	}

	[DataSourceProperty]
	public string RefinementText
	{
		get
		{
			return _refinementText;
		}
		set
		{
			if (value != _refinementText)
			{
				_refinementText = value;
				OnPropertyChangedWithValue(value, "RefinementText");
			}
		}
	}

	public RefinementVM(Action onRefinementSelectionChange, Func<CraftingAvailableHeroItemVM> getCurrentHero)
	{
		_onRefinementSelectionChange = onRefinementSelectionChange;
		_craftingBehavior = Campaign.Current.GetCampaignBehavior<ICraftingCampaignBehavior>();
		_getCurrentHero = getCurrentHero;
		AvailableRefinementActions = new MBBindingList<RefinementActionItemVM>();
		SetupRefinementActionsList(_getCurrentHero().Hero);
	}

	private void SetupRefinementActionsList(Hero craftingHero)
	{
		UpdateRefinementFormulas(craftingHero);
		RefreshRefinementActionsList(craftingHero);
	}

	internal void OnCraftingHeroChanged(CraftingAvailableHeroItemVM newHero)
	{
		SetupRefinementActionsList(_getCurrentHero().Hero);
		SelectDefaultAction();
	}

	private void UpdateRefinementFormulas(Hero hero)
	{
		AvailableRefinementActions.Clear();
		foreach (Crafting.RefiningFormula refiningFormula in Campaign.Current.Models.SmithingModel.GetRefiningFormulas(hero))
		{
			AvailableRefinementActions.Add(new RefinementActionItemVM(refiningFormula, OnSelectAction));
		}
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		RefinementText = new TextObject("{=p7raHA9x}Refinement").ToString();
		AvailableRefinementActions.ApplyActionOnAllItems(delegate(RefinementActionItemVM x)
		{
			x.RefreshValues();
		});
		CurrentSelectedAction?.RefreshValues();
	}

	public void ExecuteSelectedRefinement(Hero currentCraftingHero)
	{
		if (CurrentSelectedAction != null)
		{
			_craftingBehavior?.DoRefinement(currentCraftingHero, CurrentSelectedAction.RefineFormula);
			RefreshRefinementActionsList(currentCraftingHero);
			if (!CurrentSelectedAction.IsEnabled)
			{
				OnSelectAction(null);
			}
		}
	}

	public void RefreshRefinementActionsList(Hero craftingHero)
	{
		foreach (RefinementActionItemVM availableRefinementAction in AvailableRefinementActions)
		{
			availableRefinementAction.RefreshDynamicProperties();
		}
		if (CurrentSelectedAction == null)
		{
			SelectDefaultAction();
		}
	}

	private void SelectDefaultAction()
	{
		RefinementActionItemVM refinementActionItemVM = AvailableRefinementActions.FirstOrDefault((RefinementActionItemVM a) => a.IsEnabled);
		if (refinementActionItemVM != null)
		{
			OnSelectAction(refinementActionItemVM);
		}
	}

	private void OnSelectAction(RefinementActionItemVM selectedAction)
	{
		if (CurrentSelectedAction != null)
		{
			CurrentSelectedAction.IsSelected = false;
		}
		CurrentSelectedAction = selectedAction;
		_onRefinementSelectionChange();
		if (CurrentSelectedAction != null)
		{
			CurrentSelectedAction.IsSelected = true;
		}
	}
}
