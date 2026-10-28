using System;
using System.Collections.Generic;
using System.Linq;
using BUTR.DependencyInjection;
using MCM.Abstractions;
using MCM.Abstractions.Base;
using MCM.Abstractions.Base.PerCampaign;
using MCM.Abstractions.GameFeatures;
using MCM.Abstractions.PerCampaign;

namespace MCM.Implementation.PerCampaign;

internal sealed class FluentPerCampaignSettingsContainer : BaseSettingsContainer<FluentPerCampaignSettings>, IFluentPerCampaignSettingsContainer, IPerCampaignSettingsContainer, ISettingsContainer, ISettingsContainerHasSettingsDefinitions, ISettingsContainerCanOverride, ISettingsContainerCanReset, ISettingsContainerPresets, ISettingsContainerHasUnavailable, ISettingsContainerHasSettingsPack, ISettingsContainerCanInvalidateCache
{
	private readonly IGameEventListener _gameEventListener;

	protected override GameDirectory RootFolder { get; }

	public event Action? InstanceCacheInvalidated;

	public FluentPerCampaignSettingsContainer(IGameEventListener gameEventListener)
	{
		_gameEventListener = gameEventListener;
		_gameEventListener.GameStarted += GameStarted;
		_gameEventListener.GameEnded += GameEnded;
		IFileSystemProvider service = GenericServiceProvider.GetService<IFileSystemProvider>();
		RootFolder = service.GetDirectory(base.RootFolder, "PerCampaign");
	}

	protected override void RegisterSettings(FluentPerCampaignSettings? settings)
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

	public void Register(FluentPerCampaignSettings settings)
	{
		RegisterSettings(settings);
	}

	public void Unregister(FluentPerCampaignSettings settings)
	{
		if (LoadedSettings.ContainsKey(settings.Id))
		{
			LoadedSettings.Remove(settings.Id);
		}
	}

	private void GameStarted()
	{
		LoadedSettings.Clear();
	}

	private void GameEnded()
	{
		LoadedSettings.Clear();
	}

	public IEnumerable<UnavailableSetting> GetUnavailableSettings()
	{
		return Array.Empty<UnavailableSetting>();
	}
}
