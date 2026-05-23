using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;

namespace System
{
	// Token: 0x020000AD RID: 173
	[AttributeUsage(AttributeTargets.All, Inherited = true, AllowMultiple = false)]
	[ClassInterface(ClassInterfaceType.None)]
	[ComDefaultInterface(typeof(_Attribute))]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public abstract class Attribute : _Attribute
	{
		// Token: 0x060009CC RID: 2508 RVA: 0x0001F7BC File Offset: 0x0001D9BC
		private static Attribute[] InternalGetCustomAttributes(PropertyInfo element, Type type, bool inherit)
		{
			Attribute[] array = (Attribute[])element.GetCustomAttributes(type, inherit);
			if (!inherit)
			{
				return array;
			}
			Dictionary<Type, AttributeUsageAttribute> types = new Dictionary<Type, AttributeUsageAttribute>(11);
			List<Attribute> list = new List<Attribute>();
			Attribute.CopyToArrayList(list, array, types);
			Type[] indexParameterTypes = Attribute.GetIndexParameterTypes(element);
			PropertyInfo parentDefinition = Attribute.GetParentDefinition(element, indexParameterTypes);
			while (parentDefinition != null)
			{
				array = Attribute.GetCustomAttributes(parentDefinition, type, false);
				Attribute.AddAttributesToList(list, array, types);
				parentDefinition = Attribute.GetParentDefinition(parentDefinition, indexParameterTypes);
			}
			Array array2 = Attribute.CreateAttributeArrayHelper(type, list.Count);
			Array.Copy(list.ToArray(), 0, array2, 0, list.Count);
			return (Attribute[])array2;
		}

		// Token: 0x060009CD RID: 2509 RVA: 0x0001F854 File Offset: 0x0001DA54
		private static bool InternalIsDefined(PropertyInfo element, Type attributeType, bool inherit)
		{
			if (element.IsDefined(attributeType, inherit))
			{
				return true;
			}
			if (inherit)
			{
				AttributeUsageAttribute attributeUsageAttribute = Attribute.InternalGetAttributeUsage(attributeType);
				if (!attributeUsageAttribute.Inherited)
				{
					return false;
				}
				Type[] indexParameterTypes = Attribute.GetIndexParameterTypes(element);
				PropertyInfo parentDefinition = Attribute.GetParentDefinition(element, indexParameterTypes);
				while (parentDefinition != null)
				{
					if (parentDefinition.IsDefined(attributeType, false))
					{
						return true;
					}
					parentDefinition = Attribute.GetParentDefinition(parentDefinition, indexParameterTypes);
				}
			}
			return false;
		}

		// Token: 0x060009CE RID: 2510 RVA: 0x0001F8B0 File Offset: 0x0001DAB0
		private static PropertyInfo GetParentDefinition(PropertyInfo property, Type[] propertyParameters)
		{
			MethodInfo methodInfo = property.GetGetMethod(true);
			if (methodInfo == null)
			{
				methodInfo = property.GetSetMethod(true);
			}
			RuntimeMethodInfo runtimeMethodInfo = methodInfo as RuntimeMethodInfo;
			if (runtimeMethodInfo != null)
			{
				runtimeMethodInfo = runtimeMethodInfo.GetParentDefinition();
				if (runtimeMethodInfo != null)
				{
					return runtimeMethodInfo.DeclaringType.GetProperty(property.Name, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, property.PropertyType, propertyParameters, null);
				}
			}
			return null;
		}

		// Token: 0x060009CF RID: 2511 RVA: 0x0001F914 File Offset: 0x0001DB14
		private static Attribute[] InternalGetCustomAttributes(EventInfo element, Type type, bool inherit)
		{
			Attribute[] array = (Attribute[])element.GetCustomAttributes(type, inherit);
			if (inherit)
			{
				Dictionary<Type, AttributeUsageAttribute> types = new Dictionary<Type, AttributeUsageAttribute>(11);
				List<Attribute> list = new List<Attribute>();
				Attribute.CopyToArrayList(list, array, types);
				EventInfo parentDefinition = Attribute.GetParentDefinition(element);
				while (parentDefinition != null)
				{
					array = Attribute.GetCustomAttributes(parentDefinition, type, false);
					Attribute.AddAttributesToList(list, array, types);
					parentDefinition = Attribute.GetParentDefinition(parentDefinition);
				}
				Array array2 = Attribute.CreateAttributeArrayHelper(type, list.Count);
				Array.Copy(list.ToArray(), 0, array2, 0, list.Count);
				return (Attribute[])array2;
			}
			return array;
		}

		// Token: 0x060009D0 RID: 2512 RVA: 0x0001F9A0 File Offset: 0x0001DBA0
		private static EventInfo GetParentDefinition(EventInfo ev)
		{
			MethodInfo addMethod = ev.GetAddMethod(true);
			RuntimeMethodInfo runtimeMethodInfo = addMethod as RuntimeMethodInfo;
			if (runtimeMethodInfo != null)
			{
				runtimeMethodInfo = runtimeMethodInfo.GetParentDefinition();
				if (runtimeMethodInfo != null)
				{
					return runtimeMethodInfo.DeclaringType.GetEvent(ev.Name);
				}
			}
			return null;
		}

		// Token: 0x060009D1 RID: 2513 RVA: 0x0001F9E8 File Offset: 0x0001DBE8
		private static bool InternalIsDefined(EventInfo element, Type attributeType, bool inherit)
		{
			if (element.IsDefined(attributeType, inherit))
			{
				return true;
			}
			if (inherit)
			{
				AttributeUsageAttribute attributeUsageAttribute = Attribute.InternalGetAttributeUsage(attributeType);
				if (!attributeUsageAttribute.Inherited)
				{
					return false;
				}
				EventInfo parentDefinition = Attribute.GetParentDefinition(element);
				while (parentDefinition != null)
				{
					if (parentDefinition.IsDefined(attributeType, false))
					{
						return true;
					}
					parentDefinition = Attribute.GetParentDefinition(parentDefinition);
				}
			}
			return false;
		}

		// Token: 0x060009D2 RID: 2514 RVA: 0x0001FA3C File Offset: 0x0001DC3C
		private static ParameterInfo GetParentDefinition(ParameterInfo param)
		{
			RuntimeMethodInfo runtimeMethodInfo = param.Member as RuntimeMethodInfo;
			if (runtimeMethodInfo != null)
			{
				runtimeMethodInfo = runtimeMethodInfo.GetParentDefinition();
				if (runtimeMethodInfo != null)
				{
					ParameterInfo[] parameters = runtimeMethodInfo.GetParameters();
					return parameters[param.Position];
				}
			}
			return null;
		}

		// Token: 0x060009D3 RID: 2515 RVA: 0x0001FA80 File Offset: 0x0001DC80
		private static Attribute[] InternalParamGetCustomAttributes(ParameterInfo param, Type type, bool inherit)
		{
			List<Type> list = new List<Type>();
			if (type == null)
			{
				type = typeof(Attribute);
			}
			object[] customAttributes = param.GetCustomAttributes(type, false);
			for (int i = 0; i < customAttributes.Length; i++)
			{
				Type type2 = customAttributes[i].GetType();
				AttributeUsageAttribute attributeUsageAttribute = Attribute.InternalGetAttributeUsage(type2);
				if (!attributeUsageAttribute.AllowMultiple)
				{
					list.Add(type2);
				}
			}
			Attribute[] array;
			if (customAttributes.Length == 0)
			{
				array = Attribute.CreateAttributeArrayHelper(type, 0);
			}
			else
			{
				array = (Attribute[])customAttributes;
			}
			if (param.Member.DeclaringType == null)
			{
				return array;
			}
			if (!inherit)
			{
				return array;
			}
			for (ParameterInfo parentDefinition = Attribute.GetParentDefinition(param); parentDefinition != null; parentDefinition = Attribute.GetParentDefinition(parentDefinition))
			{
				customAttributes = parentDefinition.GetCustomAttributes(type, false);
				int num = 0;
				for (int j = 0; j < customAttributes.Length; j++)
				{
					Type type3 = customAttributes[j].GetType();
					AttributeUsageAttribute attributeUsageAttribute2 = Attribute.InternalGetAttributeUsage(type3);
					if (attributeUsageAttribute2.Inherited && !list.Contains(type3))
					{
						if (!attributeUsageAttribute2.AllowMultiple)
						{
							list.Add(type3);
						}
						num++;
					}
					else
					{
						customAttributes[j] = null;
					}
				}
				Attribute[] array2 = Attribute.CreateAttributeArrayHelper(type, num);
				num = 0;
				for (int k = 0; k < customAttributes.Length; k++)
				{
					if (customAttributes[k] != null)
					{
						array2[num] = (Attribute)customAttributes[k];
						num++;
					}
				}
				Attribute[] array3 = array;
				array = Attribute.CreateAttributeArrayHelper(type, array3.Length + num);
				Array.Copy(array3, array, array3.Length);
				int num2 = array3.Length;
				for (int l = 0; l < array2.Length; l++)
				{
					array[num2 + l] = array2[l];
				}
			}
			return array;
		}

		// Token: 0x060009D4 RID: 2516 RVA: 0x0001FC14 File Offset: 0x0001DE14
		private static bool InternalParamIsDefined(ParameterInfo param, Type type, bool inherit)
		{
			if (param.IsDefined(type, false))
			{
				return true;
			}
			if (param.Member.DeclaringType == null || !inherit)
			{
				return false;
			}
			for (ParameterInfo parentDefinition = Attribute.GetParentDefinition(param); parentDefinition != null; parentDefinition = Attribute.GetParentDefinition(parentDefinition))
			{
				object[] customAttributes = parentDefinition.GetCustomAttributes(type, false);
				for (int i = 0; i < customAttributes.Length; i++)
				{
					Type type2 = customAttributes[i].GetType();
					AttributeUsageAttribute attributeUsageAttribute = Attribute.InternalGetAttributeUsage(type2);
					if (customAttributes[i] is Attribute && attributeUsageAttribute.Inherited)
					{
						return true;
					}
				}
			}
			return false;
		}

		// Token: 0x060009D5 RID: 2517 RVA: 0x0001FC98 File Offset: 0x0001DE98
		private static void CopyToArrayList(List<Attribute> attributeList, Attribute[] attributes, Dictionary<Type, AttributeUsageAttribute> types)
		{
			for (int i = 0; i < attributes.Length; i++)
			{
				attributeList.Add(attributes[i]);
				Type type = attributes[i].GetType();
				if (!types.ContainsKey(type))
				{
					types[type] = Attribute.InternalGetAttributeUsage(type);
				}
			}
		}

		// Token: 0x060009D6 RID: 2518 RVA: 0x0001FCDC File Offset: 0x0001DEDC
		private static Type[] GetIndexParameterTypes(PropertyInfo element)
		{
			ParameterInfo[] indexParameters = element.GetIndexParameters();
			if (indexParameters.Length != 0)
			{
				Type[] array = new Type[indexParameters.Length];
				for (int i = 0; i < indexParameters.Length; i++)
				{
					array[i] = indexParameters[i].ParameterType;
				}
				return array;
			}
			return Array.Empty<Type>();
		}

		// Token: 0x060009D7 RID: 2519 RVA: 0x0001FD20 File Offset: 0x0001DF20
		private static void AddAttributesToList(List<Attribute> attributeList, Attribute[] attributes, Dictionary<Type, AttributeUsageAttribute> types)
		{
			for (int i = 0; i < attributes.Length; i++)
			{
				Type type = attributes[i].GetType();
				AttributeUsageAttribute attributeUsageAttribute = null;
				types.TryGetValue(type, out attributeUsageAttribute);
				if (attributeUsageAttribute == null)
				{
					attributeUsageAttribute = Attribute.InternalGetAttributeUsage(type);
					types[type] = attributeUsageAttribute;
					if (attributeUsageAttribute.Inherited)
					{
						attributeList.Add(attributes[i]);
					}
				}
				else if (attributeUsageAttribute.Inherited && attributeUsageAttribute.AllowMultiple)
				{
					attributeList.Add(attributes[i]);
				}
			}
		}

		// Token: 0x060009D8 RID: 2520 RVA: 0x0001FD90 File Offset: 0x0001DF90
		private static AttributeUsageAttribute InternalGetAttributeUsage(Type type)
		{
			object[] customAttributes = type.GetCustomAttributes(typeof(AttributeUsageAttribute), false);
			if (customAttributes.Length == 1)
			{
				return (AttributeUsageAttribute)customAttributes[0];
			}
			if (customAttributes.Length == 0)
			{
				return AttributeUsageAttribute.Default;
			}
			throw new FormatException(Environment.GetResourceString("Format_AttributeUsage", new object[] { type }));
		}

		// Token: 0x060009D9 RID: 2521 RVA: 0x0001FDE1 File Offset: 0x0001DFE1
		[SecuritySafeCritical]
		private static Attribute[] CreateAttributeArrayHelper(Type elementType, int elementCount)
		{
			return (Attribute[])Array.UnsafeCreateInstance(elementType, elementCount);
		}

		// Token: 0x060009DA RID: 2522 RVA: 0x0001FDEF File Offset: 0x0001DFEF
		[__DynamicallyInvokable]
		public static Attribute[] GetCustomAttributes(MemberInfo element, Type type)
		{
			return Attribute.GetCustomAttributes(element, type, true);
		}

		// Token: 0x060009DB RID: 2523 RVA: 0x0001FDFC File Offset: 0x0001DFFC
		[__DynamicallyInvokable]
		public static Attribute[] GetCustomAttributes(MemberInfo element, Type type, bool inherit)
		{
			if (element == null)
			{
				throw new ArgumentNullException("element");
			}
			if (type == null)
			{
				throw new ArgumentNullException("type");
			}
			if (!type.IsSubclassOf(typeof(Attribute)) && type != typeof(Attribute))
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_MustHaveAttributeBaseClass"));
			}
			MemberTypes memberType = element.MemberType;
			if (memberType == MemberTypes.Event)
			{
				return Attribute.InternalGetCustomAttributes((EventInfo)element, type, inherit);
			}
			if (memberType == MemberTypes.Property)
			{
				return Attribute.InternalGetCustomAttributes((PropertyInfo)element, type, inherit);
			}
			return element.GetCustomAttributes(type, inherit) as Attribute[];
		}

