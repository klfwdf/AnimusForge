using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace ManagedCallbacks
{
	// Token: 0x02000015 RID: 21
	internal class ScriptingInterfaceOfIInput : IInput
	{
		// Token: 0x06000281 RID: 641 RVA: 0x00012B3E File Offset: 0x00010D3E
		public void ClearKeys()
		{
			ScriptingInterfaceOfIInput.call_ClearKeysDelegate();
		}

		// Token: 0x06000282 RID: 642 RVA: 0x00012B4A File Offset: 0x00010D4A
		public string GetClipboardText()
		{
			if (ScriptingInterfaceOfIInput.call_GetClipboardTextDelegate() != 1)
			{
				return null;
			}
			return Managed.ReturnValueFromEngine;
		}

		// Token: 0x06000283 RID: 643 RVA: 0x00012B60 File Offset: 0x00010D60
		public int GetControllerType()
		{
			return ScriptingInterfaceOfIInput.call_GetControllerTypeDelegate();
		}

		// Token: 0x06000284 RID: 644 RVA: 0x00012B6C File Offset: 0x00010D6C
		public float GetGyroX()
		{
			return ScriptingInterfaceOfIInput.call_GetGyroXDelegate();
		}

		// Token: 0x06000285 RID: 645 RVA: 0x00012B78 File Offset: 0x00010D78
		public float GetGyroY()
		{
			return ScriptingInterfaceOfIInput.call_GetGyroYDelegate();
		}

		// Token: 0x06000286 RID: 646 RVA: 0x00012B84 File Offset: 0x00010D84
		public float GetGyroZ()
		{
			return ScriptingInterfaceOfIInput.call_GetGyroZDelegate();
		}

		// Token: 0x06000287 RID: 647 RVA: 0x00012B90 File Offset: 0x00010D90
		public Vec2 GetKeyState(InputKey key)
		{
			return ScriptingInterfaceOfIInput.call_GetKeyStateDelegate(key);
		}

		// Token: 0x06000288 RID: 648 RVA: 0x00012B9D File Offset: 0x00010D9D
		public float GetMouseDeltaZ()
		{
			return ScriptingInterfaceOfIInput.call_GetMouseDeltaZDelegate();
		}

		// Token: 0x06000289 RID: 649 RVA: 0x00012BA9 File Offset: 0x00010DA9
		public float GetMouseMoveX()
		{
			return ScriptingInterfaceOfIInput.call_GetMouseMoveXDelegate();
		}

		// Token: 0x0600028A RID: 650 RVA: 0x00012BB5 File Offset: 0x00010DB5
		public float GetMouseMoveY()
		{
			return ScriptingInterfaceOfIInput.call_GetMouseMoveYDelegate();
		}

		// Token: 0x0600028B RID: 651 RVA: 0x00012BC1 File Offset: 0x00010DC1
		public float GetMousePositionX()
		{
			return ScriptingInterfaceOfIInput.call_GetMousePositionXDelegate();
		}

		// Token: 0x0600028C RID: 652 RVA: 0x00012BCD File Offset: 0x00010DCD
		public float GetMousePositionY()
		{
			return ScriptingInterfaceOfIInput.call_GetMousePositionYDelegate();
		}

		// Token: 0x0600028D RID: 653 RVA: 0x00012BD9 File Offset: 0x00010DD9
		public float GetMouseScrollValue()
		{
			return ScriptingInterfaceOfIInput.call_GetMouseScrollValueDelegate();
		}

		// Token: 0x0600028E RID: 654 RVA: 0x00012BE5 File Offset: 0x00010DE5
		public float GetMouseSensitivity()
		{
			return ScriptingInterfaceOfIInput.call_GetMouseSensitivityDelegate();
		}

		// Token: 0x0600028F RID: 655 RVA: 0x00012BF1 File Offset: 0x00010DF1
		public int GetVirtualKeyCode(InputKey key)
		{
			return ScriptingInterfaceOfIInput.call_GetVirtualKeyCodeDelegate(key);
		}

		// Token: 0x06000290 RID: 656 RVA: 0x00012BFE File Offset: 0x00010DFE
		public bool IsAnyTouchActive()
		{
			return ScriptingInterfaceOfIInput.call_IsAnyTouchActiveDelegate();
		}

		// Token: 0x06000291 RID: 657 RVA: 0x00012C0A File Offset: 0x00010E0A
		public bool IsControllerConnected()
		{
			return ScriptingInterfaceOfIInput.call_IsControllerConnectedDelegate();
		}

		// Token: 0x06000292 RID: 658 RVA: 0x00012C16 File Offset: 0x00010E16
		public bool IsKeyDown(InputKey key)
		{
			return ScriptingInterfaceOfIInput.call_IsKeyDownDelegate(key);
		}

		// Token: 0x06000293 RID: 659 RVA: 0x00012C23 File Offset: 0x00010E23
		public bool IsKeyDownImmediate(InputKey key)
		{
			return ScriptingInterfaceOfIInput.call_IsKeyDownImmediateDelegate(key);
		}

		// Token: 0x06000294 RID: 660 RVA: 0x00012C30 File Offset: 0x00010E30
		public bool IsKeyPressed(InputKey key)
		{
			return ScriptingInterfaceOfIInput.call_IsKeyPressedDelegate(key);
		}

		// Token: 0x06000295 RID: 661 RVA: 0x00012C3D File Offset: 0x00010E3D
		public bool IsKeyReleased(InputKey key)
		{
			return ScriptingInterfaceOfIInput.call_IsKeyReleasedDelegate(key);
		}

		// Token: 0x06000296 RID: 662 RVA: 0x00012C4A File Offset: 0x00010E4A
		public bool IsMouseActive()
		{
			return ScriptingInterfaceOfIInput.call_IsMouseActiveDelegate();
		}

		// Token: 0x06000297 RID: 663 RVA: 0x00012C56 File Offset: 0x00010E56
		public void PressKey(InputKey key)
		{
			ScriptingInterfaceOfIInput.call_PressKeyDelegate(key);
		}

		// Token: 0x06000298 RID: 664 RVA: 0x00012C64 File Offset: 0x00010E64
		public void SetClipboardText(string text)
		{
			byte[] array = null;
			if (text != null)
			{
				int byteCount = ScriptingInterfaceOfIInput._utf8.GetByteCount(text);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIInput._utf8.GetBytes(text, 0, text.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIInput.call_SetClipboardTextDelegate(array);
		}

		// Token: 0x06000299 RID: 665 RVA: 0x00012CBE File Offset: 0x00010EBE
		public void SetCursorFrictionValue(float frictionValue)
		{
			ScriptingInterfaceOfIInput.call_SetCursorFrictionValueDelegate(frictionValue);
		}

		// Token: 0x0600029A RID: 666 RVA: 0x00012CCB File Offset: 0x00010ECB
		public void SetCursorPosition(int x, int y)
		{
			ScriptingInterfaceOfIInput.call_SetCursorPositionDelegate(x, y);
		}

		// Token: 0x0600029B RID: 667 RVA: 0x00012CD9 File Offset: 0x00010ED9
		public void SetLightbarColor(float red, float green, float blue)
		{
			ScriptingInterfaceOfIInput.call_SetLightbarColorDelegate(red, green, blue);
		}

		// Token: 0x0600029C RID: 668 RVA: 0x00012CE8 File Offset: 0x00010EE8
		public void SetRumbleEffect(float[] lowFrequencyLevels, float[] lowFrequencyDurations, int numLowFrequencyElements, float[] highFrequencyLevels, float[] highFrequencyDurations, int numHighFrequencyElements)
		{
			PinnedArrayData<float> pinnedArrayData = new PinnedArrayData<float>(lowFrequencyLevels, false);
			IntPtr pointer = pinnedArrayData.Pointer;
			PinnedArrayData<float> pinnedArrayData2 = new PinnedArrayData<float>(lowFrequencyDurations, false);
			IntPtr pointer2 = pinnedArrayData2.Pointer;
			PinnedArrayData<float> pinnedArrayData3 = new PinnedArrayData<float>(highFrequencyLevels, false);
			IntPtr pointer3 = pinnedArrayData3.Pointer;
			PinnedArrayData<float> pinnedArrayData4 = new PinnedArrayData<float>(highFrequencyDurations, false);
			IntPtr pointer4 = pinnedArrayData4.Pointer;
			ScriptingInterfaceOfIInput.call_SetRumbleEffectDelegate(pointer, pointer2, numLowFrequencyElements, pointer3, pointer4, numHighFrequencyElements);
			pinnedArrayData.Dispose();
			pinnedArrayData2.Dispose();
			pinnedArrayData3.Dispose();
			pinnedArrayData4.Dispose();
		}

		// Token: 0x0600029D RID: 669 RVA: 0x00012D6C File Offset: 0x00010F6C
		public void SetTriggerFeedback(byte leftTriggerPosition, byte leftTriggerStrength, byte rightTriggerPosition, byte rightTriggerStrength)
		{
			ScriptingInterfaceOfIInput.call_SetTriggerFeedbackDelegate(leftTriggerPosition, leftTriggerStrength, rightTriggerPosition, rightTriggerStrength);
		}

		// Token: 0x0600029E RID: 670 RVA: 0x00012D80 File Offset: 0x00010F80
		public void SetTriggerVibration(float[] leftTriggerAmplitudes, float[] leftTriggerFrequencies, float[] leftTriggerDurations, int numLeftTriggerElements, float[] rightTriggerAmplitudes, float[] rightTriggerFrequencies, float[] rightTriggerDurations, int numRightTriggerElements)
		{
			PinnedArrayData<float> pinnedArrayData = new PinnedArrayData<float>(leftTriggerAmplitudes, false);
			IntPtr pointer = pinnedArrayData.Pointer;
			PinnedArrayData<float> pinnedArrayData2 = new PinnedArrayData<float>(leftTriggerFrequencies, false);
			IntPtr pointer2 = pinnedArrayData2.Pointer;
			PinnedArrayData<float> pinnedArrayData3 = new PinnedArrayData<float>(leftTriggerDurations, false);
			IntPtr pointer3 = pinnedArrayData3.Pointer;
			PinnedArrayData<float> pinnedArrayData4 = new PinnedArrayData<float>(rightTriggerAmplitudes, false);
			IntPtr pointer4 = pinnedArrayData4.Pointer;
			PinnedArrayData<float> pinnedArrayData5 = new PinnedArrayData<float>(rightTriggerFrequencies, false);
			IntPtr pointer5 = pinnedArrayData5.Pointer;
			PinnedArrayData<float> pinnedArrayData6 = new PinnedArrayData<float>(rightTriggerDurations, false);
			IntPtr pointer6 = pinnedArrayData6.Pointer;
			ScriptingInterfaceOfIInput.call_SetTriggerVibrationDelegate(pointer, pointer2, pointer3, numLeftTriggerElements, pointer4, pointer5, pointer6, numRightTriggerElements);
			pinnedArrayData.Dispose();
			pinnedArrayData2.Dispose();
			pinnedArrayData3.Dispose();
			pinnedArrayData4.Dispose();
			pinnedArrayData5.Dispose();
			pinnedArrayData6.Dispose();
		}

		// Token: 0x0600029F RID: 671 RVA: 0x00012E3C File Offset: 0x0001103C
		public void SetTriggerWeaponEffect(byte leftStartPosition, byte leftEnd_position, byte leftStrength, byte rightStartPosition, byte rightEndPosition, byte rightStrength)
		{
			ScriptingInterfaceOfIInput.call_SetTriggerWeaponEffectDelegate(leftStartPosition, leftEnd_position, leftStrength, rightStartPosition, rightEndPosition, rightStrength);
		}

		// Token: 0x060002A0 RID: 672 RVA: 0x00012E54 File Offset: 0x00011054
		public void UpdateKeyData(byte[] keyData)
		{
			PinnedArrayData<byte> pinnedArrayData = new PinnedArrayData<byte>(keyData, false);
			IntPtr pointer = pinnedArrayData.Pointer;
			ManagedArray keyData2 = new ManagedArray(pointer, (keyData != null) ? keyData.Length : 0);
			ScriptingInterfaceOfIInput.call_UpdateKeyDataDelegate(keyData2);
			pinnedArrayData.Dispose();
		}

		// Token: 0x040001FB RID: 507
		private static readonly Encoding _utf8 = Encoding.UTF8;

		// Token: 0x040001FC RID: 508
		public static ScriptingInterfaceOfIInput.ClearKeysDelegate call_ClearKeysDelegate;

		// Token: 0x040001FD RID: 509
		public static ScriptingInterfaceOfIInput.GetClipboardTextDelegate call_GetClipboardTextDelegate;

		// Token: 0x040001FE RID: 510
		public static ScriptingInterfaceOfIInput.GetControllerTypeDelegate call_GetControllerTypeDelegate;

		// Token: 0x040001FF RID: 511
		public static ScriptingInterfaceOfIInput.GetGyroXDelegate call_GetGyroXDelegate;

		// Token: 0x04000200 RID: 512
		public static ScriptingInterfaceOfIInput.GetGyroYDelegate call_GetGyroYDelegate;

		// Token: 0x04000201 RID: 513
		public static ScriptingInterfaceOfIInput.GetGyroZDelegate call_GetGyroZDelegate;

		// Token: 0x04000202 RID: 514
		public static ScriptingInterfaceOfIInput.GetKeyStateDelegate call_GetKeyStateDelegate;

		// Token: 0x04000203 RID: 515
		public static ScriptingInterfaceOfIInput.GetMouseDeltaZDelegate call_GetMouseDeltaZDelegate;

		// Token: 0x04000204 RID: 516
		public static ScriptingInterfaceOfIInput.GetMouseMoveXDelegate call_GetMouseMoveXDelegate;

		// Token: 0x04000205 RID: 517
		public static ScriptingInterfaceOfIInput.GetMouseMoveYDelegate call_GetMouseMoveYDelegate;

		// Token: 0x04000206 RID: 518
		public static ScriptingInterfaceOfIInput.GetMousePositionXDelegate call_GetMousePositionXDelegate;

		// Token: 0x04000207 RID: 519
		public static ScriptingInterfaceOfIInput.GetMousePositionYDelegate call_GetMousePositionYDelegate;

		// Token: 0x04000208 RID: 520
		public static ScriptingInterfaceOfIInput.GetMouseScrollValueDelegate call_GetMouseScrollValueDelegate;

		// Token: 0x04000209 RID: 521
		public static ScriptingInterfaceOfIInput.GetMouseSensitivityDelegate call_GetMouseSensitivityDelegate;

		// Token: 0x0400020A RID: 522
		public static ScriptingInterfaceOfIInput.GetVirtualKeyCodeDelegate call_GetVirtualKeyCodeDelegate;

		// Token: 0x0400020B RID: 523
		public static ScriptingInterfaceOfIInput.IsAnyTouchActiveDelegate call_IsAnyTouchActiveDelegate;

		// Token: 0x0400020C RID: 524
		public static ScriptingInterfaceOfIInput.IsControllerConnectedDelegate call_IsControllerConnectedDelegate;

		// Token: 0x0400020D RID: 525
		public static ScriptingInterfaceOfIInput.IsKeyDownDelegate call_IsKeyDownDelegate;

		// Token: 0x0400020E RID: 526
		public static ScriptingInterfaceOfIInput.IsKeyDownImmediateDelegate call_IsKeyDownImmediateDelegate;

		// Token: 0x0400020F RID: 527
		public static ScriptingInterfaceOfIInput.IsKeyPressedDelegate call_IsKeyPressedDelegate;

		// Token: 0x04000210 RID: 528
		public static ScriptingInterfaceOfIInput.IsKeyReleasedDelegate call_IsKeyReleasedDelegate;

		// Token: 0x04000211 RID: 529
		public static ScriptingInterfaceOfIInput.IsMouseActiveDelegate call_IsMouseActiveDelegate;

		// Token: 0x04000212 RID: 530
		public static ScriptingInterfaceOfIInput.PressKeyDelegate call_PressKeyDelegate;

		// Token: 0x04000213 RID: 531
		public static ScriptingInterfaceOfIInput.SetClipboardTextDelegate call_SetClipboardTextDelegate;

		// Token: 0x04000214 RID: 532
		public static ScriptingInterfaceOfIInput.SetCursorFrictionValueDelegate call_SetCursorFrictionValueDelegate;

		// Token: 0x04000215 RID: 533
		public static ScriptingInterfaceOfIInput.SetCursorPositionDelegate call_SetCursorPositionDelegate;

		// Token: 0x04000216 RID: 534
		public static ScriptingInterfaceOfIInput.SetLightbarColorDelegate call_SetLightbarColorDelegate;

		// Token: 0x04000217 RID: 535
		public static ScriptingInterfaceOfIInput.SetRumbleEffectDelegate call_SetRumbleEffectDelegate;

		// Token: 0x04000218 RID: 536
		public static ScriptingInterfaceOfIInput.SetTriggerFeedbackDelegate call_SetTriggerFeedbackDelegate;

		// Token: 0x04000219 RID: 537
		public static ScriptingInterfaceOfIInput.SetTriggerVibrationDelegate call_SetTriggerVibrationDelegate;

		// Token: 0x0400021A RID: 538
		public static ScriptingInterfaceOfIInput.SetTriggerWeaponEffectDelegate call_SetTriggerWeaponEffectDelegate;

		// Token: 0x0400021B RID: 539
		public static ScriptingInterfaceOfIInput.UpdateKeyDataDelegate call_UpdateKeyDataDelegate;

		// Token: 0x02000275 RID: 629
		// (Invoke) Token: 0x06000FE7 RID: 4071
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ClearKeysDelegate();

		// Token: 0x02000276 RID: 630
		// (Invoke) Token: 0x06000FEB RID: 4075
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetClipboardTextDelegate();

		// Token: 0x02000277 RID: 631
		// (Invoke) Token: 0x06000FEF RID: 4079
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetControllerTypeDelegate();

		// Token: 0x02000278 RID: 632
		// (Invoke) Token: 0x06000FF3 RID: 4083
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate float GetGyroXDelegate();

		// Token: 0x02000279 RID: 633
		// (Invoke) Token: 0x06000FF7 RID: 4087
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate float GetGyroYDelegate();

		// Token: 0x0200027A RID: 634
		// (Invoke) Token: 0x06000FFB RID: 4091
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate float GetGyroZDelegate();

		// Token: 0x0200027B RID: 635
		// (Invoke) Token: 0x06000FFF RID: 4095
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate Vec2 GetKeyStateDelegate(InputKey key);

		// Token: 0x0200027C RID: 636
		// (Invoke) Token: 0x06001003 RID: 4099
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate float GetMouseDeltaZDelegate();

		// Token: 0x0200027D RID: 637
		// (Invoke) Token: 0x06001007 RID: 4103
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate float GetMouseMoveXDelegate();

		// Token: 0x0200027E RID: 638
		// (Invoke) Token: 0x0600100B RID: 4107
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate float GetMouseMoveYDelegate();

		// Token: 0x0200027F RID: 639
		// (Invoke) Token: 0x0600100F RID: 4111
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate float GetMousePositionXDelegate();

		// Token: 0x02000280 RID: 640
		// (Invoke) Token: 0x06001013 RID: 4115
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate float GetMousePositionYDelegate();

		// Token: 0x02000281 RID: 641
		// (Invoke) Token: 0x06001017 RID: 4119
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate float GetMouseScrollValueDelegate();

		// Token: 0x02000282 RID: 642
		// (Invoke) Token: 0x0600101B RID: 4123
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate float GetMouseSensitivityDelegate();

		// Token: 0x02000283 RID: 643
		// (Invoke) Token: 0x0600101F RID: 4127
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetVirtualKeyCodeDelegate(InputKey key);

		// Token: 0x02000284 RID: 644
		// (Invoke) Token: 0x06001023 RID: 4131
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool IsAnyTouchActiveDelegate();

		// Token: 0x02000285 RID: 645
		// (Invoke) Token: 0x06001027 RID: 4135
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool IsControllerConnectedDelegate();

		// Token: 0x02000286 RID: 646
		// (Invoke) Token: 0x0600102B RID: 4139
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool IsKeyDownDelegate(InputKey key);

		// Token: 0x02000287 RID: 647
		// (Invoke) Token: 0x0600102F RID: 4143
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool IsKeyDownImmediateDelegate(InputKey key);

		// Token: 0x02000288 RID: 648
		// (Invoke) Token: 0x06001033 RID: 4147
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool IsKeyPressedDelegate(InputKey key);

		// Token: 0x02000289 RID: 649
		// (Invoke) Token: 0x06001037 RID: 4151
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool IsKeyReleasedDelegate(InputKey key);

		// Token: 0x0200028A RID: 650
		// (Invoke) Token: 0x0600103B RID: 4155
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool IsMouseActiveDelegate();

		// Token: 0x0200028B RID: 651
		// (Invoke) Token: 0x0600103F RID: 4159
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void PressKeyDelegate(InputKey key);

		// Token: 0x0200028C RID: 652
		// (Invoke) Token: 0x06001043 RID: 4163
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetClipboardTextDelegate(byte[] text);

		// Token: 0x0200028D RID: 653
		// (Invoke) Token: 0x06001047 RID: 4167
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetCursorFrictionValueDelegate(float frictionValue);

		// Token: 0x0200028E RID: 654
		// (Invoke) Token: 0x0600104B RID: 4171
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetCursorPositionDelegate(int x, int y);

		// Token: 0x0200028F RID: 655
		// (Invoke) Token: 0x0600104F RID: 4175
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetLightbarColorDelegate(float red, float green, float blue);

		// Token: 0x02000290 RID: 656
		// (Invoke) Token: 0x06001053 RID: 4179
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetRumbleEffectDelegate(IntPtr lowFrequencyLevels, IntPtr lowFrequencyDurations, int numLowFrequencyElements, IntPtr highFrequencyLevels, IntPtr highFrequencyDurations, int numHighFrequencyElements);

		// Token: 0x02000291 RID: 657
		// (Invoke) Token: 0x06001057 RID: 4183
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetTriggerFeedbackDelegate(byte leftTriggerPosition, byte leftTriggerStrength, byte rightTriggerPosition, byte rightTriggerStrength);

		// Token: 0x02000292 RID: 658
		// (Invoke) Token: 0x0600105B RID: 4187
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetTriggerVibrationDelegate(IntPtr leftTriggerAmplitudes, IntPtr leftTriggerFrequencies, IntPtr leftTriggerDurations, int numLeftTriggerElements, IntPtr rightTriggerAmplitudes, IntPtr rightTriggerFrequencies, IntPtr rightTriggerDurations, int numRightTriggerElements);

		// Token: 0x02000293 RID: 659
		// (Invoke) Token: 0x0600105F RID: 4191
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetTriggerWeaponEffectDelegate(byte leftStartPosition, byte leftEnd_position, byte leftStrength, byte rightStartPosition, byte rightEndPosition, byte rightStrength);

		// Token: 0x02000294 RID: 660
		// (Invoke) Token: 0x06001063 RID: 4195
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void UpdateKeyDataDelegate(ManagedArray keyData);
	}
}
