using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace ManagedCallbacks
{
	// Token: 0x02000028 RID: 40
	internal class ScriptingInterfaceOfISoundEvent : ISoundEvent
	{
		// Token: 0x060005AE RID: 1454 RVA: 0x000188E9 File Offset: 0x00016AE9
		public int CreateEvent(int fmodEventIndex, UIntPtr scene)
		{
			return ScriptingInterfaceOfISoundEvent.call_CreateEventDelegate(fmodEventIndex, scene);
		}

		// Token: 0x060005AF RID: 1455 RVA: 0x000188F8 File Offset: 0x00016AF8
		public int CreateEventFromExternalFile(string programmerSoundEventName, string filePath, UIntPtr scene, bool is3d, bool isBlocking)
		{
			byte[] array = null;
			if (programmerSoundEventName != null)
			{
				int byteCount = ScriptingInterfaceOfISoundEvent._utf8.GetByteCount(programmerSoundEventName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfISoundEvent._utf8.GetBytes(programmerSoundEventName, 0, programmerSoundEventName.Length, array, 0);
				array[byteCount] = 0;
			}
			byte[] array2 = null;
			if (filePath != null)
			{
				int byteCount2 = ScriptingInterfaceOfISoundEvent._utf8.GetByteCount(filePath);
				array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
				ScriptingInterfaceOfISoundEvent._utf8.GetBytes(filePath, 0, filePath.Length, array2, 0);
				array2[byteCount2] = 0;
			}
			return ScriptingInterfaceOfISoundEvent.call_CreateEventFromExternalFileDelegate(array, array2, scene, is3d, isBlocking);
		}

		// Token: 0x060005B0 RID: 1456 RVA: 0x0001899C File Offset: 0x00016B9C
		public int CreateEventFromSoundBuffer(string programmerSoundEventName, byte[] soundBuffer, UIntPtr scene, bool is3d, bool isBlocking)
		{
			byte[] array = null;
			if (programmerSoundEventName != null)
			{
				int byteCount = ScriptingInterfaceOfISoundEvent._utf8.GetByteCount(programmerSoundEventName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfISoundEvent._utf8.GetBytes(programmerSoundEventName, 0, programmerSoundEventName.Length, array, 0);
				array[byteCount] = 0;
			}
			PinnedArrayData<byte> pinnedArrayData = new PinnedArrayData<byte>(soundBuffer, false);
			IntPtr pointer = pinnedArrayData.Pointer;
			ManagedArray soundBuffer2 = new ManagedArray(pointer, (soundBuffer != null) ? soundBuffer.Length : 0);
			int result = ScriptingInterfaceOfISoundEvent.call_CreateEventFromSoundBufferDelegate(array, soundBuffer2, scene, is3d, isBlocking);
			pinnedArrayData.Dispose();
			return result;
		}

		// Token: 0x060005B1 RID: 1457 RVA: 0x00018A2C File Offset: 0x00016C2C
		public int CreateEventFromString(string eventName, UIntPtr scene)
		{
			byte[] array = null;
			if (eventName != null)
			{
				int byteCount = ScriptingInterfaceOfISoundEvent._utf8.GetByteCount(eventName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfISoundEvent._utf8.GetBytes(eventName, 0, eventName.Length, array, 0);
				array[byteCount] = 0;
			}
			return ScriptingInterfaceOfISoundEvent.call_CreateEventFromStringDelegate(array, scene);
		}

		// Token: 0x060005B2 RID: 1458 RVA: 0x00018A88 File Offset: 0x00016C88
		public int GetEventIdFromString(string eventName)
		{
			byte[] array = null;
			if (eventName != null)
			{
				int byteCount = ScriptingInterfaceOfISoundEvent._utf8.GetByteCount(eventName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfISoundEvent._utf8.GetBytes(eventName, 0, eventName.Length, array, 0);
				array[byteCount] = 0;
			}
			return ScriptingInterfaceOfISoundEvent.call_GetEventIdFromStringDelegate(array);
		}

		// Token: 0x060005B3 RID: 1459 RVA: 0x00018AE2 File Offset: 0x00016CE2
		public Vec3 GetEventMinMaxDistance(int eventId)
		{
			return ScriptingInterfaceOfISoundEvent.call_GetEventMinMaxDistanceDelegate(eventId);
		}

		// Token: 0x060005B4 RID: 1460 RVA: 0x00018AEF File Offset: 0x00016CEF
		public int GetTotalEventCount()
		{
			return ScriptingInterfaceOfISoundEvent.call_GetTotalEventCountDelegate();
		}

		// Token: 0x060005B5 RID: 1461 RVA: 0x00018AFB File Offset: 0x00016CFB
		public bool IsPaused(int eventId)
		{
			return ScriptingInterfaceOfISoundEvent.call_IsPausedDelegate(eventId);
		}

		// Token: 0x060005B6 RID: 1462 RVA: 0x00018B08 File Offset: 0x00016D08
		public bool IsPlaying(int eventId)
		{
			return ScriptingInterfaceOfISoundEvent.call_IsPlayingDelegate(eventId);
		}

		// Token: 0x060005B7 RID: 1463 RVA: 0x00018B15 File Offset: 0x00016D15
		public bool IsValid(int eventId)
		{
			return ScriptingInterfaceOfISoundEvent.call_IsValidDelegate(eventId);
		}

		// Token: 0x060005B8 RID: 1464 RVA: 0x00018B22 File Offset: 0x00016D22
		public void PauseEvent(int eventId)
		{
			ScriptingInterfaceOfISoundEvent.call_PauseEventDelegate(eventId);
		}

		// Token: 0x060005B9 RID: 1465 RVA: 0x00018B30 File Offset: 0x00016D30
		public void PlayExtraEvent(int soundId, string eventName)
		{
			byte[] array = null;
			if (eventName != null)
			{
				int byteCount = ScriptingInterfaceOfISoundEvent._utf8.GetByteCount(eventName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfISoundEvent._utf8.GetBytes(eventName, 0, eventName.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfISoundEvent.call_PlayExtraEventDelegate(soundId, array);
		}

		// Token: 0x060005BA RID: 1466 RVA: 0x00018B8B File Offset: 0x00016D8B
		public bool PlaySound2D(int fmodEventIndex)
		{
			return ScriptingInterfaceOfISoundEvent.call_PlaySound2DDelegate(fmodEventIndex);
		}

		// Token: 0x060005BB RID: 1467 RVA: 0x00018B98 File Offset: 0x00016D98
		public void ReleaseEvent(int eventId)
		{
			ScriptingInterfaceOfISoundEvent.call_ReleaseEventDelegate(eventId);
		}

		// Token: 0x060005BC RID: 1468 RVA: 0x00018BA5 File Offset: 0x00016DA5
		public void ResumeEvent(int eventId)
		{
			ScriptingInterfaceOfISoundEvent.call_ResumeEventDelegate(eventId);
		}

		// Token: 0x060005BD RID: 1469 RVA: 0x00018BB2 File Offset: 0x00016DB2
		public void SetEventMinMaxDistance(int fmodEventIndex, Vec3 radius)
		{
			ScriptingInterfaceOfISoundEvent.call_SetEventMinMaxDistanceDelegate(fmodEventIndex, radius);
		}

		// Token: 0x060005BE RID: 1470 RVA: 0x00018BC0 File Offset: 0x00016DC0
		public void SetEventParameterAtIndex(int soundId, int parameterIndex, float value)
		{
			ScriptingInterfaceOfISoundEvent.call_SetEventParameterAtIndexDelegate(soundId, parameterIndex, value);
		}

		// Token: 0x060005BF RID: 1471 RVA: 0x00018BD0 File Offset: 0x00016DD0
		public void SetEventParameterFromString(int eventId, string name, float value)
		{
			byte[] array = null;
			if (name != null)
			{
				int byteCount = ScriptingInterfaceOfISoundEvent._utf8.GetByteCount(name);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfISoundEvent._utf8.GetBytes(name, 0, name.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfISoundEvent.call_SetEventParameterFromStringDelegate(eventId, array, value);
		}

		// Token: 0x060005C0 RID: 1472 RVA: 0x00018C2C File Offset: 0x00016E2C
		public void SetEventPosition(int eventId, ref Vec3 position)
		{
			ScriptingInterfaceOfISoundEvent.call_SetEventPositionDelegate(eventId, ref position);
		}

		// Token: 0x060005C1 RID: 1473 RVA: 0x00018C3A File Offset: 0x00016E3A
		public void SetEventVelocity(int eventId, ref Vec3 velocity)
		{
			ScriptingInterfaceOfISoundEvent.call_SetEventVelocityDelegate(eventId, ref velocity);
		}

		// Token: 0x060005C2 RID: 1474 RVA: 0x00018C48 File Offset: 0x00016E48
		public void SetSwitch(int soundId, string switchGroupName, string newSwitchStateName)
		{
			byte[] array = null;
			if (switchGroupName != null)
			{
				int byteCount = ScriptingInterfaceOfISoundEvent._utf8.GetByteCount(switchGroupName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfISoundEvent._utf8.GetBytes(switchGroupName, 0, switchGroupName.Length, array, 0);
				array[byteCount] = 0;
			}
			byte[] array2 = null;
			if (newSwitchStateName != null)
			{
				int byteCount2 = ScriptingInterfaceOfISoundEvent._utf8.GetByteCount(newSwitchStateName);
				array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
				ScriptingInterfaceOfISoundEvent._utf8.GetBytes(newSwitchStateName, 0, newSwitchStateName.Length, array2, 0);
				array2[byteCount2] = 0;
			}
			ScriptingInterfaceOfISoundEvent.call_SetSwitchDelegate(soundId, array, array2);
		}

		// Token: 0x060005C3 RID: 1475 RVA: 0x00018CE6 File Offset: 0x00016EE6
		public bool StartEvent(int eventId)
		{
			return ScriptingInterfaceOfISoundEvent.call_StartEventDelegate(eventId);
		}

		// Token: 0x060005C4 RID: 1476 RVA: 0x00018CF3 File Offset: 0x00016EF3
		public bool StartEventInPosition(int eventId, ref Vec3 position)
		{
			return ScriptingInterfaceOfISoundEvent.call_StartEventInPositionDelegate(eventId, ref position);
		}

		// Token: 0x060005C5 RID: 1477 RVA: 0x00018D01 File Offset: 0x00016F01
		public void StopEvent(int eventId)
		{
			ScriptingInterfaceOfISoundEvent.call_StopEventDelegate(eventId);
		}

		// Token: 0x060005C6 RID: 1478 RVA: 0x00018D0E File Offset: 0x00016F0E
		public void TriggerCue(int eventId)
		{
			ScriptingInterfaceOfISoundEvent.call_TriggerCueDelegate(eventId);
		}

		// Token: 0x04000503 RID: 1283
		private static readonly Encoding _utf8 = Encoding.UTF8;

		// Token: 0x04000504 RID: 1284
		public static ScriptingInterfaceOfISoundEvent.CreateEventDelegate call_CreateEventDelegate;

		// Token: 0x04000505 RID: 1285
		public static ScriptingInterfaceOfISoundEvent.CreateEventFromExternalFileDelegate call_CreateEventFromExternalFileDelegate;

		// Token: 0x04000506 RID: 1286
		public static ScriptingInterfaceOfISoundEvent.CreateEventFromSoundBufferDelegate call_CreateEventFromSoundBufferDelegate;

		// Token: 0x04000507 RID: 1287
		public static ScriptingInterfaceOfISoundEvent.CreateEventFromStringDelegate call_CreateEventFromStringDelegate;

		// Token: 0x04000508 RID: 1288
		public static ScriptingInterfaceOfISoundEvent.GetEventIdFromStringDelegate call_GetEventIdFromStringDelegate;

		// Token: 0x04000509 RID: 1289
		public static ScriptingInterfaceOfISoundEvent.GetEventMinMaxDistanceDelegate call_GetEventMinMaxDistanceDelegate;

		// Token: 0x0400050A RID: 1290
		public static ScriptingInterfaceOfISoundEvent.GetTotalEventCountDelegate call_GetTotalEventCountDelegate;

		// Token: 0x0400050B RID: 1291
		public static ScriptingInterfaceOfISoundEvent.IsPausedDelegate call_IsPausedDelegate;

		// Token: 0x0400050C RID: 1292
		public static ScriptingInterfaceOfISoundEvent.IsPlayingDelegate call_IsPlayingDelegate;

		// Token: 0x0400050D RID: 1293
		public static ScriptingInterfaceOfISoundEvent.IsValidDelegate call_IsValidDelegate;

		// Token: 0x0400050E RID: 1294
		public static ScriptingInterfaceOfISoundEvent.PauseEventDelegate call_PauseEventDelegate;

		// Token: 0x0400050F RID: 1295
		public static ScriptingInterfaceOfISoundEvent.PlayExtraEventDelegate call_PlayExtraEventDelegate;

		// Token: 0x04000510 RID: 1296
		public static ScriptingInterfaceOfISoundEvent.PlaySound2DDelegate call_PlaySound2DDelegate;

		// Token: 0x04000511 RID: 1297
		public static ScriptingInterfaceOfISoundEvent.ReleaseEventDelegate call_ReleaseEventDelegate;

		// Token: 0x04000512 RID: 1298
		public static ScriptingInterfaceOfISoundEvent.ResumeEventDelegate call_ResumeEventDelegate;

		// Token: 0x04000513 RID: 1299
		public static ScriptingInterfaceOfISoundEvent.SetEventMinMaxDistanceDelegate call_SetEventMinMaxDistanceDelegate;

		// Token: 0x04000514 RID: 1300
		public static ScriptingInterfaceOfISoundEvent.SetEventParameterAtIndexDelegate call_SetEventParameterAtIndexDelegate;

		// Token: 0x04000515 RID: 1301
		public static ScriptingInterfaceOfISoundEvent.SetEventParameterFromStringDelegate call_SetEventParameterFromStringDelegate;

		// Token: 0x04000516 RID: 1302
		public static ScriptingInterfaceOfISoundEvent.SetEventPositionDelegate call_SetEventPositionDelegate;

		// Token: 0x04000517 RID: 1303
		public static ScriptingInterfaceOfISoundEvent.SetEventVelocityDelegate call_SetEventVelocityDelegate;

		// Token: 0x04000518 RID: 1304
		public static ScriptingInterfaceOfISoundEvent.SetSwitchDelegate call_SetSwitchDelegate;

		// Token: 0x04000519 RID: 1305
		public static ScriptingInterfaceOfISoundEvent.StartEventDelegate call_StartEventDelegate;

		// Token: 0x0400051A RID: 1306
		public static ScriptingInterfaceOfISoundEvent.StartEventInPositionDelegate call_StartEventInPositionDelegate;

		// Token: 0x0400051B RID: 1307
		public static ScriptingInterfaceOfISoundEvent.StopEventDelegate call_StopEventDelegate;

		// Token: 0x0400051C RID: 1308
		public static ScriptingInterfaceOfISoundEvent.TriggerCueDelegate call_TriggerCueDelegate;

		// Token: 0x0200056A RID: 1386
		// (Invoke) Token: 0x06001BBB RID: 7099
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int CreateEventDelegate(int fmodEventIndex, UIntPtr scene);

		// Token: 0x0200056B RID: 1387
		// (Invoke) Token: 0x06001BBF RID: 7103
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int CreateEventFromExternalFileDelegate(byte[] programmerSoundEventName, byte[] filePath, UIntPtr scene, [MarshalAs(UnmanagedType.U1)] bool is3d, [MarshalAs(UnmanagedType.U1)] bool isBlocking);

		// Token: 0x0200056C RID: 1388
		// (Invoke) Token: 0x06001BC3 RID: 7107
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int CreateEventFromSoundBufferDelegate(byte[] programmerSoundEventName, ManagedArray soundBuffer, UIntPtr scene, [MarshalAs(UnmanagedType.U1)] bool is3d, [MarshalAs(UnmanagedType.U1)] bool isBlocking);

		// Token: 0x0200056D RID: 1389
		// (Invoke) Token: 0x06001BC7 RID: 7111
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int CreateEventFromStringDelegate(byte[] eventName, UIntPtr scene);

		// Token: 0x0200056E RID: 1390
		// (Invoke) Token: 0x06001BCB RID: 7115
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetEventIdFromStringDelegate(byte[] eventName);

		// Token: 0x0200056F RID: 1391
		// (Invoke) Token: 0x06001BCF RID: 7119
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate Vec3 GetEventMinMaxDistanceDelegate(int eventId);

		// Token: 0x02000570 RID: 1392
		// (Invoke) Token: 0x06001BD3 RID: 7123
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetTotalEventCountDelegate();

		// Token: 0x02000571 RID: 1393
		// (Invoke) Token: 0x06001BD7 RID: 7127
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool IsPausedDelegate(int eventId);

		// Token: 0x02000572 RID: 1394
		// (Invoke) Token: 0x06001BDB RID: 7131
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool IsPlayingDelegate(int eventId);

		// Token: 0x02000573 RID: 1395
		// (Invoke) Token: 0x06001BDF RID: 7135
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool IsValidDelegate(int eventId);

		// Token: 0x02000574 RID: 1396
		// (Invoke) Token: 0x06001BE3 RID: 7139
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void PauseEventDelegate(int eventId);

		// Token: 0x02000575 RID: 1397
		// (Invoke) Token: 0x06001BE7 RID: 7143
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void PlayExtraEventDelegate(int soundId, byte[] eventName);

		// Token: 0x02000576 RID: 1398
		// (Invoke) Token: 0x06001BEB RID: 7147
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool PlaySound2DDelegate(int fmodEventIndex);

		// Token: 0x02000577 RID: 1399
		// (Invoke) Token: 0x06001BEF RID: 7151
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ReleaseEventDelegate(int eventId);

		// Token: 0x02000578 RID: 1400
		// (Invoke) Token: 0x06001BF3 RID: 7155
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ResumeEventDelegate(int eventId);

		// Token: 0x02000579 RID: 1401
		// (Invoke) Token: 0x06001BF7 RID: 7159
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetEventMinMaxDistanceDelegate(int fmodEventIndex, Vec3 radius);

		// Token: 0x0200057A RID: 1402
		// (Invoke) Token: 0x06001BFB RID: 7163
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetEventParameterAtIndexDelegate(int soundId, int parameterIndex, float value);

		// Token: 0x0200057B RID: 1403
		// (Invoke) Token: 0x06001BFF RID: 7167
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetEventParameterFromStringDelegate(int eventId, byte[] name, float value);

		// Token: 0x0200057C RID: 1404
		// (Invoke) Token: 0x06001C03 RID: 7171
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetEventPositionDelegate(int eventId, ref Vec3 position);

		// Token: 0x0200057D RID: 1405
		// (Invoke) Token: 0x06001C07 RID: 7175
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetEventVelocityDelegate(int eventId, ref Vec3 velocity);

		// Token: 0x0200057E RID: 1406
		// (Invoke) Token: 0x06001C0B RID: 7179
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetSwitchDelegate(int soundId, byte[] switchGroupName, byte[] newSwitchStateName);

		// Token: 0x0200057F RID: 1407
		// (Invoke) Token: 0x06001C0F RID: 7183
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool StartEventDelegate(int eventId);

		// Token: 0x02000580 RID: 1408
		// (Invoke) Token: 0x06001C13 RID: 7187
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool StartEventInPositionDelegate(int eventId, ref Vec3 position);

		// Token: 0x02000581 RID: 1409
		// (Invoke) Token: 0x06001C17 RID: 7191
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void StopEventDelegate(int eventId);

		// Token: 0x02000582 RID: 1410
		// (Invoke) Token: 0x06001C1B RID: 7195
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void TriggerCueDelegate(int eventId);
	}
}
