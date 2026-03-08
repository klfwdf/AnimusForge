using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit
{
	// Token: 0x0200064B RID: 1611
	internal sealed class SymbolType : TypeInfo
	{
		// Token: 0x06004B9E RID: 19358 RVA: 0x00111BF5 File Offset: 0x0010FDF5
		public override bool IsAssignableFrom(TypeInfo typeInfo)
		{
			return !(typeInfo == null) && this.IsAssignableFrom(typeInfo.AsType());
		}

		// Token: 0x06004B9F RID: 19359 RVA: 0x00111C10 File Offset: 0x0010FE10
		internal static Type FormCompoundType(char[] bFormat, Type baseType, int curIndex)
		{
			if (bFormat == null || curIndex == bFormat.Length)
			{
				return baseType;
			}
			if (bFormat[curIndex] == '&')
			{
				SymbolType symbolType = new SymbolType(TypeKind.IsByRef);
				symbolType.SetFormat(bFormat, curIndex, 1);
				curIndex++;
				if (curIndex != bFormat.Length)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_BadSigFormat"));
				}
				symbolType.SetElementType(baseType);
				return symbolType;
			}
			else
			{
				if (bFormat[curIndex] == '[')
				{
					SymbolType symbolType = new SymbolType(TypeKind.IsArray);
					int num = curIndex;
					curIndex++;
					int num2 = 0;
					int num3 = -1;
					while (bFormat[curIndex] != ']')
					{
						if (bFormat[curIndex] == '*')
						{
							symbolType.m_isSzArray = false;
							curIndex++;
						}
						if ((bFormat[curIndex] >= '0' && bFormat[curIndex] <= '9') || bFormat[curIndex] == '-')
						{
							bool flag = false;
							if (bFormat[curIndex] == '-')
							{
								flag = true;
								curIndex++;
							}
							while (bFormat[curIndex] >= '0' && bFormat[curIndex] <= '9')
							{
								num2 *= 10;
								num2 += (int)(bFormat[curIndex] - '0');
								curIndex++;
							}
							if (flag)
							{
								num2 = 0 - num2;
							}
							num3 = num2 - 1;
						}
						if (bFormat[curIndex] == '.')
						{
							curIndex++;
							if (bFormat[curIndex] != '.')
							{
								throw new ArgumentException(Environment.GetResourceString("Argument_BadSigFormat"));
							}
							curIndex++;
							if ((bFormat[curIndex] >= '0' && bFormat[curIndex] <= '9') || bFormat[curIndex] == '-')
							{
								bool flag2 = false;
								num3 = 0;
								if (bFormat[curIndex] == '-')
								{
									flag2 = true;
									curIndex++;
								}
								while (bFormat[curIndex] >= '0' && bFormat[curIndex] <= '9')
								{
									num3 *= 10;
									num3 += (int)(bFormat[curIndex] - '0');
									curIndex++;
								}
								if (flag2)
								{
									num3 = 0 - num3;
								}
								if (num3 < num2)
								{
									throw new ArgumentException(Environment.GetResourceString("Argument_BadSigFormat"));
								}
							}
						}
						if (bFormat[curIndex] == ',')
						{
							curIndex++;
							symbolType.SetBounds(num2, num3);
							num2 = 0;
							num3 = -1;
						}
						else if (bFormat[curIndex] != ']')
						{
							throw new ArgumentException(Environment.GetResourceString("Argument_BadSigFormat"));
						}
					}
					symbolType.SetBounds(num2, num3);
					curIndex++;
					symbolType.SetFormat(bFormat, num, curIndex - num);
					symbolType.SetElementType(baseType);
					return SymbolType.FormCompoundType(bFormat, symbolType, curIndex);
				}
				if (bFormat[curIndex] == '*')
				{
					SymbolType symbolType = new SymbolType(TypeKind.IsPointer);
					symbolType.SetFormat(bFormat, curIndex, 1);
					curIndex++;
					symbolType.SetElementType(baseType);
					return SymbolType.FormCompoundType(bFormat, symbolType, curIndex);
				}
				return null;
			}
		}

		// Token: 0x06004BA0 RID: 19360 RVA: 0x00111E10 File Offset: 0x00110010
		internal SymbolType(TypeKind typeKind)
		{
			this.m_typeKind = typeKind;
			this.m_iaLowerBound = new int[4];
			this.m_iaUpperBound = new int[4];
		}

		// Token: 0x06004BA1 RID: 19361 RVA: 0x00111E3E File Offset: 0x0011003E
		internal void SetElementType(Type baseType)
		{
			if (baseType == null)
			{
				throw new ArgumentNullException("baseType");
			}
			this.m_baseType = baseType;
		}

		// Token: 0x06004BA2 RID: 19362 RVA: 0x00111E5C File Offset: 0x0011005C
		private void SetBounds(int lower, int upper)
		{
			if (lower != 0 || upper != -1)
			{
				this.m_isSzArray = false;
			}
			if (this.m_iaLowerBound.Length <= this.m_cRank)
			{
				int[] array = new int[this.m_cRank * 2];
				Array.Copy(this.m_iaLowerBound, array, this.m_cRank);
				this.m_iaLowerBound = array;
				Array.Copy(this.m_iaUpperBound, array, this.m_cRank);
				this.m_iaUpperBound = array;
			}
			this.m_iaLowerBound[this.m_cRank] = lower;
			this.m_iaUpperBound[this.m_cRank] = upper;
			this.m_cRank++;
		}

		// Token: 0x06004BA3 RID: 19363 RVA: 0x00111EF4 File Offset: 0x001100F4
		internal void SetFormat(char[] bFormat, int curIndex, int length)
		{
			char[] array = new char[length];
			Array.Copy(bFormat, curIndex, array, 0, length);
			this.m_bFormat = array;
		}

		// Token: 0x17000BD9 RID: 3033
		// (get) Token: 0x06004BA4 RID: 19364 RVA: 0x00111F19 File Offset: 0x00110119
		internal override bool IsSzArray
		{
			get
			{
				return this.m_cRank <= 1 && this.m_isSzArray;
			}
		}

		// Token: 0x06004BA5 RID: 19365 RVA: 0x00111F2C File Offset: 0x0011012C
		public override Type MakePointerType()
		{
			return SymbolType.FormCompoundType((new string(this.m_bFormat) + "*").ToCharArray(), this.m_baseType, 0);
		}

		// Token: 0x06004BA6 RID: 19366 RVA: 0x00111F54 File Offset: 0x00110154
		public override Type MakeByRefType()
		{
			return SymbolType.FormCompoundType((new string(this.m_bFormat) + "&").ToCharArray(), this.m_baseType, 0);
		}

		// Token: 0x06004BA7 RID: 19367 RVA: 0x00111F7C File Offset: 0x0011017C
		public override Type MakeArrayType()
		{
			return SymbolType.FormCompoundType((new string(this.m_bFormat) + "[]").ToCharArray(), this.m_baseType, 0);
		}

		// Token: 0x06004BA8 RID: 19368 RVA: 0x00111FA4 File Offset: 0x001101A4
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
			string str = string.Format(CultureInfo.InvariantCulture, "[{0}]", text);
			return SymbolType.FormCompoundType((new string(this.m_bFormat) + str).ToCharArray(), this.m_baseType, 0) as SymbolType;
		}

		// Token: 0x06004BA9 RID: 19369 RVA: 0x0011201F File Offset: 0x0011021F
		public override int GetArrayRank()
		{
			if (!base.IsArray)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_SubclassOverride"));
			}
			return this.m_cRank;
		}

		// Token: 0x17000BDA RID: 3034
		// (get) Token: 0x06004BAA RID: 19370 RVA: 0x0011203F File Offset: 0x0011023F
		public override Guid GUID
		{
			get
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
			}
		}

		// Token: 0x06004BAB RID: 19371 RVA: 0x00112050 File Offset: 0x00110250
		public override object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
		}

		// Token: 0x17000BDB RID: 3035
		// (get) Token: 0x06004BAC RID: 19372 RVA: 0x00112064 File Offset: 0x00110264
		public override Module Module
		{
			get
			{
				Type baseType = this.m_baseType;
				while (baseType is SymbolType)
				{
					baseType = ((SymbolType)baseType).m_baseType;
				}
				return baseType.Module;
			}
		}

		// Token: 0x17000BDC RID: 3036
		// (get) Token: 0x06004BAD RID: 19373 RVA: 0x00112094 File Offset: 0x00110294
		public override Assembly Assembly
		{
			get
			{
				Type baseType = this.m_baseType;
				while (baseType is SymbolType)
				{
					baseType = ((SymbolType)baseType).m_baseType;
				}
				return baseType.Assembly;
			}
		}

		// Token: 0x17000BDD RID: 3037
		// (get) Token: 0x06004BAE RID: 19374 RVA: 0x001120C4 File Offset: 0x001102C4
		public override RuntimeTypeHandle TypeHandle
		{
			get
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
			}
		}

		// Token: 0x17000BDE RID: 3038
		// (get) Token: 0x06004BAF RID: 19375 RVA: 0x001120D8 File Offset: 0x001102D8
		public override string Name
		{
			get
			{
				string str = new string(this.m_bFormat);
				Type baseType = this.m_baseType;
				while (baseType is SymbolType)
				{
					str = new string(((SymbolType)baseType).m_bFormat) + str;
					baseType = ((SymbolType)baseType).m_baseType;
				}
				return baseType.Name + str;
			}
		}

		// Token: 0x17000BDF RID: 3039
		// (get) Token: 0x06004BB0 RID: 19376 RVA: 0x00112131 File Offset: 0x00110331
		public override string FullName
		{
			get
			{
				return TypeNameBuilder.ToString(this, TypeNameBuilder.Format.FullName);
			}
		}

		// Token: 0x17000BE0 RID: 3040
		// (get) Token: 0x06004BB1 RID: 19377 RVA: 0x0011213A File Offset: 0x0011033A
		public override string AssemblyQualifiedName
		{
			get
			{
				return TypeNameBuilder.ToString(this, TypeNameBuilder.Format.AssemblyQualifiedName);
			}
		}

		// Token: 0x06004BB2 RID: 19378 RVA: 0x00112143 File Offset: 0x00110343
		public override string ToString()
		{
			return TypeNameBuilder.ToString(this, TypeNameBuilder.Format.ToString);
		}

		// Token: 0x17000BE1 RID: 3041
		// (get) Token: 0x06004BB3 RID: 19379 RVA: 0x0011214C File Offset: 0x0011034C
		public override string Namespace
		{
			get
			{
				return this.m_baseType.Namespace;
			}
		}

		// Token: 0x17000BE2 RID: 3042
		// (get) Token: 0x06004BB4 RID: 19380 RVA: 0x00112159 File Offset: 0x00110359
		public override Type BaseType
		{
			get
			{
				return typeof(Array);
			}
		}

		// Token: 0x06004BB5 RID: 19381 RVA: 0x00112165 File Offset: 0x00110365
		protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
		}

		// Token: 0x06004BB6 RID: 19382 RVA: 0x00112176 File Offset: 0x00110376
		[ComVisible(true)]
		public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
		}

		// Token: 0x06004BB7 RID: 19383 RVA: 0x00112187 File Offset: 0x00110387
		protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
		}

		// Token: 0x06004BB8 RID: 19384 RVA: 0x00112198 File Offset: 0x00110398
		public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
		}

		// Token: 0x06004BB9 RID: 19385 RVA: 0x001121A9 File Offset: 0x001103A9
		public override FieldInfo GetField(string name, BindingFlags bindingAttr)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
		}

		// Token: 0x06004BBA RID: 19386 RVA: 0x001121BA File Offset: 0x001103BA
		public override FieldInfo[] GetFields(BindingFlags bindingAttr)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
		}

		// Token: 0x06004BBB RID: 19387 RVA: 0x001121CB File Offset: 0x001103CB
		public override Type GetInterface(string name, bool ignoreCase)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
		}

		// Token: 0x06004BBC RID: 19388 RVA: 0x001121DC File Offset: 0x001103DC
		public override Type[] GetInterfaces()
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
		}

		// Token: 0x06004BBD RID: 19389 RVA: 0x001121ED File Offset: 0x001103ED
		public override EventInfo GetEvent(string name, BindingFlags bindingAttr)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
		}

		// Token: 0x06004BBE RID: 19390 RVA: 0x001121FE File Offset: 0x001103FE
		public override EventInfo[] GetEvents()
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
		}

		// Token: 0x06004BBF RID: 19391 RVA: 0x0011220F File Offset: 0x0011040F
		protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
		}

		// Token: 0x06004BC0 RID: 19392 RVA: 0x00112220 File Offset: 0x00110420
		public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
		}

		// Token: 0x06004BC1 RID: 19393 RVA: 0x00112231 File Offset: 0x00110431
		public override Type[] GetNestedTypes(BindingFlags bindingAttr)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
		}

		// Token: 0x06004BC2 RID: 19394 RVA: 0x00112242 File Offset: 0x00110442
		public override Type GetNestedType(string name, BindingFlags bindingAttr)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
		}

		// Token: 0x06004BC3 RID: 19395 RVA: 0x00112253 File Offset: 0x00110453
		public override MemberInfo[] GetMember(string name, MemberTypes type, BindingFlags bindingAttr)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
		}

		// Token: 0x06004BC4 RID: 19396 RVA: 0x00112264 File Offset: 0x00110464
		public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
		}

		// Token: 0x06004BC5 RID: 19397 RVA: 0x00112275 File Offset: 0x00110475
		[ComVisible(true)]
		public override InterfaceMapping GetInterfaceMap(Type interfaceType)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
		}

		// Token: 0x06004BC6 RID: 19398 RVA: 0x00112286 File Offset: 0x00110486
		public override EventInfo[] GetEvents(BindingFlags bindingAttr)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
		}

		// Token: 0x06004BC7 RID: 19399 RVA: 0x00112298 File Offset: 0x00110498
		protected override TypeAttributes GetAttributeFlagsImpl()
		{
			Type baseType = this.m_baseType;
			while (baseType is SymbolType)
			{
				baseType = ((SymbolType)baseType).m_baseType;
			}
			return baseType.Attributes;
		}

		// Token: 0x06004BC8 RID: 19400 RVA: 0x001122C8 File Offset: 0x001104C8
		protected override bool IsArrayImpl()
		{
			return this.m_typeKind == TypeKind.IsArray;
		}

		// Token: 0x06004BC9 RID: 19401 RVA: 0x001122D3 File Offset: 0x001104D3
		protected override bool IsPointerImpl()
		{
			return this.m_typeKind == TypeKind.IsPointer;
		}

		// Token: 0x06004BCA RID: 19402 RVA: 0x001122DE File Offset: 0x001104DE
		protected override bool IsByRefImpl()
		{
			return this.m_typeKind == TypeKind.IsByRef;
		}

		// Token: 0x06004BCB RID: 19403 RVA: 0x001122E9 File Offset: 0x001104E9
		protected override bool IsPrimitiveImpl()
		{
			return false;
		}

		// Token: 0x06004BCC RID: 19404 RVA: 0x001122EC File Offset: 0x001104EC
		protected override bool IsValueTypeImpl()
		{
			return false;
		}

		// Token: 0x06004BCD RID: 19405 RVA: 0x001122EF File Offset: 0x001104EF
		protected override bool IsCOMObjectImpl()
		{
			return false;
		}

		// Token: 0x17000BE3 RID: 3043
		// (get) Token: 0x06004BCE RID: 19406 RVA: 0x001122F2 File Offset: 0x001104F2
		public override bool IsConstructedGenericType
		{
			get
			{
				return false;
			}
		}

		// Token: 0x06004BCF RID: 19407 RVA: 0x001122F5 File Offset: 0x001104F5
		public override Type GetElementType()
		{
			return this.m_baseType;
		}

		// Token: 0x06004BD0 RID: 19408 RVA: 0x001122FD File Offset: 0x001104FD
		protected override bool HasElementTypeImpl()
		{
			return this.m_baseType != null;
		}

		// Token: 0x17000BE4 RID: 3044
		// (get) Token: 0x06004BD1 RID: 19409 RVA: 0x0011230B File Offset: 0x0011050B
		public override Type UnderlyingSystemType
		{
			get
			{
				return this;
			}
		}

		// Token: 0x06004BD2 RID: 19410 RVA: 0x0011230E File Offset: 0x0011050E
		public override object[] GetCustomAttributes(bool inherit)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
		}

		// Token: 0x06004BD3 RID: 19411 RVA: 0x0011231F File Offset: 0x0011051F
		public override object[] GetCustomAttributes(Type attributeType, bool inherit)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
		}

		// Token: 0x06004BD4 RID: 19412 RVA: 0x00112330 File Offset: 0x00110530
		public override bool IsDefined(Type attributeType, bool inherit)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
		}

		// Token: 0x04001F40 RID: 8000
		internal TypeKind m_typeKind;

		// Token: 0x04001F41 RID: 8001
		internal Type m_baseType;

		// Token: 0x04001F42 RID: 8002
		internal int m_cRank;

		// Token: 0x04001F43 RID: 8003
		internal int[] m_iaLowerBound;

		// Token: 0x04001F44 RID: 8004
		internal int[] m_iaUpperBound;

		// Token: 0x04001F45 RID: 8005
		private char[] m_bFormat;

		// Token: 0x04001F46 RID: 8006
		private bool m_isSzArray = true;
	}
}
