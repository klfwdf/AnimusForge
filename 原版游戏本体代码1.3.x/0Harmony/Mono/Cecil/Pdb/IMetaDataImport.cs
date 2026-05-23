using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Mono.Cecil.Pdb
{
	// Token: 0x02000359 RID: 857
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("7DAC8207-D3AE-4c75-9B67-92801A497D44")]
	[ComImport]
	internal interface IMetaDataImport
	{
		// Token: 0x0600163A RID: 5690
		[PreserveSig]
		void CloseEnum(uint hEnum);

		// Token: 0x0600163B RID: 5691
		uint CountEnum(uint hEnum);

		// Token: 0x0600163C RID: 5692
		void ResetEnum(uint hEnum, uint ulPos);

		// Token: 0x0600163D RID: 5693
		uint EnumTypeDefs(ref uint phEnum, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] uint[] rTypeDefs, uint cMax);

		// Token: 0x0600163E RID: 5694
		uint EnumInterfaceImpls(ref uint phEnum, uint td, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] uint[] rImpls, uint cMax);

		// Token: 0x0600163F RID: 5695
		uint EnumTypeRefs(ref uint phEnum, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] uint[] rTypeRefs, uint cMax);

		// Token: 0x06001640 RID: 5696
		uint FindTypeDefByName(string szTypeDef, uint tkEnclosingClass);

		// Token: 0x06001641 RID: 5697
		Guid GetScopeProps(StringBuilder szName, uint cchName, out uint pchName);

		// Token: 0x06001642 RID: 5698
		uint GetModuleFromScope();

		// Token: 0x06001643 RID: 5699
		[PreserveSig]
		unsafe uint GetTypeDefProps(uint td, char* szTypeDef, uint cchTypeDef, uint* pchTypeDef, uint* pdwTypeDefFlags, uint* ptkExtends);

		// Token: 0x06001644 RID: 5700
		uint GetInterfaceImplProps(uint iiImpl, out uint pClass);

		// Token: 0x06001645 RID: 5701
		uint GetTypeRefProps(uint tr, out uint ptkResolutionScope, StringBuilder szName, uint cchName);

		// Token: 0x06001646 RID: 5702
		uint ResolveTypeRef(uint tr, [In] ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object ppIScope);

		// Token: 0x06001647 RID: 5703
		uint EnumMembers(ref uint phEnum, uint cl, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] uint[] rMembers, uint cMax);

		// Token: 0x06001648 RID: 5704
		uint EnumMembersWithName(ref uint phEnum, uint cl, string szName, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] uint[] rMembers, uint cMax);

		// Token: 0x06001649 RID: 5705
		uint EnumMethods(ref uint phEnum, uint cl, IntPtr rMethods, uint cMax);

		// Token: 0x0600164A RID: 5706
		uint EnumMethodsWithName(ref uint phEnum, uint cl, string szName, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] uint[] rMethods, uint cMax);

		// Token: 0x0600164B RID: 5707
		uint EnumFields(ref uint phEnum, uint cl, IntPtr rFields, uint cMax);

		// Token: 0x0600164C RID: 5708
		uint EnumFieldsWithName(ref uint phEnum, uint cl, string szName, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] uint[] rFields, uint cMax);

		// Token: 0x0600164D RID: 5709
		uint EnumParams(ref uint phEnum, uint mb, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] uint[] rParams, uint cMax);

		// Token: 0x0600164E RID: 5710
		uint EnumMemberRefs(ref uint phEnum, uint tkParent, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] uint[] rMemberRefs, uint cMax);

		// Token: 0x0600164F RID: 5711
		uint EnumMethodImpls(ref uint phEnum, uint td, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] uint[] rMethodBody, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] uint[] rMethodDecl, uint cMax);

		// Token: 0x06001650 RID: 5712
		uint EnumPermissionSets(ref uint phEnum, uint tk, uint dwActions, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] uint[] rPermission, uint cMax);

		// Token: 0x06001651 RID: 5713
		uint FindMember(uint td, string szName, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] pvSigBlob, uint cbSigBlob);

		// Token: 0x06001652 RID: 5714
		uint FindMethod(uint td, string szName, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] pvSigBlob, uint cbSigBlob);

		// Token: 0x06001653 RID: 5715
		uint FindField(uint td, string szName, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] pvSigBlob, uint cbSigBlob);

		// Token: 0x06001654 RID: 5716
		uint FindMemberRef(uint td, string szName, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] pvSigBlob, uint cbSigBlob);

		// Token: 0x06001655 RID: 5717
		[PreserveSig]
		unsafe uint GetMethodProps(uint mb, uint* pClass, char* szMethod, uint cchMethod, uint* pchMethod, uint* pdwAttr, IntPtr ppvSigBlob, IntPtr pcbSigBlob, uint* pulCodeRVA, uint* pdwImplFlags);

		// Token: 0x06001656 RID: 5718
		uint GetMemberRefProps(uint mr, ref uint ptk, StringBuilder szMember, uint cchMember, out uint pchMember, out IntPtr ppvSigBlob);

		// Token: 0x06001657 RID: 5719
		uint EnumProperties(ref uint phEnum, uint td, IntPtr rProperties, uint cMax);

		// Token: 0x06001658 RID: 5720
		uint EnumEvents(ref uint phEnum, uint td, IntPtr rEvents, uint cMax);

		// Token: 0x06001659 RID: 5721
		uint GetEventProps(uint ev, out uint pClass, StringBuilder szEvent, uint cchEvent, out uint pchEvent, out uint pdwEventFlags, out uint ptkEventType, out uint pmdAddOn, out uint pmdRemoveOn, out uint pmdFire, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 11)] uint[] rmdOtherMethod, uint cMax);

		// Token: 0x0600165A RID: 5722
		uint EnumMethodSemantics(ref uint phEnum, uint mb, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] uint[] rEventProp, uint cMax);

		// Token: 0x0600165B RID: 5723
		uint GetMethodSemantics(uint mb, uint tkEventProp);

		// Token: 0x0600165C RID: 5724
		uint GetClassLayout(uint td, out uint pdwPackSize, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] IntPtr rFieldOffset, uint cMax, out uint pcFieldOffset);

		// Token: 0x0600165D RID: 5725
		uint GetFieldMarshal(uint tk, out IntPtr ppvNativeType);

		// Token: 0x0600165E RID: 5726
		uint GetRVA(uint tk, out uint pulCodeRVA);

		// Token: 0x0600165F RID: 5727
		uint GetPermissionSetProps(uint pm, out uint pdwAction, out IntPtr ppvPermission);

		// Token: 0x06001660 RID: 5728
		uint GetSigFromToken(uint mdSig, out IntPtr ppvSig);

		// Token: 0x06001661 RID: 5729
		uint GetModuleRefProps(uint mur, StringBuilder szName, uint cchName);

		// Token: 0x06001662 RID: 5730
		uint EnumModuleRefs(ref uint phEnum, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] uint[] rModuleRefs, uint cmax);

		// Token: 0x06001663 RID: 5731
		uint GetTypeSpecFromToken(uint typespec, out IntPtr ppvSig);

		// Token: 0x06001664 RID: 5732
		uint GetNameFromToken(uint tk);

		// Token: 0x06001665 RID: 5733
		uint EnumUnresolvedMethods(ref uint phEnum, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] uint[] rMethods, uint cMax);

		// Token: 0x06001666 RID: 5734
		uint GetUserString(uint stk, StringBuilder szString, uint cchString);

		// Token: 0x06001667 RID: 5735
		uint GetPinvokeMap(uint tk, out uint pdwMappingFlags, StringBuilder szImportName, uint cchImportName, out uint pchImportName);

		// Token: 0x06001668 RID: 5736
		uint EnumSignatures(ref uint phEnum, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] uint[] rSignatures, uint cmax);

		// Token: 0x06001669 RID: 5737
		uint EnumTypeSpecs(ref uint phEnum, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] uint[] rTypeSpecs, uint cmax);

		// Token: 0x0600166A RID: 5738
		uint EnumUserStrings(ref uint phEnum, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] uint[] rStrings, uint cmax);

		// Token: 0x0600166B RID: 5739
		[PreserveSig]
		int GetParamForMethodIndex(uint md, uint ulParamSeq, out uint pParam);

		// Token: 0x0600166C RID: 5740
		uint EnumCustomAttributes(ref uint phEnum, uint tk, uint tkType, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] uint[] rCustomAttributes, uint cMax);

		// Token: 0x0600166D RID: 5741
		uint GetCustomAttributeProps(uint cv, out uint ptkObj, out uint ptkType, out IntPtr ppBlob);

		// Token: 0x0600166E RID: 5742
		uint FindTypeRef(uint tkResolutionScope, string szName);

		// Token: 0x0600166F RID: 5743
		uint GetMemberProps(uint mb, out uint pClass, StringBuilder szMember, uint cchMember, out uint pchMember, out uint pdwAttr, out IntPtr ppvSigBlob, out uint pcbSigBlob, out uint pulCodeRVA, out uint pdwImplFlags, out uint pdwCPlusTypeFlag, out IntPtr ppValue);

		// Token: 0x06001670 RID: 5744
		uint GetFieldProps(uint mb, out uint pClass, StringBuilder szField, uint cchField, out uint pchField, out uint pdwAttr, out IntPtr ppvSigBlob, out uint pcbSigBlob, out uint pdwCPlusTypeFlag, out IntPtr ppValue);

		// Token: 0x06001671 RID: 5745
		uint GetPropertyProps(uint prop, out uint pClass, StringBuilder szProperty, uint cchProperty, out uint pchProperty, out uint pdwPropFlags, out IntPtr ppvSig, out uint pbSig, out uint pdwCPlusTypeFlag, out IntPtr ppDefaultValue, out uint pcchDefaultValue, out uint pmdSetter, out uint pmdGetter, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 14)] uint[] rmdOtherMethod, uint cMax);

		// Token: 0x06001672 RID: 5746
		uint GetParamProps(uint tk, out uint pmd, out uint pulSequence, StringBuilder szName, uint cchName, out uint pchName, out uint pdwAttr, out uint pdwCPlusTypeFlag, out IntPtr ppValue);

		// Token: 0x06001673 RID: 5747
		uint GetCustomAttributeByName(uint tkObj, string szName, out IntPtr ppData);

		// Token: 0x06001674 RID: 5748
		[PreserveSig]
		[return: MarshalAs(UnmanagedType.Bool)]
		bool IsValidToken(uint tk);

		// Token: 0x06001675 RID: 5749
		[PreserveSig]
		unsafe uint GetNestedClassProps(uint tdNestedClass, uint* ptdEnclosingClass);

		// Token: 0x06001676 RID: 5750
		uint GetNativeCallConvFromSig(IntPtr pvSig, uint cbSig);

		// Token: 0x06001677 RID: 5751
		int IsGlobal(uint pd);
	}
}
