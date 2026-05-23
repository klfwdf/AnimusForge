using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Newtonsoft.Json.Serialization;

namespace Newtonsoft.Json.Utilities
{
	// Token: 0x02000069 RID: 105
	[NullableContext(1)]
	[Nullable(0)]
	internal static class ReflectionUtils
	{
		// Token: 0x060005A0 RID: 1440 RVA: 0x0001810C File Offset: 0x0001630C
		public static bool IsVirtual(this PropertyInfo propertyInfo)
		{
			ValidationUtils.ArgumentNotNull(propertyInfo, "propertyInfo");
			MethodInfo methodInfo = propertyInfo.GetGetMethod(true);
			if (methodInfo != null && methodInfo.IsVirtual)
			{
				return true;
			}
			methodInfo = propertyInfo.GetSetMethod(true);
			return methodInfo != null && methodInfo.IsVirtual;
		}

		// Token: 0x060005A1 RID: 1441 RVA: 0x0001815C File Offset: 0x0001635C
		[return: Nullable(2)]
		public static MethodInfo GetBaseDefinition(this PropertyInfo propertyInfo)
		{
			ValidationUtils.ArgumentNotNull(propertyInfo, "propertyInfo");
			MethodInfo getMethod = propertyInfo.GetGetMethod(true);
			if (getMethod != null)
			{
				return getMethod.GetBaseDefinition();
			}
			MethodInfo setMethod = propertyInfo.GetSetMethod(true);
			if (setMethod == null)
			{
				return null;
			}
			return setMethod.GetBaseDefinition();
		}

		// Token: 0x060005A2 RID: 1442 RVA: 0x000181A0 File Offset: 0x000163A0
		public static bool IsPublic(PropertyInfo property)
		{
			MethodInfo getMethod = property.GetGetMethod();
			if (getMethod != null && getMethod.IsPublic)
			{
				return true;
			}
			MethodInfo setMethod = property.GetSetMethod();
			return setMethod != null && setMethod.IsPublic;
		}

		// Token: 0x060005A3 RID: 1443 RVA: 0x000181E2 File Offset: 0x000163E2
		[NullableContext(2)]
		public static Type GetObjectType(object v)
		{
			if (v == null)
			{
				return null;
			}
			return v.GetType();
		}

		// Token: 0x060005A4 RID: 1444 RVA: 0x000181F0 File Offset: 0x000163F0
		public static string GetTypeName(Type t, TypeNameAssemblyFormatHandling assemblyFormat, [Nullable(2)] ISerializationBinder binder)
		{
			string fullyQualifiedTypeName = ReflectionUtils.GetFullyQualifiedTypeName(t, binder);
			if (assemblyFormat == TypeNameAssemblyFormatHandling.Simple)
			{
				return ReflectionUtils.RemoveAssemblyDetails(fullyQualifiedTypeName);
			}
			if (assemblyFormat != TypeNameAssemblyFormatHandling.Full)
			{
				throw new ArgumentOutOfRangeException();
			}
			return fullyQualifiedTypeName;
		}

		// Token: 0x060005A5 RID: 1445 RVA: 0x0001821C File Offset: 0x0001641C
		private static string GetFullyQualifiedTypeName(Type t, [Nullable(2)] ISerializationBinder binder)
		{
			if (binder != null)
			{
				string text;
				string str;
				binder.BindToName(t, out text, out str);
				return str + ((text == null) ? "" : (", " + text));
			}
			return t.AssemblyQualifiedName;
		}

		// Token: 0x060005A6 RID: 1446 RVA: 0x0001825C File Offset: 0x0001645C
		private static string RemoveAssemblyDetails(string fullyQualifiedTypeName)
		{
			StringBuilder stringBuilder = new StringBuilder();
			bool flag = false;
			bool flag2 = false;
			bool flag3 = false;
			foreach (char c in fullyQualifiedTypeName)
			{
				if (c != ',')
				{
					if (c != '[')
					{
						if (c != ']')
						{
							flag3 = false;
							if (!flag2)
							{
								stringBuilder.Append(c);
							}
						}
						else
						{
							flag = false;
							flag2 = false;
							flag3 = false;
							stringBuilder.Append(c);
						}
					}
					else
					{
						flag = false;
						flag2 = false;
						flag3 = true;
						stringBuilder.Append(c);
					}
				}
				else if (flag3)
				{
					stringBuilder.Append(c);
				}
				else if (!flag)
				{
					flag = true;
					stringBuilder.Append(c);
				}
				else
				{
					flag2 = true;
				}
			}
			return stringBuilder.ToString();
		}

