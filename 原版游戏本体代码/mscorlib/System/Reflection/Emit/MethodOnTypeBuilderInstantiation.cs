using System;
using System.Globalization;

namespace System.Reflection.Emit
{
	// Token: 0x02000668 RID: 1640
	internal sealed class MethodOnTypeBuilderInstantiation : MethodInfo
	{
		// Token: 0x06004EBE RID: 20158 RVA: 0x0011BD16 File Offset: 0x00119F16
		internal static MethodInfo GetMethod(MethodInfo method, TypeBuilderInstantiation type)
		{
			return new MethodOnTypeBuilderInstantiation(method, type);
		}

		// Token: 0x06004EBF RID: 20159 RVA: 0x0011BD1F File Offset: 0x00119F1F
		internal MethodOnTypeBuilderInstantiation(MethodInfo method, TypeBuilderInstantiation type)
		{
			this.m_method = method;
			this.m_type = type;
		}

		// Token: 0x06004EC0 RID: 20160 RVA: 0x0011BD35 File Offset: 0x00119F35
		internal override Type[] GetParameterTypes()
		{
			return this.m_method.GetParameterTypes();
		}

		// Token: 0x17000C6D RID: 3181
		// (get) Token: 0x06004EC1 RID: 20161 RVA: 0x0011BD42 File Offset: 0x00119F42
		public override MemberTypes MemberType
		{
			get
			{
				return this.m_method.MemberType;
			}
		}

		// Token: 0x17000C6E RID: 3182
		// (get) Token: 0x06004EC2 RID: 20162 RVA: 0x0011BD4F File Offset: 0x00119F4F
		public override string Name
		{
			get
			{
				return this.m_method.Name;
			}
		}

		// Token: 0x17000C6F RID: 3183
		// (get) Token: 0x06004EC3 RID: 20163 RVA: 0x0011BD5C File Offset: 0x00119F5C
		public override Type DeclaringType
		{
			get
			{
				return this.m_type;
			}
		}

		// Token: 0x17000C70 RID: 3184
		// (get) Token: 0x06004EC4 RID: 20164 RVA: 0x0011BD64 File Offset: 0x00119F64
		public override Type ReflectedType
		{
			get
			{
				return this.m_type;
			}
		}

		// Token: 0x06004EC5 RID: 20165 RVA: 0x0011BD6C File Offset: 0x00119F6C
		public override object[] GetCustomAttributes(bool inherit)
		{
			return this.m_method.GetCustomAttributes(inherit);
		}

		// Token: 0x06004EC6 RID: 20166 RVA: 0x0011BD7A File Offset: 0x00119F7A
		public override object[] GetCustomAttributes(Type attributeType, bool inherit)
		{
			return this.m_method.GetCustomAttributes(attributeType, inherit);
		}

		// Token: 0x06004EC7 RID: 20167 RVA: 0x0011BD89 File Offset: 0x00119F89
		public override bool IsDefined(Type attributeType, bool inherit)
		{
			return this.m_method.IsDefined(attributeType, inherit);
		}

		// Token: 0x17000C71 RID: 3185
		// (get) Token: 0x06004EC8 RID: 20168 RVA: 0x0011BD98 File Offset: 0x00119F98
		internal int MetadataTokenInternal
		{
			get
			{
				MethodBuilder methodBuilder = this.m_method as MethodBuilder;
				if (methodBuilder != null)
				{
					return methodBuilder.MetadataTokenInternal;
				}
				return this.m_method.MetadataToken;
			}
		}

		// Token: 0x17000C72 RID: 3186
		// (get) Token: 0x06004EC9 RID: 20169 RVA: 0x0011BDCC File Offset: 0x00119FCC
		public override Module Module
		{
			get
			{
				return this.m_method.Module;
			}
		}

		// Token: 0x06004ECA RID: 20170 RVA: 0x0011BDD9 File Offset: 0x00119FD9
		public new Type GetType()
		{
			return base.GetType();
		}

		// Token: 0x06004ECB RID: 20171 RVA: 0x0011BDE1 File Offset: 0x00119FE1
		public override ParameterInfo[] GetParameters()
		{
			return this.m_method.GetParameters();
		}

