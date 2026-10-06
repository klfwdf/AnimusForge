using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class StandingPointWithVolumeBox : StandingPointWithWeaponRequirement
{
	private const float MaxUserAgentDistance = 10f;

	private const float MaxUserAgentElevation = 2f;

	public string VolumeBoxTag = "volumebox";

	public override Agent.AIScriptedFrameFlags DisableScriptedFrameFlags => Agent.AIScriptedFrameFlags.NoAttack;

	public override bool IsDisabledForAgent(Agent agent)
	{
		if (!base.IsDisabledForAgent(agent) && !(MathF.Abs(agent.Position.z - base.GameEntity.GlobalPosition.z) > 2f))
		{
			return agent.Position.DistanceSquared(base.GameEntity.GlobalPosition) > 100f;
		}
		return true;
	}

	protected internal override void OnEditorTick(float dt)
	{
		base.OnEditorTick(dt);
		MBEditor.IsEntitySelected(base.GameEntity);
	}
}
