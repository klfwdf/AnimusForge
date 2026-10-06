using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.Objectives;

namespace NavalDLC.Storyline.Objectives.Quest3;

internal class CheckpointObjectiveTarget : MissionObjectiveTarget
{
	public GameEntity GameEntity { get; private set; }

	public bool Active { get; private set; }

	public VolumeBox VolumeBox { get; private set; }

	public float Radius { get; private set; } = 20f;

	public TextObject Name { get; private set; }

	public CheckpointObjectiveTarget(GameEntity gameEntity)
	{
		GameEntity = gameEntity;
		VolumeBox = GameEntity?.GetFirstScriptOfType<VolumeBox>();
		Active = false;
		Name = TextObject.GetEmpty();
	}

	public void SetActive(bool isActive)
	{
		Active = isActive;
	}

	public void SetRadius(float radius)
	{
		Radius = radius;
	}

	public void SetName(TextObject name)
	{
		Name = name;
	}

	public override Vec3 GetGlobalPosition()
	{
		return GameEntity.GlobalPosition;
	}

	public bool IsInside(Vec3 position)
	{
		if (VolumeBox != null)
		{
			return VolumeBox.IsPointIn(position);
		}
		return GetGlobalPosition().DistanceSquared(position) <= Radius * Radius;
	}

	public override TextObject GetName()
	{
		return Name;
	}

	public override bool IsActive()
	{
		return Active;
	}
}
