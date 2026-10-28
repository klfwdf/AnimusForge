using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using TaleWorlds.ModuleManager;

namespace TaleWorlds.MountAndBlade.DedicatedCustomServer.ClientHelper;

internal static class ModHelpers
{
	public static string RootPath => ModuleHelper.GetModuleFullPath("Multiplayer");

	public static string GetSceneObjRootPath()
	{
		string text = Path.Combine(RootPath, "SceneObj");
		if (!Directory.Exists(text))
		{
			ModLogger.Log("Multiplayer module didn't have 'SceneObj' directory; creating it now");
			Directory.CreateDirectory(text);
		}
		return text;
	}

	public static bool DoesSceneFolderAlreadyExist(string sceneName)
	{
		return Directory.Exists(Path.Combine(GetSceneObjRootPath(), sceneName));
	}

	public static string GetTempFilePath(string anyIdentifier)
	{
		return Path.Combine(Path.GetTempPath(), "BL_" + anyIdentifier + "_" + Guid.NewGuid().ToString());
	}

	public static string ReadSceneNameOfDirectory(string sceneDirectoryPath)
	{
		string text = null;
		using (StreamReader input = new StreamReader(Path.Combine(sceneDirectoryPath, "scene.xscene")))
		{
			using (XmlReader xmlReader = XmlReader.Create(input))
			{
				if (xmlReader.MoveToContent() == XmlNodeType.Element && xmlReader.Name == "scene")
				{
					text = xmlReader.GetAttribute("name");
				}
			}
			if (text == null)
			{
				throw new Exception("Couldn't retrieve name from 'scene.xscene'");
			}
			if (DedicatedCustomServerClientHelperSubModule.DebugMode)
			{
				text = text + "__" + Guid.NewGuid().ToString();
			}
		}
		return text;
	}

	public static string WriteBufferToTempFile(byte[] buffer)
	{
		string tempFilePath = GetTempFilePath("map_dl");
		using FileStream fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write);
		fileStream.Write(buffer, 0, buffer.Length);
		ModLogger.Log("Wrote buffer to temp file");
		return tempFilePath;
	}

	public static FileStream GetTempFileStream()
	{
		return new FileStream(GetTempFilePath("map_dl"), FileMode.Create, FileAccess.Write);
	}

	public static string ExtractZipToTempDirectory(string sourceZipFilePath)
	{
		DirectoryInfo directoryInfo = Directory.CreateDirectory(Path.Combine(GetSceneObjRootPath(), "temp_" + Guid.NewGuid().ToString()));
		using (ZipArchive source = ZipFile.OpenRead(sourceZipFilePath))
		{
			source.ExtractToDirectory(directoryInfo.FullName);
		}
		ModLogger.Log("Extracted zip to directory '" + directoryInfo.FullName + "'");
		return directoryInfo.FullName;
	}

	public static async Task<string> DownloadToTempFile(HttpClient httpClient, string url, IProgress<ProgressUpdate> progress = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		string tempFilePath;
		using (FileStream tempFileStream = GetTempFileStream())
		{
			tempFilePath = tempFileStream.Name;
			using HttpResponseMessage response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
			if (!response.IsSuccessStatusCode)
			{
				string arg = await response.Content.ReadAsStringAsync();
				throw new Exception($"Server responded with {response.StatusCode}: '{arg}'");
			}
			long? contentLength = response.Content.Headers.ContentLength;
			using Stream downloadStream = await response.Content.ReadAsStreamAsync();
			if (progress == null || !contentLength.HasValue)
			{
				await downloadStream.CopyToAsync(tempFileStream);
			}
			else
			{
				byte[] buffer = new byte[81920];
				long totalBytesRead = 0L;
				while (true)
				{
					int num;
					int bytesRead = (num = await downloadStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken));
					if (num == 0)
					{
						break;
					}
					await tempFileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
					totalBytesRead += bytesRead;
					progress.Report(new ProgressUpdate(totalBytesRead, contentLength.Value));
				}
			}
		}
		return tempFilePath;
	}
}
