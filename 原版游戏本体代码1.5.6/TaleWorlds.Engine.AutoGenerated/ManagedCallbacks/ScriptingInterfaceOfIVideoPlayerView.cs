using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfIVideoPlayerView : IVideoPlayerView
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer CreateVideoPlayerViewDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void FinalizeDelegate(UIntPtr pointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool IsVideoFinishedDelegate(UIntPtr pointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void PlayVideoDelegate(UIntPtr pointer, byte[] videoFileName, byte[] soundFileName, float framerate, [MarshalAs(UnmanagedType.U1)] bool looping);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void StopVideoDelegate(UIntPtr pointer);

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static CreateVideoPlayerViewDelegate call_CreateVideoPlayerViewDelegate;

	public static FinalizeDelegate call_FinalizeDelegate;

	public static IsVideoFinishedDelegate call_IsVideoFinishedDelegate;

	public static PlayVideoDelegate call_PlayVideoDelegate;

	public static StopVideoDelegate call_StopVideoDelegate;

	public VideoPlayerView CreateVideoPlayerView()
	{
		NativeObjectPointer nativeObjectPointer = call_CreateVideoPlayerViewDelegate();
		VideoPlayerView result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new VideoPlayerView(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public void Finalize(UIntPtr pointer)
	{
		call_FinalizeDelegate(pointer);
	}

	public bool IsVideoFinished(UIntPtr pointer)
	{
		return call_IsVideoFinishedDelegate(pointer);
	}

	public void PlayVideo(UIntPtr pointer, string videoFileName, string soundFileName, float framerate, bool looping)
	{
		byte[] array = null;
		if (videoFileName != null)
		{
			int byteCount = _utf8.GetByteCount(videoFileName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(videoFileName, 0, videoFileName.Length, array, 0);
			array[byteCount] = 0;
		}
		byte[] array2 = null;
		if (soundFileName != null)
		{
			int byteCount2 = _utf8.GetByteCount(soundFileName);
			array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
			_utf8.GetBytes(soundFileName, 0, soundFileName.Length, array2, 0);
			array2[byteCount2] = 0;
		}
		call_PlayVideoDelegate(pointer, array, array2, framerate, looping);
	}

	public void StopVideo(UIntPtr pointer)
	{
		call_StopVideoDelegate(pointer);
	}
}
