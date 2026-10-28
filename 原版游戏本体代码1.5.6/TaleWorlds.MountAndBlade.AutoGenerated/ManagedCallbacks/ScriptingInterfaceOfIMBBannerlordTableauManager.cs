using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.MountAndBlade;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfIMBBannerlordTableauManager : IMBBannerlordTableauManager
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetNumberOfPendingTableauRequestsDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void InitializeCharacterTableauRenderSystemDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void RequestCharacterTableauRenderDelegate(int characterCodeId, byte[] path, UIntPtr poseEntity, UIntPtr cameraObject, int tableauType);

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static GetNumberOfPendingTableauRequestsDelegate call_GetNumberOfPendingTableauRequestsDelegate;

	public static InitializeCharacterTableauRenderSystemDelegate call_InitializeCharacterTableauRenderSystemDelegate;

	public static RequestCharacterTableauRenderDelegate call_RequestCharacterTableauRenderDelegate;

	public int GetNumberOfPendingTableauRequests()
	{
		return call_GetNumberOfPendingTableauRequestsDelegate();
	}

	public void InitializeCharacterTableauRenderSystem()
	{
		call_InitializeCharacterTableauRenderSystemDelegate();
	}

	public void RequestCharacterTableauRender(int characterCodeId, string path, UIntPtr poseEntity, UIntPtr cameraObject, int tableauType)
	{
		byte[] array = null;
		if (path != null)
		{
			int byteCount = _utf8.GetByteCount(path);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(path, 0, path.Length, array, 0);
			array[byteCount] = 0;
		}
		call_RequestCharacterTableauRenderDelegate(characterCodeId, array, poseEntity, cameraObject, tableauType);
	}
}
