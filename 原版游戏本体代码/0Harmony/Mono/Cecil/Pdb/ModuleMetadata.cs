using System;
using System.Collections.Generic;
using System.Text;

namespace Mono.Cecil.Pdb
{
	// Token: 0x0200035A RID: 858
	internal class ModuleMetadata : IMetaDataEmit, IMetaDataImport
	{
		// Token: 0x06001678 RID: 5752 RVA: 0x00045E4B File Offset: 0x0004404B
		public ModuleMetadata(ModuleDefinition module)
		{
			this.module = module;
		}

		// Token: 0x06001679 RID: 5753 RVA: 0x00045E5A File Offset: 0x0004405A
		private bool TryGetType(uint token, out TypeDefinition type)
		{
			if (this.types == null)
			{
				this.InitializeMetadata(this.module);
			}
			return this.types.TryGetValue(token, out type);
		}

		// Token: 0x0600167A RID: 5754 RVA: 0x00045E7D File Offset: 0x0004407D
		private bool TryGetMethod(uint token, out MethodDefinition method)
		{
			if (this.methods == null)
			{
				this.InitializeMetadata(this.module);
			}
			return this.methods.TryGetValue(token, out method);
		}

		// Token: 0x0600167B RID: 5755 RVA: 0x00045EA0 File Offset: 0x000440A0
		private void InitializeMetadata(ModuleDefinition module)
		{
			this.types = new Dictionary<uint, TypeDefinition>();
			this.methods = new Dictionary<uint, MethodDefinition>();
			foreach (TypeDefinition type in module.GetTypes())
			{
				this.types.Add(type.MetadataToken.ToUInt32(), type);
				this.InitializeMethods(type);
			}
		}

		// Token: 0x0600167C RID: 5756 RVA: 0x00045F20 File Offset: 0x00044120
		private void InitializeMethods(TypeDefinition type)
		{
			foreach (MethodDefinition method in type.Methods)
			{
				this.methods.Add(method.MetadataToken.ToUInt32(), method);
			}
		}

		// Token: 0x0600167D RID: 5757 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public void SetModuleProps(string szName)
		{
			throw new NotImplementedException();
		}

		// Token: 0x0600167E RID: 5758 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public void Save(string szFile, uint dwSaveFlags)
		{
			throw new NotImplementedException();
		}

		// Token: 0x0600167F RID: 5759 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public void SaveToStream(IntPtr pIStream, uint dwSaveFlags)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06001680 RID: 5760 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint GetSaveSize(uint fSave)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06001681 RID: 5761 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint DefineTypeDef(IntPtr szTypeDef, uint dwTypeDefFlags, uint tkExtends, IntPtr rtkImplements)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06001682 RID: 5762 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint DefineNestedType(IntPtr szTypeDef, uint dwTypeDefFlags, uint tkExtends, IntPtr rtkImplements, uint tdEncloser)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06001683 RID: 5763 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public void SetHandler(object pUnk)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06001684 RID: 5764 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint DefineMethod(uint td, IntPtr zName, uint dwMethodFlags, IntPtr pvSigBlob, uint cbSigBlob, uint ulCodeRVA, uint dwImplFlags)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06001685 RID: 5765 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public void DefineMethodImpl(uint td, uint tkBody, uint tkDecl)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06001686 RID: 5766 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint DefineTypeRefByName(uint tkResolutionScope, IntPtr szName)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06001687 RID: 5767 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint DefineImportType(IntPtr pAssemImport, IntPtr pbHashValue, uint cbHashValue, IMetaDataImport pImport, uint tdImport, IntPtr pAssemEmit)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06001688 RID: 5768 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint DefineMemberRef(uint tkImport, string szName, IntPtr pvSigBlob, uint cbSigBlob)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06001689 RID: 5769 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint DefineImportMember(IntPtr pAssemImport, IntPtr pbHashValue, uint cbHashValue, IMetaDataImport pImport, uint mbMember, IntPtr pAssemEmit, uint tkParent)
		{
			throw new NotImplementedException();
		}

		// Token: 0x0600168A RID: 5770 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint DefineEvent(uint td, string szEvent, uint dwEventFlags, uint tkEventType, uint mdAddOn, uint mdRemoveOn, uint mdFire, IntPtr rmdOtherMethods)
		{
			throw new NotImplementedException();
		}

