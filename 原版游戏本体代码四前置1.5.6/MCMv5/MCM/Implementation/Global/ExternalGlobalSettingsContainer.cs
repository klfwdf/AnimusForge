using System.Collections.Generic;
using System.Linq;
using BUTR.DependencyInjection;
using BUTR.DependencyInjection.Logger;
using MCM.Abstractions;
using MCM.Abstractions.Base;
using MCM.Abstractions.Base.Global;
using MCM.Abstractions.Global;

namespace MCM.Implementation.Global;

internal sealed class ExternalGlobalSettingsContainer : IFluentGlobalSettingsContainer, IGlobalSettingsContainer, ISettingsContainer, ISettingsContainerHasSettingsDefinitions, ISettingsContainerCanOverride, ISettingsContainerCanReset, ISettingsContainerPresets, ISettingsContainerHasSettingsPack
{
	private Dictionary<string, ExternalGlobalSettings> LoadedSettings { get; } = new Dictionary<string, ExternalGlobalSettings>();

	public IEnumerable<SettingsDefinition> SettingsDefinitions => LoadedSettings.Values.Select((ExternalGlobalSettings s) => new SettingsDefinition(s.Id, s.DisplayName, s.GetSettingPropertyGroups()));

	public ExternalGlobalSettingsContainer(IBUTRLogger<ExternalGlobalSettingsContainer> logger)
	{
	}

	public BaseSettings? GetSettings(string id)
	{
		ExternalGlobalSettings value;
		return LoadedSettings.TryGetValue(id, out value) ? value : null;
	}

	public bool SaveSettings(BaseSettings settings)
	{
		if (!(settings is ExternalGlobalSettings) || !LoadedSettings.ContainsKey(settings.Id))
		{
			return false;
		}
		settings.OnPropertyChanged("SAVE_TRIGGERED");
		return true;
	}

	public bool OverrideSettings(BaseSettings settings)
	{
		if (!(settings is ExternalGlobalSettings) || !LoadedSettings.ContainsKey(settings.Id))
		{
			return false;
		}
		SettingsUtils.OverrideSettings(LoadedSettings[settings.Id], settings);
		return true;
	}

	public bool ResetSettings(BaseSettings settings)
	{
		if (!(settings is ExternalGlobalSettings) || !LoadedSettings.ContainsKey(settings.Id))
		{
			return false;
		}
		SettingsUtils.ResetSettings(LoadedSettings[settings.Id]);
		return true;
	}

	public IEnumerable<ISettingsPreset> GetPresets(string settingsId)
	{
		yield break;
	}

	public void Register(FluentGlobalSettings settings)
	{
		if (settings is ExternalGlobalSettings externalGlobalSettings)
		{
			LoadedSettings.Add(externalGlobalSettings.Id, externalGlobalSettings);
			settings.OnPropertyChanged("LOADING_COMPLETE");
		}
	}

	public void Unregister(FluentGlobalSettings settings)
	{
		if (settings is ExternalGlobalSettings externalGlobalSettings && LoadedSettings.ContainsKey(externalGlobalSettings.Id))
		{
			LoadedSettings.Remove(externalGlobalSettings.Id);
		}
	}

	public bool SavePresets(ISettingsPreset preset)
	{
		if (!LoadedSettings.TryGetValue(preset.SettingsId, out ExternalGlobalSettings value))
		{
			return false;
		}
		return preset.SavePreset(value);
	}

	public IEnumerable<SettingSnapshot> SaveAvailableSnapshots()
	{
		JsonSettingsFormat jsonFormat = GenericServiceProvider.GetService<JsonSettingsFormat>();
		if (jsonFormat == null)
		{
			yield break;
		}
		foreach (ExternalGlobalSettings setting in LoadedSettings.Values)
		{
			string json = jsonFormat.SaveJson(setting);
			string folderName = ((!string.IsNullOrEmpty(setting.FolderName)) ? (setting.FolderName + "/") : string.Empty);
			string subFolderName = ((!string.IsNullOrEmpty(setting.SubFolder)) ? (setting.SubFolder + "/") : string.Empty);
			string path = folderName + subFolderName + setting.Id + ".json";
			yield return new SettingSnapshot(path, json);
		}
	}

	public IEnumerable<BaseSettings> LoadAvailableSnapshots(IEnumerable<SettingSnapshot> snapshots)
	{
		JsonSettingsFormat jsonFormat = GenericServiceProvider.GetService<JsonSettingsFormat>();
		if (jsonFormat == null)
		{
			yield break;
		}
		Dictionary<string, string> snapshotDict = snapshots.ToDictionary((SettingSnapshot x) => x.Path, (SettingSnapshot x) => x.JsonContent);
		foreach (ExternalGlobalSettings setting in LoadedSettings.Values)
		{
			string folderName = ((!string.IsNullOrEmpty(setting.FolderName)) ? (setting.FolderName + "/") : string.Empty);
			string subFolderName = ((!string.IsNullOrEmpty(setting.SubFolder)) ? (setting.SubFolder + "/") : string.Empty);
			string path = folderName + subFolderName + setting.Id + ".json";
			if (snapshotDict.TryGetValue(path, out var json))
			{
				yield return jsonFormat.LoadFromJson(setting.CreateNew(), json);
			}
			json = null;
		}
	}
}
