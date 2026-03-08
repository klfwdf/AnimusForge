using System;

namespace TaleWorlds.Engine
{
	// Token: 0x0200006E RID: 110
	public class Music
	{
		// Token: 0x06000A35 RID: 2613 RVA: 0x0000A56D File Offset: 0x0000876D
		public static int GetFreeMusicChannelIndex()
		{
			return EngineApplicationInterface.IMusic.GetFreeMusicChannelIndex();
		}

		// Token: 0x06000A36 RID: 2614 RVA: 0x0000A579 File Offset: 0x00008779
		public static void LoadClip(int index, string pathToClip)
		{
			EngineApplicationInterface.IMusic.LoadClip(index, pathToClip);
		}

		// Token: 0x06000A37 RID: 2615 RVA: 0x0000A587 File Offset: 0x00008787
		public static void UnloadClip(int index)
		{
			EngineApplicationInterface.IMusic.UnloadClip(index);
		}

		// Token: 0x06000A38 RID: 2616 RVA: 0x0000A594 File Offset: 0x00008794
		public static bool IsClipLoaded(int index)
		{
			return EngineApplicationInterface.IMusic.IsClipLoaded(index);
		}

		// Token: 0x06000A39 RID: 2617 RVA: 0x0000A5A1 File Offset: 0x000087A1
		public static void PlayMusic(int index)
		{
			EngineApplicationInterface.IMusic.PlayMusic(index);
		}

		// Token: 0x06000A3A RID: 2618 RVA: 0x0000A5AE File Offset: 0x000087AE
		public static void PlayDelayed(int index, int deltaMilliseconds)
		{
			EngineApplicationInterface.IMusic.PlayDelayed(index, deltaMilliseconds);
		}

		// Token: 0x06000A3B RID: 2619 RVA: 0x0000A5BC File Offset: 0x000087BC
		public static bool IsMusicPlaying(int index)
		{
			return EngineApplicationInterface.IMusic.IsMusicPlaying(index);
		}

		// Token: 0x06000A3C RID: 2620 RVA: 0x0000A5C9 File Offset: 0x000087C9
		public static void PauseMusic(int index)
		{
			EngineApplicationInterface.IMusic.PauseMusic(index);
		}

		// Token: 0x06000A3D RID: 2621 RVA: 0x0000A5D6 File Offset: 0x000087D6
		public static void StopMusic(int index)
		{
			EngineApplicationInterface.IMusic.StopMusic(index);
		}

		// Token: 0x06000A3E RID: 2622 RVA: 0x0000A5E3 File Offset: 0x000087E3
		public static void SetVolume(int index, float volume)
		{
			EngineApplicationInterface.IMusic.SetVolume(index, volume);
		}
	}
}
