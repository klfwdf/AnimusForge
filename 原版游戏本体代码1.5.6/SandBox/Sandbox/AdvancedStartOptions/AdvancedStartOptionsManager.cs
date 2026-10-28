using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;

namespace SandBox.AdvancedStartOptions;

public static class AdvancedStartOptionsManager
{
	private delegate void StartOptionsProviderDelegate(AdvancedStartOptions options);

	private static readonly List<StartOptionsProviderDelegate> _providers = new List<StartOptionsProviderDelegate>();

	private static void Initialize()
	{
		_providers.Clear();
		MBList<Assembly> activeGameAssemblies = ModuleHelper.GetActiveGameAssemblies();
		for (int i = 0; i < activeGameAssemblies.Count; i++)
		{
			List<Type> typesSafe = activeGameAssemblies[i].GetTypesSafe();
			for (int j = 0; j < typesSafe.Count; j++)
			{
				Type type = typesSafe[j];
				if (type == null)
				{
					continue;
				}
				MethodInfo[] methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				foreach (MethodInfo methodInfo in methods)
				{
					object[] customAttributesSafe = methodInfo.GetCustomAttributesSafe(typeof(StartOptionsProviderAttribute), inherit: false);
					if (customAttributesSafe == null || customAttributesSafe.Length == 0)
					{
						continue;
					}
					try
					{
						if (Delegate.CreateDelegate(typeof(StartOptionsProviderDelegate), methodInfo) is StartOptionsProviderDelegate item)
						{
							_providers.Add(item);
							continue;
						}
						Debug.FailedAssert("Start options provider " + type.Name + "." + methodInfo.Name + " does not match the expected signature 'static void (CampaignStartOptions)' and will be ignored", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\AdvancedStartOptions\\AdvancedStartOptionsManager.cs", "Initialize", 50);
					}
					catch (Exception ex)
					{
						Debug.FailedAssert("Error when creating start options provider " + type.Name + "." + methodInfo.Name + ": " + ex.Message, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\AdvancedStartOptions\\AdvancedStartOptionsManager.cs", "Initialize", 55);
						Debug.Print("Error when creating start options provider: " + ex.Message);
					}
				}
			}
		}
	}

	public static AdvancedStartOptions CreateCampaignStartOptions()
	{
		Initialize();
		AdvancedStartOptions advancedStartOptions = new AdvancedStartOptions();
		for (int i = 0; i < _providers.Count; i++)
		{
			_providers[i](advancedStartOptions);
		}
		return advancedStartOptions;
	}
}
