using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade.View.MissionViews.SiegeWeapon;

[DefaultView]
public class RangedSiegeWeaponViewController : MissionView
{
	public override void OnObjectUsed(Agent userAgent, UsableMissionObject usedObject)
	{
		base.OnObjectUsed(userAgent, usedObject);
		if (!userAgent.IsMainAgent || !(usedObject is StandingPoint))
		{
			return;
		}
		UsableMachine usableMachineFromPoint = GetUsableMachineFromPoint(usedObject as StandingPoint);
		if (usableMachineFromPoint is RangedSiegeWeapon)
		{
			RangedSiegeWeapon rangedSiegeWeapon = usableMachineFromPoint as RangedSiegeWeapon;
			if (rangedSiegeWeapon.GetComponent<RangedSiegeWeaponView>() == null)
			{
				AddRangedSiegeWeaponView(rangedSiegeWeapon);
			}
		}
	}

	private UsableMachine GetUsableMachineFromPoint(StandingPoint standingPoint)
	{
		WeakGameEntity weakGameEntity = standingPoint.GameEntity;
		while (weakGameEntity.IsValid && !weakGameEntity.HasScriptOfType<UsableMachine>())
		{
			weakGameEntity = weakGameEntity.Parent;
		}
		if (weakGameEntity.IsValid)
		{
			UsableMachine firstScriptOfType = weakGameEntity.GetFirstScriptOfType<UsableMachine>();
			if (firstScriptOfType != null)
			{
				return firstScriptOfType;
			}
		}
		return null;
	}

	private void AddRangedSiegeWeaponView(RangedSiegeWeapon rangedSiegeWeapon)
	{
		RangedSiegeWeaponView rangedSiegeWeaponView = null;
		rangedSiegeWeaponView = ((rangedSiegeWeapon is Trebuchet) ? new TrebuchetView() : ((rangedSiegeWeapon is Mangonel) ? new MangonelView() : ((!(rangedSiegeWeapon is Ballista)) ? new RangedSiegeWeaponView() : new BallistaView())));
		rangedSiegeWeaponView.Initialize(rangedSiegeWeapon, base.MissionScreen);
		rangedSiegeWeapon.AddComponent(rangedSiegeWeaponView);
	}
}
