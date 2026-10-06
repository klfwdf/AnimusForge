namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection;

public static class MultiplayerSpectatorHelper
{
	public static bool IsLocalPeerSpectator()
	{
		return SpectatorHelper.IsLocalPeerSpectator();
	}

	public static bool ShouldShowBothTeamsData()
	{
		return IsStreamerModeActive();
	}

	public static bool IsStreamerModeActive()
	{
		if (IsLocalPeerSpectator())
		{
			return MultiplayerOptions.OptionType.StreamerModeEnabled.GetBoolValue();
		}
		return false;
	}
}
