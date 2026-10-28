using System.Collections.Generic;
using System.IO;
using BUTR.DependencyInjection;
using MCM.Abstractions.Base.PerSave;
using MCM.Abstractions.GameFeatures;
using MCM.Implementation;
using MCM.Implementation.PerSave;
using TaleWorlds.CampaignSystem;

namespace MCM.Internal.GameFeatures;

internal sealed class PerSaveCampaignBehavior : CampaignBehaviorBase, IPerSaveSettingsProvider
{
	private Dictionary<string, string>? _settings = new Dictionary<string, string>();

	public Dictionary<string, string>? Settings => _settings;

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_settings", ref _settings);
		if (dataStore.IsLoading)
		{
			if (_settings == null)
			{
				_settings = new Dictionary<string, string>();
			}
			GenericServiceProvider.GetService<PerSaveSettingsContainer>()?.LoadSettings();
		}
	}

	public override void RegisterEvents()
	{
		CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreatedEvent);
	}

	private void OnNewGameCreatedEvent(CampaignGameStarter campaignGameStarter)
	{
		_settings = new Dictionary<string, string>();
		GenericServiceProvider.GetService<PerSaveSettingsContainer>()?.LoadSettings();
	}

	public bool SaveSettings(PerSaveSettings perSaveSettings)
	{
		JsonSettingsFormat service = GenericServiceProvider.GetService<JsonSettingsFormat>();
		if (_settings == null || service == null)
		{
			return false;
		}
		string key = Path.Combine(perSaveSettings.FolderName, perSaveSettings.SubFolder, perSaveSettings.Id) ?? "";
		_settings[key] = service.SaveJson(perSaveSettings);
		return true;
	}

	public void LoadSettings(PerSaveSettings perSaveSettings)
	{
		JsonSettingsFormat service = GenericServiceProvider.GetService<JsonSettingsFormat>();
		if (_settings != null && service != null)
		{
			string key = Path.Combine(perSaveSettings.FolderName, perSaveSettings.SubFolder, perSaveSettings.Id) ?? "";
			if (_settings.TryGetValue(key, out string value))
			{
				service.LoadFromJson(perSaveSettings, value);
			}
		}
	}
}
