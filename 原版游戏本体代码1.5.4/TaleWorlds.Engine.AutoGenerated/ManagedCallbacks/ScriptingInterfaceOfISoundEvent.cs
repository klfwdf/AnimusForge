using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfISoundEvent : ISoundEvent
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int CreateEventDelegate(int fmodEventIndex, UIntPtr scene);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int CreateEventFromExternalFileDelegate(byte[] programmerSoundEventName, byte[] filePath, UIntPtr scene, [MarshalAs(UnmanagedType.U1)] bool is3d, [MarshalAs(UnmanagedType.U1)] bool isBlocking);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int CreateEventFromSoundBufferDelegate(byte[] programmerSoundEventName, ManagedArray soundBuffer, UIntPtr scene, [MarshalAs(UnmanagedType.U1)] bool is3d, [MarshalAs(UnmanagedType.U1)] bool isBlocking);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int CreateEventFromStringDelegate(byte[] eventName, UIntPtr scene);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetEventIdFromStringDelegate(byte[] eventName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate Vec3 GetEventMinMaxDistanceDelegate(int eventId);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetTotalEventCountDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool IsPausedDelegate(int eventId);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool IsPlayingDelegate(int eventId);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool IsStoppedDelegate(int eventId);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool IsValidDelegate(int eventId);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void PauseEventDelegate(int eventId);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void PlayExtraEventDelegate(int soundId, byte[] eventName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool PlaySound2DDelegate(int fmodEventIndex);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ReleaseEventDelegate(int eventId);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ResumeEventDelegate(int eventId);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetEventMinMaxDistanceDelegate(int fmodEventIndex, Vec3 radius);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetEventParameterAtIndexDelegate(int soundId, int parameterIndex, float value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetEventParameterFromStringDelegate(int eventId, byte[] name, float value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetEventPositionDelegate(int eventId, ref Vec3 position);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetEventVelocityDelegate(int eventId, ref Vec3 velocity);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetSwitchDelegate(int soundId, byte[] switchGroupName, byte[] newSwitchStateName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool StartEventDelegate(int eventId);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool StartEventInPositionDelegate(int eventId, ref Vec3 position);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void StopEventDelegate(int eventId);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void TriggerCueDelegate(int eventId);

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static CreateEventDelegate call_CreateEventDelegate;

	public static CreateEventFromExternalFileDelegate call_CreateEventFromExternalFileDelegate;

	public static CreateEventFromSoundBufferDelegate call_CreateEventFromSoundBufferDelegate;

	public static CreateEventFromStringDelegate call_CreateEventFromStringDelegate;

	public static GetEventIdFromStringDelegate call_GetEventIdFromStringDelegate;

	public static GetEventMinMaxDistanceDelegate call_GetEventMinMaxDistanceDelegate;

	public static GetTotalEventCountDelegate call_GetTotalEventCountDelegate;

	public static IsPausedDelegate call_IsPausedDelegate;

	public static IsPlayingDelegate call_IsPlayingDelegate;

	public static IsStoppedDelegate call_IsStoppedDelegate;

	public static IsValidDelegate call_IsValidDelegate;

	public static PauseEventDelegate call_PauseEventDelegate;

	public static PlayExtraEventDelegate call_PlayExtraEventDelegate;

	public static PlaySound2DDelegate call_PlaySound2DDelegate;

	public static ReleaseEventDelegate call_ReleaseEventDelegate;

	public static ResumeEventDelegate call_ResumeEventDelegate;

	public static SetEventMinMaxDistanceDelegate call_SetEventMinMaxDistanceDelegate;

	public static SetEventParameterAtIndexDelegate call_SetEventParameterAtIndexDelegate;

	public static SetEventParameterFromStringDelegate call_SetEventParameterFromStringDelegate;

	public static SetEventPositionDelegate call_SetEventPositionDelegate;

	public static SetEventVelocityDelegate call_SetEventVelocityDelegate;

	public static SetSwitchDelegate call_SetSwitchDelegate;

	public static StartEventDelegate call_StartEventDelegate;

	public static StartEventInPositionDelegate call_StartEventInPositionDelegate;

	public static StopEventDelegate call_StopEventDelegate;

	public static TriggerCueDelegate call_TriggerCueDelegate;

	public int CreateEvent(int fmodEventIndex, UIntPtr scene)
	{
		return call_CreateEventDelegate(fmodEventIndex, scene);
	}

	public int CreateEventFromExternalFile(string programmerSoundEventName, string filePath, UIntPtr scene, bool is3d, bool isBlocking)
	{
		byte[] array = null;
		if (programmerSoundEventName != null)
		{
			int byteCount = _utf8.GetByteCount(programmerSoundEventName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(programmerSoundEventName, 0, programmerSoundEventName.Length, array, 0);
			array[byteCount] = 0;
		}
		byte[] array2 = null;
		if (filePath != null)
		{
			int byteCount2 = _utf8.GetByteCount(filePath);
			array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
			_utf8.GetBytes(filePath, 0, filePath.Length, array2, 0);
			array2[byteCount2] = 0;
		}
		return call_CreateEventFromExternalFileDelegate(array, array2, scene, is3d, isBlocking);
	}

	public int CreateEventFromSoundBuffer(string programmerSoundEventName, byte[] soundBuffer, UIntPtr scene, bool is3d, bool isBlocking)
	{
		byte[] array = null;
		if (programmerSoundEventName != null)
		{
			int byteCount = _utf8.GetByteCount(programmerSoundEventName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(programmerSoundEventName, 0, programmerSoundEventName.Length, array, 0);
			array[byteCount] = 0;
		}
		PinnedArrayData<byte> pinnedArrayData = new PinnedArrayData<byte>(soundBuffer);
		IntPtr pointer = pinnedArrayData.Pointer;
		ManagedArray soundBuffer2 = new ManagedArray(pointer, (soundBuffer != null) ? soundBuffer.Length : 0);
		int result = call_CreateEventFromSoundBufferDelegate(array, soundBuffer2, scene, is3d, isBlocking);
		pinnedArrayData.Dispose();
		return result;
	}

	public int CreateEventFromString(string eventName, UIntPtr scene)
	{
		byte[] array = null;
		if (eventName != null)
		{
			int byteCount = _utf8.GetByteCount(eventName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(eventName, 0, eventName.Length, array, 0);
			array[byteCount] = 0;
		}
		return call_CreateEventFromStringDelegate(array, scene);
	}

	public int GetEventIdFromString(string eventName)
	{
		byte[] array = null;
		if (eventName != null)
		{
			int byteCount = _utf8.GetByteCount(eventName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(eventName, 0, eventName.Length, array, 0);
			array[byteCount] = 0;
		}
		return call_GetEventIdFromStringDelegate(array);
	}

	public Vec3 GetEventMinMaxDistance(int eventId)
	{
		return call_GetEventMinMaxDistanceDelegate(eventId);
	}

	public int GetTotalEventCount()
	{
		return call_GetTotalEventCountDelegate();
	}

	public bool IsPaused(int eventId)
	{
		return call_IsPausedDelegate(eventId);
	}

	public bool IsPlaying(int eventId)
	{
		return call_IsPlayingDelegate(eventId);
	}

	public bool IsStopped(int eventId)
	{
		return call_IsStoppedDelegate(eventId);
	}

	public bool IsValid(int eventId)
	{
		return call_IsValidDelegate(eventId);
	}

	public void PauseEvent(int eventId)
	{
		call_PauseEventDelegate(eventId);
	}

	public void PlayExtraEvent(int soundId, string eventName)
	{
		byte[] array = null;
		if (eventName != null)
		{
			int byteCount = _utf8.GetByteCount(eventName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(eventName, 0, eventName.Length, array, 0);
			array[byteCount] = 0;
		}
		call_PlayExtraEventDelegate(soundId, array);
	}

	public bool PlaySound2D(int fmodEventIndex)
	{
		return call_PlaySound2DDelegate(fmodEventIndex);
	}

	public void ReleaseEvent(int eventId)
	{
		call_ReleaseEventDelegate(eventId);
	}

	public void ResumeEvent(int eventId)
	{
		call_ResumeEventDelegate(eventId);
	}

	public void SetEventMinMaxDistance(int fmodEventIndex, Vec3 radius)
	{
		call_SetEventMinMaxDistanceDelegate(fmodEventIndex, radius);
	}

	public void SetEventParameterAtIndex(int soundId, int parameterIndex, float value)
	{
		call_SetEventParameterAtIndexDelegate(soundId, parameterIndex, value);
	}

	public void SetEventParameterFromString(int eventId, string name, float value)
	{
		byte[] array = null;
		if (name != null)
		{
			int byteCount = _utf8.GetByteCount(name);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(name, 0, name.Length, array, 0);
			array[byteCount] = 0;
		}
		call_SetEventParameterFromStringDelegate(eventId, array, value);
	}

	public void SetEventPosition(int eventId, ref Vec3 position)
	{
		call_SetEventPositionDelegate(eventId, ref position);
	}

	public void SetEventVelocity(int eventId, ref Vec3 velocity)
	{
		call_SetEventVelocityDelegate(eventId, ref velocity);
	}

	public void SetSwitch(int soundId, string switchGroupName, string newSwitchStateName)
	{
		byte[] array = null;
		if (switchGroupName != null)
		{
			int byteCount = _utf8.GetByteCount(switchGroupName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(switchGroupName, 0, switchGroupName.Length, array, 0);
			array[byteCount] = 0;
		}
		byte[] array2 = null;
		if (newSwitchStateName != null)
		{
			int byteCount2 = _utf8.GetByteCount(newSwitchStateName);
			array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
			_utf8.GetBytes(newSwitchStateName, 0, newSwitchStateName.Length, array2, 0);
			array2[byteCount2] = 0;
		}
		call_SetSwitchDelegate(soundId, array, array2);
	}

	public bool StartEvent(int eventId)
	{
		return call_StartEventDelegate(eventId);
	}

	public bool StartEventInPosition(int eventId, ref Vec3 position)
	{
		return call_StartEventInPositionDelegate(eventId, ref position);
	}

	public void StopEvent(int eventId)
	{
		call_StopEventDelegate(eventId);
	}

	public void TriggerCue(int eventId)
	{
		call_TriggerCueDelegate(eventId);
	}
}
