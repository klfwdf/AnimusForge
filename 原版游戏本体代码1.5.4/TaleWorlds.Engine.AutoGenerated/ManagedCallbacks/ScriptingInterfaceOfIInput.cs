using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfIInput : IInput
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ClearKeysDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetClipboardTextDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetControllerTypeDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetGyroXDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetGyroYDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetGyroZDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate Vec2 GetKeyStateDelegate(InputKey key);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetMouseDeltaZDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetMouseMoveXDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetMouseMoveYDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetMousePositionXDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetMousePositionYDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetMouseScrollValueDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetMouseSensitivityDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetVirtualKeyCodeDelegate(InputKey key);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool IsAnyTouchActiveDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool IsControllerConnectedDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool IsKeyDownDelegate(InputKey key);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool IsKeyDownImmediateDelegate(InputKey key);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool IsKeyPressedDelegate(InputKey key);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool IsKeyReleasedDelegate(InputKey key);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool IsMouseActiveDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void PressKeyDelegate(InputKey key);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetClipboardTextDelegate(byte[] text);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetCursorFrictionValueDelegate(float frictionValue);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetCursorPositionDelegate(int x, int y);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetLightbarColorDelegate(float red, float green, float blue);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetRumbleEffectDelegate(IntPtr lowFrequencyLevels, IntPtr lowFrequencyDurations, int numLowFrequencyElements, IntPtr highFrequencyLevels, IntPtr highFrequencyDurations, int numHighFrequencyElements);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetTriggerFeedbackDelegate(byte leftTriggerPosition, byte leftTriggerStrength, byte rightTriggerPosition, byte rightTriggerStrength);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetTriggerVibrationDelegate(IntPtr leftTriggerAmplitudes, IntPtr leftTriggerFrequencies, IntPtr leftTriggerDurations, int numLeftTriggerElements, IntPtr rightTriggerAmplitudes, IntPtr rightTriggerFrequencies, IntPtr rightTriggerDurations, int numRightTriggerElements);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetTriggerWeaponEffectDelegate(byte leftStartPosition, byte leftEnd_position, byte leftStrength, byte rightStartPosition, byte rightEndPosition, byte rightStrength);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void UpdateKeyDataDelegate(ManagedArray keyData);

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static ClearKeysDelegate call_ClearKeysDelegate;

	public static GetClipboardTextDelegate call_GetClipboardTextDelegate;

	public static GetControllerTypeDelegate call_GetControllerTypeDelegate;

	public static GetGyroXDelegate call_GetGyroXDelegate;

	public static GetGyroYDelegate call_GetGyroYDelegate;

	public static GetGyroZDelegate call_GetGyroZDelegate;

	public static GetKeyStateDelegate call_GetKeyStateDelegate;

	public static GetMouseDeltaZDelegate call_GetMouseDeltaZDelegate;

	public static GetMouseMoveXDelegate call_GetMouseMoveXDelegate;

	public static GetMouseMoveYDelegate call_GetMouseMoveYDelegate;

	public static GetMousePositionXDelegate call_GetMousePositionXDelegate;

	public static GetMousePositionYDelegate call_GetMousePositionYDelegate;

	public static GetMouseScrollValueDelegate call_GetMouseScrollValueDelegate;

	public static GetMouseSensitivityDelegate call_GetMouseSensitivityDelegate;

	public static GetVirtualKeyCodeDelegate call_GetVirtualKeyCodeDelegate;

	public static IsAnyTouchActiveDelegate call_IsAnyTouchActiveDelegate;

	public static IsControllerConnectedDelegate call_IsControllerConnectedDelegate;

	public static IsKeyDownDelegate call_IsKeyDownDelegate;

	public static IsKeyDownImmediateDelegate call_IsKeyDownImmediateDelegate;

	public static IsKeyPressedDelegate call_IsKeyPressedDelegate;

	public static IsKeyReleasedDelegate call_IsKeyReleasedDelegate;

	public static IsMouseActiveDelegate call_IsMouseActiveDelegate;

	public static PressKeyDelegate call_PressKeyDelegate;

	public static SetClipboardTextDelegate call_SetClipboardTextDelegate;

	public static SetCursorFrictionValueDelegate call_SetCursorFrictionValueDelegate;

	public static SetCursorPositionDelegate call_SetCursorPositionDelegate;

	public static SetLightbarColorDelegate call_SetLightbarColorDelegate;

	public static SetRumbleEffectDelegate call_SetRumbleEffectDelegate;

	public static SetTriggerFeedbackDelegate call_SetTriggerFeedbackDelegate;

	public static SetTriggerVibrationDelegate call_SetTriggerVibrationDelegate;

	public static SetTriggerWeaponEffectDelegate call_SetTriggerWeaponEffectDelegate;

	public static UpdateKeyDataDelegate call_UpdateKeyDataDelegate;

	public void ClearKeys()
	{
		call_ClearKeysDelegate();
	}

	public string GetClipboardText()
	{
		if (call_GetClipboardTextDelegate() != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public int GetControllerType()
	{
		return call_GetControllerTypeDelegate();
	}

	public float GetGyroX()
	{
		return call_GetGyroXDelegate();
	}

	public float GetGyroY()
	{
		return call_GetGyroYDelegate();
	}

	public float GetGyroZ()
	{
		return call_GetGyroZDelegate();
	}

	public Vec2 GetKeyState(InputKey key)
	{
		return call_GetKeyStateDelegate(key);
	}

	public float GetMouseDeltaZ()
	{
		return call_GetMouseDeltaZDelegate();
	}

	public float GetMouseMoveX()
	{
		return call_GetMouseMoveXDelegate();
	}

	public float GetMouseMoveY()
	{
		return call_GetMouseMoveYDelegate();
	}

	public float GetMousePositionX()
	{
		return call_GetMousePositionXDelegate();
	}

	public float GetMousePositionY()
	{
		return call_GetMousePositionYDelegate();
	}

	public float GetMouseScrollValue()
	{
		return call_GetMouseScrollValueDelegate();
	}

	public float GetMouseSensitivity()
	{
		return call_GetMouseSensitivityDelegate();
	}

	public int GetVirtualKeyCode(InputKey key)
	{
		return call_GetVirtualKeyCodeDelegate(key);
	}

	public bool IsAnyTouchActive()
	{
		return call_IsAnyTouchActiveDelegate();
	}

	public bool IsControllerConnected()
	{
		return call_IsControllerConnectedDelegate();
	}

	public bool IsKeyDown(InputKey key)
	{
		return call_IsKeyDownDelegate(key);
	}

	public bool IsKeyDownImmediate(InputKey key)
	{
		return call_IsKeyDownImmediateDelegate(key);
	}

	public bool IsKeyPressed(InputKey key)
	{
		return call_IsKeyPressedDelegate(key);
	}

	public bool IsKeyReleased(InputKey key)
	{
		return call_IsKeyReleasedDelegate(key);
	}

	public bool IsMouseActive()
	{
		return call_IsMouseActiveDelegate();
	}

	public void PressKey(InputKey key)
	{
		call_PressKeyDelegate(key);
	}

	public void SetClipboardText(string text)
	{
		byte[] array = null;
		if (text != null)
		{
			int byteCount = _utf8.GetByteCount(text);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(text, 0, text.Length, array, 0);
			array[byteCount] = 0;
		}
		call_SetClipboardTextDelegate(array);
	}

	public void SetCursorFrictionValue(float frictionValue)
	{
		call_SetCursorFrictionValueDelegate(frictionValue);
	}

	public void SetCursorPosition(int x, int y)
	{
		call_SetCursorPositionDelegate(x, y);
	}

	public void SetLightbarColor(float red, float green, float blue)
	{
		call_SetLightbarColorDelegate(red, green, blue);
	}

	public void SetRumbleEffect(float[] lowFrequencyLevels, float[] lowFrequencyDurations, int numLowFrequencyElements, float[] highFrequencyLevels, float[] highFrequencyDurations, int numHighFrequencyElements)
	{
		PinnedArrayData<float> pinnedArrayData = new PinnedArrayData<float>(lowFrequencyLevels);
		IntPtr pointer = pinnedArrayData.Pointer;
		PinnedArrayData<float> pinnedArrayData2 = new PinnedArrayData<float>(lowFrequencyDurations);
		IntPtr pointer2 = pinnedArrayData2.Pointer;
		PinnedArrayData<float> pinnedArrayData3 = new PinnedArrayData<float>(highFrequencyLevels);
		IntPtr pointer3 = pinnedArrayData3.Pointer;
		PinnedArrayData<float> pinnedArrayData4 = new PinnedArrayData<float>(highFrequencyDurations);
		IntPtr pointer4 = pinnedArrayData4.Pointer;
		call_SetRumbleEffectDelegate(pointer, pointer2, numLowFrequencyElements, pointer3, pointer4, numHighFrequencyElements);
		pinnedArrayData.Dispose();
		pinnedArrayData2.Dispose();
		pinnedArrayData3.Dispose();
		pinnedArrayData4.Dispose();
	}

	public void SetTriggerFeedback(byte leftTriggerPosition, byte leftTriggerStrength, byte rightTriggerPosition, byte rightTriggerStrength)
	{
		call_SetTriggerFeedbackDelegate(leftTriggerPosition, leftTriggerStrength, rightTriggerPosition, rightTriggerStrength);
	}

	public void SetTriggerVibration(float[] leftTriggerAmplitudes, float[] leftTriggerFrequencies, float[] leftTriggerDurations, int numLeftTriggerElements, float[] rightTriggerAmplitudes, float[] rightTriggerFrequencies, float[] rightTriggerDurations, int numRightTriggerElements)
	{
		PinnedArrayData<float> pinnedArrayData = new PinnedArrayData<float>(leftTriggerAmplitudes);
		IntPtr pointer = pinnedArrayData.Pointer;
		PinnedArrayData<float> pinnedArrayData2 = new PinnedArrayData<float>(leftTriggerFrequencies);
		IntPtr pointer2 = pinnedArrayData2.Pointer;
		PinnedArrayData<float> pinnedArrayData3 = new PinnedArrayData<float>(leftTriggerDurations);
		IntPtr pointer3 = pinnedArrayData3.Pointer;
		PinnedArrayData<float> pinnedArrayData4 = new PinnedArrayData<float>(rightTriggerAmplitudes);
		IntPtr pointer4 = pinnedArrayData4.Pointer;
		PinnedArrayData<float> pinnedArrayData5 = new PinnedArrayData<float>(rightTriggerFrequencies);
		IntPtr pointer5 = pinnedArrayData5.Pointer;
		PinnedArrayData<float> pinnedArrayData6 = new PinnedArrayData<float>(rightTriggerDurations);
		IntPtr pointer6 = pinnedArrayData6.Pointer;
		call_SetTriggerVibrationDelegate(pointer, pointer2, pointer3, numLeftTriggerElements, pointer4, pointer5, pointer6, numRightTriggerElements);
		pinnedArrayData.Dispose();
		pinnedArrayData2.Dispose();
		pinnedArrayData3.Dispose();
		pinnedArrayData4.Dispose();
		pinnedArrayData5.Dispose();
		pinnedArrayData6.Dispose();
	}

	public void SetTriggerWeaponEffect(byte leftStartPosition, byte leftEnd_position, byte leftStrength, byte rightStartPosition, byte rightEndPosition, byte rightStrength)
	{
		call_SetTriggerWeaponEffectDelegate(leftStartPosition, leftEnd_position, leftStrength, rightStartPosition, rightEndPosition, rightStrength);
	}

	public void UpdateKeyData(byte[] keyData)
	{
		PinnedArrayData<byte> pinnedArrayData = new PinnedArrayData<byte>(keyData);
		IntPtr pointer = pinnedArrayData.Pointer;
		ManagedArray keyData2 = new ManagedArray(pointer, (keyData != null) ? keyData.Length : 0);
		call_UpdateKeyDataDelegate(keyData2);
		pinnedArrayData.Dispose();
	}
}
