using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Reflection.Emit
{
	// Token: 0x0200065B RID: 1627
	[ClassInterface(ClassInterfaceType.None)]
	[ComDefaultInterface(typeof(_ParameterBuilder))]
	[ComVisible(true)]
	public class ParameterBuilder : _ParameterBuilder
	{
		// Token: 0x06004CB7 RID: 19639 RVA: 0x00116DCC File Offset: 0x00114FCC
		[SecuritySafeCritical]
		[Obsolete("An alternate API is available: Emit the MarshalAs custom attribute instead. http://go.microsoft.com/fwlink/?linkid=14202")]
		public virtual void SetMarshal(UnmanagedMarshal unmanagedMarshal)
		{
			if (unmanagedMarshal == null)
			{
				throw new ArgumentNullException("unmanagedMarshal");
			}
			byte[] array = unmanagedMarshal.InternalGetBytes();
			TypeBuilder.SetFieldMarshal(this.m_methodBuilder.GetModuleBuilder().GetNativeHandle(), this.m_pdToken.Token, array, array.Length);
		}

		// Token: 0x06004CB8 RID: 19640 RVA: 0x00116E14 File Offset: 0x00115014
		[SecuritySafeCritical]
		public virtual void SetConstant(object defaultValue)
		{
			TypeBuilder.SetConstantValue(this.m_methodBuilder.GetModuleBuilder(), this.m_pdToken.Token, (this.m_iPosition == 0) ? this.m_methodBuilder.ReturnType : this.m_methodBuilder.m_parameterTypes[this.m_iPosition - 1], defaultValue);
		}

		// Token: 0x06004CB9 RID: 19641 RVA: 0x00116E68 File Offset: 0x00115068
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
			TypeBuilder.DefineCustomAttribute(this.m_methodBuilder.GetModuleBuilder(), this.m_pdToken.Token, ((ModuleBuilder)this.m_methodBuilder.GetModule()).GetConstructorToken(con).Token, binaryAttribute, false, false);
		}

		// Token: 0x06004CBA RID: 19642 RVA: 0x00116ED3 File Offset: 0x001150D3
		[SecuritySafeCritical]
		public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
		{
			if (customBuilder == null)
			{
				throw new ArgumentNullException("customBuilder");
			}
			customBuilder.CreateCustomAttribute((ModuleBuilder)this.m_methodBuilder.GetModule(), this.m_pdToken.Token);
		}

		// Token: 0x06004CBB RID: 19643 RVA: 0x00116F04 File Offset: 0x00115104
		private ParameterBuilder()
		{
		}

		// Token: 0x06004CBC RID: 19644 RVA: 0x00116F0C File Offset: 0x0011510C
		[SecurityCritical]
		internal ParameterBuilder(MethodBuilder methodBuilder, int sequence, ParameterAttributes attributes, string strParamName)
		{
			this.m_iPosition = sequence;
			this.m_strParamName = strParamName;
			this.m_methodBuilder = methodBuilder;
			this.m_strParamName = strParamName;
			this.m_attributes = attributes;
			this.m_pdToken = new ParameterToken(TypeBuilder.SetParamInfo(this.m_methodBuilder.GetModuleBuilder().GetNativeHandle(), this.m_methodBuilder.GetToken().Token, sequence, attributes, strParamName));
		}

		// Token: 0x06004CBD RID: 19645 RVA: 0x00116F7B File Offset: 0x0011517B
		public virtual ParameterToken GetToken()
		{
			return this.m_pdToken;
		}

		// Token: 0x06004CBE RID: 19646 RVA: 0x00116F83 File Offset: 0x00115183
		void _ParameterBuilder.GetTypeInfoCount(out uint pcTInfo)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06004CBF RID: 19647 RVA: 0x00116F8A File Offset: 0x0011518A
		void _ParameterBuilder.GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06004CC0 RID: 19648 RVA: 0x00116F91 File Offset: 0x00115191
		void _ParameterBuilder.GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06004CC1 RID: 19649 RVA: 0x00116F98 File Offset: 0x00115198
		void _ParameterBuilder.Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr)
		{
			throw new NotImplementedException();
		}

		// Token: 0x17000C02 RID: 3074
		// (get) Token: 0x06004CC2 RID: 19650 RVA: 0x00116F9F File Offset: 0x0011519F
		internal int MetadataTokenInternal
		{
			get
			{
				return this.m_pdToken.Token;
			}
		}

		// Token: 0x17000C03 RID: 3075
		// (get) Token: 0x06004CC3 RID: 19651 RVA: 0x00116FAC File Offset: 0x001151AC
		public virtual string Name
		{
			get
			{
				return this.m_strParamName;
			}
		}

		// Token: 0x17000C04 RID: 3076
		// (get) Token: 0x06004CC4 RID: 19652 RVA: 0x00116FB4 File Offset: 0x001151B4
		public virtual int Position
		{
			get
			{
				return this.m_iPosition;
			}
		}

		// Token: 0x17000C05 RID: 3077
		// (get) Token: 0x06004CC5 RID: 19653 RVA: 0x00116FBC File Offset: 0x001151BC
		public virtual int Attributes
		{
			get
			{
				return (int)this.m_attributes;
			}
		}

		// Token: 0x17000C06 RID: 3078
		// (get) Token: 0x06004CC6 RID: 19654 RVA: 0x00116FC4 File Offset: 0x001151C4
		public bool IsIn
		{
			get
			{
				return (this.m_attributes & ParameterAttributes.In) > ParameterAttributes.None;
			}
		}

		// Token: 0x17000C07 RID: 3079
		// (get) Token: 0x06004CC7 RID: 19655 RVA: 0x00116FD1 File Offset: 0x001151D1
		public bool IsOut
		{
			get
			{
				return (this.m_attributes & ParameterAttributes.Out) > ParameterAttributes.None;
			}
		}

		// Token: 0x17000C08 RID: 3080
		// (get) Token: 0x06004CC8 RID: 19656 RVA: 0x00116FDE File Offset: 0x001151DE
		public bool IsOptional
		{
			get
			{
				return (this.m_attributes & ParameterAttributes.Optional) > ParameterAttributes.None;
			}
		}

		// Token: 0x0400218B RID: 8587
		private string m_strParamName;

		// Token: 0x0400218C RID: 8588
		private int m_iPosition;

		// Token: 0x0400218D RID: 8589
		private ParameterAttributes m_attributes;

		// Token: 0x0400218E RID: 8590
		private MethodBuilder m_methodBuilder;

		// Token: 0x0400218F RID: 8591
		private ParameterToken m_pdToken;
	}
}
