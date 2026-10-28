using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection;

public class ProfitItemPropertyVM : ViewModel
{
	public enum PropertyType
	{
		None,
		Tax,
		Tariff,
		Garrison,
		Village,
		Governor
	}

	private int _type;

	private string _name;

	private int _value;

	private string _valueString;

	private BasicTooltipViewModel _hint;

	private string _colonText;

	private CharacterImageIdentifierVM _governorVisual;

	private bool _showGovernorPortrait;

	[DataSourceProperty]
	public int Type
	{
		get
		{
			return _type;
		}
		set
		{
			if (value != _type)
			{
				_type = value;
				ShowGovernorPortrait = _type == 5;
				OnPropertyChangedWithValue(value, "Type");
			}
		}
	}

	[DataSourceProperty]
	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			if (value != _name)
			{
				_name = value;
				OnPropertyChangedWithValue(value, "Name");
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
				ValueString = _value.ToString("+0;-#");
				OnPropertyChangedWithValue(value, "Value");
			}
		}
	}

	[DataSourceProperty]
	public string ValueString
	{
		get
		{
			return _valueString;
		}
		private set
		{
			if (value != _valueString)
			{
				_valueString = value;
				OnPropertyChangedWithValue(value, "ValueString");
			}
		}
	}

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
	public string ColonText
	{
		get
		{
			return _colonText;
		}
		set
		{
			if (value != _colonText)
			{
				_colonText = value;
				OnPropertyChangedWithValue(value, "ColonText");
			}
		}
	}

	[DataSourceProperty]
	public CharacterImageIdentifierVM GovernorVisual
	{
		get
		{
			return _governorVisual;
		}
		set
		{
			if (value != _governorVisual)
			{
				_governorVisual = value;
				OnPropertyChangedWithValue(value, "GovernorVisual");
			}
		}
	}

	[DataSourceProperty]
	public bool ShowGovernorPortrait
	{
		get
		{
			return _showGovernorPortrait;
		}
		private set
		{
			if (value != _showGovernorPortrait)
			{
				_showGovernorPortrait = value;
				OnPropertyChangedWithValue(value, "ShowGovernorPortrait");
			}
		}
	}

	public ProfitItemPropertyVM(string name, int value, PropertyType type = PropertyType.None, CharacterImageIdentifierVM governorVisual = null, BasicTooltipViewModel hint = null)
	{
		Name = name;
		Value = value;
		Type = (int)type;
		GovernorVisual = governorVisual;
		Hint = hint;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		ColonText = GameTexts.FindText("str_colon").ToString();
	}
}
