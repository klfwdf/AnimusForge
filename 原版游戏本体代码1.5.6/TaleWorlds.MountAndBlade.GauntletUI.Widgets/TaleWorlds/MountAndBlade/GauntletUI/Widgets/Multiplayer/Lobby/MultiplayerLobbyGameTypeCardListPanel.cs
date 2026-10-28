using System.Collections.Generic;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.Lobby;

public class MultiplayerLobbyGameTypeCardListPanel : ListPanel
{
	private List<MultiplayerLobbyGameTypeCardButtonWidget> _cardButtons;

	public MultiplayerLobbyGameTypeCardListPanel(UIContext context)
		: base(context)
	{
		_cardButtons = new List<MultiplayerLobbyGameTypeCardButtonWidget>();
	}
}
