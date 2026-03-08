using System;
using System.Reflection;
using System.Security;

namespace System.Runtime.InteropServices
{
	// Token: 0x0200092B RID: 2347
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue, Inherited = false)]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public sealed class MarshalAsAttribute : Attribute
	{
		// Token: 0x0600601E RID: 24606 RVA: 0x0014B99F File Offset: 0x00149B9F
		[SecurityCritical]
		internal static Attribute GetCustomAttribute(RuntimeParameterInfo parameter)
		{
			return MarshalAsAttribute.GetCustomAttribute(parameter.MetadataToken, parameter.GetRuntimeModule());
		}

		// Token: 0x0600601F RID: 24607 RVA: 0x0014B9B2 File Offset: 0x00149BB2
		[SecurityCritical]
		internal static bool IsDefined(RuntimeParameterInfo parameter)
		{
			return MarshalAsAttribute.GetCustomAttribute(parameter) != null;
		}

		// Token: 0x06006020 RID: 24608 RVA: 0x0014B9BD File Offset: 0x00149BBD
		[SecurityCritical]
		internal static Attribute GetCustomAttribute(RuntimeFieldInfo field)
		{
			return MarshalAsAttribute.GetCustomAttribute(field.MetadataToken, field.GetRuntimeModule());
		}

		// Token: 0x06006021 RID: 24609 RVA: 0x0014B9D0 File Offset: 0x00149BD0
		[SecurityCritical]
		internal static bool IsDefined(RuntimeFieldInfo field)
		{
			return MarshalAsAttribute.GetCustomAttribute(field) != null;
		}

		// Token: 0x06006022 RID: 24610 RVA: 0x0014B9DC File Offset: 0x00149BDC
		[SecurityCritical]
		internal static Attribute GetCustomAttribute(int token, RuntimeModule scope)
		{
			int num = 0;
			int sizeConst = 0;
			string text = null;
			string marshalCookie = null;
			string text2 = null;
			int iidParamIndex = 0;
			ConstArray fieldMarshal = ModuleHandle.GetMetadataImport(scope.GetNativeHandle()).GetFieldMarshal(token);
			if (fieldMarshal.Length == 0)
			{
				return null;
			}
			UnmanagedType val;
			VarEnum safeArraySubType;
			UnmanagedType arraySubType;
			MetadataImport.GetMarshalAs(fieldMarshal, out val, out safeArraySubType, out text2, out arraySubType, out num, out sizeConst, out text, out marshalCookie, out iidParamIndex);
			RuntimeType safeArrayUserDefinedSubType = ((text2 == null || text2.Length == 0) ? null : RuntimeTypeHandle.GetTypeByNameUsingCARules(text2, scope));
			RuntimeType marshalTypeRef = null;
			try
			{
				marshalTypeRef = ((text == null) ? null : RuntimeTypeHandle.GetTypeByNameUsingCARules(text, scope));
			}
			catch (TypeLoadException)
			{
			}
			return new MarshalAsAttribute(val, safeArraySubType, safeArrayUserDefinedSubType, arraySubType, (short)num, sizeConst, text, marshalTypeRef, marshalCookie, iidParamIndex);
		}

		// Token: 0x06006023 RID: 24611 RVA: 0x0014BA90 File Offset: 0x00149C90
		internal MarshalAsAttribute(UnmanagedType val, VarEnum safeArraySubType, RuntimeType safeArrayUserDefinedSubType, UnmanagedType arraySubType, short sizeParamIndex, int sizeConst, string marshalType, RuntimeType marshalTypeRef, string marshalCookie, int iidParamIndex)
		{
			this._val = val;
			this.SafeArraySubType = safeArraySubType;
			this.SafeArrayUserDefinedSubType = safeArrayUserDefinedSubType;
			this.IidParameterIndex = iidParamIndex;
			this.ArraySubType = arraySubType;
			this.SizeParamIndex = sizeParamIndex;
			this.SizeConst = sizeConst;
			this.MarshalType = marshalType;
			this.MarshalTypeRef = marshalTypeRef;
			this.MarshalCookie = marshalCookie;
		}

		// Token: 0x06006024 RID: 24612 RVA: 0x0014BAF0 File Offset: 0x00149CF0
		[__DynamicallyInvokable]
		public MarshalAsAttribute(UnmanagedType unmanagedType)
		{
			this._val = unmanagedType;
		}

		// Token: 0x06006025 RID: 24613 RVA: 0x0014BAFF File Offset: 0x00149CFF
		[__DynamicallyInvokable]
		public MarshalAsAttribute(short unmanagedType)
		{
			this._val = (UnmanagedType)unmanagedType;
		}

		// Token: 0x170010DF RID: 4319
		// (get) Token: 0x06006026 RID: 24614 RVA: 0x0014BB0E File Offset: 0x00149D0E
		[__DynamicallyInvokable]
		public UnmanagedType Value
		{
			[__DynamicallyInvokable]
			get
			{
				return this._val;
			}
		}

		// Token: 0x04002AFF RID: 11007
		internal UnmanagedType _val;

		// Token: 0x04002B00 RID: 11008
		[__DynamicallyInvokable]
		public VarEnum SafeArraySubType;

		// Token: 0x04002B01 RID: 11009
		[__DynamicallyInvokable]
		public Type SafeArrayUserDefinedSubType;

		// Token: 0x04002B02 RID: 11010
		[__DynamicallyInvokable]
		public int IidParameterIndex;

		// Token: 0x04002B03 RID: 11011
		[__DynamicallyInvokable]
		public UnmanagedType ArraySubType;

		// Token: 0x04002B04 RID: 11012
		[__DynamicallyInvokable]
		public short SizeParamIndex;

		// Token: 0x04002B05 RID: 11013
		[__DynamicallyInvokable]
		public int SizeConst;

		// Token: 0x04002B06 RID: 11014
		[ComVisible(true)]
		[__DynamicallyInvokable]
		public string MarshalType;

		// Token: 0x04002B07 RID: 11015
		[ComVisible(true)]
		[__DynamicallyInvokable]
		public Type MarshalTypeRef;

		// Token: 0x04002B08 RID: 11016
		[__DynamicallyInvokable]
		public string MarshalCookie;
	}
}
