using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BUTR.DependencyInjection;
using BUTR.DependencyInjection.ButterLib;
using BUTR.DependencyInjection.Extensions;
using BUTR.DependencyInjection.Logger;
using BUTR.MessageBoxPInvoke.Helpers;
using Bannerlord.BUTR.Shared.Helpers;
using Bannerlord.ButterLib.Common.Extensions;
using Bannerlord.ButterLib.HotKeys;
using Bannerlord.UIExtenderEx;
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

namespace MCM.UI;

public sealed class MCMUISubModule : MBSubModuleBase
{
	private const string SMessageContinue = "{=eXs6FLm5DP}It's strongly recommended to terminate the game now. Do you wish to terminate it?";

	private const string SWarningTitle = "{=dzeWx4xSfR}Warning from MCM!";

	private static readonly UIExtender Extender = new UIExtender("MCM.UI");

	internal static ILogger<MCMUISubModule> Logger = NullLogger<MCMUISubModule>.Instance;

	internal static ResetValueToDefault? ResetValueToDefault;

	private bool DelayedServiceCreation { get; set; }

	private bool ServiceRegistrationWasCalled { get; set; }

	private bool OnBeforeInitialModuleScreenSetAsRootWasCalled { get; set; }

	public MCMUISubModule()
	{
		MCMSubModule.Instance?.OverrideServiceContainer(new ButterLibServiceContainer());
		ValidateLoadOrder();
	}

	public void OnServiceRegistration()
	{
		ServiceRegistrationWasCalled = true;
		IGenericServiceContainer serviceContainer = this.GetServiceContainer();
		if (serviceContainer != null)
		{
			serviceContainer.AddSettingsContainer<ButterLibSettingsContainer>();
			serviceContainer.AddTransient(typeof(IServiceProvider), () => this.GetTempServiceProvider() ?? this.GetServiceProvider());
			serviceContainer.AddTransient<IBUTRLogger, LoggerWrapper>();
			serviceContainer.AddTransient(typeof(IBUTRLogger<>), typeof(LoggerWrapper<>));
			serviceContainer.AddTransient<IMCMOptionsScreen, ModOptionsGauntletScreen>();
			serviceContainer.AddSingleton<BaseGameMenuScreenHandler, DefaultGameMenuScreenHandler>();
			serviceContainer.AddSingleton<ResourceInjector, DefaultResourceInjector>();
		}
	}

	protected override void OnSubModuleLoad()
	{
		base.OnSubModuleLoad();
		IServiceProvider provider;
		if (!ServiceRegistrationWasCalled)
		{
			OnServiceRegistration();
			DelayedServiceCreation = true;
			provider = this.GetTempServiceProvider();
		}
		else
		{
			provider = this.GetServiceProvider();
		}
		Logger = provider.GetRequiredService<ILogger<MCMUISubModule>>();
		Logger.LogTrace("OnSubModuleLoad: Logging started...");
		Harmony harmony = new Harmony("bannerlord.mcm.ui.optionsgauntletscreenpatch");
		OptionsGauntletScreenPatch.Patch(harmony);
		MissionGauntletOptionsUIHandlerPatch.Patch(harmony);
		Harmony harmony2 = new Harmony("bannerlord.mcm.ui.optionsswitchpatch");
		OptionsVMPatch.Patch(harmony2);
	}

	protected override void OnBeforeInitialModuleScreenSetAsRoot()
	{
		base.OnBeforeInitialModuleScreenSetAsRoot();
		if (!OnBeforeInitialModuleScreenSetAsRootWasCalled)
		{
			OnBeforeInitialModuleScreenSetAsRootWasCalled = true;
			if (DelayedServiceCreation)
			{
				Logger = this.GetServiceProvider().GetRequiredService<ILogger<MCMUISubModule>>();
			}
			Extender.Register(typeof(MCMUISubModule).Assembly);
			Extender.Enable();
			HotKeyManager hotKeyManager = HotKeyManager.Create("MCM.UI");
			if (hotKeyManager != null)
			{
				ResetValueToDefault = hotKeyManager.Add<ResetValueToDefault>();
				hotKeyManager.Build();
			}
			GenericServiceProvider.GetService<ResourceInjector>()?.Inject();
		}
	}

	internal static void UpdateOptionScreen(MCMUISettings settings)
	{
		if (settings.UseStandardOptionScreen)
		{
			BaseGameMenuScreenHandler.Instance?.RemoveScreen("MCM_OptionScreen");
			return;
		}
		BaseGameMenuScreenHandler.Instance?.AddScreen("MCM_OptionScreen", 9990, () => GenericServiceProvider.GetService<IMCMOptionsScreen>() as ScreenBase, new TextObject("{=MainMenu_ModOptions}Mod Options"));
	}

	private static void ValidateLoadOrder()
	{
		List<Bannerlord.BUTR.Shared.Helpers.ModuleInfoExtendedHelper> list = Bannerlord.BUTR.Shared.Helpers.ModuleInfoHelper.GetLoadedModules().ToList();
		if (list.Count == 0)
		{
			return;
		}
		StringBuilder stringBuilder = new StringBuilder();
		if (!Bannerlord.BUTR.Shared.Helpers.ModuleInfoHelper.ValidateLoadOrder(typeof(MCMUISubModule), out string report))
		{
			stringBuilder.AppendLine(report);
			stringBuilder.AppendLine();
			stringBuilder.AppendLine(new TextObject("{=eXs6FLm5DP}It's strongly recommended to terminate the game now. Do you wish to terminate it?").ToString());
			BUTR.MessageBoxPInvoke.Helpers.MessageBoxResult messageBoxResult = BUTR.MessageBoxPInvoke.Helpers.MessageBoxDialog.Show(stringBuilder.ToString(), new TextObject("{=dzeWx4xSfR}Warning from MCM!").ToString(), BUTR.MessageBoxPInvoke.Helpers.MessageBoxButtons.YesNo, BUTR.MessageBoxPInvoke.Helpers.MessageBoxIcon.Warning, BUTR.MessageBoxPInvoke.Helpers.MessageBoxDefaultButton.Button1, (BUTR.MessageBoxPInvoke.Helpers.MessageBoxModal)262144u);
			if (messageBoxResult == BUTR.MessageBoxPInvoke.Helpers.MessageBoxResult.Yes)
			{
				Environment.Exit(1);
			}
		}
	}
}
