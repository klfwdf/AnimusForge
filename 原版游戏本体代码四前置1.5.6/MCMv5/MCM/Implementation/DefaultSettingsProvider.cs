using System;
using System.Collections.Generic;
using System.Linq;
using BUTR.DependencyInjection;
using BUTR.DependencyInjection.Logger;
using MCM.Abstractions;
using MCM.Abstractions.Base;
using MCM.Abstractions.Base.Global;
using MCM.Abstractions.Base.PerCampaign;
using MCM.Abstractions.Base.PerSave;
using MCM.Abstractions.Global;
using MCM.Abstractions.PerCampaign;
using MCM.Abstractions.PerSave;

namespace MCM.Implementation;

internal sealed class DefaultSettingsProvider : BaseSettingsProvider
{
	private readonly IBUTRLogger _logger;

	private readonly List<ISettingsContainer> _settingsContainers;

	private readonly List<IExternalSettingsProvider> _externalSettingsProviders;

	public override IEnumerable<SettingsDefinition> SettingsDefinitions => _settingsContainers.OfType<ISettingsContainerHasSettingsDefinitions>().SelectMany((ISettingsContainerHasSettingsDefinitions sp) => sp.SettingsDefinitions).Concat(_externalSettingsProviders.SelectMany((IExternalSettingsProvider x) => x.SettingsDefinitions));

	public DefaultSettingsProvider(IBUTRLogger<DefaultSettingsProvider> logger)
	{
		_logger = logger;
		List<IGlobalSettingsContainer> list = (GenericServiceProvider.GetService<IEnumerable<IGlobalSettingsContainer>>() ?? Array.Empty<IGlobalSettingsContainer>()).ToList();
		List<IPerSaveSettingsContainer> list2 = (GenericServiceProvider.GetService<IEnumerable<IPerSaveSettingsContainer>>() ?? Array.Empty<IPerSaveSettingsContainer>()).ToList();
		List<IPerCampaignSettingsContainer> list3 = (GenericServiceProvider.GetService<IEnumerable<IPerCampaignSettingsContainer>>() ?? Array.Empty<IPerCampaignSettingsContainer>()).ToList();
		List<IExternalSettingsProvider> list4 = (GenericServiceProvider.GetService<IEnumerable<IExternalSettingsProvider>>() ?? Array.Empty<IExternalSettingsProvider>()).ToList();
		foreach (IGlobalSettingsContainer item in list)
		{
			if (item is ISettingsContainerCanInvalidateCache settingsContainerCanInvalidateCache)
			{
				settingsContainerCanInvalidateCache.InstanceCacheInvalidated += GlobalSettings.InvalidateCache;
			}
			logger.LogInformation($"Found Global container {item.GetType()} ({item.SettingsDefinitions.Count()})");
		}
		foreach (IPerSaveSettingsContainer item2 in list2)
		{
			ISettingsContainerCanInvalidateCache settingsContainerCanInvalidateCache2 = item2;
			if (settingsContainerCanInvalidateCache2 != null)
			{
				settingsContainerCanInvalidateCache2.InstanceCacheInvalidated += PerSaveSettings.InvalidateCache;
			}
			logger.LogInformation($"Found PerSave container {item2.GetType()} ({item2.SettingsDefinitions.Count()})");
		}
		foreach (IPerCampaignSettingsContainer item3 in list3)
		{
			ISettingsContainerCanInvalidateCache settingsContainerCanInvalidateCache3 = item3;
			if (settingsContainerCanInvalidateCache3 != null)
			{
				settingsContainerCanInvalidateCache3.InstanceCacheInvalidated += PerCampaignSettings.InvalidateCache;
			}
			logger.LogInformation($"Found Campaign container {item3.GetType()} ({item3.SettingsDefinitions.Count()})");
		}
		foreach (IExternalSettingsProvider item4 in list4)
		{
			if (item4 is IExternalSettingsProviderCanInvalidateCache externalSettingsProviderCanInvalidateCache)
			{
				externalSettingsProviderCanInvalidateCache.InstanceCacheInvalidated += delegate(ExternalSettingsProviderInvalidateCacheType cacheType)
				{
					switch (cacheType)
					{
					case ExternalSettingsProviderInvalidateCacheType.Global:
						GlobalSettings.InvalidateCache();
						break;
					case ExternalSettingsProviderInvalidateCacheType.PerSave:
						PerSaveSettings.InvalidateCache();
						break;
					case ExternalSettingsProviderInvalidateCacheType.PerCampaign:
						PerCampaignSettings.InvalidateCache();
						break;
					}
				};
			}
			logger.LogInformation($"Found external provider {item4.GetType()} ({item4.SettingsDefinitions.Count()})");
		}
		_settingsContainers = Enumerable.Empty<ISettingsContainer>().Concat(list).Concat(list2)
			.Concat(list3)
			.ToList();
		_externalSettingsProviders = list4.ToList();
	}

