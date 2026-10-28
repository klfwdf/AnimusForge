using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace NavalDLC.Missions.NavalPhysics;

public class CustomNavalPhysicsParameters : ScriptComponentBehavior
{
	public bool BehaveLikeShip;

	public float FloatingForceMultiplier = 1f;

	public float LinearFrictionMultiplierRight = 1f;

	public float LinearFrictionMultiplierLeft = 1f;

	public float LinearFrictionMultiplierForward = 1f;

	public float LinearFrictionMultiplierBackward = 1f;

	public float LinearFrictionMultiplierUp = 1f;

	public float LinearFrictionMultiplierDown = 1f;

	public Vec3 AngularFrictionMultiplier = Vec3.One;

	public float ContinuousDriftSpeed;

	protected override void OnInit()
	{
		base.OnInit();
		base.GameEntity.GetFirstScriptOfType<NavalPhysics>().SetContinuousDriftSpeed(ContinuousDriftSpeed);
	}

	protected override void OnEditorTick(float dt)
	{
		base.OnEditorTick(dt);
		base.GameEntity.GetFirstScriptOfType<NavalPhysics>()?.SetContinuousDriftSpeed(ContinuousDriftSpeed);
	}
}
