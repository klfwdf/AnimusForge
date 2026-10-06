using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.CustomBattle.CustomBattle;

namespace TaleWorlds.MountAndBlade.CustomBattle;

public class CustomBattleState : GameState
{
	public override bool IsMusicMenuState => true;

	protected override void OnInitialize()
	{
		base.OnInitialize();
		CustomBattleHelper.AssertMissingTroopsForDebug();
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("enable_custom_record", "replay_mission")]
	public static string EnableRecordMission(List<string> strings)
	{
		if (!(GameStateManager.Current.ActiveState is CustomBattleState))
		{
			return "Mission recording for custom battle can only be enabled while in custom battle screen.";
		}
		MissionState.RecordMission = true;
		return "Mission recording activated.";
	}
}