		// Token: 0x0600168B RID: 5771 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public void SetClassLayout(uint td, uint dwPackSize, IntPtr rFieldOffsets, uint ulClassSize)
		{
			throw new NotImplementedException();
		}

		// Token: 0x0600168C RID: 5772 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public void DeleteClassLayout(uint td)
		{
			throw new NotImplementedException();
		}

		// Token: 0x0600168D RID: 5773 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public void SetFieldMarshal(uint tk, IntPtr pvNativeType, uint cbNativeType)
		{
			throw new NotImplementedException();
		}

		// Token: 0x0600168E RID: 5774 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public void DeleteFieldMarshal(uint tk)
		{
			throw new NotImplementedException();
		}

		// Token: 0x0600168F RID: 5775 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint DefinePermissionSet(uint tk, uint dwAction, IntPtr pvPermission, uint cbPermission)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06001690 RID: 5776 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public void SetRVA(uint md, uint ulRVA)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06001691 RID: 5777 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint GetTokenFromSig(IntPtr pvSig, uint cbSig)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06001692 RID: 5778 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint DefineModuleRef(string szName)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06001693 RID: 5779 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public void SetParent(uint mr, uint tk)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06001694 RID: 5780 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint GetTokenFromTypeSpec(IntPtr pvSig, uint cbSig)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06001695 RID: 5781 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public void SaveToMemory(IntPtr pbData, uint cbData)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06001696 RID: 5782 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint DefineUserString(string szString, uint cchString)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06001697 RID: 5783 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public void DeleteToken(uint tkObj)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06001698 RID: 5784 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public void SetMethodProps(uint md, uint dwMethodFlags, uint ulCodeRVA, uint dwImplFlags)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06001699 RID: 5785 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public void SetTypeDefProps(uint td, uint dwTypeDefFlags, uint tkExtends, IntPtr rtkImplements)
		{
			throw new NotImplementedException();
		}

		// Token: 0x0600169A RID: 5786 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public void SetEventProps(uint ev, uint dwEventFlags, uint tkEventType, uint mdAddOn, uint mdRemoveOn, uint mdFire, IntPtr rmdOtherMethods)
		{
			throw new NotImplementedException();
		}

		// Token: 0x0600169B RID: 5787 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint SetPermissionSetProps(uint tk, uint dwAction, IntPtr pvPermission, uint cbPermission)
		{
			throw new NotImplementedException();
		}

		// Token: 0x0600169C RID: 5788 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public void DefinePinvokeMap(uint tk, uint dwMappingFlags, string szImportName, uint mrImportDLL)
		{
			throw new NotImplementedException();
		}

		// Token: 0x0600169D RID: 5789 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public void SetPinvokeMap(uint tk, uint dwMappingFlags, string szImportName, uint mrImportDLL)
		{
			throw new NotImplementedException();
		}

		// Token: 0x0600169E RID: 5790 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public void DeletePinvokeMap(uint tk)
		{
			throw new NotImplementedException();
		}

