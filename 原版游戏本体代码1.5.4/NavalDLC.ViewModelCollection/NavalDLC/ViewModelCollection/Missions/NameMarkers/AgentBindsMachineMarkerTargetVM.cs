using NavalDLC.Missions.Objects.UsableMachines;
using SandBox.ViewModelCollection.Missions.NameMarker;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.ViewModelCollection.Missions.NameMarkers;

public class AgentBindsMachineMarkerTargetVM : MissionNameMarkerTargetVM<AgentBindsMachine>
{
	public AgentBindsMachineMarkerTargetVM(AgentBindsMachine target)
		: base(target)
	{
		base.NameType = "Normal";
		base.IconType = "prisoner";
		RefreshValues();
	}

	public override void UpdatePosition(Camera missionCamera)
	{
		if (Agent.Main == null || !base.Target.IsStandingPointAvailableForAgent(Agent.Main))
		{
			base.ScreenPosition = new Vec2(-5000f, -5000f);
			base.Distance = -1;
		}
		else
		{
			UpdatePositionWith(missionCamera, base.Target.GameEntity.GlobalPosition + base.Target.GameEntity.GetGlobalFrame().rotation.u * 1.5f);
		}
	}

	protected override TextObject GetName()
	{
		return new TextObject("{=mx9zqEzQ}Unchain");
	}
}
