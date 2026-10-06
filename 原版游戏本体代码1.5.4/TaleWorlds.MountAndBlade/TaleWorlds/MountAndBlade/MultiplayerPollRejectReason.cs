namespace TaleWorlds.MountAndBlade;

public enum MultiplayerPollRejectReason
{
	NotEnoughPlayersToOpenPoll,
	HasOngoingPoll,
	TooManyPollRequests,
	KickPollTargetNotSynced,
	Count
}
