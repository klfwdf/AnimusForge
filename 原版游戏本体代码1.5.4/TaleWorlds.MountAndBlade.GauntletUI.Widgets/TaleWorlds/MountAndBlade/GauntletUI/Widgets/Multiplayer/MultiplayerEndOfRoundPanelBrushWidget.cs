using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer;

public class MultiplayerEndOfRoundPanelBrushWidget : BrushWidget
{
	private bool _isShown;

	private bool _isRoundWinner;

	[DataSourceProperty]
	public bool IsShown
	{
		get
		{
			return _isShown;
		}
		set
		{
			if (value != _isShown)
			{
				_isShown = value;
				OnPropertyChanged(value, "IsShown");
				IsShownUpdated();
			}
		}
	}

	[DataSourceProperty]
	public bool IsRoundWinner
	{
		get
		{
			return _isRoundWinner;
		}
		set
		{
			if (value != _isRoundWinner)
			{
				_isRoundWinner = value;
				OnPropertyChanged(value, "IsRoundWinner");
			}
		}
	}

	public MultiplayerEndOfRoundPanelBrushWidget(UIContext context)
		: base(context)
	{
	}

	private void IsShownUpdated()
	{
		if (IsShown)
		{
			string eventName = (IsRoundWinner ? "Victory" : "Defeat");
			EventFired(eventName);
		}
	}
}
