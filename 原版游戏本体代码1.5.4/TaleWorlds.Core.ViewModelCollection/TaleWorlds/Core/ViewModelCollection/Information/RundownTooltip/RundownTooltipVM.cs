using System;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.Core.ViewModelCollection.Information.RundownTooltip;

public class RundownTooltipVM : TooltipBaseVM
{
	public enum ValueCategorization
	{
		None,
		LargeIsBetter,
		SmallIsBetter
	}

	public float CurrentExpectedChange;

	private readonly ValueCategorization _valueCategorization;

	private readonly TextObject _titleTextSource;

	private readonly TextObject _summaryTextSource;

	private MBBindingList<RundownLineVM> _lines;

	private string _titleText;

	private string _expectedChangeText;

	private int _valueCategorizationAsInt;

	private string _extendText;

	public bool IsInitializedProperly { get; }

	[DataSourceProperty]
	public MBBindingList<RundownLineVM> Lines
	{
		get
		{
			return _lines;
		}
		set
		{
			if (value != _lines)
			{
				_lines = value;
				OnPropertyChangedWithValue(value, "Lines");
			}
		}
	}

	[DataSourceProperty]
	public string TitleText
	{
		get
		{
			return _titleText;
		}
		set
		{
			if (value != _titleText)
			{
				_titleText = value;
				OnPropertyChangedWithValue(value, "TitleText");
			}
		}
	}

	[DataSourceProperty]
	public string ExpectedChangeText
	{
		get
		{
			return _expectedChangeText;
		}
		set
		{
			if (value != _expectedChangeText)
			{
				_expectedChangeText = value;
				OnPropertyChangedWithValue(value, "ExpectedChangeText");
			}
		}
	}

	[DataSourceProperty]
	public int ValueCategorizationAsInt
	{
		get
		{
			return _valueCategorizationAsInt;
		}
		set
		{
			if (value != _valueCategorizationAsInt)
			{
				_valueCategorizationAsInt = value;
				OnPropertyChangedWithValue(value, "ValueCategorizationAsInt");
			}
		}
	}

	[DataSourceProperty]
	public string ExtendText
	{
		get
		{
			return _extendText;
		}
		set
		{
			if (value != _extendText)
			{
				_extendText = value;
				OnPropertyChangedWithValue(value, "ExtendText");
			}
		}
	}

	public RundownTooltipVM(Type invokedType, object[] invokedArgs)
		: base(invokedType, invokedArgs)
	{
		Lines = new MBBindingList<RundownLineVM>();
		if (invokedArgs.Length == 5)
		{
			_titleTextSource = invokedArgs[2] as TextObject;
			_summaryTextSource = invokedArgs[3] as TextObject;
			_valueCategorization = (ValueCategorization)invokedArgs[4];
			bool flag = !TextObject.IsNullOrEmpty(_titleTextSource);
			bool flag2 = !TextObject.IsNullOrEmpty(_summaryTextSource);
			IsInitializedProperly = flag && flag2;
		}
		else
		{
			Debug.FailedAssert("Unexpected number of arguments for rundown tooltip", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core.ViewModelCollection\\Information\\RundownTooltip\\RundownTooltipVM.cs", ".ctor", 46);
		}
		ValueCategorizationAsInt = (int)_valueCategorization;
		_isPeriodicRefreshEnabled = true;
		_periodicRefreshDelay = 1f;
		Refresh();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		TitleText = _titleTextSource?.ToString();
		RefreshExtendText();
		RefreshExpectedChangeText();
	}

	protected override void OnPeriodicRefresh()
	{
		base.OnPeriodicRefresh();
		Refresh();
	}

	protected override void OnIsExtendedChanged()
	{
		base.OnIsExtendedChanged();
		base.IsActive = false;
		Refresh();
	}

	private void Refresh()
	{
		InvokeRefreshData(this);
		RefreshExtendText();
		RefreshExpectedChangeText();
	}

	private void RefreshExpectedChangeText()
	{
		if (_summaryTextSource != null)
		{
			string text = "DefaultChange";
			if (_valueCategorization != ValueCategorization.None)
			{
				text = (((float)((_valueCategorization == ValueCategorization.LargeIsBetter) ? 1 : (-1)) * CurrentExpectedChange < 0f) ? "NegativeChange" : "PositiveChange");
			}
			TextObject textObject = GameTexts.FindText("str_LEFT_colon_RIGHT_wSpaceAfterColon");
			textObject.SetTextVariable("LEFT", _summaryTextSource.ToString());
			textObject.SetTextVariable("RIGHT", "<span style=\"" + text + "\">" + $"{CurrentExpectedChange:0.##}" + "</span>");
			ExpectedChangeText = textObject.ToString();
		}
	}

	private void RefreshExtendText()
	{
		GameTexts.SetVariable("EXTEND_KEY", GameTexts.FindText("str_game_key_text", "anyalt").ToString());
		ExtendText = GameTexts.FindText("str_map_tooltip_info").ToString();
	}

	public static void RefreshGenericRundownTooltip(RundownTooltipVM rundownTooltip, object[] args)
	{
		rundownTooltip.IsActive = rundownTooltip.IsInitializedProperly;
		if (!rundownTooltip.IsActive)
		{
			return;
		}
		Func<List<RundownLineVM>> func = args[0] as Func<List<RundownLineVM>>;
		Func<List<RundownLineVM>> func2 = args[1] as Func<List<RundownLineVM>>;
		float num = 0f;
		rundownTooltip.Lines.Clear();
		List<RundownLineVM> list = ((rundownTooltip.IsExtended && func2 != null) ? func2 : func)?.Invoke();
		if (list != null)
		{
			foreach (RundownLineVM item in list)
			{
				num += item.Value;
				rundownTooltip.Lines.Add(item);
			}
		}
		rundownTooltip.CurrentExpectedChange = num;
	}
}