		// Token: 0x0600169F RID: 5791 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint DefineCustomAttribute(uint tkObj, uint tkType, IntPtr pCustomAttribute, uint cbCustomAttribute)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016A0 RID: 5792 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public void SetCustomAttributeValue(uint pcv, IntPtr pCustomAttribute, uint cbCustomAttribute)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016A1 RID: 5793 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint DefineField(uint td, string szName, uint dwFieldFlags, IntPtr pvSigBlob, uint cbSigBlob, uint dwCPlusTypeFlag, IntPtr pValue, uint cchValue)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016A2 RID: 5794 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint DefineProperty(uint td, string szProperty, uint dwPropFlags, IntPtr pvSig, uint cbSig, uint dwCPlusTypeFlag, IntPtr pValue, uint cchValue, uint mdSetter, uint mdGetter, IntPtr rmdOtherMethods)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016A3 RID: 5795 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint DefineParam(uint md, uint ulParamSeq, string szName, uint dwParamFlags, uint dwCPlusTypeFlag, IntPtr pValue, uint cchValue)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016A4 RID: 5796 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public void SetFieldProps(uint fd, uint dwFieldFlags, uint dwCPlusTypeFlag, IntPtr pValue, uint cchValue)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016A5 RID: 5797 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public void SetPropertyProps(uint pr, uint dwPropFlags, uint dwCPlusTypeFlag, IntPtr pValue, uint cchValue, uint mdSetter, uint mdGetter, IntPtr rmdOtherMethods)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016A6 RID: 5798 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public void SetParamProps(uint pd, string szName, uint dwParamFlags, uint dwCPlusTypeFlag, IntPtr pValue, uint cchValue)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016A7 RID: 5799 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint DefineSecurityAttributeSet(uint tkObj, IntPtr rSecAttrs, uint cSecAttrs)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016A8 RID: 5800 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public void ApplyEditAndContinue(object pImport)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016A9 RID: 5801 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint TranslateSigWithScope(IntPtr pAssemImport, IntPtr pbHashValue, uint cbHashValue, IMetaDataImport import, IntPtr pbSigBlob, uint cbSigBlob, IntPtr pAssemEmit, IMetaDataEmit emit, IntPtr pvTranslatedSig, uint cbTranslatedSigMax)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016AA RID: 5802 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public void SetMethodImplFlags(uint md, uint dwImplFlags)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016AB RID: 5803 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public void SetFieldRVA(uint fd, uint ulRVA)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016AC RID: 5804 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public void Merge(IMetaDataImport pImport, IntPtr pHostMapToken, object pHandler)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016AD RID: 5805 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public void MergeEnd()
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016AE RID: 5806 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public void CloseEnum(uint hEnum)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016AF RID: 5807 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint CountEnum(uint hEnum)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016B0 RID: 5808 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public void ResetEnum(uint hEnum, uint ulPos)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016B1 RID: 5809 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint EnumTypeDefs(ref uint phEnum, uint[] rTypeDefs, uint cMax)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016B2 RID: 5810 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint EnumInterfaceImpls(ref uint phEnum, uint td, uint[] rImpls, uint cMax)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016B3 RID: 5811 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint EnumTypeRefs(ref uint phEnum, uint[] rTypeRefs, uint cMax)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016B4 RID: 5812 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint FindTypeDefByName(string szTypeDef, uint tkEnclosingClass)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016B5 RID: 5813 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public Guid GetScopeProps(StringBuilder szName, uint cchName, out uint pchName)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016B6 RID: 5814 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint GetModuleFromScope()
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016B7 RID: 5815 RVA: 0x00045F88 File Offset: 0x00044188
		public unsafe uint GetTypeDefProps(uint td, char* szTypeDef, uint cchTypeDef, uint* pchTypeDef, uint* pdwTypeDefFlags, uint* ptkExtends)
		{
			TypeDefinition type;
			if (!this.TryGetType(td, out type))
			{
				return 2147500037U;
			}
			ModuleMetadata.WriteNameBuffer(type.IsNested ? type.Name : type.FullName, szTypeDef, cchTypeDef, pchTypeDef);
			if (pdwTypeDefFlags != null)
			{
				*pdwTypeDefFlags = (uint)type.Attributes;
			}
			if (ptkExtends != null)
			{
				*ptkExtends = ((type.BaseType != null) ? type.BaseType.MetadataToken.ToUInt32() : 0U);
			}
			return 0U;
		}

