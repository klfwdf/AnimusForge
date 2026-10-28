using System;
using System.Collections.Generic;
using System.Linq;
using BUTR.DependencyInjection;
using BUTR.DependencyInjection.Logger;
using HarmonyLib.BUTR.Extensions;
using MCM.Abstractions.Base;
using MCM.Abstractions.Wrapper;
using MCM.Common;

namespace MCM.Abstractions;

public class SettingsUtils
{
	public static void CheckIsValid(ISettingsPropertyDefinition prop, object? settings)
	{
		if (settings == null)
		{
			throw new Exception("Settings is null.");
		}
		if (prop.PropertyReference is PropertyRef propertyRef)
		{
			if (!propertyRef.PropertyInfo.CanRead)
			{
				throw new Exception("Property " + propertyRef.PropertyInfo.Name + " in " + settings.GetType().FullName + " must have a getter.");
			}
			if (prop.SettingType != SettingType.Dropdown && !propertyRef.PropertyInfo.CanWrite)
			{
				throw new Exception("Property " + propertyRef.PropertyInfo.Name + " in " + settings.GetType().FullName + " must have a setter.");
			}
			SettingType settingType = prop.SettingType;
			bool flag = (uint)(settingType - 1) <= 1u;
			if (flag && prop.MinValue == prop.MaxValue)
			{
				throw new Exception("Property " + propertyRef.PropertyInfo.Name + " in " + settings.GetType().FullName + " is a numeric type but the MinValue and MaxValue are the same.");
			}
			if (prop.SettingType != SettingType.Bool && prop.IsToggle)
			{
				throw new Exception("Property " + propertyRef.PropertyInfo.Name + " in " + settings.GetType().FullName + " is marked as the main toggle for the group but is a numeric type. The main toggle must be a boolean type.");
			}
		}
	}

	public static void ResetSettings(BaseSettings settings)
	{
		if (settings is IWrapper wrapper)
		{
			object obj = Activator.CreateInstance(wrapper.Object?.GetType());
			if (obj != null && Activator.CreateInstance(wrapper.GetType(), obj) is BaseSettings overrideSettings)
			{
				OverrideSettings(settings, overrideSettings);
				return;
			}
		}
		if (Activator.CreateInstance(settings.GetType()) is BaseSettings overrideSettings2)
		{
			OverrideSettings(settings, overrideSettings2);
		}
	}

	public static void OverrideSettings(BaseSettings settings, BaseSettings overrideSettings)
	{
		OverrideValues(settings, overrideSettings);
	}

	public static bool Equals(BaseSettings settings1, BaseSettings settings2)
	{
		Dictionary<(string, string), ISettingsPropertyDefinition> dictionary = settings1.GetAllSettingPropertyDefinitions().ToDictionary((ISettingsPropertyDefinition x) => (DisplayName: x.DisplayName, GroupName: x.GroupName), (ISettingsPropertyDefinition x) => x);
		Dictionary<(string, string), ISettingsPropertyDefinition> dictionary2 = settings2.GetAllSettingPropertyDefinitions().ToDictionary((ISettingsPropertyDefinition x) => (DisplayName: x.DisplayName, GroupName: x.GroupName), (ISettingsPropertyDefinition x) => x);
		if (dictionary.Count != dictionary2.Count)
		{
			return false;
		}
		foreach (KeyValuePair<(string, string), ISettingsPropertyDefinition> item in dictionary)
		{
			if (!dictionary2.TryGetValue(item.Key, out var value) || !Equals(item.Value, value))
			{
				return false;
			}
		}
		return true;
	}

