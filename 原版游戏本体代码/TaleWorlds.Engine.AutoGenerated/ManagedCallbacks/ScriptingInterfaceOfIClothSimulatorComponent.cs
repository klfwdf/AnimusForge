using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace ManagedCallbacks
{
	// Token: 0x0200000B RID: 11
	internal class ScriptingInterfaceOfIClothSimulatorComponent : IClothSimulatorComponent
	{
		// Token: 0x0600008F RID: 143 RVA: 0x0000EECF File Offset: 0x0000D0CF
		public void DisableForcedWind(UIntPtr cloth_pointer)
		{
			ScriptingInterfaceOfIClothSimulatorComponent.call_DisableForcedWindDelegate(cloth_pointer);
		}

		// Token: 0x06000090 RID: 144 RVA: 0x0000EEDC File Offset: 0x0000D0DC
		public void DisableMorphAnimation(UIntPtr cloth_pointer)
		{
			ScriptingInterfaceOfIClothSimulatorComponent.call_DisableMorphAnimationDelegate(cloth_pointer);
		}

		// Token: 0x06000091 RID: 145 RVA: 0x0000EEEC File Offset: 0x0000D0EC
		public void GetMorphAnimCenterPoints(UIntPtr cloth_pointer, Vec3[] leftPoints)
		{
			PinnedArrayData<Vec3> pinnedArrayData = new PinnedArrayData<Vec3>(leftPoints, false);
			IntPtr pointer = pinnedArrayData.Pointer;
			ScriptingInterfaceOfIClothSimulatorComponent.call_GetMorphAnimCenterPointsDelegate(cloth_pointer, pointer);
			pinnedArrayData.Dispose();
		}

		// Token: 0x06000092 RID: 146 RVA: 0x0000EF20 File Offset: 0x0000D120
		public void GetMorphAnimLeftPoints(UIntPtr cloth_pointer, Vec3[] leftPoints)
		{
			PinnedArrayData<Vec3> pinnedArrayData = new PinnedArrayData<Vec3>(leftPoints, false);
			IntPtr pointer = pinnedArrayData.Pointer;
			ScriptingInterfaceOfIClothSimulatorComponent.call_GetMorphAnimLeftPointsDelegate(cloth_pointer, pointer);
			pinnedArrayData.Dispose();
		}

		// Token: 0x06000093 RID: 147 RVA: 0x0000EF54 File Offset: 0x0000D154
		public void GetMorphAnimRightPoints(UIntPtr cloth_pointer, Vec3[] rightPoints)
		{
			PinnedArrayData<Vec3> pinnedArrayData = new PinnedArrayData<Vec3>(rightPoints, false);
			IntPtr pointer = pinnedArrayData.Pointer;
			ScriptingInterfaceOfIClothSimulatorComponent.call_GetMorphAnimRightPointsDelegate(cloth_pointer, pointer);
			pinnedArrayData.Dispose();
		}

		// Token: 0x06000094 RID: 148 RVA: 0x0000EF85 File Offset: 0x0000D185
		public int GetNumberOfMorphKeys(UIntPtr cloth_pointer)
		{
			return ScriptingInterfaceOfIClothSimulatorComponent.call_GetNumberOfMorphKeysDelegate(cloth_pointer);
		}

		// Token: 0x06000095 RID: 149 RVA: 0x0000EF92 File Offset: 0x0000D192
		public void SetForcedGustStrength(UIntPtr cloth_pointer, float gustStrength)
		{
			ScriptingInterfaceOfIClothSimulatorComponent.call_SetForcedGustStrengthDelegate(cloth_pointer, gustStrength);
		}

		// Token: 0x06000096 RID: 150 RVA: 0x0000EFA0 File Offset: 0x0000D1A0
		public void SetForcedVelocity(UIntPtr cloth_pointer, in Vec3 velocity)
		{
			ScriptingInterfaceOfIClothSimulatorComponent.call_SetForcedVelocityDelegate(cloth_pointer, velocity);
		}

		// Token: 0x06000097 RID: 151 RVA: 0x0000EFAE File Offset: 0x0000D1AE
		public void SetForcedWind(UIntPtr cloth_pointer, Vec3 windVector, bool isLocal)
		{
			ScriptingInterfaceOfIClothSimulatorComponent.call_SetForcedWindDelegate(cloth_pointer, windVector, isLocal);
		}

		// Token: 0x06000098 RID: 152 RVA: 0x0000EFBD File Offset: 0x0000D1BD
		public void SetMaxDistanceMultiplier(UIntPtr cloth_pointer, float multiplier)
		{
			ScriptingInterfaceOfIClothSimulatorComponent.call_SetMaxDistanceMultiplierDelegate(cloth_pointer, multiplier);
		}

		// Token: 0x06000099 RID: 153 RVA: 0x0000EFCB File Offset: 0x0000D1CB
		public void SetMorphAnimation(UIntPtr cloth_pointer, float morphKey)
		{
			ScriptingInterfaceOfIClothSimulatorComponent.call_SetMorphAnimationDelegate(cloth_pointer, morphKey);
		}

		// Token: 0x0600009A RID: 154 RVA: 0x0000EFD9 File Offset: 0x0000D1D9
		public void SetResetRequired(UIntPtr cloth_pointer)
		{
			ScriptingInterfaceOfIClothSimulatorComponent.call_SetResetRequiredDelegate(cloth_pointer);
		}

		// Token: 0x0600009B RID: 155 RVA: 0x0000EFE6 File Offset: 0x0000D1E6
		public void SetVectorArgument(UIntPtr cloth_pointer, float x, float y, float z, float w)
		{
			ScriptingInterfaceOfIClothSimulatorComponent.call_SetVectorArgumentDelegate(cloth_pointer, x, y, z, w);
		}

		// Token: 0x0600009E RID: 158 RVA: 0x0000F00D File Offset: 0x0000D20D
		void IClothSimulatorComponent.SetForcedVelocity(UIntPtr cloth_pointer, in Vec3 velocity)
		{
			this.SetForcedVelocity(cloth_pointer, velocity);
		}

		// Token: 0x04000026 RID: 38
		private static readonly Encoding _utf8 = Encoding.UTF8;

		// Token: 0x04000027 RID: 39
		public static ScriptingInterfaceOfIClothSimulatorComponent.DisableForcedWindDelegate call_DisableForcedWindDelegate;

		// Token: 0x04000028 RID: 40
		public static ScriptingInterfaceOfIClothSimulatorComponent.DisableMorphAnimationDelegate call_DisableMorphAnimationDelegate;

		// Token: 0x04000029 RID: 41
		public static ScriptingInterfaceOfIClothSimulatorComponent.GetMorphAnimCenterPointsDelegate call_GetMorphAnimCenterPointsDelegate;

		// Token: 0x0400002A RID: 42
		public static ScriptingInterfaceOfIClothSimulatorComponent.GetMorphAnimLeftPointsDelegate call_GetMorphAnimLeftPointsDelegate;

		// Token: 0x0400002B RID: 43
		public static ScriptingInterfaceOfIClothSimulatorComponent.GetMorphAnimRightPointsDelegate call_GetMorphAnimRightPointsDelegate;

		// Token: 0x0400002C RID: 44
		public static ScriptingInterfaceOfIClothSimulatorComponent.GetNumberOfMorphKeysDelegate call_GetNumberOfMorphKeysDelegate;

		// Token: 0x0400002D RID: 45
		public static ScriptingInterfaceOfIClothSimulatorComponent.SetForcedGustStrengthDelegate call_SetForcedGustStrengthDelegate;

		// Token: 0x0400002E RID: 46
		public static ScriptingInterfaceOfIClothSimulatorComponent.SetForcedVelocityDelegate call_SetForcedVelocityDelegate;

		// Token: 0x0400002F RID: 47
		public static ScriptingInterfaceOfIClothSimulatorComponent.SetForcedWindDelegate call_SetForcedWindDelegate;

		// Token: 0x04000030 RID: 48
		public static ScriptingInterfaceOfIClothSimulatorComponent.SetMaxDistanceMultiplierDelegate call_SetMaxDistanceMultiplierDelegate;

		// Token: 0x04000031 RID: 49
		public static ScriptingInterfaceOfIClothSimulatorComponent.SetMorphAnimationDelegate call_SetMorphAnimationDelegate;

		// Token: 0x04000032 RID: 50
		public static ScriptingInterfaceOfIClothSimulatorComponent.SetResetRequiredDelegate call_SetResetRequiredDelegate;

		// Token: 0x04000033 RID: 51
		public static ScriptingInterfaceOfIClothSimulatorComponent.SetVectorArgumentDelegate call_SetVectorArgumentDelegate;

		// Token: 0x020000AA RID: 170
		// (Invoke) Token: 0x060008BB RID: 2235
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void DisableForcedWindDelegate(UIntPtr cloth_pointer);

		// Token: 0x020000AB RID: 171
		// (Invoke) Token: 0x060008BF RID: 2239
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void DisableMorphAnimationDelegate(UIntPtr cloth_pointer);

		// Token: 0x020000AC RID: 172
		// (Invoke) Token: 0x060008C3 RID: 2243
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetMorphAnimCenterPointsDelegate(UIntPtr cloth_pointer, IntPtr leftPoints);

		// Token: 0x020000AD RID: 173
		// (Invoke) Token: 0x060008C7 RID: 2247
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetMorphAnimLeftPointsDelegate(UIntPtr cloth_pointer, IntPtr leftPoints);

		// Token: 0x020000AE RID: 174
		// (Invoke) Token: 0x060008CB RID: 2251
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetMorphAnimRightPointsDelegate(UIntPtr cloth_pointer, IntPtr rightPoints);

		// Token: 0x020000AF RID: 175
		// (Invoke) Token: 0x060008CF RID: 2255
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetNumberOfMorphKeysDelegate(UIntPtr cloth_pointer);

		// Token: 0x020000B0 RID: 176
		// (Invoke) Token: 0x060008D3 RID: 2259
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetForcedGustStrengthDelegate(UIntPtr cloth_pointer, float gustStrength);

		// Token: 0x020000B1 RID: 177
		// (Invoke) Token: 0x060008D7 RID: 2263
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetForcedVelocityDelegate(UIntPtr cloth_pointer, in Vec3 velocity);

		// Token: 0x020000B2 RID: 178
		// (Invoke) Token: 0x060008DB RID: 2267
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetForcedWindDelegate(UIntPtr cloth_pointer, Vec3 windVector, [MarshalAs(UnmanagedType.U1)] bool isLocal);

		// Token: 0x020000B3 RID: 179
		// (Invoke) Token: 0x060008DF RID: 2271
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetMaxDistanceMultiplierDelegate(UIntPtr cloth_pointer, float multiplier);

		// Token: 0x020000B4 RID: 180
		// (Invoke) Token: 0x060008E3 RID: 2275
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetMorphAnimationDelegate(UIntPtr cloth_pointer, float morphKey);

		// Token: 0x020000B5 RID: 181
		// (Invoke) Token: 0x060008E7 RID: 2279
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetResetRequiredDelegate(UIntPtr cloth_pointer);

		// Token: 0x020000B6 RID: 182
		// (Invoke) Token: 0x060008EB RID: 2283
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetVectorArgumentDelegate(UIntPtr cloth_pointer, float x, float y, float z, float w);
	}
}
