using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BUTR.DependencyInjection.Logger;
using HarmonyLib.BUTR.Extensions;
using MCM.Abstractions;
using MCM.Abstractions.Base;

namespace MCM.UI.Adapter.MCMv5.Providers;

public sealed class MCMv5ExternalSettingsProvider : IExternalSettingsProvider, IExternalSettingsProviderHasInitialize
{
	private readonly IBUTRLogger _logger;

	private readonly List<MCMv5SettingsProviderWrapper> _settingsProviderWrappers = new List<MCMv5SettingsProviderWrapper>();

	public IEnumerable<SettingsDefinition> SettingsDefinitions => from x in _settingsProviderWrappers.SelectMany((MCMv5SettingsProviderWrapper x) => x.SettingsDefinitions)
		where x.SettingsId != "MCM_v5"
		select x;

	public MCMv5ExternalSettingsProvider(IBUTRLogger<MCMv5ExternalSettingsProvider> logger)
	{
		_logger = logger;
	}

	public void Initialize()
	{
		IEnumerable<object> source = (from x in HarmonyLib.BUTR.Extensions.AccessTools2.AllTypes()
			where x.Assembly != typeof(BaseSettings).Assembly
			where !x.IsSubclassOf(typeof(BaseSettingsProvider)) && ReflectionUtils.ImplementsOrImplementsEquivalent(x, "MCM.Abstractions.BaseSettingsProvider")
			select x).Select(delegate(Type x)
		{
			PropertyInfo propertyInfo = HarmonyLib.BUTR.Extensions.AccessTools2.DeclaredProperty(x, "Instance");
			object result;
			if ((object)propertyInfo != null)
			{
				MethodInfo getMethod = propertyInfo.GetMethod;
				if ((object)getMethod != null && getMethod.IsStatic)
				{
					result = propertyInfo.GetValue(null);
					goto IL_002c;
				}
			}
			result = null;
			goto IL_002c;
			IL_002c:
			return result;
		}).OfType<object>();
		_settingsProviderWrappers.AddRange(source.Select((object x) => new MCMv5SettingsProviderWrapper(x)));
	}

	public BaseSettings? GetSettings(string id)
	{
		foreach (MCMv5SettingsProviderWrapper settingsProviderWrapper in _settingsProviderWrappers)
		{
			BaseSettings settings = settingsProviderWrapper.GetSettings(id);
			if (settings != null)
			{
				return settings;
			}
		}
		_logger.LogWarning("GetSettings " + id + " returned null");
		return null;
	}

	public void SaveSettings(BaseSettings settings)
	{
		foreach (MCMv5SettingsProviderWrapper settingsProviderWrapper in _settingsProviderWrappers)
		{
			settingsProviderWrapper.SaveSettings(settings);
		}
	}

	public void ResetSettings(BaseSettings settings)
	{
		foreach (MCMv5SettingsProviderWrapper settingsProviderWrapper in _settingsProviderWrappers)
		{
			settingsProviderWrapper.ResetSettings(settings);
		}
	}

	public void OverrideSettings(BaseSettings settings)
	{
		foreach (MCMv5SettingsProviderWrapper settingsProviderWrapper in _settingsProviderWrappers)
		{
			settingsProviderWrapper.OverrideSettings(settings);
		}
	}

	public IEnumerable<ISettingsPreset> GetPresets(string id)
	{
		foreach (MCMv5SettingsProviderWrapper settingsProvider in _settingsProviderWrappers)
		{
			foreach (ISettingsPreset preset in settingsProvider.GetPresets(id))
			{
				yield return preset;
			}
		}
	}
}
