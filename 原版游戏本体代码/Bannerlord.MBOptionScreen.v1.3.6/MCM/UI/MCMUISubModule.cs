using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Bannerlord.BUTR.Shared.Helpers;
using Bannerlord.ButterLib.Common.Extensions;
using Bannerlord.ButterLib.HotKeys;
using Bannerlord.UIExtenderEx;
using BUTR.DependencyInjection;
using BUTR.DependencyInjection.ButterLib;
using BUTR.DependencyInjection.Extensions;
using BUTR.DependencyInjection.Logger;
using BUTR.MessageBoxPInvoke.Helpers;
using HarmonyLib;
using MCM.Abstractions;
using MCM.Internal.Extensions;
using MCM.UI.ButterLib;
using MCM.UI.Functionality;
using MCM.UI.Functionality.Injectors;
using MCM.UI.GUI.GauntletUI;
using MCM.UI.HotKeys;
using MCM.UI.Patches;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ScreenSystem;

namespace MCM.UI
{
	// Token: 0x02000010 RID: 16
	[NullableContext(1)]
	[Nullable(0)]
	public sealed class MCMUISubModule : MBSubModuleBase
	{
		// Token: 0x1700000A RID: 10
		// (get) Token: 0x0600003A RID: 58 RVA: 0x000027F1 File Offset: 0x000009F1
		// (set) Token: 0x0600003B RID: 59 RVA: 0x000027F9 File Offset: 0x000009F9
		private bool DelayedServiceCreation { get; set; }

		// Token: 0x1700000B RID: 11
		// (get) Token: 0x0600003C RID: 60 RVA: 0x00002802 File Offset: 0x00000A02
		// (set) Token: 0x0600003D RID: 61 RVA: 0x0000280A File Offset: 0x00000A0A
		private bool ServiceRegistrationWasCalled { get; set; }

		// Token: 0x1700000C RID: 12
		// (get) Token: 0x0600003E RID: 62 RVA: 0x00002813 File Offset: 0x00000A13
		// (set) Token: 0x0600003F RID: 63 RVA: 0x0000281B File Offset: 0x00000A1B
		private bool OnBeforeInitialModuleScreenSetAsRootWasCalled { get; set; }

		// Token: 0x06000040 RID: 64 RVA: 0x00002824 File Offset: 0x00000A24
		public MCMUISubModule()
		{
			MCMSubModule instance = MCMSubModule.Instance;
			if (instance != null)
			{
				instance.OverrideServiceContainer(new ButterLibServiceContainer());
			}
			MCMUISubModule.ValidateLoadOrder();
		}

		// Token: 0x06000041 RID: 65 RVA: 0x00002848 File Offset: 0x00000A48
		public void OnServiceRegistration()
		{
			this.ServiceRegistrationWasCalled = true;
			IGenericServiceContainer services = ServiceCollectionExtensions.GetServiceContainer(this);
			if (services != null)
			{
				ServiceCollectionExtensions.AddSettingsContainer<ButterLibSettingsContainer>(services);
				ServiceCollectionExtensions.AddTransient(services, typeof(IServiceProvider), () => DependencyInjectionExtensions.GetTempServiceProvider(this) ?? DependencyInjectionExtensions.GetServiceProvider(this));
				ServiceCollectionExtensions.AddTransient<IBUTRLogger, LoggerWrapper>(services);
				ServiceCollectionExtensions.AddTransient(services, typeof(IBUTRLogger), typeof(LoggerWrapper<>));
				ServiceCollectionExtensions.AddTransient<IMCMOptionsScreen, ModOptionsGauntletScreen>(services);
				ServiceCollectionExtensions.AddSingleton<BaseGameMenuScreenHandler, DefaultGameMenuScreenHandler>(services);
				ServiceCollectionExtensions.AddSingleton<ResourceInjector, DefaultResourceInjector>(services);
			}
		}

		// Token: 0x06000042 RID: 66 RVA: 0x000028C4 File Offset: 0x00000AC4
		protected override void OnSubModuleLoad()
		{
			base.OnSubModuleLoad();
			IServiceProvider serviceProvider;
			if (!this.ServiceRegistrationWasCalled)
			{
				this.OnServiceRegistration();
				this.DelayedServiceCreation = true;
				serviceProvider = DependencyInjectionExtensions.GetTempServiceProvider(this);
			}
			else
			{
				serviceProvider = DependencyInjectionExtensions.GetServiceProvider(this);
			}
			MCMUISubModule.Logger = ServiceProviderServiceExtensions.GetRequiredService<ILogger<MCMUISubModule>>(serviceProvider);
			LoggerExtensions.LogTrace(MCMUISubModule.Logger, "OnSubModuleLoad: Logging started...", Array.Empty<object>());
			Harmony optionsGauntletScreenHarmony = new Harmony("bannerlord.mcm.ui.optionsgauntletscreenpatch");
			OptionsGauntletScreenPatch.Patch(optionsGauntletScreenHarmony);
			MissionGauntletOptionsUIHandlerPatch.Patch(optionsGauntletScreenHarmony);
			Harmony optionsSwitchHarmony = new Harmony("bannerlord.mcm.ui.optionsswitchpatch");
			OptionsVMPatch.Patch(optionsSwitchHarmony);
		}

