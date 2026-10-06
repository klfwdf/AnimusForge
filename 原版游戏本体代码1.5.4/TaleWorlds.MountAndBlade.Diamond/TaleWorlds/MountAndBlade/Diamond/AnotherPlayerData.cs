using System;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
public class AnotherPlayerData
{
	public AnotherPlayerState PlayerState { get; set; }

	public int Experience { get; set; }

	public CustomBattleId? SpectatableCustomBattleId { get; set; }

	public AnotherPlayerData()
	{
	}

	public AnotherPlayerData(AnotherPlayerState anotherPlayerState, int anotherPlayerExperience)
	{
		PlayerState = anotherPlayerState;
		Experience = anotherPlayerExperience;
	}

	public AnotherPlayerData(AnotherPlayerState anotherPlayerState, int anotherPlayerExperience, CustomBattleId? spectatableCustomBattleId)
	{
		PlayerState = anotherPlayerState;
		Experience = anotherPlayerExperience;
		SpectatableCustomBattleId = spectatableCustomBattleId;
	}
}
