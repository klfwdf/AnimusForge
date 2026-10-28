#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Logger;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Bannerlord.BUTR.Shared.Helpers;
using Bannerlord.ButterLib.Common.Extensions;
using Bannerlord.ButterLib.CrashUploader;
using Bannerlord.ButterLib.DelayedSubModule;
using Bannerlord.ButterLib.DynamicAPI;
using Bannerlord.ButterLib.ExceptionHandler;
using Bannerlord.ButterLib.ObjectSystem.Extensions;
using Bannerlord.ButterLib.Options;
using Bannerlord.ButterLib.SubModuleWrappers2;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.ButterLib;

public sealed class ButterLibSubModule : MBSubModuleBase
{
	private const string SWarningTitle = "{=BguqytVG3q}Warning from Bannerlord.ButterLib!";

	private const string SMessageContinue = "{=eXs6FLm5DP}It's strongly recommended to terminate the game now. Do you wish to terminate it?";

	private ILogger Logger { get; set; }

	private bool DelayedServiceCreation { get; set; }

	private bool ServiceRegistrationWasCalled { get; set; }

	private bool OnBeforeInitialModuleScreenSetAsRootWasCalled { get; set; }

	private TextWriterTraceListener? TextWriterTraceListener { get; set; }

	public static bool CanBeConfigured { get; private set; } = true;

	private static List<Action<IServiceCollection>> BeforeInitialization { get; } = new List<Action<IServiceCollection>>();

	public static ButterLibSubModule? Instance { get; set; }

	private static IServiceProvider? GlobalServiceProvider { get; set; }

	private static IServiceProvider? GameScopeServiceProvider => GameScope?.ServiceProvider;

	private static IServiceScope? GameScope { get; set; }

	internal static IServiceProvider? ServiceProvider => GameScopeServiceProvider ?? GlobalServiceProvider;

	internal static IServiceCollection? Services { get; set; }

	internal event Action<float>? OnApplicationTickEvent;

	public ButterLibSubModule()
	{
		Instance = this;
		ValidateLoadOrder();
	}

	public void OnServiceRegistration()
	{
		ServiceRegistrationWasCalled = true;
		CanBeConfigured = false;
		Services = new ServiceCollection();
		Services.AddOptions();
		Services.Configure(delegate(ButterLibOptions o)
		{
			ButterLibOptions settings = SettingsProvider.GetSettings();
			o.MinLogLevel = settings.MinLogLevel;
		});
		foreach (Action<IServiceCollection> item in BeforeInitialization)
		{
			item?.Invoke(Services);
		}
		this.AddDefaultSerilogLogger();
		this.AddSerilogLoggerProvider("butterlib.txt", new global::_003C_003Ez__ReadOnlySingleElementList<string>("Bannerlord.ButterLib.*"));
		this.AddSerilogLoggerProvider("trace.txt", new global::_003C_003Ez__ReadOnlySingleElementList<string>("System.Diagnostics.Logger.*"));
		Services.AddSubSystem<DelayedSubModuleSubSystem>();
		Services.AddSubSystem<ExceptionHandlerSubSystem>();
		Services.AddSubSystem<CrashUploaderSubSystem>();
		Services.AddSubSystem<SubModuleWrappers2SubSystem>();
		Services.AddSingleton<ICrashUploader, BUTRCrashUploader>();
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
		Logger = provider.GetRequiredService<ILogger<ButterLibSubModule>>();
		Logger.LogTrace("OnSubModuleLoad: Logging started...");
		if (!DelayedServiceCreation)
		{
			InitializeServices();
		}
		DelayedSubModuleSubSystem.Instance?.Enable();
		SubModuleWrappers2SubSystem.Instance?.Enable();
		ExceptionHandlerSubSystem.Instance?.Enable();
		CrashUploaderSubSystem.Instance?.Enable();
		TraceListenerCollection listeners = Trace.Listeners;
		TextWriterTraceListener listener = (TextWriterTraceListener = new TextWriterTraceListener(new StreamWriter(new MemoryStream(), Encoding.UTF8, 1024, leaveOpen: true)));
		listeners.Add(listener);
		Trace.AutoFlush = true;
		Logger.LogTrace("Added System.Diagnostics.Trace temporary listener");
		Logger.LogTrace("OnSubModuleLoad: Done");
	}

