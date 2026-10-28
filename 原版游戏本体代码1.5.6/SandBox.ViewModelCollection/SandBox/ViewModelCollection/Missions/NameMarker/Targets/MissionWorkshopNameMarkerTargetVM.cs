using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace SandBox.ViewModelCollection.Missions.NameMarker.Targets;

public class MissionWorkshopNameMarkerTargetVM : MissionNameMarkerTargetVM<Workshop>
{
	private readonly Vec3 _signPosition;

	public MissionWorkshopNameMarkerTargetVM(Workshop target, Vec3 signPosition)
		: base(target)
	{
		base.NameType = "Passage";
		base.IconType = target.WorkshopType.StringId;
		_signPosition = signPosition;
		RefreshValues();
	}

	public override void UpdatePosition(Camera missionCamera)
	{
		UpdatePositionWith(missionCamera, _signPosition + MissionNameMarkerHelper.DefaultHeightOffset);
	}

	protected override TextObject GetName()
	{
		return base.Target.WorkshopType.Name;
	}
}
