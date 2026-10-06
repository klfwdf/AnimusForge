using TaleWorlds.InputSystem;

namespace TaleWorlds.MountAndBlade;

public static class DebugSiegeBehavior
{
	public enum DebugStateAttacker
	{
		None,
		DebugAttackersToBallistae,
		DebugAttackersToMangonels,
		DebugAttackersToBattlements
	}

	public enum DebugStateDefender
	{
		None,
		DebugDefendersToBallistae,
		DebugDefendersToMangonels,
		DebugDefendersToRam,
		DebugDefendersToTower
	}

	public static bool ToggleTargetDebug;

	public static DebugStateAttacker DebugAttackState;

	public static DebugStateDefender DebugDefendState;

	public static void SiegeDebug()
	{
		if (Input.DebugInput.IsHotKeyPressed("DebugSiegeBehaviorHotkeyAimAtRam"))
		{
			DebugDefendState = DebugStateDefender.DebugDefendersToRam;
		}
		else if (Input.DebugInput.IsHotKeyPressed("DebugSiegeBehaviorHotkeyAimAtSt"))
		{
			DebugDefendState = DebugStateDefender.DebugDefendersToTower;
		}
		else if (Input.DebugInput.IsHotKeyPressed("DebugSiegeBehaviorHotkeyAimAtBallistas2"))
		{
			DebugDefendState = DebugStateDefender.DebugDefendersToBallistae;
		}
		else if (Input.DebugInput.IsHotKeyPressed("DebugSiegeBehaviorHotkeyAimAtMangonels2"))
		{
			DebugDefendState = DebugStateDefender.DebugDefendersToMangonels;
		}
		else if (Input.DebugInput.IsHotKeyPressed("DebugSiegeBehaviorHotkeyAimAtNone2"))
		{
			DebugDefendState = DebugStateDefender.None;
		}
		else if (Input.DebugInput.IsHotKeyPressed("DebugSiegeBehaviorHotkeyAimAtBallistas"))
		{
			DebugAttackState = DebugStateAttacker.DebugAttackersToBallistae;
		}
		else if (Input.DebugInput.IsHotKeyPressed("DebugSiegeBehaviorHotkeyAimAtMangonels"))
		{
			DebugAttackState = DebugStateAttacker.DebugAttackersToMangonels;
		}
		else if (Input.DebugInput.IsHotKeyPressed("DebugSiegeBehaviorHotkeyAimAtBattlements"))
		{
			DebugAttackState = DebugStateAttacker.DebugAttackersToBattlements;
		}
		else if (Input.DebugInput.IsHotKeyPressed("DebugSiegeBehaviorHotkeyAimAtNone"))
		{
			DebugAttackState = DebugStateAttacker.None;
		}
		else if (Input.DebugInput.IsHotKeyPressed("DebugSiegeBehaviorHotkeyTargetDebugActive"))
		{
			ToggleTargetDebug = true;
		}
		else if (Input.DebugInput.IsHotKeyPressed("DebugSiegeBehaviorHotkeyTargetDebugDisactive"))
		{
			ToggleTargetDebug = false;
		}
		_ = ToggleTargetDebug;
	}
}
