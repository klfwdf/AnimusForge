using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;

namespace ManagedCallbacks
{
	// Token: 0x02000031 RID: 49
	internal class ScriptingInterfaceOfIVideoPlayerView : IVideoPlayerView
	{
		// Token: 0x060006CA RID: 1738 RVA: 0x0001B9F4 File Offset: 0x00019BF4
		public VideoPlayerView CreateVideoPlayerView()
		{
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfIVideoPlayerView.call_CreateVideoPlayerViewDelegate();
			VideoPlayerView result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new VideoPlayerView(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x060006CB RID: 1739 RVA: 0x0001BA3D File Offset: 0x00019C3D
		public void Finalize(UIntPtr pointer)
		{
			ScriptingInterfaceOfIVideoPlayerView.call_FinalizeDelegate(pointer);
		}

		// Token: 0x060006CC RID: 1740 RVA: 0x0001BA4A File Offset: 0x00019C4A
		public bool IsVideoFinished(UIntPtr pointer)
		{
			return ScriptingInterfaceOfIVideoPlayerView.call_IsVideoFinishedDelegate(pointer);
		}

		// Token: 0x060006CD RID: 1741 RVA: 0x0001BA58 File Offset: 0x00019C58
		public void PlayVideo(UIntPtr pointer, string videoFileName, string soundFileName, float framerate, bool looping)
		{
			byte[] array = null;
			if (videoFileName != null)
			{
				int byteCount = ScriptingInterfaceOfIVideoPlayerView._utf8.GetByteCount(videoFileName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIVideoPlayerView._utf8.GetBytes(videoFileName, 0, videoFileName.Length, array, 0);
				array[byteCount] = 0;
			}
			byte[] array2 = null;
			if (soundFileName != null)
			{
				int byteCount2 = ScriptingInterfaceOfIVideoPlayerView._utf8.GetByteCount(soundFileName);
				array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
				ScriptingInterfaceOfIVideoPlayerView._utf8.GetBytes(soundFileName, 0, soundFileName.Length, array2, 0);
				array2[byteCount2] = 0;
			}
			ScriptingInterfaceOfIVideoPlayerView.call_PlayVideoDelegate(pointer, array, array2, framerate, looping);
		}

		// Token: 0x060006CE RID: 1742 RVA: 0x0001BAFA File Offset: 0x00019CFA
		public void StopVideo(UIntPtr pointer)
		{
			ScriptingInterfaceOfIVideoPlayerView.call_StopVideoDelegate(pointer);
		}

		// Token: 0x04000616 RID: 1558
		private static readonly Encoding _utf8 = Encoding.UTF8;

		// Token: 0x04000617 RID: 1559
		public static ScriptingInterfaceOfIVideoPlayerView.CreateVideoPlayerViewDelegate call_CreateVideoPlayerViewDelegate;

		// Token: 0x04000618 RID: 1560
		public static ScriptingInterfaceOfIVideoPlayerView.FinalizeDelegate call_FinalizeDelegate;

		// Token: 0x04000619 RID: 1561
		public static ScriptingInterfaceOfIVideoPlayerView.IsVideoFinishedDelegate call_IsVideoFinishedDelegate;

		// Token: 0x0400061A RID: 1562
		public static ScriptingInterfaceOfIVideoPlayerView.PlayVideoDelegate call_PlayVideoDelegate;

		// Token: 0x0400061B RID: 1563
		public static ScriptingInterfaceOfIVideoPlayerView.StopVideoDelegate call_StopVideoDelegate;

		// Token: 0x02000674 RID: 1652
		// (Invoke) Token: 0x06001FE3 RID: 8163
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer CreateVideoPlayerViewDelegate();

		// Token: 0x02000675 RID: 1653
		// (Invoke) Token: 0x06001FE7 RID: 8167
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void FinalizeDelegate(UIntPtr pointer);

		// Token: 0x02000676 RID: 1654
		// (Invoke) Token: 0x06001FEB RID: 8171
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool IsVideoFinishedDelegate(UIntPtr pointer);

		// Token: 0x02000677 RID: 1655
		// (Invoke) Token: 0x06001FEF RID: 8175
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void PlayVideoDelegate(UIntPtr pointer, byte[] videoFileName, byte[] soundFileName, float framerate, [MarshalAs(UnmanagedType.U1)] bool looping);

		// Token: 0x02000678 RID: 1656
		// (Invoke) Token: 0x06001FF3 RID: 8179
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void StopVideoDelegate(UIntPtr pointer);
	}
}
