using System;
using System.Runtime.InteropServices;

namespace Mono.Cecil.Pdb
{
	// Token: 0x02000358 RID: 856
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("BA3FEE4C-ECB9-4e41-83B7-183FA41CD859")]
	[ComImport]
	internal interface IMetaDataEmit
	{
		// Token: 0x06001609 RID: 5641
		void SetModuleProps(string szName);

		// Token: 0x0600160A RID: 5642
		void Save(string szFile, uint dwSaveFlags);

		// Token: 0x0600160B RID: 5643
		void SaveToStream(IntPtr pIStream, uint dwSaveFlags);

		// Token: 0x0600160C RID: 5644
		uint GetSaveSize(uint fSave);

		// Token: 0x0600160D RID: 5645
		uint DefineTypeDef(IntPtr szTypeDef, uint dwTypeDefFlags, uint tkExtends, IntPtr rtkImplements);

		// Token: 0x0600160E RID: 5646
		uint DefineNestedType(IntPtr szTypeDef, uint dwTypeDefFlags, uint tkExtends, IntPtr rtkImplements, uint tdEncloser);

		// Token: 0x0600160F RID: 5647
		void SetHandler([MarshalAs(UnmanagedType.IUnknown)] [In] object pUnk);

		// Token: 0x06001610 RID: 5648
		uint DefineMethod(uint td, IntPtr zName, uint dwMethodFlags, IntPtr pvSigBlob, uint cbSigBlob, uint ulCodeRVA, uint dwImplFlags);

		// Token: 0x06001611 RID: 5649
		void DefineMethodImpl(uint td, uint tkBody, uint tkDecl);

		// Token: 0x06001612 RID: 5650
		uint DefineTypeRefByName(uint tkResolutionScope, IntPtr szName);

		// Token: 0x06001613 RID: 5651
		uint DefineImportType(IntPtr pAssemImport, IntPtr pbHashValue, uint cbHashValue, IMetaDataImport pImport, uint tdImport, IntPtr pAssemEmit);

		// Token: 0x06001614 RID: 5652
		uint DefineMemberRef(uint tkImport, string szName, IntPtr pvSigBlob, uint cbSigBlob);

		// Token: 0x06001615 RID: 5653
		uint DefineImportMember(IntPtr pAssemImport, IntPtr pbHashValue, uint cbHashValue, IMetaDataImport pImport, uint mbMember, IntPtr pAssemEmit, uint tkParent);

		// Token: 0x06001616 RID: 5654
		uint DefineEvent(uint td, string szEvent, uint dwEventFlags, uint tkEventType, uint mdAddOn, uint mdRemoveOn, uint mdFire, IntPtr rmdOtherMethods);

		// Token: 0x06001617 RID: 5655
		void SetClassLayout(uint td, uint dwPackSize, IntPtr rFieldOffsets, uint ulClassSize);

		// Token: 0x06001618 RID: 5656
		void DeleteClassLayout(uint td);

		// Token: 0x06001619 RID: 5657
		void SetFieldMarshal(uint tk, IntPtr pvNativeType, uint cbNativeType);

		// Token: 0x0600161A RID: 5658
		void DeleteFieldMarshal(uint tk);

		// Token: 0x0600161B RID: 5659
		uint DefinePermissionSet(uint tk, uint dwAction, IntPtr pvPermission, uint cbPermission);

		// Token: 0x0600161C RID: 5660
		void SetRVA(uint md, uint ulRVA);

		// Token: 0x0600161D RID: 5661
		uint GetTokenFromSig(IntPtr pvSig, uint cbSig);

		// Token: 0x0600161E RID: 5662
		uint DefineModuleRef(string szName);

		// Token: 0x0600161F RID: 5663
		void SetParent(uint mr, uint tk);

		// Token: 0x06001620 RID: 5664
		uint GetTokenFromTypeSpec(IntPtr pvSig, uint cbSig);

		// Token: 0x06001621 RID: 5665
		void SaveToMemory(IntPtr pbData, uint cbData);

		// Token: 0x06001622 RID: 5666
		uint DefineUserString(string szString, uint cchString);

		// Token: 0x06001623 RID: 5667
		void DeleteToken(uint tkObj);

		// Token: 0x06001624 RID: 5668
		void SetMethodProps(uint md, uint dwMethodFlags, uint ulCodeRVA, uint dwImplFlags);

		// Token: 0x06001625 RID: 5669
		void SetTypeDefProps(uint td, uint dwTypeDefFlags, uint tkExtends, IntPtr rtkImplements);

		// Token: 0x06001626 RID: 5670
		void SetEventProps(uint ev, uint dwEventFlags, uint tkEventType, uint mdAddOn, uint mdRemoveOn, uint mdFire, IntPtr rmdOtherMethods);

		// Token: 0x06001627 RID: 5671
		uint SetPermissionSetProps(uint tk, uint dwAction, IntPtr pvPermission, uint cbPermission);

		// Token: 0x06001628 RID: 5672
		void DefinePinvokeMap(uint tk, uint dwMappingFlags, string szImportName, uint mrImportDLL);

		// Token: 0x06001629 RID: 5673
		void SetPinvokeMap(uint tk, uint dwMappingFlags, string szImportName, uint mrImportDLL);

		// Token: 0x0600162A RID: 5674
		void DeletePinvokeMap(uint tk);

		// Token: 0x0600162B RID: 5675
		uint DefineCustomAttribute(uint tkObj, uint tkType, IntPtr pCustomAttribute, uint cbCustomAttribute);

		// Token: 0x0600162C RID: 5676
		void SetCustomAttributeValue(uint pcv, IntPtr pCustomAttribute, uint cbCustomAttribute);

		// Token: 0x0600162D RID: 5677
		uint DefineField(uint td, string szName, uint dwFieldFlags, IntPtr pvSigBlob, uint cbSigBlob, uint dwCPlusTypeFlag, IntPtr pValue, uint cchValue);

		// Token: 0x0600162E RID: 5678
		uint DefineProperty(uint td, string szProperty, uint dwPropFlags, IntPtr pvSig, uint cbSig, uint dwCPlusTypeFlag, IntPtr pValue, uint cchValue, uint mdSetter, uint mdGetter, IntPtr rmdOtherMethods);

		// Token: 0x0600162F RID: 5679
		uint DefineParam(uint md, uint ulParamSeq, string szName, uint dwParamFlags, uint dwCPlusTypeFlag, IntPtr pValue, uint cchValue);

		// Token: 0x06001630 RID: 5680
		void SetFieldProps(uint fd, uint dwFieldFlags, uint dwCPlusTypeFlag, IntPtr pValue, uint cchValue);

		// Token: 0x06001631 RID: 5681
		void SetPropertyProps(uint pr, uint dwPropFlags, uint dwCPlusTypeFlag, IntPtr pValue, uint cchValue, uint mdSetter, uint mdGetter, IntPtr rmdOtherMethods);

		// Token: 0x06001632 RID: 5682
		void SetParamProps(uint pd, string szName, uint dwParamFlags, uint dwCPlusTypeFlag, IntPtr pValue, uint cchValue);

		// Token: 0x06001633 RID: 5683
		uint DefineSecurityAttributeSet(uint tkObj, IntPtr rSecAttrs, uint cSecAttrs);

		// Token: 0x06001634 RID: 5684
		void ApplyEditAndContinue([MarshalAs(UnmanagedType.IUnknown)] object pImport);

		// Token: 0x06001635 RID: 5685
		uint TranslateSigWithScope(IntPtr pAssemImport, IntPtr pbHashValue, uint cbHashValue, IMetaDataImport import, IntPtr pbSigBlob, uint cbSigBlob, IntPtr pAssemEmit, IMetaDataEmit emit, IntPtr pvTranslatedSig, uint cbTranslatedSigMax);

		// Token: 0x06001636 RID: 5686
		void SetMethodImplFlags(uint md, uint dwImplFlags);

		// Token: 0x06001637 RID: 5687
		void SetFieldRVA(uint fd, uint ulRVA);

		// Token: 0x06001638 RID: 5688
		void Merge(IMetaDataImport pImport, IntPtr pHostMapToken, [MarshalAs(UnmanagedType.IUnknown)] object pHandler);

		// Token: 0x06001639 RID: 5689
		void MergeEnd();
	}
}
