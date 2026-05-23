using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit
{
	// Token: 0x02000665 RID: 1637
	[ComVisible(true)]
	public sealed class GenericTypeParameterBuilder : TypeInfo
	{
		// Token: 0x06004E2F RID: 20015 RVA: 0x0011B58D File Offset: 0x0011978D
		public override bool IsAssignableFrom(TypeInfo typeInfo)
		{
			return !(typeInfo == null) && this.IsAssignableFrom(typeInfo.AsType());
		}

		// Token: 0x06004E30 RID: 20016 RVA: 0x0011B5A6 File Offset: 0x001197A6
		internal GenericTypeParameterBuilder(TypeBuilder type)
		{
			this.m_type = type;
		}

		// Token: 0x06004E31 RID: 20017 RVA: 0x0011B5B5 File Offset: 0x001197B5
		public override string ToString()
		{
			return this.m_type.Name;
		}

		// Token: 0x06004E32 RID: 20018 RVA: 0x0011B5C4 File Offset: 0x001197C4
		public override bool Equals(object o)
		{
			GenericTypeParameterBuilder genericTypeParameterBuilder = o as GenericTypeParameterBuilder;
			return !(genericTypeParameterBuilder == null) && genericTypeParameterBuilder.m_type == this.m_type;
		}

		// Token: 0x06004E33 RID: 20019 RVA: 0x0011B5F1 File Offset: 0x001197F1
		public override int GetHashCode()
		{
			return this.m_type.GetHashCode();
		}

		// Token: 0x17000C47 RID: 3143
		// (get) Token: 0x06004E34 RID: 20020 RVA: 0x0011B5FE File Offset: 0x001197FE
		public override Type DeclaringType
		{
			get
			{
				return this.m_type.DeclaringType;
			}
		}

		// Token: 0x17000C48 RID: 3144
		// (get) Token: 0x06004E35 RID: 20021 RVA: 0x0011B60B File Offset: 0x0011980B
		public override Type ReflectedType
		{
			get
			{
				return this.m_type.ReflectedType;
			}
		}

		// Token: 0x17000C49 RID: 3145
		// (get) Token: 0x06004E36 RID: 20022 RVA: 0x0011B618 File Offset: 0x00119818
		public override string Name
		{
			get
			{
				return this.m_type.Name;
			}
		}

		// Token: 0x17000C4A RID: 3146
		// (get) Token: 0x06004E37 RID: 20023 RVA: 0x0011B625 File Offset: 0x00119825
		public override Module Module
		{
			get
			{
				return this.m_type.Module;
			}
		}

		// Token: 0x17000C4B RID: 3147
		// (get) Token: 0x06004E38 RID: 20024 RVA: 0x0011B632 File Offset: 0x00119832
		internal int MetadataTokenInternal
		{
			get
			{
				return this.m_type.MetadataTokenInternal;
			}
		}

		// Token: 0x06004E39 RID: 20025 RVA: 0x0011B63F File Offset: 0x0011983F
		public override Type MakePointerType()
		{
			return SymbolType.FormCompoundType("*".ToCharArray(), this, 0);
		}

		// Token: 0x06004E3A RID: 20026 RVA: 0x0011B652 File Offset: 0x00119852
		public override Type MakeByRefType()
		{
			return SymbolType.FormCompoundType("&".ToCharArray(), this, 0);
		}

		// Token: 0x06004E3B RID: 20027 RVA: 0x0011B665 File Offset: 0x00119865
		public override Type MakeArrayType()
		{
			return SymbolType.FormCompoundType("[]".ToCharArray(), this, 0);
		}

		// Token: 0x06004E3C RID: 20028 RVA: 0x0011B678 File Offset: 0x00119878
		public override Type MakeArrayType(int rank)
		{
			if (rank <= 0)
			{
				throw new IndexOutOfRangeException();
			}
			string text = "";
			if (rank == 1)
			{
				text = "*";
			}
			else
			{
				for (int i = 1; i < rank; i++)
				{
					text += ",";
				}
			}
			string text2 = string.Format(CultureInfo.InvariantCulture, "[{0}]", text);
			return SymbolType.FormCompoundType(text2.ToCharArray(), this, 0) as SymbolType;
		}

		// Token: 0x17000C4C RID: 3148
		// (get) Token: 0x06004E3D RID: 20029 RVA: 0x0011B6DE File Offset: 0x001198DE
		public override Guid GUID
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		// Token: 0x06004E3E RID: 20030 RVA: 0x0011B6E5 File Offset: 0x001198E5
		public override object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters)
		{
			throw new NotSupportedException();
		}

		// Token: 0x17000C4D RID: 3149
		// (get) Token: 0x06004E3F RID: 20031 RVA: 0x0011B6EC File Offset: 0x001198EC
		public override Assembly Assembly
		{
			get
			{
				return this.m_type.Assembly;
			}
		}

		// Token: 0x17000C4E RID: 3150
		// (get) Token: 0x06004E40 RID: 20032 RVA: 0x0011B6F9 File Offset: 0x001198F9
		public override RuntimeTypeHandle TypeHandle
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		// Token: 0x17000C4F RID: 3151
		// (get) Token: 0x06004E41 RID: 20033 RVA: 0x0011B700 File Offset: 0x00119900
		public override string FullName
		{
			get
			{
				return null;
			}
		}

		// Token: 0x17000C50 RID: 3152
		// (get) Token: 0x06004E42 RID: 20034 RVA: 0x0011B703 File Offset: 0x00119903
		public override string Namespace
		{
			get
			{
				return null;
			}
		}

		// Token: 0x17000C51 RID: 3153
		// (get) Token: 0x06004E43 RID: 20035 RVA: 0x0011B706 File Offset: 0x00119906
		public override string AssemblyQualifiedName
		{
			get
			{
				return null;
			}
		}

		// Token: 0x17000C52 RID: 3154
		// (get) Token: 0x06004E44 RID: 20036 RVA: 0x0011B709 File Offset: 0x00119909
		public override Type BaseType
		{
			get
			{
				return this.m_type.BaseType;
			}
		}

		// Token: 0x06004E45 RID: 20037 RVA: 0x0011B716 File Offset: 0x00119916
		protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06004E46 RID: 20038 RVA: 0x0011B71D File Offset: 0x0011991D
		[ComVisible(true)]
		public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06004E47 RID: 20039 RVA: 0x0011B724 File Offset: 0x00119924
		protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06004E48 RID: 20040 RVA: 0x0011B72B File Offset: 0x0011992B
		public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06004E49 RID: 20041 RVA: 0x0011B732 File Offset: 0x00119932
		public override FieldInfo GetField(string name, BindingFlags bindingAttr)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06004E4A RID: 20042 RVA: 0x0011B739 File Offset: 0x00119939
		public override FieldInfo[] GetFields(BindingFlags bindingAttr)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06004E4B RID: 20043 RVA: 0x0011B740 File Offset: 0x00119940
		public override Type GetInterface(string name, bool ignoreCase)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06004E4C RID: 20044 RVA: 0x0011B747 File Offset: 0x00119947
		public override Type[] GetInterfaces()
		{
			throw new NotSupportedException();
		}

		// Token: 0x06004E4D RID: 20045 RVA: 0x0011B74E File Offset: 0x0011994E
		public override EventInfo GetEvent(string name, BindingFlags bindingAttr)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06004E4E RID: 20046 RVA: 0x0011B755 File Offset: 0x00119955
		public override EventInfo[] GetEvents()
		{
			throw new NotSupportedException();
		}

		// Token: 0x06004E4F RID: 20047 RVA: 0x0011B75C File Offset: 0x0011995C
		protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06004E50 RID: 20048 RVA: 0x0011B763 File Offset: 0x00119963
		public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06004E51 RID: 20049 RVA: 0x0011B76A File Offset: 0x0011996A
		public override Type[] GetNestedTypes(BindingFlags bindingAttr)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06004E52 RID: 20050 RVA: 0x0011B771 File Offset: 0x00119971
		public override Type GetNestedType(string name, BindingFlags bindingAttr)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06004E53 RID: 20051 RVA: 0x0011B778 File Offset: 0x00119978
		public override MemberInfo[] GetMember(string name, MemberTypes type, BindingFlags bindingAttr)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06004E54 RID: 20052 RVA: 0x0011B77F File Offset: 0x0011997F
		[ComVisible(true)]
		public override InterfaceMapping GetInterfaceMap(Type interfaceType)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06004E55 RID: 20053 RVA: 0x0011B786 File Offset: 0x00119986
		public override EventInfo[] GetEvents(BindingFlags bindingAttr)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06004E56 RID: 20054 RVA: 0x0011B78D File Offset: 0x0011998D
		public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06004E57 RID: 20055 RVA: 0x0011B794 File Offset: 0x00119994
		protected override TypeAttributes GetAttributeFlagsImpl()
		{
			return TypeAttributes.Public;
		}

		// Token: 0x06004E58 RID: 20056 RVA: 0x0011B797 File Offset: 0x00119997
		protected override bool IsArrayImpl()
		{
			return false;
		}

		// Token: 0x06004E59 RID: 20057 RVA: 0x0011B79A File Offset: 0x0011999A
		protected override bool IsByRefImpl()
		{
			return false;
		}

		// Token: 0x06004E5A RID: 20058 RVA: 0x0011B79D File Offset: 0x0011999D
		protected override bool IsPointerImpl()
		{
			return false;
		}

		// Token: 0x06004E5B RID: 20059 RVA: 0x0011B7A0 File Offset: 0x001199A0
		protected override bool IsPrimitiveImpl()
		{
			return false;
		}

		// Token: 0x06004E5C RID: 20060 RVA: 0x0011B7A3 File Offset: 0x001199A3
		protected override bool IsCOMObjectImpl()
		{
			return false;
		}

		// Token: 0x06004E5D RID: 20061 RVA: 0x0011B7A6 File Offset: 0x001199A6
		public override Type GetElementType()
		{
			throw new NotSupportedException();
		}

		// Token: 0x06004E5E RID: 20062 RVA: 0x0011B7AD File Offset: 0x001199AD
		protected override bool HasElementTypeImpl()
		{
			return false;
		}

		// Token: 0x17000C53 RID: 3155
		// (get) Token: 0x06004E5F RID: 20063 RVA: 0x0011B7B0 File Offset: 0x001199B0
		public override Type UnderlyingSystemType
		{
			get
			{
				return this;
			}
		}

		// Token: 0x06004E60 RID: 20064 RVA: 0x0011B7B3 File Offset: 0x001199B3
		public override Type[] GetGenericArguments()
		{
			throw new InvalidOperationException();
		}

		// Token: 0x17000C54 RID: 3156
		// (get) Token: 0x06004E61 RID: 20065 RVA: 0x0011B7BA File Offset: 0x001199BA
		public override bool IsGenericTypeDefinition
		{
			get
			{
				return false;
			}
		}

		// Token: 0x17000C55 RID: 3157
		// (get) Token: 0x06004E62 RID: 20066 RVA: 0x0011B7BD File Offset: 0x001199BD
		public override bool IsGenericType
		{
			get
			{
				return false;
			}
		}

		// Token: 0x17000C56 RID: 3158
		// (get) Token: 0x06004E63 RID: 20067 RVA: 0x0011B7C0 File Offset: 0x001199C0
		public override bool IsGenericParameter
		{
			get
			{
				return true;
			}
		}

		// Token: 0x17000C57 RID: 3159
		// (get) Token: 0x06004E64 RID: 20068 RVA: 0x0011B7C3 File Offset: 0x001199C3
		public override bool IsConstructedGenericType
		{
			get
			{
				return false;
			}
		}

		// Token: 0x17000C58 RID: 3160
		// (get) Token: 0x06004E65 RID: 20069 RVA: 0x0011B7C6 File Offset: 0x001199C6
		public override int GenericParameterPosition
		{
			get
			{
				return this.m_type.GenericParameterPosition;
			}
		}

		// Token: 0x17000C59 RID: 3161
		// (get) Token: 0x06004E66 RID: 20070 RVA: 0x0011B7D3 File Offset: 0x001199D3
		public override bool ContainsGenericParameters
		{
			get
			{
				return this.m_type.ContainsGenericParameters;
			}
		}

		// Token: 0x17000C5A RID: 3162
		// (get) Token: 0x06004E67 RID: 20071 RVA: 0x0011B7E0 File Offset: 0x001199E0
		public override GenericParameterAttributes GenericParameterAttributes
		{
			get
			{
				return this.m_type.GenericParameterAttributes;
			}
		}

		// Token: 0x17000C5B RID: 3163
		// (get) Token: 0x06004E68 RID: 20072 RVA: 0x0011B7ED File Offset: 0x001199ED
		public override MethodBase DeclaringMethod
		{
			get
			{
				return this.m_type.DeclaringMethod;
			}
		}

		// Token: 0x06004E69 RID: 20073 RVA: 0x0011B7FA File Offset: 0x001199FA
		public override Type GetGenericTypeDefinition()
		{
			throw new InvalidOperationException();
		}

		// Token: 0x06004E6A RID: 20074 RVA: 0x0011B801 File Offset: 0x00119A01
		public override Type MakeGenericType(params Type[] typeArguments)
		{
			throw new InvalidOperationException(Environment.GetResourceString("Arg_NotGenericTypeDefinition"));
		}

		// Token: 0x06004E6B RID: 20075 RVA: 0x0011B812 File Offset: 0x00119A12
		protected override bool IsValueTypeImpl()
		{
			return false;
		}

		// Token: 0x06004E6C RID: 20076 RVA: 0x0011B815 File Offset: 0x00119A15
		public override bool IsAssignableFrom(Type c)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06004E6D RID: 20077 RVA: 0x0011B81C File Offset: 0x00119A1C
		[ComVisible(true)]
		public override bool IsSubclassOf(Type c)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06004E6E RID: 20078 RVA: 0x0011B823 File Offset: 0x00119A23
		public override object[] GetCustomAttributes(bool inherit)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06004E6F RID: 20079 RVA: 0x0011B82A File Offset: 0x00119A2A
		public override object[] GetCustomAttributes(Type attributeType, bool inherit)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06004E70 RID: 20080 RVA: 0x0011B831 File Offset: 0x00119A31
		public override bool IsDefined(Type attributeType, bool inherit)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06004E71 RID: 20081 RVA: 0x0011B838 File Offset: 0x00119A38
		public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
		{
			this.m_type.SetGenParamCustomAttribute(con, binaryAttribute);
		}

		// Token: 0x06004E72 RID: 20082 RVA: 0x0011B847 File Offset: 0x00119A47
		public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
		{
			this.m_type.SetGenParamCustomAttribute(customBuilder);
		}

		// Token: 0x06004E73 RID: 20083 RVA: 0x0011B855 File Offset: 0x00119A55
		public void SetBaseTypeConstraint(Type baseTypeConstraint)
		{
			this.m_type.CheckContext(new Type[] { baseTypeConstraint });
			this.m_type.SetParent(baseTypeConstraint);
		}

		// Token: 0x06004E74 RID: 20084 RVA: 0x0011B878 File Offset: 0x00119A78
		[ComVisible(true)]
		public void SetInterfaceConstraints(params Type[] interfaceConstraints)
		{
			this.m_type.CheckContext(interfaceConstraints);
			this.m_type.SetInterfaces(interfaceConstraints);
		}

		// Token: 0x06004E75 RID: 20085 RVA: 0x0011B892 File Offset: 0x00119A92
		public void SetGenericParameterAttributes(GenericParameterAttributes genericParameterAttributes)
		{
			this.m_type.SetGenParamAttributes(genericParameterAttributes);
		}

		// Token: 0x040021D1 RID: 8657
		internal TypeBuilder m_type;
	}
}
