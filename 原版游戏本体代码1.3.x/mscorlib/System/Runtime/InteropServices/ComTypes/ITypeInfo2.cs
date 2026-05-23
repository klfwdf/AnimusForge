using System;

namespace System.Runtime.InteropServices.ComTypes
{
	// Token: 0x02000A54 RID: 2644
	[Guid("00020412-0000-0000-C000-000000000046")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[__DynamicallyInvokable]
	[ComImport]
	public interface ITypeInfo2 : ITypeInfo
	{
		// Token: 0x0600669C RID: 26268
		void GetTypeAttr(out IntPtr ppTypeAttr);

		// Token: 0x0600669D RID: 26269
		[__DynamicallyInvokable]
		void GetTypeComp(out ITypeComp ppTComp);

		// Token: 0x0600669E RID: 26270
		void GetFuncDesc(int index, out IntPtr ppFuncDesc);

		// Token: 0x0600669F RID: 26271
		void GetVarDesc(int index, out IntPtr ppVarDesc);

		// Token: 0x060066A0 RID: 26272
		[__DynamicallyInvokable]
		void GetNames(int memid, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] [Out] string[] rgBstrNames, int cMaxNames, out int pcNames);

		// Token: 0x060066A1 RID: 26273
		[__DynamicallyInvokable]
		void GetRefTypeOfImplType(int index, out int href);

		// Token: 0x060066A2 RID: 26274
		[__DynamicallyInvokable]
		void GetImplTypeFlags(int index, out IMPLTYPEFLAGS pImplTypeFlags);

		// Token: 0x060066A3 RID: 26275
		[__DynamicallyInvokable]
		void GetIDsOfNames([MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 1)] [In] string[] rgszNames, int cNames, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] [Out] int[] pMemId);

		// Token: 0x060066A4 RID: 26276
		void Invoke([MarshalAs(UnmanagedType.IUnknown)] object pvInstance, int memid, short wFlags, ref DISPPARAMS pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, out int puArgErr);

		// Token: 0x060066A5 RID: 26277
		[__DynamicallyInvokable]
		void GetDocumentation(int index, out string strName, out string strDocString, out int dwHelpContext, out string strHelpFile);

		// Token: 0x060066A6 RID: 26278
		void GetDllEntry(int memid, INVOKEKIND invKind, IntPtr pBstrDllName, IntPtr pBstrName, IntPtr pwOrdinal);

		// Token: 0x060066A7 RID: 26279
		[__DynamicallyInvokable]
		void GetRefTypeInfo(int hRef, out ITypeInfo ppTI);

		// Token: 0x060066A8 RID: 26280
		void AddressOfMember(int memid, INVOKEKIND invKind, out IntPtr ppv);

		// Token: 0x060066A9 RID: 26281
		[__DynamicallyInvokable]
		void CreateInstance([MarshalAs(UnmanagedType.IUnknown)] object pUnkOuter, [In] ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppvObj);

		// Token: 0x060066AA RID: 26282
		[__DynamicallyInvokable]
		void GetMops(int memid, out string pBstrMops);

		// Token: 0x060066AB RID: 26283
		[__DynamicallyInvokable]
		void GetContainingTypeLib(out ITypeLib ppTLB, out int pIndex);

		// Token: 0x060066AC RID: 26284
		[PreserveSig]
		void ReleaseTypeAttr(IntPtr pTypeAttr);

		// Token: 0x060066AD RID: 26285
		[PreserveSig]
		void ReleaseFuncDesc(IntPtr pFuncDesc);

		// Token: 0x060066AE RID: 26286
		[PreserveSig]
		void ReleaseVarDesc(IntPtr pVarDesc);

		// Token: 0x060066AF RID: 26287
		[__DynamicallyInvokable]
		void GetTypeKind(out TYPEKIND pTypeKind);

		// Token: 0x060066B0 RID: 26288
		[__DynamicallyInvokable]
		void GetTypeFlags(out int pTypeFlags);

		// Token: 0x060066B1 RID: 26289
		[__DynamicallyInvokable]
		void GetFuncIndexOfMemId(int memid, INVOKEKIND invKind, out int pFuncIndex);

		// Token: 0x060066B2 RID: 26290
		[__DynamicallyInvokable]
		void GetVarIndexOfMemId(int memid, out int pVarIndex);

		// Token: 0x060066B3 RID: 26291
		[__DynamicallyInvokable]
		void GetCustData(ref Guid guid, out object pVarVal);

		// Token: 0x060066B4 RID: 26292
		[__DynamicallyInvokable]
		void GetFuncCustData(int index, ref Guid guid, out object pVarVal);

		// Token: 0x060066B5 RID: 26293
		[__DynamicallyInvokable]
		void GetParamCustData(int indexFunc, int indexParam, ref Guid guid, out object pVarVal);

		// Token: 0x060066B6 RID: 26294
		[__DynamicallyInvokable]
		void GetVarCustData(int index, ref Guid guid, out object pVarVal);

		// Token: 0x060066B7 RID: 26295
		[__DynamicallyInvokable]
		void GetImplTypeCustData(int index, ref Guid guid, out object pVarVal);

		// Token: 0x060066B8 RID: 26296
		[LCIDConversion(1)]
		[__DynamicallyInvokable]
		void GetDocumentation2(int memid, out string pbstrHelpString, out int pdwHelpStringContext, out string pbstrHelpStringDll);

		// Token: 0x060066B9 RID: 26297
		void GetAllCustData(IntPtr pCustData);

		// Token: 0x060066BA RID: 26298
		void GetAllFuncCustData(int index, IntPtr pCustData);

		// Token: 0x060066BB RID: 26299
		void GetAllParamCustData(int indexFunc, int indexParam, IntPtr pCustData);

		// Token: 0x060066BC RID: 26300
		void GetAllVarCustData(int index, IntPtr pCustData);

		// Token: 0x060066BD RID: 26301
		void GetAllImplTypeCustData(int index, IntPtr pCustData);
	}
}
