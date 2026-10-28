using NavalDLC.Missions.Objects.UsableMachines;
using SandBox.ViewModelCollection.Missions.NameMarker;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.ViewModelCollection.Missions.NameMarkers;

public class NavalMissionShipControlPointMarkerTargetVM : MissionNameMarkerTargetVM<ShipControllerMachine>
{
	public NavalMissionShipControlPointMarkerTargetVM(ShipControllerMachine target)
		: base(target)
	{
		base.NameType = "Normal";
		base.IconType = "control_point";
		RefreshValues();
	}

	public override void UpdatePosition(Camera missionCamera)
	{
		if (Agent.Main == null || !base.Target.IsStandingPointAvailableForAgent(Agent.Main))
		{
			base.ScreenPosition = new Vec2(-5000f, -5000f);
			base.Distance = -1;
		}
		else if (base.Target.HandTargetEntity != null)
		{
			UpdatePositionWith(missionCamera, base.Target.HandTargetEntity.GlobalPosition + base.Target.HandTargetEntity.GetGlobalFrame().rotation.u * 1.5f);
		}
		else if (base.Target.ControllerEntity != null)
		{
			UpdatePositionWith(missionCamera, base.Target.ControllerEntity.GlobalPosition + base.Target.ControllerEntity.GetGlobalFrame().rotation.u * 1.5f);
		}
		else
		{
			UpdatePositionWith(missionCamera, base.Target.GameEntity.GlobalPosition + base.Target.GameEntity.GetGlobalFrame().rotation.u * 1.5f);
		}
	}

	protected override TextObject GetName()
	{
		return new TextObject("{=OGY9BKOM}Control the Ship");
	}
}
