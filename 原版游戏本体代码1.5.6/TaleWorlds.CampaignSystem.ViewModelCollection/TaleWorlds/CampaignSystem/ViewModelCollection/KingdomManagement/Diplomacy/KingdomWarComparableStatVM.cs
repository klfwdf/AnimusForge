using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Diplomacy;

public class KingdomWarComparableStatVM : ViewModel
{
	private TextObject _nameObj;

	private int _defaultRange;

	private BasicTooltipViewModel _faction1Hint;

	private BasicTooltipViewModel _faction2Hint;

	private string _name;

	private string _faction1Color;

	private string _faction2Color;

	private int _faction1Percentage;

	private int _faction1Value;

	private int _faction2Percentage;

	private int _faction2Value;

	[DataSourceProperty]
	public BasicTooltipViewModel Faction1Hint
	{
		get
		{
			return _faction1Hint;
		}
		set
		{
			if (value != _faction1Hint)
			{
				_faction1Hint = value;
				OnPropertyChangedWithValue(value, "Faction1Hint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel Faction2Hint
	{
		get
		{
			return _faction2Hint;
		}
		set
		{
			if (value != _faction2Hint)
			{
				_faction2Hint = value;
				OnPropertyChangedWithValue(value, "Faction2Hint");
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
	public string Faction1Color
	{
		get
		{
			return _faction1Color;
		}
		set
		{
			if (value != _faction1Color)
			{
				_faction1Color = value;
				OnPropertyChangedWithValue(value, "Faction1Color");
			}
		}
	}

	[DataSourceProperty]
	public string Faction2Color
	{
		get
		{
			return _faction2Color;
		}
		set
		{
			if (value != _faction2Color)
			{
				_faction2Color = value;
				OnPropertyChangedWithValue(value, "Faction2Color");
			}
		}
	}

	[DataSourceProperty]
	public int Faction1Percentage
	{
		get
		{
			return _faction1Percentage;
		}
		set
		{
			if (value != _faction1Percentage)
			{
				_faction1Percentage = value;
				OnPropertyChangedWithValue(value, "Faction1Percentage");
			}
		}
	}

	[DataSourceProperty]
	public int Faction1Value
	{
		get
		{
			return _faction1Value;
		}
		set
		{
			if (value != _faction1Value)
			{
				_faction1Value = value;
				OnPropertyChangedWithValue(value, "Faction1Value");
			}
		}
	}

	[DataSourceProperty]
	public int Faction2Percentage
	{
		get
		{
			return _faction2Percentage;
		}
		set
		{
			if (value != _faction2Percentage)
			{
				_faction2Percentage = value;
				OnPropertyChangedWithValue(value, "Faction2Percentage");
			}
		}
	}

	[DataSourceProperty]
	public int Faction2Value
	{
		get
		{
			return _faction2Value;
		}
		set
		{
			if (value != _faction2Value)
			{
				_faction2Value = value;
				OnPropertyChangedWithValue(value, "Faction2Value");
			}
		}
	}

	public KingdomWarComparableStatVM(int faction1Stat, int faction2Stat, TextObject name, string faction1Color, string faction2Color, int defaultRange, BasicTooltipViewModel faction1Hint = null, BasicTooltipViewModel faction2Hint = null)
	{
		int num = MathF.Max(MathF.Max(faction1Stat, faction2Stat), defaultRange);
		if (num == 0)
		{
			num = 1;
		}
		Faction1Color = faction1Color;
		Faction2Color = faction2Color;
		Faction1Value = faction1Stat;
		Faction2Value = faction2Stat;
		_defaultRange = defaultRange;
		Faction1Percentage = MathF.Round((float)faction1Stat / (float)num * 100f);
		Faction2Percentage = MathF.Round((float)faction2Stat / (float)num * 100f);
		_nameObj = name;
		Faction1Hint = faction1Hint;
		Faction2Hint = faction2Hint;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Name = _nameObj.ToString();
	}
}
