using System;
using Newtonsoft.Json;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
public class ClanPlayerInfo
{
	[JsonProperty]
	public PlayerId PlayerId { get; private set; }

	[JsonProperty]
	public string PlayerName { get; private set; }

	[JsonProperty]
	public AnotherPlayerState State { get; private set; }

	[JsonProperty]
	public string ActiveBadgeId { get; private set; }

	public ClanPlayerInfo(PlayerId playerId, string playerName, AnotherPlayerState anotherPlayerState, string activeBadgeId)
	{
		PlayerId = playerId;
		PlayerName = playerName;
		ActiveBadgeId = activeBadgeId;
		State = anotherPlayerState;
	}
}
