using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.View.MissionViews;

public class ReplayCaptureLogic : MissionView
{
	private ReplayMissionView _replayLogic;

	private bool _renderActive;

	public const float CaptureFrameRate = 60f;

	private float _replayTimeDiff;

	private bool _frameSkip;

	private Path _path;

	private PlatformDirectoryPath _directoryPath;

	private bool _saveScreenshots;

	private readonly KeyValuePair<float, MatrixFrame> _invalid = new KeyValuePair<float, MatrixFrame>(-1f, default(MatrixFrame));

	private SortedDictionary<float, SortedDictionary<int, MatrixFrame>> _cameraKeys;

	private bool _isRendered;

	private int _lastUsedIndex;

	private int _ssNum;

	private bool RenderActive
	{
		get
		{
			return _renderActive;
		}
		set
		{
			_renderActive = value;
			CheckFixedDeltaTimeMode();
		}
	}

	private Camera MissionCamera
	{
		get
		{
			if (base.MissionScreen == null || !(base.MissionScreen.CombatCamera != null))
			{
				return null;
			}
			return base.MissionScreen.CombatCamera;
		}
	}

	private float ReplayTime => base.Mission.CurrentTime - _replayTimeDiff;

	private bool SaveScreenshots
	{
		get
		{
			return _saveScreenshots;
		}
		set
		{
			_saveScreenshots = value;
			CheckFixedDeltaTimeMode();
		}
	}

	private KeyValuePair<float, MatrixFrame> PreviousKey => GetPreviousKey();

	private KeyValuePair<float, MatrixFrame> NextKey => GetNextKey();

	private void CheckFixedDeltaTimeMode()
	{
		if (RenderActive && SaveScreenshots)
		{
			base.Mission.FixedDeltaTime = 1f / 60f;
			base.Mission.FixedDeltaTimeMode = true;
		}
		else
		{
			base.Mission.FixedDeltaTime = 0f;
			base.Mission.FixedDeltaTimeMode = false;
		}
	}

	private KeyValuePair<float, MatrixFrame> GetPreviousKey()
	{
		KeyValuePair<float, MatrixFrame> result = _invalid;
		if (!_cameraKeys.Any())
		{
			return result;
		}
		foreach (KeyValuePair<float, SortedDictionary<int, MatrixFrame>> cameraKey in _cameraKeys)
		{
			if (cameraKey.Key <= ReplayTime)
			{
				result = new KeyValuePair<float, MatrixFrame>(cameraKey.Key, cameraKey.Value[cameraKey.Value.Count - 1]);
			}
		}
		return result;
	}

	private KeyValuePair<float, MatrixFrame> GetNextKey()
	{
		KeyValuePair<float, MatrixFrame> result = _invalid;
		if (!_cameraKeys.Any())
		{
			return result;
		}
		foreach (KeyValuePair<float, SortedDictionary<int, MatrixFrame>> cameraKey in _cameraKeys)
		{
			if (cameraKey.Key > ReplayTime)
			{
				result = new KeyValuePair<float, MatrixFrame>(cameraKey.Key, cameraKey.Value[0]);
				break;
			}
		}
		return result;
	}

