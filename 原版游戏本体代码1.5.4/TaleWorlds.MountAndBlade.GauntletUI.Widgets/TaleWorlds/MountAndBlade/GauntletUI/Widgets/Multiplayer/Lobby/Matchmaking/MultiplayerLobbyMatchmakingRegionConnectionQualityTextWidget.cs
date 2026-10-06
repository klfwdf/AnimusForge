using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.Lobby.Matchmaking;

public class MultiplayerLobbyMatchmakingRegionConnectionQualityTextWidget : TextWidget
{
	private int _connectionQualityLevel;

	[Editor(false)]
	public int ConnectionQualityLevel
	{
		get
		{
			return _connectionQualityLevel;
		}
		set
		{
			if (_connectionQualityLevel != value)
			{
				_connectionQualityLevel = value;
				OnPropertyChanged(value, "ConnectionQualityLevel");
				ConnectionQualityLevelUpdated();
			}
		}
	}

	public MultiplayerLobbyMatchmakingRegionConnectionQualityTextWidget(UIContext context)
		: base(context)
	{
		AddState("PoorQuality");
		AddState("AverageQuality");
		AddState("GoodQuality");
		ConnectionQualityLevelUpdated();
	}

	private void ConnectionQualityLevelUpdated()
	{
		switch (ConnectionQualityLevel)
		{
		case 0:
			SetState("PoorQuality");
			break;
		case 1:
			SetState("AverageQuality");
			break;
		case 2:
			SetState("GoodQuality");
			break;
		default:
			SetState("Default");
			break;
		}
	}
}
