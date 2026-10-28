using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.MountAndBlade;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfIMBWorld : IMBWorld
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void CheckResourceModificationsDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void FixSkeletonsDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetGameTypeDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetGlobalTimeDelegate(MBCommon.TimeType timeType);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetLastMessagesDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void PauseGameDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetBodyUsedDelegate(byte[] bodyName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetGameTypeDelegate(int gameType);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetMaterialUsedDelegate(byte[] materialName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetMeshUsedDelegate(byte[] meshName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void UnpauseGameDelegate();

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static CheckResourceModificationsDelegate call_CheckResourceModificationsDelegate;

	public static FixSkeletonsDelegate call_FixSkeletonsDelegate;

	public static GetGameTypeDelegate call_GetGameTypeDelegate;

	public static GetGlobalTimeDelegate call_GetGlobalTimeDelegate;

	public static GetLastMessagesDelegate call_GetLastMessagesDelegate;

	public static PauseGameDelegate call_PauseGameDelegate;

	public static SetBodyUsedDelegate call_SetBodyUsedDelegate;

	public static SetGameTypeDelegate call_SetGameTypeDelegate;

	public static SetMaterialUsedDelegate call_SetMaterialUsedDelegate;

	public static SetMeshUsedDelegate call_SetMeshUsedDelegate;

	public static UnpauseGameDelegate call_UnpauseGameDelegate;

	public void CheckResourceModifications()
	{
		call_CheckResourceModificationsDelegate();
	}

	public void FixSkeletons()
	{
		call_FixSkeletonsDelegate();
	}

	public int GetGameType()
	{
		return call_GetGameTypeDelegate();
	}

	public float GetGlobalTime(MBCommon.TimeType timeType)
	{
		return call_GetGlobalTimeDelegate(timeType);
	}

	public string GetLastMessages()
	{
		if (call_GetLastMessagesDelegate() != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public void PauseGame()
	{
		call_PauseGameDelegate();
	}

	public void SetBodyUsed(string bodyName)
	{
		byte[] array = null;
		if (bodyName != null)
		{
			int byteCount = _utf8.GetByteCount(bodyName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(bodyName, 0, bodyName.Length, array, 0);
			array[byteCount] = 0;
		}
		call_SetBodyUsedDelegate(array);
	}

	public void SetGameType(int gameType)
	{
		call_SetGameTypeDelegate(gameType);
	}

	public void SetMaterialUsed(string materialName)
	{
		byte[] array = null;
		if (materialName != null)
		{
			int byteCount = _utf8.GetByteCount(materialName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(materialName, 0, materialName.Length, array, 0);
			array[byteCount] = 0;
		}
		call_SetMaterialUsedDelegate(array);
	}

	public void SetMeshUsed(string meshName)
	{
		byte[] array = null;
		if (meshName != null)
		{
			int byteCount = _utf8.GetByteCount(meshName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(meshName, 0, meshName.Length, array, 0);
			array[byteCount] = 0;
		}
		call_SetMeshUsedDelegate(array);
	}

	public void UnpauseGame()
	{
		call_UnpauseGameDelegate();
	}
}
