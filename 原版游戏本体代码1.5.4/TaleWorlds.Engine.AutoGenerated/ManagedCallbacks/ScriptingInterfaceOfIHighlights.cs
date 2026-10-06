using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfIHighlights : IHighlights
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddHighlightDelegate(byte[] id, byte[] name);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void CloseGroupDelegate(byte[] id, [MarshalAs(UnmanagedType.U1)] bool destroy);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void InitializeDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void OpenGroupDelegate(byte[] id);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void OpenSummaryDelegate(byte[] groups);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void RemoveHighlightDelegate(byte[] id);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SaveScreenshotDelegate(byte[] highlightId, byte[] groupId);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SaveVideoDelegate(byte[] highlightId, byte[] groupId, int startDelta, int endDelta);

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static AddHighlightDelegate call_AddHighlightDelegate;

	public static CloseGroupDelegate call_CloseGroupDelegate;

	public static InitializeDelegate call_InitializeDelegate;

	public static OpenGroupDelegate call_OpenGroupDelegate;

	public static OpenSummaryDelegate call_OpenSummaryDelegate;

	public static RemoveHighlightDelegate call_RemoveHighlightDelegate;

	public static SaveScreenshotDelegate call_SaveScreenshotDelegate;

	public static SaveVideoDelegate call_SaveVideoDelegate;

	public void AddHighlight(string id, string name)
	{
		byte[] array = null;
		if (id != null)
		{
			int byteCount = _utf8.GetByteCount(id);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(id, 0, id.Length, array, 0);
			array[byteCount] = 0;
		}
		byte[] array2 = null;
		if (name != null)
		{
			int byteCount2 = _utf8.GetByteCount(name);
			array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
			_utf8.GetBytes(name, 0, name.Length, array2, 0);
			array2[byteCount2] = 0;
		}
		call_AddHighlightDelegate(array, array2);
	}

	public void CloseGroup(string id, bool destroy)
	{
		byte[] array = null;
		if (id != null)
		{
			int byteCount = _utf8.GetByteCount(id);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(id, 0, id.Length, array, 0);
			array[byteCount] = 0;
		}
		call_CloseGroupDelegate(array, destroy);
	}

	public void Initialize()
	{
		call_InitializeDelegate();
	}

	public void OpenGroup(string id)
	{
		byte[] array = null;
		if (id != null)
		{
			int byteCount = _utf8.GetByteCount(id);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(id, 0, id.Length, array, 0);
			array[byteCount] = 0;
		}
		call_OpenGroupDelegate(array);
	}

	public void OpenSummary(string groups)
	{
		byte[] array = null;
		if (groups != null)
		{
			int byteCount = _utf8.GetByteCount(groups);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(groups, 0, groups.Length, array, 0);
			array[byteCount] = 0;
		}
		call_OpenSummaryDelegate(array);
	}

	public void RemoveHighlight(string id)
	{
		byte[] array = null;
		if (id != null)
		{
			int byteCount = _utf8.GetByteCount(id);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(id, 0, id.Length, array, 0);
			array[byteCount] = 0;
		}
		call_RemoveHighlightDelegate(array);
	}

	public void SaveScreenshot(string highlightId, string groupId)
	{
		byte[] array = null;
		if (highlightId != null)
		{
			int byteCount = _utf8.GetByteCount(highlightId);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(highlightId, 0, highlightId.Length, array, 0);
			array[byteCount] = 0;
		}
		byte[] array2 = null;
		if (groupId != null)
		{
			int byteCount2 = _utf8.GetByteCount(groupId);
			array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
			_utf8.GetBytes(groupId, 0, groupId.Length, array2, 0);
			array2[byteCount2] = 0;
		}
		call_SaveScreenshotDelegate(array, array2);
	}

	public void SaveVideo(string highlightId, string groupId, int startDelta, int endDelta)
	{
		byte[] array = null;
		if (highlightId != null)
		{
			int byteCount = _utf8.GetByteCount(highlightId);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(highlightId, 0, highlightId.Length, array, 0);
			array[byteCount] = 0;
		}
		byte[] array2 = null;
		if (groupId != null)
		{
			int byteCount2 = _utf8.GetByteCount(groupId);
			array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
			_utf8.GetBytes(groupId, 0, groupId.Length, array2, 0);
			array2[byteCount2] = 0;
		}
		call_SaveVideoDelegate(array, array2, startDelta, endDelta);
	}
}
