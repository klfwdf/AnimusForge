using System;
using System.Collections.Generic;
using MCM.Common;

namespace MCM.Abstractions;

public class SettingsPropertyGroupDefinition
{
	private class DefaultPropertyGroupDefinition : IPropertyGroupDefinition
	{
		public string GroupName => DefaultGroupName;

		public int GroupOrder => 0;
	}

	public static readonly string DefaultGroupName = "{=SettingsPropertyGroupDefinition_Misc}Misc";

	public static readonly IPropertyGroupDefinition DefaultGroup = new DefaultPropertyGroupDefinition();

	protected readonly string _groupNameRaw;

	protected readonly string _groupNameOverrideRaw = string.Empty;

	protected readonly List<SettingsPropertyGroupDefinition> subGroups = new List<SettingsPropertyGroupDefinition>();

	protected readonly List<ISettingsPropertyDefinition> settingProperties = new List<ISettingsPropertyDefinition>();

	protected char SubGroupDelimiter { get; set; }

	public SettingsPropertyGroupDefinition? Parent { get; set; }

	public string GroupNameRaw => _groupNameRaw;

	public string GroupName => DisplayGroupNameRaw;

	public string DisplayGroupNameRaw
	{
		get
		{
			if (Parent == null)
			{
				return LocalizationUtils.Localize(_groupNameRaw);
			}
			string oldValue = LocalizationUtils.Localize(Parent._groupNameRaw);
			string text = LocalizationUtils.Localize(_groupNameRaw);
			return text.Replace(oldValue, string.Empty).TrimStart(new char[1] { SubGroupDelimiter });
		}
	}

	public int Order { get; }

	public IEnumerable<SettingsPropertyGroupDefinition> SubGroups => subGroups.SortDefault();

	public IEnumerable<ISettingsPropertyDefinition> SettingProperties => settingProperties.SortDefault();

	public SettingsPropertyGroupDefinition(string groupName, int order = -1)
	{
		_groupNameRaw = groupName;
		Order = order;
	}

	[Obsolete("Override not needed", true)]
	public SettingsPropertyGroupDefinition(string groupName, string? _, int order = -1)
	{
		_groupNameRaw = groupName;
		Order = order;
	}

	public SettingsPropertyGroupDefinition SetParent(SettingsPropertyGroupDefinition parent)
	{
		Parent = parent;
		return this;
	}

	public SettingsPropertyGroupDefinition SetSubGroupDelimiter(char subGroupDelimiter)
	{
		SubGroupDelimiter = subGroupDelimiter;
		return this;
	}

	public void Add(ISettingsPropertyDefinition settingProp)
	{
		settingProperties.Add(settingProp);
	}

	public void Add(SettingsPropertyGroupDefinition settingProp)
	{
		subGroups.Add(settingProp.SetParent(this));
	}

	public SettingsPropertyGroupDefinition? GetGroup(string groupName)
	{
		return subGroups.GetGroupFromName(groupName);
	}

	[Obsolete("Use GetGroup", true)]
	public SettingsPropertyGroupDefinition? GetGroupFor(string groupName)
	{
		return subGroups.GetGroupFromName(groupName);
	}

	public override string ToString()
	{
		return GroupName;
	}

	public SettingsPropertyGroupDefinition Clone(bool keepRefs = true)
	{
		SettingsPropertyGroupDefinition settingsPropertyGroupDefinition = new SettingsPropertyGroupDefinition(GroupName, Order).SetSubGroupDelimiter(SubGroupDelimiter);
		foreach (ISettingsPropertyDefinition settingProperty in SettingProperties)
		{
			settingsPropertyGroupDefinition.Add(settingProperty.Clone(keepRefs));
		}
		foreach (SettingsPropertyGroupDefinition subGroup in SubGroups)
		{
			settingsPropertyGroupDefinition.Add(subGroup.Clone(keepRefs));
		}
		return settingsPropertyGroupDefinition;
	}
}
