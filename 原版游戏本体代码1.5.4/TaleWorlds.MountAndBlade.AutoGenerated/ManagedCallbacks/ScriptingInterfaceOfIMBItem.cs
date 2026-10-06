using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfIMBItem : IMBItem
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetHolsterFrameByIndexDelegate(int index, ref MatrixFrame outFrame);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetItemHolsterIndexDelegate(byte[] itemholstername);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool GetItemIsPassiveUsageDelegate(byte[] itemUsageName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetItemUsageIndexDelegate(byte[] itemusagename);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetItemUsageReloadActionCodeDelegate(byte[] itemUsageName, int usageDirection, [MarshalAs(UnmanagedType.U1)] bool isMounted, int leftHandUsageSetIndex, [MarshalAs(UnmanagedType.U1)] bool isLeftStance, [MarshalAs(UnmanagedType.U1)] bool isLowLookDirection);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetItemUsageSetFlagsDelegate(byte[] ItemUsageName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetItemUsageStrikeTypeDelegate(byte[] itemUsageName, int usageDirection, [MarshalAs(UnmanagedType.U1)] bool isMounted, int leftHandUsageSetIndex, [MarshalAs(UnmanagedType.U1)] bool isLeftStance, [MarshalAs(UnmanagedType.U1)] bool isLowLookDirection);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetMissileRangeDelegate(float shootSpeed, float zDiff);

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static GetHolsterFrameByIndexDelegate call_GetHolsterFrameByIndexDelegate;

	public static GetItemHolsterIndexDelegate call_GetItemHolsterIndexDelegate;

	public static GetItemIsPassiveUsageDelegate call_GetItemIsPassiveUsageDelegate;

	public static GetItemUsageIndexDelegate call_GetItemUsageIndexDelegate;

	public static GetItemUsageReloadActionCodeDelegate call_GetItemUsageReloadActionCodeDelegate;

	public static GetItemUsageSetFlagsDelegate call_GetItemUsageSetFlagsDelegate;

	public static GetItemUsageStrikeTypeDelegate call_GetItemUsageStrikeTypeDelegate;

	public static GetMissileRangeDelegate call_GetMissileRangeDelegate;

	public void GetHolsterFrameByIndex(int index, ref MatrixFrame outFrame)
	{
		call_GetHolsterFrameByIndexDelegate(index, ref outFrame);
	}

	public int GetItemHolsterIndex(string itemholstername)
	{
		byte[] array = null;
		if (itemholstername != null)
		{
			int byteCount = _utf8.GetByteCount(itemholstername);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(itemholstername, 0, itemholstername.Length, array, 0);
			array[byteCount] = 0;
		}
		return call_GetItemHolsterIndexDelegate(array);
	}

	public bool GetItemIsPassiveUsage(string itemUsageName)
	{
		byte[] array = null;
		if (itemUsageName != null)
		{
			int byteCount = _utf8.GetByteCount(itemUsageName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(itemUsageName, 0, itemUsageName.Length, array, 0);
			array[byteCount] = 0;
		}
		return call_GetItemIsPassiveUsageDelegate(array);
	}

	public int GetItemUsageIndex(string itemusagename)
	{
		byte[] array = null;
		if (itemusagename != null)
		{
			int byteCount = _utf8.GetByteCount(itemusagename);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(itemusagename, 0, itemusagename.Length, array, 0);
			array[byteCount] = 0;
		}
		return call_GetItemUsageIndexDelegate(array);
	}

	public int GetItemUsageReloadActionCode(string itemUsageName, int usageDirection, bool isMounted, int leftHandUsageSetIndex, bool isLeftStance, bool isLowLookDirection)
	{
		byte[] array = null;
		if (itemUsageName != null)
		{
			int byteCount = _utf8.GetByteCount(itemUsageName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(itemUsageName, 0, itemUsageName.Length, array, 0);
			array[byteCount] = 0;
		}
		return call_GetItemUsageReloadActionCodeDelegate(array, usageDirection, isMounted, leftHandUsageSetIndex, isLeftStance, isLowLookDirection);
	}

	public int GetItemUsageSetFlags(string ItemUsageName)
	{
		byte[] array = null;
		if (ItemUsageName != null)
		{
			int byteCount = _utf8.GetByteCount(ItemUsageName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(ItemUsageName, 0, ItemUsageName.Length, array, 0);
			array[byteCount] = 0;
		}
		return call_GetItemUsageSetFlagsDelegate(array);
	}

	public int GetItemUsageStrikeType(string itemUsageName, int usageDirection, bool isMounted, int leftHandUsageSetIndex, bool isLeftStance, bool isLowLookDirection)
	{
		byte[] array = null;
		if (itemUsageName != null)
		{
			int byteCount = _utf8.GetByteCount(itemUsageName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(itemUsageName, 0, itemUsageName.Length, array, 0);
			array[byteCount] = 0;
		}
		return call_GetItemUsageStrikeTypeDelegate(array, usageDirection, isMounted, leftHandUsageSetIndex, isLeftStance, isLowLookDirection);
	}

	public float GetMissileRange(float shootSpeed, float zDiff)
	{
		return call_GetMissileRangeDelegate(shootSpeed, zDiff);
	}
}
