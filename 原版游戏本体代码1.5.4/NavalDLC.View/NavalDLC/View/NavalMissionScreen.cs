using NavalDLC.HotKeyCategories;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.Objects;
using TaleWorlds.Engine.Screens;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Screens;

namespace NavalDLC.View;

[GameStateScreen(typeof(NavalMissionState))]
internal class NavalMissionScreen : MissionScreen
{
	private NavalShipsLogic _navalShipsLogic;

	public NavalMissionScreen(MissionState missionState)
		: base(missionState)
	{
	}

	protected override void InitializeMissionView()
	{
		base.InitializeMissionView();
		FindLayer<SceneLayer>()?.Input.RegisterHotKeyCategory(new NavalCheatsHotKeyCategory());
		_navalShipsLogic = base.Mission.GetMissionBehavior<NavalShipsLogic>();
	}

	protected override bool CanViewCharacter()
	{
		if (_navalShipsLogic == null)
		{
			return true;
		}
		return _navalShipsLogic.PlayerControlledShip == null;
	}

	protected override bool CanToggleCamera()
	{
		if (_navalShipsLogic?.PlayerControlledShip == null)
		{
			return base.CanToggleCamera();
		}
		return false;
	}

	public override void TeleportMainAgentToCameraFocusForCheat()
	{
		NavalShipsLogic missionBehavior = Mission.Current.GetMissionBehavior<NavalShipsLogic>();
		MissionShip missionShip = missionBehavior?.PlayerControlledShip;
		if (missionShip != null)
		{
			MatrixFrame globalFrame = missionShip.GlobalFrame;
			MatrixFrame lastFinalRenderCameraFrame = base.Mission.Scene.LastFinalRenderCameraFrame;
			float num = globalFrame.origin.Z - lastFinalRenderCameraFrame.origin.Z;
			Vec3 vec = -lastFinalRenderCameraFrame.rotation.u;
			float num2 = num / vec.Z;
			Vec3 direction = lastFinalRenderCameraFrame.rotation.f;
			direction.z = 0f;
			direction.Normalize();
			if (num2 <= 400f)
			{
				vec *= num2;
				globalFrame.origin = lastFinalRenderCameraFrame.origin + vec;
				globalFrame.origin = new Vec3(globalFrame.origin.AsVec2, Mission.Current.Scene.GetWaterLevelAtPosition(globalFrame.origin.AsVec2, useWaterRenderer: true, checkWaterBodyEntities: false));
				globalFrame.rotation = Mat3.CreateMat3WithForward(in direction);
				missionBehavior.TeleportShip(missionShip, globalFrame, checkFreeArea: false);
			}
		}
		else
		{
			base.TeleportMainAgentToCameraFocusForCheat();
		}
	}
}
