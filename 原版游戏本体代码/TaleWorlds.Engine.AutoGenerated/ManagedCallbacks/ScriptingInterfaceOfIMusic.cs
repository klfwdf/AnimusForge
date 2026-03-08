using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;

namespace ManagedCallbacks
{
	// Token: 0x0200001D RID: 29
	internal class ScriptingInterfaceOfIMusic : IMusic
	{
		// Token: 0x060003A8 RID: 936 RVA: 0x00014B18 File Offset: 0x00012D18
		public int GetFreeMusicChannelIndex()
		{
			return ScriptingInterfaceOfIMusic.call_GetFreeMusicChannelIndexDelegate();
		}

		// Token: 0x060003A9 RID: 937 RVA: 0x00014B24 File Offset: 0x00012D24
		public bool IsClipLoaded(int index)
		{
			return ScriptingInterfaceOfIMusic.call_IsClipLoadedDelegate(index);
		}

		// Token: 0x060003AA RID: 938 RVA: 0x00014B31 File Offset: 0x00012D31
		public bool IsMusicPlaying(int index)
		{
			return ScriptingInterfaceOfIMusic.call_IsMusicPlayingDelegate(index);
		}

		// Token: 0x060003AB RID: 939 RVA: 0x00014B40 File Offset: 0x00012D40
		public void LoadClip(int index, string pathToClip)
		{
			byte[] array = null;
			if (pathToClip != null)
			{
				int byteCount = ScriptingInterfaceOfIMusic._utf8.GetByteCount(pathToClip);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIMusic._utf8.GetBytes(pathToClip, 0, pathToClip.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIMusic.call_LoadClipDelegate(index, array);
		}

		// Token: 0x060003AC RID: 940 RVA: 0x00014B9B File Offset: 0x00012D9B
		public void PauseMusic(int index)
		{
			ScriptingInterfaceOfIMusic.call_PauseMusicDelegate(index);
		}

		// Token: 0x060003AD RID: 941 RVA: 0x00014BA8 File Offset: 0x00012DA8
		public void PlayDelayed(int index, int delayMilliseconds)
		{
			ScriptingInterfaceOfIMusic.call_PlayDelayedDelegate(index, delayMilliseconds);
		}

		// Token: 0x060003AE RID: 942 RVA: 0x00014BB6 File Offset: 0x00012DB6
		public void PlayMusic(int index)
		{
			ScriptingInterfaceOfIMusic.call_PlayMusicDelegate(index);
		}

		// Token: 0x060003AF RID: 943 RVA: 0x00014BC3 File Offset: 0x00012DC3
		public void SetVolume(int index, float volume)
		{
			ScriptingInterfaceOfIMusic.call_SetVolumeDelegate(index, volume);
		}

		// Token: 0x060003B0 RID: 944 RVA: 0x00014BD1 File Offset: 0x00012DD1
		public void StopMusic(int index)
		{
			ScriptingInterfaceOfIMusic.call_StopMusicDelegate(index);
		}

		// Token: 0x060003B1 RID: 945 RVA: 0x00014BDE File Offset: 0x00012DDE
		public void UnloadClip(int index)
		{
			ScriptingInterfaceOfIMusic.call_UnloadClipDelegate(index);
		}

		// Token: 0x04000319 RID: 793
		private static readonly Encoding _utf8 = Encoding.UTF8;

		// Token: 0x0400031A RID: 794
		public static ScriptingInterfaceOfIMusic.GetFreeMusicChannelIndexDelegate call_GetFreeMusicChannelIndexDelegate;

		// Token: 0x0400031B RID: 795
		public static ScriptingInterfaceOfIMusic.IsClipLoadedDelegate call_IsClipLoadedDelegate;

		// Token: 0x0400031C RID: 796
		public static ScriptingInterfaceOfIMusic.IsMusicPlayingDelegate call_IsMusicPlayingDelegate;

		// Token: 0x0400031D RID: 797
		public static ScriptingInterfaceOfIMusic.LoadClipDelegate call_LoadClipDelegate;

		// Token: 0x0400031E RID: 798
		public static ScriptingInterfaceOfIMusic.PauseMusicDelegate call_PauseMusicDelegate;

		// Token: 0x0400031F RID: 799
		public static ScriptingInterfaceOfIMusic.PlayDelayedDelegate call_PlayDelayedDelegate;

		// Token: 0x04000320 RID: 800
		public static ScriptingInterfaceOfIMusic.PlayMusicDelegate call_PlayMusicDelegate;

		// Token: 0x04000321 RID: 801
		public static ScriptingInterfaceOfIMusic.SetVolumeDelegate call_SetVolumeDelegate;

		// Token: 0x04000322 RID: 802
		public static ScriptingInterfaceOfIMusic.StopMusicDelegate call_StopMusicDelegate;

		// Token: 0x04000323 RID: 803
		public static ScriptingInterfaceOfIMusic.UnloadClipDelegate call_UnloadClipDelegate;

		// Token: 0x0200038B RID: 907
		// (Invoke) Token: 0x0600143F RID: 5183
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetFreeMusicChannelIndexDelegate();

		// Token: 0x0200038C RID: 908
		// (Invoke) Token: 0x06001443 RID: 5187
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool IsClipLoadedDelegate(int index);

		// Token: 0x0200038D RID: 909
		// (Invoke) Token: 0x06001447 RID: 5191
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool IsMusicPlayingDelegate(int index);

		// Token: 0x0200038E RID: 910
		// (Invoke) Token: 0x0600144B RID: 5195
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void LoadClipDelegate(int index, byte[] pathToClip);

		// Token: 0x0200038F RID: 911
		// (Invoke) Token: 0x0600144F RID: 5199
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void PauseMusicDelegate(int index);

		// Token: 0x02000390 RID: 912
		// (Invoke) Token: 0x06001453 RID: 5203
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void PlayDelayedDelegate(int index, int delayMilliseconds);

		// Token: 0x02000391 RID: 913
		// (Invoke) Token: 0x06001457 RID: 5207
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void PlayMusicDelegate(int index);

		// Token: 0x02000392 RID: 914
		// (Invoke) Token: 0x0600145B RID: 5211
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetVolumeDelegate(int index, float volume);

		// Token: 0x02000393 RID: 915
		// (Invoke) Token: 0x0600145F RID: 5215
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void StopMusicDelegate(int index);

		// Token: 0x02000394 RID: 916
		// (Invoke) Token: 0x06001463 RID: 5219
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void UnloadClipDelegate(int index);
	}
}
