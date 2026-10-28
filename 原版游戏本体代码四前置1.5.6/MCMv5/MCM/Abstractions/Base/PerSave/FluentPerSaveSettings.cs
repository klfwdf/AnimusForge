using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BUTR.DependencyInjection;
using MCM.Abstractions.FluentBuilder;
using MCM.Abstractions.PerSave;

namespace MCM.Abstractions.Base.PerSave;

public class FluentPerSaveSettings : PerSaveSettings, IFluentSettings
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

	public FluentPerSaveSettings(string id, string displayName, string folderName, string subFolder, int uiVersion, char subGroupDelimiter, PropertyChangedEventHandler? onPropertyChanged, IEnumerable<SettingsPropertyGroupDefinition> settingPropertyGroups, IEnumerable<ISettingsPresetBuilder> presets)
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
		IEnumerable<IFluentPerSaveSettingsContainer> enumerable = GenericServiceProvider.GetService<IEnumerable<IFluentPerSaveSettingsContainer>>() ?? Array.Empty<IFluentPerSaveSettingsContainer>();
		foreach (IFluentPerSaveSettingsContainer item in enumerable)
		{
			item?.Register(this);
		}
	}

	public void Unregister()
	{
		IEnumerable<IFluentPerSaveSettingsContainer> enumerable = GenericServiceProvider.GetService<IEnumerable<IFluentPerSaveSettingsContainer>>() ?? Array.Empty<IFluentPerSaveSettingsContainer>();
		foreach (IFluentPerSaveSettingsContainer item in enumerable)
		{
			item?.Unregister(this);
		}
	}

	public override BaseSettings CreateNew()
	{
		return new FluentPerSaveSettings(Id, DisplayName, FolderName, SubFolder, UIVersion, SubGroupDelimiter, null, SettingPropertyGroups.Select((SettingsPropertyGroupDefinition g) => g.Clone(keepRefs: false)), Presets);
	}

	public sealed override IEnumerable<ISettingsPreset> GetBuiltInPresets()
	{
		return Presets.Select((ISettingsPresetBuilder presetBuilder) => presetBuilder.Build(CreateNew()));
	}
}
