using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items;

public class EncyclopediaTraitItemVM : ViewModel
{
	private readonly TraitObject _traitObj;

	private string _traitId;

	private int _value;

	private HintViewModel _hint;

	[DataSourceProperty]
	public string TraitId
	{
		get
		{
			return _traitId;
		}
		set
		{
			if (value != _traitId)
			{
				_traitId = value;
				OnPropertyChangedWithValue(value, "TraitId");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel Hint
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
	public int Value
	{
		get
		{
			return _value;
		}
		set
		{
			if (value != _value)
			{
				_value = value;
				OnPropertyChangedWithValue(value, "Value");
			}
		}
	}

	public EncyclopediaTraitItemVM(TraitObject traitObj, int value)
	{
		_traitObj = traitObj;
		TraitId = traitObj.StringId;
		Value = value;
		string traitTooltipText = CampaignUIHelper.GetTraitTooltipText(traitObj, Value);
		Hint = new HintViewModel(new TextObject("{=!}" + traitTooltipText));
	}

	public EncyclopediaTraitItemVM(TraitObject traitObj, Hero hero)
		: this(traitObj, hero.GetTraitLevel(traitObj))
	{
	}
}
