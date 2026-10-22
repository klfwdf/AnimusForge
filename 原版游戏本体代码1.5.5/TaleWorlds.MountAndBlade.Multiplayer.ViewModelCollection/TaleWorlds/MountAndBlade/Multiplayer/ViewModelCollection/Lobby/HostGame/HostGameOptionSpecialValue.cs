using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.HostGame;

public sealed class HostGameOptionSpecialValue
{
	public readonly int Value;

	public readonly TextObject Label;

	public HostGameOptionSpecialValue(int value, TextObject label)
	{
		Value = value;
		Label = label;
	}
}
