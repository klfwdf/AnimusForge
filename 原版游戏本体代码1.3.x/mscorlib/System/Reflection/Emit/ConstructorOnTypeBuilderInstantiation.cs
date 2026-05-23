using System;
using System.Globalization;

namespace System.Reflection.Emit
{
	// Token: 0x02000669 RID: 1641
	internal sealed class ConstructorOnTypeBuilderInstantiation : ConstructorInfo
	{
		// Token: 0x06004EDB RID: 20187 RVA: 0x0011BEA8 File Offset: 0x0011A0A8
		internal static ConstructorInfo GetConstructor(ConstructorInfo Constructor, TypeBuilderInstantiation type)
		{
			return new ConstructorOnTypeBuilderInstantiation(Constructor, type);
		}

		// Token: 0x06004EDC RID: 20188 RVA: 0x0011BEB1 File Offset: 0x0011A0B1
		internal ConstructorOnTypeBuilderInstantiation(ConstructorInfo constructor, TypeBuilderInstantiation type)
		{
			this.m_ctor = constructor;
			this.m_type = type;
		}

		// Token: 0x06004EDD RID: 20189 RVA: 0x0011BEC7 File Offset: 0x0011A0C7
		internal override Type[] GetParameterTypes()
		{
			return this.m_ctor.GetParameterTypes();
		}

		// Token: 0x06004EDE RID: 20190 RVA: 0x0011BED4 File Offset: 0x0011A0D4
		internal override Type GetReturnType()
		{
			return this.DeclaringType;
		}

		// Token: 0x17000C7C RID: 3196
		// (get) Token: 0x06004EDF RID: 20191 RVA: 0x0011BEDC File Offset: 0x0011A0DC
		public override MemberTypes MemberType
		{
			get
			{
				return this.m_ctor.MemberType;
			}
		}

		// Token: 0x17000C7D RID: 3197
		// (get) Token: 0x06004EE0 RID: 20192 RVA: 0x0011BEE9 File Offset: 0x0011A0E9
		public override string Name
		{
			get
			{
				return this.m_ctor.Name;
			}
		}

		// Token: 0x17000C7E RID: 3198
		// (get) Token: 0x06004EE1 RID: 20193 RVA: 0x0011BEF6 File Offset: 0x0011A0F6
		public override Type DeclaringType
		{
			get
			{
				return this.m_type;
			}
		}

		// Token: 0x17000C7F RID: 3199
		// (get) Token: 0x06004EE2 RID: 20194 RVA: 0x0011BEFE File Offset: 0x0011A0FE
		public override Type ReflectedType
		{
			get
			{
				return this.m_type;
			}
		}

		// Token: 0x06004EE3 RID: 20195 RVA: 0x0011BF06 File Offset: 0x0011A106
		public override object[] GetCustomAttributes(bool inherit)
		{
			return this.m_ctor.GetCustomAttributes(inherit);
		}

		// Token: 0x06004EE4 RID: 20196 RVA: 0x0011BF14 File Offset: 0x0011A114
		public override object[] GetCustomAttributes(Type attributeType, bool inherit)
		{
			return this.m_ctor.GetCustomAttributes(attributeType, inherit);
		}

		// Token: 0x06004EE5 RID: 20197 RVA: 0x0011BF23 File Offset: 0x0011A123
		public override bool IsDefined(Type attributeType, bool inherit)
		{
			return this.m_ctor.IsDefined(attributeType, inherit);
		}

		// Token: 0x17000C80 RID: 3200
		// (get) Token: 0x06004EE6 RID: 20198 RVA: 0x0011BF34 File Offset: 0x0011A134
		internal int MetadataTokenInternal
		{
			get
			{
				ConstructorBuilder constructorBuilder = this.m_ctor as ConstructorBuilder;
				if (constructorBuilder != null)
				{
					return constructorBuilder.MetadataTokenInternal;
				}
				return this.m_ctor.MetadataToken;
			}
		}

		// Token: 0x17000C81 RID: 3201
		// (get) Token: 0x06004EE7 RID: 20199 RVA: 0x0011BF68 File Offset: 0x0011A168
		public override Module Module
		{
			get
			{
				return this.m_ctor.Module;
			}
		}

		// Token: 0x06004EE8 RID: 20200 RVA: 0x0011BF75 File Offset: 0x0011A175
		public new Type GetType()
		{
			return base.GetType();
		}

		// Token: 0x06004EE9 RID: 20201 RVA: 0x0011BF7D File Offset: 0x0011A17D
		public override ParameterInfo[] GetParameters()
		{
			return this.m_ctor.GetParameters();
		}

		// Token: 0x06004EEA RID: 20202 RVA: 0x0011BF8A File Offset: 0x0011A18A
		public override MethodImplAttributes GetMethodImplementationFlags()
		{
			return this.m_ctor.GetMethodImplementationFlags();
		}

		// Token: 0x17000C82 RID: 3202
		// (get) Token: 0x06004EEB RID: 20203 RVA: 0x0011BF97 File Offset: 0x0011A197
		public override RuntimeMethodHandle MethodHandle
		{
			get
			{
				return this.m_ctor.MethodHandle;
			}
		}

		// Token: 0x17000C83 RID: 3203
		// (get) Token: 0x06004EEC RID: 20204 RVA: 0x0011BFA4 File Offset: 0x0011A1A4
		public override MethodAttributes Attributes
		{
			get
			{
				return this.m_ctor.Attributes;
			}
		}

		// Token: 0x06004EED RID: 20205 RVA: 0x0011BFB1 File Offset: 0x0011A1B1
		public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
		{
			throw new NotSupportedException();
		}

		// Token: 0x17000C84 RID: 3204
		// (get) Token: 0x06004EEE RID: 20206 RVA: 0x0011BFB8 File Offset: 0x0011A1B8
		public override CallingConventions CallingConvention
		{
			get
			{
				return this.m_ctor.CallingConvention;
			}
		}

		// Token: 0x06004EEF RID: 20207 RVA: 0x0011BFC5 File Offset: 0x0011A1C5
		public override Type[] GetGenericArguments()
		{
			return this.m_ctor.GetGenericArguments();
		}

		// Token: 0x17000C85 RID: 3205
		// (get) Token: 0x06004EF0 RID: 20208 RVA: 0x0011BFD2 File Offset: 0x0011A1D2
		public override bool IsGenericMethodDefinition
		{
			get
			{
				return false;
			}
		}

		// Token: 0x17000C86 RID: 3206
		// (get) Token: 0x06004EF1 RID: 20209 RVA: 0x0011BFD5 File Offset: 0x0011A1D5
		public override bool ContainsGenericParameters
		{
			get
			{
				return false;
			}
		}

		// Token: 0x17000C87 RID: 3207
		// (get) Token: 0x06004EF2 RID: 20210 RVA: 0x0011BFD8 File Offset: 0x0011A1D8
		public override bool IsGenericMethod
		{
			get
			{
				return false;
			}
		}

		// Token: 0x06004EF3 RID: 20211 RVA: 0x0011BFDB File Offset: 0x0011A1DB
		public override object Invoke(BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
		{
			throw new InvalidOperationException();
		}

		// Token: 0x040021D8 RID: 8664
		internal ConstructorInfo m_ctor;

		// Token: 0x040021D9 RID: 8665
		private TypeBuilderInstantiation m_type;
	}
}
