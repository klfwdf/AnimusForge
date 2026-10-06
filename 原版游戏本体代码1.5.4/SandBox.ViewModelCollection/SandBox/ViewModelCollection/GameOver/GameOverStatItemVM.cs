using System;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.GameOver;

public class GameOverStatItemVM : ViewModel
{
	private readonly StatItem _item;

	private string _definitionText;

	private string _valueText;

	private string _statTypeAsString;

	[DataSourceProperty]
	public string DefinitionText
	{
		get
		{
			return _definitionText;
		}
		set
		{
			if (value != _definitionText)
			{
				_definitionText = value;
				OnPropertyChangedWithValue(value, "DefinitionText");
			}
		}
	}

	[DataSourceProperty]
	public string ValueText
	{
		get
		{
			return _valueText;
		}
		set
		{
			if (value != _valueText)
			{
				_valueText = value;
				OnPropertyChangedWithValue(value, "ValueText");
			}
		}
	}

	[DataSourceProperty]
	public string StatTypeAsString
	{
		get
		{
			return _statTypeAsString;
		}
		set
		{
			if (value != _statTypeAsString)
			{
				_statTypeAsString = value;
				OnPropertyChangedWithValue(value, "StatTypeAsString");
			}
		}
	}

	public GameOverStatItemVM(StatItem item)
	{
		_item = item;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		DefinitionText = GameTexts.FindText("str_game_over_stat_item", _item.ID).ToString();
		ValueText = _item.Value;
		StatTypeAsString = Enum.GetName(typeof(StatItem.StatType), _item.Type);
	}
}
