using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.Scoreboard;

public class MultiplayerScoreboardStatsListPanel : ListPanel
{
	private ContainerItemDescription _nameColumnItemDescription;

	private ContainerItemDescription _scoreColumnItemDescription;

	private ContainerItemDescription _soldiersColumnItemDescription;

	private const string _nameColumnWidgetID = "name";

	private const string _scoreColumnWidgetID = "score";

	private const string _soldiersColumnWidgetID = "soldiers";

	private float _nameColumnWidthRatio = 1f;

	private float _scoreColumnWidthRatio = 1f;

	private float _soldiersColumnWidthRatio = 1f;

	public float NameColumnWidthRatio
	{
		get
		{
			return _nameColumnWidthRatio;
		}
		set
		{
			if (value != _nameColumnWidthRatio)
			{
				_nameColumnWidthRatio = value;
				OnPropertyChanged(value, "NameColumnWidthRatio");
				NameColumnWidthRatioUpdated();
			}
		}
	}

	public float ScoreColumnWidthRatio
	{
		get
		{
			return _scoreColumnWidthRatio;
		}
		set
		{
			if (value != _scoreColumnWidthRatio)
			{
				_scoreColumnWidthRatio = value;
				OnPropertyChanged(value, "ScoreColumnWidthRatio");
				ScoreColumnWidthRatioUpdated();
			}
		}
	}

	public float SoldiersColumnWidthRatio
	{
		get
		{
			return _soldiersColumnWidthRatio;
		}
		set
		{
			if (value != _soldiersColumnWidthRatio)
			{
				_soldiersColumnWidthRatio = value;
				OnPropertyChanged(value, "SoldiersColumnWidthRatio");
				SoldiersColumnWidthRatioUpdated();
			}
		}
	}

	public MultiplayerScoreboardStatsListPanel(UIContext context)
		: base(context)
	{
		_nameColumnItemDescription = new ContainerItemDescription
		{
			WidgetId = "name"
		};
		_scoreColumnItemDescription = new ContainerItemDescription
		{
			WidgetId = "score"
		};
		_soldiersColumnItemDescription = new ContainerItemDescription
		{
			WidgetId = "soldiers"
		};
	}

	private void NameColumnWidthRatioUpdated()
	{
		_nameColumnItemDescription.WidthStretchRatio = NameColumnWidthRatio;
		AddItemDescription(_nameColumnItemDescription);
		SetMeasureAndLayoutDirty();
	}

	private void ScoreColumnWidthRatioUpdated()
	{
		_scoreColumnItemDescription.WidthStretchRatio = ScoreColumnWidthRatio;
		AddItemDescription(_scoreColumnItemDescription);
		SetMeasureAndLayoutDirty();
	}

	private void SoldiersColumnWidthRatioUpdated()
	{
		_soldiersColumnItemDescription.WidthStretchRatio = SoldiersColumnWidthRatio;
		AddItemDescription(_soldiersColumnItemDescription);
		SetMeasureAndLayoutDirty();
	}
}
