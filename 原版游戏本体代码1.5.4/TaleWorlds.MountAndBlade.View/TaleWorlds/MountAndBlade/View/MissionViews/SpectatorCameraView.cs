using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.MountAndBlade.View.MissionViews;

public class SpectatorCameraView : MissionView
{
	private const float _clickToFollowMaxScreenDistanceSq = 0.0016f;

	private const int SpectateCameraSlotCount = 9;

	private List<MatrixFrame> _spectateCameraFrames = new List<MatrixFrame>();

	private bool[] _spectateCameraFrameIsSet = new bool[9];

	private SpectatorCameraTypes _cameraCycleMode;

	private bool _hasCameraCycleMode;

	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		base.MissionScreen.SceneLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("MultiplayerHotkeyCategory"));
		ScreenManager.TrySetFocus(base.MissionScreen.SceneLayer);
	}

	public override void AfterStart()
	{
		for (int i = 0; i < 9; i++)
		{
			_spectateCameraFrames.Add(MatrixFrame.Identity);
		}
		for (int j = 0; j < 9; j++)
		{
			string tag = "spectate_cam_" + j;
			List<GameEntity> list = Mission.Current.Scene.FindEntitiesWithTag(tag).ToList();
			if (list.Count > 0)
			{
				_spectateCameraFrames[j] = list[0].GetGlobalFrame();
				_spectateCameraFrameIsSet[j] = true;
			}
		}
	}

	public override void OnPreDisplayMissionTick(float dt)
	{
		base.OnPreDisplayMissionTick(dt);
		if (IsSpectatorPeer())
		{
			HandleClickToFollow(base.MissionScreen.SceneLayer.Input);
		}
	}

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		if (IsSpectatorPeer())
		{
			InputContext input = base.MissionScreen.SceneLayer.Input;
			if (input.IsControlDown())
			{
				HandleStoreCameraPosition(input);
			}
			else
			{
				HandleRecallCameraPosition(input);
			}
			HandlePovToggle(input);
			HandleCycleTarget(input);
		}
	}

	private void HandleCycleTarget(InputContext input)
	{
		if (input.IsHotKeyPressed("CycleSpectatorTargetPrevious"))
		{
			base.MissionScreen.RequestSpectatorCycle(-1);
		}
		else if (input.IsHotKeyPressed("CycleSpectatorTargetNext"))
		{
			base.MissionScreen.RequestSpectatorCycle(1);
		}
	}

	private void HandleStoreCameraPosition(InputContext input)
	{
		string[] storeCameraPositionHotKeys = MultiplayerHotkeyCategory.StoreCameraPositionHotKeys;
		for (int i = 0; i < 9 && i < storeCameraPositionHotKeys.Length; i++)
		{
			if (input.IsHotKeyPressed(storeCameraPositionHotKeys[i]))
			{
				_spectateCameraFrames[i] = base.MissionScreen.CombatCamera.Frame;
				_spectateCameraFrameIsSet[i] = true;
				break;
			}
		}
	}

	private void HandleRecallCameraPosition(InputContext input)
	{
		string[] spectateCameraPositionHotKeys = MultiplayerHotkeyCategory.SpectateCameraPositionHotKeys;
		for (int i = 0; i < 9 && i < spectateCameraPositionHotKeys.Length; i++)
		{
			if (input.IsHotKeyPressed(spectateCameraPositionHotKeys[i]))
			{
				if (_spectateCameraFrameIsSet[i])
				{
					base.MissionScreen.UpdateFreeCamera(_spectateCameraFrames[i]);
				}
				break;
			}
		}
	}

	public override void OnMissionScreenFinalize()
	{
		base.MissionScreen?.SetSpectatorCameraOverride(null);
		base.OnMissionScreenFinalize();
	}

	private void HandlePovToggle(InputContext input)
	{
		if (input.IsHotKeyReleased("CycleSpectatorCamera") && MultiplayerOptions.IsSpectatorCameraFreedomAllowed())
		{
			if (!_hasCameraCycleMode)
			{
				_cameraCycleMode = (SpectatorCameraTypes)MultiplayerOptions.OptionType.SpectatorCamera.GetIntValue();
				_hasCameraCycleMode = true;
			}
			switch (_cameraCycleMode)
			{
			case SpectatorCameraTypes.Free:
				_cameraCycleMode = SpectatorCameraTypes.LockToAnyPlayer;
				break;
			case SpectatorCameraTypes.LockToAnyPlayer:
				_cameraCycleMode = SpectatorCameraTypes.OrbitAroundTarget;
				break;
			default:
				_cameraCycleMode = SpectatorCameraTypes.Free;
				break;
			}
			base.MissionScreen.SetSpectatorCameraOverride(_cameraCycleMode);
		}
	}

	private void HandleClickToFollow(InputContext input)
	{
		if (input.IsKeyReleased(InputKey.LeftMouseButton) && !base.MissionScreen.IsRightButtonDragging)
		{
			Vec2 mousePositionRanged = input.GetMousePositionRanged();
			Agent agent = FindAgentNearestToScreenPoint(mousePositionRanged);
			if (agent != null)
			{
				base.MissionScreen.SetAgentToFollow(agent);
				base.MissionScreen.SuppressSpectatorCyclingThisFrame();
			}
		}
	}

	private Agent FindAgentNearestToScreenPoint(Vec2 screenPoint)
	{
		Agent result = null;
		float num = 0.0016f;
		foreach (Agent agent in Mission.Current.Agents)
		{
			if (!agent.IsActive() || !agent.IsCameraAttachable() || agent.MissionPeer == null)
			{
				continue;
			}
			Vec3 position = agent.VisualPosition + new Vec3(0f, 0f, 1.2f);
			Vec2 vec = base.MissionScreen.SceneLayer.WorldPointToScreenPoint(position);
			if (!(vec.x < 0f) && !(vec.x > 1f) && !(vec.y < 0f) && !(vec.y > 1f))
			{
				float num2 = vec.DistanceSquared(screenPoint);
				if (num2 < num)
				{
					num = num2;
					result = agent;
				}
			}
		}
		return result;
	}

	private bool IsSpectatorPeer()
	{
		if (!GameNetwork.IsMultiplayer || !GameNetwork.IsMyPeerReady)
		{
			return false;
		}
		return SpectatorHelper.IsLocalPeerSpectator();
	}
}
