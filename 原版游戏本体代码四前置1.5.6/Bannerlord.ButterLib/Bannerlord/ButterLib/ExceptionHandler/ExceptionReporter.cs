using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using BUTR.CrashReport;
using BUTR.CrashReport.Bannerlord;
using BUTR.CrashReport.Models;
using BUTR.CrashReport.Renderer.ImGui;
using BUTR.CrashReport.Renderer.WinForms;
using Bannerlord.BLSE;
using Bannerlord.ButterLib.ExceptionHandler.Extensions;
using Bannerlord.ButterLib.ExceptionHandler.Utils;
using Bannerlord.ButterLib.Logger;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Sinks.File;
using TaleWorlds.Library;

namespace Bannerlord.ButterLib.ExceptionHandler;

[BLSEExceptionHandler]
public static class ExceptionReporter
{
	private const string ExceptionWasShownMarker = "ButterLibExceptionWasShown";

	private static void OnException(Exception exception)
	{
		Show(exception);
	}

	public static void Show(Exception exception)
	{
		if (exception.Data.Contains("ButterLibExceptionWasShown"))
		{
			object obj = exception.Data["ButterLibExceptionWasShown"];
			if (obj is bool && (bool)obj)
			{
				return;
			}
		}
		exception.Data["ButterLibExceptionWasShown"] = true;
		Dictionary<string, string> dictionary = new Dictionary<string, string> { 
		{
			"METADATA:TW_ConfigName",
			TaleWorlds.Library.Common.ConfigName
		} };
		Process process = Process.GetCurrentProcess().ParentProcess();
		if (process != null)
		{
			dictionary.Add("Parent_Process_Name", process.ProcessName);
			dictionary.Add("Parent_Process_File_Version", process.MainModule?.FileVersionInfo.FileVersion ?? "0");
		}
		StacktraceFilter stacktraceFilter = new StacktraceFilter(BEWPatch.FinalizerMethod);
		CrashReportInfoHelper crashReportInfoHelper = new CrashReportInfoHelper();
		HarmonyProvider harmonyProvider = new HarmonyProvider();
		CrashReportRendererUtilities crashReportRendererUtilities = new CrashReportRendererUtilities();
		CrashReportModel crashReportModel = CrashReportInfo.ToModel(CrashReportInfo.Create(exception, dictionary, stacktraceFilter, crashReportInfoHelper, crashReportInfoHelper, crashReportInfoHelper, harmonyProvider), crashReportInfoHelper, crashReportInfoHelper, crashReportInfoHelper, crashReportInfoHelper, crashReportInfoHelper, crashReportInfoHelper);
		LogSource[] logSources = GetLogSources().ToArray();
		try
		{
			CrashReportImGui.ShowAndWait(crashReportModel, logSources, crashReportRendererUtilities);
		}
		catch (Exception ex)
		{
			try
			{
				new CrashReportWinForms(crashReportModel, logSources, crashReportRendererUtilities).ShowDialog();
			}
			catch (Exception ex2)
			{
				throw new AggregateException(ex, ex2);
			}
		}
	}

	private static IEnumerable<LogSource> GetLogSources()
	{
		DateTimeOffset now = DateTimeOffset.Now;
		foreach (ILogSource item in ButterLibSubModule.ServiceProvider?.GetService<IEnumerable<ILogSource>>() ?? Array.Empty<ILogSource>())
		{
			if (!(item is IFileLogSource fileLogSource))
			{
				continue;
			}
			fileLogSource.Deconstruct(out string name, out string path, out IFlushableFileSink[] sinks);
			if (!File.Exists(path))
			{
				continue;
			}
			string name2 = Path.GetFullPath(path).Replace(Path.DirectorySeparatorChar, ':') + ".serilog";
			Mutex mutex = new Mutex(initiallyOwned: false, name2);
			IFlushableFileSink[] array = sinks;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].FlushToDisk();
			}
			ImmutableArray<LogEntry>.Builder builder = ImmutableArray.CreateBuilder<LogEntry>();
			try
			{
				if (!mutex.WaitOne(TimeSpan.FromSeconds(5.0)))
				{
					continue;
				}
				using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				{
					ReverseTextReader reverseTextReader = new ReverseTextReader(stream);
					for (int j = 0; j < 2000; j++)
					{
						string text = reverseTextReader.ReadLine();
						if (text != null)
						{
							int num = text.IndexOf('[') + 1;
							int num2 = text.IndexOf(']');
							if (num != -1 && num2 != -1 && DateTimeOffset.TryParse(text.Substring(num, num2 - num), DateTimeFormatInfo.InvariantInfo, DateTimeStyles.RoundtripKind, out var result) && !(result - now > TimeSpan.FromMinutes(60.0)))
							{
								int num3 = text.IndexOf('[', num2 + 1) + 1;
								int num4 = text.IndexOf(']', num2 + 1);
								if (num3 != -1 && num4 != -1)
								{
									int num5 = text.IndexOf('[', num4 + 1) + 1;
									int num6 = text.IndexOf(']', num4 + 1);
									if (num5 != -1 && num6 != -1)
									{
										ImmutableArray<LogEntry>.Builder builder2 = builder;
										LogEntry logEntry = new LogEntry();
										logEntry.Date = result;
										logEntry.Type = text.Substring(num3, num4 - num3);
										LogEntry logEntry2 = logEntry;
										logEntry2.Level = text.Substring(num5, num6 - num5) switch
										{
											"FTL" => LogLevel.Fatal, 
											"ERR" => LogLevel.Error, 
											"WRN" => LogLevel.Warning, 
											"INF" => LogLevel.Information, 
											"DBG" => LogLevel.Debug, 
											"VRB" => LogLevel.Verbose, 
											_ => LogLevel.None, 
										};
										logEntry.Message = text.Substring(num6 + 3);
										builder2.Add(logEntry);
										continue;
									}
									break;
								}
								break;
							}
							break;
						}
						break;
					}
				}
				goto IL_02fd;
			}
			catch (Exception)
			{
				goto IL_02fd;
			}
			finally
			{
				mutex.ReleaseMutex();
			}
			IL_02fd:
			yield return new LogSource
			{
				Name = name,
				Logs = builder.ToArray(),
				AdditionalMetadata = Array.Empty<MetadataModel>()
			};
		}
	}
}