	public static bool Equals(ISettingsPropertyDefinition? currentDefinition, ISettingsPropertyDefinition? newDefinition)
	{
		if (currentDefinition == null || newDefinition == null)
		{
			return false;
		}
		switch (currentDefinition.SettingType)
		{
		case SettingType.Bool:
		case SettingType.Int:
		case SettingType.Float:
		case SettingType.String:
		case SettingType.Button:
		{
			if (currentDefinition.PropertyReference.Value == null || newDefinition.PropertyReference.Value == null)
			{
				return false;
			}
			object value = currentDefinition.PropertyReference.Value;
			object value2 = newDefinition.PropertyReference.Value;
			return value.Equals(value2);
		}
		case SettingType.Dropdown:
		{
			if (currentDefinition.PropertyReference.Value == null || newDefinition.PropertyReference.Value == null)
			{
				return false;
			}
			SelectedIndexWrapper selectedIndexWrapper = new SelectedIndexWrapper(currentDefinition.PropertyReference.Value);
			SelectedIndexWrapper selectedIndexWrapper2 = new SelectedIndexWrapper(newDefinition.PropertyReference.Value);
			return selectedIndexWrapper.SelectedIndex.Equals(selectedIndexWrapper2.SelectedIndex);
		}
		default:
			return false;
		}
	}

	public static void OverrideValues(BaseSettings current, BaseSettings @new)
	{
		Dictionary<string, SettingsPropertyGroupDefinition> dictionary = current.GetUnsortedSettingPropertyGroups().ToDictionary((SettingsPropertyGroupDefinition x) => x.GroupNameRaw, (SettingsPropertyGroupDefinition x) => x);
		foreach (SettingsPropertyGroupDefinition unsortedSettingPropertyGroup in @new.GetUnsortedSettingPropertyGroups())
		{
			if (dictionary.TryGetValue(unsortedSettingPropertyGroup.GroupNameRaw, out var value))
			{
				OverrideValues(value, unsortedSettingPropertyGroup);
				continue;
			}
			GenericServiceProvider.GetService<IBUTRLogger<SettingsUtils>>()?.LogWarning(@new.Id + "::" + unsortedSettingPropertyGroup.GroupNameRaw + " was not found on, " + current.Id);
		}
	}

	public static void OverrideValues(SettingsPropertyGroupDefinition current, SettingsPropertyGroupDefinition @new)
	{
		Dictionary<string, SettingsPropertyGroupDefinition> dictionary = current.SubGroups.ToDictionary((SettingsPropertyGroupDefinition x) => x.GroupNameRaw, (SettingsPropertyGroupDefinition x) => x);
		Dictionary<string, ISettingsPropertyDefinition> dictionary2 = current.SettingProperties.ToDictionary((ISettingsPropertyDefinition x) => x.DisplayName, (ISettingsPropertyDefinition x) => x);
		foreach (SettingsPropertyGroupDefinition subGroup in @new.SubGroups)
		{
			if (dictionary.TryGetValue(subGroup.GroupNameRaw, out var value))
			{
				OverrideValues(value, subGroup);
				continue;
			}
			GenericServiceProvider.GetService<IBUTRLogger<SettingsUtils>>()?.LogWarning(@new.GroupName + "::" + subGroup.GroupNameRaw + " was not found on, " + current.GroupNameRaw);
		}
		foreach (ISettingsPropertyDefinition settingProperty in @new.SettingProperties)
		{
			if (dictionary2.TryGetValue(settingProperty.DisplayName, out var value2))
			{
				OverrideValues(value2, settingProperty);
				continue;
			}
			GenericServiceProvider.GetService<IBUTRLogger<SettingsUtils>>()?.LogWarning(@new.GroupNameRaw + "::" + settingProperty.DisplayName + " was not found on, " + current.GroupNameRaw);
		}
	}

	public static void OverrideValues(ISettingsPropertyDefinition current, ISettingsPropertyDefinition @new)
	{
		if (!Equals(current, @new))
		{
			current.PropertyReference.Value = @new.PropertyReference.Value;
		}
	}

