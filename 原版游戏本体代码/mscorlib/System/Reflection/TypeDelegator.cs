using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Reflection
{
	// Token: 0x02000625 RID: 1573
	[ComVisible(true)]
	[Serializable]
	public class TypeDelegator : TypeInfo
	{
		// Token: 0x060048C0 RID: 18624 RVA: 0x00107A83 File Offset: 0x00105C83
		public override bool IsAssignableFrom(TypeInfo typeInfo)
		{
			return !(typeInfo == null) && this.IsAssignableFrom(typeInfo.AsType());
		}

		// Token: 0x060048C1 RID: 18625 RVA: 0x00107A9C File Offset: 0x00105C9C
		protected TypeDelegator()
		{
		}

		// Token: 0x060048C2 RID: 18626 RVA: 0x00107AA4 File Offset: 0x00105CA4
		public TypeDelegator(Type delegatingType)
		{
			if (delegatingType == null)
			{
				throw new ArgumentNullException("delegatingType");
			}
			this.typeImpl = delegatingType;
		}

		// Token: 0x17000B56 RID: 2902
		// (get) Token: 0x060048C3 RID: 18627 RVA: 0x00107AC7 File Offset: 0x00105CC7
		public override Guid GUID
		{
			get
			{
				return this.typeImpl.GUID;
			}
		}

		// Token: 0x17000B57 RID: 2903
		// (get) Token: 0x060048C4 RID: 18628 RVA: 0x00107AD4 File Offset: 0x00105CD4
		public override int MetadataToken
		{
			get
			{
				return this.typeImpl.MetadataToken;
			}
		}

		// Token: 0x060048C5 RID: 18629 RVA: 0x00107AE4 File Offset: 0x00105CE4
		public override object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters)
		{
			return this.typeImpl.InvokeMember(name, invokeAttr, binder, target, args, modifiers, culture, namedParameters);
		}

		// Token: 0x17000B58 RID: 2904
		// (get) Token: 0x060048C6 RID: 18630 RVA: 0x00107B09 File Offset: 0x00105D09
		public override Module Module
		{
			get
			{
				return this.typeImpl.Module;
			}
		}

		// Token: 0x17000B59 RID: 2905
		// (get) Token: 0x060048C7 RID: 18631 RVA: 0x00107B16 File Offset: 0x00105D16
		public override Assembly Assembly
		{
			get
			{
				return this.typeImpl.Assembly;
			}
		}

		// Token: 0x17000B5A RID: 2906
		// (get) Token: 0x060048C8 RID: 18632 RVA: 0x00107B23 File Offset: 0x00105D23
		public override RuntimeTypeHandle TypeHandle
		{
			get
			{
				return this.typeImpl.TypeHandle;
			}
		}

		// Token: 0x17000B5B RID: 2907
		// (get) Token: 0x060048C9 RID: 18633 RVA: 0x00107B30 File Offset: 0x00105D30
		public override string Name
		{
			get
			{
				return this.typeImpl.Name;
			}
		}

		// Token: 0x17000B5C RID: 2908
		// (get) Token: 0x060048CA RID: 18634 RVA: 0x00107B3D File Offset: 0x00105D3D
		public override string FullName
		{
			get
			{
				return this.typeImpl.FullName;
			}
		}

		// Token: 0x17000B5D RID: 2909
		// (get) Token: 0x060048CB RID: 18635 RVA: 0x00107B4A File Offset: 0x00105D4A
		public override string Namespace
		{
			get
			{
				return this.typeImpl.Namespace;
			}
		}

		// Token: 0x17000B5E RID: 2910
		// (get) Token: 0x060048CC RID: 18636 RVA: 0x00107B57 File Offset: 0x00105D57
		public override string AssemblyQualifiedName
		{
			get
			{
				return this.typeImpl.AssemblyQualifiedName;
			}
		}

		// Token: 0x17000B5F RID: 2911
		// (get) Token: 0x060048CD RID: 18637 RVA: 0x00107B64 File Offset: 0x00105D64
		public override Type BaseType
		{
			get
			{
				return this.typeImpl.BaseType;
			}
		}

		// Token: 0x060048CE RID: 18638 RVA: 0x00107B71 File Offset: 0x00105D71
		protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
		{
			return this.typeImpl.GetConstructor(bindingAttr, binder, callConvention, types, modifiers);
		}

		// Token: 0x060048CF RID: 18639 RVA: 0x00107B85 File Offset: 0x00105D85
		[ComVisible(true)]
		public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
		{
			return this.typeImpl.GetConstructors(bindingAttr);
		}

		// Token: 0x060048D0 RID: 18640 RVA: 0x00107B93 File Offset: 0x00105D93
		protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
		{
			if (types == null)
			{
				return this.typeImpl.GetMethod(name, bindingAttr);
			}
			return this.typeImpl.GetMethod(name, bindingAttr, binder, callConvention, types, modifiers);
		}

		// Token: 0x060048D1 RID: 18641 RVA: 0x00107BBB File Offset: 0x00105DBB
		public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
		{
			return this.typeImpl.GetMethods(bindingAttr);
		}

		// Token: 0x060048D2 RID: 18642 RVA: 0x00107BC9 File Offset: 0x00105DC9
		public override FieldInfo GetField(string name, BindingFlags bindingAttr)
		{
			return this.typeImpl.GetField(name, bindingAttr);
		}

		// Token: 0x060048D3 RID: 18643 RVA: 0x00107BD8 File Offset: 0x00105DD8
		public override FieldInfo[] GetFields(BindingFlags bindingAttr)
		{
			return this.typeImpl.GetFields(bindingAttr);
		}

		// Token: 0x060048D4 RID: 18644 RVA: 0x00107BE6 File Offset: 0x00105DE6
		public override Type GetInterface(string name, bool ignoreCase)
		{
			return this.typeImpl.GetInterface(name, ignoreCase);
		}

		// Token: 0x060048D5 RID: 18645 RVA: 0x00107BF5 File Offset: 0x00105DF5
		public override Type[] GetInterfaces()
		{
			return this.typeImpl.GetInterfaces();
		}

		// Token: 0x060048D6 RID: 18646 RVA: 0x00107C02 File Offset: 0x00105E02
		public override EventInfo GetEvent(string name, BindingFlags bindingAttr)
		{
			return this.typeImpl.GetEvent(name, bindingAttr);
		}

		// Token: 0x060048D7 RID: 18647 RVA: 0x00107C11 File Offset: 0x00105E11
		public override EventInfo[] GetEvents()
		{
			return this.typeImpl.GetEvents();
		}

		// Token: 0x060048D8 RID: 18648 RVA: 0x00107C1E File Offset: 0x00105E1E
		protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers)
		{
			if (returnType == null && types == null)
			{
				return this.typeImpl.GetProperty(name, bindingAttr);
			}
			return this.typeImpl.GetProperty(name, bindingAttr, binder, returnType, types, modifiers);
		}

		// Token: 0x060048D9 RID: 18649 RVA: 0x00107C50 File Offset: 0x00105E50
		public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
		{
			return this.typeImpl.GetProperties(bindingAttr);
		}

		// Token: 0x060048DA RID: 18650 RVA: 0x00107C5E File Offset: 0x00105E5E
		public override EventInfo[] GetEvents(BindingFlags bindingAttr)
		{
			return this.typeImpl.GetEvents(bindingAttr);
		}

		// Token: 0x060048DB RID: 18651 RVA: 0x00107C6C File Offset: 0x00105E6C
		public override Type[] GetNestedTypes(BindingFlags bindingAttr)
		{
			return this.typeImpl.GetNestedTypes(bindingAttr);
		}

		// Token: 0x060048DC RID: 18652 RVA: 0x00107C7A File Offset: 0x00105E7A
		public override Type GetNestedType(string name, BindingFlags bindingAttr)
		{
			return this.typeImpl.GetNestedType(name, bindingAttr);
		}

		// Token: 0x060048DD RID: 18653 RVA: 0x00107C89 File Offset: 0x00105E89
		public override MemberInfo[] GetMember(string name, MemberTypes type, BindingFlags bindingAttr)
		{
			return this.typeImpl.GetMember(name, type, bindingAttr);
		}

		// Token: 0x060048DE RID: 18654 RVA: 0x00107C99 File Offset: 0x00105E99
		public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
		{
			return this.typeImpl.GetMembers(bindingAttr);
		}

		// Token: 0x060048DF RID: 18655 RVA: 0x00107CA7 File Offset: 0x00105EA7
		protected override TypeAttributes GetAttributeFlagsImpl()
		{
			return this.typeImpl.Attributes;
		}

		// Token: 0x060048E0 RID: 18656 RVA: 0x00107CB4 File Offset: 0x00105EB4
		protected override bool IsArrayImpl()
		{
			return this.typeImpl.IsArray;
		}

		// Token: 0x060048E1 RID: 18657 RVA: 0x00107CC1 File Offset: 0x00105EC1
		protected override bool IsPrimitiveImpl()
		{
			return this.typeImpl.IsPrimitive;
		}

		// Token: 0x060048E2 RID: 18658 RVA: 0x00107CCE File Offset: 0x00105ECE
		protected override bool IsByRefImpl()
		{
			return this.typeImpl.IsByRef;
		}

		// Token: 0x060048E3 RID: 18659 RVA: 0x00107CDB File Offset: 0x00105EDB
		protected override bool IsPointerImpl()
		{
			return this.typeImpl.IsPointer;
		}

		// Token: 0x060048E4 RID: 18660 RVA: 0x00107CE8 File Offset: 0x00105EE8
		protected override bool IsValueTypeImpl()
		{
			return this.typeImpl.IsValueType;
		}

		// Token: 0x060048E5 RID: 18661 RVA: 0x00107CF5 File Offset: 0x00105EF5
		protected override bool IsCOMObjectImpl()
		{
			return this.typeImpl.IsCOMObject;
		}

		// Token: 0x17000B60 RID: 2912
		// (get) Token: 0x060048E6 RID: 18662 RVA: 0x00107D02 File Offset: 0x00105F02
		public override bool IsConstructedGenericType
		{
			get
			{
				return this.typeImpl.IsConstructedGenericType;
			}
		}

		// Token: 0x060048E7 RID: 18663 RVA: 0x00107D0F File Offset: 0x00105F0F
		public override Type GetElementType()
		{
			return this.typeImpl.GetElementType();
		}

		// Token: 0x060048E8 RID: 18664 RVA: 0x00107D1C File Offset: 0x00105F1C
		protected override bool HasElementTypeImpl()
		{
			return this.typeImpl.HasElementType;
		}

		// Token: 0x17000B61 RID: 2913
		// (get) Token: 0x060048E9 RID: 18665 RVA: 0x00107D29 File Offset: 0x00105F29
		public override Type UnderlyingSystemType
		{
			get
			{
				return this.typeImpl.UnderlyingSystemType;
			}
		}

		// Token: 0x060048EA RID: 18666 RVA: 0x00107D36 File Offset: 0x00105F36
		public override object[] GetCustomAttributes(bool inherit)
		{
			return this.typeImpl.GetCustomAttributes(inherit);
		}

		// Token: 0x060048EB RID: 18667 RVA: 0x00107D44 File Offset: 0x00105F44
		public override object[] GetCustomAttributes(Type attributeType, bool inherit)
		{
			return this.typeImpl.GetCustomAttributes(attributeType, inherit);
		}

		// Token: 0x060048EC RID: 18668 RVA: 0x00107D53 File Offset: 0x00105F53
		public override bool IsDefined(Type attributeType, bool inherit)
		{
			return this.typeImpl.IsDefined(attributeType, inherit);
		}

		// Token: 0x060048ED RID: 18669 RVA: 0x00107D62 File Offset: 0x00105F62
		[ComVisible(true)]
		public override InterfaceMapping GetInterfaceMap(Type interfaceType)
		{
			return this.typeImpl.GetInterfaceMap(interfaceType);
		}

		// Token: 0x04001E42 RID: 7746
		protected Type typeImpl;
	}
}
