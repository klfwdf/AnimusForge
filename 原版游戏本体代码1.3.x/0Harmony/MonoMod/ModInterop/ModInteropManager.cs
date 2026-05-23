using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using MonoMod.Utils;

namespace MonoMod.ModInterop
{
	// Token: 0x02000814 RID: 2068
	[NullableContext(1)]
	[Nullable(0)]
	internal static class ModInteropManager
	{
		// Token: 0x0600278C RID: 10124 RVA: 0x00087E60 File Offset: 0x00086060
		public static void ModInterop(this Type type)
		{
			Helpers.ThrowIfArgumentNull<Type>(type, "type");
			if (ModInteropManager.Registered.Contains(type))
			{
				return;
			}
			ModInteropManager.Registered.Add(type);
			string prefix = type.Assembly.GetName().Name;
			object[] customAttributes = type.GetCustomAttributes(typeof(ModExportNameAttribute), false);
			for (int i = 0; i < customAttributes.Length; i++)
			{
				prefix = ((ModExportNameAttribute)customAttributes[i]).Name;
			}
			foreach (FieldInfo field in type.GetFields(BindingFlags.Static | BindingFlags.Public))
			{
				if (typeof(Delegate).IsAssignableFrom(field.FieldType))
				{
					ModInteropManager.Fields.Add(field);
				}
			}
			foreach (MethodInfo method2 in type.GetMethods(BindingFlags.Static | BindingFlags.Public))
			{
				method2.RegisterModExport(null);
				method2.RegisterModExport(prefix);
			}
			foreach (FieldInfo field2 in ModInteropManager.Fields)
			{
				List<MethodInfo> methods;
				if (!ModInteropManager.Methods.TryGetValue(field2.GetModImportName(), out methods))
				{
					field2.SetValue(null, null);
				}
				else
				{
					bool matched = false;
					foreach (MethodInfo method in methods)
					{
						try
						{
							field2.SetValue(null, Delegate.CreateDelegate(field2.FieldType, null, method));
							matched = true;
							break;
						}
						catch
						{
						}
					}
					if (!matched)
					{
						field2.SetValue(null, null);
					}
				}
			}
		}

		// Token: 0x0600278D RID: 10125 RVA: 0x00088018 File Offset: 0x00086218
		public static void RegisterModExport(this MethodInfo method, [Nullable(2)] string prefix = null)
		{
			Helpers.ThrowIfArgumentNull<MethodInfo>(method, "method");
			if (!method.IsPublic || !method.IsStatic)
			{
				throw new MemberAccessException("Utility must be public static");
			}
			string name = method.Name;
			if (!string.IsNullOrEmpty(prefix))
			{
				name = prefix + "." + name;
			}
			List<MethodInfo> methods;
			if (!ModInteropManager.Methods.TryGetValue(name, out methods))
			{
				methods = (ModInteropManager.Methods[name] = new List<MethodInfo>());
			}
			if (!methods.Contains(method))
			{
				methods.Add(method);
			}
		}

		// Token: 0x0600278E RID: 10126 RVA: 0x00088098 File Offset: 0x00086298
		private static string GetModImportName(this FieldInfo field)
		{
			object[] customAttributes = field.GetCustomAttributes(typeof(ModImportNameAttribute), false);
			int num = 0;
			if (num >= customAttributes.Length)
			{
				if (field.DeclaringType != null)
				{
					customAttributes = field.DeclaringType.GetCustomAttributes(typeof(ModImportNameAttribute), false);
					num = 0;
					if (num < customAttributes.Length)
					{
						return ((ModImportNameAttribute)customAttributes[num]).Name + "." + field.Name;
					}
				}
				return field.Name;
			}
			return ((ModImportNameAttribute)customAttributes[num]).Name;
		}

		// Token: 0x040039E3 RID: 14819
		private static HashSet<Type> Registered = new HashSet<Type>();

		// Token: 0x040039E4 RID: 14820
		private static Dictionary<string, List<MethodInfo>> Methods = new Dictionary<string, List<MethodInfo>>();

		// Token: 0x040039E5 RID: 14821
		private static List<FieldInfo> Fields = new List<FieldInfo>();
	}
}
