using System;
using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade;

public class VideoPlaybackState : GameState
{
	private Action _onVideoFinised;

	public string VideoPath { get; private set; }

	public string AudioPath { get; private set; }

	public float FrameRate { get; private set; }

	public string SubtitleFileBasePath { get; private set; }

	public bool CanUserSkip { get; private set; }

	public void SetStartingParameters(string videoPath, string audioPath, string subtitleFileBasePath, float frameRate = 30f, bool canUserSkip = true)
	{
		VideoPath = videoPath;
		AudioPath = audioPath;
		FrameRate = frameRate;
		SubtitleFileBasePath = subtitleFileBasePath;
		CanUserSkip = canUserSkip;
	}

	public void SetOnVideoFinisedDelegate(Action onVideoFinised)
	{
		_onVideoFinised = onVideoFinised;
	}

	public void OnVideoStarted()
	{
		MBMusicManager.Current.DeactivateCurrentMode();
		MBMusicManager.Current.PauseMusicManagerSystem();
	}

	public void OnVideoFinished()
	{
		MBMusicManager.Current.UnpauseMusicManagerSystem();
		_onVideoFinised?.Invoke();
	}
}