	public ReplayCaptureLogic()
	{
		_cameraKeys = new SortedDictionary<float, SortedDictionary<int, MatrixFrame>>();
	}

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		_replayLogic = base.Mission.GetMissionBehavior<ReplayMissionView>();
		_replayLogic.OverrideInput(isOverridden: true);
		if (!MBCommon.IsPaused)
		{
			_replayLogic.Pause();
		}
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		if (_frameSkip && !MBCommon.IsPaused)
		{
			if (!_isRendered)
			{
				_isRendered = true;
				return;
			}
			_replayLogic.Pause();
			_frameSkip = false;
		}
		if (!RenderActive)
		{
			return;
		}
		SaveScreenshot();
		if (!base.Mission.Recorder.IsEndOfRecord())
		{
			KeyValuePair<float, MatrixFrame> previousKey = PreviousKey;
			KeyValuePair<float, MatrixFrame> nextKey = NextKey;
			_replayLogic.Resume();
			if (nextKey.Key >= 0f)
			{
				for (int i = 0; i < _cameraKeys.Count; i++)
				{
					if (previousKey.Key == _cameraKeys.ElementAt(i).Key)
					{
						float num = nextKey.Key - previousKey.Key;
						float num2 = (ReplayTime - previousKey.Key) / num;
						int count = _cameraKeys[previousKey.Key].Count;
						MatrixFrame frame;
						if (_lastUsedIndex != i && count > 1)
						{
							frame = _cameraKeys[previousKey.Key][count - 1];
						}
						else
						{
							frame = new MatrixFrame
							{
								origin = _path.GetHermiteFrameForDt(num2, i).origin
							};
							Vec3 s = previousKey.Value.rotation.s * (1f - num2) + nextKey.Value.rotation.s * num2;
							Vec3 u = previousKey.Value.rotation.u * (1f - num2) + nextKey.Value.rotation.u * num2;
							Vec3 f = previousKey.Value.rotation.f * (1f - num2) + nextKey.Value.rotation.f * num2;
							frame.rotation.s = s;
							frame.rotation.u = u;
							frame.rotation.f = f;
						}
						frame.rotation.s.Normalize();
						frame.rotation.u.Normalize();
						frame.rotation.f.Normalize();
						frame.rotation.Orthonormalize();
						base.MissionScreen.CustomCamera.Frame = frame;
						_lastUsedIndex = i;
						break;
					}
				}
			}
			else if (previousKey.Key >= 0f)
			{
				int count2 = _cameraKeys[previousKey.Key].Count;
				if (count2 > 1)
				{
					MatrixFrame frame2 = _cameraKeys[previousKey.Key][count2 - 1];
					frame2.rotation.s.Normalize();
					frame2.rotation.u.Normalize();
					frame2.rotation.f.Normalize();
					frame2.rotation.Orthonormalize();
					base.MissionScreen.CustomCamera.Frame = frame2;
				}
			}
		}
		else
		{
			MBDebug.Print("All images are saved.", 0, Debug.DebugColor.DarkCyan, 64uL);
			RenderActive = false;
			_replayLogic.ResetReplay();
			_replayTimeDiff = base.Mission.CurrentTime;
			base.MissionScreen.CustomCamera = null;
			_replayLogic.Pause();
			SaveScreenshots = false;
			_ssNum = 0;
		}
	}

	private void InsertCamKey()
	{
		float replayTime = ReplayTime;
		MatrixFrame frame = MissionCamera.Frame;
		int num = 0;
		if (_cameraKeys.ContainsKey(replayTime))
		{
			num = _cameraKeys[replayTime].Count;
			_cameraKeys[replayTime].Add(num, frame);
		}
		else
		{
			_cameraKeys.Add(replayTime, new SortedDictionary<int, MatrixFrame> { { num, frame } });
		}
		MBDebug.Print("Keyframe to \"" + replayTime + "\" has been inserted with the index: " + num + ".\n", 0, Debug.DebugColor.Green, 64uL);
	}

	private void MoveToNextFrame()
	{
		_replayLogic.FastForward(1f / 60f);
		_replayLogic.Resume();
		_frameSkip = true;
	}

	private void GoToKey(float keyTime)
	{
		if (!(keyTime < 0f) && _cameraKeys.ContainsKey(keyTime) && keyTime != ReplayTime)
		{
			MatrixFrame frame;
			if (keyTime < ReplayTime)
			{
				frame = _cameraKeys[keyTime][_cameraKeys[keyTime].Count - 1];
				_replayLogic.Rewind(ReplayTime - keyTime);
				_replayTimeDiff = base.Mission.CurrentTime;
			}
			else
			{
				frame = _cameraKeys[keyTime][0];
				_replayLogic.FastForward(keyTime - ReplayTime);
			}
			MissionCamera.Frame = frame;
		}
	}

	private void SetPath()
	{
		if (base.Mission.Scene.GetPathWithName("CameraPath") != null)
		{
			base.Mission.Scene.DeletePathWithName("CameraPath");
		}
		base.Mission.Scene.AddPath("CameraPath");
		foreach (KeyValuePair<float, SortedDictionary<int, MatrixFrame>> cameraKey in _cameraKeys)
		{
			base.Mission.Scene.AddPathPoint("CameraPath", cameraKey.Value[0]);
		}
		_path = base.Mission.Scene.GetPathWithName("CameraPath");
	}

	private void Render(bool saveScreenshots = false)
	{
		if (!_cameraKeys.ContainsKey(0f))
		{
			_cameraKeys.Add(0f, new SortedDictionary<int, MatrixFrame> { { 0, MissionCamera.Frame } });
		}
		else
		{
			_cameraKeys[0f] = new SortedDictionary<int, MatrixFrame> { { 0, MissionCamera.Frame } };
		}
		_replayLogic.ResetReplay();
		_replayLogic.Pause();
		_replayTimeDiff = base.Mission.CurrentTime;
		SetPath();
		SaveScreenshots = saveScreenshots;
		RenderActive = true;
		_lastUsedIndex = 0;
		base.MissionScreen.CustomCamera = base.MissionScreen.CombatCamera;
	}

	private void SaveScreenshot()
	{
		if (SaveScreenshots)
		{
			if (string.IsNullOrEmpty(_directoryPath.Path))
			{
				PlatformDirectoryPath platformDirectoryPath = new PlatformDirectoryPath(PlatformFileType.User, "Captures");
				string text = "Cap_" + $"{DateTime.Now:yyyy-MM-dd_hh-mm-ss-tt}";
				_directoryPath = platformDirectoryPath + text;
			}
			Utilities.TakeScreenshot(new PlatformFilePath(_directoryPath, "time_" + $"{_ssNum:000000}" + ".bmp"));
			_ssNum++;
		}
	}
}
