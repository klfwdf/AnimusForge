using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Bannerlord.ButterLib.Logger;
using Bannerlord.ButterLib.Options;
using Bannerlord.ButterLib.SubSystems;
using HarmonyLib;
using HarmonyLib.BUTR.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Serilog.Filters;
using Serilog.Sinks.File;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.ButterLib.Common.Extensions;

public static class DependencyInjectionExtensions
{
	private static readonly AccessTools.FieldRef<LoggerConfiguration, List<ILogEventSink>>? _getSinks = HarmonyLib.BUTR.Extensions.AccessTools2.FieldRefAccess<LoggerConfiguration, List<ILogEventSink>>("_logEventSinks");

	private static readonly PlatformDirectoryPath ModLogsPath = EngineFilePaths.ConfigsPath + "/ModLogs";

	private static readonly string OutputTemplate = "[{Timestamp:o}] [{SourceContext}] [{Level:u3}]: {Message:lj}{NewLine}{Exception}";

	public static IServiceProvider? GetServiceProvider(this Game _)
	{
		return ButterLibSubModule.ServiceProvider;
	}

	public static IServiceProvider? GetServiceProvider(this CampaignBehaviorBase _)
	{
		return ButterLibSubModule.ServiceProvider;
	}

	public static IServiceProvider? GetServiceProvider(this MBSubModuleBase _)
	{
		return ButterLibSubModule.ServiceProvider;
	}

	public static IServiceCollection? GetServices(this MBSubModuleBase _)
	{
		return ButterLibSubModule.Services;
	}

	public static IServiceProvider? GetTempServiceProvider(this MBSubModuleBase _)
	{
		return ButterLibSubModule.Services?.BuildServiceProvider();
	}

	internal static IServiceCollection? AddDefaultSerilogLogger(this MBSubModuleBase subModule)
	{
		IServiceCollection? services = subModule.GetServices();
		IOptions<ButterLibOptions> service = services.BuildServiceProvider().GetService<IOptions<ButterLibOptions>>();
		PlatformFilePath platformFilePath = new PlatformFilePath(ModLogsPath, "default.log");
		LoggerSinkConfiguration writeTo = new LoggerConfiguration().MinimumLevel.Is((LogEventLevel)service.Value.MinLogLevel).Enrich.FromLogContext().WriteTo;
		string outputTemplate = OutputTemplate;
		string fileFullPath = platformFilePath.FileFullPath;
		int? retainedFileCountLimit = 7;
		long? fileSizeLimitBytes = 5242880L;
		TimeSpan? retainedFileTimeLimit = TimeSpan.FromDays(7.0);
		LoggerConfiguration loggerConfiguration = writeTo.File(fileFullPath, LogEventLevel.Verbose, outputTemplate, null, fileSizeLimitBytes, null, buffered: false, shared: true, null, RollingInterval.Day, rollOnFileSizeLimit: true, retainedFileCountLimit, null, null, retainedFileTimeLimit);
		IFlushableFileSink[] sinks = _getSinks(loggerConfiguration).OfType<IFlushableFileSink>().ToArray();
		Serilog.Core.Logger logger = loggerConfiguration.CreateLogger();
		services.AddSingleton((ILogSource)new RollingFileLogSource(platformFilePath.FileFullPath, sinks));
		services.AddLogging(delegate(ILoggingBuilder loggingBuilder)
		{
			loggingBuilder.AddSerilog(logger, dispose: true);
		});
		return services;
	}

	public static IServiceCollection AddSerilogLoggerProvider(this MBSubModuleBase subModule, string filename, IEnumerable<Assembly> filter)
	{
		return subModule.AddSerilogLoggerProvider(filename, filter.Select((Assembly x) => x.GetName().Name ?? string.Empty));
	}

	public static IServiceCollection AddSerilogLoggerProvider(this MBSubModuleBase subModule, string filename, IEnumerable<string>? filter = null, Action<LoggerConfiguration>? configure = null)
	{
		if (filter == null)
		{
			filter = new List<string> { subModule.GetType().Assembly.GetName().Name + ".*" };
		}
		List<string> sources = filter.ToList();
		IServiceCollection? obj = subModule.GetServices() ?? throw new Exception("Past Configuration stage.");
		PlatformFilePath platformFilePath = new PlatformFilePath(ModLogsPath, filename);
		LoggerSinkConfiguration writeTo = new LoggerConfiguration().Enrich.FromLogContext().Filter.ByIncludingOnly(FromSources(sources)).WriteTo;
		string outputTemplate = OutputTemplate;
		string fileFullPath = platformFilePath.FileFullPath;
		int? retainedFileCountLimit = 5;
		long? fileSizeLimitBytes = 3145728L;
		TimeSpan? retainedFileTimeLimit = TimeSpan.FromDays(3.0);
		LoggerConfiguration loggerConfiguration = writeTo.File(fileFullPath, LogEventLevel.Verbose, outputTemplate, null, fileSizeLimitBytes, null, buffered: false, shared: false, null, RollingInterval.Day, rollOnFileSizeLimit: true, retainedFileCountLimit, null, null, retainedFileTimeLimit);
		configure?.Invoke(loggerConfiguration);
		Serilog.Core.Logger logger = loggerConfiguration.CreateLogger();
		obj.AddSingleton<ILoggerProvider, SerilogLoggerProvider>((IServiceProvider _) => new SerilogLoggerProvider(logger, dispose: true));
		return obj;
	}

	public static Func<LogEvent, bool> FromSources(IEnumerable<string> sources)
	{
		if (sources == null)
		{
			throw new ArgumentNullException("sources");
		}
		return Matching.WithProperty("SourceContext", (string s) => s != null && sources.Any((string x) => MatchWildcardString(x, s)));
	}

	private static bool MatchWildcardString(string pattern, string input)
	{
		string text = pattern.Aggregate("^", (string current, char c) => current + c switch
		{
			'*' => ".*", 
			'?' => ".", 
			_ => $"[{c}]", 
		});
		return Regex.IsMatch(input, text + "$");
	}

	public static IServiceCollection AddSubSystem<TImplementation>(this IServiceCollection services) where TImplementation : class, ISubSystem, new()
	{
		TImplementation instance = new TImplementation();
		services.AddSingleton((IServiceProvider _) => instance);
		services.AddSingleton((Func<IServiceProvider, ISubSystem>)((IServiceProvider sp) => sp.GetService<TImplementation>()));
		return services;
	}

	public static ISubSystem? GetSubSystem(this IServiceProvider sp, string id)
	{
		return sp.GetServices<ISubSystem>().FirstOrDefault((ISubSystem s) => s.Id == id);
	}
}
