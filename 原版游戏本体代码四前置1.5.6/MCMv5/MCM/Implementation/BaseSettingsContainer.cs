using System;
using System.Collections.Generic;
using System.Linq;
using BUTR.DependencyInjection;
using MCM.Abstractions;
using MCM.Abstractions.Base;
using MCM.Abstractions.GameFeatures;

namespace MCM.Implementation;

public abstract class BaseSettingsContainer<TSettings> : ISettingsContainer, ISettingsContainerHasSettingsDefinitions, ISettingsContainerCanOverride, ISettingsContainerCanReset, ISettingsContainerPresets, ISettingsContainerHasSettingsPack where TSettings : BaseSettings
{
	protected virtual GameDirectory RootFolder { get; } = GenericServiceProvider.GetService<IFileSystemProvider>()?.GetModSettingsDirectory() ?? new GameDirectory(PlatformDirectoryType.User, "Configs\\ModSettings\\");

	protected virtual Dictionary<string, TSettings> LoadedSettings { get; } = new Dictionary<string, TSettings>();

	public virtual IEnumerable<SettingsDefinition> SettingsDefinitions => LoadedSettings.Values.Select((TSettings s) => new SettingsDefinition(s.Id, s.DisplayName, s.GetSettingPropertyGroups()));

	protected virtual void RegisterSettings(TSettings? settings)
	{
		if (settings == null || LoadedSettings.ContainsKey(settings.Id))
		{
			return;
		}
		IFileSystemProvider service = GenericServiceProvider.GetService<IFileSystemProvider>();
		if (service == null)
		{
			return;
		}
		LoadedSettings.Add(settings.Id, settings);
		GameDirectory orCreateDirectory = service.GetOrCreateDirectory(RootFolder, settings.FolderName);
		GameDirectory directory = (string.IsNullOrEmpty(settings.SubFolder) ? orCreateDirectory : service.GetOrCreateDirectory(orCreateDirectory, settings.SubFolder));
		IEnumerable<ISettingsFormat> source = GenericServiceProvider.GetService<IEnumerable<ISettingsFormat>>() ?? Array.Empty<ISettingsFormat>();
		source.FirstOrDefault((ISettingsFormat x) => x.FormatTypes.Any((string y) => y == settings.FormatType))?.Load(settings, directory, settings.Id);
		settings.OnPropertyChanged("LOADING_COMPLETE");
	}

	public virtual BaseSettings? GetSettings(string id)
	{
		TSettings value;
		return LoadedSettings.TryGetValue(id, out value) ? value : null;
	}

	public virtual bool SaveSettings(BaseSettings settings)
	{
		if (!(settings is TSettings) || !LoadedSettings.ContainsKey(settings.Id))
		{
			return false;
		}
		IFileSystemProvider service = GenericServiceProvider.GetService<IFileSystemProvider>();
		if (service == null)
		{
			return false;
		}
		GameDirectory orCreateDirectory = service.GetOrCreateDirectory(RootFolder, settings.FolderName);
		GameDirectory directory = (string.IsNullOrEmpty(settings.SubFolder) ? orCreateDirectory : service.GetOrCreateDirectory(orCreateDirectory, settings.SubFolder));
		IEnumerable<ISettingsFormat> source = GenericServiceProvider.GetService<IEnumerable<ISettingsFormat>>() ?? Array.Empty<ISettingsFormat>();
		source.FirstOrDefault((ISettingsFormat x) => x.FormatTypes.Any((string y) => y == settings.FormatType))?.Save(settings, directory, settings.Id);
		settings.OnPropertyChanged("SAVE_TRIGGERED");
		return true;
	}

	public virtual bool OverrideSettings(BaseSettings settings)
	{
		if (!(settings is TSettings) || !LoadedSettings.ContainsKey(settings.Id))
		{
			return false;
		}
		SettingsUtils.OverrideSettings(LoadedSettings[settings.Id], settings);
		return true;
	}

	public virtual bool ResetSettings(BaseSettings settings)
	{
		if (!(settings is TSettings) || !LoadedSettings.ContainsKey(settings.Id))
		{
			return false;
		}
		SettingsUtils.ResetSettings(LoadedSettings[settings.Id]);
		return true;
	}

	public IEnumerable<ISettingsPreset> GetPresets(string settingsId)
	{
		if (!LoadedSettings.TryGetValue(settingsId, out var settings))
		{
			yield break;
		}
		IFileSystemProvider fileSystemProvider = GenericServiceProvider.GetService<IFileSystemProvider>();
		if (fileSystemProvider == null)
		{
			yield break;
		}
		GameDirectory presetsDirectory = fileSystemProvider.GetOrCreateDirectory(fileSystemProvider.GetModSettingsDirectory(), "Presets");
		GameDirectory settingsDirectory = fileSystemProvider.GetOrCreateDirectory(presetsDirectory, settingsId);
		GameFile[] files = fileSystemProvider.GetFiles(settingsDirectory, "*.json");
		for (int i = 0; i < files.Length; i++)
		{
			JsonSettingsPreset preset = JsonSettingsPreset.FromFile(file: files[i], settings: settings);
			if (preset != null)
			{
				yield return preset;
			}
		}
	}

	public bool SavePresets(ISettingsPreset preset)
	{
		if (!LoadedSettings.TryGetValue(preset.SettingsId, out var value))
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
		foreach (TSettings setting in LoadedSettings.Values)
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
		foreach (TSettings setting in LoadedSettings.Values)
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
