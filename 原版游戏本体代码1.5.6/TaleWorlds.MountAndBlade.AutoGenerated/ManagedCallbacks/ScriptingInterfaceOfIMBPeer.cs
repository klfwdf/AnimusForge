using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.MountAndBlade;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfIMBPeer : IMBPeer
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void BeginModuleEventDelegate(int index, [MarshalAs(UnmanagedType.U1)] bool isReliable);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void EndModuleEventDelegate([MarshalAs(UnmanagedType.U1)] bool isReliable);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate double GetAverageLossPercentDelegate(int index);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate double GetAveragePingInMillisecondsDelegate(int index);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate uint GetHostDelegate(int index);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool GetIsSynchronizedDelegate(int index);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate ushort GetPortDelegate(int index);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate uint GetReversedHostDelegate(int index);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool IsActiveDelegate(int index);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SendExistingObjectsDelegate(int index, UIntPtr missionPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetControlledAgentDelegate(int index, UIntPtr missionPointer, int agentIndex);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetIsSynchronizedDelegate(int index, [MarshalAs(UnmanagedType.U1)] bool value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetRelevantGameOptionsDelegate(int index, [MarshalAs(UnmanagedType.U1)] bool sendMeBloodEvents, [MarshalAs(UnmanagedType.U1)] bool sendMeSoundEvents);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetTeamDelegate(int index, int teamIndex);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetUserDataDelegate(int index, int data);

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static BeginModuleEventDelegate call_BeginModuleEventDelegate;

	public static EndModuleEventDelegate call_EndModuleEventDelegate;

	public static GetAverageLossPercentDelegate call_GetAverageLossPercentDelegate;

	public static GetAveragePingInMillisecondsDelegate call_GetAveragePingInMillisecondsDelegate;

	public static GetHostDelegate call_GetHostDelegate;

	public static GetIsSynchronizedDelegate call_GetIsSynchronizedDelegate;

	public static GetPortDelegate call_GetPortDelegate;

	public static GetReversedHostDelegate call_GetReversedHostDelegate;

	public static IsActiveDelegate call_IsActiveDelegate;

	public static SendExistingObjectsDelegate call_SendExistingObjectsDelegate;

	public static SetControlledAgentDelegate call_SetControlledAgentDelegate;

	public static SetIsSynchronizedDelegate call_SetIsSynchronizedDelegate;

	public static SetRelevantGameOptionsDelegate call_SetRelevantGameOptionsDelegate;

	public static SetTeamDelegate call_SetTeamDelegate;

	public static SetUserDataDelegate call_SetUserDataDelegate;

	public void BeginModuleEvent(int index, bool isReliable)
	{
		call_BeginModuleEventDelegate(index, isReliable);
	}

	public void EndModuleEvent(bool isReliable)
	{
		call_EndModuleEventDelegate(isReliable);
	}

	public double GetAverageLossPercent(int index)
	{
		return call_GetAverageLossPercentDelegate(index);
	}

	public double GetAveragePingInMilliseconds(int index)
	{
		return call_GetAveragePingInMillisecondsDelegate(index);
	}

	public uint GetHost(int index)
	{
		return call_GetHostDelegate(index);
	}

	public bool GetIsSynchronized(int index)
	{
		return call_GetIsSynchronizedDelegate(index);
	}

	public ushort GetPort(int index)
	{
		return call_GetPortDelegate(index);
	}

	public uint GetReversedHost(int index)
	{
		return call_GetReversedHostDelegate(index);
	}

	public bool IsActive(int index)
	{
		return call_IsActiveDelegate(index);
	}

	public void SendExistingObjects(int index, UIntPtr missionPointer)
	{
		call_SendExistingObjectsDelegate(index, missionPointer);
	}

	public void SetControlledAgent(int index, UIntPtr missionPointer, int agentIndex)
	{
		call_SetControlledAgentDelegate(index, missionPointer, agentIndex);
	}

	public void SetIsSynchronized(int index, bool value)
	{
		call_SetIsSynchronizedDelegate(index, value);
	}

	public void SetRelevantGameOptions(int index, bool sendMeBloodEvents, bool sendMeSoundEvents)
	{
		call_SetRelevantGameOptionsDelegate(index, sendMeBloodEvents, sendMeSoundEvents);
	}

	public void SetTeam(int index, int teamIndex)
	{
		call_SetTeamDelegate(index, teamIndex);
	}

	public void SetUserData(int index, MBNetworkPeer data)
	{
		call_SetUserDataDelegate(index, data?.GetManagedId() ?? 0);
	}
}
