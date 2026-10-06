using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfIMBMessageManager : IMBMessageManager
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void DisplayMessageDelegate(byte[] message);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void DisplayMessageWithColorDelegate(byte[] message, uint color);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetMessageManagerDelegate(int messageManager);

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static DisplayMessageDelegate call_DisplayMessageDelegate;

	public static DisplayMessageWithColorDelegate call_DisplayMessageWithColorDelegate;

	public static SetMessageManagerDelegate call_SetMessageManagerDelegate;

	public void DisplayMessage(string message)
	{
		byte[] array = null;
		if (message != null)
		{
			int byteCount = _utf8.GetByteCount(message);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(message, 0, message.Length, array, 0);
			array[byteCount] = 0;
		}
		call_DisplayMessageDelegate(array);
	}

	public void DisplayMessageWithColor(string message, uint color)
	{
		byte[] array = null;
		if (message != null)
		{
			int byteCount = _utf8.GetByteCount(message);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(message, 0, message.Length, array, 0);
			array[byteCount] = 0;
		}
		call_DisplayMessageWithColorDelegate(array, color);
	}

	public void SetMessageManager(MessageManagerBase messageManager)
	{
		call_SetMessageManagerDelegate(messageManager?.GetManagedId() ?? 0);
	}
}
