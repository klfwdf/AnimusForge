using NavalDLC.Storyline.MissionControllers;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace NavalDLC.View.MissionViews.Storyline;

public class Quest5SetPieceBattleBossFightCameraView : MissionView
{
	private Quest5SetPieceBattleMissionController _quest5SetPieceBattleMissionController;

	private Camera _bossFightCamera;

	public override void AfterStart()
	{
		base.AfterStart();
		_quest5SetPieceBattleMissionController = Mission.Current.GetMissionBehavior<Quest5SetPieceBattleMissionController>();
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		if (_quest5SetPieceBattleMissionController.State < Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.BossFightConversationInProgress)
		{
			return;
		}
		if (_quest5SetPieceBattleMissionController.BossFightConversationCameraGameEntity != null)
		{
			if (_bossFightCamera == null)
			{
				Vec3 dofParams = Vec3.Invalid;
				_bossFightCamera = Camera.CreateCamera();
				_quest5SetPieceBattleMissionController.BossFightConversationCameraGameEntity.GetCameraParamsFromCameraScript(_bossFightCamera, ref dofParams);
				_bossFightCamera.SetFovVertical(_bossFightCamera.GetFovVertical(), Screen.AspectRatio, _bossFightCamera.Near, _bossFightCamera.Far);
			}
			else
			{
				_bossFightCamera.Frame = _quest5SetPieceBattleMissionController.BossFightConversationCameraGameEntity.GetGlobalFrame();
			}
			base.MissionScreen.CustomCamera = _bossFightCamera;
		}
		else if (base.MissionScreen.CustomCamera != null)
		{
			base.MissionScreen.CustomCamera = null;
		}
	}
}
