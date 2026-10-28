using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BUTR.DependencyInjection;
using MCM.Abstractions.FluentBuilder;
using MCM.Abstractions.PerCampaign;

namespace MCM.Abstractions.Base.PerCampaign;

public class FluentPerCampaignSettings : PerCampaignSettings, IFluentSettings
{
	public sealed override string Id { get; }

	public sealed override string DisplayName { get; }

	public sealed override string FolderName { get; }

	public sealed override string SubFolder { get; }

	public override string DiscoveryType => "fluent";

	public sealed override int UIVersion { get; }

	public sealed override char SubGroupDelimiter { get; }

	public List<SettingsPropertyGroupDefinition> SettingPropertyGroups { get; }

	private List<ISettingsPresetBuilder> Presets { get; }

	public sealed override event PropertyChangedEventHandler? PropertyChanged
	{
		add
		{
			base.PropertyChanged += value;
		}
		remove
		{
			base.PropertyChanged -= value;
		}
	}

	public FluentPerCampaignSettings(string id, string displayName, string folderName, string subFolder, int uiVersion, char subGroupDelimiter, PropertyChangedEventHandler? onPropertyChanged, IEnumerable<SettingsPropertyGroupDefinition> settingPropertyGroups, IEnumerable<ISettingsPresetBuilder> presets)
	{
		Id = id;
		DisplayName = displayName;
		FolderName = folderName;
		SubFolder = subFolder;
		UIVersion = uiVersion;
		SubGroupDelimiter = subGroupDelimiter;
		SettingPropertyGroups = settingPropertyGroups.ToList();
		Presets = presets.ToList();
		if (onPropertyChanged != null)
		{
			PropertyChanged += onPropertyChanged;
		}
	}

	public void Register()
	{
		IEnumerable<IFluentPerCampaignSettingsContainer> enumerable = GenericServiceProvider.GetService<IEnumerable<IFluentPerCampaignSettingsContainer>>() ?? Array.Empty<IFluentPerCampaignSettingsContainer>();
		foreach (IFluentPerCampaignSettingsContainer item in enumerable)
		{
			item?.Register(this);
		}
	}

	public void Unregister()
	{
		IEnumerable<IFluentPerCampaignSettingsContainer> enumerable = GenericServiceProvider.GetService<IEnumerable<IFluentPerCampaignSettingsContainer>>() ?? Array.Empty<IFluentPerCampaignSettingsContainer>();
		foreach (IFluentPerCampaignSettingsContainer item in enumerable)
		{
			item?.Unregister(this);
		}
	}

	public override BaseSettings CreateNew()
	{
		return new FluentPerCampaignSettings(Id, DisplayName, FolderName, SubFolder, UIVersion, SubGroupDelimiter, null, SettingPropertyGroups.Select((SettingsPropertyGroupDefinition g) => g.Clone(keepRefs: false)), Presets);
	}

	public sealed override IEnumerable<ISettingsPreset> GetBuiltInPresets()
	{
		return Presets.Select((ISettingsPresetBuilder presetBuilder) => presetBuilder.Build(CreateNew()));
	}
}
