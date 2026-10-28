using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BUTR.DependencyInjection;
using MCM.Abstractions.FluentBuilder;
using MCM.Abstractions.Global;

namespace MCM.Abstractions.Base.Global;

public class FluentGlobalSettings : GlobalSettings, IFluentSettings
{
	public sealed override string Id { get; }

	public sealed override string DisplayName { get; }

	public sealed override string FolderName { get; }

	public sealed override string SubFolder { get; }

	public sealed override string FormatType { get; }

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

	public FluentGlobalSettings(string id, string displayName, string folderName, string subFolder, string format, int uiVersion, char subGroupDelimiter, PropertyChangedEventHandler? onPropertyChanged, IEnumerable<SettingsPropertyGroupDefinition> settingPropertyGroups, IEnumerable<ISettingsPresetBuilder> presets)
	{
		Id = id;
		DisplayName = displayName;
		FolderName = folderName;
		SubFolder = subFolder;
		FormatType = format;
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
		IEnumerable<IFluentGlobalSettingsContainer> enumerable = GenericServiceProvider.GetService<IEnumerable<IFluentGlobalSettingsContainer>>() ?? Array.Empty<IFluentGlobalSettingsContainer>();
		foreach (IFluentGlobalSettingsContainer item in enumerable)
		{
			item?.Register(this);
		}
	}

	public void Unregister()
	{
		IEnumerable<IFluentGlobalSettingsContainer> enumerable = GenericServiceProvider.GetService<IEnumerable<IFluentGlobalSettingsContainer>>() ?? Array.Empty<IFluentGlobalSettingsContainer>();
		foreach (IFluentGlobalSettingsContainer item in enumerable)
		{
			item?.Unregister(this);
		}
	}

	public sealed override BaseSettings CreateNew()
	{
		return new FluentGlobalSettings(Id, DisplayName, FolderName, SubFolder, FormatType, UIVersion, SubGroupDelimiter, null, SettingPropertyGroups.Select((SettingsPropertyGroupDefinition g) => g.Clone(keepRefs: false)), Presets);
	}

	public sealed override IEnumerable<ISettingsPreset> GetBuiltInPresets()
	{
		return Presets.Select((ISettingsPresetBuilder presetBuilder) => presetBuilder.Build(CreateNew()));
	}
}