	protected override void OnSubModuleUnloaded()
	{
		base.OnSubModuleUnloaded();
		Logger.LogTrace("OnSubModuleUnloaded: Started...");
		Instance = null;
		Logger.LogTrace("OnSubModuleUnloaded: Done");
	}

	protected override void OnBeforeInitialModuleScreenSetAsRoot()
	{
		base.OnBeforeInitialModuleScreenSetAsRoot();
		Logger.LogTrace("OnBeforeInitialModuleScreenSetAsRoot: Started...");
		if (!OnBeforeInitialModuleScreenSetAsRootWasCalled)
		{
			OnBeforeInitialModuleScreenSetAsRootWasCalled = true;
			if (DelayedServiceCreation)
			{
				InitializeServices();
			}
		}
		DynamicAPIProvider.Initialize();
		Logger.LogTrace("OnBeforeInitialModuleScreenSetAsRoot: Done");
	}

	protected override void OnApplicationTick(float dt)
	{
		OnApplicationTickEvent?.Invoke(dt);
	}

	protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
	{
		base.OnGameStart(game, gameStarterObject);
		Logger.LogTrace("OnGameStart: Started");
		GameScope = ServiceProvider.CreateScope();
		Logger.LogInformation("Created GameScope");
		Logger.LogTrace("OnGameStart: Done");
	}

	public override void OnGameEnd(Game game)
	{
		base.OnGameEnd(game);
		Logger.LogTrace("OnGameEnd: Started");
		GameScope?.Dispose();
		GameScope = null;
		if (game.GameType is Campaign)
		{
			MBObjectBaseExtensions.OnGameEnd();
		}
		Logger.LogTrace("OnGameEnd: Done");
	}

	private static void ValidateLoadOrder()
	{
		if (ModuleInfoHelper.GetLoadedModules().ToList().Count == 0)
		{
			return;
		}
		StringBuilder stringBuilder = new StringBuilder();
		if (!ModuleInfoHelper.ValidateLoadOrder(typeof(ButterLibSubModule), out string report))
		{
			stringBuilder.AppendLine(report);
			stringBuilder.AppendLine();
			stringBuilder.AppendLine(new TextObject("{=eXs6FLm5DP}It's strongly recommended to terminate the game now. Do you wish to terminate it?").ToString());
			if (MessageBox.Show(stringBuilder.ToString(), new TextObject("{=BguqytVG3q}Warning from Bannerlord.ButterLib!").ToString(), MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, (MessageBoxOptions)262144) == DialogResult.Yes)
			{
				Environment.Exit(1);
			}
		}
	}

	private void InitializeServices()
	{
		if (Services == null)
		{
			return;
		}
		GlobalServiceProvider = Services.BuildServiceProvider();
		Logger.LogTrace("Created GlobalServiceProvider");
		Services = null;
		Logger.LogTrace("Set Services to null");
		Logger = this.GetServiceProvider().GetRequiredService<ILogger<ButterLibSubModule>>();
		Logger.LogTrace("Assigned new _logger from GlobalServiceProvider");
		ILogger<LoggerTraceListener> requiredService = this.GetServiceProvider().GetRequiredService<ILogger<LoggerTraceListener>>();
		Trace.Listeners.Add(new LoggerTraceListener(requiredService));
		Logger.LogTrace("Added System.Diagnostics.Trace main listener");
		if (TextWriterTraceListener == null)
		{
			return;
		}
		try
		{
			Trace.Flush();
			Trace.Listeners.Remove(TextWriterTraceListener);
			if (!(TextWriterTraceListener.Writer is StreamWriter { BaseStream: MemoryStream baseStream }))
			{
				return;
			}
			baseStream.Seek(0L, SeekOrigin.Begin);
			using StreamReader streamReader = new StreamReader(baseStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, 1024, leaveOpen: true);
			while (streamReader.Peek() >= 0)
			{
				Trace.WriteLine(streamReader.ReadLine());
			}
			Logger.LogTrace("Flushed logs from the System.Diagnostics.Trace temp listener");
		}
		finally
		{
			TextWriterTraceListener.Dispose();
		}
	}

	public static void ConfigureBeforeInitialization(Action<IServiceCollection> action)
	{
		if (CanBeConfigured)
		{
			BeforeInitialization.Add(action);
		}
	}
}
