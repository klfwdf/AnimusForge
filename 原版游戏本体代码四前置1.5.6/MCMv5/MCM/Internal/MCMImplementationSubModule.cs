using System;
using System.IO;
using System.Linq;
using BUTR.DependencyInjection;
using BUTR.DependencyInjection.Extensions;
using MCM.Abstractions;
using MCM.Abstractions.GameFeatures;
using MCM.Abstractions.Properties;
using MCM.Implementation;
using MCM.Implementation.FluentBuilder;
using MCM.Implementation.Global;
using MCM.Implementation.PerCampaign;
using MCM.Implementation.PerSave;
using MCM.Internal.Extensions;
using MCM.Internal.GameFeatures;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace MCM.Internal;

internal class MCMImplementationSubModule : MBSubModuleBase, IGameEventListener
{
	private bool ServiceRegistrationWasCalled { get; set; }

	public event Action? GameStarted;

	public event Action? GameLoaded;

	public event Action? GameEnded;

	public void OnServiceRegistration()
	{
		ServiceRegistrationWasCalled = true;
		IGenericServiceContainer serviceContainer = this.GetServiceContainer();
		if (serviceContainer != null)
		{
			serviceContainer.AddSettingsContainer<FluentGlobalSettingsContainer>();
			serviceContainer.AddSettingsContainer<ExternalGlobalSettingsContainer>();
			serviceContainer.AddSettingsContainer<GlobalSettingsContainer>();
			serviceContainer.AddSettingsContainer<FluentPerSaveSettingsContainer>();
			serviceContainer.AddSettingsContainer<PerSaveSettingsContainer>();
			serviceContainer.AddSettingsContainer<FluentPerCampaignSettingsContainer>();
			serviceContainer.AddSettingsContainer<PerCampaignSettingsContainer>();
			serviceContainer.AddSettingsFormat<JsonSettingsFormat>();
			serviceContainer.AddSettingsFormat<XmlSettingsFormat>();
			serviceContainer.AddSettingsPropertyDiscoverer<IAttributeSettingsPropertyDiscoverer, AttributeSettingsPropertyDiscoverer>();
			serviceContainer.AddSettingsPropertyDiscoverer<IFluentSettingsPropertyDiscoverer, FluentSettingsPropertyDiscoverer>();
			serviceContainer.AddSettingsBuilderFactory<DefaultSettingsBuilderFactory>();
			serviceContainer.AddSettingsProvider<DefaultSettingsProvider>();
			serviceContainer.AddSingleton<IGameEventListener, MCMImplementationSubModule>((IGenericServiceFactory sp) => this);
			serviceContainer.AddSingleton<ICampaignIdProvider, CampaignIdProvider>();
			serviceContainer.AddSingleton<IFileSystemProvider, FileSystemProvider>();
			serviceContainer.AddScoped<PerSaveCampaignBehavior>();
			serviceContainer.AddTransient<IPerSaveSettingsProvider, PerSaveCampaignBehavior>((IGenericServiceFactory sp) => sp.GetService<PerSaveCampaignBehavior>());
		}
	}

	protected override void OnSubModuleLoad()
	{
		base.OnSubModuleLoad();
		PerformMigration001();
		PerformMigration002();
		if (!ServiceRegistrationWasCalled)
		{
			OnServiceRegistration();
		}
	}

	protected override void OnBeforeInitialModuleScreenSetAsRoot()
	{
		base.OnBeforeInitialModuleScreenSetAsRoot();
	}

	protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
	{
		base.OnGameStart(game, gameStarterObject);
		if (game.GameType is Campaign)
		{
			GameStarted?.Invoke();
			CampaignGameStarter campaignGameStarter = (CampaignGameStarter)gameStarterObject;
			campaignGameStarter.AddBehavior(GenericServiceProvider.GetService<PerSaveCampaignBehavior>());
		}
	}

	public override void OnNewGameCreated(Game game, object initializerObject)
	{
		base.OnNewGameCreated(game, initializerObject);
		if (game.GameType is Campaign)
		{
			GameLoaded?.Invoke();
		}
	}

	public override void OnGameLoaded(Game game, object initializerObject)
	{
		base.OnGameLoaded(game, initializerObject);
		if (game.GameType is Campaign)
		{
			GameLoaded?.Invoke();
		}
	}

	public override void OnGameEnd(Game game)
	{
		base.OnGameEnd(game);
		if (game.GameType is Campaign)
		{
			GameEnded?.Invoke();
		}
	}

