using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace ManagedCallbacks
{
	// Token: 0x0200001E RID: 30
	internal class ScriptingInterfaceOfIParticleSystem : IParticleSystem
	{
		// Token: 0x060003B4 RID: 948 RVA: 0x00014C00 File Offset: 0x00012E00
		public ParticleSystem CreateParticleSystemAttachedToBone(int runtimeId, UIntPtr skeletonPtr, sbyte boneIndex, ref MatrixFrame boneLocalFrame)
		{
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfIParticleSystem.call_CreateParticleSystemAttachedToBoneDelegate(runtimeId, skeletonPtr, boneIndex, ref boneLocalFrame);
			ParticleSystem result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new ParticleSystem(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x060003B5 RID: 949 RVA: 0x00014C50 File Offset: 0x00012E50
		public ParticleSystem CreateParticleSystemAttachedToEntity(int runtimeId, UIntPtr entityPtr, ref MatrixFrame boneLocalFrame)
		{
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfIParticleSystem.call_CreateParticleSystemAttachedToEntityDelegate(runtimeId, entityPtr, ref boneLocalFrame);
			ParticleSystem result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new ParticleSystem(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x060003B6 RID: 950 RVA: 0x00014C9C File Offset: 0x00012E9C
		public void GetLocalFrame(UIntPtr pointer, ref MatrixFrame frame)
		{
			ScriptingInterfaceOfIParticleSystem.call_GetLocalFrameDelegate(pointer, ref frame);
		}

		// Token: 0x060003B7 RID: 951 RVA: 0x00014CAC File Offset: 0x00012EAC
		public int GetRuntimeIdByName(string particleSystemName)
		{
			byte[] array = null;
			if (particleSystemName != null)
			{
				int byteCount = ScriptingInterfaceOfIParticleSystem._utf8.GetByteCount(particleSystemName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIParticleSystem._utf8.GetBytes(particleSystemName, 0, particleSystemName.Length, array, 0);
				array[byteCount] = 0;
			}
			return ScriptingInterfaceOfIParticleSystem.call_GetRuntimeIdByNameDelegate(array);
		}

		// Token: 0x060003B8 RID: 952 RVA: 0x00014D06 File Offset: 0x00012F06
		public bool HasAliveParticles(UIntPtr pointer)
		{
			return ScriptingInterfaceOfIParticleSystem.call_HasAliveParticlesDelegate(pointer);
		}

		// Token: 0x060003B9 RID: 953 RVA: 0x00014D13 File Offset: 0x00012F13
		public void Restart(UIntPtr psysPointer)
		{
			ScriptingInterfaceOfIParticleSystem.call_RestartDelegate(psysPointer);
		}

		// Token: 0x060003BA RID: 954 RVA: 0x00014D20 File Offset: 0x00012F20
		public void SetDontRemoveFromEntity(UIntPtr pointer, bool value)
		{
			ScriptingInterfaceOfIParticleSystem.call_SetDontRemoveFromEntityDelegate(pointer, value);
		}

		// Token: 0x060003BB RID: 955 RVA: 0x00014D2E File Offset: 0x00012F2E
		public void SetEnable(UIntPtr psysPointer, bool enable)
		{
			ScriptingInterfaceOfIParticleSystem.call_SetEnableDelegate(psysPointer, enable);
		}

		// Token: 0x060003BC RID: 956 RVA: 0x00014D3C File Offset: 0x00012F3C
		public void SetLocalFrame(UIntPtr pointer, in MatrixFrame newFrame)
		{
			ScriptingInterfaceOfIParticleSystem.call_SetLocalFrameDelegate(pointer, newFrame);
		}

		// Token: 0x060003BD RID: 957 RVA: 0x00014D4C File Offset: 0x00012F4C
		public void SetParticleEffectByName(UIntPtr pointer, string effectName)
		{
			byte[] array = null;
			if (effectName != null)
			{
				int byteCount = ScriptingInterfaceOfIParticleSystem._utf8.GetByteCount(effectName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIParticleSystem._utf8.GetBytes(effectName, 0, effectName.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIParticleSystem.call_SetParticleEffectByNameDelegate(pointer, array);
		}

		// Token: 0x060003BE RID: 958 RVA: 0x00014DA7 File Offset: 0x00012FA7
		public void SetPreviousGlobalFrame(UIntPtr pointer, in MatrixFrame newFrame)
		{
			ScriptingInterfaceOfIParticleSystem.call_SetPreviousGlobalFrameDelegate(pointer, newFrame);
		}

		// Token: 0x060003BF RID: 959 RVA: 0x00014DB5 File Offset: 0x00012FB5
		public void SetRuntimeEmissionRateMultiplier(UIntPtr pointer, float multiplier)
		{
			ScriptingInterfaceOfIParticleSystem.call_SetRuntimeEmissionRateMultiplierDelegate(pointer, multiplier);
		}

		// Token: 0x060003C2 RID: 962 RVA: 0x00014DD7 File Offset: 0x00012FD7
		void IParticleSystem.SetLocalFrame(UIntPtr pointer, in MatrixFrame newFrame)
		{
			this.SetLocalFrame(pointer, newFrame);
		}

		// Token: 0x060003C3 RID: 963 RVA: 0x00014DE1 File Offset: 0x00012FE1
		void IParticleSystem.SetPreviousGlobalFrame(UIntPtr pointer, in MatrixFrame newFrame)
		{
			this.SetPreviousGlobalFrame(pointer, newFrame);
		}

		// Token: 0x04000324 RID: 804
		private static readonly Encoding _utf8 = Encoding.UTF8;

		// Token: 0x04000325 RID: 805
		public static ScriptingInterfaceOfIParticleSystem.CreateParticleSystemAttachedToBoneDelegate call_CreateParticleSystemAttachedToBoneDelegate;

		// Token: 0x04000326 RID: 806
		public static ScriptingInterfaceOfIParticleSystem.CreateParticleSystemAttachedToEntityDelegate call_CreateParticleSystemAttachedToEntityDelegate;

		// Token: 0x04000327 RID: 807
		public static ScriptingInterfaceOfIParticleSystem.GetLocalFrameDelegate call_GetLocalFrameDelegate;

		// Token: 0x04000328 RID: 808
		public static ScriptingInterfaceOfIParticleSystem.GetRuntimeIdByNameDelegate call_GetRuntimeIdByNameDelegate;

		// Token: 0x04000329 RID: 809
		public static ScriptingInterfaceOfIParticleSystem.HasAliveParticlesDelegate call_HasAliveParticlesDelegate;

		// Token: 0x0400032A RID: 810
		public static ScriptingInterfaceOfIParticleSystem.RestartDelegate call_RestartDelegate;

		// Token: 0x0400032B RID: 811
		public static ScriptingInterfaceOfIParticleSystem.SetDontRemoveFromEntityDelegate call_SetDontRemoveFromEntityDelegate;

		// Token: 0x0400032C RID: 812
		public static ScriptingInterfaceOfIParticleSystem.SetEnableDelegate call_SetEnableDelegate;

		// Token: 0x0400032D RID: 813
		public static ScriptingInterfaceOfIParticleSystem.SetLocalFrameDelegate call_SetLocalFrameDelegate;

		// Token: 0x0400032E RID: 814
		public static ScriptingInterfaceOfIParticleSystem.SetParticleEffectByNameDelegate call_SetParticleEffectByNameDelegate;

		// Token: 0x0400032F RID: 815
		public static ScriptingInterfaceOfIParticleSystem.SetPreviousGlobalFrameDelegate call_SetPreviousGlobalFrameDelegate;

		// Token: 0x04000330 RID: 816
		public static ScriptingInterfaceOfIParticleSystem.SetRuntimeEmissionRateMultiplierDelegate call_SetRuntimeEmissionRateMultiplierDelegate;

		// Token: 0x02000395 RID: 917
		// (Invoke) Token: 0x06001467 RID: 5223
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer CreateParticleSystemAttachedToBoneDelegate(int runtimeId, UIntPtr skeletonPtr, sbyte boneIndex, ref MatrixFrame boneLocalFrame);

		// Token: 0x02000396 RID: 918
		// (Invoke) Token: 0x0600146B RID: 5227
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer CreateParticleSystemAttachedToEntityDelegate(int runtimeId, UIntPtr entityPtr, ref MatrixFrame boneLocalFrame);

		// Token: 0x02000397 RID: 919
		// (Invoke) Token: 0x0600146F RID: 5231
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetLocalFrameDelegate(UIntPtr pointer, ref MatrixFrame frame);

		// Token: 0x02000398 RID: 920
		// (Invoke) Token: 0x06001473 RID: 5235
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetRuntimeIdByNameDelegate(byte[] particleSystemName);

		// Token: 0x02000399 RID: 921
		// (Invoke) Token: 0x06001477 RID: 5239
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool HasAliveParticlesDelegate(UIntPtr pointer);

		// Token: 0x0200039A RID: 922
		// (Invoke) Token: 0x0600147B RID: 5243
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void RestartDelegate(UIntPtr psysPointer);

		// Token: 0x0200039B RID: 923
		// (Invoke) Token: 0x0600147F RID: 5247
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetDontRemoveFromEntityDelegate(UIntPtr pointer, [MarshalAs(UnmanagedType.U1)] bool value);

		// Token: 0x0200039C RID: 924
		// (Invoke) Token: 0x06001483 RID: 5251
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetEnableDelegate(UIntPtr psysPointer, [MarshalAs(UnmanagedType.U1)] bool enable);

		// Token: 0x0200039D RID: 925
		// (Invoke) Token: 0x06001487 RID: 5255
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetLocalFrameDelegate(UIntPtr pointer, in MatrixFrame newFrame);

		// Token: 0x0200039E RID: 926
		// (Invoke) Token: 0x0600148B RID: 5259
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetParticleEffectByNameDelegate(UIntPtr pointer, byte[] effectName);

		// Token: 0x0200039F RID: 927
		// (Invoke) Token: 0x0600148F RID: 5263
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetPreviousGlobalFrameDelegate(UIntPtr pointer, in MatrixFrame newFrame);

		// Token: 0x020003A0 RID: 928
		// (Invoke) Token: 0x06001493 RID: 5267
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetRuntimeEmissionRateMultiplierDelegate(UIntPtr pointer, float multiplier);
	}
}