		// Token: 0x06004ECC RID: 20172 RVA: 0x0011BDEE File Offset: 0x00119FEE
		public override MethodImplAttributes GetMethodImplementationFlags()
		{
			return this.m_method.GetMethodImplementationFlags();
		}

		// Token: 0x17000C73 RID: 3187
		// (get) Token: 0x06004ECD RID: 20173 RVA: 0x0011BDFB File Offset: 0x00119FFB
		public override RuntimeMethodHandle MethodHandle
		{
			get
			{
				return this.m_method.MethodHandle;
			}
		}

		// Token: 0x17000C74 RID: 3188
		// (get) Token: 0x06004ECE RID: 20174 RVA: 0x0011BE08 File Offset: 0x0011A008
		public override MethodAttributes Attributes
		{
			get
			{
				return this.m_method.Attributes;
			}
		}

		// Token: 0x06004ECF RID: 20175 RVA: 0x0011BE15 File Offset: 0x0011A015
		public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
		{
			throw new NotSupportedException();
		}

		// Token: 0x17000C75 RID: 3189
		// (get) Token: 0x06004ED0 RID: 20176 RVA: 0x0011BE1C File Offset: 0x0011A01C
		public override CallingConventions CallingConvention
		{
			get
			{
				return this.m_method.CallingConvention;
			}
		}

		// Token: 0x06004ED1 RID: 20177 RVA: 0x0011BE29 File Offset: 0x0011A029
		public override Type[] GetGenericArguments()
		{
			return this.m_method.GetGenericArguments();
		}

		// Token: 0x06004ED2 RID: 20178 RVA: 0x0011BE36 File Offset: 0x0011A036
		public override MethodInfo GetGenericMethodDefinition()
		{
			return this.m_method;
		}

		// Token: 0x17000C76 RID: 3190
		// (get) Token: 0x06004ED3 RID: 20179 RVA: 0x0011BE3E File Offset: 0x0011A03E
		public override bool IsGenericMethodDefinition
		{
			get
			{
				return this.m_method.IsGenericMethodDefinition;
			}
		}

		// Token: 0x17000C77 RID: 3191
		// (get) Token: 0x06004ED4 RID: 20180 RVA: 0x0011BE4B File Offset: 0x0011A04B
		public override bool ContainsGenericParameters
		{
			get
			{
				return this.m_method.ContainsGenericParameters;
			}
		}

		// Token: 0x06004ED5 RID: 20181 RVA: 0x0011BE58 File Offset: 0x0011A058
		public override MethodInfo MakeGenericMethod(params Type[] typeArgs)
		{
			if (!this.IsGenericMethodDefinition)
			{
				throw new InvalidOperationException(Environment.GetResourceString("Arg_NotGenericMethodDefinition"));
			}
			return MethodBuilderInstantiation.MakeGenericMethod(this, typeArgs);
		}

		// Token: 0x17000C78 RID: 3192
		// (get) Token: 0x06004ED6 RID: 20182 RVA: 0x0011BE79 File Offset: 0x0011A079
		public override bool IsGenericMethod
		{
			get
			{
				return this.m_method.IsGenericMethod;
			}
		}

		// Token: 0x17000C79 RID: 3193
		// (get) Token: 0x06004ED7 RID: 20183 RVA: 0x0011BE86 File Offset: 0x0011A086
		public override Type ReturnType
		{
			get
			{
				return this.m_method.ReturnType;
			}
		}

		// Token: 0x17000C7A RID: 3194
		// (get) Token: 0x06004ED8 RID: 20184 RVA: 0x0011BE93 File Offset: 0x0011A093
		public override ParameterInfo ReturnParameter
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		// Token: 0x17000C7B RID: 3195
		// (get) Token: 0x06004ED9 RID: 20185 RVA: 0x0011BE9A File Offset: 0x0011A09A
		public override ICustomAttributeProvider ReturnTypeCustomAttributes
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		// Token: 0x06004EDA RID: 20186 RVA: 0x0011BEA1 File Offset: 0x0011A0A1
		public override MethodInfo GetBaseDefinition()
		{
			throw new NotSupportedException();
		}

		// Token: 0x040021D6 RID: 8662
		internal MethodInfo m_method;

		// Token: 0x040021D7 RID: 8663
		private TypeBuilderInstantiation m_type;
	}
}
