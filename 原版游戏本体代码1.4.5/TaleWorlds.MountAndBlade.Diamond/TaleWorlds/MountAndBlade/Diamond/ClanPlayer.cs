using System;
using Newtonsoft.Json;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
public class ClanPlayer
{
	[JsonProperty]
	public PlayerId PlayerId { get; private set; }

	[JsonProperty]
	public Guid ClanId { get; private set; }

	[JsonProperty]
	public ClanPlayerRole Role { get; private set; }

	public ClanPlayer(PlayerId playerId, Guid clanId, ClanPlayerRole role)
	{
		PlayerId = playerId;
		ClanId = clanId;
		Role = role;
	}
}
