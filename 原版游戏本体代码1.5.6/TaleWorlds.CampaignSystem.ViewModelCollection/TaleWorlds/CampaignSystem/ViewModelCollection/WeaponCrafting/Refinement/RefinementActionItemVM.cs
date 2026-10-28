using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.Refinement;

public class RefinementActionItemVM : ViewModel
{
	private readonly Action<RefinementActionItemVM> _onSelect;

	private MBBindingList<CraftingResourceItemVM> _inputMaterials;

	private MBBindingList<CraftingResourceItemVM> _outputMaterials;

	private bool _isSelected;

	private bool _isEnabled;

	public Crafting.RefiningFormula RefineFormula { get; }

	[DataSourceProperty]
	public MBBindingList<CraftingResourceItemVM> InputMaterials
	{
		get
		{
			return _inputMaterials;
		}
		set
		{
			if (value != _inputMaterials)
			{
				_inputMaterials = value;
				OnPropertyChangedWithValue(value, "InputMaterials");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<CraftingResourceItemVM> OutputMaterials
	{
		get
		{
			return _outputMaterials;
		}
		set
		{
			if (value != _outputMaterials)
			{
				_outputMaterials = value;
				OnPropertyChangedWithValue(value, "OutputMaterials");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSelected
	{
		get
		{
			return _isSelected;
		}
		set
		{
			if (value != _isSelected)
			{
				_isSelected = value;
				OnPropertyChangedWithValue(value, "IsSelected");
			}
		}
	}

	[DataSourceProperty]
	public bool IsEnabled
	{
		get
		{
			return _isEnabled;
		}
		set
		{
			if (value != _isEnabled)
			{
				_isEnabled = value;
				OnPropertyChangedWithValue(value, "IsEnabled");
			}
		}
	}

	public RefinementActionItemVM(Crafting.RefiningFormula refineFormula, Action<RefinementActionItemVM> onSelect)
	{
		_onSelect = onSelect;
		RefineFormula = refineFormula;
		InputMaterials = new MBBindingList<CraftingResourceItemVM>();
		OutputMaterials = new MBBindingList<CraftingResourceItemVM>();
		_ = Campaign.Current.Models.SmithingModel;
		if (RefineFormula.Input1Count > 0)
		{
			InputMaterials.Add(new CraftingResourceItemVM(RefineFormula.Input1, RefineFormula.Input1Count));
		}
		if (RefineFormula.Input2Count > 0)
		{
			InputMaterials.Add(new CraftingResourceItemVM(RefineFormula.Input2, RefineFormula.Input2Count));
		}
		if (RefineFormula.OutputCount > 0)
		{
			OutputMaterials.Add(new CraftingResourceItemVM(RefineFormula.Output, RefineFormula.OutputCount));
		}
		if (RefineFormula.Output2Count > 0)
		{
			OutputMaterials.Add(new CraftingResourceItemVM(RefineFormula.Output2, RefineFormula.Output2Count));
		}
		RefreshDynamicProperties();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		InputMaterials.ApplyActionOnAllItems(delegate(CraftingResourceItemVM m)
		{
			m.RefreshValues();
		});
		OutputMaterials.ApplyActionOnAllItems(delegate(CraftingResourceItemVM m)
		{
			m.RefreshValues();
		});
	}

	public void RefreshDynamicProperties()
	{
		IsEnabled = UpdateInputAvailabilities();
	}

	private bool UpdateInputAvailabilities()
	{
		bool result = true;
		ItemRoster itemRoster = MobileParty.MainParty.ItemRoster;
		foreach (CraftingResourceItemVM inputMaterial in InputMaterials)
		{
			if (itemRoster.GetItemNumber(inputMaterial.ResourceItem) < inputMaterial.ResourceAmount)
			{
				result = false;
				inputMaterial.IsResourceAvailable = false;
			}
			else
			{
				inputMaterial.IsResourceAvailable = true;
			}
		}
		return result;
	}

	public void ExecuteSelectAction()
	{
		_onSelect(this);
	}
}
