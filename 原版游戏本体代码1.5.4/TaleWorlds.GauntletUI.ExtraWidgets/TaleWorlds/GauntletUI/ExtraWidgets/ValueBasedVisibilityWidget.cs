using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.GauntletUI.ExtraWidgets;

public class ValueBasedVisibilityWidget : Widget
{
	public enum WatchTypes
	{
		Equal,
		BiggerThan,
		BiggerThanEqual,
		LessThan,
		LessThanEqual,
		NotEqual
	}

	private WatchTypes _watchType;

	private float _indexToBeVisibleFloat;

	private float _indexToWatchFloat = -1f;

	public WatchTypes WatchType
	{
		get
		{
			return _watchType;
		}
		set
		{
			if (value != _watchType)
			{
				_watchType = value;
				UpdateIsVisible();
			}
		}
	}

	[Editor(false)]
	public int IndexToWatch
	{
		get
		{
			return (int)IndexToWatchFloat;
		}
		set
		{
			IndexToWatchFloat = value;
		}
	}

	[Editor(false)]
	public float IndexToWatchFloat
	{
		get
		{
			return _indexToWatchFloat;
		}
		set
		{
			if (_indexToWatchFloat != value)
			{
				_indexToWatchFloat = value;
				OnPropertyChanged(value, "IndexToWatchFloat");
				UpdateIsVisible();
			}
		}
	}

	[Editor(false)]
	public int IndexToBeVisible
	{
		get
		{
			return (int)IndexToBeVisibleFloat;
		}
		set
		{
			IndexToBeVisibleFloat = value;
		}
	}

	[Editor(false)]
	public float IndexToBeVisibleFloat
	{
		get
		{
			return _indexToBeVisibleFloat;
		}
		set
		{
			if (_indexToBeVisibleFloat != value)
			{
				_indexToBeVisibleFloat = value;
				OnPropertyChanged(value, "IndexToBeVisibleFloat");
				UpdateIsVisible();
			}
		}
	}

	public ValueBasedVisibilityWidget(UIContext context)
		: base(context)
	{
		UpdateIsVisible();
	}

	private void UpdateIsVisible()
	{
		switch (WatchType)
		{
		case WatchTypes.Equal:
			base.IsVisible = IndexToWatchFloat == IndexToBeVisibleFloat;
			break;
		case WatchTypes.BiggerThan:
			base.IsVisible = IndexToWatchFloat > IndexToBeVisibleFloat;
			break;
		case WatchTypes.LessThan:
			base.IsVisible = IndexToWatchFloat < IndexToBeVisibleFloat;
			break;
		case WatchTypes.BiggerThanEqual:
			base.IsVisible = IndexToWatchFloat >= IndexToBeVisibleFloat;
			break;
		case WatchTypes.LessThanEqual:
			base.IsVisible = IndexToWatchFloat <= IndexToBeVisibleFloat;
			break;
		case WatchTypes.NotEqual:
			base.IsVisible = IndexToWatchFloat != IndexToBeVisibleFloat;
			break;
		}
	}
}
