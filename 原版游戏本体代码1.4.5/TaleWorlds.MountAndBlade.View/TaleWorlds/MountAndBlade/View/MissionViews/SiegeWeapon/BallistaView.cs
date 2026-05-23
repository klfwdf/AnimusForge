using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade.View.MissionViews.SiegeWeapon;

public class BallistaView : RangedSiegeWeaponView
{
	protected override void OnAdded(Scene scene)
	{
		base.OnAdded(scene);
		UsesMouseForAiming = true;
	}

	protected override void StartUsingWeaponCamera()
	{
		base.StartUsingWeaponCamera();
		base.MissionScreen.SetExtraCameraParameters(newForceCanZoom: true, 1.5f);
	}

	protected override void HandleUserCameraRotation(float dt)
	{
	}
}
