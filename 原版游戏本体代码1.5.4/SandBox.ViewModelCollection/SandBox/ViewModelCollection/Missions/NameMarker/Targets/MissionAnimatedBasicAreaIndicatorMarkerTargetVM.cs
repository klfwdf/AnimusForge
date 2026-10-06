using SandBox.Objects.AreaMarkers;
using TaleWorlds.Engine;
using TaleWorlds.Localization;

namespace SandBox.ViewModelCollection.Missions.NameMarker.Targets;

public class MissionAnimatedBasicAreaIndicatorMarkerTargetVM : MissionNameMarkerTargetVM<AnimatedBasicAreaIndicator>
{
	public MissionAnimatedBasicAreaIndicatorMarkerTargetVM(AnimatedBasicAreaIndicator target)
		: base(target)
	{
		base.NameType = "Passage";
		base.IconType = (string.IsNullOrEmpty(base.Target.Type) ? "common_area" : base.Target.Type);
		RefreshValues();
	}

	public override void UpdatePosition(Camera missionCamera)
	{
		UpdatePositionWith(missionCamera, base.Target.GetPosition() + MissionNameMarkerHelper.DefaultHeightOffset);
	}

	protected override TextObject GetName()
	{
		return base.Target.GetName();
	}
}
