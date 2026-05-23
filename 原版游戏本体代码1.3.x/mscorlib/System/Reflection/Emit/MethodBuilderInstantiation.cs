using System;
using System.Globalization;

namespace System.Reflection.Emit
{
	// Token: 0x02000649 RID: 1609
	internal sealed class MethodBuilderInstantiation : MethodInfo
	{
		// Token: 0x06004B82 RID: 19330 RVA: 0x00111A68 File Offset: 0x0010FC68
		internal static MethodInfo MakeGenericMethod(MethodInfo method, Type[] inst)
		{
			if (!method.IsGenericMethodDefinition)
			{
				throw new InvalidOperationException();
			}
			return new MethodBuilderInstantiation(method, inst);
		}

		// Token: 0x06004B83 RID: 19331 RVA: 0x00111A7F File Offset: 0x0010FC7F
		internal MethodBuilderInstantiation(MethodInfo method, Type[] inst)
		{
			this.m_method = method;
			this.m_inst = inst;
		}

		// Token: 0x06004B84 RID: 19332 RVA: 0x00111A95 File Offset: 0x0010FC95
		internal override Type[] GetParameterTypes()
		{
			return this.m_method.GetParameterTypes();
		}

		// Token: 0x17000BCB RID: 3019
		// (get) Token: 0x06004B85 RID: 19333 RVA: 0x00111AA2 File Offset: 0x0010FCA2
		public override MemberTypes MemberType
		{
			get
			{
				return this.m_method.MemberType;
			}
		}

		// Token: 0x17000BCC RID: 3020
		// (get) Token: 0x06004B86 RID: 19334 RVA: 0x00111AAF File Offset: 0x0010FCAF
		public override string Name
		{
			get
			{
				return this.m_method.Name;
			}
		}

		// Token: 0x17000BCD RID: 3021
		// (get) Token: 0x06004B87 RID: 19335 RVA: 0x00111ABC File Offset: 0x0010FCBC
		public override Type DeclaringType
		{
			get
			{
				return this.m_method.DeclaringType;
			}
		}

		// Token: 0x17000BCE RID: 3022
		// (get) Token: 0x06004B88 RID: 19336 RVA: 0x00111AC9 File Offset: 0x0010FCC9
		public override Type ReflectedType
		{
			get
			{
				return this.m_method.ReflectedType;
			}
		}

		// Token: 0x06004B89 RID: 19337 RVA: 0x00111AD6 File Offset: 0x0010FCD6
		public override object[] GetCustomAttributes(bool inherit)
		{
			return this.m_method.GetCustomAttributes(inherit);
		}

		// Token: 0x06004B8A RID: 19338 RVA: 0x00111AE4 File Offset: 0x0010FCE4
		public override object[] GetCustomAttributes(Type attributeType, bool inherit)
		{
			return this.m_method.GetCustomAttributes(attributeType, inherit);
		}

		// Token: 0x06004B8B RID: 19339 RVA: 0x00111AF3 File Offset: 0x0010FCF3
		public override bool IsDefined(Type attributeType, bool inherit)
		{
			return this.m_method.IsDefined(attributeType, inherit);
		}

		// Token: 0x17000BCF RID: 3023
		// (get) Token: 0x06004B8C RID: 19340 RVA: 0x00111B02 File Offset: 0x0010FD02
		public override Module Module
		{
			get
			{
				return this.m_method.Module;
			}
		}

		// Token: 0x06004B8D RID: 19341 RVA: 0x00111B0F File Offset: 0x0010FD0F
		public new Type GetType()
		{
			return base.GetType();
		}

		// Token: 0x06004B8E RID: 19342 RVA: 0x00111B17 File Offset: 0x0010FD17
		public override ParameterInfo[] GetParameters()
		{
			throw new NotSupportedException();
		}

		// Token: 0x06004B8F RID: 19343 RVA: 0x00111B1E File Offset: 0x0010FD1E
		public override MethodImplAttributes GetMethodImplementationFlags()
		{
			return this.m_method.GetMethodImplementationFlags();
		}

