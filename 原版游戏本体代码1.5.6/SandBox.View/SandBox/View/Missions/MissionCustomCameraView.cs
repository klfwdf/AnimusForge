using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace SandBox.View.Missions;

public class MissionCustomCameraView : MissionView
{
	public string tag = "customcamera";

	private readonly List<Camera> _cameras = new List<Camera>();

	public Vec3 _dofParams;

	private int _currentCameraIndex;

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		foreach (GameEntity item in base.Mission.Scene.FindEntitiesWithTag(tag))
		{
			Camera camera = Camera.CreateCamera();
			item.GetCameraParamsFromCameraScript(camera, ref _dofParams);
			_cameras.Add(camera);
		}
		base.MissionScreen.CustomCamera = _cameras[0];
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		if (base.DebugInput.IsHotKeyReleased("CustomCameraMissionViewHotkeyIncreaseCustomCameraIndex"))
		{
			_currentCameraIndex++;
			if (_currentCameraIndex >= _cameras.Count)
			{
				_currentCameraIndex = 0;
			}
			base.MissionScreen.CustomCamera = _cameras[_currentCameraIndex];
		}
	}
}
