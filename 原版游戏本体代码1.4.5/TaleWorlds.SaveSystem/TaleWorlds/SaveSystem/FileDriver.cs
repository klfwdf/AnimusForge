using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using TaleWorlds.Library;

namespace TaleWorlds.SaveSystem;

public class FileDriver : ISaveDriver
{
	public const string SaveDirectoryName = "Game Saves";

	public static PlatformDirectoryPath SavePath
	{
		get
		{
			string path = "Game Saves\\";
			return new PlatformDirectoryPath(PlatformFileType.User, path);
		}
	}

	public static PlatformFilePath GetSaveFilePath(string fileName)
	{
		return new PlatformFilePath(SavePath, fileName);
	}

	public Task<SaveResultWithMessage> Save(string saveName, int version, MetaData metaData, GameData gameData)
	{
		SaveResult result = SaveResult.FileDriverFailure;
		PlatformFilePath saveFilePath = GetSaveFilePath(saveName + ".sav");
		MemoryStream memoryStream = new MemoryStream();
		metaData.Add("Version", version.ToString());
		metaData.Serialize(memoryStream);
		using (DeflateStream output = new DeflateStream(memoryStream, CompressionLevel.Fastest, leaveOpen: true))
		{
			using System.IO.BinaryWriter writer = new System.IO.BinaryWriter(output);
			GameData.Write(writer, gameData);
		}
		if (memoryStream.TryGetBuffer(out var buffer))
		{
			byte[] array = buffer.Array;
			Array.Resize(ref array, buffer.Count);
			result = FileHelper.SaveFile(saveFilePath, array);
		}
		memoryStream.Close();
		string error = Common.PlatformFileHelper.GetError();
		return Task.FromResult(new SaveResultWithMessage(result, error));
	}

	public MetaData LoadMetaData(string saveName)
	{
		byte[] metaDataContent = FileHelper.GetMetaDataContent(GetSaveFilePath(saveName + ".sav"));
		if (metaDataContent != null)
		{
			return MetaData.Deserialize(new MemoryStream(metaDataContent));
		}
		Debug.Print("[Load meta data error]: " + saveName);
		return null;
	}

	public LoadData Load(string saveName)
	{
		byte[] fileContent = FileHelper.GetFileContent(GetSaveFilePath(saveName + ".sav"));
		if (fileContent != null)
		{
			MemoryStream stream = new MemoryStream(fileContent);
			MetaData metaData = MetaData.Deserialize(stream);
			using DeflateStream input = new DeflateStream(stream, CompressionMode.Decompress);
			try
			{
				GameData gameData;
				if (GetApplicationVersionOfMetaData(metaData) < ApplicationVersion.FromString("v1.1.0"))
				{
					gameData = LegacyGameDataDeserializer.Deserialize(stream);
					return new LoadData(metaData, gameData);
				}
				using (System.IO.BinaryReader reader = new System.IO.BinaryReader(input))
				{
					gameData = GameData.Read(reader);
				}
				return new LoadData(metaData, gameData);
			}
			catch (Exception ex)
			{
				Debug.Print(ex.ToString());
				return null;
			}
		}
		Debug.Print("[Load error]: " + saveName);
		return null;
	}

	public SaveGameFileInfo[] GetSaveGameFileInfos()
	{
		PlatformFilePath[] files = FileHelper.GetFiles(SavePath, "*.sav", SearchOption.TopDirectoryOnly);
		List<SaveGameFileInfo> list = new List<SaveGameFileInfo>((files != null) ? files.Length : 0);
		if (files != null)
		{
			foreach (PlatformFilePath platformFilePath in files)
			{
				string fileNameWithoutExtension = platformFilePath.GetFileNameWithoutExtension();
				MetaData metaData = SaveManager.LoadMetaData(fileNameWithoutExtension, this);
				SaveGameFileInfo saveGameFileInfo = new SaveGameFileInfo();
				saveGameFileInfo.Name = fileNameWithoutExtension;
				saveGameFileInfo.MetaData = metaData;
				saveGameFileInfo.IsCorrupted = metaData == null || GetApplicationVersionOfMetaData(metaData) == ApplicationVersion.Empty;
				list.Add(saveGameFileInfo);
			}
		}
		return list.ToArray();
	}

	private ApplicationVersion GetApplicationVersionOfMetaData(MetaData metaData)
	{
		string text = metaData?["ApplicationVersion"];
		if (text == null)
		{
			return ApplicationVersion.Empty;
		}
		return ApplicationVersion.FromString(text);
	}

	public string[] GetSaveGameFileNames()
	{
		List<string> list = new List<string>();
		PlatformFilePath[] files = FileHelper.GetFiles(SavePath, "*.sav", SearchOption.TopDirectoryOnly);
		if (files != null)
		{
			foreach (PlatformFilePath platformFilePath in files)
			{
				string fileNameWithoutExtension = platformFilePath.GetFileNameWithoutExtension();
				list.Add(fileNameWithoutExtension);
			}
		}
		return list.ToArray();
	}

	public bool Delete(string saveName)
	{
		PlatformFilePath saveFilePath = GetSaveFilePath(saveName + ".sav");
		if (FileHelper.FileExists(saveFilePath))
		{
			FileHelper.DeleteFile(saveFilePath);
			return true;
		}
		return false;
	}

	public bool IsSaveGameFileExists(string saveName)
	{
		return FileHelper.FileExists(GetSaveFilePath(saveName + ".sav"));
	}

	public bool IsWorkingAsync()
	{
		return false;
	}
}
