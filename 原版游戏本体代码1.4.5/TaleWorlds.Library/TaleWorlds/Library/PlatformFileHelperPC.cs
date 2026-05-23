using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace TaleWorlds.Library;

public class PlatformFileHelperPC : IPlatformFileHelper
{
	private readonly string ApplicationName;

	private static string Error;

	private string DocumentsPath => Environment.GetFolderPath(Environment.SpecialFolder.Personal);

	private string ProgramDataPath => Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

	public PlatformFileHelperPC(string applicationName)
	{
		ApplicationName = applicationName;
	}

	public SaveResult SaveFile(PlatformFilePath path, byte[] data)
	{
		SaveResult saveResult = SaveResult.PlatformFileHelperFailure;
		Error = "";
		try
		{
			CreateDirectory(path.FolderPath);
			File.WriteAllBytes(GetFileFullPath(path), data);
			return SaveResult.Success;
		}
		catch (Exception ex)
		{
			Error = ex.Message;
			return SaveResult.PlatformFileHelperFailure;
		}
	}

	public SaveResult SaveFileString(PlatformFilePath path, string data)
	{
		SaveResult saveResult = SaveResult.PlatformFileHelperFailure;
		Error = "";
		try
		{
			CreateDirectory(path.FolderPath);
			File.WriteAllText(GetFileFullPath(path), data, Encoding.UTF8);
			return SaveResult.Success;
		}
		catch (Exception ex)
		{
			Error = ex.Message;
			return SaveResult.PlatformFileHelperFailure;
		}
	}

	public Task<SaveResult> SaveFileAsync(PlatformFilePath path, byte[] data)
	{
		return Task.FromResult(SaveFile(path, data));
	}

	public Task<SaveResult> SaveFileStringAsync(PlatformFilePath path, string data)
	{
		return Task.FromResult(SaveFileString(path, data));
	}

	public SaveResult AppendLineToFileString(PlatformFilePath path, string data)
	{
		SaveResult saveResult = SaveResult.PlatformFileHelperFailure;
		Error = "";
		try
		{
			CreateDirectory(path.FolderPath);
			File.AppendAllText(GetFileFullPath(path), "\n" + data, Encoding.UTF8);
			return SaveResult.Success;
		}
		catch (Exception ex)
		{
			Error = ex.Message;
			return SaveResult.PlatformFileHelperFailure;
		}
	}

	private string GetDirectoryFullPath(PlatformDirectoryPath directoryPath)
	{
		string path = "";
		switch (directoryPath.Type)
		{
		case PlatformFileType.User:
			path = Path.Combine(DocumentsPath, ApplicationName);
			break;
		case PlatformFileType.Application:
			path = Path.Combine(ProgramDataPath, ApplicationName);
			break;
		case PlatformFileType.Temporary:
			path = Path.Combine(DocumentsPath, ApplicationName, "Temp");
			break;
		}
		return Path.Combine(path, directoryPath.Path);
	}

	public string GetFileFullPath(PlatformFilePath filePath)
	{
		return Path.GetFullPath(Path.Combine(GetDirectoryFullPath(filePath.FolderPath), filePath.FileName));
	}

	public bool FileExists(PlatformFilePath path)
	{
		return File.Exists(GetFileFullPath(path));
	}

	public async Task<string> GetFileContentStringAsync(PlatformFilePath path)
	{
		if (!FileExists(path))
		{
			return null;
		}
		string fileFullPath = GetFileFullPath(path);
		string text = string.Empty;
		using (FileStream sourceStream = File.Open(fileFullPath, FileMode.Open))
		{
			byte[] buffer = new byte[sourceStream.Length];
			await sourceStream.ReadAsync(buffer, 0, (int)sourceStream.Length);
			text = Encoding.UTF8.GetString(buffer);
		}
		string text2 = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
		if (text.StartsWith(text2, StringComparison.Ordinal))
		{
			text = text.Remove(0, text2.Length);
		}
		return text;
	}

	public string GetFileContentString(PlatformFilePath path)
	{
		if (!FileExists(path))
		{
			return null;
		}
		string fileFullPath = GetFileFullPath(path);
		string result = null;
		Error = "";
		try
		{
			result = File.ReadAllText(fileFullPath, Encoding.UTF8);
		}
		catch (Exception ex)
		{
			Error = ex.Message;
			Debug.Print(Error);
		}
		return result;
	}

	public byte[] GetMetaDataContent(PlatformFilePath path)
	{
		if (!FileExists(path))
		{
			return null;
		}
		string fileFullPath = GetFileFullPath(path);
		try
		{
			using FileStream fileStream = new FileStream(fileFullPath, FileMode.Open, FileAccess.Read);
			using System.IO.BinaryReader binaryReader = new System.IO.BinaryReader(fileStream);
			int num = binaryReader.ReadInt32();
			if (num > fileStream.Length - fileStream.Position)
			{
				return null;
			}
			byte[] array = new byte[num + 4];
			BitConverter.GetBytes(num).CopyTo(array, 0);
			if (binaryReader.Read(array, 4, num) < num)
			{
				return null;
			}
			return array;
		}
		catch (Exception)
		{
		}
		return null;
	}

	public byte[] GetFileContent(PlatformFilePath path)
	{
		if (!FileExists(path))
		{
			return null;
		}
		string fileFullPath = GetFileFullPath(path);
		byte[] result = null;
		Error = "";
		try
		{
			result = File.ReadAllBytes(fileFullPath);
		}
		catch (Exception ex)
		{
			Error = ex.Message;
			Debug.Print(Error);
		}
		return result;
	}

	public bool DeleteFile(PlatformFilePath path)
	{
		string fileFullPath = GetFileFullPath(path);
		if (!FileExists(path))
		{
			return false;
		}
		try
		{
			File.Delete(fileFullPath);
			return true;
		}
		catch (Exception ex)
		{
			Error = ex.Message;
			Debug.Print(Error);
			return false;
		}
	}

	public void CreateDirectory(PlatformDirectoryPath path)
	{
		Directory.CreateDirectory(GetDirectoryFullPath(path));
	}

	public PlatformFilePath[] GetFiles(PlatformDirectoryPath path, string searchPattern, SearchOption searchOption)
	{
		string directoryFullPath = GetDirectoryFullPath(path);
		DirectoryInfo directoryInfo = new DirectoryInfo(directoryFullPath);
		PlatformFilePath[] array = null;
		Error = "";
		if (directoryInfo.Exists)
		{
			try
			{
				FileInfo[] files = directoryInfo.GetFiles(searchPattern, searchOption);
				array = new PlatformFilePath[files.Length];
				for (int i = 0; i < files.Length; i++)
				{
					FileInfo fileInfo = files[i];
					fileInfo.FullName.Substring(directoryFullPath.Length);
					PlatformFilePath platformFilePath = new PlatformFilePath(path, fileInfo.Name);
					array[i] = platformFilePath;
				}
			}
			catch (Exception ex)
			{
				Error = ex.Message;
			}
		}
		else
		{
			array = new PlatformFilePath[0];
		}
		return array;
	}

	public void RenameFile(PlatformFilePath filePath, string newName)
	{
		string fileFullPath = GetFileFullPath(filePath);
		string fileFullPath2 = GetFileFullPath(new PlatformFilePath(filePath.FolderPath, newName));
		File.Move(fileFullPath, fileFullPath2);
	}

	public string GetError()
	{
		return Error;
	}
}
