using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.Scoreboard;

public class MultiplayerScoreboardScreenWidget : Widget
{
	private bool _isSingleSide;

	private int _singleColumnedWidth;

	private int _doubleColumnedWidth;

	private ListPanel _sidesList;

	[DataSourceProperty]
	public bool IsSingleSide
	{
		get
		{
			return _isSingleSide;
		}
		set
		{
			if (value != _isSingleSide)
			{
				_isSingleSide = value;
				OnPropertyChanged(value, "IsSingleSide");
				UpdateSidesList();
			}
		}
	}

	[DataSourceProperty]
	public int SingleColumnedWidth
	{
		get
		{
			return _singleColumnedWidth;
		}
		set
		{
			if (value != _singleColumnedWidth)
			{
				_singleColumnedWidth = value;
				OnPropertyChanged(value, "SingleColumnedWidth");
			}
		}
	}

	[DataSourceProperty]
	public int DoubleColumnedWidth
	{
		get
		{
			return _doubleColumnedWidth;
		}
		set
		{
			if (value != _doubleColumnedWidth)
			{
				_doubleColumnedWidth = value;
				OnPropertyChanged(value, "DoubleColumnedWidth");
			}
		}
	}

	[DataSourceProperty]
	public ListPanel SidesList
	{
		get
		{
			return _sidesList;
		}
		set
		{
			if (value != _sidesList)
			{
				_sidesList = value;
				OnPropertyChanged(value, "SidesList");
				UpdateSidesList();
			}
		}
	}

	public MultiplayerScoreboardScreenWidget(UIContext context)
		: base(context)
	{
	}

	private void UpdateSidesList()
	{
		if (SidesList != null)
		{
			base.SuggestedWidth = (IsSingleSide ? SingleColumnedWidth : DoubleColumnedWidth);
		}
	}
}
