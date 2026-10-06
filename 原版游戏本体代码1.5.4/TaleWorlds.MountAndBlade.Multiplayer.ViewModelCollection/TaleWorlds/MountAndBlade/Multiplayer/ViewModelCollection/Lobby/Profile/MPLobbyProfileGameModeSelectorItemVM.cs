using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Profile;

public class MPLobbyProfileGameModeSelectorItemVM : SelectorItemVM
{
	public string GameModeCode { get; private set; }

	public MPLobbyProfileGameModeSelectorItemVM(string gameModeCode, TextObject gameModeName)
		: base(gameModeName)
	{
		GameModeCode = gameModeCode;
	}
}
