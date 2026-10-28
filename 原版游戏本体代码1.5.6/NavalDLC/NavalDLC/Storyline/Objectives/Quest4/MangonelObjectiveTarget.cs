using NavalDLC.Missions.Objects.UsableMachines;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Missions.Objectives;

namespace NavalDLC.Storyline.Objectives.Quest4;

public class MangonelObjectiveTarget : MissionObjectiveTarget
{
	private readonly ShipMangonel _shipMangonel;

	public MangonelObjectiveTarget(ShipMangonel shipMangonel)
	{
		_shipMangonel = shipMangonel;
	}

	public override bool IsActive()
	{
		ShipMangonel shipMangonel = _shipMangonel;
		if (shipMangonel == null)
		{
			return false;
		}
		return shipMangonel.DestructionComponent?.IsDestroyed == false;
	}

	public override TextObject GetName()
	{
		return new TextObject("{=NbpcDXtJ}Mangonel");
	}

	public override Vec3 GetGlobalPosition()
	{
		return _shipMangonel.GameEntity.GlobalPosition + Vec3.Up * 7f;
	}
}
