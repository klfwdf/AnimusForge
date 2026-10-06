using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection;

public class SelectableItemPropertyVM : ViewModel
{
	public enum PropertyType
	{
		None,
		Wall,
		Garrison,
		Militia,
		Prosperity,
		Food,
		Loyalty,
		Security,
		Shipyard,
		Patrol,
		CoastalPatrol
	}

	private int _type;

	private bool _isWarning;

	private string _name;

	private string _value;

	private BasicTooltipViewModel _hint;

	private string _colonText;

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
				OnPropertyChangedWithValue(value, "Type");
			}
		}
	}

	[DataSourceProperty]
	public bool IsWarning
	{
		get
		{
			return _isWarning;
		}
		set
		{
			if (value != _isWarning)
			{
				_isWarning = value;
				OnPropertyChangedWithValue(value, "IsWarning");
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
	public string Value
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

	public SelectableItemPropertyVM(string name, string value, bool isWarning = false, BasicTooltipViewModel hint = null)
	{
		Name = name;
		Value = value;
		Hint = hint;
		Type = 0;
		IsWarning = isWarning;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		ColonText = GameTexts.FindText("str_colon").ToString();
	}

	private void ExecuteLink(string link)
	{
		Campaign.Current.EncyclopediaManager.GoToLink(link);
	}
}
