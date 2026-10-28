using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib.BUTR.Extensions;
using MCM.Abstractions;
using MCM.Abstractions.Base;

namespace MCM.UI.Utils;

internal static class SettingPropertyDefinitionCache
{
	private delegate void ClearDelegate(ConditionalWeakTable<BaseSettings, List<SettingsPropertyGroupDefinition>> instance);

	private static readonly ClearDelegate? ClearMethod = HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegate<ClearDelegate>(typeof(ConditionalWeakTable<BaseSettings, List<SettingsPropertyGroupDefinition>>), "Clear");

	private static readonly ConditionalWeakTable<BaseSettings, List<SettingsPropertyGroupDefinition>> _cache = new ConditionalWeakTable<BaseSettings, List<SettingsPropertyGroupDefinition>>();

	public static void Clear()
	{
		ClearMethod?.Invoke(_cache);
	}

	public static IEnumerable<SettingsPropertyGroupDefinition> GetSettingPropertyGroups(BaseSettings settings)
	{
		if (!_cache.TryGetValue(settings, out List<SettingsPropertyGroupDefinition> value))
		{
			value = settings.GetSettingPropertyGroups();
			_cache.Add(settings, value);
		}
		return value;
	}

	public static IEnumerable<ISettingsPropertyDefinition> GetAllSettingPropertyDefinitions(BaseSettings settings)
	{
		return GetSettingPropertyGroups(settings).SelectMany(SettingsUtils.GetAllSettingPropertyDefinitions);
	}

	public static IEnumerable<SettingsPropertyGroupDefinition> GetAllSettingPropertyGroupDefinitions(BaseSettings settings)
	{
		return GetSettingPropertyGroups(settings).SelectMany(SettingsUtils.GetAllSettingPropertyGroupDefinitions);
	}

	public static bool Equals(BaseSettings settings1, BaseSettings settings2)
	{
		Dictionary<(string, string), ISettingsPropertyDefinition> dictionary = GetAllSettingPropertyDefinitions(settings1).ToDictionary((ISettingsPropertyDefinition x) => (DisplayName: x.DisplayName, GroupName: x.GroupName), (ISettingsPropertyDefinition x) => x);
		Dictionary<(string, string), ISettingsPropertyDefinition> dictionary2 = GetAllSettingPropertyDefinitions(settings2).ToDictionary((ISettingsPropertyDefinition x) => (DisplayName: x.DisplayName, GroupName: x.GroupName), (ISettingsPropertyDefinition x) => x);
		if (dictionary.Count != dictionary2.Count)
		{
			return false;
		}
		foreach (KeyValuePair<(string, string), ISettingsPropertyDefinition> item in dictionary)
		{
			if (!dictionary2.TryGetValue(item.Key, out var value) || !SettingsUtils.Equals(item.Value, value))
			{
				return false;
			}
		}
		return true;
	}
}
