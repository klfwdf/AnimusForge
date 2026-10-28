using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BUTR.DependencyInjection.Logger;
using HarmonyLib.BUTR.Extensions;
using MCM.Abstractions;
using MCM.Abstractions.Base.Global;
using MCM.Abstractions.Global;
using MCM.Common;

namespace MCM.Implementation.Global;

internal sealed class GlobalSettingsContainer : BaseGlobalSettingsContainer, IGlobalSettingsContainer, ISettingsContainer, ISettingsContainerHasSettingsDefinitions, ISettingsContainerCanOverride, ISettingsContainerCanReset, ISettingsContainerPresets, ISettingsContainerHasSettingsPack
{
	public GlobalSettingsContainer(IBUTRLogger<GlobalSettingsContainer> logger)
	{
		foreach (GlobalSettings globalSetting in GetGlobalSettings())
		{
			logger.LogTrace($"Registering settings {globalSetting.GetType()}.");
			RegisterSettings(globalSetting);
		}
		IEnumerable<GlobalSettings> GetGlobalSettings()
		{
			foreach (Assembly assembly in from a in AccessTools2.AllAssemblies()
				where !a.IsDynamic
				select a)
			{
				IEnumerable<GlobalSettings> settings;
				try
				{
					settings = (from t in AccessTools2.GetTypesFromAssemblyIfValid(assembly)
						where t.IsClass && !t.IsAbstract
						where (object)t.GetConstructor(Type.EmptyTypes) != null
						where typeof(GlobalSettings).IsAssignableFrom(t)
						where !typeof(EmptyGlobalSettings).IsAssignableFrom(t)
						where !typeof(IWrapper).IsAssignableFrom(t)
						select t).Select(delegate(Type t)
					{
						try
						{
							return Activator.CreateInstance(t) as GlobalSettings;
						}
						catch (Exception exception2)
						{
							try
							{
								logger.LogError(exception2, $"Failed to initialize type {t}");
							}
							catch (Exception)
							{
								logger.LogError(exception2, "Failed to initialize and log type!");
							}
							return (GlobalSettings)null;
						}
					}).OfType<GlobalSettings>().ToList();
				}
				catch (TypeLoadException exception)
				{
					settings = Array.Empty<GlobalSettings>();
					logger.LogError(exception, $"Error while handling assembly {assembly}!");
				}
				foreach (GlobalSettings item in settings)
				{
					yield return item;
				}
			}
		}
	}
}
