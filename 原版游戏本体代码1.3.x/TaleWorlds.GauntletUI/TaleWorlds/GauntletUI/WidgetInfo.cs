using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.GauntletUI
{
	// Token: 0x0200003C RID: 60
	public class WidgetInfo
	{
		// Token: 0x17000135 RID: 309
		// (get) Token: 0x060003F2 RID: 1010 RVA: 0x0000FC95 File Offset: 0x0000DE95
		// (set) Token: 0x060003F3 RID: 1011 RVA: 0x0000FC9D File Offset: 0x0000DE9D
		public string Name { get; private set; }

		// Token: 0x17000136 RID: 310
		// (get) Token: 0x060003F4 RID: 1012 RVA: 0x0000FCA6 File Offset: 0x0000DEA6
		// (set) Token: 0x060003F5 RID: 1013 RVA: 0x0000FCAE File Offset: 0x0000DEAE
		public Type Type { get; private set; }

		// Token: 0x17000137 RID: 311
		// (get) Token: 0x060003F6 RID: 1014 RVA: 0x0000FCB7 File Offset: 0x0000DEB7
		// (set) Token: 0x060003F7 RID: 1015 RVA: 0x0000FCBF File Offset: 0x0000DEBF
		public bool GotCustomUpdate { get; private set; }

		// Token: 0x17000138 RID: 312
		// (get) Token: 0x060003F8 RID: 1016 RVA: 0x0000FCC8 File Offset: 0x0000DEC8
		// (set) Token: 0x060003F9 RID: 1017 RVA: 0x0000FCD0 File Offset: 0x0000DED0
		public bool GotCustomLateUpdate { get; private set; }

		// Token: 0x17000139 RID: 313
		// (get) Token: 0x060003FA RID: 1018 RVA: 0x0000FCD9 File Offset: 0x0000DED9
		// (set) Token: 0x060003FB RID: 1019 RVA: 0x0000FCE1 File Offset: 0x0000DEE1
		public bool GotCustomParallelUpdate { get; private set; }

		// Token: 0x1700013A RID: 314
		// (get) Token: 0x060003FC RID: 1020 RVA: 0x0000FCEA File Offset: 0x0000DEEA
		// (set) Token: 0x060003FD RID: 1021 RVA: 0x0000FCF2 File Offset: 0x0000DEF2
		public bool GotUpdateBrushes { get; private set; }

		// Token: 0x060003FE RID: 1022 RVA: 0x0000FCFC File Offset: 0x0000DEFC
		public WidgetInfo(Type type)
		{
			this.Name = type.Name;
			this.Type = type;
			this.GotCustomUpdate = this.IsMethodOverridden("OnUpdate");
			this.GotCustomLateUpdate = this.IsMethodOverridden("OnLateUpdate");
			this.GotCustomParallelUpdate = this.IsMethodOverridden("OnParallelUpdate");
			this.GotUpdateBrushes = this.IsMethodOverridden("UpdateBrushes");
		}

		// Token: 0x060003FF RID: 1023 RVA: 0x0000FD66 File Offset: 0x0000DF66
		public static void Refresh()
		{
			WidgetInfo._widgetInfos = new Dictionary<Type, WidgetInfo>();
			WidgetInfo.CollectWidgetTypes();
			TextureProviderFactory.RefreshProviderTypes();
		}

		// Token: 0x06000400 RID: 1024 RVA: 0x0000FD7C File Offset: 0x0000DF7C
		public static WidgetInfo GetWidgetInfo(Type type)
		{
			return WidgetInfo._widgetInfos[type];
		}

		// Token: 0x06000401 RID: 1025 RVA: 0x0000FD89 File Offset: 0x0000DF89
		public static WidgetInfo[] GetWidgetInfos()
		{
			return WidgetInfo._widgetInfos.Values.ToArray<WidgetInfo>();
		}

		// Token: 0x06000402 RID: 1026 RVA: 0x0000FD9C File Offset: 0x0000DF9C
		private static bool CheckAssemblyReferencesThis(Assembly assembly)
		{
			Assembly assembly2 = typeof(Widget).Assembly;
			AssemblyName[] referencedAssemblies = assembly.GetReferencedAssemblies();
			for (int i = 0; i < referencedAssemblies.Length; i++)
			{
				if (referencedAssemblies[i].Name == assembly2.GetName().Name)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06000403 RID: 1027 RVA: 0x0000FDEC File Offset: 0x0000DFEC
		private static void CollectWidgetTypes()
		{
			new List<Type>();
			Assembly assembly = typeof(Widget).Assembly;
			foreach (Assembly assembly2 in AppDomain.CurrentDomain.GetAssemblies())
			{
				if (WidgetInfo.CheckAssemblyReferencesThis(assembly2) || assembly2 == assembly)
				{
					foreach (Type type in assembly2.GetTypesSafe(null))
					{
						if (typeof(Widget).IsAssignableFrom(type))
						{
							WidgetInfo._widgetInfos.Add(type, new WidgetInfo(type));
						}
					}
				}
			}
		}

		// Token: 0x06000404 RID: 1028 RVA: 0x0000FEA8 File Offset: 0x0000E0A8
		private bool IsMethodOverridden(string methodName)
		{
			MethodInfo method = this.Type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			bool result;
			if (method == null)
			{
				result = false;
			}
			else
			{
				Type right = this.Type;
				Type type = this.Type;
				while (type != null)
				{
					if (type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) != null)
					{
						right = type;
					}
					type = type.BaseType;
				}
				result = method.DeclaringType != right;
			}
			return result;
		}

		// Token: 0x040001EE RID: 494
		private static Dictionary<Type, WidgetInfo> _widgetInfos;
	}
}
