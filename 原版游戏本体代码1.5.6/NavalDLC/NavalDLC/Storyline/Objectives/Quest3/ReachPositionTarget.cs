using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Missions.Objectives;

namespace NavalDLC.Storyline.Objectives.Quest3;

internal class ReachPositionTarget : MissionObjectiveTarget
{
	private readonly Vec3 _position;

	private readonly TextObject _name;

	internal ReachPositionTarget(Vec3 escapePosition, TextObject name)
	{
		_name = name;
		_position = escapePosition;
	}

	public override Vec3 GetGlobalPosition()
	{
		return _position + Vec3.Up * 3f;
	}

	public override TextObject GetName()
	{
		return _name;
	}

	public override bool IsActive()
	{
		return true;
	}
}
