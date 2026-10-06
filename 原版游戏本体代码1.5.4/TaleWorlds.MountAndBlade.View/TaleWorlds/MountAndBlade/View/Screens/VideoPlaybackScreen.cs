using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.MountAndBlade.View.Screens;

public class VideoPlaybackScreen : ScreenBase, IGameStateListener
{
	protected VideoPlaybackState _videoPlaybackState;

	protected VideoPlayerView _videoPlayerView;

	protected float _totalElapsedTimeSinceVideoStart;

	public VideoPlaybackScreen(VideoPlaybackState videoPlaybackState)
	{
		_videoPlaybackState = videoPlaybackState;
		_videoPlayerView = VideoPlayerView.CreateVideoPlayerView();
		_videoPlayerView.SetRenderOrder(-10000);
	}

	protected sealed override void OnFrameTick(float dt)
	{
		_totalElapsedTimeSinceVideoStart += dt;
		base.OnFrameTick(dt);
		if (_videoPlayerView != null && _videoPlaybackState != null)
		{
			if (_videoPlaybackState.CanUserSkip && (Input.IsKeyReleased(InputKey.Escape) || Input.IsKeyReleased(InputKey.ControllerROption)))
			{
				_videoPlayerView.StopVideo();
			}
			if (_videoPlayerView.IsVideoFinished())
			{
				_videoPlaybackState.OnVideoFinished();
				_videoPlayerView.SetEnable(value: false);
				_videoPlayerView.FinalizePlayer();
				_videoPlayerView = null;
			}
			if (ScreenManager.TopScreen == this)
			{
				OnVideoPlaybackTick(dt);
			}
		}
	}

	protected virtual void OnVideoPlaybackTick(float dt)
	{
	}

	void IGameStateListener.OnInitialize()
	{
		_videoPlayerView.PlayVideo(_videoPlaybackState.VideoPath, _videoPlaybackState.AudioPath, _videoPlaybackState.FrameRate, looping: false);
		_videoPlaybackState.OnVideoStarted();
		LoadingWindow.DisableGlobalLoadingWindow();
		Utilities.DisableGlobalLoadingWindow();
	}

	void IGameStateListener.OnFinalize()
	{
		_videoPlayerView?.SetEnable(value: false);
		_videoPlayerView?.FinalizePlayer();
	}

	void IGameStateListener.OnActivate()
	{
		base.OnActivate();
	}

	void IGameStateListener.OnDeactivate()
	{
		base.OnDeactivate();
	}
}
