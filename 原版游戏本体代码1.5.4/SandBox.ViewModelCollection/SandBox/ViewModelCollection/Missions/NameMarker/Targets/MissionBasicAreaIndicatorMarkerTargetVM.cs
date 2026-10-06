using SandBox.Objects.AreaMarkers;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace SandBox.ViewModelCollection.Missions.NameMarker.Targets;

public class MissionBasicAreaIndicatorMarkerTargetVM : MissionNameMarkerTargetVM<BasicAreaIndicator>
{
	private readonly Vec3 _position;

	public MissionBasicAreaIndicatorMarkerTargetVM(BasicAreaIndicator target, Vec3 position)
		: base(target)
	{
		base.NameType = "Passage";
		base.IconType = (string.IsNullOrEmpty(base.Target.Type) ? "common_area" : base.Target.Type);
		_position = position;
		RefreshValues();
	}

	public override void UpdatePosition(Camera missionCamera)
	{
		UpdatePositionWith(missionCamera, _position + MissionNameMarkerHelper.DefaultHeightOffset);
	}

	protected override TextObject GetName()
	{
		return base.Target.GetName();
	}
}
