using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade;

public static class ItemPhysicsSoundContainer
{
	public static int SoundCodePhysicsBoulderDefault { get; private set; }

	public static int SoundCodePhysicsArrowlikeDefault { get; private set; }

	public static int SoundCodePhysicsBowlikeDefault { get; private set; }

	public static int SoundCodePhysicsDaggerlikeDefault { get; private set; }

	public static int SoundCodePhysicsGreatswordlikeDefault { get; private set; }

	public static int SoundCodePhysicsShieldlikeDefault { get; private set; }

	public static int SoundCodePhysicsSpearlikeDefault { get; private set; }

	public static int SoundCodePhysicsSwordlikeDefault { get; private set; }

	public static int SoundCodePhysicsBoulderWood { get; private set; }

	public static int SoundCodePhysicsArrowlikeWood { get; private set; }

	public static int SoundCodePhysicsBowlikeWood { get; private set; }

	public static int SoundCodePhysicsDaggerlikeWood { get; private set; }

	public static int SoundCodePhysicsGreatswordlikeWood { get; private set; }

	public static int SoundCodePhysicsShieldlikeWood { get; private set; }

	public static int SoundCodePhysicsSpearlikeWood { get; private set; }

	public static int SoundCodePhysicsSwordlikeWood { get; private set; }

	public static int SoundCodePhysicsBoulderStone { get; private set; }

	public static int SoundCodePhysicsArrowlikeStone { get; private set; }

	public static int SoundCodePhysicsBowlikeStone { get; private set; }

	public static int SoundCodePhysicsDaggerlikeStone { get; private set; }

	public static int SoundCodePhysicsGreatswordlikeStone { get; private set; }

	public static int SoundCodePhysicsShieldlikeStone { get; private set; }

	public static int SoundCodePhysicsSpearlikeStone { get; private set; }

	public static int SoundCodePhysicsSwordlikeStone { get; private set; }

	public static int SoundCodePhysicsWater { get; private set; }

	static ItemPhysicsSoundContainer()
	{
		UpdateItemPhysicsSoundCodes();
	}

	private static void UpdateItemPhysicsSoundCodes()
	{
		SoundCodePhysicsBoulderDefault = SoundEvent.GetEventIdFromString("event:/physics/boulder/default");
		SoundCodePhysicsArrowlikeDefault = SoundEvent.GetEventIdFromString("event:/physics/arrowlike/default");
		SoundCodePhysicsBowlikeDefault = SoundEvent.GetEventIdFromString("event:/physics/bowlike/default");
		SoundCodePhysicsDaggerlikeDefault = SoundEvent.GetEventIdFromString("event:/physics/daggerlike/default");
		SoundCodePhysicsGreatswordlikeDefault = SoundEvent.GetEventIdFromString("event:/physics/greatswordlike/default");
		SoundCodePhysicsShieldlikeDefault = SoundEvent.GetEventIdFromString("event:/physics/shieldlike/default");
		SoundCodePhysicsSpearlikeDefault = SoundEvent.GetEventIdFromString("event:/physics/spearlike/default");
		SoundCodePhysicsSwordlikeDefault = SoundEvent.GetEventIdFromString("event:/physics/swordlike/default");
		SoundCodePhysicsBoulderWood = SoundEvent.GetEventIdFromString("event:/physics/boulder/wood");
		SoundCodePhysicsArrowlikeWood = SoundEvent.GetEventIdFromString("event:/physics/arrowlike/wood");
		SoundCodePhysicsBowlikeWood = SoundEvent.GetEventIdFromString("event:/physics/bowlike/wood");
		SoundCodePhysicsDaggerlikeWood = SoundEvent.GetEventIdFromString("event:/physics/daggerlike/wood");
		SoundCodePhysicsGreatswordlikeWood = SoundEvent.GetEventIdFromString("event:/physics/greatswordlike/wood");
		SoundCodePhysicsShieldlikeWood = SoundEvent.GetEventIdFromString("event:/physics/shieldlike/wood");
		SoundCodePhysicsSpearlikeWood = SoundEvent.GetEventIdFromString("event:/physics/spearlike/wood");
		SoundCodePhysicsSwordlikeWood = SoundEvent.GetEventIdFromString("event:/physics/swordlike/wood");
		SoundCodePhysicsBoulderStone = SoundEvent.GetEventIdFromString("event:/physics/boulder/stone");
		SoundCodePhysicsArrowlikeStone = SoundEvent.GetEventIdFromString("event:/physics/arrowlike/stone");
		SoundCodePhysicsBowlikeStone = SoundEvent.GetEventIdFromString("event:/physics/bowlike/stone");
		SoundCodePhysicsDaggerlikeStone = SoundEvent.GetEventIdFromString("event:/physics/daggerlike/stone");
		SoundCodePhysicsGreatswordlikeStone = SoundEvent.GetEventIdFromString("event:/physics/greatswordlike/stone");
		SoundCodePhysicsShieldlikeStone = SoundEvent.GetEventIdFromString("event:/physics/shieldlike/stone");
		SoundCodePhysicsSpearlikeStone = SoundEvent.GetEventIdFromString("event:/physics/spearlike/stone");
		SoundCodePhysicsSwordlikeStone = SoundEvent.GetEventIdFromString("event:/physics/swordlike/stone");
		SoundCodePhysicsWater = SoundEvent.GetEventIdFromString("event:/physics/water");
	}
}
