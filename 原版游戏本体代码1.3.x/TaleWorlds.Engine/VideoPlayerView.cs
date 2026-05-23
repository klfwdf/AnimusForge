using System;
using TaleWorlds.DotNet;

namespace TaleWorlds.Engine
{
	// Token: 0x0200009A RID: 154
	[EngineClass("rglVideo_player_view")]
	public sealed class VideoPlayerView : View
	{
		// Token: 0x06000DB9 RID: 3513 RVA: 0x0000F5C6 File Offset: 0x0000D7C6
		internal VideoPlayerView(UIntPtr meshPointer)
			: base(meshPointer)
		{
		}

		// Token: 0x06000DBA RID: 3514 RVA: 0x0000F5CF File Offset: 0x0000D7CF
		public static VideoPlayerView CreateVideoPlayerView()
		{
			return EngineApplicationInterface.IVideoPlayerView.CreateVideoPlayerView();
		}

		// Token: 0x06000DBB RID: 3515 RVA: 0x0000F5DB File Offset: 0x0000D7DB
		public void PlayVideo(string videoFileName, string soundFileName, float framerate, bool looping)
		{
			EngineApplicationInterface.IVideoPlayerView.PlayVideo(base.Pointer, videoFileName, soundFileName, framerate, looping);
		}

		// Token: 0x06000DBC RID: 3516 RVA: 0x0000F5F2 File Offset: 0x0000D7F2
		public void StopVideo()
		{
			EngineApplicationInterface.IVideoPlayerView.StopVideo(base.Pointer);
		}

		// Token: 0x06000DBD RID: 3517 RVA: 0x0000F604 File Offset: 0x0000D804
		public bool IsVideoFinished()
		{
			return EngineApplicationInterface.IVideoPlayerView.IsVideoFinished(base.Pointer);
		}

		// Token: 0x06000DBE RID: 3518 RVA: 0x0000F616 File Offset: 0x0000D816
		public void FinalizePlayer()
		{
			EngineApplicationInterface.IVideoPlayerView.Finalize(base.Pointer);
		}
	}
}
