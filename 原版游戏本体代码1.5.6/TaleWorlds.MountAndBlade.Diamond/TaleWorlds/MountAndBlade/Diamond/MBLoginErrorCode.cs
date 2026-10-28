namespace TaleWorlds.MountAndBlade.Diamond;

public enum MBLoginErrorCode
{
	None,
	CouldNotLogin,
	VersionMismatch,
	IncorrectPassword,
	FamilyShareNotAllowed,
	BannedFromGame,
	NoAuthenticationToken,
	AuthTokenExpired,
	BannedFromHostingServers,
	CustomBattleServerIncompatibleVersion,
	ReachedMaxNumberofCustomBattleServers,
	CouldNotDestroyOldSession,
	LoggingInDisabled
}
