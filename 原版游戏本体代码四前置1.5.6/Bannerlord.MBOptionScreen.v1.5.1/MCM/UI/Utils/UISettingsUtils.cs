using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bannerlord.ModuleManager;
using MCM.Abstractions;
using MCM.Abstractions.Base;
using MCM.Common;
using MCM.UI.Actions;
using MCM.UI.GUI.ViewModels;
using Microsoft.Extensions.Logging;
using TaleWorlds.Localization;

namespace MCM.UI.Utils;

internal static class UISettingsUtils
{
	private static readonly Bannerlord.ModuleManager.AlphanumComparatorFast AlphanumComparator = new Bannerlord.ModuleManager.AlphanumComparatorFast();

	public static readonly IComparer<SettingsPropertyVM> SettingsPropertyVMComparer = Comparer<SettingsPropertyVM>.Create(delegate(SettingsPropertyVM x, SettingsPropertyVM y)
	{
		ISettingsPropertyDefinition settingPropertyDefinition = x.SettingPropertyDefinition;
		ISettingsPropertyDefinition settingPropertyDefinition2 = y.SettingPropertyDefinition;
		int num = settingPropertyDefinition.Order.CompareTo(settingPropertyDefinition2.Order);
		if (num != 0)
		{
			return num;
		}
		int num2 = AlphanumComparator.Compare(ResolveName(settingPropertyDefinition.DisplayName), ResolveName(settingPropertyDefinition2.DisplayName));
		return (num2 != 0) ? num2 : string.CompareOrdinal(settingPropertyDefinition.Id, settingPropertyDefinition2.Id);
	});

	public static readonly IComparer<SettingsPropertyGroupVM> SettingsPropertyGroupVMComparer = Comparer<SettingsPropertyGroupVM>.Create(delegate(SettingsPropertyGroupVM x, SettingsPropertyGroupVM y)
	{
		SettingsPropertyGroupDefinition settingPropertyGroupDefinition = x.SettingPropertyGroupDefinition;
		SettingsPropertyGroupDefinition settingPropertyGroupDefinition2 = y.SettingPropertyGroupDefinition;
		int num = IsDefaultGroup(settingPropertyGroupDefinition2).CompareTo(IsDefaultGroup(settingPropertyGroupDefinition));
		if (num != 0)
		{
			return num;
		}
		int num2 = settingPropertyGroupDefinition.Order.CompareTo(settingPropertyGroupDefinition2.Order);
		if (num2 != 0)
		{
			return num2;
		}
		int num3 = AlphanumComparator.Compare(ResolveName(settingPropertyGroupDefinition.GroupName), ResolveName(settingPropertyGroupDefinition2.GroupName));
		return (num3 != 0) ? num3 : string.CompareOrdinal(settingPropertyGroupDefinition.GroupNameRaw, settingPropertyGroupDefinition2.GroupNameRaw);
	});

	private static bool IsDefaultGroup(SettingsPropertyGroupDefinition group)
	{
		return group.GroupNameRaw == SettingsPropertyGroupDefinition.DefaultGroupName;
	}

	private static string ResolveName(string name)
	{
		return new TextObject(name).ToString();
	}

	public static void OverrideValues(UndoRedoStack urs, BaseSettings current, BaseSettings @new)
	{
		Dictionary<string, SettingsPropertyGroupDefinition> dictionary = SettingPropertyDefinitionCache.GetSettingPropertyGroups(current).ToDictionary((SettingsPropertyGroupDefinition x) => x.GroupNameRaw, (SettingsPropertyGroupDefinition x) => x);
		foreach (SettingsPropertyGroupDefinition allSettingPropertyGroupDefinition in SettingPropertyDefinitionCache.GetAllSettingPropertyGroupDefinitions(@new))
		{
			if (dictionary.TryGetValue(allSettingPropertyGroupDefinition.GroupNameRaw, out var value))
			{
				OverrideValues(urs, value, allSettingPropertyGroupDefinition);
				continue;
			}
			MCMUISubModule.Logger.LogWarning("{NewId}::{GroupName} was not found on, {CurrentId}", @new.Id, allSettingPropertyGroupDefinition.GroupNameRaw, current.Id);
		}
	}

	public static void OverrideValues(UndoRedoStack urs, SettingsPropertyGroupDefinition current, SettingsPropertyGroupDefinition @new)
	{
		Dictionary<string, SettingsPropertyGroupDefinition> dictionary = current.SubGroups.ToDictionary((SettingsPropertyGroupDefinition x) => x.GroupNameRaw, (SettingsPropertyGroupDefinition x) => x);
		Dictionary<string, ISettingsPropertyDefinition> dictionary2 = current.SettingProperties.ToDictionary((ISettingsPropertyDefinition x) => x.DisplayName, (ISettingsPropertyDefinition x) => x);
		foreach (SettingsPropertyGroupDefinition subGroup in @new.SubGroups)
		{
			if (dictionary.TryGetValue(subGroup.GroupNameRaw, out var value))
			{
				OverrideValues(urs, value, subGroup);
				continue;
			}
			MCMUISubModule.Logger.LogWarning("{NewId}::{GroupName} was not found on, {CurrentId}", @new.GroupNameRaw, subGroup.GroupNameRaw, current.GroupNameRaw);
		}
		foreach (ISettingsPropertyDefinition settingProperty in @new.SettingProperties)
		{
			if (dictionary2.TryGetValue(settingProperty.DisplayName, out var value2))
			{
				OverrideValues(urs, value2, settingProperty);
				continue;
			}
			MCMUISubModule.Logger.LogWarning("{NewId}::{GroupName} was not found on, {CurrentId}", @new.GroupNameRaw, settingProperty.DisplayName, current.GroupNameRaw);
		}
	}

	public static void OverrideValues(UndoRedoStack urs, ISettingsPropertyDefinition current, ISettingsPropertyDefinition @new)
	{
		if (SettingsUtils.Equals(current, @new))
		{
			return;
		}
		switch (current.SettingType)
		{
		case SettingType.Bool:
			if (@new.PropertyReference.Value is bool value2)
			{
				urs.Do(new SetValueTypeAction<bool>(current.PropertyReference, value2));
			}
			break;
		case SettingType.Int:
			if (@new.PropertyReference.Value is int value4)
			{
				urs.Do(new SetValueTypeAction<int>(current.PropertyReference, value4));
			}
			break;
		case SettingType.Float:
			if (@new.PropertyReference.Value is float value5)
			{
				urs.Do(new SetValueTypeAction<float>(current.PropertyReference, value5));
			}
			break;
		case SettingType.String:
			if (@new.PropertyReference.Value is string value3)
			{
				urs.Do(new SetStringAction(current.PropertyReference, value3));
			}
			break;
		case SettingType.Dropdown:
		{
			object value = @new.PropertyReference.Value;
			if (value != null)
			{
				urs.Do(new SetSelectedIndexAction(current.PropertyReference, value));
			}
			break;
		}
		case SettingType.Button:
		{
			Action action = @new.PropertyReference.Value as Action;
			break;
		}
		}
	}

	public static IEnumerable<object> GetDropdownValues(IRef @ref)
	{
		object value = @ref.Value;
		if (!(value is IEnumerable<object> result))
		{
			if (value is IEnumerable source)
			{
				return source.Cast<object>();
			}
			return Array.Empty<object>();
		}
		return result;
	}
}
