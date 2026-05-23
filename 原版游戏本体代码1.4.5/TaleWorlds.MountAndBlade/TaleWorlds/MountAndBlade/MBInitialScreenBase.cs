using System;
using System.Collections.Generic;
using System.IO;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ModuleManager;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.MountAndBlade;

public class MBInitialScreenBase : ScreenBase, IGameStateListener
{
	private Camera _camera;

	protected VideoPlayerView _videoPlayerView;

	private Vec2 _screenResUsedForVideo = Vec2.Zero;

	private bool _isPlayingVideo;

	protected InitialState _state { get; private set; }

	public MBInitialScreenBase(InitialState state)
	{
		_state = state;
	}

	void IGameStateListener.OnActivate()
	{
	}

	void IGameStateListener.OnDeactivate()
	{
	}

	void IGameStateListener.OnInitialize()
	{
		_state.OnInitialMenuOptionInvoked += OnExecutedInitialStateOption;
	}

	void IGameStateListener.OnFinalize()
	{
		_state.OnInitialMenuOptionInvoked -= OnExecutedInitialStateOption;
	}

	private void OnExecutedInitialStateOption(InitialStateOption target)
	{
	}

	protected override void OnInitialize()
	{
		base.OnInitialize();
		_camera = Camera.CreateCamera();
		Common.MemoryCleanupGC();
		if (Game.Current != null)
		{
			Game.Current.Destroy();
		}
		MBMusicManager.Initialize();
	}

	protected override void OnFinalize()
	{
		_camera = null;
		_videoPlayerView.SetEnable(value: false);
		_videoPlayerView.FinalizePlayer();
		base.OnFinalize();
	}

	protected sealed override void OnFrameTick(float dt)
	{
		base.OnFrameTick(dt);
		if (_videoPlayerView == null)
		{
			Console.WriteLine("InitialScreen::OnFrameTick scene view null");
		}
		LoadingWindow.DisableGlobalLoadingWindow();
		if (Input.IsKeyDown(InputKey.LeftControl) && Input.IsKeyReleased(InputKey.E))
		{
			OnEditModeEnterPress();
		}
		if (ScreenManager.TopScreen == this)
		{
			OnInitialScreenTick(dt);
		}
		Vec2 screenResolution = MBWindowManager.GetScreenResolution();
		if (_screenResUsedForVideo != screenResolution)
		{
			RefreshVideoAspect(screenResolution);
			_screenResUsedForVideo = screenResolution;
		}
	}

	protected virtual void OnInitialScreenTick(float dt)
	{
		if (_videoPlayerView == null || !_isPlayingVideo)
		{
			RefreshScene();
		}
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		if (Utilities.renderingActive)
		{
			RefreshScene();
			Utilities.DisableGlobalLoadingWindow();
		}
		if (NativeConfig.DoLocalizationCheckAtStartup)
		{
			LocalizedTextManager.CheckValidity(new List<string>());
		}
		Module.CurrentModule.SetCanLoadModules(canLoadModules: true);
	}

	private void RefreshScene()
	{
		_isPlayingVideo = false;
		if (_videoPlayerView != null)
		{
			_videoPlayerView.StopVideo();
			_videoPlayerView.FinalizePlayer();
			_videoPlayerView = null;
		}
		_videoPlayerView = VideoPlayerView.CreateVideoPlayerView();
		List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>();
		foreach (ModuleInfo activeModule in ModuleHelper.GetActiveModules())
		{
			string path = System.IO.Path.Combine(activeModule.FolderPath, "Videos", "initial_menu").ToString();
			if (!Directory.Exists(path))
			{
				continue;
			}
			string searchPattern = "*_pc.ivf";
			string[] directories = Directory.GetDirectories(path);
			foreach (string path2 in directories)
			{
				string[] files = Directory.GetFiles(path2, "*.ogg");
				bool flag = files.Length != 0;
				string[] files2 = Directory.GetFiles(path2, searchPattern);
				bool flag2 = files2.Length != 0;
				if (flag && flag2)
				{
					list.Add(new KeyValuePair<string, string>(files2[0], files[0]));
				}
			}
		}
		float framerate = 24f;
		string text = string.Empty;
		string text2 = string.Empty;
		if (list.Count > 0)
		{
			int count = list.Count;
			int index = new Random(DateTime.Now.Second).Next(count);
			text = list[index].Key;
			text2 = list[index].Value;
			_videoPlayerView.PlayVideo(text, text2, framerate, looping: true);
			_isPlayingVideo = true;
		}
		Vec2 screenResolution = MBWindowManager.GetScreenResolution();
		RefreshVideoAspect(screenResolution);
		Debug.Print($"Initial Screen: Video is playing after refresh: {_isPlayingVideo} {text}::{text2}");
	}

	private void RefreshVideoAspect(Vec2 screenRes)
	{
		float num = screenRes.x / screenRes.y;
		if (num > 1.7777778f)
		{
			float num2 = 1f - 1.7777778f / num;
			_videoPlayerView.SetOffset(new Vec2(num2 * 0.5f, 0f));
			_videoPlayerView.SetScale(new Vec2(1f - num2, 1f));
		}
		else if (num < 1.7777778f)
		{
			float num3 = screenRes.y / screenRes.x;
			float num4 = 1f - 0.5625f / num3;
			_videoPlayerView.SetOffset(new Vec2(0f, num4 * 0.5f));
			_videoPlayerView.SetScale(new Vec2(1f, 1f - num4));
		}
	}

	private void OnSceneEditorWindowOpen()
	{
		GameStateManager.Current.CleanAndPushState(GameStateManager.Current.CreateState<EditorState>());
	}

	protected override void OnDeactivate()
	{
		base.OnDeactivate();
		_videoPlayerView.StopVideo();
		Module.CurrentModule.SetCanLoadModules(canLoadModules: false);
	}

	protected override void OnPause()
	{
		LoadingWindow.DisableGlobalLoadingWindow();
		_videoPlayerView.StopVideo();
		base.OnPause();
	}

	protected override void OnResume()
	{
		base.OnResume();
	}

	public static void DoExitButtonAction()
	{
		MBAPI.IMBScreen.OnExitButtonClick();
	}

	public bool StartedRendering()
	{
		return true;
	}

	public static void OnEditModeEnterPress()
	{
		MBAPI.IMBScreen.OnEditModeEnterPress();
	}

	public static void OnEditModeEnterRelease()
	{
		MBAPI.IMBScreen.OnEditModeEnterRelease();
	}
}
