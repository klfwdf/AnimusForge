using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x02000033 RID: 51
	[ApplicationInterfaceBase]
	internal interface IVideoPlayerView
	{
		// Token: 0x0600053A RID: 1338
		[EngineMethod("create_video_player_view", false, null, false)]
		VideoPlayerView CreateVideoPlayerView();

		// Token: 0x0600053B RID: 1339
		[EngineMethod("play_video", false, null, false)]
		void PlayVideo(UIntPtr pointer, string videoFileName, string soundFileName, float framerate, bool looping);

		// Token: 0x0600053C RID: 1340
		[EngineMethod("stop_video", false, null, false)]
		void StopVideo(UIntPtr pointer);

		// Token: 0x0600053D RID: 1341
		[EngineMethod("is_video_finished", false, null, false)]
		bool IsVideoFinished(UIntPtr pointer);

		// Token: 0x0600053E RID: 1342
		[EngineMethod("finalize", false, null, false)]
		void Finalize(UIntPtr pointer);
	}
}
