using System;
using System.Collections.Generic;
using System.Linq;
using BUTR.DependencyInjection;
using HarmonyLib;
using HarmonyLib.BUTR.Extensions;
using MCM.Abstractions;
using MCM.Internal.Extensions;
using MCM.UI.Adapter.MCMv5.Properties;
using MCM.UI.Adapter.MCMv5.Providers;
using TaleWorlds.MountAndBlade;

namespace MCM.UI;

public sealed class MCMUIAdapterSubModule : MBSubModuleBase
{
	private Harmony Harmony { get; } = new Harmony("MCM.UI.Adapter.MCMv5");

	private bool ServiceRegistrationWasCalled { get; set; }

	private static void OnAfterSetInitialModuleScreenAsRootScreen()
	{
		IEnumerable<IExternalSettingsProviderHasInitialize> enumerable = GenericServiceProvider.GetService<IEnumerable<IExternalSettingsProvider>>()?.OfType<IExternalSettingsProviderHasInitialize>() ?? Array.Empty<IExternalSettingsProviderHasInitialize>();
		foreach (IExternalSettingsProviderHasInitialize item in enumerable)
		{
			item.Initialize();
		}
	}

	public void OnServiceRegistration()
	{
		ServiceRegistrationWasCalled = true;
		IGenericServiceContainer serviceContainer = this.GetServiceContainer();
		if (serviceContainer != null)
		{
			serviceContainer.AddSettingsPropertyDiscoverer<MCMv5AttributeSettingsPropertyDiscoverer>();
			serviceContainer.AddSettingsPropertyDiscoverer<MCMv5FluentSettingsPropertyDiscoverer>();
			serviceContainer.AddExternalSettingsProvider<MCMv5ExternalSettingsProvider>();
		}
	}

	protected override void OnSubModuleLoad()
	{
		base.OnSubModuleLoad();
		if (!ServiceRegistrationWasCalled)
		{
			OnServiceRegistration();
		}
		Harmony.Patch(HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(Module), "SetInitialModuleScreenAsRootScreen"), null, new HarmonyMethod(HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MCMUIAdapterSubModule), "OnAfterSetInitialModuleScreenAsRootScreen")));
	}

	protected override void OnSubModuleUnloaded()
	{
		base.OnSubModuleUnloaded();
		Harmony.Unpatch(HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(Module), "SetInitialModuleScreenAsRootScreen"), HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(MCMUIAdapterSubModule), "OnAfterSetInitialModuleScreenAsRootScreen"));
	}
}