		// Token: 0x060009DC RID: 2524 RVA: 0x0001FE9E File Offset: 0x0001E09E
		[__DynamicallyInvokable]
		public static Attribute[] GetCustomAttributes(MemberInfo element)
		{
			return Attribute.GetCustomAttributes(element, true);
		}

		// Token: 0x060009DD RID: 2525 RVA: 0x0001FEA8 File Offset: 0x0001E0A8
		[__DynamicallyInvokable]
		public static Attribute[] GetCustomAttributes(MemberInfo element, bool inherit)
		{
			if (element == null)
			{
				throw new ArgumentNullException("element");
			}
			MemberTypes memberType = element.MemberType;
			if (memberType == MemberTypes.Event)
			{
				return Attribute.InternalGetCustomAttributes((EventInfo)element, typeof(Attribute), inherit);
			}
			if (memberType == MemberTypes.Property)
			{
				return Attribute.InternalGetCustomAttributes((PropertyInfo)element, typeof(Attribute), inherit);
			}
			return element.GetCustomAttributes(typeof(Attribute), inherit) as Attribute[];
		}

		// Token: 0x060009DE RID: 2526 RVA: 0x0001FF1D File Offset: 0x0001E11D
		[__DynamicallyInvokable]
		public static bool IsDefined(MemberInfo element, Type attributeType)
		{
			return Attribute.IsDefined(element, attributeType, true);
		}

