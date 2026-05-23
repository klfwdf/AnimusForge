using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.GauntletUI;

public class WidgetInfo
{
	private static Dictionary<Type, WidgetInfo> _widgetInfos;

	public string Name { get; private set; }

	public Type Type { get; private set; }

	public bool GotCustomUpdate { get; private set; }

	public bool GotCustomLateUpdate { get; private set; }

	public bool GotCustomParallelUpdate { get; private set; }

	public bool GotUpdateBrushes { get; private set; }

	public WidgetInfo(Type type)
	{
		Name = type.Name;
		Type = type;
		GotCustomUpdate = IsMethodOverridden("OnUpdate");
		GotCustomLateUpdate = IsMethodOverridden("OnLateUpdate");
		GotCustomParallelUpdate = IsMethodOverridden("OnParallelUpdate");
		GotUpdateBrushes = IsMethodOverridden("UpdateBrushes");
	}

	public static void Refresh()
	{
		_widgetInfos = new Dictionary<Type, WidgetInfo>();
		CollectWidgetTypes();
		TextureProviderFactory.RefreshProviderTypes();
	}

	public static WidgetInfo GetWidgetInfo(Type type)
	{
		return _widgetInfos[type];
	}

	public static WidgetInfo[] GetWidgetInfos()
	{
		return _widgetInfos.Values.ToArray();
	}

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

	private static void CollectWidgetTypes()
	{
		new List<Type>();
		Assembly assembly = typeof(Widget).Assembly;
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		foreach (Assembly assembly2 in assemblies)
		{
			if (!CheckAssemblyReferencesThis(assembly2) && !(assembly2 == assembly))
			{
				continue;
			}
			foreach (Type item in assembly2.GetTypesSafe())
			{
				if (typeof(Widget).IsAssignableFrom(item))
				{
					_widgetInfos.Add(item, new WidgetInfo(item));
				}
			}
		}
	}

	private bool IsMethodOverridden(string methodName)
	{
		MethodInfo method = Type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		if (method == null)
		{
			return false;
		}
		Type type = Type;
		Type type2 = Type;
		while (type2 != null)
		{
			if (type2.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) != null)
			{
				type = type2;
			}
			type2 = type2.BaseType;
		}
		return method.DeclaringType != type;
	}
}
