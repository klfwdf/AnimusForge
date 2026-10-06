namespace TaleWorlds.MountAndBlade.Diamond;

public enum DisconnectType
{
	QuitFromGame,
	TimedOut,
	KickedByHost,
	KickedByPoll,
	BannedByPoll,
	Inactivity,
	DisconnectedFromLobby,
	GameEnded,
	ServerNotResponding,
	KickedDueToFriendlyDamage,
	PlayStateMismatch,
	Unknown
}
