using TaleWorlds.Engine;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.ViewModelCollection.Missions.NameMarker.Targets.Hideout;

public class MissionStealthSentryNameMarkerTargetVM : MissionNameMarkerTargetVM<Agent>
{
	public MissionStealthSentryNameMarkerTargetVM(Agent target)
		: base(target)
	{
		base.IconType = "sentry";
		base.NameType = "Enemy";
		base.IsEnemy = true;
		RefreshValues();
	}

	public override void UpdatePosition(Camera missionCamera)
	{
		UpdatePositionWith(missionCamera, base.Target.GetEyeGlobalPosition() + MissionNameMarkerHelper.AgentHeightOffset);
	}

	protected override TextObject GetName()
	{
		return new TextObject("{=KdT0PM8Y}Sentry");
	}
}
