using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BUTR.CrashReport.Models;
using Newtonsoft.Json;

namespace Bannerlord.ButterLib.CrashUploader;

internal class BUTRCrashUploader : ICrashUploader
{
	private sealed record CrashReportUploadBody(CrashReportModel CrashReport, IEnumerable<LogSource> LogSources);

	public async Task<CrashUploaderResult> UploadAsync(CrashReportModel crashReportModel, IEnumerable<LogSource> logSources)
	{
		_ = 3;
		try
		{
			AssemblyMetadataAttribute assemblyMetadataAttribute = typeof(BUTRCrashUploader).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault((AssemblyMetadataAttribute a) => a.Key == "BUTRUploadUrl");
			if (assemblyMetadataAttribute == null)
			{
				return CrashUploaderResult.MetadataNotFound();
			}
			byte[] data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new CrashReportUploadBody(crashReportModel, logSources)));
			HttpWebRequest httpWebRequest = WebRequest.CreateHttp(assemblyMetadataAttribute.Value);
			httpWebRequest.Method = "POST";
			httpWebRequest.ContentType = "application/json";
			httpWebRequest.UserAgent = $"ButterLib v{typeof(BUTRCrashUploader).Assembly.GetName().Version}";
			httpWebRequest.Headers.Add("Content-Encoding", "gzip,deflate");
			httpWebRequest.Headers.Add("Tenant", "1");
			httpWebRequest.Headers.Add("CrashReportVersion", crashReportModel.Version.ToString());
			using Stream writeStream = await httpWebRequest.GetRequestStreamAsync().ConfigureAwait(continueOnCapturedContext: false);
			using (GZipStream gzip = new GZipStream(writeStream, CompressionMode.Compress, leaveOpen: true))
			{
				await gzip.WriteAsync(data, 0, data.Length);
			}
			using WebResponse response = await httpWebRequest.GetResponseAsync().ConfigureAwait(continueOnCapturedContext: false);
			if (!(response is HttpWebResponse { StatusCode: var statusCode } httpWebResponse))
			{
				return CrashUploaderResult.ResponseIsNotHttpWebResponse();
			}
			if (statusCode != HttpStatusCode.OK && statusCode != HttpStatusCode.Created)
			{
				return CrashUploaderResult.WrongStatusCode(httpWebResponse.StatusCode.ToString());
			}
			using Stream stream = response.GetResponseStream();
			if (stream == null)
			{
				return CrashUploaderResult.ResponseStreamIsNull();
			}
			using StreamReader responseReader = new StreamReader(stream);
			string text = await responseReader.ReadLineAsync().ConfigureAwait(continueOnCapturedContext: false);
			if (string.IsNullOrEmpty(text))
			{
				return CrashUploaderResult.ResponseUrlIsNullOrEmpty();
			}
			return CrashUploaderResult.Success(text);
		}
		catch (Exception ex)
		{
			return CrashUploaderResult.FailedWithException(ex.ToString());
		}
	}
}
