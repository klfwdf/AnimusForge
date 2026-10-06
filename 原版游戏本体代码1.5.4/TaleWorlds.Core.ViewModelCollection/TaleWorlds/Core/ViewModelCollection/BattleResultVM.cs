using System;
using System.Collections.Generic;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace TaleWorlds.Core.ViewModelCollection;

public class BattleResultVM : ViewModel
{
	private string _text;

	private BasicTooltipViewModel _hint;

	private CharacterImageIdentifierVM _deadLordPortrait;

	private BannerImageIdentifierVM _deadLordClanBanner;

	[DataSourceProperty]
	public string Text
	{
		get
		{
			return _text;
		}
		set
		{
			if (value != _text)
			{
				_text = value;
				OnPropertyChangedWithValue(value, "Text");
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
	public CharacterImageIdentifierVM DeadLordPortrait
	{
		get
		{
			return _deadLordPortrait;
		}
		set
		{
			if (value != _deadLordPortrait)
			{
				_deadLordPortrait = value;
				OnPropertyChangedWithValue(value, "DeadLordPortrait");
			}
		}
	}

	[DataSourceProperty]
	public BannerImageIdentifierVM DeadLordClanBanner
	{
		get
		{
			return _deadLordClanBanner;
		}
		set
		{
			if (value != _deadLordClanBanner)
			{
				_deadLordClanBanner = value;
				OnPropertyChangedWithValue(value, "DeadLordClanBanner");
			}
		}
	}

	public BattleResultVM(string text, Func<List<TooltipProperty>> propertyFunc, CharacterCode deadHeroCode = null)
	{
		Text = text;
		Hint = new BasicTooltipViewModel(propertyFunc);
		if (deadHeroCode != null)
		{
			DeadLordPortrait = new CharacterImageIdentifierVM(deadHeroCode);
			DeadLordClanBanner = new BannerImageIdentifierVM(deadHeroCode.Banner, nineGrid: true);
		}
		else
		{
			DeadLordPortrait = null;
			DeadLordClanBanner = null;
		}
	}
}