		// Token: 0x060016B8 RID: 5816 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint GetInterfaceImplProps(uint iiImpl, out uint pClass)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016B9 RID: 5817 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint GetTypeRefProps(uint tr, out uint ptkResolutionScope, StringBuilder szName, uint cchName)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016BA RID: 5818 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint ResolveTypeRef(uint tr, ref Guid riid, out object ppIScope)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016BB RID: 5819 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint EnumMembers(ref uint phEnum, uint cl, uint[] rMembers, uint cMax)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016BC RID: 5820 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint EnumMembersWithName(ref uint phEnum, uint cl, string szName, uint[] rMembers, uint cMax)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016BD RID: 5821 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint EnumMethods(ref uint phEnum, uint cl, IntPtr rMethods, uint cMax)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016BE RID: 5822 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint EnumMethodsWithName(ref uint phEnum, uint cl, string szName, uint[] rMethods, uint cMax)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016BF RID: 5823 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint EnumFields(ref uint phEnum, uint cl, IntPtr rFields, uint cMax)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016C0 RID: 5824 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint EnumFieldsWithName(ref uint phEnum, uint cl, string szName, uint[] rFields, uint cMax)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016C1 RID: 5825 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint EnumParams(ref uint phEnum, uint mb, uint[] rParams, uint cMax)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016C2 RID: 5826 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint EnumMemberRefs(ref uint phEnum, uint tkParent, uint[] rMemberRefs, uint cMax)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016C3 RID: 5827 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint EnumMethodImpls(ref uint phEnum, uint td, uint[] rMethodBody, uint[] rMethodDecl, uint cMax)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016C4 RID: 5828 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint EnumPermissionSets(ref uint phEnum, uint tk, uint dwActions, uint[] rPermission, uint cMax)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016C5 RID: 5829 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint FindMember(uint td, string szName, byte[] pvSigBlob, uint cbSigBlob)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016C6 RID: 5830 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint FindMethod(uint td, string szName, byte[] pvSigBlob, uint cbSigBlob)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016C7 RID: 5831 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint FindField(uint td, string szName, byte[] pvSigBlob, uint cbSigBlob)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016C8 RID: 5832 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint FindMemberRef(uint td, string szName, byte[] pvSigBlob, uint cbSigBlob)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016C9 RID: 5833 RVA: 0x00045FFC File Offset: 0x000441FC
		public unsafe uint GetMethodProps(uint mb, uint* pClass, char* szMethod, uint cchMethod, uint* pchMethod, uint* pdwAttr, IntPtr ppvSigBlob, IntPtr pcbSigBlob, uint* pulCodeRVA, uint* pdwImplFlags)
		{
			MethodDefinition method;
			if (!this.TryGetMethod(mb, out method))
			{
				return 2147500037U;
			}
			if (pClass != null)
			{
				*pClass = method.DeclaringType.MetadataToken.ToUInt32();
			}
			ModuleMetadata.WriteNameBuffer(method.Name, szMethod, cchMethod, pchMethod);
			if (pdwAttr != null)
			{
				*pdwAttr = (uint)method.Attributes;
			}
			if (pulCodeRVA != null)
			{
				*pulCodeRVA = (uint)method.RVA;
			}
			if (pdwImplFlags != null)
			{
				*pdwImplFlags = (uint)method.ImplAttributes;
			}
			return 0U;
		}

		// Token: 0x060016CA RID: 5834 RVA: 0x00046074 File Offset: 0x00044274
		private unsafe static void WriteNameBuffer(string name, char* buffer, uint bufferLength, uint* actualLength)
		{
			long length = Math.Min((long)name.Length, (long)((ulong)(bufferLength - 1U)));
			if (actualLength != null)
			{
				*actualLength = (uint)length;
			}
			if (buffer != null && bufferLength > 0U)
			{
				int i = 0;
				while ((long)i < length)
				{
					buffer[i] = name[i];
					i++;
				}
				buffer[(length + 1L) * 2L / 2L] = '\0';
			}
		}

