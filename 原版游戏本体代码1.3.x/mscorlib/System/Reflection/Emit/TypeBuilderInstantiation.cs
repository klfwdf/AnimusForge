using System;
using System.Collections;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit
{
	// Token: 0x02000664 RID: 1636
	internal sealed class TypeBuilderInstantiation : TypeInfo
	{
		// Token: 0x06004DEF RID: 19951 RVA: 0x0011B1DB File Offset: 0x001193DB
		public override bool IsAssignableFrom(TypeInfo typeInfo)
		{
			return !(typeInfo == null) && this.IsAssignableFrom(typeInfo.AsType());
		}

		// Token: 0x06004DF0 RID: 19952 RVA: 0x0011B1F4 File Offset: 0x001193F4
		internal static Type MakeGenericType(Type type, Type[] typeArguments)
		{
			if (!type.IsGenericTypeDefinition)
			{
				throw new InvalidOperationException();
			}
			if (typeArguments == null)
			{
				throw new ArgumentNullException("typeArguments");
			}
			foreach (Type left in typeArguments)
			{
				if (left == null)
				{
					throw new ArgumentNullException("typeArguments");
				}
			}
			return new TypeBuilderInstantiation(type, typeArguments);
		}

		// Token: 0x06004DF1 RID: 19953 RVA: 0x0011B24C File Offset: 0x0011944C
		private TypeBuilderInstantiation(Type type, Type[] inst)
		{
			this.m_type = type;
			this.m_inst = inst;
			this.m_hashtable = new Hashtable();
		}

		// Token: 0x06004DF2 RID: 19954 RVA: 0x0011B278 File Offset: 0x00119478
		public override string ToString()
		{
			return TypeNameBuilder.ToString(this, TypeNameBuilder.Format.ToString);
		}

		// Token: 0x17000C34 RID: 3124
		// (get) Token: 0x06004DF3 RID: 19955 RVA: 0x0011B281 File Offset: 0x00119481
		public override Type DeclaringType
		{
			get
			{
				return this.m_type.DeclaringType;
			}
		}

		// Token: 0x17000C35 RID: 3125
		// (get) Token: 0x06004DF4 RID: 19956 RVA: 0x0011B28E File Offset: 0x0011948E
		public override Type ReflectedType
		{
			get
			{
				return this.m_type.ReflectedType;
			}
		}

		// Token: 0x17000C36 RID: 3126
		// (get) Token: 0x06004DF5 RID: 19957 RVA: 0x0011B29B File Offset: 0x0011949B
		public override string Name
		{
			get
			{
				return this.m_type.Name;
			}
		}

		// Token: 0x17000C37 RID: 3127
		// (get) Token: 0x06004DF6 RID: 19958 RVA: 0x0011B2A8 File Offset: 0x001194A8
		public override Module Module
		{
			get
			{
				return this.m_type.Module;
			}
		}

		// Token: 0x06004DF7 RID: 19959 RVA: 0x0011B2B5 File Offset: 0x001194B5
		public override Type MakePointerType()
		{
			return SymbolType.FormCompoundType("*".ToCharArray(), this, 0);
		}

		// Token: 0x06004DF8 RID: 19960 RVA: 0x0011B2C8 File Offset: 0x001194C8
		public override Type MakeByRefType()
		{
			return SymbolType.FormCompoundType("&".ToCharArray(), this, 0);
		}

		// Token: 0x06004DF9 RID: 19961 RVA: 0x0011B2DB File Offset: 0x001194DB
		public override Type MakeArrayType()
		{
			return SymbolType.FormCompoundType("[]".ToCharArray(), this, 0);
		}

		// Token: 0x06004DFA RID: 19962 RVA: 0x0011B2F0 File Offset: 0x001194F0
		public override Type MakeArrayType(int rank)
		{
			if (rank <= 0)
			{
				throw new IndexOutOfRangeException();
			}
			string text = "";
			for (int i = 1; i < rank; i++)
			{
				text += ",";
			}
			string text2 = string.Format(CultureInfo.InvariantCulture, "[{0}]", text);
			return SymbolType.FormCompoundType(text2.ToCharArray(), this, 0);
		}

		// Token: 0x17000C38 RID: 3128
		// (get) Token: 0x06004DFB RID: 19963 RVA: 0x0011B343 File Offset: 0x00119543
		public override Guid GUID
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		// Token: 0x06004DFC RID: 19964 RVA: 0x0011B34A File Offset: 0x0011954A
		public override object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters)
		{
			throw new NotSupportedException();
		}

		// Token: 0x17000C39 RID: 3129
		// (get) Token: 0x06004DFD RID: 19965 RVA: 0x0011B351 File Offset: 0x00119551
		public override Assembly Assembly
		{
			get
			{
				return this.m_type.Assembly;
			}
		}

		// Token: 0x17000C3A RID: 3130
		// (get) Token: 0x06004DFE RID: 19966 RVA: 0x0011B35E File Offset: 0x0011955E
		public override RuntimeTypeHandle TypeHandle
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		// Token: 0x17000C3B RID: 3131
		// (get) Token: 0x06004DFF RID: 19967 RVA: 0x0011B365 File Offset: 0x00119565
		public override string FullName
		{
			get
			{
				if (this.m_strFullQualName == null)
				{
					this.m_strFullQualName = TypeNameBuilder.ToString(this, TypeNameBuilder.Format.FullName);
				}
				return this.m_strFullQualName;
			}
		}

		// Token: 0x17000C3C RID: 3132
		// (get) Token: 0x06004E00 RID: 19968 RVA: 0x0011B382 File Offset: 0x00119582
		public override string Namespace
		{
			get
			{
				return this.m_type.Namespace;
			}
		}

		// Token: 0x17000C3D RID: 3133
		// (get) Token: 0x06004E01 RID: 19969 RVA: 0x0011B38F File Offset: 0x0011958F
		public override string AssemblyQualifiedName
		{
			get
			{
				return TypeNameBuilder.ToString(this, TypeNameBuilder.Format.AssemblyQualifiedName);
			}
		}

		// Token: 0x06004E02 RID: 19970 RVA: 0x0011B398 File Offset: 0x00119598
		private Type Substitute(Type[] substitutes)
		{
			Type[] genericArguments = this.GetGenericArguments();
			Type[] array = new Type[genericArguments.Length];
			for (int i = 0; i < array.Length; i++)
			{
				Type type = genericArguments[i];
				if (type is TypeBuilderInstantiation)
				{
					array[i] = (type as TypeBuilderInstantiation).Substitute(substitutes);
				}
				else if (type is GenericTypeParameterBuilder)
				{
					array[i] = substitutes[type.GenericParameterPosition];
				}
				else
				{
					array[i] = type;
				}
			}
			return this.GetGenericTypeDefinition().MakeGenericType(array);
		}

		// Token: 0x17000C3E RID: 3134
		// (get) Token: 0x06004E03 RID: 19971 RVA: 0x0011B408 File Offset: 0x00119608
		public override Type BaseType
		{
			get
			{
				Type baseType = this.m_type.BaseType;
				if (baseType == null)
				{
					return null;
				}
				TypeBuilderInstantiation typeBuilderInstantiation = baseType as TypeBuilderInstantiation;
				if (typeBuilderInstantiation == null)
				{
					return baseType;
				}
				return typeBuilderInstantiation.Substitute(this.GetGenericArguments());
			}
		}

		// Token: 0x06004E04 RID: 19972 RVA: 0x0011B44A File Offset: 0x0011964A
		protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06004E05 RID: 19973 RVA: 0x0011B451 File Offset: 0x00119651
		[ComVisible(true)]
		public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06004E06 RID: 19974 RVA: 0x0011B458 File Offset: 0x00119658
		protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06004E07 RID: 19975 RVA: 0x0011B45F File Offset: 0x0011965F
		public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06004E08 RID: 19976 RVA: 0x0011B466 File Offset: 0x00119666
		public override FieldInfo GetField(string name, BindingFlags bindingAttr)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06004E09 RID: 19977 RVA: 0x0011B46D File Offset: 0x0011966D
		public override FieldInfo[] GetFields(BindingFlags bindingAttr)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06004E0A RID: 19978 RVA: 0x0011B474 File Offset: 0x00119674
		public override Type GetInterface(string name, bool ignoreCase)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06004E0B RID: 19979 RVA: 0x0011B47B File Offset: 0x0011967B
		public override Type[] GetInterfaces()
		{
			throw new NotSupportedException();
		}

		// Token: 0x06004E0C RID: 19980 RVA: 0x0011B482 File Offset: 0x00119682
		public override EventInfo GetEvent(string name, BindingFlags bindingAttr)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06004E0D RID: 19981 RVA: 0x0011B489 File Offset: 0x00119689
		public override EventInfo[] GetEvents()
		{
			throw new NotSupportedException();
		}

		// Token: 0x06004E0E RID: 19982 RVA: 0x0011B490 File Offset: 0x00119690
		protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06004E0F RID: 19983 RVA: 0x0011B497 File Offset: 0x00119697
		public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06004E10 RID: 19984 RVA: 0x0011B49E File Offset: 0x0011969E
		public override Type[] GetNestedTypes(BindingFlags bindingAttr)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06004E11 RID: 19985 RVA: 0x0011B4A5 File Offset: 0x001196A5
		public override Type GetNestedType(string name, BindingFlags bindingAttr)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06004E12 RID: 19986 RVA: 0x0011B4AC File Offset: 0x001196AC
		public override MemberInfo[] GetMember(string name, MemberTypes type, BindingFlags bindingAttr)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06004E13 RID: 19987 RVA: 0x0011B4B3 File Offset: 0x001196B3
		[ComVisible(true)]
		public override InterfaceMapping GetInterfaceMap(Type interfaceType)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06004E14 RID: 19988 RVA: 0x0011B4BA File Offset: 0x001196BA
		public override EventInfo[] GetEvents(BindingFlags bindingAttr)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06004E15 RID: 19989 RVA: 0x0011B4C1 File Offset: 0x001196C1
		public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06004E16 RID: 19990 RVA: 0x0011B4C8 File Offset: 0x001196C8
		protected override TypeAttributes GetAttributeFlagsImpl()
		{
			return this.m_type.Attributes;
		}

		// Token: 0x06004E17 RID: 19991 RVA: 0x0011B4D5 File Offset: 0x001196D5
		protected override bool IsArrayImpl()
		{
			return false;
		}

		// Token: 0x06004E18 RID: 19992 RVA: 0x0011B4D8 File Offset: 0x001196D8
		protected override bool IsByRefImpl()
		{
			return false;
		}

		// Token: 0x06004E19 RID: 19993 RVA: 0x0011B4DB File Offset: 0x001196DB
		protected override bool IsPointerImpl()
		{
			return false;
		}

		// Token: 0x06004E1A RID: 19994 RVA: 0x0011B4DE File Offset: 0x001196DE
		protected override bool IsPrimitiveImpl()
		{
			return false;
		}

		// Token: 0x06004E1B RID: 19995 RVA: 0x0011B4E1 File Offset: 0x001196E1
		protected override bool IsCOMObjectImpl()
		{
			return false;
		}

		// Token: 0x06004E1C RID: 19996 RVA: 0x0011B4E4 File Offset: 0x001196E4
		public override Type GetElementType()
		{
			throw new NotSupportedException();
		}

		// Token: 0x06004E1D RID: 19997 RVA: 0x0011B4EB File Offset: 0x001196EB
		protected override bool HasElementTypeImpl()
		{
			return false;
		}

		// Token: 0x17000C3F RID: 3135
		// (get) Token: 0x06004E1E RID: 19998 RVA: 0x0011B4EE File Offset: 0x001196EE
		public override Type UnderlyingSystemType
		{
			get
			{
				return this;
			}
		}

		// Token: 0x06004E1F RID: 19999 RVA: 0x0011B4F1 File Offset: 0x001196F1
		public override Type[] GetGenericArguments()
		{
			return this.m_inst;
		}

		// Token: 0x17000C40 RID: 3136
		// (get) Token: 0x06004E20 RID: 20000 RVA: 0x0011B4F9 File Offset: 0x001196F9
		public override bool IsGenericTypeDefinition
		{
			get
			{
				return false;
			}
		}

		// Token: 0x17000C41 RID: 3137
		// (get) Token: 0x06004E21 RID: 20001 RVA: 0x0011B4FC File Offset: 0x001196FC
		public override bool IsGenericType
		{
			get
			{
				return true;
			}
		}

		// Token: 0x17000C42 RID: 3138
		// (get) Token: 0x06004E22 RID: 20002 RVA: 0x0011B4FF File Offset: 0x001196FF
		public override bool IsConstructedGenericType
		{
			get
			{
				return true;
			}
		}

		// Token: 0x17000C43 RID: 3139
		// (get) Token: 0x06004E23 RID: 20003 RVA: 0x0011B502 File Offset: 0x00119702
		public override bool IsGenericParameter
		{
			get
			{
				return false;
			}
		}

		// Token: 0x17000C44 RID: 3140
		// (get) Token: 0x06004E24 RID: 20004 RVA: 0x0011B505 File Offset: 0x00119705
		public override int GenericParameterPosition
		{
			get
			{
				throw new InvalidOperationException();
			}
		}

		// Token: 0x06004E25 RID: 20005 RVA: 0x0011B50C File Offset: 0x0011970C
		protected override bool IsValueTypeImpl()
		{
			return this.m_type.IsValueType;
		}

		// Token: 0x17000C45 RID: 3141
		// (get) Token: 0x06004E26 RID: 20006 RVA: 0x0011B51C File Offset: 0x0011971C
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
				return false;
			}
		}

		// Token: 0x17000C46 RID: 3142
		// (get) Token: 0x06004E27 RID: 20007 RVA: 0x0011B54E File Offset: 0x0011974E
		public override MethodBase DeclaringMethod
		{
			get
			{
				return null;
			}
		}

		// Token: 0x06004E28 RID: 20008 RVA: 0x0011B551 File Offset: 0x00119751
		public override Type GetGenericTypeDefinition()
		{
			return this.m_type;
		}

		// Token: 0x06004E29 RID: 20009 RVA: 0x0011B559 File Offset: 0x00119759
		public override Type MakeGenericType(params Type[] inst)
		{
			throw new InvalidOperationException(Environment.GetResourceString("Arg_NotGenericTypeDefinition"));
		}

		// Token: 0x06004E2A RID: 20010 RVA: 0x0011B56A File Offset: 0x0011976A
		public override bool IsAssignableFrom(Type c)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06004E2B RID: 20011 RVA: 0x0011B571 File Offset: 0x00119771
		[ComVisible(true)]
		public override bool IsSubclassOf(Type c)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06004E2C RID: 20012 RVA: 0x0011B578 File Offset: 0x00119778
		public override object[] GetCustomAttributes(bool inherit)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06004E2D RID: 20013 RVA: 0x0011B57F File Offset: 0x0011977F
		public override object[] GetCustomAttributes(Type attributeType, bool inherit)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06004E2E RID: 20014 RVA: 0x0011B586 File Offset: 0x00119786
		public override bool IsDefined(Type attributeType, bool inherit)
		{
			throw new NotSupportedException();
		}

		// Token: 0x040021CD RID: 8653
		private Type m_type;

		// Token: 0x040021CE RID: 8654
		private Type[] m_inst;

		// Token: 0x040021CF RID: 8655
		private string m_strFullQualName;

		// Token: 0x040021D0 RID: 8656
		internal Hashtable m_hashtable = new Hashtable();
	}
}
