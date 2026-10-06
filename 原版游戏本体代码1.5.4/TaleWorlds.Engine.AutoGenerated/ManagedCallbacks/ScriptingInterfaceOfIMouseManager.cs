using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfIMouseManager : IMouseManager
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ActivateMouseCursorDelegate(int id);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void LockCursorAtCurrentPositionDelegate([MarshalAs(UnmanagedType.U1)] bool lockCursor);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void LockCursorAtPositionDelegate(float x, float y);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetMouseCursorDelegate(int id, byte[] mousePath);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ShowCursorDelegate([MarshalAs(UnmanagedType.U1)] bool show);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void UnlockCursorDelegate();

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static ActivateMouseCursorDelegate call_ActivateMouseCursorDelegate;

	public static LockCursorAtCurrentPositionDelegate call_LockCursorAtCurrentPositionDelegate;

	public static LockCursorAtPositionDelegate call_LockCursorAtPositionDelegate;

	public static SetMouseCursorDelegate call_SetMouseCursorDelegate;

	public static ShowCursorDelegate call_ShowCursorDelegate;

	public static UnlockCursorDelegate call_UnlockCursorDelegate;

	public void ActivateMouseCursor(int id)
	{
		call_ActivateMouseCursorDelegate(id);
	}

	public void LockCursorAtCurrentPosition(bool lockCursor)
	{
		call_LockCursorAtCurrentPositionDelegate(lockCursor);
	}

	public void LockCursorAtPosition(float x, float y)
	{
		call_LockCursorAtPositionDelegate(x, y);
	}

	public void SetMouseCursor(int id, string mousePath)
	{
		byte[] array = null;
		if (mousePath != null)
		{
			int byteCount = _utf8.GetByteCount(mousePath);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(mousePath, 0, mousePath.Length, array, 0);
			array[byteCount] = 0;
		}
		call_SetMouseCursorDelegate(id, array);
	}

	public void ShowCursor(bool show)
	{
		call_ShowCursorDelegate(show);
	}

	public void UnlockCursor()
	{
		call_UnlockCursorDelegate();
	}
}
