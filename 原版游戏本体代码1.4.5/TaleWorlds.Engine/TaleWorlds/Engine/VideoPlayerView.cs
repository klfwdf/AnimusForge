using System;
using TaleWorlds.DotNet;

namespace TaleWorlds.Engine;

[EngineClass("rglVideo_player_view")]
public sealed class VideoPlayerView : View
{
	internal VideoPlayerView(UIntPtr meshPointer)
		: base(meshPointer)
	{
	}

	public static VideoPlayerView CreateVideoPlayerView()
	{
		return EngineApplicationInterface.IVideoPlayerView.CreateVideoPlayerView();
	}

	public void PlayVideo(string videoFileName, string soundFileName, float framerate, bool looping)
	{
		EngineApplicationInterface.IVideoPlayerView.PlayVideo(base.Pointer, videoFileName, soundFileName, framerate, looping);
	}

	public void StopVideo()
	{
		EngineApplicationInterface.IVideoPlayerView.StopVideo(base.Pointer);
	}

	public bool IsVideoFinished()
	{
		return EngineApplicationInterface.IVideoPlayerView.IsVideoFinished(base.Pointer);
	}

	public void FinalizePlayer()
	{
		EngineApplicationInterface.IVideoPlayerView.Finalize(base.Pointer);
	}
}