	public static bool IsForGenericDropdown(Type type)
	{
		bool flag = type.GetInterfaces().Any((Type x) => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IList<>));
		bool flag2 = (object)AccessTools2.Property(type, "SelectedIndex", logErrorInTrace: false) != null;
		return flag & flag2;
	}

	public static bool IsForTextDropdown(Type type)
	{
		return IsForGenericDropdown(type) && type.IsGenericType && (object)AccessTools2.Property(type.GenericTypeArguments[0], "IsSelected", logErrorInTrace: false) == null;
	}

	public static bool IsForCheckboxDropdown(Type type)
	{
		return IsForGenericDropdown(type) && type.IsGenericType && (object)AccessTools2.Property(type.GenericTypeArguments[0], "IsSelected", logErrorInTrace: false) != null;
	}

	public static bool IsForTextDropdown(object? obj)
	{
		return obj != null && IsForTextDropdown(obj.GetType());
	}

	public static bool IsForCheckboxDropdown(object? obj)
	{
		return obj != null && IsForCheckboxDropdown(obj.GetType());
	}

	public static IEnumerable<ISettingsPropertyDefinition> GetAllSettingPropertyDefinitions(SettingsPropertyGroupDefinition settingPropertyGroup1)
	{
		foreach (ISettingsPropertyDefinition settingProperty in settingPropertyGroup1.SettingProperties)
		{
			yield return settingProperty;
		}
		foreach (SettingsPropertyGroupDefinition settingPropertyGroup2 in settingPropertyGroup1.SubGroups)
		{
			foreach (ISettingsPropertyDefinition allSettingPropertyDefinition in GetAllSettingPropertyDefinitions(settingPropertyGroup2))
			{
				yield return allSettingPropertyDefinition;
			}
		}
	}

	public static IEnumerable<SettingsPropertyGroupDefinition> GetAllSettingPropertyGroupDefinitions(SettingsPropertyGroupDefinition settingPropertyGroup)
	{
		yield return settingPropertyGroup;
		foreach (SettingsPropertyGroupDefinition subGroup in settingPropertyGroup.SubGroups)
		{
			yield return subGroup;
		}
	}

	public static List<SettingsPropertyGroupDefinition> GetSettingsPropertyGroups(char subGroupDelimiter, IEnumerable<ISettingsPropertyDefinition> settingsPropertyDefinitions)
	{
		List<SettingsPropertyGroupDefinition> list = new List<SettingsPropertyGroupDefinition>();
		foreach (ISettingsPropertyDefinition settingsPropertyDefinition in settingsPropertyDefinitions)
		{
			SettingsPropertyGroupDefinition groupFor = GetGroupFor(subGroupDelimiter, settingsPropertyDefinition, list);
			groupFor.Add(settingsPropertyDefinition);
		}
		return list;
	}

	public static SettingsPropertyGroupDefinition GetGroupFor(char subGroupDelimiter, ISettingsPropertyDefinition sp, ICollection<SettingsPropertyGroupDefinition> rootCollection)
	{
		string groupName = sp.GroupName;
		SettingsPropertyGroupDefinition settingsPropertyGroupDefinition2;
		if (Enumerable.Contains(groupName, subGroupDelimiter))
		{
			string topGroupName = GetTopGroupName(subGroupDelimiter, groupName, out string truncatedGroupName);
			SettingsPropertyGroupDefinition settingsPropertyGroupDefinition = rootCollection.GetGroupFromName(topGroupName);
			if (settingsPropertyGroupDefinition == null)
			{
				settingsPropertyGroupDefinition = new SettingsPropertyGroupDefinition(topGroupName).SetSubGroupDelimiter(subGroupDelimiter);
				rootCollection.Add(settingsPropertyGroupDefinition);
			}
			settingsPropertyGroupDefinition2 = GetGroupForRecursive(subGroupDelimiter, truncatedGroupName, settingsPropertyGroupDefinition, sp);
		}
		else
		{
			settingsPropertyGroupDefinition2 = rootCollection.GetGroupFromName(groupName);
			if (settingsPropertyGroupDefinition2 == null)
			{
				settingsPropertyGroupDefinition2 = new SettingsPropertyGroupDefinition(groupName, sp.GroupOrder).SetSubGroupDelimiter(subGroupDelimiter);
				rootCollection.Add(settingsPropertyGroupDefinition2);
			}
		}
		return settingsPropertyGroupDefinition2;
	}

	public static SettingsPropertyGroupDefinition GetGroupForRecursive(char subGroupDelimiter, string groupName, SettingsPropertyGroupDefinition sgp, ISettingsPropertyDefinition sp)
	{
		while (Enumerable.Contains(groupName, subGroupDelimiter))
		{
			string topGroupName = GetTopGroupName(subGroupDelimiter, groupName, out string truncatedGroupName);
			SettingsPropertyGroupDefinition settingsPropertyGroupDefinition = sgp.GetGroup(topGroupName);
			if (settingsPropertyGroupDefinition == null)
			{
				settingsPropertyGroupDefinition = new SettingsPropertyGroupDefinition(topGroupName).SetSubGroupDelimiter(subGroupDelimiter);
				sgp.Add(settingsPropertyGroupDefinition);
			}
			groupName = truncatedGroupName;
			sgp = settingsPropertyGroupDefinition;
		}
		SettingsPropertyGroupDefinition settingsPropertyGroupDefinition2 = sgp.GetGroup(groupName);
		if (settingsPropertyGroupDefinition2 == null)
		{
			settingsPropertyGroupDefinition2 = new SettingsPropertyGroupDefinition(groupName, sp.GroupOrder).SetSubGroupDelimiter(subGroupDelimiter);
			sgp.Add(settingsPropertyGroupDefinition2);
		}
		return settingsPropertyGroupDefinition2;
	}

	public static string GetTopGroupName(char subGroupDelimiter, string groupName, out string truncatedGroupName)
	{
		int num = groupName.IndexOf(subGroupDelimiter);
		string result = groupName.Substring(0, num);
		truncatedGroupName = groupName.Remove(0, num + 1);
		return result;
	}

	public static IEnumerable<IPropertyDefinitionBase> GetPropertyDefinitionWrappers(object property)
	{
		return GetPropertyDefinitionWrappers(new object[1] { property });
	}

	public static IEnumerable<IPropertyDefinitionBase> GetPropertyDefinitionWrappers(IReadOnlyCollection<object> properties)
	{
		object propAttr = properties.SingleOrDefault((object a) => a is IPropertyDefinitionBool);
		if (propAttr != null)
		{
			yield return new PropertyDefinitionBoolWrapper(propAttr);
		}
		propAttr = properties.SingleOrDefault((object a) => a is IPropertyDefinitionDropdown);
		if (propAttr != null)
		{
			yield return new PropertyDefinitionDropdownWrapper(propAttr);
		}
		propAttr = properties.SingleOrDefault((object a) => a is IPropertyDefinitionGroupToggle);
		if (propAttr != null)
		{
			yield return new PropertyDefinitionGroupToggleWrapper(propAttr);
		}
		propAttr = properties.SingleOrDefault((object a) => a is IPropertyDefinitionText);
		if (propAttr != null)
		{
			yield return new PropertyDefinitionTextWrapper(propAttr);
		}
		propAttr = properties.SingleOrDefault((object a) => a is IPropertyDefinitionWithActionFormat);
		if (propAttr != null)
		{
			yield return new PropertyDefinitionWithActionFormatWrapper(propAttr);
		}
		propAttr = properties.SingleOrDefault((object a) => a is IPropertyDefinitionWithCustomFormatter);
		if (propAttr != null)
		{
			yield return new PropertyDefinitionWithCustomFormatterWrapper(propAttr);
		}
		propAttr = properties.SingleOrDefault((object a) => a is IPropertyDefinitionWithEditableMinMax);
		if (propAttr != null)
		{
			yield return new PropertyDefinitionWithEditableMinMaxWrapper(propAttr);
		}
		propAttr = properties.SingleOrDefault((object a) => a is IPropertyDefinitionWithFormat);
		if (propAttr != null)
		{
			yield return new PropertyDefinitionWithFormatWrapper(propAttr);
		}
		propAttr = properties.SingleOrDefault((object a) => a is IPropertyDefinitionWithId);
		if (propAttr != null)
		{
			yield return new PropertyDefinitionWithIdWrapper(propAttr);
		}
		propAttr = properties.SingleOrDefault((object a) => a is IPropertyDefinitionWithMinMax);
		if (propAttr != null)
		{
			yield return new PropertyDefinitionWithMinMaxWrapper(propAttr);
		}
		propAttr = properties.SingleOrDefault((object a) => a is IPropertyDefinitionButton);
		if (propAttr != null)
		{
			yield return new PropertyDefinitionButtonWrapper(propAttr);
		}
	}
}
