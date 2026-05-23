using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.MountAndBlade.View;

public static class ViewCreatorManager
{
	private static Dictionary<string, MBList<MethodInfo>> _viewCreators;

	private static Dictionary<Type, MBList<Type>> _actualViewTypes;

	private static HashSet<Type> _defaultTypes;

	static ViewCreatorManager()
	{
		_viewCreators = new Dictionary<string, MBList<MethodInfo>>();
		_actualViewTypes = new Dictionary<Type, MBList<Type>>();
		_defaultTypes = new HashSet<Type>();
		CollectTypes();
	}

	internal static void CollectTypes()
	{
		_viewCreators.Clear();
		_actualViewTypes.Clear();
		_defaultTypes.Clear();
		Assembly[] referencingAssembliesSafe = typeof(ViewCreatorModule).Assembly.GetReferencingAssembliesSafe();
		Assembly assembly = typeof(ViewCreatorModule).Assembly;
		CheckAssemblyScreens(assembly);
		Assembly[] array = referencingAssembliesSafe;
		for (int i = 0; i < array.Length; i++)
		{
			CheckAssemblyScreens(array[i]);
		}
		CollectDefaults(assembly);
		array = referencingAssembliesSafe;
		for (int i = 0; i < array.Length; i++)
		{
			CollectDefaults(array[i]);
		}
		array = referencingAssembliesSafe;
		for (int i = 0; i < array.Length; i++)
		{
			CheckOverridenViews(array[i]);
		}
	}

	private static void CheckAssemblyScreens(Assembly assembly)
	{
		foreach (Type item in assembly.GetTypesSafe())
		{
			object[] customAttributesSafe = item.GetCustomAttributesSafe(typeof(ViewCreatorModule), inherit: false);
			if (customAttributesSafe == null || customAttributesSafe.Length != 1 || !(customAttributesSafe[0] is ViewCreatorModule))
			{
				continue;
			}
			MethodInfo[] methods = item.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (MethodInfo methodInfo in methods)
			{
				if (methodInfo.GetCustomAttributesSafe(typeof(ViewMethod), inherit: false)[0] is ViewMethod viewMethod)
				{
					if (_viewCreators.TryGetValue(viewMethod.Name, out var value))
					{
						value.Add(methodInfo);
						continue;
					}
					_viewCreators.Add(viewMethod.Name, new MBList<MethodInfo> { methodInfo });
				}
			}
		}
	}

