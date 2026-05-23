using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.CharacterCreation;

public class CharacterCreationGainedAttributeItemVM : ViewModel
{
	private readonly CharacterAttribute _attributeObj;

	private string _nameText;

	private bool _hasIncreasedInCurrentStage;

	private BasicTooltipViewModel _hint;

	[DataSourceProperty]
	public BasicTooltipViewModel Hint
	{
		get
		{
			return _hint;
		}
		set
		{
			if (value != _hint)
			{
				_hint = value;
				OnPropertyChangedWithValue(value, "Hint");
			}
		}
	}

	[DataSourceProperty]
	public string NameText
	{
		get
		{
			return _nameText;
		}
		set
		{
			if (value != _nameText)
			{
				_nameText = value;
				OnPropertyChangedWithValue(value, "NameText");
			}
		}
	}

	[DataSourceProperty]
	public bool HasIncreasedInCurrentStage
	{
		get
		{
			return _hasIncreasedInCurrentStage;
		}
		set
		{
			if (value != _hasIncreasedInCurrentStage)
			{
				_hasIncreasedInCurrentStage = value;
				OnPropertyChangedWithValue(value, "HasIncreasedInCurrentStage");
			}
		}
	}

	public CharacterCreationGainedAttributeItemVM(CharacterAttribute attributeObj)
	{
		_attributeObj = attributeObj;
		TextObject nameExtended = _attributeObj.Name;
		TextObject desc = _attributeObj.Description;
		Hint = new BasicTooltipViewModel(delegate
		{
			GameTexts.SetVariable("STR1", nameExtended);
			GameTexts.SetVariable("STR2", desc);
			return GameTexts.FindText("str_string_newline_string").ToString();
		});
		SetValue(0, 0);
	}

	internal void ResetValues()
	{
		SetValue(0, 0);
	}

	public void SetValue(int gainedFromOtherStages, int gainedFromCurrentStage)
	{
		HasIncreasedInCurrentStage = gainedFromCurrentStage > 0;
		GameTexts.SetVariable("LEFT", _attributeObj.Name);
		GameTexts.SetVariable("RIGHT", gainedFromOtherStages + gainedFromCurrentStage);
		NameText = GameTexts.FindText("str_LEFT_colon_RIGHT_wSpaceAfterColon").ToString();
	}
}
