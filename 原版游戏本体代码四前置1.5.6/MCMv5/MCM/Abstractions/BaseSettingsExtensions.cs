using System;
using System.Collections.Generic;
using System.Linq;
using BUTR.DependencyInjection;
using MCM.Abstractions.Base;
using MCM.Abstractions.Properties;

namespace MCM.Abstractions;

public static class BaseSettingsExtensions
{
	public static List<SettingsPropertyGroupDefinition> GetSettingPropertyGroups(this BaseSettings settings)
	{
		return settings.GetUnsortedSettingPropertyGroups().SortDefault().ToList();
	}

	public static IEnumerable<SettingsPropertyGroupDefinition> GetUnsortedSettingPropertyGroups(this BaseSettings settings)
	{
		IEnumerable<ISettingsPropertyDiscoverer> source = GenericServiceProvider.GetService<IEnumerable<ISettingsPropertyDiscoverer>>() ?? Array.Empty<ISettingsPropertyDiscoverer>();
		ISettingsPropertyDiscoverer settingsPropertyDiscoverer = source.FirstOrDefault((ISettingsPropertyDiscoverer x) => x.DiscoveryTypes.Any((string y) => y == settings.DiscoveryType));
		return SettingsUtils.GetSettingsPropertyGroups(settings.SubGroupDelimiter, settingsPropertyDiscoverer?.GetProperties(settings) ?? Array.Empty<ISettingsPropertyDefinition>());
	}

	public static IEnumerable<ISettingsPropertyDefinition> GetAllSettingPropertyDefinitions(this BaseSettings settings)
	{
		return settings.GetSettingPropertyGroups().SelectMany(SettingsUtils.GetAllSettingPropertyDefinitions);
	}

	public static IEnumerable<SettingsPropertyGroupDefinition> GetAllSettingPropertyGroupDefinitions(this BaseSettings settings)
	{
		return settings.GetSettingPropertyGroups().SelectMany(SettingsUtils.GetAllSettingPropertyGroupDefinitions);
	}

	public static IEnumerable<ISettingsPreset> GetExternalPresets(this BaseSettings settings)
	{
		return GenericServiceProvider.GetService<BaseSettingsProvider>()?.GetPresets(settings.Id) ?? Array.Empty<ISettingsPreset>();
	}
}
