using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace ManagedCallbacks
{
	// Token: 0x0200000F RID: 15
	internal class ScriptingInterfaceOfIDecal : IDecal
	{
		// Token: 0x0600010F RID: 271 RVA: 0x0000FCB2 File Offset: 0x0000DEB2
		public void CheckAndRegisterToDecalSet(UIntPtr pointer)
		{
			ScriptingInterfaceOfIDecal.call_CheckAndRegisterToDecalSetDelegate(pointer);
		}

		// Token: 0x06000110 RID: 272 RVA: 0x0000FCC0 File Offset: 0x0000DEC0
		public Decal CreateCopy(UIntPtr pointer)
		{
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfIDecal.call_CreateCopyDelegate(pointer);
			Decal result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new Decal(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x06000111 RID: 273 RVA: 0x0000FD0C File Offset: 0x0000DF0C
		public Decal CreateDecal(string name)
		{
			byte[] array = null;
			if (name != null)
			{
				int byteCount = ScriptingInterfaceOfIDecal._utf8.GetByteCount(name);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIDecal._utf8.GetBytes(name, 0, name.Length, array, 0);
				array[byteCount] = 0;
			}
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfIDecal.call_CreateDecalDelegate(array);
			Decal result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new Decal(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x06000112 RID: 274 RVA: 0x0000FD98 File Offset: 0x0000DF98
		public uint GetFactor1(UIntPtr decalPointer)
		{
			return ScriptingInterfaceOfIDecal.call_GetFactor1Delegate(decalPointer);
		}

		// Token: 0x06000113 RID: 275 RVA: 0x0000FDA5 File Offset: 0x0000DFA5
		public void GetFrame(UIntPtr decalPointer, ref MatrixFrame outFrame)
		{
			ScriptingInterfaceOfIDecal.call_GetFrameDelegate(decalPointer, ref outFrame);
		}

		// Token: 0x06000114 RID: 276 RVA: 0x0000FDB4 File Offset: 0x0000DFB4
		public Material GetMaterial(UIntPtr decalPointer)
		{
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfIDecal.call_GetMaterialDelegate(decalPointer);
			Material result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new Material(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x06000115 RID: 277 RVA: 0x0000FDFE File Offset: 0x0000DFFE
		public void OverrideRoadBoundaryP0(UIntPtr decalPointer, in Vec2 data)
		{
			ScriptingInterfaceOfIDecal.call_OverrideRoadBoundaryP0Delegate(decalPointer, data);
		}

		// Token: 0x06000116 RID: 278 RVA: 0x0000FE0C File Offset: 0x0000E00C
		public void OverrideRoadBoundaryP1(UIntPtr decalPointer, in Vec2 data)
		{
			ScriptingInterfaceOfIDecal.call_OverrideRoadBoundaryP1Delegate(decalPointer, data);
		}

		// Token: 0x06000117 RID: 279 RVA: 0x0000FE1A File Offset: 0x0000E01A
		public void SetAlpha(UIntPtr decalPointer, float alpha)
		{
			ScriptingInterfaceOfIDecal.call_SetAlphaDelegate(decalPointer, alpha);
		}

		// Token: 0x06000118 RID: 280 RVA: 0x0000FE28 File Offset: 0x0000E028
		public void SetFactor1(UIntPtr decalPointer, uint factorColor1)
		{
			ScriptingInterfaceOfIDecal.call_SetFactor1Delegate(decalPointer, factorColor1);
		}

		// Token: 0x06000119 RID: 281 RVA: 0x0000FE36 File Offset: 0x0000E036
		public void SetFactor1Linear(UIntPtr decalPointer, uint linearFactorColor1)
		{
			ScriptingInterfaceOfIDecal.call_SetFactor1LinearDelegate(decalPointer, linearFactorColor1);
		}

		// Token: 0x0600011A RID: 282 RVA: 0x0000FE44 File Offset: 0x0000E044
		public void SetFrame(UIntPtr decalPointer, ref MatrixFrame decalFrame)
		{
			ScriptingInterfaceOfIDecal.call_SetFrameDelegate(decalPointer, ref decalFrame);
		}

		// Token: 0x0600011B RID: 283 RVA: 0x0000FE52 File Offset: 0x0000E052
		public void SetIsVisible(UIntPtr pointer, bool value)
		{
			ScriptingInterfaceOfIDecal.call_SetIsVisibleDelegate(pointer, value);
		}

		// Token: 0x0600011C RID: 284 RVA: 0x0000FE60 File Offset: 0x0000E060
		public void SetMaterial(UIntPtr decalPointer, UIntPtr materialPointer)
		{
			ScriptingInterfaceOfIDecal.call_SetMaterialDelegate(decalPointer, materialPointer);
		}

		// Token: 0x0600011D RID: 285 RVA: 0x0000FE6E File Offset: 0x0000E06E
		public void SetVectorArgument(UIntPtr decalPointer, float vectorArgument0, float vectorArgument1, float vectorArgument2, float vectorArgument3)
		{
			ScriptingInterfaceOfIDecal.call_SetVectorArgumentDelegate(decalPointer, vectorArgument0, vectorArgument1, vectorArgument2, vectorArgument3);
		}

		// Token: 0x0600011E RID: 286 RVA: 0x0000FE81 File Offset: 0x0000E081
		public void SetVectorArgument2(UIntPtr decalPointer, float vectorArgument0, float vectorArgument1, float vectorArgument2, float vectorArgument3)
		{
			ScriptingInterfaceOfIDecal.call_SetVectorArgument2Delegate(decalPointer, vectorArgument0, vectorArgument1, vectorArgument2, vectorArgument3);
		}

		// Token: 0x06000121 RID: 289 RVA: 0x0000FEA8 File Offset: 0x0000E0A8
		void IDecal.OverrideRoadBoundaryP0(UIntPtr decalPointer, in Vec2 data)
		{
			this.OverrideRoadBoundaryP0(decalPointer, data);
		}

		// Token: 0x06000122 RID: 290 RVA: 0x0000FEB2 File Offset: 0x0000E0B2
		void IDecal.OverrideRoadBoundaryP1(UIntPtr decalPointer, in Vec2 data)
		{
			this.OverrideRoadBoundaryP1(decalPointer, data);
		}

		// Token: 0x040000A1 RID: 161
		private static readonly Encoding _utf8 = Encoding.UTF8;

		// Token: 0x040000A2 RID: 162
		public static ScriptingInterfaceOfIDecal.CheckAndRegisterToDecalSetDelegate call_CheckAndRegisterToDecalSetDelegate;

		// Token: 0x040000A3 RID: 163
		public static ScriptingInterfaceOfIDecal.CreateCopyDelegate call_CreateCopyDelegate;

		// Token: 0x040000A4 RID: 164
		public static ScriptingInterfaceOfIDecal.CreateDecalDelegate call_CreateDecalDelegate;

		// Token: 0x040000A5 RID: 165
		public static ScriptingInterfaceOfIDecal.GetFactor1Delegate call_GetFactor1Delegate;

		// Token: 0x040000A6 RID: 166
		public static ScriptingInterfaceOfIDecal.GetFrameDelegate call_GetFrameDelegate;

		// Token: 0x040000A7 RID: 167
		public static ScriptingInterfaceOfIDecal.GetMaterialDelegate call_GetMaterialDelegate;

		// Token: 0x040000A8 RID: 168
		public static ScriptingInterfaceOfIDecal.OverrideRoadBoundaryP0Delegate call_OverrideRoadBoundaryP0Delegate;

		// Token: 0x040000A9 RID: 169
		public static ScriptingInterfaceOfIDecal.OverrideRoadBoundaryP1Delegate call_OverrideRoadBoundaryP1Delegate;

		// Token: 0x040000AA RID: 170
		public static ScriptingInterfaceOfIDecal.SetAlphaDelegate call_SetAlphaDelegate;

		// Token: 0x040000AB RID: 171
		public static ScriptingInterfaceOfIDecal.SetFactor1Delegate call_SetFactor1Delegate;

		// Token: 0x040000AC RID: 172
		public static ScriptingInterfaceOfIDecal.SetFactor1LinearDelegate call_SetFactor1LinearDelegate;

		// Token: 0x040000AD RID: 173
		public static ScriptingInterfaceOfIDecal.SetFrameDelegate call_SetFrameDelegate;

		// Token: 0x040000AE RID: 174
		public static ScriptingInterfaceOfIDecal.SetIsVisibleDelegate call_SetIsVisibleDelegate;

		// Token: 0x040000AF RID: 175
		public static ScriptingInterfaceOfIDecal.SetMaterialDelegate call_SetMaterialDelegate;

		// Token: 0x040000B0 RID: 176
		public static ScriptingInterfaceOfIDecal.SetVectorArgumentDelegate call_SetVectorArgumentDelegate;

		// Token: 0x040000B1 RID: 177
		public static ScriptingInterfaceOfIDecal.SetVectorArgument2Delegate call_SetVectorArgument2Delegate;

		// Token: 0x02000121 RID: 289
		// (Invoke) Token: 0x06000A97 RID: 2711
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void CheckAndRegisterToDecalSetDelegate(UIntPtr pointer);

		// Token: 0x02000122 RID: 290
		// (Invoke) Token: 0x06000A9B RID: 2715
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer CreateCopyDelegate(UIntPtr pointer);

		// Token: 0x02000123 RID: 291
		// (Invoke) Token: 0x06000A9F RID: 2719
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer CreateDecalDelegate(byte[] name);

		// Token: 0x02000124 RID: 292
		// (Invoke) Token: 0x06000AA3 RID: 2723
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate uint GetFactor1Delegate(UIntPtr decalPointer);

		// Token: 0x02000125 RID: 293
		// (Invoke) Token: 0x06000AA7 RID: 2727
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetFrameDelegate(UIntPtr decalPointer, ref MatrixFrame outFrame);

		// Token: 0x02000126 RID: 294
		// (Invoke) Token: 0x06000AAB RID: 2731
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer GetMaterialDelegate(UIntPtr decalPointer);

		// Token: 0x02000127 RID: 295
		// (Invoke) Token: 0x06000AAF RID: 2735
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void OverrideRoadBoundaryP0Delegate(UIntPtr decalPointer, in Vec2 data);

		// Token: 0x02000128 RID: 296
		// (Invoke) Token: 0x06000AB3 RID: 2739
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void OverrideRoadBoundaryP1Delegate(UIntPtr decalPointer, in Vec2 data);

		// Token: 0x02000129 RID: 297
		// (Invoke) Token: 0x06000AB7 RID: 2743
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetAlphaDelegate(UIntPtr decalPointer, float alpha);

		// Token: 0x0200012A RID: 298
		// (Invoke) Token: 0x06000ABB RID: 2747
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetFactor1Delegate(UIntPtr decalPointer, uint factorColor1);

		// Token: 0x0200012B RID: 299
		// (Invoke) Token: 0x06000ABF RID: 2751
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetFactor1LinearDelegate(UIntPtr decalPointer, uint linearFactorColor1);

		// Token: 0x0200012C RID: 300
		// (Invoke) Token: 0x06000AC3 RID: 2755
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetFrameDelegate(UIntPtr decalPointer, ref MatrixFrame decalFrame);

		// Token: 0x0200012D RID: 301
		// (Invoke) Token: 0x06000AC7 RID: 2759
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetIsVisibleDelegate(UIntPtr pointer, [MarshalAs(UnmanagedType.U1)] bool value);

		// Token: 0x0200012E RID: 302
		// (Invoke) Token: 0x06000ACB RID: 2763
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetMaterialDelegate(UIntPtr decalPointer, UIntPtr materialPointer);

		// Token: 0x0200012F RID: 303
		// (Invoke) Token: 0x06000ACF RID: 2767
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetVectorArgumentDelegate(UIntPtr decalPointer, float vectorArgument0, float vectorArgument1, float vectorArgument2, float vectorArgument3);

		// Token: 0x02000130 RID: 304
		// (Invoke) Token: 0x06000AD3 RID: 2771
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetVectorArgument2Delegate(UIntPtr decalPointer, float vectorArgument0, float vectorArgument1, float vectorArgument2, float vectorArgument3);
	}
}
