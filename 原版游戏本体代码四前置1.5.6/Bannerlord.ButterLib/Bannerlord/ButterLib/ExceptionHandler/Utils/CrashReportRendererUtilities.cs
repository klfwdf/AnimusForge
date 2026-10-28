using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using BUTR.CrashReport.Models;
using BUTR.CrashReport.Renderer.Html;
using BUTR.CrashReport.Renderer.ImGui;
using BUTR.CrashReport.Renderer.WinForms;
using Bannerlord.BUTR.Shared.Helpers;
using Bannerlord.ButterLib.CrashUploader;
using Microsoft.Extensions.DependencyInjection;
using TaleWorlds.Library;

namespace Bannerlord.ButterLib.ExceptionHandler.Utils;

internal sealed class CrashReportRendererUtilities : global::BUTR.CrashReport.Renderer.ImGui.ICrashReportRendererUtilities, global::BUTR.CrashReport.Renderer.WinForms.ICrashReportRendererUtilities
{
	private static async Task<bool> SetClipboardTextAsync(string text)
	{
		TaskCompletionSource<bool> completionSource = new TaskCompletionSource<bool>();
		Thread thread = new Thread((ThreadStart)delegate
		{
			try
			{
				DataObject dataObject = new DataObject();
				dataObject.SetText(text, TextDataFormat.Text);
				Clipboard.SetDataObject(dataObject, copy: true, 10, 100);
				completionSource.SetResult(result: true);
			}
			catch (Exception)
			{
				completionSource.SetResult(result: false);
			}
		});
		thread.SetApartmentState(ApartmentState.STA);
		thread.Start();
		return await completionSource.Task;
	}

	private static async Task<(bool, string)> UploadInternal(CrashReportModel crashReport, ICollection<LogSource> logSources)
	{
		ICrashUploader crashUploader = ButterLibSubModule.ServiceProvider?.GetService<ICrashUploader>();
		if (crashUploader == null)
		{
			return (false, "Critical Error: Failed to get the crash uploader!");
		}
		CrashUploaderResult crashUploaderResult = await crashUploader.UploadAsync(crashReport, logSources).ConfigureAwait(continueOnCapturedContext: false);
		return crashUploaderResult.Status switch
		{
			CrashUploaderStatus.Success => (true, crashUploaderResult.Url ?? string.Empty), 
			CrashUploaderStatus.MetadataNotFound => (false, $"Status: {crashUploaderResult.Status}"), 
			CrashUploaderStatus.ResponseIsNotHttpWebResponse => (false, $"Status: {crashUploaderResult.Status}"), 
			CrashUploaderStatus.ResponseStreamIsNull => (false, $"Status: {crashUploaderResult.Status}"), 
			CrashUploaderStatus.WrongStatusCode => (false, $"Status: {crashUploaderResult.Status}\nStatusCode: {crashUploaderResult.StatusCode}"), 
			CrashUploaderStatus.FailedWithException => (false, $"Status: {crashUploaderResult.Status}\nException: {crashUploaderResult.Exception}"), 
			_ => (false, "Unknown error"), 
		};
	}

	public IEnumerable<string> GetNativeLibrariesFolderPath()
	{
		string modulePath = ModuleInfoHelper.GetModulePath(typeof(CrashReportRendererUtilities));
		yield return Path.Combine(modulePath, "bin", TaleWorlds.Library.Common.ConfigName);
	}

	public void Upload(CrashReportModel crashReport, ICollection<LogSource> logSources)
	{
		try
		{
			Task.Run(async delegate
			{
				(bool, string) result = await UploadInternal(crashReport, logSources).ConfigureAwait(continueOnCapturedContext: false);
				if (result.Item1)
				{
					await SetClipboardTextAsync(result.Item2).ConfigureAwait(continueOnCapturedContext: false);
					MessageBox.Show("Report available at\n" + result.Item2 + "\nThe url was copied to the clipboard!", "Success!");
				}
				else
				{
					MessageBox.Show("The crash uploader could not upload the report!\nPlease report this to the mod developers!\n" + result.Item2, "Error!");
				}
			}).ConfigureAwait(continueOnCapturedContext: false).GetAwaiter()
				.GetResult();
		}
		catch (Exception value)
		{
			Console.WriteLine(value);
			throw;
		}
	}

	string global::BUTR.CrashReport.Renderer.WinForms.ICrashReportRendererUtilities.CopyAsHtml(CrashReportModel crashReport, ICollection<LogSource> logSources)
	{
		return CrashReportHtml.Build(crashReport, logSources);
	}

	void global::BUTR.CrashReport.Renderer.ImGui.ICrashReportRendererUtilities.CopyAsHtml(CrashReportModel crashReport, ICollection<LogSource> logSources)
	{
		Task.Run(async delegate
		{
			if (!(await SetClipboardTextAsync(CrashReportHtml.Build(crashReport, logSources)).ConfigureAwait(continueOnCapturedContext: false)))
			{
				MessageBox.Show("Failed to copy the HTML content to the clipboard!", "Error!");
			}
		}).ConfigureAwait(continueOnCapturedContext: false).GetAwaiter()
			.GetResult();
	}

	public void SaveCrashReportAsHtml(CrashReportModel crashReport, ICollection<LogSource> logSources, bool addMiniDump, bool addLatestSave, bool addScreenshots)
	{
		string html = CrashReportHtml.Build(crashReport, logSources);
		using SaveFileDialog saveFileDialog = new SaveFileDialog();
		saveFileDialog.Filter = "HTML files|*.html|All files (*.*)|*.*";
		saveFileDialog.RestoreDirectory = true;
		saveFileDialog.AddExtension = true;
		saveFileDialog.CheckPathExists = true;
		saveFileDialog.ValidateNames = true;
		saveFileDialog.FileName = "crashreport";
		saveFileDialog.CreatePrompt = true;
		saveFileDialog.OverwritePrompt = true;
		if (saveFileDialog.ShowDialog() != DialogResult.OK)
		{
			return;
		}
		Stream stream = saveFileDialog.OpenFile();
		if (stream != null)
		{
			using (Stream stream2 = stream)
			{
				CreatorHtml.Create(crashReport, html, addMiniDump, addLatestSave, addScreenshots, stream2);
				return;
			}
		}
	}

	public void SaveCrashReportAsZip(CrashReportModel crashReport, ICollection<LogSource> logSources)
	{
		using SaveFileDialog saveFileDialog = new SaveFileDialog();
		saveFileDialog.Filter = "ZIP files|*.zip|All files (*.*)|*.*";
		saveFileDialog.RestoreDirectory = true;
		saveFileDialog.AddExtension = true;
		saveFileDialog.CheckPathExists = true;
		saveFileDialog.ValidateNames = true;
		saveFileDialog.FileName = "crashreport";
		saveFileDialog.CreatePrompt = true;
		saveFileDialog.OverwritePrompt = true;
		if (saveFileDialog.ShowDialog() != DialogResult.OK)
		{
			return;
		}
		Stream stream = saveFileDialog.OpenFile();
		if (stream != null)
		{
			using (Stream stream2 = stream)
			{
				CreatorZip.Create(crashReport, logSources, stream2);
				return;
			}
		}
	}
}
