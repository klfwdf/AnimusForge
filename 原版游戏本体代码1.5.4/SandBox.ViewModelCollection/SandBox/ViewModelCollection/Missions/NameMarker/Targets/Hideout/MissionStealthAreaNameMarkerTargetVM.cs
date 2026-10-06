using SandBox.Objects.AreaMarkers;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace SandBox.ViewModelCollection.Missions.NameMarker.Targets.Hideout;

public class MissionStealthAreaNameMarkerTargetVM : MissionNameMarkerTargetVM<StealthAreaMarker>
{
	private readonly Vec3 _position;

	public MissionStealthAreaNameMarkerTargetVM(StealthAreaMarker target, Vec3 position)
		: base(target)
	{
		_position = position;
		base.NameType = "Passage";
		base.IconType = "stealth_area";
		RefreshValues();
	}

	public override void UpdatePosition(Camera missionCamera)
	{
		UpdatePositionWith(missionCamera, _position + MissionNameMarkerHelper.DefaultHeightOffset);
	}

	protected override TextObject GetName()
	{
		return new TextObject("{=WcSky2KB}Stealth Area");
	}
}
