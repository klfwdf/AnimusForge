using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;

namespace System.Reflection.Emit
{
	// Token: 0x0200063B RID: 1595
	[ClassInterface(ClassInterfaceType.None)]
	[ComDefaultInterface(typeof(_FieldBuilder))]
	[ComVisible(true)]
	[HostProtection(SecurityAction.LinkDemand, MayLeakOnAbort = true)]
	public sealed class FieldBuilder : FieldInfo, _FieldBuilder
	{
		// Token: 0x06004A79 RID: 19065 RVA: 0x0010D5F4 File Offset: 0x0010B7F4
		[SecurityCritical]
		internal FieldBuilder(TypeBuilder typeBuilder, string fieldName, Type type, Type[] requiredCustomModifiers, Type[] optionalCustomModifiers, FieldAttributes attributes)
		{
			if (fieldName == null)
			{
				throw new ArgumentNullException("fieldName");
			}
			if (fieldName.Length == 0)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), "fieldName");
			}
			if (fieldName[0] == '\0')
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_IllegalName"), "fieldName");
			}
			if (type == null)
			{
				throw new ArgumentNullException("type");
			}
			if (type == typeof(void))
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_BadFieldType"));
			}
			this.m_fieldName = fieldName;
			this.m_typeBuilder = typeBuilder;
			this.m_fieldType = type;
			this.m_Attributes = attributes & ~FieldAttributes.ReservedMask;
			SignatureHelper fieldSigHelper = SignatureHelper.GetFieldSigHelper(this.m_typeBuilder.Module);
			fieldSigHelper.AddArgument(type, requiredCustomModifiers, optionalCustomModifiers);
			int sigLength;
			byte[] signature = fieldSigHelper.InternalGetSignature(out sigLength);
			this.m_fieldTok = TypeBuilder.DefineField(this.m_typeBuilder.GetModuleBuilder().GetNativeHandle(), typeBuilder.TypeToken.Token, fieldName, signature, sigLength, this.m_Attributes);
			this.m_tkField = new FieldToken(this.m_fieldTok, type);
		}

		// Token: 0x06004A7A RID: 19066 RVA: 0x0010D712 File Offset: 0x0010B912
		[SecurityCritical]
		internal void SetData(byte[] data, int size)
		{
			ModuleBuilder.SetFieldRVAContent(this.m_typeBuilder.GetModuleBuilder().GetNativeHandle(), this.m_tkField.Token, data, size);
		}

		// Token: 0x06004A7B RID: 19067 RVA: 0x0010D736 File Offset: 0x0010B936
		internal TypeBuilder GetTypeBuilder()
		{
			return this.m_typeBuilder;
		}

		// Token: 0x17000BA1 RID: 2977
		// (get) Token: 0x06004A7C RID: 19068 RVA: 0x0010D73E File Offset: 0x0010B93E
		internal int MetadataTokenInternal
		{
			get
			{
				return this.m_fieldTok;
			}
		}

		// Token: 0x17000BA2 RID: 2978
		// (get) Token: 0x06004A7D RID: 19069 RVA: 0x0010D746 File Offset: 0x0010B946
		public override Module Module
		{
			get
			{
				return this.m_typeBuilder.Module;
			}
		}

		// Token: 0x17000BA3 RID: 2979
		// (get) Token: 0x06004A7E RID: 19070 RVA: 0x0010D753 File Offset: 0x0010B953
		public override string Name
		{
			get
			{
				return this.m_fieldName;
			}
		}

		// Token: 0x17000BA4 RID: 2980
		// (get) Token: 0x06004A7F RID: 19071 RVA: 0x0010D75B File Offset: 0x0010B95B
		public override Type DeclaringType
		{
			get
			{
				if (this.m_typeBuilder.m_isHiddenGlobalType)
				{
					return null;
				}
				return this.m_typeBuilder;
			}
		}

		// Token: 0x17000BA5 RID: 2981
		// (get) Token: 0x06004A80 RID: 19072 RVA: 0x0010D772 File Offset: 0x0010B972
		public override Type ReflectedType
		{
			get
			{
				if (this.m_typeBuilder.m_isHiddenGlobalType)
				{
					return null;
				}
				return this.m_typeBuilder;
			}
		}

		// Token: 0x17000BA6 RID: 2982
		// (get) Token: 0x06004A81 RID: 19073 RVA: 0x0010D789 File Offset: 0x0010B989
		public override Type FieldType
		{
			get
			{
				return this.m_fieldType;
			}
		}

		// Token: 0x06004A82 RID: 19074 RVA: 0x0010D791 File Offset: 0x0010B991
		public override object GetValue(object obj)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicModule"));
		}

		// Token: 0x06004A83 RID: 19075 RVA: 0x0010D7A2 File Offset: 0x0010B9A2
		public override void SetValue(object obj, object val, BindingFlags invokeAttr, Binder binder, CultureInfo culture)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicModule"));
		}

		// Token: 0x17000BA7 RID: 2983
		// (get) Token: 0x06004A84 RID: 19076 RVA: 0x0010D7B3 File Offset: 0x0010B9B3
		public override RuntimeFieldHandle FieldHandle
		{
			get
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicModule"));
			}
		}

		// Token: 0x17000BA8 RID: 2984
		// (get) Token: 0x06004A85 RID: 19077 RVA: 0x0010D7C4 File Offset: 0x0010B9C4
		public override FieldAttributes Attributes
		{
			get
			{
				return this.m_Attributes;
			}
		}

		// Token: 0x06004A86 RID: 19078 RVA: 0x0010D7CC File Offset: 0x0010B9CC
		public override object[] GetCustomAttributes(bool inherit)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicModule"));
		}

		// Token: 0x06004A87 RID: 19079 RVA: 0x0010D7DD File Offset: 0x0010B9DD
		public override object[] GetCustomAttributes(Type attributeType, bool inherit)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicModule"));
		}

		// Token: 0x06004A88 RID: 19080 RVA: 0x0010D7EE File Offset: 0x0010B9EE
		public override bool IsDefined(Type attributeType, bool inherit)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicModule"));
		}

		// Token: 0x06004A89 RID: 19081 RVA: 0x0010D7FF File Offset: 0x0010B9FF
		public FieldToken GetToken()
		{
			return this.m_tkField;
		}

		// Token: 0x06004A8A RID: 19082 RVA: 0x0010D808 File Offset: 0x0010BA08
		[SecuritySafeCritical]
		public void SetOffset(int iOffset)
		{
			this.m_typeBuilder.ThrowIfCreated();
			TypeBuilder.SetFieldLayoutOffset(this.m_typeBuilder.GetModuleBuilder().GetNativeHandle(), this.GetToken().Token, iOffset);
		}

		// Token: 0x06004A8B RID: 19083 RVA: 0x0010D844 File Offset: 0x0010BA44
		[SecuritySafeCritical]
		[Obsolete("An alternate API is available: Emit the MarshalAs custom attribute instead. http://go.microsoft.com/fwlink/?linkid=14202")]
		public void SetMarshal(UnmanagedMarshal unmanagedMarshal)
		{
			if (unmanagedMarshal == null)
			{
				throw new ArgumentNullException("unmanagedMarshal");
			}
			this.m_typeBuilder.ThrowIfCreated();
			byte[] array = unmanagedMarshal.InternalGetBytes();
			TypeBuilder.SetFieldMarshal(this.m_typeBuilder.GetModuleBuilder().GetNativeHandle(), this.GetToken().Token, array, array.Length);
		}

		// Token: 0x06004A8C RID: 19084 RVA: 0x0010D898 File Offset: 0x0010BA98
		[SecuritySafeCritical]
		public void SetConstant(object defaultValue)
		{
			this.m_typeBuilder.ThrowIfCreated();
			TypeBuilder.SetConstantValue(this.m_typeBuilder.GetModuleBuilder(), this.GetToken().Token, this.m_fieldType, defaultValue);
		}

		// Token: 0x06004A8D RID: 19085 RVA: 0x0010D8D8 File Offset: 0x0010BAD8
		[SecuritySafeCritical]
		[ComVisible(true)]
		public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
		{
			if (con == null)
			{
				throw new ArgumentNullException("con");
			}
			if (binaryAttribute == null)
			{
				throw new ArgumentNullException("binaryAttribute");
			}
			ModuleBuilder moduleBuilder = this.m_typeBuilder.Module as ModuleBuilder;
			this.m_typeBuilder.ThrowIfCreated();
			TypeBuilder.DefineCustomAttribute(moduleBuilder, this.m_tkField.Token, moduleBuilder.GetConstructorToken(con).Token, binaryAttribute, false, false);
		}

		// Token: 0x06004A8E RID: 19086 RVA: 0x0010D948 File Offset: 0x0010BB48
		[SecuritySafeCritical]
		public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
		{
			if (customBuilder == null)
			{
				throw new ArgumentNullException("customBuilder");
			}
			this.m_typeBuilder.ThrowIfCreated();
			ModuleBuilder mod = this.m_typeBuilder.Module as ModuleBuilder;
			customBuilder.CreateCustomAttribute(mod, this.m_tkField.Token);
		}

		// Token: 0x06004A8F RID: 19087 RVA: 0x0010D991 File Offset: 0x0010BB91
		void _FieldBuilder.GetTypeInfoCount(out uint pcTInfo)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06004A90 RID: 19088 RVA: 0x0010D998 File Offset: 0x0010BB98
		void _FieldBuilder.GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06004A91 RID: 19089 RVA: 0x0010D99F File Offset: 0x0010BB9F
		void _FieldBuilder.GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06004A92 RID: 19090 RVA: 0x0010D9A6 File Offset: 0x0010BBA6
		void _FieldBuilder.Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr)
		{
			throw new NotImplementedException();
		}

		// Token: 0x04001EB6 RID: 7862
		private int m_fieldTok;

		// Token: 0x04001EB7 RID: 7863
		private FieldToken m_tkField;

		// Token: 0x04001EB8 RID: 7864
		private TypeBuilder m_typeBuilder;

		// Token: 0x04001EB9 RID: 7865
		private string m_fieldName;

		// Token: 0x04001EBA RID: 7866
		private FieldAttributes m_Attributes;

		// Token: 0x04001EBB RID: 7867
		private Type m_fieldType;
	}
}
