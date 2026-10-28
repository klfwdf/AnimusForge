using SandBox.Objects;
using TaleWorlds.CampaignSystem.ViewModelCollection.Quests;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace SandBox.ViewModelCollection.Missions.NameMarker.Targets;

public class MissionPassageUsePointNameMarkerTargetVM : MissionNameMarkerTargetVM<PassageUsePoint>
{
	public MissionPassageUsePointNameMarkerTargetVM(PassageUsePoint target)
		: base(target)
	{
		base.NameType = "Passage";
		base.IconType = ((base.Target.ToLocation == null && base.Target.IsMissionExit) ? "center" : base.Target.ToLocation.StringId);
		base.Quests = new MBBindingList<QuestMarkerVM>();
		RefreshValues();
	}

	public override void UpdatePosition(Camera missionCamera)
	{
		UpdatePositionWith(missionCamera, base.Target.GameEntity.GlobalPosition + MissionNameMarkerHelper.DefaultHeightOffset);
	}

	protected override TextObject GetName()
	{
		if (base.Target.ToLocation == null && base.Target.IsMissionExit)
		{
			return GameTexts.FindText("str_mission_exit");
		}
		return base.Target.ToLocation.Name;
	}
}
