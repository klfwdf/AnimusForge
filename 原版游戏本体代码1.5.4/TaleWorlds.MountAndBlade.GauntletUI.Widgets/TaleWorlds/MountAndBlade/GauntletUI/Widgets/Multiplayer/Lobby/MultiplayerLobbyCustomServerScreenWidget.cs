using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.Lobby;

public class MultiplayerLobbyCustomServerScreenWidget : Widget
{
	private bool _isPartyLeader;

	private bool _isInParty;

	[Editor(false)]
	public bool IsPartyLeader
	{
		get
		{
			return _isPartyLeader;
		}
		set
		{
			if (_isPartyLeader != value)
			{
				_isPartyLeader = value;
				OnPropertyChanged(value, "IsPartyLeader");
			}
		}
	}

	[Editor(false)]
	public bool IsInParty
	{
		get
		{
			return _isInParty;
		}
		set
		{
			if (_isInParty != value)
			{
				_isInParty = value;
				OnPropertyChanged(value, "IsInParty");
			}
		}
	}

	public MultiplayerLobbyCustomServerScreenWidget(UIContext context)
		: base(context)
	{
	}
}
