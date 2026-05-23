using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Reflection
{
	// Token: 0x020005FE RID: 1534
	internal struct MetadataImport
	{
		// Token: 0x06004696 RID: 18070 RVA: 0x00102C00 File Offset: 0x00100E00
		public override int GetHashCode()
		{
			return ValueType.GetHashCodeOfPtr(this.m_metadataImport2);
		}

		// Token: 0x06004697 RID: 18071 RVA: 0x00102C0D File Offset: 0x00100E0D
		public override bool Equals(object obj)
		{
			return obj is MetadataImport && this.Equals((MetadataImport)obj);
		}

		// Token: 0x06004698 RID: 18072 RVA: 0x00102C25 File Offset: 0x00100E25
		private bool Equals(MetadataImport import)
		{
			return import.m_metadataImport2 == this.m_metadataImport2;
		}

		// Token: 0x06004699 RID: 18073
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void _GetMarshalAs(IntPtr pNativeType, int cNativeType, out int unmanagedType, out int safeArraySubType, out string safeArrayUserDefinedSubType, out int arraySubType, out int sizeParamIndex, out int sizeConst, out string marshalType, out string marshalCookie, out int iidParamIndex);

		// Token: 0x0600469A RID: 18074 RVA: 0x00102C38 File Offset: 0x00100E38
		[SecurityCritical]
		internal static void GetMarshalAs(ConstArray nativeType, out UnmanagedType unmanagedType, out VarEnum safeArraySubType, out string safeArrayUserDefinedSubType, out UnmanagedType arraySubType, out int sizeParamIndex, out int sizeConst, out string marshalType, out string marshalCookie, out int iidParamIndex)
		{
			int num;
			int num2;
			int num3;
			MetadataImport._GetMarshalAs(nativeType.Signature, nativeType.Length, out num, out num2, out safeArrayUserDefinedSubType, out num3, out sizeParamIndex, out sizeConst, out marshalType, out marshalCookie, out iidParamIndex);
			unmanagedType = (UnmanagedType)num;
			safeArraySubType = (VarEnum)num2;
			arraySubType = (UnmanagedType)num3;
		}

		// Token: 0x0600469B RID: 18075 RVA: 0x00102C73 File Offset: 0x00100E73
		internal static void ThrowError(int hResult)
		{
			throw new MetadataException(hResult);
		}

		// Token: 0x0600469C RID: 18076 RVA: 0x00102C7B File Offset: 0x00100E7B
		internal MetadataImport(IntPtr metadataImport2, object keepalive)
		{
			this.m_metadataImport2 = metadataImport2;
			this.m_keepalive = keepalive;
		}

		// Token: 0x0600469D RID: 18077
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void _Enum(IntPtr scope, int type, int parent, out MetadataEnumResult result);

		// Token: 0x0600469E RID: 18078 RVA: 0x00102C8B File Offset: 0x00100E8B
		[SecurityCritical]
		public void Enum(MetadataTokenType type, int parent, out MetadataEnumResult result)
		{
			MetadataImport._Enum(this.m_metadataImport2, (int)type, parent, out result);
		}

		// Token: 0x0600469F RID: 18079 RVA: 0x00102C9B File Offset: 0x00100E9B
		[SecurityCritical]
		public void EnumNestedTypes(int mdTypeDef, out MetadataEnumResult result)
		{
			this.Enum(MetadataTokenType.TypeDef, mdTypeDef, out result);
		}

		// Token: 0x060046A0 RID: 18080 RVA: 0x00102CAA File Offset: 0x00100EAA
		[SecurityCritical]
		public void EnumCustomAttributes(int mdToken, out MetadataEnumResult result)
		{
			this.Enum(MetadataTokenType.CustomAttribute, mdToken, out result);
		}

		// Token: 0x060046A1 RID: 18081 RVA: 0x00102CB9 File Offset: 0x00100EB9
		[SecurityCritical]
		public void EnumParams(int mdMethodDef, out MetadataEnumResult result)
		{
			this.Enum(MetadataTokenType.ParamDef, mdMethodDef, out result);
		}

		// Token: 0x060046A2 RID: 18082 RVA: 0x00102CC8 File Offset: 0x00100EC8
		[SecurityCritical]
		public void EnumFields(int mdTypeDef, out MetadataEnumResult result)
		{
			this.Enum(MetadataTokenType.FieldDef, mdTypeDef, out result);
		}

		// Token: 0x060046A3 RID: 18083 RVA: 0x00102CD7 File Offset: 0x00100ED7
		[SecurityCritical]
		public void EnumProperties(int mdTypeDef, out MetadataEnumResult result)
		{
			this.Enum(MetadataTokenType.Property, mdTypeDef, out result);
		}

		// Token: 0x060046A4 RID: 18084 RVA: 0x00102CE6 File Offset: 0x00100EE6
		[SecurityCritical]
		public void EnumEvents(int mdTypeDef, out MetadataEnumResult result)
		{
			this.Enum(MetadataTokenType.Event, mdTypeDef, out result);
		}

		// Token: 0x060046A5 RID: 18085
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern string _GetDefaultValue(IntPtr scope, int mdToken, out long value, out int length, out int corElementType);

		// Token: 0x060046A6 RID: 18086 RVA: 0x00102CF8 File Offset: 0x00100EF8
		[SecurityCritical]
		public string GetDefaultValue(int mdToken, out long value, out int length, out CorElementType corElementType)
		{
			int num;
			string result = MetadataImport._GetDefaultValue(this.m_metadataImport2, mdToken, out value, out length, out num);
			corElementType = (CorElementType)num;
			return result;
		}

		// Token: 0x060046A7 RID: 18087
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private unsafe static extern void _GetUserString(IntPtr scope, int mdToken, void** name, out int length);

		// Token: 0x060046A8 RID: 18088 RVA: 0x00102D1C File Offset: 0x00100F1C
		[SecurityCritical]
		public unsafe string GetUserString(int mdToken)
		{
			void* ptr;
			int num;
			MetadataImport._GetUserString(this.m_metadataImport2, mdToken, &ptr, out num);
			if (ptr == null)
			{
				return null;
			}
			char[] array = new char[num];
			for (int i = 0; i < num; i++)
			{
				array[i] = (char)Marshal.ReadInt16((IntPtr)((void*)((byte*)ptr + (IntPtr)i * 2)));
			}
			return new string(array);
		}

		// Token: 0x060046A9 RID: 18089
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private unsafe static extern void _GetName(IntPtr scope, int mdToken, void** name);

		// Token: 0x060046AA RID: 18090 RVA: 0x00102D70 File Offset: 0x00100F70
		[SecurityCritical]
		public unsafe Utf8String GetName(int mdToken)
		{
			void* pStringHeap;
			MetadataImport._GetName(this.m_metadataImport2, mdToken, &pStringHeap);
			return new Utf8String(pStringHeap);
		}

		// Token: 0x060046AB RID: 18091
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private unsafe static extern void _GetNamespace(IntPtr scope, int mdToken, void** namesp);

		// Token: 0x060046AC RID: 18092 RVA: 0x00102D94 File Offset: 0x00100F94
		[SecurityCritical]
		public unsafe Utf8String GetNamespace(int mdToken)
		{
			void* pStringHeap;
			MetadataImport._GetNamespace(this.m_metadataImport2, mdToken, &pStringHeap);
			return new Utf8String(pStringHeap);
		}

		// Token: 0x060046AD RID: 18093
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private unsafe static extern void _GetEventProps(IntPtr scope, int mdToken, void** name, out int eventAttributes);

		// Token: 0x060046AE RID: 18094 RVA: 0x00102DB8 File Offset: 0x00100FB8
		[SecurityCritical]
		public unsafe void GetEventProps(int mdToken, out void* name, out EventAttributes eventAttributes)
		{
			void* ptr;
			int num;
			MetadataImport._GetEventProps(this.m_metadataImport2, mdToken, &ptr, out num);
			name = ptr;
			eventAttributes = (EventAttributes)num;
		}

		// Token: 0x060046AF RID: 18095
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void _GetFieldDefProps(IntPtr scope, int mdToken, out int fieldAttributes);

		// Token: 0x060046B0 RID: 18096 RVA: 0x00102DDC File Offset: 0x00100FDC
		[SecurityCritical]
		public void GetFieldDefProps(int mdToken, out FieldAttributes fieldAttributes)
		{
			int num;
			MetadataImport._GetFieldDefProps(this.m_metadataImport2, mdToken, out num);
			fieldAttributes = (FieldAttributes)num;
		}

		// Token: 0x060046B1 RID: 18097
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private unsafe static extern void _GetPropertyProps(IntPtr scope, int mdToken, void** name, out int propertyAttributes, out ConstArray signature);

		// Token: 0x060046B2 RID: 18098 RVA: 0x00102DFC File Offset: 0x00100FFC
		[SecurityCritical]
		public unsafe void GetPropertyProps(int mdToken, out void* name, out PropertyAttributes propertyAttributes, out ConstArray signature)
		{
			void* ptr;
			int num;
			MetadataImport._GetPropertyProps(this.m_metadataImport2, mdToken, &ptr, out num, out signature);
			name = ptr;
			propertyAttributes = (PropertyAttributes)num;
		}

		// Token: 0x060046B3 RID: 18099
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void _GetParentToken(IntPtr scope, int mdToken, out int tkParent);

		// Token: 0x060046B4 RID: 18100 RVA: 0x00102E24 File Offset: 0x00101024
		[SecurityCritical]
		public int GetParentToken(int tkToken)
		{
			int result;
			MetadataImport._GetParentToken(this.m_metadataImport2, tkToken, out result);
			return result;
		}

		// Token: 0x060046B5 RID: 18101
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void _GetParamDefProps(IntPtr scope, int parameterToken, out int sequence, out int attributes);

		// Token: 0x060046B6 RID: 18102 RVA: 0x00102E40 File Offset: 0x00101040
		[SecurityCritical]
		public void GetParamDefProps(int parameterToken, out int sequence, out ParameterAttributes attributes)
		{
			int num;
			MetadataImport._GetParamDefProps(this.m_metadataImport2, parameterToken, out sequence, out num);
			attributes = (ParameterAttributes)num;
		}

		// Token: 0x060046B7 RID: 18103
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void _GetGenericParamProps(IntPtr scope, int genericParameter, out int flags);

		// Token: 0x060046B8 RID: 18104 RVA: 0x00102E60 File Offset: 0x00101060
		[SecurityCritical]
		public void GetGenericParamProps(int genericParameter, out GenericParameterAttributes attributes)
		{
			int num;
			MetadataImport._GetGenericParamProps(this.m_metadataImport2, genericParameter, out num);
			attributes = (GenericParameterAttributes)num;
		}

		// Token: 0x060046B9 RID: 18105
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void _GetScopeProps(IntPtr scope, out Guid mvid);

		// Token: 0x060046BA RID: 18106 RVA: 0x00102E7E File Offset: 0x0010107E
		[SecurityCritical]
		public void GetScopeProps(out Guid mvid)
		{
			MetadataImport._GetScopeProps(this.m_metadataImport2, out mvid);
		}

		// Token: 0x060046BB RID: 18107 RVA: 0x00102E8C File Offset: 0x0010108C
		[SecurityCritical]
		public ConstArray GetMethodSignature(MetadataToken token)
		{
			if (token.IsMemberRef)
			{
				return this.GetMemberRefProps(token);
			}
			return this.GetSigOfMethodDef(token);
		}

		// Token: 0x060046BC RID: 18108
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void _GetSigOfMethodDef(IntPtr scope, int methodToken, ref ConstArray signature);

		// Token: 0x060046BD RID: 18109 RVA: 0x00102EB0 File Offset: 0x001010B0
		[SecurityCritical]
		public ConstArray GetSigOfMethodDef(int methodToken)
		{
			ConstArray result = default(ConstArray);
			MetadataImport._GetSigOfMethodDef(this.m_metadataImport2, methodToken, ref result);
			return result;
		}

		// Token: 0x060046BE RID: 18110
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void _GetSignatureFromToken(IntPtr scope, int methodToken, ref ConstArray signature);

		// Token: 0x060046BF RID: 18111 RVA: 0x00102ED4 File Offset: 0x001010D4
		[SecurityCritical]
		public ConstArray GetSignatureFromToken(int token)
		{
			ConstArray result = default(ConstArray);
			MetadataImport._GetSignatureFromToken(this.m_metadataImport2, token, ref result);
			return result;
		}

		// Token: 0x060046C0 RID: 18112
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void _GetMemberRefProps(IntPtr scope, int memberTokenRef, out ConstArray signature);

		// Token: 0x060046C1 RID: 18113 RVA: 0x00102EF8 File Offset: 0x001010F8
		[SecurityCritical]
		public ConstArray GetMemberRefProps(int memberTokenRef)
		{
			ConstArray result = default(ConstArray);
			MetadataImport._GetMemberRefProps(this.m_metadataImport2, memberTokenRef, out result);
			return result;
		}

		// Token: 0x060046C2 RID: 18114
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void _GetCustomAttributeProps(IntPtr scope, int customAttributeToken, out int constructorToken, out ConstArray signature);

		// Token: 0x060046C3 RID: 18115 RVA: 0x00102F1C File Offset: 0x0010111C
		[SecurityCritical]
		public void GetCustomAttributeProps(int customAttributeToken, out int constructorToken, out ConstArray signature)
		{
			MetadataImport._GetCustomAttributeProps(this.m_metadataImport2, customAttributeToken, out constructorToken, out signature);
		}

		// Token: 0x060046C4 RID: 18116
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void _GetClassLayout(IntPtr scope, int typeTokenDef, out int packSize, out int classSize);

		// Token: 0x060046C5 RID: 18117 RVA: 0x00102F2C File Offset: 0x0010112C
		[SecurityCritical]
		public void GetClassLayout(int typeTokenDef, out int packSize, out int classSize)
		{
			MetadataImport._GetClassLayout(this.m_metadataImport2, typeTokenDef, out packSize, out classSize);
		}

		// Token: 0x060046C6 RID: 18118
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern bool _GetFieldOffset(IntPtr scope, int typeTokenDef, int fieldTokenDef, out int offset);

		// Token: 0x060046C7 RID: 18119 RVA: 0x00102F3C File Offset: 0x0010113C
		[SecurityCritical]
		public bool GetFieldOffset(int typeTokenDef, int fieldTokenDef, out int offset)
		{
			return MetadataImport._GetFieldOffset(this.m_metadataImport2, typeTokenDef, fieldTokenDef, out offset);
		}

		// Token: 0x060046C8 RID: 18120
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void _GetSigOfFieldDef(IntPtr scope, int fieldToken, ref ConstArray fieldMarshal);

		// Token: 0x060046C9 RID: 18121 RVA: 0x00102F4C File Offset: 0x0010114C
		[SecurityCritical]
		public ConstArray GetSigOfFieldDef(int fieldToken)
		{
			ConstArray result = default(ConstArray);
			MetadataImport._GetSigOfFieldDef(this.m_metadataImport2, fieldToken, ref result);
			return result;
		}

		// Token: 0x060046CA RID: 18122
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void _GetFieldMarshal(IntPtr scope, int fieldToken, ref ConstArray fieldMarshal);

		// Token: 0x060046CB RID: 18123 RVA: 0x00102F70 File Offset: 0x00101170
		[SecurityCritical]
		public ConstArray GetFieldMarshal(int fieldToken)
		{
			ConstArray result = default(ConstArray);
			MetadataImport._GetFieldMarshal(this.m_metadataImport2, fieldToken, ref result);
			return result;
		}

		// Token: 0x060046CC RID: 18124
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private unsafe static extern void _GetPInvokeMap(IntPtr scope, int token, out int attributes, void** importName, void** importDll);

		// Token: 0x060046CD RID: 18125 RVA: 0x00102F94 File Offset: 0x00101194
		[SecurityCritical]
		public unsafe void GetPInvokeMap(int token, out PInvokeAttributes attributes, out string importName, out string importDll)
		{
			int num;
			void* pStringHeap;
			void* pStringHeap2;
			MetadataImport._GetPInvokeMap(this.m_metadataImport2, token, out num, &pStringHeap, &pStringHeap2);
			importName = new Utf8String(pStringHeap).ToString();
			importDll = new Utf8String(pStringHeap2).ToString();
			attributes = (PInvokeAttributes)num;
		}

		// Token: 0x060046CE RID: 18126
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern bool _IsValidToken(IntPtr scope, int token);

		// Token: 0x060046CF RID: 18127 RVA: 0x00102FE5 File Offset: 0x001011E5
		[SecurityCritical]
		public bool IsValidToken(int token)
		{
			return MetadataImport._IsValidToken(this.m_metadataImport2, token);
		}

		// Token: 0x04001D54 RID: 7508
		private IntPtr m_metadataImport2;

		// Token: 0x04001D55 RID: 7509
		private object m_keepalive;

		// Token: 0x04001D56 RID: 7510
		internal static readonly MetadataImport EmptyImport = new MetadataImport((IntPtr)0, null);
	}
}
