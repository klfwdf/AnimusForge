using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using BUTR.DependencyInjection;
using HarmonyLib;
using HarmonyLib.BUTR.Extensions;
using MCM.Abstractions;
using MCM.Internal.Extensions;
using MCM.UI.Adapter.MCMv5.Properties;
using MCM.UI.Adapter.MCMv5.Providers;
using TaleWorlds.MountAndBlade;

namespace MCM.UI
{
	// Token: 0x0200000E RID: 14
	[NullableContext(1)]
	[Nullable(0)]
	public sealed class MCMUIAdapterSubModule : MBSubModuleBase
	{
		// Token: 0x0600002A RID: 42 RVA: 0x0000259C File Offset: 0x0000079C
		private static void OnAfterSetInitialModuleScreenAsRootScreen()
		{
			IEnumerable<IExternalSettingsProvider> service = GenericServiceProvider.GetService<IEnumerable<IExternalSettingsProvider>>();
			IEnumerable<IExternalSettingsProviderHasInitialize> enumerable = ((service != null) ? service.OfType<IExternalSettingsProviderHasInitialize>() : null) ?? Array.Empty<IExternalSettingsProviderHasInitialize>();
			foreach (IExternalSettingsProviderHasInitialize hasInitialize in enumerable)
			{
				hasInitialize.Initialize();
			}
		}

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x0600002B RID: 43 RVA: 0x00002600 File Offset: 0x00000800
		private Harmony Harmony { get; } = new Harmony("MCM.UI.Adapter.MCMv5");

		// Token: 0x17000004 RID: 4
		// (get) Token: 0x0600002C RID: 44 RVA: 0x00002608 File Offset: 0x00000808
		// (set) Token: 0x0600002D RID: 45 RVA: 0x00002610 File Offset: 0x00000810
		private bool ServiceRegistrationWasCalled { get; set; }

		// Token: 0x0600002E RID: 46 RVA: 0x0000261C File Offset: 0x0000081C
		public void OnServiceRegistration()
		{
			this.ServiceRegistrationWasCalled = true;
			IGenericServiceContainer services = ServiceCollectionExtensions.GetServiceContainer(this);
			if (services != null)
			{
				ServiceCollectionExtensions.AddSettingsPropertyDiscoverer<MCMv5AttributeSettingsPropertyDiscoverer>(services);
				ServiceCollectionExtensions.AddSettingsPropertyDiscoverer<MCMv5FluentSettingsPropertyDiscoverer>(services);
				ServiceCollectionExtensions.AddExternalSettingsProvider<MCMv5ExternalSettingsProvider>(services);
			}
		}

		// Token: 0x0600002F RID: 47 RVA: 0x00002650 File Offset: 0x00000850
		protected override void OnSubModuleLoad()
		{
			base.OnSubModuleLoad();
			if (!this.ServiceRegistrationWasCalled)
			{
				this.OnServiceRegistration();
			}
			this.Harmony.Patch(AccessTools2.Method(typeof(Module), "SetInitialModuleScreenAsRootScreen", null, null, true), null, new HarmonyMethod(AccessTools2.Method(typeof(MCMUIAdapterSubModule), "OnAfterSetInitialModuleScreenAsRootScreen", null, null, true)), null, null);
		}

		// Token: 0x06000030 RID: 48 RVA: 0x000026B4 File Offset: 0x000008B4
		protected override void OnSubModuleUnloaded()
		{
			base.OnSubModuleUnloaded();
			this.Harmony.Unpatch(AccessTools2.Method(typeof(Module), "SetInitialModuleScreenAsRootScreen", null, null, true), AccessTools2.Method(typeof(MCMUIAdapterSubModule), "OnAfterSetInitialModuleScreenAsRootScreen", null, null, true));
		}
	}
}