	internal static IEnumerable<MissionBehavior> CreateDefaultMissionBehaviors(Mission mission)
	{
		List<MissionBehavior> list = new List<MissionBehavior>();
		foreach (Type defaultType in _defaultTypes)
		{
			Type type = null;
			if (_actualViewTypes.TryGetValue(defaultType, out var value))
			{
				MBList<Assembly> activeGameAssemblies = ModuleHelper.GetActiveGameAssemblies();
				for (int num = value.Count - 1; num >= 0; num--)
				{
					if (activeGameAssemblies.Contains(value[num].Assembly))
					{
						type = value[num];
						break;
					}
				}
			}
			if (type == null && !defaultType.IsAbstract)
			{
				type = defaultType;
			}
			if (type != null)
			{
				MissionBehavior item = Activator.CreateInstance(type) as MissionBehavior;
				list.Add(item);
			}
			else
			{
				Debug.FailedAssert($"Failed to initialize default mission view type: {defaultType}", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.View\\ViewCreatorManager.cs", "CreateDefaultMissionBehaviors", 129);
			}
		}
		return list;
	}

	internal static IEnumerable<MissionBehavior> CollectMissionBehaviors(string missionName, Mission mission, IEnumerable<MissionBehavior> behaviors)
	{
		List<MissionBehavior> list = new List<MissionBehavior>();
		if (_viewCreators.TryGetValue(missionName, out var value))
		{
			MethodInfo methodInfo = null;
			MBList<Assembly> activeGameAssemblies = ModuleHelper.GetActiveGameAssemblies();
			for (int num = value.Count - 1; num >= 0; num--)
			{
				if (activeGameAssemblies.Contains(value[num].DeclaringType.Assembly))
				{
					methodInfo = value[num];
					break;
				}
			}
			if (methodInfo != null)
			{
				MissionBehavior[] collection = methodInfo.Invoke(null, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new object[1] { mission }, null) as MissionBehavior[];
				list.AddRange(collection);
			}
			else
			{
				Debug.FailedAssert("Failed to invoke view creator method for: " + missionName, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.View\\ViewCreatorManager.cs", "CollectMissionBehaviors", 170);
			}
		}
		return behaviors.Concat(list);
	}

	public static ScreenBase CreateScreenView<T>() where T : ScreenBase, new()
	{
		if (_actualViewTypes.TryGetValue(typeof(T), out var value))
		{
			MBList<Assembly> activeGameAssemblies = ModuleHelper.GetActiveGameAssemblies();
			Type type = null;
			for (int num = value.Count - 1; num >= 0; num--)
			{
				if (activeGameAssemblies.Contains(value[num].Assembly))
				{
					type = value[num];
					break;
				}
			}
			if (type != null)
			{
				return Activator.CreateInstance(type) as ScreenBase;
			}
		}
		return new T();
	}

	public static ScreenBase CreateScreenView<T>(params object[] parameters) where T : ScreenBase
	{
		Type type = typeof(T);
		if (_actualViewTypes.TryGetValue(typeof(T), out var value))
		{
			MBList<Assembly> activeGameAssemblies = ModuleHelper.GetActiveGameAssemblies();
			for (int num = value.Count - 1; num >= 0; num--)
			{
				if (activeGameAssemblies.Contains(value[num].Assembly))
				{
					type = value[num];
					break;
				}
			}
		}
		return Activator.CreateInstance(type, parameters) as ScreenBase;
	}

	public static MissionView CreateMissionView<T>(bool isNetwork = false, Mission mission = null, params object[] parameters) where T : MissionView, new()
	{
		Type type = null;
		if (_actualViewTypes.TryGetValue(typeof(T), out var value))
		{
			MBList<Assembly> activeGameAssemblies = ModuleHelper.GetActiveGameAssemblies();
			for (int num = value.Count - 1; num >= 0; num--)
			{
				if (activeGameAssemblies.Contains(value[num].Assembly))
				{
					type = value[num];
					break;
				}
			}
			return Activator.CreateInstance(type, parameters) as MissionView;
		}
		return new T();
	}

	public static MissionView CreateMissionViewWithArgs<T>(params object[] parameters) where T : MissionView
	{
		Type type = typeof(T);
		if (_actualViewTypes.TryGetValue(typeof(T), out var value))
		{
			MBList<Assembly> activeGameAssemblies = ModuleHelper.GetActiveGameAssemblies();
			for (int num = value.Count - 1; num >= 0; num--)
			{
				if (activeGameAssemblies.Contains(value[num].Assembly))
				{
					type = value[num];
					break;
				}
			}
		}
		return Activator.CreateInstance(type, parameters) as MissionView;
	}

	private static void CheckOverridenViews(Assembly assembly)
	{
		foreach (Type item in assembly.GetTypesSafe())
		{
			if (!typeof(MissionView).IsAssignableFrom(item) && !typeof(ScreenBase).IsAssignableFrom(item))
			{
				continue;
			}
			object[] customAttributesSafe = item.GetCustomAttributesSafe(typeof(OverrideView), inherit: false);
			if (customAttributesSafe != null && customAttributesSafe.Length == 1 && customAttributesSafe[0] is OverrideView overrideView)
			{
				if (_actualViewTypes.TryGetValue(overrideView.BaseType, out var value))
				{
					value.Add(item);
					continue;
				}
				_actualViewTypes[overrideView.BaseType] = new MBList<Type> { item };
			}
		}
	}

	private static void CollectDefaults(Assembly assembly)
	{
		foreach (Type item in assembly.GetTypesSafe())
		{
			if (typeof(MissionBehavior).IsAssignableFrom(item) && item.GetCustomAttributesSafe(typeof(DefaultView), inherit: false).Length == 1)
			{
				_defaultTypes.Add(item);
			}
		}
	}
}
