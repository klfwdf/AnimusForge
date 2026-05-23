using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items;

public class EncyclopediaShipStatVM : ViewModel
{
	private readonly TextObject _nameTextObj;

	private string _statId;

	private string _name;

	private string _valueText;

	private BasicTooltipViewModel _tooltip;

	[DataSourceProperty]
	public string StatId
	{
		get
		{
			return _statId;
		}
		set
		{
			if (value != _statId)
			{
				_statId = value;
				OnPropertyChangedWithValue(value, "StatId");
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
	public BasicTooltipViewModel Tooltip
	{
		get
		{
			return _tooltip;
		}
		set
		{
			if (value != _tooltip)
			{
				_tooltip = value;
				OnPropertyChangedWithValue(value, "Tooltip");
			}
		}
	}

	public EncyclopediaShipStatVM(string statId, TextObject name, string value, Func<List<TooltipProperty>> getTooltipProperties = null)
	{
		_nameTextObj = name;
		ValueText = value;
		StatId = statId;
		if (getTooltipProperties != null)
		{
			Tooltip = new BasicTooltipViewModel(getTooltipProperties);
		}
		else
		{
			Tooltip = new BasicTooltipViewModel(() => GameTexts.FindText("str_ship_stat_explanation", StatId).ToString());
		}
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		TextObject textObject = GameTexts.FindText("str_LEFT_colon");
		textObject.SetTextVariable("LEFT", _nameTextObj.ToString());
		Name = textObject.ToString();
	}
}
