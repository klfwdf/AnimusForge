using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[ApplicationInterfaceBase]
internal interface IVideoPlayerView
{
	[EngineMethod("create_video_player_view", false, null, false)]
	VideoPlayerView CreateVideoPlayerView();

	[EngineMethod("play_video", false, null, false)]
	void PlayVideo(UIntPtr pointer, string videoFileName, string soundFileName, float framerate, bool looping);

	[EngineMethod("stop_video", false, null, false)]
	void StopVideo(UIntPtr pointer);

	[EngineMethod("is_video_finished", false, null, false)]
	bool IsVideoFinished(UIntPtr pointer);

	[EngineMethod("finalize", false, null, false)]
	void Finalize(UIntPtr pointer);
}