		// Token: 0x17000BD0 RID: 3024
		// (get) Token: 0x06004B90 RID: 19344 RVA: 0x00111B2B File Offset: 0x0010FD2B
		public override RuntimeMethodHandle MethodHandle
		{
			get
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicModule"));
			}
		}

		// Token: 0x17000BD1 RID: 3025
		// (get) Token: 0x06004B91 RID: 19345 RVA: 0x00111B3C File Offset: 0x0010FD3C
		public override MethodAttributes Attributes
		{
			get
			{
				return this.m_method.Attributes;
			}
		}

		// Token: 0x06004B92 RID: 19346 RVA: 0x00111B49 File Offset: 0x0010FD49
		public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
		{
			throw new NotSupportedException();
		}

		// Token: 0x17000BD2 RID: 3026
		// (get) Token: 0x06004B93 RID: 19347 RVA: 0x00111B50 File Offset: 0x0010FD50
		public override CallingConventions CallingConvention
		{
			get
			{
				return this.m_method.CallingConvention;
			}
		}

		// Token: 0x06004B94 RID: 19348 RVA: 0x00111B5D File Offset: 0x0010FD5D
		public override Type[] GetGenericArguments()
		{
			return this.m_inst;
		}

		// Token: 0x06004B95 RID: 19349 RVA: 0x00111B65 File Offset: 0x0010FD65
		public override MethodInfo GetGenericMethodDefinition()
		{
			return this.m_method;
		}

		// Token: 0x17000BD3 RID: 3027
		// (get) Token: 0x06004B96 RID: 19350 RVA: 0x00111B6D File Offset: 0x0010FD6D
		public override bool IsGenericMethodDefinition
		{
			get
			{
				return false;
			}
		}

		// Token: 0x17000BD4 RID: 3028
		// (get) Token: 0x06004B97 RID: 19351 RVA: 0x00111B70 File Offset: 0x0010FD70
		public override bool ContainsGenericParameters
		{
			get
			{
				for (int i = 0; i < this.m_inst.Length; i++)
				{
					if (this.m_inst[i].ContainsGenericParameters)
					{
						return true;
					}
				}
				return this.DeclaringType != null && this.DeclaringType.ContainsGenericParameters;
			}
		}

		// Token: 0x06004B98 RID: 19352 RVA: 0x00111BBF File Offset: 0x0010FDBF
		public override MethodInfo MakeGenericMethod(params Type[] arguments)
		{
			throw new InvalidOperationException(Environment.GetResourceString("Arg_NotGenericMethodDefinition"));
		}

		// Token: 0x17000BD5 RID: 3029
		// (get) Token: 0x06004B99 RID: 19353 RVA: 0x00111BD0 File Offset: 0x0010FDD0
		public override bool IsGenericMethod
		{
			get
			{
				return true;
			}
		}

		// Token: 0x17000BD6 RID: 3030
		// (get) Token: 0x06004B9A RID: 19354 RVA: 0x00111BD3 File Offset: 0x0010FDD3
		public override Type ReturnType
		{
			get
			{
				return this.m_method.ReturnType;
			}
		}

		// Token: 0x17000BD7 RID: 3031
		// (get) Token: 0x06004B9B RID: 19355 RVA: 0x00111BE0 File Offset: 0x0010FDE0
		public override ParameterInfo ReturnParameter
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		// Token: 0x17000BD8 RID: 3032
		// (get) Token: 0x06004B9C RID: 19356 RVA: 0x00111BE7 File Offset: 0x0010FDE7
		public override ICustomAttributeProvider ReturnTypeCustomAttributes
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		// Token: 0x06004B9D RID: 19357 RVA: 0x00111BEE File Offset: 0x0010FDEE
		public override MethodInfo GetBaseDefinition()
		{
			throw new NotSupportedException();
		}

		// Token: 0x04001F3A RID: 7994
		internal MethodInfo m_method;

		// Token: 0x04001F3B RID: 7995
		private Type[] m_inst;
	}
}
