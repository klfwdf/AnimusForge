using SandBox.Objects.Usables;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace SandBox.ViewModelCollection.Missions.NameMarker.Targets.Hideout;

public class MissionStealthAreaUsePointNameMarkerTargetVM : MissionNameMarkerTargetBaseVM
{
	private StealthAreaUsePoint _usePoint;

	public MissionStealthAreaUsePointNameMarkerTargetVM(StealthAreaUsePoint usePoint)
	{
		_usePoint = usePoint;
		base.IconType = "call_troops";
		base.NameType = "Normal";
		RefreshValues();
	}

	public override bool Equals(MissionNameMarkerTargetBaseVM other)
	{
		return false;
	}

	public override void UpdatePosition(Camera missionCamera)
	{
		UpdatePositionWith(missionCamera, _usePoint.GameEntity.GetGlobalFrame().origin + Vec3.Up * 0.5f);
	}

	protected override TextObject GetName()
	{
		return new TextObject("{=GmjiZk9P}Call Troops");
	}
}