	public override BaseSettings? GetSettings(string id)
	{
		foreach (ISettingsContainer settingsContainer in _settingsContainers)
		{
			BaseSettings settings = settingsContainer.GetSettings(id);
			if (settings != null)
			{
				return settings;
			}
		}
		foreach (IExternalSettingsProvider externalSettingsProvider in _externalSettingsProviders)
		{
			BaseSettings settings2 = externalSettingsProvider.GetSettings(id);
			if (settings2 != null)
			{
				return settings2;
			}
		}
		_logger.LogWarning("GetSettings " + id + " returned null");
		return null;
	}

	public override void SaveSettings(BaseSettings settings)
	{
		foreach (ISettingsContainer settingsContainer in _settingsContainers)
		{
			settingsContainer.SaveSettings(settings);
		}
		foreach (IExternalSettingsProvider externalSettingsProvider in _externalSettingsProviders)
		{
			externalSettingsProvider.SaveSettings(settings);
		}
	}

	public override void ResetSettings(BaseSettings settings)
	{
		foreach (ISettingsContainerCanReset item in _settingsContainers.OfType<ISettingsContainerCanReset>())
		{
			item.ResetSettings(settings);
		}
		foreach (IExternalSettingsProvider externalSettingsProvider in _externalSettingsProviders)
		{
			externalSettingsProvider.ResetSettings(settings);
		}
	}

	public override void OverrideSettings(BaseSettings settings)
	{
		foreach (ISettingsContainerCanOverride item in _settingsContainers.OfType<ISettingsContainerCanOverride>())
		{
			item.OverrideSettings(settings);
		}
		foreach (IExternalSettingsProvider externalSettingsProvider in _externalSettingsProviders)
		{
			externalSettingsProvider.OverrideSettings(settings);
		}
	}

	public override IEnumerable<ISettingsPreset> GetPresets(string id)
	{
		foreach (ISettingsContainerPresets settingsContainer in _settingsContainers.OfType<ISettingsContainerPresets>())
		{
			foreach (ISettingsPreset preset in settingsContainer.GetPresets(id))
			{
				yield return preset;
			}
		}
		foreach (IExternalSettingsProvider settingsProvider in _externalSettingsProviders)
		{
			foreach (ISettingsPreset preset2 in settingsProvider.GetPresets(id))
			{
				yield return preset2;
			}
		}
	}

	public override IEnumerable<UnavailableSetting> GetUnavailableSettings()
	{
		foreach (ISettingsContainerHasUnavailable settingsContainer in _settingsContainers.OfType<ISettingsContainerHasUnavailable>())
		{
			foreach (UnavailableSetting unavailableSetting in settingsContainer.GetUnavailableSettings())
			{
				yield return unavailableSetting;
			}
		}
	}

	public override IEnumerable<SettingSnapshot> SaveAvailableSnapshots()
	{
		foreach (ISettingsContainerHasSettingsPack settingsContainer in _settingsContainers.OfType<ISettingsContainerHasSettingsPack>())
		{
			foreach (SettingSnapshot item in settingsContainer.SaveAvailableSnapshots())
			{
				yield return item;
			}
		}
	}

	public override IEnumerable<BaseSettings> LoadAvailableSnapshots(IEnumerable<SettingSnapshot> snapshots)
	{
		foreach (ISettingsContainerHasSettingsPack settingsContainer in _settingsContainers.OfType<ISettingsContainerHasSettingsPack>())
		{
			foreach (BaseSettings item in settingsContainer.LoadAvailableSnapshots(snapshots))
			{
				yield return item;
			}
		}
	}
}
