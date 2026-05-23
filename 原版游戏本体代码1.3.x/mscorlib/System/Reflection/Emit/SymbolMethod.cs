using System;
using System.Globalization;
using System.Security;

namespace System.Reflection.Emit
{
	// Token: 0x0200064C RID: 1612
	internal sealed class SymbolMethod : MethodInfo
	{
		// Token: 0x06004BD5 RID: 19413 RVA: 0x00112344 File Offset: 0x00110544
		[SecurityCritical]
		internal SymbolMethod(ModuleBuilder mod, MethodToken token, Type arrayClass, string methodName, CallingConventions callingConvention, Type returnType, Type[] parameterTypes)
		{
			this.m_mdMethod = token;
			this.m_returnType = returnType;
			if (parameterTypes != null)
			{
				this.m_parameterTypes = new Type[parameterTypes.Length];
				Array.Copy(parameterTypes, this.m_parameterTypes, parameterTypes.Length);
			}
			else
			{
				this.m_parameterTypes = EmptyArray<Type>.Value;
			}
			this.m_module = mod;
			this.m_containingType = arrayClass;
			this.m_name = methodName;
			this.m_callingConvention = callingConvention;
			this.m_signature = SignatureHelper.GetMethodSigHelper(mod, callingConvention, returnType, null, null, parameterTypes, null, null);
		}

		// Token: 0x06004BD6 RID: 19414 RVA: 0x001123CB File Offset: 0x001105CB
		internal override Type[] GetParameterTypes()
		{
			return this.m_parameterTypes;
		}

		// Token: 0x06004BD7 RID: 19415 RVA: 0x001123D3 File Offset: 0x001105D3
		internal MethodToken GetToken(ModuleBuilder mod)
		{
			return mod.GetArrayMethodToken(this.m_containingType, this.m_name, this.m_callingConvention, this.m_returnType, this.m_parameterTypes);
		}

		// Token: 0x17000BE5 RID: 3045
		// (get) Token: 0x06004BD8 RID: 19416 RVA: 0x001123F9 File Offset: 0x001105F9
		public override Module Module
		{
			get
			{
				return this.m_module;
			}
		}

		// Token: 0x17000BE6 RID: 3046
		// (get) Token: 0x06004BD9 RID: 19417 RVA: 0x00112401 File Offset: 0x00110601
		public override Type ReflectedType
		{
			get
			{
				return this.m_containingType;
			}
		}

		// Token: 0x17000BE7 RID: 3047
		// (get) Token: 0x06004BDA RID: 19418 RVA: 0x00112409 File Offset: 0x00110609
		public override string Name
		{
			get
			{
				return this.m_name;
			}
		}

		// Token: 0x17000BE8 RID: 3048
		// (get) Token: 0x06004BDB RID: 19419 RVA: 0x00112411 File Offset: 0x00110611
		public override Type DeclaringType
		{
			get
			{
				return this.m_containingType;
			}
		}

		// Token: 0x06004BDC RID: 19420 RVA: 0x00112419 File Offset: 0x00110619
		public override ParameterInfo[] GetParameters()
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_SymbolMethod"));
		}

		// Token: 0x06004BDD RID: 19421 RVA: 0x0011242A File Offset: 0x0011062A
		public override MethodImplAttributes GetMethodImplementationFlags()
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_SymbolMethod"));
		}

		// Token: 0x17000BE9 RID: 3049
		// (get) Token: 0x06004BDE RID: 19422 RVA: 0x0011243B File Offset: 0x0011063B
		public override MethodAttributes Attributes
		{
			get
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_SymbolMethod"));
			}
		}

		// Token: 0x17000BEA RID: 3050
		// (get) Token: 0x06004BDF RID: 19423 RVA: 0x0011244C File Offset: 0x0011064C
		public override CallingConventions CallingConvention
		{
			get
			{
				return this.m_callingConvention;
			}
		}

		// Token: 0x17000BEB RID: 3051
		// (get) Token: 0x06004BE0 RID: 19424 RVA: 0x00112454 File Offset: 0x00110654
		public override RuntimeMethodHandle MethodHandle
		{
			get
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_SymbolMethod"));
			}
		}

		// Token: 0x17000BEC RID: 3052
		// (get) Token: 0x06004BE1 RID: 19425 RVA: 0x00112465 File Offset: 0x00110665
		public override Type ReturnType
		{
			get
			{
				return this.m_returnType;
			}
		}

		// Token: 0x17000BED RID: 3053
		// (get) Token: 0x06004BE2 RID: 19426 RVA: 0x0011246D File Offset: 0x0011066D
		public override ICustomAttributeProvider ReturnTypeCustomAttributes
		{
			get
			{
				return null;
			}
		}

		// Token: 0x06004BE3 RID: 19427 RVA: 0x00112470 File Offset: 0x00110670
		public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_SymbolMethod"));
		}

		// Token: 0x06004BE4 RID: 19428 RVA: 0x00112481 File Offset: 0x00110681
		public override MethodInfo GetBaseDefinition()
		{
			return this;
		}

		// Token: 0x06004BE5 RID: 19429 RVA: 0x00112484 File Offset: 0x00110684
		public override object[] GetCustomAttributes(bool inherit)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_SymbolMethod"));
		}

		// Token: 0x06004BE6 RID: 19430 RVA: 0x00112495 File Offset: 0x00110695
		public override object[] GetCustomAttributes(Type attributeType, bool inherit)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_SymbolMethod"));
		}

		// Token: 0x06004BE7 RID: 19431 RVA: 0x001124A6 File Offset: 0x001106A6
		public override bool IsDefined(Type attributeType, bool inherit)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_SymbolMethod"));
		}

		// Token: 0x06004BE8 RID: 19432 RVA: 0x001124B7 File Offset: 0x001106B7
		public Module GetModule()
		{
			return this.m_module;
		}

		// Token: 0x06004BE9 RID: 19433 RVA: 0x001124BF File Offset: 0x001106BF
		public MethodToken GetToken()
		{
			return this.m_mdMethod;
		}

		// Token: 0x04001F47 RID: 8007
		private ModuleBuilder m_module;

		// Token: 0x04001F48 RID: 8008
		private Type m_containingType;

		// Token: 0x04001F49 RID: 8009
		private string m_name;

		// Token: 0x04001F4A RID: 8010
		private CallingConventions m_callingConvention;

		// Token: 0x04001F4B RID: 8011
		private Type m_returnType;

		// Token: 0x04001F4C RID: 8012
		private MethodToken m_mdMethod;

		// Token: 0x04001F4D RID: 8013
		private Type[] m_parameterTypes;

		// Token: 0x04001F4E RID: 8014
		private SignatureHelper m_signature;
	}
}
