using TaleWorlds.CampaignSystem.Incidents;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace SandBox.ViewModelCollection.Map.Incidents;

public class MapIncidentHintVM : ViewModel
{
	private readonly TextObject _hintText;

	private readonly int _chancePercentage;

	private bool _hasText;

	private bool _hasChance;

	private bool _hasIndent;

	private bool _hasChildHints;

	private string _text;

	private string _chanceText;

	private MBBindingList<MapIncidentHintVM> _childHints;

	[DataSourceProperty]
	public bool HasText
	{
		get
		{
			return _hasText;
		}
		set
		{
			if (value != _hasText)
			{
				_hasText = value;
				OnPropertyChangedWithValue(value, "HasText");
			}
		}
	}

	[DataSourceProperty]
	public bool HasChance
	{
		get
		{
			return _hasChance;
		}
		set
		{
			if (value != _hasChance)
			{
				_hasChance = value;
				OnPropertyChangedWithValue(value, "HasChance");
			}
		}
	}

	[DataSourceProperty]
	public bool HasIndent
	{
		get
		{
			return _hasIndent;
		}
		set
		{
			if (value != _hasIndent)
			{
				_hasIndent = value;
				OnPropertyChangedWithValue(value, "HasIndent");
			}
		}
	}

	[DataSourceProperty]
	public bool HasChildHints
	{
		get
		{
			return _hasChildHints;
		}
		set
		{
			if (value != _hasChildHints)
			{
				_hasChildHints = value;
				OnPropertyChangedWithValue(value, "HasChildHints");
			}
		}
	}

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
	public string ChanceText
	{
		get
		{
			return _chanceText;
		}
		set
		{
			if (value != _chanceText)
			{
				_chanceText = value;
				OnPropertyChangedWithValue(value, "ChanceText");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MapIncidentHintVM> ChildHints
	{
		get
		{
			return _childHints;
		}
		set
		{
			if (value != _childHints)
			{
				_childHints = value;
				OnPropertyChangedWithValue(value, "ChildHints");
			}
		}
	}

	public MapIncidentHintVM(IncidentHint hint, bool showChanceVisually = false)
	{
		_hintText = GetDisplayedText(hint);
		_chancePercentage = MathF.Round(hint.Chance * 100f);
		ChildHints = new MBBindingList<MapIncidentHintVM>();
		bool flag = hint.Type == IncidentHintType.Select;
		IncidentHint[] children = hint.Children;
		foreach (IncidentHint hint2 in children)
		{
			ChildHints.Add(new MapIncidentHintVM(hint2, flag));
		}
		HasText = _hintText != null;
		HasChildHints = ChildHints.Count > 0;
		HasChance = showChanceVisually && HasChildHints;
		HasIndent = HasChildHints && (HasChance || (HasText && !flag));
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Text = (HasText ? ("• " + _hintText.ToString()) : null);
		ChanceText = (HasChance ? GameTexts.FindText("str_NUMBER_percent").SetTextVariable("NUMBER", _chancePercentage).ToString() : null);
		ChildHints.ApplyActionOnAllItems(delegate(MapIncidentHintVM h)
		{
			h.RefreshValues();
		});
	}

	private static TextObject GetDisplayedText(IncidentHint hint)
	{
		return hint.Type switch
		{
			IncidentHintType.SelectBranch => null, 
			IncidentHintType.Select => new TextObject("{=ABZbb0xQ}One of the following happens:"), 
			_ => hint.Text, 
		};
	}
}
