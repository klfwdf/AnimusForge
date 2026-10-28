using System;
using BUTR.DependencyInjection;
using BUTR.DependencyInjection.Extensions;
using BUTR.DependencyInjection.Logger;
using MCM.Abstractions;
using MCM.Abstractions.Properties;
using MCM.Internal.Extensions;
using MCM.LightInject;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace MCM;

public class MCMSubModule : MBSubModuleBase
{
	private static IBUTRLogger<MCMSubModule> Logger = new DefaultBUTRLogger<MCMSubModule>();

	private static readonly ServiceContainer LightInjectServiceContainer = new ServiceContainer(delegate(ContainerOptions options)
	{
		options.EnableCurrentScope = false;
	});

	public static MCMSubModule? Instance { get; private set; }

	private bool ServiceRegistrationWasCalled { get; set; }

	private bool OnBeforeInitialModuleScreenSetAsRootWasCalled { get; set; }

	public MCMSubModule()
	{
		Instance = this;
		BUTR.DependencyInjection.Extensions.ServiceCollectionExtensions.ServiceContainer = new WithHistoryGenericServiceContainer(new LightInjectServiceContainer(LightInjectServiceContainer));
	}

	public void OnServiceRegistration()
	{
		ServiceRegistrationWasCalled = true;
		IGenericServiceContainer serviceContainer = this.GetServiceContainer();
		if (serviceContainer != null)
		{
			serviceContainer.AddSettingsFormat<MemorySettingsFormat>();
			serviceContainer.AddSettingsFormat<MemorySettingsFormat>();
			serviceContainer.AddSettingsPropertyDiscoverer<NoneSettingsPropertyDiscoverer>();
			serviceContainer.AddTransient<IBUTRLogger, DefaultBUTRLogger>();
			serviceContainer.AddTransient(typeof(IBUTRLogger<>), typeof(DefaultBUTRLogger<>));
		}
	}

	protected override void OnSubModuleLoad()
	{
		base.OnSubModuleLoad();
		if (!ServiceRegistrationWasCalled)
		{
			OnServiceRegistration();
		}
	}

	protected override void OnSubModuleUnloaded()
	{
		base.OnSubModuleUnloaded();
		Instance = null;
	}

	protected override void OnBeforeInitialModuleScreenSetAsRoot()
	{
		base.OnBeforeInitialModuleScreenSetAsRoot();
		if (!OnBeforeInitialModuleScreenSetAsRootWasCalled)
		{
			OnBeforeInitialModuleScreenSetAsRootWasCalled = true;
			GenericServiceProvider.GlobalServiceProvider = BUTR.DependencyInjection.Extensions.ServiceCollectionExtensions.ServiceContainer.Build();
			Logger = GenericServiceProvider.GetService<IBUTRLogger<MCMSubModule>>() ?? Logger;
			BaseSettingsProvider.Instance = GenericServiceProvider.GetService<BaseSettingsProvider>();
		}
	}

	public void OverrideServiceContainer(IGenericServiceContainer serviceContainer)
	{
		WithHistoryGenericServiceContainer serviceContainer2 = BUTR.DependencyInjection.Extensions.ServiceCollectionExtensions.ServiceContainer;
		if (serviceContainer2 != null)
		{
			BUTR.DependencyInjection.Extensions.ServiceCollectionExtensions.ServiceContainer = new WithHistoryGenericServiceContainer(serviceContainer);
			{
				foreach (Action<IGenericServiceContainer> item in serviceContainer2.History)
				{
					item(BUTR.DependencyInjection.Extensions.ServiceCollectionExtensions.ServiceContainer);
				}
				return;
			}
		}
		BUTR.DependencyInjection.Extensions.ServiceCollectionExtensions.ServiceContainer = new WithHistoryGenericServiceContainer(serviceContainer);
	}

	public override void OnCampaignStart(Game game, object starterObject)
	{
		base.OnCampaignStart(game, starterObject);
		if (starterObject is CampaignGameStarter campaignGameStarter)
		{
			campaignGameStarter.AddBehavior(new SettingsProviderCampaignBehavior(GenericServiceProvider.GetService<BaseSettingsProvider>()));
		}
	}

	public override void OnMissionBehaviorInitialize(Mission mission)
	{
		base.OnMissionBehaviorInitialize(mission);
		mission.AddMissionBehavior(new SettingsProviderMissionBehavior(GenericServiceProvider.GetService<BaseSettingsProvider>()));
	}

	protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
	{
		base.OnGameStart(game, gameStarterObject);
		GenericServiceProvider.GameScopeServiceProvider = GenericServiceProvider.CreateScope();
	}

	public override void OnGameEnd(Game game)
	{
		base.OnGameEnd(game);
		GenericServiceProvider.GameScopeServiceProvider?.Dispose();
		GenericServiceProvider.GameScopeServiceProvider = null;
	}
}
