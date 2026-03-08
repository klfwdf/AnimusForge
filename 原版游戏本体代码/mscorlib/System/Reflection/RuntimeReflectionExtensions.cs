using System;
using System.Collections.Generic;

namespace System.Reflection
{
	// Token: 0x020005ED RID: 1517
	[__DynamicallyInvokable]
	public static class RuntimeReflectionExtensions
	{
		// Token: 0x06004654 RID: 18004 RVA: 0x001025C7 File Offset: 0x001007C7
		private static void CheckAndThrow(Type t)
		{
			if (t == null)
			{
				throw new ArgumentNullException("type");
			}
			if (!(t is RuntimeType))
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeType"));
			}
		}

		// Token: 0x06004655 RID: 18005 RVA: 0x001025F5 File Offset: 0x001007F5
		private static void CheckAndThrow(MethodInfo m)
		{
			if (m == null)
			{
				throw new ArgumentNullException("method");
			}
			if (!(m is RuntimeMethodInfo))
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeMethodInfo"));
			}
		}

		// Token: 0x06004656 RID: 18006 RVA: 0x00102623 File Offset: 0x00100823
		[__DynamicallyInvokable]
		public static IEnumerable<PropertyInfo> GetRuntimeProperties(this Type type)
		{
			RuntimeReflectionExtensions.CheckAndThrow(type);
			return type.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		}

		// Token: 0x06004657 RID: 18007 RVA: 0x00102633 File Offset: 0x00100833
		[__DynamicallyInvokable]
		public static IEnumerable<EventInfo> GetRuntimeEvents(this Type type)
		{
			RuntimeReflectionExtensions.CheckAndThrow(type);
			return type.GetEvents(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		}

		// Token: 0x06004658 RID: 18008 RVA: 0x00102643 File Offset: 0x00100843
		[__DynamicallyInvokable]
		public static IEnumerable<MethodInfo> GetRuntimeMethods(this Type type)
		{
			RuntimeReflectionExtensions.CheckAndThrow(type);
			return type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		}

		// Token: 0x06004659 RID: 18009 RVA: 0x00102653 File Offset: 0x00100853
		[__DynamicallyInvokable]
		public static IEnumerable<FieldInfo> GetRuntimeFields(this Type type)
		{
			RuntimeReflectionExtensions.CheckAndThrow(type);
			return type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		}

		// Token: 0x0600465A RID: 18010 RVA: 0x00102663 File Offset: 0x00100863
		[__DynamicallyInvokable]
		public static PropertyInfo GetRuntimeProperty(this Type type, string name)
		{
			RuntimeReflectionExtensions.CheckAndThrow(type);
			return type.GetProperty(name);
		}

		// Token: 0x0600465B RID: 18011 RVA: 0x00102672 File Offset: 0x00100872
		[__DynamicallyInvokable]
		public static EventInfo GetRuntimeEvent(this Type type, string name)
		{
			RuntimeReflectionExtensions.CheckAndThrow(type);
			return type.GetEvent(name);
		}

		// Token: 0x0600465C RID: 18012 RVA: 0x00102681 File Offset: 0x00100881
		[__DynamicallyInvokable]
		public static MethodInfo GetRuntimeMethod(this Type type, string name, Type[] parameters)
		{
			RuntimeReflectionExtensions.CheckAndThrow(type);
			return type.GetMethod(name, parameters);
		}

		// Token: 0x0600465D RID: 18013 RVA: 0x00102691 File Offset: 0x00100891
		[__DynamicallyInvokable]
		public static FieldInfo GetRuntimeField(this Type type, string name)
		{
			RuntimeReflectionExtensions.CheckAndThrow(type);
			return type.GetField(name);
		}

		// Token: 0x0600465E RID: 18014 RVA: 0x001026A0 File Offset: 0x001008A0
		[__DynamicallyInvokable]
		public static MethodInfo GetRuntimeBaseDefinition(this MethodInfo method)
		{
			RuntimeReflectionExtensions.CheckAndThrow(method);
			return method.GetBaseDefinition();
		}

		// Token: 0x0600465F RID: 18015 RVA: 0x001026AE File Offset: 0x001008AE
		[__DynamicallyInvokable]
		public static InterfaceMapping GetRuntimeInterfaceMap(this TypeInfo typeInfo, Type interfaceType)
		{
			if (typeInfo == null)
			{
				throw new ArgumentNullException("typeInfo");
			}
			if (!(typeInfo is RuntimeType))
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeType"));
			}
			return typeInfo.GetInterfaceMap(interfaceType);
		}

		// Token: 0x06004660 RID: 18016 RVA: 0x001026E3 File Offset: 0x001008E3
		[__DynamicallyInvokable]
		public static MethodInfo GetMethodInfo(this Delegate del)
		{
			if (del == null)
			{
				throw new ArgumentNullException("del");
			}
			return del.Method;
		}

		// Token: 0x04001CCF RID: 7375
		private const BindingFlags everything = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
	}
}
