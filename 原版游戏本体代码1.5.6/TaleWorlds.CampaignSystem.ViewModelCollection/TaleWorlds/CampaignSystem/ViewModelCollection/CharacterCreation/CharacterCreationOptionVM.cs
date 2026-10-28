using System;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.CharacterCreation;

public class CharacterCreationOptionVM : ViewModel
{
	public readonly NarrativeMenuOption Option;

	private readonly Action<CharacterCreationOptionVM> _onSelect;

	private bool _isSelected;

	private string _actionText;

	private string _positiveEffectText;

	private string _negativeEffectText;

	private string _descriptionText;

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
	public string ActionText
	{
		get
		{
			return _actionText;
		}
		set
		{
			if (value != _actionText)
			{
				_actionText = value;
				OnPropertyChangedWithValue(value, "ActionText");
			}
		}
	}

	[DataSourceProperty]
	public string PositiveEffectText
	{
		get
		{
			return _positiveEffectText;
		}
		set
		{
			if (value != _positiveEffectText)
			{
				_positiveEffectText = value;
				OnPropertyChangedWithValue(value, "PositiveEffectText");
			}
		}
	}

	[DataSourceProperty]
	public string NegativeEffectText
	{
		get
		{
			return _negativeEffectText;
		}
		set
		{
			if (value != _negativeEffectText)
			{
				_negativeEffectText = value;
				OnPropertyChangedWithValue(value, "NegativeEffectText");
			}
		}
	}

	[DataSourceProperty]
	public string DescriptionText
	{
		get
		{
			return _descriptionText;
		}
		set
		{
			if (value != _descriptionText)
			{
				_descriptionText = value;
				OnPropertyChangedWithValue(value, "DescriptionText");
			}
		}
	}

	public CharacterCreationOptionVM(Action<CharacterCreationOptionVM> onSelect, NarrativeMenuOption option)
	{
		_onSelect = onSelect;
		Option = option;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		ActionText = Option.Text.ToString();
		PositiveEffectText = Option.PositiveEffectText.ToString();
		DescriptionText = Option.DescriptionText.ToString();
	}

	public void ExecuteSelect()
	{
		_onSelect?.Invoke(this);
	}
}
