using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BUTR.DependencyInjection;
using BUTR.DependencyInjection.Logger;
using HarmonyLib.BUTR.Extensions;
using MCM.Abstractions;
using MCM.Abstractions.Base;
using MCM.Abstractions.Base.PerCampaign;
using MCM.Abstractions.GameFeatures;
using MCM.Abstractions.PerCampaign;
using MCM.Common;

namespace MCM.Implementation.PerCampaign;

internal sealed class PerCampaignSettingsContainer : BaseSettingsContainer<PerCampaignSettings>, IPerCampaignSettingsContainer, ISettingsContainer, ISettingsContainerHasSettingsDefinitions, ISettingsContainerCanOverride, ISettingsContainerCanReset, ISettingsContainerPresets, ISettingsContainerHasUnavailable, ISettingsContainerHasSettingsPack, ISettingsContainerCanInvalidateCache
{
	private readonly IBUTRLogger _logger;

	private readonly IGameEventListener _gameEventListener;

	private bool _hasGameStarted;

	protected override GameDirectory RootFolder { get; }

	public event Action? InstanceCacheInvalidated;

	public PerCampaignSettingsContainer(IBUTRLogger<PerCampaignSettingsContainer> logger, IGameEventListener gameEventListener)
	{
		_logger = logger;
		_gameEventListener = gameEventListener;
		_gameEventListener.GameStarted += GameStarted;
		_gameEventListener.GameLoaded += GameLoaded;
		_gameEventListener.GameEnded += GameEnded;
		IFileSystemProvider service = GenericServiceProvider.GetService<IFileSystemProvider>();
		RootFolder = service.GetDirectory(base.RootFolder, "PerCampaign");
	}

	protected override void RegisterSettings(PerCampaignSettings? settings)
	{
		if (settings == null || GenericServiceProvider.GameScopeServiceProvider == null)
		{
			return;
		}
		ICampaignIdProvider service = GenericServiceProvider.GetService<ICampaignIdProvider>();
		if (service == null)
		{
			return;
		}
		string currentCampaignId = service.GetCurrentCampaignId();
		if (currentCampaignId == null)
		{
			return;
		}
		IFileSystemProvider service2 = GenericServiceProvider.GetService<IFileSystemProvider>();
		if (service2 == null)
		{
			return;
		}
		LoadedSettings.Add(settings.Id, settings);
		GameDirectory orCreateDirectory = service2.GetOrCreateDirectory(RootFolder, currentCampaignId);
		GameDirectory orCreateDirectory2 = service2.GetOrCreateDirectory(orCreateDirectory, settings.FolderName);
		GameDirectory directory = (string.IsNullOrEmpty(settings.SubFolder) ? orCreateDirectory2 : service2.GetOrCreateDirectory(orCreateDirectory2, settings.SubFolder));
		IEnumerable<ISettingsFormat> source = GenericServiceProvider.GetService<IEnumerable<ISettingsFormat>>() ?? Array.Empty<ISettingsFormat>();
		source.FirstOrDefault((ISettingsFormat x) => x.FormatTypes.Any((string y) => y == settings.FormatType))?.Load(settings, directory, settings.Id);
		settings.OnPropertyChanged("LOADING_COMPLETE");
	}

	public override bool SaveSettings(BaseSettings settings)
	{
		PerCampaignSettings perCampaignSettings = settings as PerCampaignSettings;
		if (perCampaignSettings == null)
		{
			return false;
		}
		if (GenericServiceProvider.GameScopeServiceProvider == null)
		{
			return false;
		}
		ICampaignIdProvider service = GenericServiceProvider.GetService<ICampaignIdProvider>();
		if (service != null)
		{
			string currentCampaignId = service.GetCurrentCampaignId();
			if (currentCampaignId != null)
			{
				IFileSystemProvider service2 = GenericServiceProvider.GetService<IFileSystemProvider>();
				if (service2 == null)
				{
					return false;
				}
				GameDirectory orCreateDirectory = service2.GetOrCreateDirectory(RootFolder, currentCampaignId);
				GameDirectory orCreateDirectory2 = service2.GetOrCreateDirectory(orCreateDirectory, settings.FolderName);
				GameDirectory directory = (string.IsNullOrEmpty(settings.SubFolder) ? orCreateDirectory2 : service2.GetOrCreateDirectory(orCreateDirectory2, settings.SubFolder));
				IEnumerable<ISettingsFormat> source = GenericServiceProvider.GetService<IEnumerable<ISettingsFormat>>() ?? Array.Empty<ISettingsFormat>();
				source.FirstOrDefault((ISettingsFormat x) => x.FormatTypes.Any((string y) => y == perCampaignSettings.FormatType))?.Save(perCampaignSettings, directory, perCampaignSettings.Id);
				settings.OnPropertyChanged("SAVE_TRIGGERED");
				return true;
			}
		}
		return false;
	}

	private void GameStarted()
	{
		_hasGameStarted = true;
		InstanceCacheInvalidated?.Invoke();
		LoadedSettings.Clear();
	}

	private void GameLoaded()
	{
		LoadSettings();
	}

	private IEnumerable<PerCampaignSettings> GetPerCampaignSettings()
	{
		foreach (Assembly assembly in from a in AccessTools2.AllAssemblies()
			where !a.IsDynamic
			select a)
		{
			IEnumerable<PerCampaignSettings> settings;
			try
			{
				settings = (from t in AccessTools2.GetTypesFromAssemblyIfValid(assembly)
					where t.IsClass && !t.IsAbstract
					where (object)t.GetConstructor(Type.EmptyTypes) != null
					where typeof(PerCampaignSettings).IsAssignableFrom(t)
					where !typeof(EmptyPerCampaignSettings).IsAssignableFrom(t)
					where !typeof(IWrapper).IsAssignableFrom(t)
					select t).Select(delegate(Type t)
				{
					try
					{
						return Activator.CreateInstance(t) as PerCampaignSettings;
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
						return (PerCampaignSettings)null;
					}
				}).OfType<PerCampaignSettings>().ToList();
			}
			catch (TypeLoadException exception)
			{
				settings = Array.Empty<PerCampaignSettings>();
				_logger.LogError(exception, $"Error while handling assembly {assembly}!");
			}
			foreach (PerCampaignSettings item in settings)
			{
				yield return item;
			}
		}
	}

	private void LoadSettings()
	{
		foreach (PerCampaignSettings perCampaignSetting in GetPerCampaignSettings())
		{
			RegisterSettings(perCampaignSetting);
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
			result = from setting in GetPerCampaignSettings()
				select new UnavailableSetting(setting.Id, setting.DisplayName, UnavailableSettingType.PerCampaign);
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
