using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfIMusic : IMusic
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetFreeMusicChannelIndexDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool IsClipLoadedDelegate(int index);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool IsMusicPlayingDelegate(int index);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void LoadClipDelegate(int index, byte[] pathToClip);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void PauseMusicDelegate(int index);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void PlayDelayedDelegate(int index, int delayMilliseconds);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void PlayMusicDelegate(int index);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetVolumeDelegate(int index, float volume);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void StopMusicDelegate(int index);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void UnloadClipDelegate(int index);

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static GetFreeMusicChannelIndexDelegate call_GetFreeMusicChannelIndexDelegate;

	public static IsClipLoadedDelegate call_IsClipLoadedDelegate;

	public static IsMusicPlayingDelegate call_IsMusicPlayingDelegate;

	public static LoadClipDelegate call_LoadClipDelegate;

	public static PauseMusicDelegate call_PauseMusicDelegate;

	public static PlayDelayedDelegate call_PlayDelayedDelegate;

	public static PlayMusicDelegate call_PlayMusicDelegate;

	public static SetVolumeDelegate call_SetVolumeDelegate;

	public static StopMusicDelegate call_StopMusicDelegate;

	public static UnloadClipDelegate call_UnloadClipDelegate;

	public int GetFreeMusicChannelIndex()
	{
		return call_GetFreeMusicChannelIndexDelegate();
	}

	public bool IsClipLoaded(int index)
	{
		return call_IsClipLoadedDelegate(index);
	}

	public bool IsMusicPlaying(int index)
	{
		return call_IsMusicPlayingDelegate(index);
	}

	public void LoadClip(int index, string pathToClip)
	{
		byte[] array = null;
		if (pathToClip != null)
		{
			int byteCount = _utf8.GetByteCount(pathToClip);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(pathToClip, 0, pathToClip.Length, array, 0);
			array[byteCount] = 0;
		}
		call_LoadClipDelegate(index, array);
	}

	public void PauseMusic(int index)
	{
		call_PauseMusicDelegate(index);
	}

	public void PlayDelayed(int index, int delayMilliseconds)
	{
		call_PlayDelayedDelegate(index, delayMilliseconds);
	}

	public void PlayMusic(int index)
	{
		call_PlayMusicDelegate(index);
	}

	public void SetVolume(int index, float volume)
	{
		call_SetVolumeDelegate(index, volume);
	}

	public void StopMusic(int index)
	{
		call_StopMusicDelegate(index);
	}

	public void UnloadClip(int index)
	{
		call_UnloadClipDelegate(index);
	}
}
