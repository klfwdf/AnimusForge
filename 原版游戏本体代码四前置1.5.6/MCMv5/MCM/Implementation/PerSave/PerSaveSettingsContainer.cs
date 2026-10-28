using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BUTR.DependencyInjection;
using BUTR.DependencyInjection.Logger;
using HarmonyLib.BUTR.Extensions;
using MCM.Abstractions;
using MCM.Abstractions.Base;
using MCM.Abstractions.Base.PerSave;
using MCM.Abstractions.GameFeatures;
using MCM.Abstractions.PerSave;
using MCM.Common;

namespace MCM.Implementation.PerSave;

internal sealed class PerSaveSettingsContainer : BaseSettingsContainer<PerSaveSettings>, IPerSaveSettingsContainer, ISettingsContainer, ISettingsContainerHasSettingsDefinitions, ISettingsContainerCanOverride, ISettingsContainerCanReset, ISettingsContainerPresets, ISettingsContainerHasUnavailable, ISettingsContainerHasSettingsPack, ISettingsContainerCanInvalidateCache
{
	private readonly IBUTRLogger _logger;

	private readonly IGameEventListener _gameEventListener;

	private bool _hasGameStarted;

	public event Action? InstanceCacheInvalidated;

	public PerSaveSettingsContainer(IBUTRLogger<PerSaveSettingsContainer> logger, IGameEventListener gameEventListener)
	{
		_logger = logger;
		_gameEventListener = gameEventListener;
		_gameEventListener.GameStarted += GameStarted;
		_gameEventListener.GameEnded += GameEnded;
	}

	protected override void RegisterSettings(PerSaveSettings? perSaveSettings)
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

	private void GameStarted()
	{
		_hasGameStarted = true;
		InstanceCacheInvalidated?.Invoke();
		LoadedSettings.Clear();
	}

	private IEnumerable<PerSaveSettings> GetPerSaveSettings()
	{
		foreach (Assembly assembly in from a in AccessTools2.AllAssemblies()
			where !a.IsDynamic
			select a)
		{
			IEnumerable<PerSaveSettings> settings;
			try
			{
				settings = (from t in AccessTools2.GetTypesFromAssemblyIfValid(assembly)
					where t.IsClass && !t.IsAbstract
					where (object)t.GetConstructor(Type.EmptyTypes) != null
					where typeof(PerSaveSettings).IsAssignableFrom(t)
					where !typeof(EmptyPerSaveSettings).IsAssignableFrom(t)
					where !typeof(IWrapper).IsAssignableFrom(t)
					select t).Select(delegate(Type t)
				{
					try
					{
						return Activator.CreateInstance(t) as PerSaveSettings;
					}
					catch (Exception exception2)
					{
						try
						{
							_logger.LogError(exception2, $"Failed to initialize type {t}");
						}
						catch (Exception)
						{
							_logger.LogError(exception2, "Failed to initialize and log type!");
						}
						return (PerSaveSettings)null;
					}
				}).OfType<PerSaveSettings>().ToList();
			}
			catch (TypeLoadException exception)
			{
				settings = Array.Empty<PerSaveSettings>();
				_logger.LogError(exception, $"Error while handling assembly {assembly}!");
			}
			foreach (PerSaveSettings item in settings)
			{
				yield return item;
			}
		}
	}

	public void LoadSettings()
	{
		foreach (PerSaveSettings perSaveSetting in GetPerSaveSettings())
		{
			RegisterSettings(perSaveSetting);
		}
	}

	public IEnumerable<UnavailableSetting> GetUnavailableSettings()
	{
		IEnumerable<UnavailableSetting> result;
		if (_hasGameStarted)
		{
			IEnumerable<UnavailableSetting> enumerable = Array.Empty<UnavailableSetting>();
			result = enumerable;
		}
		else
		{
			result = from setting in GetPerSaveSettings()
				select new UnavailableSetting(setting.Id, setting.DisplayName, UnavailableSettingType.PerSave);
		}
		return result;
	}

	private void GameEnded()
	{
		_hasGameStarted = false;
		InstanceCacheInvalidated?.Invoke();
		LoadedSettings.Clear();
	}
}
