using System;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.GauntletUI.ExtraWidgets;

public class StringBasedVisibilityWidget : Widget
{
	public enum WatchTypes
	{
		Equal,
		NotEqual
	}

	private string _firstString;

	private string _secondString;

	public WatchTypes WatchType { get; set; }

	[Editor(false)]
	public string FirstString
	{
		get
		{
			return _firstString;
		}
		set
		{
			if (_firstString != value)
			{
				_firstString = value;
				OnPropertyChanged(value, "FirstString");
				switch (WatchType)
				{
				case WatchTypes.Equal:
					base.IsVisible = string.Equals(value, SecondString, StringComparison.OrdinalIgnoreCase);
					break;
				case WatchTypes.NotEqual:
					base.IsVisible = !string.Equals(value, SecondString, StringComparison.OrdinalIgnoreCase);
					break;
				}
			}
		}
	}

	[Editor(false)]
	public string SecondString
	{
		get
		{
			return _secondString;
		}
		set
		{
			if (_secondString != value)
			{
				_secondString = value;
				OnPropertyChanged(value, "SecondString");
				switch (WatchType)
				{
				case WatchTypes.Equal:
					base.IsVisible = string.Equals(value, FirstString, StringComparison.OrdinalIgnoreCase);
					break;
				case WatchTypes.NotEqual:
					base.IsVisible = !string.Equals(value, FirstString, StringComparison.OrdinalIgnoreCase);
					break;
				}
			}
		}
	}

	public StringBasedVisibilityWidget(UIContext context)
		: base(context)
	{
	}
}
