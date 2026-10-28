using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using BUTR.CrashReport.Models;
using BUTR.CrashReport.Renderer.Html;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Bannerlord.ButterLib.ExceptionHandler.Utils;

internal static class CreatorHtml
{
	private static string GetCompressedMiniDump()
	{
		try
		{
			using Stream stream = CreatorShared.GetMiniDump();
			if (stream == Stream.Null)
			{
				return string.Empty;
			}
			using MemoryStream memoryStream = new MemoryStream();
			using GZipStream destination = new GZipStream(memoryStream, CompressionMode.Compress, leaveOpen: true);
			stream.Seek(0L, SeekOrigin.Begin);
			stream.CopyTo(destination);
			return Convert.ToBase64String(memoryStream.ToArray());
		}
		catch (Exception)
		{
			return string.Empty;
		}
	}

	private static string GetCompressedSaveFile()
	{
		try
		{
			using Stream stream = CreatorShared.GetSaveFile();
			if (stream == Stream.Null)
			{
				return string.Empty;
			}
			using MemoryStream memoryStream = new MemoryStream();
			using GZipStream destination = new GZipStream(memoryStream, CompressionMode.Compress, leaveOpen: true);
			stream.Seek(0L, SeekOrigin.Begin);
			stream.CopyTo(destination);
			return Convert.ToBase64String(memoryStream.ToArray());
		}
		catch (Exception)
		{
			return string.Empty;
		}
	}

	private static string GetScreenshot()
	{
		try
		{
			using Stream stream = CreatorShared.GetScreenshot();
			if (stream == Stream.Null)
			{
				return string.Empty;
			}
			if (stream is MemoryStream memoryStream)
			{
				return Convert.ToBase64String(memoryStream.ToArray());
			}
			using MemoryStream memoryStream2 = new MemoryStream();
			stream.Seek(0L, SeekOrigin.Begin);
			stream.CopyTo(memoryStream2);
			return Convert.ToBase64String(memoryStream2.ToArray());
		}
		catch (Exception)
		{
			return string.Empty;
		}
	}

	private static string CompressJson(string jsonModel)
	{
		using MemoryStream memoryStream = new MemoryStream();
		using (CryptoStream stream = new CryptoStream(memoryStream, new ToBase64Transform(), CryptoStreamMode.Write, leaveOpen: true))
		{
			using GZipStream stream2 = new GZipStream(stream, CompressionLevel.Optimal, leaveOpen: true);
			using StreamWriter streamWriter = new StreamWriter(stream2, Encoding.UTF8, 1024, leaveOpen: true);
			streamWriter.Write(jsonModel);
		}
		using StreamReader streamReader = new StreamReader(memoryStream);
		memoryStream.Seek(0L, SeekOrigin.Begin);
		return streamReader.ReadToEnd();
	}

	public static void Create(CrashReportModel crashReport, string html, bool includeMiniDump, bool includeSaveFile, bool includeScreenshot, Stream stream)
	{
		string jsonModel = JsonConvert.SerializeObject(crashReport, new JsonSerializerSettings
		{
			Formatting = Formatting.None,
			StringEscapeHandling = StringEscapeHandling.EscapeHtml,
			DefaultValueHandling = DefaultValueHandling.Include,
			NullValueHandling = NullValueHandling.Include,
			Converters = { (JsonConverter)new StringEnumConverter() }
		});
		string value = CrashReportHtml.AddData(html, CompressJson(jsonModel), includeMiniDump ? GetCompressedMiniDump() : null, includeSaveFile ? GetCompressedSaveFile() : null, includeScreenshot ? GetScreenshot() : null);
		using StreamWriter streamWriter = new StreamWriter(stream);
		streamWriter.Write(value);
	}
}