		// Token: 0x060016CB RID: 5835 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint GetMemberRefProps(uint mr, ref uint ptk, StringBuilder szMember, uint cchMember, out uint pchMember, out IntPtr ppvSigBlob)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016CC RID: 5836 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint EnumProperties(ref uint phEnum, uint td, IntPtr rProperties, uint cMax)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016CD RID: 5837 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint EnumEvents(ref uint phEnum, uint td, IntPtr rEvents, uint cMax)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016CE RID: 5838 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint GetEventProps(uint ev, out uint pClass, StringBuilder szEvent, uint cchEvent, out uint pchEvent, out uint pdwEventFlags, out uint ptkEventType, out uint pmdAddOn, out uint pmdRemoveOn, out uint pmdFire, uint[] rmdOtherMethod, uint cMax)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016CF RID: 5839 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint EnumMethodSemantics(ref uint phEnum, uint mb, uint[] rEventProp, uint cMax)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016D0 RID: 5840 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint GetMethodSemantics(uint mb, uint tkEventProp)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016D1 RID: 5841 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint GetClassLayout(uint td, out uint pdwPackSize, IntPtr rFieldOffset, uint cMax, out uint pcFieldOffset)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016D2 RID: 5842 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint GetFieldMarshal(uint tk, out IntPtr ppvNativeType)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016D3 RID: 5843 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint GetRVA(uint tk, out uint pulCodeRVA)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016D4 RID: 5844 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint GetPermissionSetProps(uint pm, out uint pdwAction, out IntPtr ppvPermission)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016D5 RID: 5845 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint GetSigFromToken(uint mdSig, out IntPtr ppvSig)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016D6 RID: 5846 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint GetModuleRefProps(uint mur, StringBuilder szName, uint cchName)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016D7 RID: 5847 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint EnumModuleRefs(ref uint phEnum, uint[] rModuleRefs, uint cmax)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016D8 RID: 5848 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint GetTypeSpecFromToken(uint typespec, out IntPtr ppvSig)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016D9 RID: 5849 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint GetNameFromToken(uint tk)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016DA RID: 5850 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint EnumUnresolvedMethods(ref uint phEnum, uint[] rMethods, uint cMax)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016DB RID: 5851 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint GetUserString(uint stk, StringBuilder szString, uint cchString)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016DC RID: 5852 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint GetPinvokeMap(uint tk, out uint pdwMappingFlags, StringBuilder szImportName, uint cchImportName, out uint pchImportName)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016DD RID: 5853 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint EnumSignatures(ref uint phEnum, uint[] rSignatures, uint cmax)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016DE RID: 5854 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint EnumTypeSpecs(ref uint phEnum, uint[] rTypeSpecs, uint cmax)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016DF RID: 5855 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint EnumUserStrings(ref uint phEnum, uint[] rStrings, uint cmax)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016E0 RID: 5856 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public int GetParamForMethodIndex(uint md, uint ulParamSeq, out uint pParam)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016E1 RID: 5857 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint EnumCustomAttributes(ref uint phEnum, uint tk, uint tkType, uint[] rCustomAttributes, uint cMax)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016E2 RID: 5858 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint GetCustomAttributeProps(uint cv, out uint ptkObj, out uint ptkType, out IntPtr ppBlob)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016E3 RID: 5859 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint FindTypeRef(uint tkResolutionScope, string szName)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016E4 RID: 5860 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint GetMemberProps(uint mb, out uint pClass, StringBuilder szMember, uint cchMember, out uint pchMember, out uint pdwAttr, out IntPtr ppvSigBlob, out uint pcbSigBlob, out uint pulCodeRVA, out uint pdwImplFlags, out uint pdwCPlusTypeFlag, out IntPtr ppValue)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016E5 RID: 5861 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint GetFieldProps(uint mb, out uint pClass, StringBuilder szField, uint cchField, out uint pchField, out uint pdwAttr, out IntPtr ppvSigBlob, out uint pcbSigBlob, out uint pdwCPlusTypeFlag, out IntPtr ppValue)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016E6 RID: 5862 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint GetPropertyProps(uint prop, out uint pClass, StringBuilder szProperty, uint cchProperty, out uint pchProperty, out uint pdwPropFlags, out IntPtr ppvSig, out uint pbSig, out uint pdwCPlusTypeFlag, out IntPtr ppDefaultValue, out uint pcchDefaultValue, out uint pmdSetter, out uint pmdGetter, uint[] rmdOtherMethod, uint cMax)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016E7 RID: 5863 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint GetParamProps(uint tk, out uint pmd, out uint pulSequence, StringBuilder szName, uint cchName, out uint pchName, out uint pdwAttr, out uint pdwCPlusTypeFlag, out IntPtr ppValue)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016E8 RID: 5864 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint GetCustomAttributeByName(uint tkObj, string szName, out IntPtr ppData)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016E9 RID: 5865 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public bool IsValidToken(uint tk)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016EA RID: 5866 RVA: 0x000460CC File Offset: 0x000442CC
		public unsafe uint GetNestedClassProps(uint tdNestedClass, uint* ptdEnclosingClass)
		{
			TypeDefinition type;
			if (!this.TryGetType(tdNestedClass, out type))
			{
				return 2147500037U;
			}
			if (ptdEnclosingClass != null)
			{
				*ptdEnclosingClass = (type.IsNested ? type.DeclaringType.MetadataToken.ToUInt32() : 0U);
			}
			return 0U;
		}

		// Token: 0x060016EB RID: 5867 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public uint GetNativeCallConvFromSig(IntPtr pvSig, uint cbSig)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060016EC RID: 5868 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public int IsGlobal(uint pd)
		{
			throw new NotImplementedException();
		}

		// Token: 0x04000B2E RID: 2862
		private readonly ModuleDefinition module;

		// Token: 0x04000B2F RID: 2863
		private Dictionary<uint, TypeDefinition> types;

		// Token: 0x04000B30 RID: 2864
		private Dictionary<uint, MethodDefinition> methods;

		// Token: 0x04000B31 RID: 2865
		private const uint S_OK = 0U;

		// Token: 0x04000B32 RID: 2866
		private const uint E_FAIL = 2147500037U;
	}
}
