using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade;

public static class CombatSoundContainer
{
	public static int SoundCodeMissionCombatBluntHigh { get; private set; }

	public static int SoundCodeMissionCombatBluntLow { get; private set; }

	public static int SoundCodeMissionCombatBluntMed { get; private set; }

	public static int SoundCodeMissionCombatBoulderHigh { get; private set; }

	public static int SoundCodeMissionCombatBoulderLow { get; private set; }

	public static int SoundCodeMissionCombatBoulderMed { get; private set; }

	public static int SoundCodeMissionCombatCutHigh { get; private set; }

	public static int SoundCodeMissionCombatCutLow { get; private set; }

	public static int SoundCodeMissionCombatCutMed { get; private set; }

	public static int SoundCodeMissionCombatMissileHigh { get; private set; }

	public static int SoundCodeMissionCombatMissileLow { get; private set; }

	public static int SoundCodeMissionCombatMissileMed { get; private set; }

	public static int SoundCodeMissionCombatPierceHigh { get; private set; }

	public static int SoundCodeMissionCombatPierceLow { get; private set; }

	public static int SoundCodeMissionCombatPierceMed { get; private set; }

	public static int SoundCodeMissionCombatPunchHigh { get; private set; }

	public static int SoundCodeMissionCombatPunchLow { get; private set; }

	public static int SoundCodeMissionCombatPunchMed { get; private set; }

	public static int SoundCodeMissionCombatThrowingAxeHigh { get; private set; }

	public static int SoundCodeMissionCombatThrowingAxeLow { get; private set; }

	public static int SoundCodeMissionCombatThrowingAxeMed { get; private set; }

	public static int SoundCodeMissionCombatThrowingDaggerHigh { get; private set; }

	public static int SoundCodeMissionCombatThrowingDaggerLow { get; private set; }

	public static int SoundCodeMissionCombatThrowingDaggerMed { get; private set; }

	public static int SoundCodeMissionCombatThrowingStoneHigh { get; private set; }

	public static int SoundCodeMissionCombatThrowingStoneLow { get; private set; }

	public static int SoundCodeMissionCombatThrowingStoneMed { get; private set; }

	public static int SoundCodeMissionCombatChargeDamage { get; private set; }

	public static int SoundCodeMissionCombatKick { get; private set; }

	public static int SoundCodeMissionCombatPlayerhit { get; private set; }

	public static int SoundCodeMissionCombatWoodShieldBash { get; private set; }

	public static int SoundCodeMissionCombatMetalShieldBash { get; private set; }

	static CombatSoundContainer()
	{
		UpdateMissionCombatSoundCodes();
	}

	private static void UpdateMissionCombatSoundCodes()
	{
		SoundCodeMissionCombatBluntHigh = SoundEvent.GetEventIdFromString("event:/mission/combat/blunt/high");
		SoundCodeMissionCombatBluntLow = SoundEvent.GetEventIdFromString("event:/mission/combat/blunt/low");
		SoundCodeMissionCombatBluntMed = SoundEvent.GetEventIdFromString("event:/mission/combat/blunt/med");
		SoundCodeMissionCombatBoulderHigh = SoundEvent.GetEventIdFromString("event:/mission/combat/boulder/high");
		SoundCodeMissionCombatBoulderLow = SoundEvent.GetEventIdFromString("event:/mission/combat/boulder/low");
		SoundCodeMissionCombatBoulderMed = SoundEvent.GetEventIdFromString("event:/mission/combat/boulder/med");
		SoundCodeMissionCombatCutHigh = SoundEvent.GetEventIdFromString("event:/mission/combat/cut/high");
		SoundCodeMissionCombatCutLow = SoundEvent.GetEventIdFromString("event:/mission/combat/cut/low");
		SoundCodeMissionCombatCutMed = SoundEvent.GetEventIdFromString("event:/mission/combat/cut/med");
		SoundCodeMissionCombatMissileHigh = SoundEvent.GetEventIdFromString("event:/mission/combat/missile/high");
		SoundCodeMissionCombatMissileLow = SoundEvent.GetEventIdFromString("event:/mission/combat/missile/low");
		SoundCodeMissionCombatMissileMed = SoundEvent.GetEventIdFromString("event:/mission/combat/missile/med");
		SoundCodeMissionCombatPierceHigh = SoundEvent.GetEventIdFromString("event:/mission/combat/pierce/high");
		SoundCodeMissionCombatPierceLow = SoundEvent.GetEventIdFromString("event:/mission/combat/pierce/low");
		SoundCodeMissionCombatPierceMed = SoundEvent.GetEventIdFromString("event:/mission/combat/pierce/med");
		SoundCodeMissionCombatPunchHigh = SoundEvent.GetEventIdFromString("event:/mission/combat/punch/high");
		SoundCodeMissionCombatPunchLow = SoundEvent.GetEventIdFromString("event:/mission/combat/punch/low");
		SoundCodeMissionCombatPunchMed = SoundEvent.GetEventIdFromString("event:/mission/combat/punch/med");
		SoundCodeMissionCombatThrowingAxeHigh = SoundEvent.GetEventIdFromString("event:/mission/combat/throwing/high");
		SoundCodeMissionCombatThrowingAxeLow = SoundEvent.GetEventIdFromString("event:/mission/combat/throwing/low");
		SoundCodeMissionCombatThrowingAxeMed = SoundEvent.GetEventIdFromString("event:/mission/combat/throwing/med");
		SoundCodeMissionCombatThrowingDaggerHigh = SoundEvent.GetEventIdFromString("event:/mission/combat/throwing/high");
		SoundCodeMissionCombatThrowingDaggerLow = SoundEvent.GetEventIdFromString("event:/mission/combat/throwing/low");
		SoundCodeMissionCombatThrowingDaggerMed = SoundEvent.GetEventIdFromString("event:/mission/combat/throwing/med");
		SoundCodeMissionCombatThrowingStoneHigh = SoundEvent.GetEventIdFromString("event:/mission/combat/throwingstone/high");
		SoundCodeMissionCombatThrowingStoneLow = SoundEvent.GetEventIdFromString("event:/mission/combat/throwingstone/low");
		SoundCodeMissionCombatThrowingStoneMed = SoundEvent.GetEventIdFromString("event:/mission/combat/throwingstone/med");
		SoundCodeMissionCombatChargeDamage = SoundEvent.GetEventIdFromString("event:/mission/combat/charge/damage");
		SoundCodeMissionCombatKick = SoundEvent.GetEventIdFromString("event:/mission/combat/kick");
		SoundCodeMissionCombatPlayerhit = SoundEvent.GetEventIdFromString("event:/mission/combat/playerHit");
		SoundCodeMissionCombatWoodShieldBash = SoundEvent.GetEventIdFromString("event:/mission/combat/shield/bash");
		SoundCodeMissionCombatMetalShieldBash = SoundEvent.GetEventIdFromString("event:/mission/combat/shield/metal_bash");
	}
}
