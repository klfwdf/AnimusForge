using SandBox.Objects.Usables;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace SandBox.View.Missions;

public class StealthMissionUIHandler : MissionView
{
	public override void OnObjectUsed(Agent userAgent, UsableMissionObject usedObject)
	{
		base.OnObjectUsed(userAgent, usedObject);
		if (usedObject is StealthAreaUsePoint)
		{
			CameraFadeInFadeOut(0.5f, 0.5f, 1f);
		}
	}

	private void CameraFadeInFadeOut(float fadeOutTime, float blackTime, float fadeInTime)
	{
		if (!ScreenFadeController.IsFadeActive)
		{
			ScreenFadeController.BeginFadeOutAndIn(fadeOutTime, blackTime, fadeInTime);
		}
	}
}
