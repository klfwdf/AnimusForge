using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;

namespace System.Reflection.Emit
{
	// Token: 0x0200065D RID: 1629
	[ClassInterface(ClassInterfaceType.None)]
	[ComDefaultInterface(typeof(_PropertyBuilder))]
	[ComVisible(true)]
	[HostProtection(SecurityAction.LinkDemand, MayLeakOnAbort = true)]
	public sealed class PropertyBuilder : PropertyInfo, _PropertyBuilder
	{
		// Token: 0x06004CD1 RID: 19665 RVA: 0x00117045 File Offset: 0x00115245
		private PropertyBuilder()
		{
		}

		// Token: 0x06004CD2 RID: 19666 RVA: 0x00117050 File Offset: 0x00115250
		internal PropertyBuilder(ModuleBuilder mod, string name, SignatureHelper sig, PropertyAttributes attr, Type returnType, PropertyToken prToken, TypeBuilder containingType)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			if (name.Length == 0)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), "name");
			}
			if (name[0] == '\0')
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_IllegalName"), "name");
			}
			this.m_name = name;
			this.m_moduleBuilder = mod;
			this.m_signature = sig;
			this.m_attributes = attr;
			this.m_returnType = returnType;
			this.m_prToken = prToken;
			this.m_tkProperty = prToken.Token;
			this.m_containingType = containingType;
		}

		// Token: 0x06004CD3 RID: 19667 RVA: 0x001170EE File Offset: 0x001152EE
		[SecuritySafeCritical]
		public void SetConstant(object defaultValue)
		{
			this.m_containingType.ThrowIfCreated();
			TypeBuilder.SetConstantValue(this.m_moduleBuilder, this.m_prToken.Token, this.m_returnType, defaultValue);
		}

		// Token: 0x17000C0A RID: 3082
		// (get) Token: 0x06004CD4 RID: 19668 RVA: 0x00117118 File Offset: 0x00115318
		public PropertyToken PropertyToken
		{
			get
			{
				return this.m_prToken;
			}
		}

		// Token: 0x17000C0B RID: 3083
		// (get) Token: 0x06004CD5 RID: 19669 RVA: 0x00117120 File Offset: 0x00115320
		internal int MetadataTokenInternal
		{
			get
			{
				return this.m_tkProperty;
			}
		}

		// Token: 0x17000C0C RID: 3084
		// (get) Token: 0x06004CD6 RID: 19670 RVA: 0x00117128 File Offset: 0x00115328
		public override Module Module
		{
			get
			{
				return this.m_containingType.Module;
			}
		}

		// Token: 0x06004CD7 RID: 19671 RVA: 0x00117138 File Offset: 0x00115338
		[SecurityCritical]
		private void SetMethodSemantics(MethodBuilder mdBuilder, MethodSemanticsAttributes semantics)
		{
			if (mdBuilder == null)
			{
				throw new ArgumentNullException("mdBuilder");
			}
			this.m_containingType.ThrowIfCreated();
			TypeBuilder.DefineMethodSemantics(this.m_moduleBuilder.GetNativeHandle(), this.m_prToken.Token, semantics, mdBuilder.GetToken().Token);
		}

		// Token: 0x06004CD8 RID: 19672 RVA: 0x0011718E File Offset: 0x0011538E
		[SecuritySafeCritical]
		public void SetGetMethod(MethodBuilder mdBuilder)
		{
			this.SetMethodSemantics(mdBuilder, MethodSemanticsAttributes.Getter);
			this.m_getMethod = mdBuilder;
		}

		// Token: 0x06004CD9 RID: 19673 RVA: 0x0011719F File Offset: 0x0011539F
		[SecuritySafeCritical]
		public void SetSetMethod(MethodBuilder mdBuilder)
		{
			this.SetMethodSemantics(mdBuilder, MethodSemanticsAttributes.Setter);
			this.m_setMethod = mdBuilder;
		}

		// Token: 0x06004CDA RID: 19674 RVA: 0x001171B0 File Offset: 0x001153B0
		[SecuritySafeCritical]
		public void AddOtherMethod(MethodBuilder mdBuilder)
		{
			this.SetMethodSemantics(mdBuilder, MethodSemanticsAttributes.Other);
		}

		// Token: 0x06004CDB RID: 19675 RVA: 0x001171BC File Offset: 0x001153BC
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
			this.m_containingType.ThrowIfCreated();
			TypeBuilder.DefineCustomAttribute(this.m_moduleBuilder, this.m_prToken.Token, this.m_moduleBuilder.GetConstructorToken(con).Token, binaryAttribute, false, false);
		}

		// Token: 0x06004CDC RID: 19676 RVA: 0x00117223 File Offset: 0x00115423
		[SecuritySafeCritical]
		public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
		{
			if (customBuilder == null)
			{
				throw new ArgumentNullException("customBuilder");
			}
			this.m_containingType.ThrowIfCreated();
			customBuilder.CreateCustomAttribute(this.m_moduleBuilder, this.m_prToken.Token);
		}

		// Token: 0x06004CDD RID: 19677 RVA: 0x00117255 File Offset: 0x00115455
		public override object GetValue(object obj, object[] index)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicModule"));
		}

		// Token: 0x06004CDE RID: 19678 RVA: 0x00117266 File Offset: 0x00115466
		public override object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicModule"));
		}

		// Token: 0x06004CDF RID: 19679 RVA: 0x00117277 File Offset: 0x00115477
		public override void SetValue(object obj, object value, object[] index)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicModule"));
		}

		// Token: 0x06004CE0 RID: 19680 RVA: 0x00117288 File Offset: 0x00115488
		public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicModule"));
		}

		// Token: 0x06004CE1 RID: 19681 RVA: 0x00117299 File Offset: 0x00115499
		public override MethodInfo[] GetAccessors(bool nonPublic)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicModule"));
		}

		// Token: 0x06004CE2 RID: 19682 RVA: 0x001172AA File Offset: 0x001154AA
		public override MethodInfo GetGetMethod(bool nonPublic)
		{
			if (nonPublic || this.m_getMethod == null)
			{
				return this.m_getMethod;
			}
			if ((this.m_getMethod.Attributes & MethodAttributes.Public) == MethodAttributes.Public)
			{
				return this.m_getMethod;
			}
			return null;
		}

		// Token: 0x06004CE3 RID: 19683 RVA: 0x001172DC File Offset: 0x001154DC
		public override MethodInfo GetSetMethod(bool nonPublic)
		{
			if (nonPublic || this.m_setMethod == null)
			{
				return this.m_setMethod;
			}
			if ((this.m_setMethod.Attributes & MethodAttributes.Public) == MethodAttributes.Public)
			{
				return this.m_setMethod;
			}
			return null;
		}

		// Token: 0x06004CE4 RID: 19684 RVA: 0x0011730E File Offset: 0x0011550E
		public override ParameterInfo[] GetIndexParameters()
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicModule"));
		}

		// Token: 0x17000C0D RID: 3085
		// (get) Token: 0x06004CE5 RID: 19685 RVA: 0x0011731F File Offset: 0x0011551F
		public override Type PropertyType
		{
			get
			{
				return this.m_returnType;
			}
		}

		// Token: 0x17000C0E RID: 3086
		// (get) Token: 0x06004CE6 RID: 19686 RVA: 0x00117327 File Offset: 0x00115527
		public override PropertyAttributes Attributes
		{
			get
			{
				return this.m_attributes;
			}
		}

		// Token: 0x17000C0F RID: 3087
		// (get) Token: 0x06004CE7 RID: 19687 RVA: 0x0011732F File Offset: 0x0011552F
		public override bool CanRead
		{
			get
			{
				return this.m_getMethod != null;
			}
		}

		// Token: 0x17000C10 RID: 3088
		// (get) Token: 0x06004CE8 RID: 19688 RVA: 0x00117342 File Offset: 0x00115542
		public override bool CanWrite
		{
			get
			{
				return this.m_setMethod != null;
			}
		}

		// Token: 0x06004CE9 RID: 19689 RVA: 0x00117355 File Offset: 0x00115555
		public override object[] GetCustomAttributes(bool inherit)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicModule"));
		}

		// Token: 0x06004CEA RID: 19690 RVA: 0x00117366 File Offset: 0x00115566
		public override object[] GetCustomAttributes(Type attributeType, bool inherit)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicModule"));
		}

		// Token: 0x06004CEB RID: 19691 RVA: 0x00117377 File Offset: 0x00115577
		public override bool IsDefined(Type attributeType, bool inherit)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicModule"));
		}

		// Token: 0x06004CEC RID: 19692 RVA: 0x00117388 File Offset: 0x00115588
		void _PropertyBuilder.GetTypeInfoCount(out uint pcTInfo)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06004CED RID: 19693 RVA: 0x0011738F File Offset: 0x0011558F
		void _PropertyBuilder.GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06004CEE RID: 19694 RVA: 0x00117396 File Offset: 0x00115596
		void _PropertyBuilder.GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06004CEF RID: 19695 RVA: 0x0011739D File Offset: 0x0011559D
		void _PropertyBuilder.Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr)
		{
			throw new NotImplementedException();
		}

		// Token: 0x17000C11 RID: 3089
		// (get) Token: 0x06004CF0 RID: 19696 RVA: 0x001173A4 File Offset: 0x001155A4
		public override string Name
		{
			get
			{
				return this.m_name;
			}
		}

		// Token: 0x17000C12 RID: 3090
		// (get) Token: 0x06004CF1 RID: 19697 RVA: 0x001173AC File Offset: 0x001155AC
		public override Type DeclaringType
		{
			get
			{
				return this.m_containingType;
			}
		}

		// Token: 0x17000C13 RID: 3091
		// (get) Token: 0x06004CF2 RID: 19698 RVA: 0x001173B4 File Offset: 0x001155B4
		public override Type ReflectedType
		{
			get
			{
				return this.m_containingType;
			}
		}

		// Token: 0x04002192 RID: 8594
		private string m_name;

		// Token: 0x04002193 RID: 8595
		private PropertyToken m_prToken;

		// Token: 0x04002194 RID: 8596
		private int m_tkProperty;

		// Token: 0x04002195 RID: 8597
		private ModuleBuilder m_moduleBuilder;

		// Token: 0x04002196 RID: 8598
		private SignatureHelper m_signature;

		// Token: 0x04002197 RID: 8599
		private PropertyAttributes m_attributes;

		// Token: 0x04002198 RID: 8600
		private Type m_returnType;

		// Token: 0x04002199 RID: 8601
		private MethodInfo m_getMethod;

		// Token: 0x0400219A RID: 8602
		private MethodInfo m_setMethod;

		// Token: 0x0400219B RID: 8603
		private TypeBuilder m_containingType;
	}
}