	private static void PerformMigration001()
	{
		try
		{
			string fullPath = System.IO.Path.GetFullPath("Configs");
			string path = System.IO.Path.Combine(fullPath, "ModSettings");
			string text = System.IO.Path.Combine(PlatformFileHelperPCExtended.GetDirectoryFullPath(EngineFilePaths.ConfigsPath) ?? string.Empty, "ModSettings");
			if (!Directory.Exists(path) || !Directory.Exists(text))
			{
				return;
			}
			string[] files = Directory.GetFiles(path);
			foreach (string text2 in files)
			{
				string fileName = System.IO.Path.GetFileName(text2);
				string destFileName = System.IO.Path.Combine(text, fileName);
				try
				{
					File.Copy(text2, destFileName, overwrite: true);
					File.Delete(text2);
				}
				catch (Exception)
				{
				}
			}
			string[] directories = Directory.GetDirectories(path);
			foreach (string text3 in directories)
			{
				string fileName2 = System.IO.Path.GetFileName(text3);
				string target = System.IO.Path.Combine(text, fileName2);
				try
				{
					MoveDirectory(text3, target);
				}
				catch (Exception)
				{
				}
			}
			string[] files2 = Directory.GetFiles(path);
			if (files2 != null && files2.Length == 0)
			{
				files2 = Directory.GetDirectories(path);
				if (files2 != null && files2.Length == 0)
				{
					Directory.Delete(path, recursive: true);
				}
			}
			files2 = Directory.GetFiles(fullPath);
			if (files2 != null && files2.Length == 0)
			{
				files2 = Directory.GetDirectories(fullPath);
				if (files2 != null && files2.Length == 0)
				{
					Directory.Delete(fullPath, recursive: true);
				}
			}
		}
		catch (Exception)
		{
		}
		static void MoveDirectory(string source, string text5)
		{
			string text4 = source.TrimEnd('\\', ' ');
			string newValue = text5.TrimEnd('\\', ' ');
			foreach (IGrouping<string, string> item in Directory.EnumerateFiles(text4, "*", SearchOption.AllDirectories).GroupBy(System.IO.Path.GetDirectoryName))
			{
				string text6 = item.Key.Replace(text4, newValue);
				Directory.CreateDirectory(text6);
				foreach (string item2 in item)
				{
					string text7 = System.IO.Path.Combine(text6, System.IO.Path.GetFileName(item2));
					if (File.Exists(text7))
					{
						File.Delete(text7);
					}
					File.Move(item2, text7);
				}
			}
			Directory.Delete(source, recursive: true);
		}
	}

	private static void PerformMigration002()
	{
		try
		{
			string fullPath = System.IO.Path.GetFullPath("Configs");
			string fullPath2 = System.IO.Path.GetFullPath(System.IO.Path.Combine(PlatformFileHelperPCExtended.GetDirectoryFullPath(EngineFilePaths.ConfigsPath) ?? string.Empty, "../", "ModSettings"));
			string text = System.IO.Path.Combine(PlatformFileHelperPCExtended.GetDirectoryFullPath(EngineFilePaths.ConfigsPath) ?? string.Empty, "ModSettings");
			if (!Directory.Exists(fullPath2) || !Directory.Exists(text))
			{
				return;
			}
			string[] files = Directory.GetFiles(fullPath2);
			foreach (string text2 in files)
			{
				string fileName = System.IO.Path.GetFileName(text2);
				string destFileName = System.IO.Path.Combine(text, fileName);
				try
				{
					File.Copy(text2, destFileName, overwrite: true);
					File.Delete(text2);
				}
				catch (Exception)
				{
				}
			}
			string[] directories = Directory.GetDirectories(fullPath2);
			foreach (string text3 in directories)
			{
				string fileName2 = System.IO.Path.GetFileName(text3);
				string target = System.IO.Path.Combine(text, fileName2);
				try
				{
					MoveDirectory(text3, target);
				}
				catch (Exception)
				{
				}
			}
			string[] files2 = Directory.GetFiles(fullPath2);
			if (files2 != null && files2.Length == 0)
			{
				files2 = Directory.GetDirectories(fullPath2);
				if (files2 != null && files2.Length == 0)
				{
					Directory.Delete(fullPath2, recursive: true);
				}
			}
			files2 = Directory.GetFiles(fullPath);
			if (files2 != null && files2.Length == 0)
			{
				files2 = Directory.GetDirectories(fullPath);
				if (files2 != null && files2.Length == 0)
				{
					Directory.Delete(fullPath, recursive: true);
				}
			}
		}
		catch (Exception)
		{
		}
		static void MoveDirectory(string source, string text5)
		{
			string text4 = source.TrimEnd('\\', ' ');
			string newValue = text5.TrimEnd('\\', ' ');
			foreach (IGrouping<string, string> item in Directory.EnumerateFiles(text4, "*", SearchOption.AllDirectories).GroupBy(System.IO.Path.GetDirectoryName))
			{
				string text6 = item.Key.Replace(text4, newValue);
				Directory.CreateDirectory(text6);
				foreach (string item2 in item)
				{
					string text7 = System.IO.Path.Combine(text6, System.IO.Path.GetFileName(item2));
					if (File.Exists(text7))
					{
						File.Delete(text7);
					}
					File.Move(item2, text7);
				}
			}
			Directory.Delete(source, recursive: true);
		}
	}
}
