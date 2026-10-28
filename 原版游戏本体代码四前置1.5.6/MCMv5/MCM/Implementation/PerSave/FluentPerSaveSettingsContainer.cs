using System;
using System.Collections.Generic;
using BUTR.DependencyInjection;
using MCM.Abstractions;
using MCM.Abstractions.Base;
using MCM.Abstractions.Base.PerSave;
using MCM.Abstractions.GameFeatures;
using MCM.Abstractions.PerSave;

namespace MCM.Implementation.PerSave;

internal sealed class FluentPerSaveSettingsContainer : BaseSettingsContainer<FluentPerSaveSettings>, IFluentPerSaveSettingsContainer, IPerSaveSettingsContainer, ISettingsContainer, ISettingsContainerHasSettingsDefinitions, ISettingsContainerCanOverride, ISettingsContainerCanReset, ISettingsContainerPresets, ISettingsContainerHasUnavailable, ISettingsContainerHasSettingsPack, ISettingsContainerCanInvalidateCache
{
	private readonly IGameEventListener _gameEventListener;

	public event Action? InstanceCacheInvalidated;

	public FluentPerSaveSettingsContainer(IGameEventListener gameEventListener)
	{
		_gameEventListener = gameEventListener;
		_gameEventListener.GameStarted += GameStarted;
		_gameEventListener.GameEnded += GameEnded;
	}

	public void Register(FluentPerSaveSettings settings)
	{
		RegisterSettings(settings);
	}

	public void Unregister(FluentPerSaveSettings settings)
	{
		if (LoadedSettings.ContainsKey(settings.Id))
		{
			LoadedSettings.Remove(settings.Id);
		}
	}

	protected override void RegisterSettings(FluentPerSaveSettings? perSaveSettings)
	{
		if (GenericServiceProvider.GameScopeServiceProvider != null)
		{
			IPerSaveSettingsProvider service = GenericServiceProvider.GetService<IPerSaveSettingsProvider>();
			if (service != null && perSaveSettings != null)
			{
				LoadedSettings.Add(perSaveSettings.Id, perSaveSettings);
				service.LoadSettings(perSaveSettings);
				perSaveSettings.OnPropertyChanged("LOADING_COMPLETE");
			}
		}
	}

	public override bool SaveSettings(BaseSettings settings)
	{
		if (GenericServiceProvider.GameScopeServiceProvider == null)
		{
			return false;
		}
		IPerSaveSettingsProvider service = GenericServiceProvider.GetService<IPerSaveSettingsProvider>();
		if (service == null)
		{
			return false;
		}
		if (!(settings is PerSaveSettings perSaveSettings) || !LoadedSettings.ContainsKey(perSaveSettings.Id))
		{
			return false;
		}
		service.SaveSettings(perSaveSettings);
		settings.OnPropertyChanged("SAVE_TRIGGERED");
		return true;
	}

	public void LoadSettings()
	{
	}

	public void GameStarted()
	{
		LoadedSettings.Clear();
	}

	public void GameEnded()
	{
		LoadedSettings.Clear();
	}

	public IEnumerable<UnavailableSetting> GetUnavailableSettings()
	{
		return Array.Empty<UnavailableSetting>();
	}
}
