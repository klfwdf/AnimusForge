using System;

namespace Steamworks
{
	// Token: 0x02000018 RID: 24
	public static class SteamMusic
	{
		// Token: 0x060002E7 RID: 743 RVA: 0x00008571 File Offset: 0x00006771
		public static bool BIsEnabled()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamMusic_BIsEnabled(CSteamAPIContext.GetSteamMusic());
		}

		// Token: 0x060002E8 RID: 744 RVA: 0x00008582 File Offset: 0x00006782
		public static bool BIsPlaying()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamMusic_BIsPlaying(CSteamAPIContext.GetSteamMusic());
		}

		// Token: 0x060002E9 RID: 745 RVA: 0x00008593 File Offset: 0x00006793
		public static AudioPlayback_Status GetPlaybackStatus()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamMusic_GetPlaybackStatus(CSteamAPIContext.GetSteamMusic());
		}

		// Token: 0x060002EA RID: 746 RVA: 0x000085A4 File Offset: 0x000067A4
		public static void Play()
		{
			InteropHelp.TestIfAvailableClient();
			NativeMethods.ISteamMusic_Play(CSteamAPIContext.GetSteamMusic());
		}

		// Token: 0x060002EB RID: 747 RVA: 0x000085B5 File Offset: 0x000067B5
		public static void Pause()
		{
			InteropHelp.TestIfAvailableClient();
			NativeMethods.ISteamMusic_Pause(CSteamAPIContext.GetSteamMusic());
		}

		// Token: 0x060002EC RID: 748 RVA: 0x000085C6 File Offset: 0x000067C6
		public static void PlayPrevious()
		{
			InteropHelp.TestIfAvailableClient();
			NativeMethods.ISteamMusic_PlayPrevious(CSteamAPIContext.GetSteamMusic());
		}

		// Token: 0x060002ED RID: 749 RVA: 0x000085D7 File Offset: 0x000067D7
		public static void PlayNext()
		{
			InteropHelp.TestIfAvailableClient();
			NativeMethods.ISteamMusic_PlayNext(CSteamAPIContext.GetSteamMusic());
		}

		// Token: 0x060002EE RID: 750 RVA: 0x000085E8 File Offset: 0x000067E8
		public static void SetVolume(float flVolume)
		{
			InteropHelp.TestIfAvailableClient();
			NativeMethods.ISteamMusic_SetVolume(CSteamAPIContext.GetSteamMusic(), flVolume);
		}

		// Token: 0x060002EF RID: 751 RVA: 0x000085FA File Offset: 0x000067FA
		public static float GetVolume()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamMusic_GetVolume(CSteamAPIContext.GetSteamMusic());
		}
	}
}