		// Token: 0x06000043 RID: 67 RVA: 0x00002944 File Offset: 0x00000B44
		protected override void OnBeforeInitialModuleScreenSetAsRoot()
		{
			base.OnBeforeInitialModuleScreenSetAsRoot();
			if (!this.OnBeforeInitialModuleScreenSetAsRootWasCalled)
			{
				this.OnBeforeInitialModuleScreenSetAsRootWasCalled = true;
				if (this.DelayedServiceCreation)
				{
					MCMUISubModule.Logger = ServiceProviderServiceExtensions.GetRequiredService<ILogger<MCMUISubModule>>(DependencyInjectionExtensions.GetServiceProvider(this));
				}
				MCMUISubModule.Extender.Register(typeof(MCMUISubModule).Assembly);
				MCMUISubModule.Extender.Enable();
				HotKeyManager hkm = HotKeyManager.Create("MCM.UI");
				if (hkm != null)
				{
					MCMUISubModule.ResetValueToDefault = hkm.Add<ResetValueToDefault>();
					hkm.Build();
				}
				ResourceInjector resourceInjector = GenericServiceProvider.GetService<ResourceInjector>();
				if (resourceInjector != null)
				{
					resourceInjector.Inject();
				}
			}
		}

		// Token: 0x06000044 RID: 68 RVA: 0x000029D0 File Offset: 0x00000BD0
		internal static void UpdateOptionScreen(MCMUISettings settings)
		{
			if (settings.UseStandardOptionScreen)
			{
				BaseGameMenuScreenHandler instance = BaseGameMenuScreenHandler.Instance;
				if (instance == null)
				{
					return;
				}
				instance.RemoveScreen("MCM_OptionScreen");
				return;
			}
			else
			{
				BaseGameMenuScreenHandler instance2 = BaseGameMenuScreenHandler.Instance;
				if (instance2 == null)
				{
					return;
				}
				instance2.AddScreen("MCM_OptionScreen", 9990, () => GenericServiceProvider.GetService<IMCMOptionsScreen>() as ScreenBase, new TextObject("{=MainMenu_ModOptions}Mod Options", null));
				return;
			}
		}

		// Token: 0x06000045 RID: 69 RVA: 0x00002A40 File Offset: 0x00000C40
		private static void ValidateLoadOrder()
		{
			List<ModuleInfoExtendedHelper> loadedModules = ModuleInfoHelper.GetLoadedModules().ToList<ModuleInfoExtendedHelper>();
			if (loadedModules.Count == 0)
			{
				return;
			}
			StringBuilder sb = new StringBuilder();
			string report;
			if (!ModuleInfoHelper.ValidateLoadOrder(typeof(MCMUISubModule), out report))
			{
				sb.AppendLine(report);
				sb.AppendLine();
				sb.AppendLine(new TextObject("{=eXs6FLm5DP}It's strongly recommended to terminate the game now. Do you wish to terminate it?", null).ToString());
				MessageBoxResult messageBoxResult = MessageBoxDialog.Show(sb.ToString(), new TextObject("{=dzeWx4xSfR}Warning from MCM!", null).ToString(), MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, (MessageBoxModal)262144U);
				if (messageBoxResult == MessageBoxResult.Yes)
				{
					Environment.Exit(1);
				}
			}
		}

		// Token: 0x0400000D RID: 13
		private const string SMessageContinue = "{=eXs6FLm5DP}It's strongly recommended to terminate the game now. Do you wish to terminate it?";

		// Token: 0x0400000E RID: 14
		private const string SWarningTitle = "{=dzeWx4xSfR}Warning from MCM!";

		// Token: 0x0400000F RID: 15
		private static readonly UIExtender Extender = new UIExtender("MCM.UI");

		// Token: 0x04000010 RID: 16
		internal static ILogger<MCMUISubModule> Logger = NullLogger<MCMUISubModule>.Instance;

		// Token: 0x04000011 RID: 17
		[Nullable(2)]
		internal static ResetValueToDefault ResetValueToDefault;
	}
}