		// Token: 0x060009DF RID: 2527 RVA: 0x0001FF28 File Offset: 0x0001E128
		[__DynamicallyInvokable]
		public static bool IsDefined(MemberInfo element, Type attributeType, bool inherit)
		{
			if (element == null)
			{
				throw new ArgumentNullException("element");
			}
			if (attributeType == null)
			{
				throw new ArgumentNullException("attributeType");
			}
			if (!attributeType.IsSubclassOf(typeof(Attribute)) && attributeType != typeof(Attribute))
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_MustHaveAttributeBaseClass"));
			}
			MemberTypes memberType = element.MemberType;
			if (memberType == MemberTypes.Event)
			{
				return Attribute.InternalIsDefined((EventInfo)element, attributeType, inherit);
			}
			if (memberType == MemberTypes.Property)
			{
				return Attribute.InternalIsDefined((PropertyInfo)element, attributeType, inherit);
			}
			return element.IsDefined(attributeType, inherit);
		}

		// Token: 0x060009E0 RID: 2528 RVA: 0x0001FFC5 File Offset: 0x0001E1C5
		[__DynamicallyInvokable]
		public static Attribute GetCustomAttribute(MemberInfo element, Type attributeType)
		{
			return Attribute.GetCustomAttribute(element, attributeType, true);
		}

		// Token: 0x060009E1 RID: 2529 RVA: 0x0001FFD0 File Offset: 0x0001E1D0
		[__DynamicallyInvokable]
		public static Attribute GetCustomAttribute(MemberInfo element, Type attributeType, bool inherit)
		{
			Attribute[] customAttributes = Attribute.GetCustomAttributes(element, attributeType, inherit);
			if (customAttributes == null || customAttributes.Length == 0)
			{
				return null;
			}
			if (customAttributes.Length == 1)
			{
				return customAttributes[0];
			}
			throw new AmbiguousMatchException(Environment.GetResourceString("RFLCT.AmbigCust"));
		}

		// Token: 0x060009E2 RID: 2530 RVA: 0x00020008 File Offset: 0x0001E208
		[__DynamicallyInvokable]
		public static Attribute[] GetCustomAttributes(ParameterInfo element)
		{
			return Attribute.GetCustomAttributes(element, true);
		}

		// Token: 0x060009E3 RID: 2531 RVA: 0x00020011 File Offset: 0x0001E211
		[__DynamicallyInvokable]
		public static Attribute[] GetCustomAttributes(ParameterInfo element, Type attributeType)
		{
			return Attribute.GetCustomAttributes(element, attributeType, true);
		}

		// Token: 0x060009E4 RID: 2532 RVA: 0x0002001C File Offset: 0x0001E21C
		[__DynamicallyInvokable]
		public static Attribute[] GetCustomAttributes(ParameterInfo element, Type attributeType, bool inherit)
		{
			if (element == null)
			{
				throw new ArgumentNullException("element");
			}
			if (attributeType == null)
			{
				throw new ArgumentNullException("attributeType");
			}
			if (!attributeType.IsSubclassOf(typeof(Attribute)) && attributeType != typeof(Attribute))
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_MustHaveAttributeBaseClass"));
			}
			if (element.Member == null)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidParameterInfo"), "element");
			}
			MemberInfo member = element.Member;
			if (member.MemberType == MemberTypes.Method && inherit)
			{
				return Attribute.InternalParamGetCustomAttributes(element, attributeType, inherit);
			}
			return element.GetCustomAttributes(attributeType, inherit) as Attribute[];
		}

		// Token: 0x060009E5 RID: 2533 RVA: 0x000200CC File Offset: 0x0001E2CC
		[__DynamicallyInvokable]
		public static Attribute[] GetCustomAttributes(ParameterInfo element, bool inherit)
		{
			if (element == null)
			{
				throw new ArgumentNullException("element");
			}
			if (element.Member == null)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidParameterInfo"), "element");
			}
			MemberInfo member = element.Member;
			if (member.MemberType == MemberTypes.Method && inherit)
			{
				return Attribute.InternalParamGetCustomAttributes(element, null, inherit);
			}
			return element.GetCustomAttributes(typeof(Attribute), inherit) as Attribute[];
		}

		// Token: 0x060009E6 RID: 2534 RVA: 0x0002013D File Offset: 0x0001E33D
		[__DynamicallyInvokable]
		public static bool IsDefined(ParameterInfo element, Type attributeType)
		{
			return Attribute.IsDefined(element, attributeType, true);
		}

		// Token: 0x060009E7 RID: 2535 RVA: 0x00020148 File Offset: 0x0001E348
		[__DynamicallyInvokable]
		public static bool IsDefined(ParameterInfo element, Type attributeType, bool inherit)
		{
			if (element == null)
			{
				throw new ArgumentNullException("element");
			}
			if (attributeType == null)
			{
				throw new ArgumentNullException("attributeType");
			}
			if (!attributeType.IsSubclassOf(typeof(Attribute)) && attributeType != typeof(Attribute))
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_MustHaveAttributeBaseClass"));
			}
			MemberInfo member = element.Member;
			MemberTypes memberType = member.MemberType;
			if (memberType == MemberTypes.Constructor)
			{
				return element.IsDefined(attributeType, false);
			}
			if (memberType == MemberTypes.Method)
			{
				return Attribute.InternalParamIsDefined(element, attributeType, inherit);
			}
			if (memberType != MemberTypes.Property)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidParamInfo"));
			}
			return element.IsDefined(attributeType, false);
		}

		// Token: 0x060009E8 RID: 2536 RVA: 0x000201F2 File Offset: 0x0001E3F2
		[__DynamicallyInvokable]
		public static Attribute GetCustomAttribute(ParameterInfo element, Type attributeType)
		{
			return Attribute.GetCustomAttribute(element, attributeType, true);
		}

		// Token: 0x060009E9 RID: 2537 RVA: 0x000201FC File Offset: 0x0001E3FC
		[__DynamicallyInvokable]
		public static Attribute GetCustomAttribute(ParameterInfo element, Type attributeType, bool inherit)
		{
			Attribute[] customAttributes = Attribute.GetCustomAttributes(element, attributeType, inherit);
			if (customAttributes == null || customAttributes.Length == 0)
			{
				return null;
			}
			if (customAttributes.Length == 0)
			{
				return null;
			}
			if (customAttributes.Length == 1)
			{
				return customAttributes[0];
			}
			throw new AmbiguousMatchException(Environment.GetResourceString("RFLCT.AmbigCust"));
		}

		// Token: 0x060009EA RID: 2538 RVA: 0x0002023A File Offset: 0x0001E43A
		public static Attribute[] GetCustomAttributes(Module element, Type attributeType)
		{
			return Attribute.GetCustomAttributes(element, attributeType, true);
		}

		// Token: 0x060009EB RID: 2539 RVA: 0x00020244 File Offset: 0x0001E444
		public static Attribute[] GetCustomAttributes(Module element)
		{
			return Attribute.GetCustomAttributes(element, true);
		}

		// Token: 0x060009EC RID: 2540 RVA: 0x0002024D File Offset: 0x0001E44D
		public static Attribute[] GetCustomAttributes(Module element, bool inherit)
		{
			if (element == null)
			{
				throw new ArgumentNullException("element");
			}
			return (Attribute[])element.GetCustomAttributes(typeof(Attribute), inherit);
		}

		// Token: 0x060009ED RID: 2541 RVA: 0x0002027C File Offset: 0x0001E47C
		public static Attribute[] GetCustomAttributes(Module element, Type attributeType, bool inherit)
		{
			if (element == null)
			{
				throw new ArgumentNullException("element");
			}
			if (attributeType == null)
			{
				throw new ArgumentNullException("attributeType");
			}
			if (!attributeType.IsSubclassOf(typeof(Attribute)) && attributeType != typeof(Attribute))
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_MustHaveAttributeBaseClass"));
			}
			return (Attribute[])element.GetCustomAttributes(attributeType, inherit);
		}

		// Token: 0x060009EE RID: 2542 RVA: 0x000202F2 File Offset: 0x0001E4F2
		public static bool IsDefined(Module element, Type attributeType)
		{
			return Attribute.IsDefined(element, attributeType, false);
		}

		// Token: 0x060009EF RID: 2543 RVA: 0x000202FC File Offset: 0x0001E4FC
		public static bool IsDefined(Module element, Type attributeType, bool inherit)
		{
			if (element == null)
			{
				throw new ArgumentNullException("element");
			}
			if (attributeType == null)
			{
				throw new ArgumentNullException("attributeType");
			}
			if (!attributeType.IsSubclassOf(typeof(Attribute)) && attributeType != typeof(Attribute))
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_MustHaveAttributeBaseClass"));
			}
			return element.IsDefined(attributeType, false);
		}

		// Token: 0x060009F0 RID: 2544 RVA: 0x0002036D File Offset: 0x0001E56D
		public static Attribute GetCustomAttribute(Module element, Type attributeType)
		{
			return Attribute.GetCustomAttribute(element, attributeType, true);
		}

		// Token: 0x060009F1 RID: 2545 RVA: 0x00020378 File Offset: 0x0001E578
		public static Attribute GetCustomAttribute(Module element, Type attributeType, bool inherit)
		{
			Attribute[] customAttributes = Attribute.GetCustomAttributes(element, attributeType, inherit);
			if (customAttributes == null || customAttributes.Length == 0)
			{
				return null;
			}
			if (customAttributes.Length == 1)
			{
				return customAttributes[0];
			}
			throw new AmbiguousMatchException(Environment.GetResourceString("RFLCT.AmbigCust"));
		}

		// Token: 0x060009F2 RID: 2546 RVA: 0x000203B0 File Offset: 0x0001E5B0
		[__DynamicallyInvokable]
		public static Attribute[] GetCustomAttributes(Assembly element, Type attributeType)
		{
			return Attribute.GetCustomAttributes(element, attributeType, true);
		}

		// Token: 0x060009F3 RID: 2547 RVA: 0x000203BC File Offset: 0x0001E5BC
		public static Attribute[] GetCustomAttributes(Assembly element, Type attributeType, bool inherit)
		{
			if (element == null)
			{
				throw new ArgumentNullException("element");
			}
			if (attributeType == null)
			{
				throw new ArgumentNullException("attributeType");
			}
			if (!attributeType.IsSubclassOf(typeof(Attribute)) && attributeType != typeof(Attribute))
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_MustHaveAttributeBaseClass"));
			}
			return (Attribute[])element.GetCustomAttributes(attributeType, inherit);
		}

		// Token: 0x060009F4 RID: 2548 RVA: 0x00020432 File Offset: 0x0001E632
		[__DynamicallyInvokable]
		public static Attribute[] GetCustomAttributes(Assembly element)
		{
			return Attribute.GetCustomAttributes(element, true);
		}

		// Token: 0x060009F5 RID: 2549 RVA: 0x0002043B File Offset: 0x0001E63B
		public static Attribute[] GetCustomAttributes(Assembly element, bool inherit)
		{
			if (element == null)
			{
				throw new ArgumentNullException("element");
			}
			return (Attribute[])element.GetCustomAttributes(typeof(Attribute), inherit);
		}

		// Token: 0x060009F6 RID: 2550 RVA: 0x00020467 File Offset: 0x0001E667
		[__DynamicallyInvokable]
		public static bool IsDefined(Assembly element, Type attributeType)
		{
			return Attribute.IsDefined(element, attributeType, true);
		}

		// Token: 0x060009F7 RID: 2551 RVA: 0x00020474 File Offset: 0x0001E674
		public static bool IsDefined(Assembly element, Type attributeType, bool inherit)
		{
			if (element == null)
			{
				throw new ArgumentNullException("element");
			}
			if (attributeType == null)
			{
				throw new ArgumentNullException("attributeType");
			}
			if (!attributeType.IsSubclassOf(typeof(Attribute)) && attributeType != typeof(Attribute))
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_MustHaveAttributeBaseClass"));
			}
			return element.IsDefined(attributeType, false);
		}

		// Token: 0x060009F8 RID: 2552 RVA: 0x000204E5 File Offset: 0x0001E6E5
		[__DynamicallyInvokable]
		public static Attribute GetCustomAttribute(Assembly element, Type attributeType)
		{
			return Attribute.GetCustomAttribute(element, attributeType, true);
		}

		// Token: 0x060009F9 RID: 2553 RVA: 0x000204F0 File Offset: 0x0001E6F0
		public static Attribute GetCustomAttribute(Assembly element, Type attributeType, bool inherit)
		{
			Attribute[] customAttributes = Attribute.GetCustomAttributes(element, attributeType, inherit);
			if (customAttributes == null || customAttributes.Length == 0)
			{
				return null;
			}
			if (customAttributes.Length == 1)
			{
				return customAttributes[0];
			}
			throw new AmbiguousMatchException(Environment.GetResourceString("RFLCT.AmbigCust"));
		}

		// Token: 0x060009FA RID: 2554 RVA: 0x00020528 File Offset: 0x0001E728
		[__DynamicallyInvokable]
		protected Attribute()
		{
		}

		// Token: 0x060009FB RID: 2555 RVA: 0x00020530 File Offset: 0x0001E730
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			RuntimeType runtimeType = (RuntimeType)base.GetType();
			RuntimeType left = (RuntimeType)obj.GetType();
			if (left != runtimeType)
			{
				return false;
			}
			FieldInfo[] fields = runtimeType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			for (int i = 0; i < fields.Length; i++)
			{
				object thisValue = ((RtFieldInfo)fields[i]).UnsafeGetValue(this);
				object thatValue = ((RtFieldInfo)fields[i]).UnsafeGetValue(obj);
				if (!Attribute.AreFieldValuesEqual(thisValue, thatValue))
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x060009FC RID: 2556 RVA: 0x000205B4 File Offset: 0x0001E7B4
		private static bool AreFieldValuesEqual(object thisValue, object thatValue)
		{
			if (thisValue == null && thatValue == null)
			{
				return true;
			}
			if (thisValue == null || thatValue == null)
			{
				return false;
			}
			if (thisValue.GetType().IsArray)
			{
				if (!thisValue.GetType().Equals(thatValue.GetType()))
				{
					return false;
				}
				Array array = thisValue as Array;
				Array array2 = thatValue as Array;
				if (array.Length != array2.Length)
				{
					return false;
				}
				for (int i = 0; i < array.Length; i++)
				{
					if (!Attribute.AreFieldValuesEqual(array.GetValue(i), array2.GetValue(i)))
					{
						return false;
					}
				}
			}
			else if (!thisValue.Equals(thatValue))
			{
				return false;
			}
			return true;
		}

		// Token: 0x060009FD RID: 2557 RVA: 0x00020648 File Offset: 0x0001E848
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public override int GetHashCode()
		{
			Type type = base.GetType();
			FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			object obj = null;
			for (int i = 0; i < fields.Length; i++)
			{
				object obj2 = ((RtFieldInfo)fields[i]).UnsafeGetValue(this);
				if (obj2 != null && !obj2.GetType().IsArray)
				{
					obj = obj2;
				}
				if (obj != null)
				{
					break;
				}
			}
			if (obj != null)
			{
				return obj.GetHashCode();
			}
			return type.GetHashCode();
		}

		// Token: 0x17000148 RID: 328
		// (get) Token: 0x060009FE RID: 2558 RVA: 0x000206AD File Offset: 0x0001E8AD
		public virtual object TypeId
		{
			get
			{
				return base.GetType();
			}
		}

		// Token: 0x060009FF RID: 2559 RVA: 0x000206B5 File Offset: 0x0001E8B5
		public virtual bool Match(object obj)
		{
			return this.Equals(obj);
		}

		// Token: 0x06000A00 RID: 2560 RVA: 0x000206BE File Offset: 0x0001E8BE
		public virtual bool IsDefaultAttribute()
		{
			return false;
		}

		// Token: 0x06000A01 RID: 2561 RVA: 0x000206C1 File Offset: 0x0001E8C1
		void _Attribute.GetTypeInfoCount(out uint pcTInfo)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06000A02 RID: 2562 RVA: 0x000206C8 File Offset: 0x0001E8C8
		void _Attribute.GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06000A03 RID: 2563 RVA: 0x000206CF File Offset: 0x0001E8CF
		void _Attribute.GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06000A04 RID: 2564 RVA: 0x000206D6 File Offset: 0x0001E8D6
		void _Attribute.Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr)
		{
			throw new NotImplementedException();
		}
	}
}
