using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper.PerkSelection;

public class PerkSelectionItemVM : ViewModel
{
	private readonly Action<PerkSelectionItemVM> _onSelection;

	public readonly PerkObject Perk;

	private string _pickText;

	private string _perkName;

	private string _perkDescription;

	private string _perkRole;

	[DataSourceProperty]
	public string PickText
	{
		get
		{
			return _pickText;
		}
		set
		{
			if (value != _pickText)
			{
				_pickText = value;
				OnPropertyChangedWithValue(value, "PickText");
			}
		}
	}

	[DataSourceProperty]
	public string PerkName
	{
		get
		{
			return _perkName;
		}
		set
		{
			if (value != _perkName)
			{
				_perkName = value;
				OnPropertyChangedWithValue(value, "PerkName");
			}
		}
	}

	[DataSourceProperty]
	public string PerkDescription
	{
		get
		{
			return _perkDescription;
		}
		set
		{
			if (value != _perkDescription)
			{
				_perkDescription = value;
				OnPropertyChangedWithValue(value, "PerkDescription");
			}
		}
	}

	[DataSourceProperty]
	public string PerkRole
	{
		get
		{
			return _perkRole;
		}
		set
		{
			if (value != _perkRole)
			{
				_perkRole = value;
				OnPropertyChangedWithValue(value, "PerkRole");
			}
		}
	}

	public PerkSelectionItemVM(PerkObject perk, Action<PerkSelectionItemVM> onSelection)
	{
		Perk = perk;
		_onSelection = onSelection;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		PickText = new TextObject("{=1CXlqb2U}Pick:").ToString();
		PerkName = Perk.Name.ToString();
		PerkDescription = Perk.Description.ToString();
		PerkRole = CampaignUIHelper.GetCombinedPerkRoleText(Perk)?.ToString() ?? "";
	}

	public void ExecuteSelection()
	{
		_onSelection(this);
	}
}