		// Token: 0x060005A7 RID: 1447 RVA: 0x00018300 File Offset: 0x00016500
		public static bool HasDefaultConstructor(Type t, bool nonPublic)
		{
			ValidationUtils.ArgumentNotNull(t, "t");
			return t.IsValueType() || ReflectionUtils.GetDefaultConstructor(t, nonPublic) != null;
		}

		// Token: 0x060005A8 RID: 1448 RVA: 0x00018324 File Offset: 0x00016524
		[return: Nullable(2)]
		public static ConstructorInfo GetDefaultConstructor(Type t)
		{
			return ReflectionUtils.GetDefaultConstructor(t, false);
		}

		// Token: 0x060005A9 RID: 1449 RVA: 0x00018330 File Offset: 0x00016530
		[return: Nullable(2)]
		public static ConstructorInfo GetDefaultConstructor(Type t, bool nonPublic)
		{
			BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public;
			if (nonPublic)
			{
				bindingFlags |= BindingFlags.NonPublic;
			}
			return t.GetConstructors(bindingFlags).SingleOrDefault((ConstructorInfo c) => !c.GetParameters().Any<ParameterInfo>());
		}

		// Token: 0x060005AA RID: 1450 RVA: 0x00018373 File Offset: 0x00016573
		public static bool IsNullable(Type t)
		{
			ValidationUtils.ArgumentNotNull(t, "t");
			return !t.IsValueType() || ReflectionUtils.IsNullableType(t);
		}

		// Token: 0x060005AB RID: 1451 RVA: 0x00018390 File Offset: 0x00016590
		public static bool IsNullableType(Type t)
		{
			ValidationUtils.ArgumentNotNull(t, "t");
			return t.IsGenericType() && t.GetGenericTypeDefinition() == typeof(Nullable<>);
		}

		// Token: 0x060005AC RID: 1452 RVA: 0x000183BC File Offset: 0x000165BC
		public static Type EnsureNotNullableType(Type t)
		{
			if (!ReflectionUtils.IsNullableType(t))
			{
				return t;
			}
			return Nullable.GetUnderlyingType(t);
		}

		// Token: 0x060005AD RID: 1453 RVA: 0x000183CE File Offset: 0x000165CE
		public static Type EnsureNotByRefType(Type t)
		{
			if (!t.IsByRef || !t.HasElementType)
			{
				return t;
			}
			return t.GetElementType();
		}

		// Token: 0x060005AE RID: 1454 RVA: 0x000183E8 File Offset: 0x000165E8
		public static bool IsGenericDefinition(Type type, Type genericInterfaceDefinition)
		{
			return type.IsGenericType() && type.GetGenericTypeDefinition() == genericInterfaceDefinition;
		}

		// Token: 0x060005AF RID: 1455 RVA: 0x00018400 File Offset: 0x00016600
		public static bool ImplementsGenericDefinition(Type type, Type genericInterfaceDefinition)
		{
			Type type2;
			return ReflectionUtils.ImplementsGenericDefinition(type, genericInterfaceDefinition, out type2);
		}

		// Token: 0x060005B0 RID: 1456 RVA: 0x00018418 File Offset: 0x00016618
		public static bool ImplementsGenericDefinition(Type type, Type genericInterfaceDefinition, [Nullable(2)] [NotNullWhen(true)] out Type implementingType)
		{
			ValidationUtils.ArgumentNotNull(type, "type");
			ValidationUtils.ArgumentNotNull(genericInterfaceDefinition, "genericInterfaceDefinition");
			if (!genericInterfaceDefinition.IsInterface() || !genericInterfaceDefinition.IsGenericTypeDefinition())
			{
				throw new ArgumentNullException("'{0}' is not a generic interface definition.".FormatWith(CultureInfo.InvariantCulture, genericInterfaceDefinition));
			}
			if (type.IsInterface() && type.IsGenericType())
			{
				Type genericTypeDefinition = type.GetGenericTypeDefinition();
				if (genericInterfaceDefinition == genericTypeDefinition)
				{
					implementingType = type;
					return true;
				}
			}
			foreach (Type type2 in type.GetInterfaces())
			{
				if (type2.IsGenericType())
				{
					Type genericTypeDefinition2 = type2.GetGenericTypeDefinition();
					if (genericInterfaceDefinition == genericTypeDefinition2)
					{
						implementingType = type2;
						return true;
					}
				}
			}
			implementingType = null;
			return false;
		}

		// Token: 0x060005B1 RID: 1457 RVA: 0x000184C4 File Offset: 0x000166C4
		public static bool InheritsGenericDefinition(Type type, Type genericClassDefinition)
		{
			Type type2;
			return ReflectionUtils.InheritsGenericDefinition(type, genericClassDefinition, out type2);
		}

