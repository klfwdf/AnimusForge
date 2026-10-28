using System;
using System.Collections.Generic;
using System.ComponentModel;
using MCM.Abstractions;
using MCM.Abstractions.Base.Global;
using MCM.Abstractions.Base.PerCampaign;
using MCM.Abstractions.Base.PerSave;
using MCM.Abstractions.FluentBuilder;
using MCM.Abstractions.Wrapper;

namespace MCM.Implementation.FluentBuilder;

internal sealed class DefaultSettingsBuilder : BaseSettingsBuilder
{
	private Dictionary<string, ISettingsPropertyGroupBuilder> PropertyGroups { get; } = new Dictionary<string, ISettingsPropertyGroupBuilder>();

	private Dictionary<string, ISettingsPresetBuilder> Presets { get; } = new Dictionary<string, ISettingsPresetBuilder>();

	private string Id { get; }

	private string DisplayName { get; }

	private string FolderName { get; set; } = string.Empty;

	private string SubFolder { get; set; } = string.Empty;

	private string Format { get; set; } = "memory";

	private int UIVersion { get; set; } = 1;

	private char SubGroupDelimiter { get; set; } = '/';

	private PropertyChangedEventHandler? OnPropertyChanged { get; set; }

	public DefaultSettingsBuilder(string id, string displayName)
	{
		Id = id;
		DisplayName = displayName;
		CreateGroup(SettingsPropertyGroupDefinition.DefaultGroupName, delegate
		{
		});
		CreatePreset("default", "{=BaseSettings_Default}Default", delegate
		{
		});
	}

	public override ISettingsBuilder SetFolderName(string value)
	{
		FolderName = value;
		return this;
	}

	public override ISettingsBuilder SetSubFolder(string value)
	{
		SubFolder = value;
		return this;
	}

	public override ISettingsBuilder SetFormat(string value)
	{
		Format = value;
		return this;
	}

	public override ISettingsBuilder SetUIVersion(int value)
	{
		UIVersion = value;
		return this;
	}

	public override ISettingsBuilder SetSubGroupDelimiter(char value)
	{
		SubGroupDelimiter = value;
		return this;
	}

	public override ISettingsBuilder SetOnPropertyChanged(PropertyChangedEventHandler value)
	{
		OnPropertyChanged = value;
		return this;
	}

	public override ISettingsBuilder CreateGroup(string name, Action<ISettingsPropertyGroupBuilder> builder)
	{
		if (!PropertyGroups.ContainsKey(name))
		{
			PropertyGroups[name] = new DefaultSettingsPropertyGroupBuilder(name);
		}
		builder(PropertyGroups[name]);
		return this;
	}

	public override ISettingsBuilder CreatePreset(string id, string name, Action<ISettingsPresetBuilder> builder)
	{
		if (!Presets.ContainsKey(id))
		{
			Presets[id] = new DefaultSettingsPresetBuilder(id, name);
		}
		builder(Presets[id]);
		return this;
	}

	public override ISettingsBuilder WithoutDefaultPreset()
	{
		Presets.Remove("default");
		return this;
	}

	public override FluentGlobalSettings BuildAsGlobal()
	{
		if (Presets.TryGetValue("default", out ISettingsPresetBuilder value))
		{
			foreach (SettingsPropertyDefinition settingProperty in GetSettingProperties())
			{
				value.SetPropertyValue(settingProperty.Id, settingProperty.PropertyReference.Value);
			}
		}
		return new FluentGlobalSettings(Id, DisplayName, FolderName, SubFolder, Format, UIVersion, SubGroupDelimiter, OnPropertyChanged, GetSettingPropertyGroups(), Presets.Values);
	}

	public override FluentPerSaveSettings BuildAsPerSave()
	{
		if (Presets.TryGetValue("default", out ISettingsPresetBuilder value))
		{
			foreach (SettingsPropertyDefinition settingProperty in GetSettingProperties())
			{
				value.SetPropertyValue(settingProperty.Id, settingProperty.PropertyReference.Value);
			}
		}
		return new FluentPerSaveSettings(Id, DisplayName, FolderName, SubFolder, UIVersion, SubGroupDelimiter, OnPropertyChanged, GetSettingPropertyGroups(), Presets.Values);
	}

	public override FluentPerCampaignSettings BuildAsPerCampaign()
	{
		if (Presets.TryGetValue("default", out ISettingsPresetBuilder value))
		{
			foreach (SettingsPropertyDefinition settingProperty in GetSettingProperties())
			{
				value.SetPropertyValue(settingProperty.Id, settingProperty.PropertyReference.Value);
			}
		}
		return new FluentPerCampaignSettings(Id, DisplayName, FolderName, SubFolder, UIVersion, SubGroupDelimiter, OnPropertyChanged, GetSettingPropertyGroups(), Presets.Values);
	}

	private IEnumerable<SettingsPropertyGroupDefinition> GetSettingPropertyGroups()
	{
		return SettingsUtils.GetSettingsPropertyGroups(SubGroupDelimiter, GetSettingProperties());
	}

	private IEnumerable<SettingsPropertyDefinition> GetSettingProperties()
	{
		foreach (ISettingsPropertyGroupBuilder settingsPropertyGroup in PropertyGroups.Values)
		{
			foreach (ISettingsPropertyBuilder settingsProperty in settingsPropertyGroup.Properties.Values)
			{
				yield return new SettingsPropertyDefinition(settingsProperty.GetDefinitions(), new PropertyGroupDefinitionWrapper(settingsPropertyGroup.GetPropertyGroupDefinition()), settingsProperty.PropertyReference, SubGroupDelimiter);
			}
		}
	}
}
