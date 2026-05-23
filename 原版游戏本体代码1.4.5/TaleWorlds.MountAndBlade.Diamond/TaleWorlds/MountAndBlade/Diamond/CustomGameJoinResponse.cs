namespace TaleWorlds.MountAndBlade.Diamond;

public enum CustomGameJoinResponse
{
	Success,
	IncorrectPlayerState,
	ServerCapacityIsFull,
	ErrorOnGameServer,
	GameServerAccessError,
	CustomGameServerNotAvailable,
	CustomGameServerFinishing,
	IncorrectPassword,
	PlayerBanned,
	HostReplyTimedOut,
	NoPlayerDataFound,
	UnspecifiedError,
	NoPlayersCanJoin,
	AlreadyRequestedWaitingForServerResponse,
	RequesterIsNotPartyLeader,
	NotAllPlayersReady,
	NotAllPlayersModulesMatchWithServer
}