		// Token: 0x060005B2 RID: 1458 RVA: 0x000184DC File Offset: 0x000166DC
		public static bool InheritsGenericDefinition(Type type, Type genericClassDefinition, [Nullable(2)] out Type implementingType)
		{
			ValidationUtils.ArgumentNotNull(type, "type");
			ValidationUtils.ArgumentNotNull(genericClassDefinition, "genericClassDefinition");
			if (!genericClassDefinition.IsClass() || !genericClassDefinition.IsGenericTypeDefinition())
			{
				throw new ArgumentNullException("'{0}' is not a generic class definition.".FormatWith(CultureInfo.InvariantCulture, genericClassDefinition));
			}
			return ReflectionUtils.InheritsGenericDefinitionInternal(type, genericClassDefinition, out implementingType);
		}

		// Token: 0x060005B3 RID: 1459 RVA: 0x00018530 File Offset: 0x00016730
		private static bool InheritsGenericDefinitionInternal(Type type, Type genericClassDefinition, [Nullable(2)] out Type implementingType)
		{
			Type type2 = type;
			while (!type2.IsGenericType() || !(genericClassDefinition == type2.GetGenericTypeDefinition()))
			{
				type2 = type2.BaseType();
				if (!(type2 != null))
				{
					implementingType = null;
					return false;
				}
			}
			implementingType = type2;
			return true;
		}

		/// <summary>
		/// Gets the type of the typed collection's items.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns>The type of the typed collection's items.</returns>
		// Token: 0x060005B4 RID: 1460 RVA: 0x00018570 File Offset: 0x00016770
		[return: Nullable(2)]
		public static Type GetCollectionItemType(Type type)
		{
			ValidationUtils.ArgumentNotNull(type, "type");
			if (type.IsArray)
			{
				return type.GetElementType();
			}
			Type type2;
			if (ReflectionUtils.ImplementsGenericDefinition(type, typeof(IEnumerable<>), out type2))
			{
				if (type2.IsGenericTypeDefinition())
				{
					throw new Exception("Type {0} is not a collection.".FormatWith(CultureInfo.InvariantCulture, type));
				}
				return type2.GetGenericArguments()[0];
			}
			else
			{
				if (typeof(IEnumerable).IsAssignableFrom(type))
				{
					return null;
				}
				throw new Exception("Type {0} is not a collection.".FormatWith(CultureInfo.InvariantCulture, type));
			}
		}

		// Token: 0x060005B5 RID: 1461 RVA: 0x000185FC File Offset: 0x000167FC
		[NullableContext(2)]
		public static void GetDictionaryKeyValueTypes([Nullable(1)] Type dictionaryType, out Type keyType, out Type valueType)
		{
			ValidationUtils.ArgumentNotNull(dictionaryType, "dictionaryType");
			Type type;
			if (ReflectionUtils.ImplementsGenericDefinition(dictionaryType, typeof(IDictionary<, >), out type))
			{
				if (type.IsGenericTypeDefinition())
				{
					throw new Exception("Type {0} is not a dictionary.".FormatWith(CultureInfo.InvariantCulture, dictionaryType));
				}
				Type[] genericArguments = type.GetGenericArguments();
				keyType = genericArguments[0];
				valueType = genericArguments[1];
				return;
			}
			else
			{
				if (typeof(IDictionary).IsAssignableFrom(dictionaryType))
				{
					keyType = null;
					valueType = null;
					return;
				}
				throw new Exception("Type {0} is not a dictionary.".FormatWith(CultureInfo.InvariantCulture, dictionaryType));
			}
		}

		/// <summary>
		/// Gets the member's underlying type.
		/// </summary>
		/// <param name="member">The member.</param>
		/// <returns>The underlying type of the member.</returns>
		// Token: 0x060005B6 RID: 1462 RVA: 0x00018688 File Offset: 0x00016888
		public static Type GetMemberUnderlyingType(MemberInfo member)
		{
			ValidationUtils.ArgumentNotNull(member, "member");
			MemberTypes memberTypes = member.MemberType();
			if (memberTypes <= MemberTypes.Field)
			{
				if (memberTypes == MemberTypes.Event)
				{
					return ((EventInfo)member).EventHandlerType;
				}
				if (memberTypes == MemberTypes.Field)
				{
					return ((FieldInfo)member).FieldType;
				}
			}
			else
			{
				if (memberTypes == MemberTypes.Method)
				{
					return ((MethodInfo)member).ReturnType;
				}
				if (memberTypes == MemberTypes.Property)
				{
					return ((PropertyInfo)member).PropertyType;
				}
			}
			throw new ArgumentException("MemberInfo must be of type FieldInfo, PropertyInfo, EventInfo or MethodInfo", "member");
		}

