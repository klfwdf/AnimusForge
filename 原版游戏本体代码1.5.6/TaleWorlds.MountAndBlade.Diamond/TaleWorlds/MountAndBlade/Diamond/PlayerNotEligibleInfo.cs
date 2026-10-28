using System;
using Newtonsoft.Json;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
public class PlayerNotEligibleInfo
{
	[JsonProperty]
	public PlayerId PlayerId { get; private set; }

	[JsonProperty]
	public PlayerNotEligibleError[] Errors { get; private set; }

	public PlayerNotEligibleInfo(PlayerId playerId, PlayerNotEligibleError[] errors)
	{
		PlayerId = playerId;
		Errors = errors;
	}
}