		// Token: 0x060005B7 RID: 1463 RVA: 0x00018700 File Offset: 0x00016900
		public static bool IsByRefLikeType(Type type)
		{
			if (!type.IsValueType())
			{
				return false;
			}
			Attribute[] attributes = ReflectionUtils.GetAttributes(type, null, false);
			for (int i = 0; i < attributes.Length; i++)
			{
				if (string.Equals(attributes[i].GetType().FullName, "System.Runtime.CompilerServices.IsByRefLikeAttribute", StringComparison.Ordinal))
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Determines whether the property is an indexed property.
		/// </summary>
		/// <param name="property">The property.</param>
		/// <returns>
		/// 	<c>true</c> if the property is an indexed property; otherwise, <c>false</c>.
		/// </returns>
		// Token: 0x060005B8 RID: 1464 RVA: 0x0001874B File Offset: 0x0001694B
		public static bool IsIndexedProperty(PropertyInfo property)
		{
			ValidationUtils.ArgumentNotNull(property, "property");
			return property.GetIndexParameters().Length != 0;
		}

		/// <summary>
		/// Gets the member's value on the object.
		/// </summary>
		/// <param name="member">The member.</param>
		/// <param name="target">The target object.</param>
		/// <returns>The member's value on the object.</returns>
		// Token: 0x060005B9 RID: 1465 RVA: 0x00018764 File Offset: 0x00016964
		[return: Nullable(2)]
		public static object GetMemberValue(MemberInfo member, object target)
		{
			ValidationUtils.ArgumentNotNull(member, "member");
			ValidationUtils.ArgumentNotNull(target, "target");
			MemberTypes memberTypes = member.MemberType();
			if (memberTypes != MemberTypes.Field)
			{
				if (memberTypes == MemberTypes.Property)
				{
					try
					{
						return ((PropertyInfo)member).GetValue(target, null);
					}
					catch (TargetParameterCountException innerException)
					{
						throw new ArgumentException("MemberInfo '{0}' has index parameters".FormatWith(CultureInfo.InvariantCulture, member.Name), innerException);
					}
				}
				throw new ArgumentException("MemberInfo '{0}' is not of type FieldInfo or PropertyInfo".FormatWith(CultureInfo.InvariantCulture, member.Name), "member");
			}
			return ((FieldInfo)member).GetValue(target);
		}

		/// <summary>
		/// Sets the member's value on the target object.
		/// </summary>
		/// <param name="member">The member.</param>
		/// <param name="target">The target.</param>
		/// <param name="value">The value.</param>
		// Token: 0x060005BA RID: 1466 RVA: 0x00018808 File Offset: 0x00016A08
		public static void SetMemberValue(MemberInfo member, object target, [Nullable(2)] object value)
		{
			ValidationUtils.ArgumentNotNull(member, "member");
			ValidationUtils.ArgumentNotNull(target, "target");
			MemberTypes memberTypes = member.MemberType();
			if (memberTypes == MemberTypes.Field)
			{
				((FieldInfo)member).SetValue(target, value);
				return;
			}
			if (memberTypes != MemberTypes.Property)
			{
				throw new ArgumentException("MemberInfo '{0}' must be of type FieldInfo or PropertyInfo".FormatWith(CultureInfo.InvariantCulture, member.Name), "member");
			}
			((PropertyInfo)member).SetValue(target, value, null);
		}

		/// <summary>
		/// Determines whether the specified MemberInfo can be read.
		/// </summary>
		/// <param name="member">The MemberInfo to determine whether can be read.</param>
		/// /// <param name="nonPublic">if set to <c>true</c> then allow the member to be gotten non-publicly.</param>
		/// <returns>
		/// 	<c>true</c> if the specified MemberInfo can be read; otherwise, <c>false</c>.
		/// </returns>
		// Token: 0x060005BB RID: 1467 RVA: 0x0001887C File Offset: 0x00016A7C
		public static bool CanReadMemberValue(MemberInfo member, bool nonPublic)
		{
			MemberTypes memberTypes = member.MemberType();
			if (memberTypes == MemberTypes.Field)
			{
				FieldInfo fieldInfo = (FieldInfo)member;
				return nonPublic || fieldInfo.IsPublic;
			}
			if (memberTypes != MemberTypes.Property)
			{
				return false;
			}
			PropertyInfo propertyInfo = (PropertyInfo)member;
			return propertyInfo.CanRead && (nonPublic || propertyInfo.GetGetMethod(nonPublic) != null);
		}

		/// <summary>
		/// Determines whether the specified MemberInfo can be set.
		/// </summary>
		/// <param name="member">The MemberInfo to determine whether can be set.</param>
		/// <param name="nonPublic">if set to <c>true</c> then allow the member to be set non-publicly.</param>
		/// <param name="canSetReadOnly">if set to <c>true</c> then allow the member to be set if read-only.</param>
		/// <returns>
		/// 	<c>true</c> if the specified MemberInfo can be set; otherwise, <c>false</c>.
		/// </returns>
		// Token: 0x060005BC RID: 1468 RVA: 0x000188D8 File Offset: 0x00016AD8
		public static bool CanSetMemberValue(MemberInfo member, bool nonPublic, bool canSetReadOnly)
		{
			MemberTypes memberTypes = member.MemberType();
			if (memberTypes == MemberTypes.Field)
			{
				FieldInfo fieldInfo = (FieldInfo)member;
				return !fieldInfo.IsLiteral && (!fieldInfo.IsInitOnly || canSetReadOnly) && (nonPublic || fieldInfo.IsPublic);
			}
			if (memberTypes != MemberTypes.Property)
			{
				return false;
			}
			PropertyInfo propertyInfo = (PropertyInfo)member;
			return propertyInfo.CanWrite && (nonPublic || propertyInfo.GetSetMethod(nonPublic) != null);
		}

		// Token: 0x060005BD RID: 1469 RVA: 0x0001894C File Offset: 0x00016B4C
		public static List<MemberInfo> GetFieldsAndProperties(Type type, BindingFlags bindingAttr)
		{
			List<MemberInfo> list = new List<MemberInfo>();
			list.AddRange(ReflectionUtils.GetFields(type, bindingAttr));
			list.AddRange(ReflectionUtils.GetProperties(type, bindingAttr));
			List<MemberInfo> list2 = new List<MemberInfo>(list.Count);
			foreach (IGrouping<string, MemberInfo> grouping in from m in list
				group m by m.Name)
			{
				if (grouping.Count<MemberInfo>() == 1)
				{
					list2.Add(grouping.First<MemberInfo>());
				}
				else
				{
					List<MemberInfo> list3 = new List<MemberInfo>();
					using (IEnumerator<MemberInfo> enumerator2 = grouping.GetEnumerator())
					{
						while (enumerator2.MoveNext())
						{
							MemberInfo memberInfo = enumerator2.Current;
							if (list3.Count == 0)
							{
								list3.Add(memberInfo);
							}
							else if ((!ReflectionUtils.IsOverridenGenericMember(memberInfo, bindingAttr) || memberInfo.Name == "Item") && !list3.Any((MemberInfo m) => m.DeclaringType == memberInfo.DeclaringType))
							{
								list3.Add(memberInfo);
							}
						}
					}
					list2.AddRange(list3);
				}
			}
			return list2;
		}

		// Token: 0x060005BE RID: 1470 RVA: 0x00018AAC File Offset: 0x00016CAC
		private static bool IsOverridenGenericMember(MemberInfo memberInfo, BindingFlags bindingAttr)
		{
			if (memberInfo.MemberType() != MemberTypes.Property)
			{
				return false;
			}
			PropertyInfo propertyInfo = (PropertyInfo)memberInfo;
			if (!propertyInfo.IsVirtual())
			{
				return false;
			}
			Type declaringType = propertyInfo.DeclaringType;
			if (!declaringType.IsGenericType())
			{
				return false;
			}
			Type genericTypeDefinition = declaringType.GetGenericTypeDefinition();
			if (genericTypeDefinition == null)
			{
				return false;
			}
			MemberInfo[] member = genericTypeDefinition.GetMember(propertyInfo.Name, bindingAttr);
			return member.Length != 0 && ReflectionUtils.GetMemberUnderlyingType(member[0]).IsGenericParameter;
		}

		// Token: 0x060005BF RID: 1471 RVA: 0x00018B1F File Offset: 0x00016D1F
		[return: Nullable(2)]
		public static T GetAttribute<[Nullable(0)] T>(object attributeProvider) where T : Attribute
		{
			return ReflectionUtils.GetAttribute<T>(attributeProvider, true);
		}

		// Token: 0x060005C0 RID: 1472 RVA: 0x00018B28 File Offset: 0x00016D28
		[return: Nullable(2)]
		public static T GetAttribute<[Nullable(0)] T>(object attributeProvider, bool inherit) where T : Attribute
		{
			T[] attributes = ReflectionUtils.GetAttributes<T>(attributeProvider, inherit);
			if (attributes == null)
			{
				return default(T);
			}
			return attributes.FirstOrDefault<T>();
		}

		// Token: 0x060005C1 RID: 1473 RVA: 0x00018B50 File Offset: 0x00016D50
		public static T[] GetAttributes<[Nullable(0)] T>(object attributeProvider, bool inherit) where T : Attribute
		{
			Attribute[] attributes = ReflectionUtils.GetAttributes(attributeProvider, typeof(T), inherit);
			T[] array = attributes as T[];
			if (array != null)
			{
				return array;
			}
			return attributes.Cast<T>().ToArray<T>();
		}

		// Token: 0x060005C2 RID: 1474 RVA: 0x00018B88 File Offset: 0x00016D88
		public static Attribute[] GetAttributes(object attributeProvider, [Nullable(2)] Type attributeType, bool inherit)
		{
			ValidationUtils.ArgumentNotNull(attributeProvider, "attributeProvider");
			Type type = attributeProvider as Type;
			if (type != null)
			{
				return ((attributeType != null) ? type.GetCustomAttributes(attributeType, inherit) : type.GetCustomAttributes(inherit)).Cast<Attribute>().ToArray<Attribute>();
			}
			Assembly assembly = attributeProvider as Assembly;
			if (assembly == null)
			{
				MemberInfo memberInfo = attributeProvider as MemberInfo;
				if (memberInfo == null)
				{
					Module module = attributeProvider as Module;
					if (module == null)
					{
						ParameterInfo parameterInfo = attributeProvider as ParameterInfo;
						if (parameterInfo == null)
						{
							ICustomAttributeProvider customAttributeProvider = (ICustomAttributeProvider)attributeProvider;
							return (Attribute[])((attributeType != null) ? customAttributeProvider.GetCustomAttributes(attributeType, inherit) : customAttributeProvider.GetCustomAttributes(inherit));
						}
						if (!(attributeType != null))
						{
							return Attribute.GetCustomAttributes(parameterInfo, inherit);
						}
						return Attribute.GetCustomAttributes(parameterInfo, attributeType, inherit);
					}
					else
					{
						if (!(attributeType != null))
						{
							return Attribute.GetCustomAttributes(module, inherit);
						}
						return Attribute.GetCustomAttributes(module, attributeType, inherit);
					}
				}
				else
				{
					if (!(attributeType != null))
					{
						return Attribute.GetCustomAttributes(memberInfo, inherit);
					}
					return Attribute.GetCustomAttributes(memberInfo, attributeType, inherit);
				}
			}
			else
			{
				if (!(attributeType != null))
				{
					return Attribute.GetCustomAttributes(assembly);
				}
				return Attribute.GetCustomAttributes(assembly, attributeType);
			}
		}

		// Token: 0x060005C3 RID: 1475 RVA: 0x00018C98 File Offset: 0x00016E98
		[return: Nullable(new byte[] { 0, 2, 1 })]
		public static StructMultiKey<string, string> SplitFullyQualifiedTypeName(string fullyQualifiedTypeName)
		{
			int? assemblyDelimiterIndex = ReflectionUtils.GetAssemblyDelimiterIndex(fullyQualifiedTypeName);
			string v;
			string v2;
			if (assemblyDelimiterIndex != null)
			{
				v = fullyQualifiedTypeName.Trim(0, assemblyDelimiterIndex.GetValueOrDefault());
				v2 = fullyQualifiedTypeName.Trim(assemblyDelimiterIndex.GetValueOrDefault() + 1, fullyQualifiedTypeName.Length - assemblyDelimiterIndex.GetValueOrDefault() - 1);
			}
			else
			{
				v = fullyQualifiedTypeName;
				v2 = null;
			}
			return new StructMultiKey<string, string>(v2, v);
		}

		// Token: 0x060005C4 RID: 1476 RVA: 0x00018CF4 File Offset: 0x00016EF4
		private static int? GetAssemblyDelimiterIndex(string fullyQualifiedTypeName)
		{
			int num = 0;
			for (int i = 0; i < fullyQualifiedTypeName.Length; i++)
			{
				char c = fullyQualifiedTypeName[i];
				if (c != ',')
				{
					if (c != '[')
					{
						if (c == ']')
						{
							num--;
						}
					}
					else
					{
						num++;
					}
				}
				else if (num == 0)
				{
					return new int?(i);
				}
			}
			return null;
		}

		// Token: 0x060005C5 RID: 1477 RVA: 0x00018D4C File Offset: 0x00016F4C
		[return: Nullable(2)]
		public static MemberInfo GetMemberInfoFromType(Type targetType, MemberInfo memberInfo)
		{
			if (memberInfo.MemberType() == MemberTypes.Property)
			{
				PropertyInfo propertyInfo = (PropertyInfo)memberInfo;
				Type[] types = (from p in propertyInfo.GetIndexParameters()
					select p.ParameterType).ToArray<Type>();
				return targetType.GetProperty(propertyInfo.Name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, propertyInfo.PropertyType, types, null);
			}
			return targetType.GetMember(memberInfo.Name, memberInfo.MemberType(), BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).SingleOrDefault<MemberInfo>();
		}

		// Token: 0x060005C6 RID: 1478 RVA: 0x00018DCB File Offset: 0x00016FCB
		public static IEnumerable<FieldInfo> GetFields(Type targetType, BindingFlags bindingAttr)
		{
			ValidationUtils.ArgumentNotNull(targetType, "targetType");
			List<MemberInfo> list = new List<MemberInfo>(targetType.GetFields(bindingAttr));
			ReflectionUtils.GetChildPrivateFields(list, targetType, bindingAttr);
			return list.Cast<FieldInfo>();
		}

		// Token: 0x060005C7 RID: 1479 RVA: 0x00018DF4 File Offset: 0x00016FF4
		private static void GetChildPrivateFields(IList<MemberInfo> initialFields, Type type, BindingFlags bindingAttr)
		{
			Type type2 = type;
			if ((bindingAttr & BindingFlags.NonPublic) != BindingFlags.Default)
			{
				BindingFlags bindingAttr2 = bindingAttr.RemoveFlag(BindingFlags.Public);
				while ((type2 = type2.BaseType()) != null)
				{
					IEnumerable<FieldInfo> collection = from f in type2.GetFields(bindingAttr2)
						where f.IsPrivate
						select f;
					initialFields.AddRange(collection);
				}
			}
		}

		// Token: 0x060005C8 RID: 1480 RVA: 0x00018E58 File Offset: 0x00017058
		public static IEnumerable<PropertyInfo> GetProperties(Type targetType, BindingFlags bindingAttr)
		{
			ValidationUtils.ArgumentNotNull(targetType, "targetType");
			List<PropertyInfo> list = new List<PropertyInfo>(targetType.GetProperties(bindingAttr));
			if (targetType.IsInterface())
			{
				foreach (Type type in targetType.GetInterfaces())
				{
					list.AddRange(type.GetProperties(bindingAttr));
				}
			}
			ReflectionUtils.GetChildPrivateProperties(list, targetType, bindingAttr);
			for (int j = 0; j < list.Count; j++)
			{
				PropertyInfo propertyInfo = list[j];
				if (propertyInfo.DeclaringType != targetType)
				{
					PropertyInfo value = (PropertyInfo)ReflectionUtils.GetMemberInfoFromType(propertyInfo.DeclaringType, propertyInfo);
					list[j] = value;
				}
			}
			return list;
		}

		// Token: 0x060005C9 RID: 1481 RVA: 0x00018F01 File Offset: 0x00017101
		public static BindingFlags RemoveFlag(this BindingFlags bindingAttr, BindingFlags flag)
		{
			if ((bindingAttr & flag) != flag)
			{
				return bindingAttr;
			}
			return bindingAttr ^ flag;
		}

		// Token: 0x060005CA RID: 1482 RVA: 0x00018F10 File Offset: 0x00017110
		private static void GetChildPrivateProperties(IList<PropertyInfo> initialProperties, Type type, BindingFlags bindingAttr)
		{
			Type type2 = type;
			while ((type2 = type2.BaseType()) != null)
			{
				foreach (PropertyInfo subTypeProperty in type2.GetProperties(bindingAttr))
				{
					ReflectionUtils.<>c__DisplayClass44_0 CS$<>8__locals1 = new ReflectionUtils.<>c__DisplayClass44_0();
					CS$<>8__locals1.subTypeProperty = subTypeProperty;
					if (!CS$<>8__locals1.subTypeProperty.IsVirtual())
					{
						if (!ReflectionUtils.IsPublic(CS$<>8__locals1.subTypeProperty))
						{
							int num = initialProperties.IndexOf((PropertyInfo p) => p.Name == CS$<>8__locals1.subTypeProperty.Name);
							if (num == -1)
							{
								initialProperties.Add(CS$<>8__locals1.subTypeProperty);
							}
							else if (!ReflectionUtils.IsPublic(initialProperties[num]))
							{
								initialProperties[num] = CS$<>8__locals1.subTypeProperty;
							}
						}
						else if (initialProperties.IndexOf((PropertyInfo p) => p.Name == CS$<>8__locals1.subTypeProperty.Name && p.DeclaringType == CS$<>8__locals1.subTypeProperty.DeclaringType) == -1)
						{
							initialProperties.Add(CS$<>8__locals1.subTypeProperty);
						}
					}
					else
					{
						ReflectionUtils.<>c__DisplayClass44_1 CS$<>8__locals2 = new ReflectionUtils.<>c__DisplayClass44_1();
						CS$<>8__locals2.CS$<>8__locals1 = CS$<>8__locals1;
						ReflectionUtils.<>c__DisplayClass44_1 CS$<>8__locals3 = CS$<>8__locals2;
						MethodInfo baseDefinition = CS$<>8__locals2.CS$<>8__locals1.subTypeProperty.GetBaseDefinition();
						CS$<>8__locals3.subTypePropertyDeclaringType = ((baseDefinition != null) ? baseDefinition.DeclaringType : null) ?? CS$<>8__locals2.CS$<>8__locals1.subTypeProperty.DeclaringType;
						if (initialProperties.IndexOf(delegate(PropertyInfo p)
						{
							if (p.Name == CS$<>8__locals2.CS$<>8__locals1.subTypeProperty.Name && p.IsVirtual())
							{
								MethodInfo baseDefinition2 = p.GetBaseDefinition();
								return (((baseDefinition2 != null) ? baseDefinition2.DeclaringType : null) ?? p.DeclaringType).IsAssignableFrom(CS$<>8__locals2.subTypePropertyDeclaringType);
							}
							return false;
						}) == -1)
						{
							initialProperties.Add(CS$<>8__locals2.CS$<>8__locals1.subTypeProperty);
						}
					}
				}
			}
		}

		// Token: 0x060005CB RID: 1483 RVA: 0x00019070 File Offset: 0x00017270
		public static bool IsMethodOverridden(Type currentType, Type methodDeclaringType, string method)
		{
			return currentType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Any((MethodInfo info) => info.Name == method && info.DeclaringType != methodDeclaringType && info.GetBaseDefinition().DeclaringType == methodDeclaringType);
		}

		// Token: 0x060005CC RID: 1484 RVA: 0x000190AC File Offset: 0x000172AC
		[return: Nullable(2)]
		public static object GetDefaultValue(Type type)
		{
			if (!type.IsValueType())
			{
				return null;
			}
			PrimitiveTypeCode typeCode = ConvertUtils.GetTypeCode(type);
			switch (typeCode)
			{
			case PrimitiveTypeCode.Char:
			case PrimitiveTypeCode.SByte:
			case PrimitiveTypeCode.Int16:
			case PrimitiveTypeCode.UInt16:
			case PrimitiveTypeCode.Int32:
			case PrimitiveTypeCode.Byte:
			case PrimitiveTypeCode.UInt32:
				return 0;
			case PrimitiveTypeCode.CharNullable:
			case PrimitiveTypeCode.BooleanNullable:
			case PrimitiveTypeCode.SByteNullable:
			case PrimitiveTypeCode.Int16Nullable:
			case PrimitiveTypeCode.UInt16Nullable:
			case PrimitiveTypeCode.Int32Nullable:
			case PrimitiveTypeCode.ByteNullable:
			case PrimitiveTypeCode.UInt32Nullable:
			case PrimitiveTypeCode.Int64Nullable:
			case PrimitiveTypeCode.UInt64Nullable:
			case PrimitiveTypeCode.SingleNullable:
			case PrimitiveTypeCode.DoubleNullable:
			case PrimitiveTypeCode.DateTimeNullable:
			case PrimitiveTypeCode.DateTimeOffsetNullable:
			case PrimitiveTypeCode.DecimalNullable:
				break;
			case PrimitiveTypeCode.Boolean:
				return false;
			case PrimitiveTypeCode.Int64:
			case PrimitiveTypeCode.UInt64:
				return 0L;
			case PrimitiveTypeCode.Single:
				return 0f;
			case PrimitiveTypeCode.Double:
				return 0.0;
			case PrimitiveTypeCode.DateTime:
				return default(DateTime);
			case PrimitiveTypeCode.DateTimeOffset:
				return default(DateTimeOffset);
			case PrimitiveTypeCode.Decimal:
				return 0m;
			case PrimitiveTypeCode.Guid:
				return default(Guid);
			default:
				if (typeCode == PrimitiveTypeCode.BigInteger)
				{
					return default(BigInteger);
				}
				break;
			}
			if (ReflectionUtils.IsNullable(type))
			{
				return null;
			}
			return Activator.CreateInstance(type);
		}

		// Token: 0x0400021B RID: 539
		public static readonly Type[] EmptyTypes = Type.EmptyTypes;
	}
}
