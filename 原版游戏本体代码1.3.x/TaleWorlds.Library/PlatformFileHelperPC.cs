using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace TaleWorlds.Library
{
	// Token: 0x0200007F RID: 127
	public class PlatformFileHelperPC : IPlatformFileHelper
	{
		// Token: 0x17000074 RID: 116
		// (get) Token: 0x0600046D RID: 1133 RVA: 0x0000FA2E File Offset: 0x0000DC2E
		private string DocumentsPath
		{
			get
			{
				return Environment.GetFolderPath(Environment.SpecialFolder.Personal);
			}
		}

		// Token: 0x17000075 RID: 117
		// (get) Token: 0x0600046E RID: 1134 RVA: 0x0000FA36 File Offset: 0x0000DC36
		private string ProgramDataPath
		{
			get
			{
				return Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
			}
		}

		// Token: 0x0600046F RID: 1135 RVA: 0x0000FA3F File Offset: 0x0000DC3F
		public PlatformFileHelperPC(string applicationName)
		{
			this.ApplicationName = applicationName;
		}

		// Token: 0x06000470 RID: 1136 RVA: 0x0000FA50 File Offset: 0x0000DC50
		public SaveResult SaveFile(PlatformFilePath path, byte[] data)
		{
			SaveResult result = SaveResult.PlatformFileHelperFailure;
			PlatformFileHelperPC.Error = "";
			try
			{
				this.CreateDirectory(path.FolderPath);
				File.WriteAllBytes(this.GetFileFullPath(path), data);
				result = SaveResult.Success;
			}
			catch (Exception ex)
			{
				PlatformFileHelperPC.Error = ex.Message;
				result = SaveResult.PlatformFileHelperFailure;
			}
			return result;
		}

		// Token: 0x06000471 RID: 1137 RVA: 0x0000FAA8 File Offset: 0x0000DCA8
		public SaveResult SaveFileString(PlatformFilePath path, string data)
		{
			SaveResult result = SaveResult.PlatformFileHelperFailure;
			PlatformFileHelperPC.Error = "";
			try
			{
				this.CreateDirectory(path.FolderPath);
				File.WriteAllText(this.GetFileFullPath(path), data, Encoding.UTF8);
				result = SaveResult.Success;
			}
			catch (Exception ex)
			{
				PlatformFileHelperPC.Error = ex.Message;
				result = SaveResult.PlatformFileHelperFailure;
			}
			return result;
		}

		// Token: 0x06000472 RID: 1138 RVA: 0x0000FB04 File Offset: 0x0000DD04
		public Task<SaveResult> SaveFileAsync(PlatformFilePath path, byte[] data)
		{
			return Task.FromResult<SaveResult>(this.SaveFile(path, data));
		}

		// Token: 0x06000473 RID: 1139 RVA: 0x0000FB13 File Offset: 0x0000DD13
		public Task<SaveResult> SaveFileStringAsync(PlatformFilePath path, string data)
		{
			return Task.FromResult<SaveResult>(this.SaveFileString(path, data));
		}

		// Token: 0x06000474 RID: 1140 RVA: 0x0000FB24 File Offset: 0x0000DD24
		public SaveResult AppendLineToFileString(PlatformFilePath path, string data)
		{
			SaveResult result = SaveResult.PlatformFileHelperFailure;
			PlatformFileHelperPC.Error = "";
			try
			{
				this.CreateDirectory(path.FolderPath);
				File.AppendAllText(this.GetFileFullPath(path), "\n" + data, Encoding.UTF8);
				result = SaveResult.Success;
			}
			catch (Exception ex)
			{
				PlatformFileHelperPC.Error = ex.Message;
				result = SaveResult.PlatformFileHelperFailure;
			}
			return result;
		}

		// Token: 0x06000475 RID: 1141 RVA: 0x0000FB88 File Offset: 0x0000DD88
		private string GetDirectoryFullPath(PlatformDirectoryPath directoryPath)
		{
			string path = "";
			switch (directoryPath.Type)
			{
			case PlatformFileType.User:
				path = Path.Combine(this.DocumentsPath, this.ApplicationName);
				break;
			case PlatformFileType.Application:
				path = Path.Combine(this.ProgramDataPath, this.ApplicationName);
				break;
			case PlatformFileType.Temporary:
				path = Path.Combine(this.DocumentsPath, this.ApplicationName, "Temp");
				break;
			}
			return Path.Combine(path, directoryPath.Path);
		}

		// Token: 0x06000476 RID: 1142 RVA: 0x0000FC01 File Offset: 0x0000DE01
		public string GetFileFullPath(PlatformFilePath filePath)
		{
			return Path.GetFullPath(Path.Combine(this.GetDirectoryFullPath(filePath.FolderPath), filePath.FileName));
		}

		// Token: 0x06000477 RID: 1143 RVA: 0x0000FC1F File Offset: 0x0000DE1F
		public bool FileExists(PlatformFilePath path)
		{
			return File.Exists(this.GetFileFullPath(path));
		}

		// Token: 0x06000478 RID: 1144 RVA: 0x0000FC30 File Offset: 0x0000DE30
		public async Task<string> GetFileContentStringAsync(PlatformFilePath path)
		{
			string result;
			if (!this.FileExists(path))
			{
				result = null;
			}
			else
			{
				string fileFullPath = this.GetFileFullPath(path);
				string text = string.Empty;
				using (FileStream sourceStream = File.Open(fileFullPath, FileMode.Open))
				{
					byte[] buffer = new byte[sourceStream.Length];
					await sourceStream.ReadAsync(buffer, 0, (int)sourceStream.Length);
					text = Encoding.UTF8.GetString(buffer);
					buffer = null;
				}
				FileStream sourceStream = null;
				string @string = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
				if (text.StartsWith(@string, StringComparison.Ordinal))
				{
					text = text.Remove(0, @string.Length);
				}
				result = text;
			}
			return result;
		}

		// Token: 0x06000479 RID: 1145 RVA: 0x0000FC80 File Offset: 0x0000DE80
		public string GetFileContentString(PlatformFilePath path)
		{
			if (!this.FileExists(path))
			{
				return null;
			}
			string fileFullPath = this.GetFileFullPath(path);
			string result = null;
			PlatformFileHelperPC.Error = "";
			try
			{
				result = File.ReadAllText(fileFullPath, Encoding.UTF8);
			}
			catch (Exception ex)
			{
				PlatformFileHelperPC.Error = ex.Message;
				Debug.Print(PlatformFileHelperPC.Error, 0, Debug.DebugColor.White, 17592186044416UL);
			}
			return result;
		}

		// Token: 0x0600047A RID: 1146 RVA: 0x0000FCF0 File Offset: 0x0000DEF0
		public byte[] GetMetaDataContent(PlatformFilePath path)
		{
			if (!this.FileExists(path))
			{
				return null;
			}
			string fileFullPath = this.GetFileFullPath(path);
			try
			{
				using (FileStream fileStream = new FileStream(fileFullPath, FileMode.Open, FileAccess.Read))
				{
					using (BinaryReader binaryReader = new BinaryReader(fileStream))
					{
						int num = binaryReader.ReadInt32();
						if ((long)num > fileStream.Length - fileStream.Position)
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
				}
			}
			catch (Exception)
			{
			}
			return null;
		}

		// Token: 0x0600047B RID: 1147 RVA: 0x0000FDB0 File Offset: 0x0000DFB0
		public byte[] GetFileContent(PlatformFilePath path)
		{
			if (!this.FileExists(path))
			{
				return null;
			}
			string fileFullPath = this.GetFileFullPath(path);
			byte[] result = null;
			PlatformFileHelperPC.Error = "";
			try
			{
				result = File.ReadAllBytes(fileFullPath);
			}
			catch (Exception ex)
			{
				PlatformFileHelperPC.Error = ex.Message;
				Debug.Print(PlatformFileHelperPC.Error, 0, Debug.DebugColor.White, 17592186044416UL);
			}
			return result;
		}

		// Token: 0x0600047C RID: 1148 RVA: 0x0000FE18 File Offset: 0x0000E018
		public bool DeleteFile(PlatformFilePath path)
		{
			string fileFullPath = this.GetFileFullPath(path);
			if (!this.FileExists(path))
			{
				return false;
			}
			bool result;
			try
			{
				File.Delete(fileFullPath);
				result = true;
			}
			catch (Exception ex)
			{
				PlatformFileHelperPC.Error = ex.Message;
				Debug.Print(PlatformFileHelperPC.Error, 0, Debug.DebugColor.White, 17592186044416UL);
				result = false;
			}
			return result;
		}

		// Token: 0x0600047D RID: 1149 RVA: 0x0000FE78 File Offset: 0x0000E078
		public void CreateDirectory(PlatformDirectoryPath path)
		{
			Directory.CreateDirectory(this.GetDirectoryFullPath(path));
		}

		// Token: 0x0600047E RID: 1150 RVA: 0x0000FE88 File Offset: 0x0000E088
		public PlatformFilePath[] GetFiles(PlatformDirectoryPath path, string searchPattern, SearchOption searchOption)
		{
			string directoryFullPath = this.GetDirectoryFullPath(path);
			DirectoryInfo directoryInfo = new DirectoryInfo(directoryFullPath);
			PlatformFilePath[] array = null;
			PlatformFileHelperPC.Error = "";
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
					return array;
				}
				catch (Exception ex)
				{
					PlatformFileHelperPC.Error = ex.Message;
					return array;
				}
			}
			array = new PlatformFilePath[0];
			return array;
		}

		// Token: 0x0600047F RID: 1151 RVA: 0x0000FF34 File Offset: 0x0000E134
		public void RenameFile(PlatformFilePath filePath, string newName)
		{
			string fileFullPath = this.GetFileFullPath(filePath);
			string fileFullPath2 = this.GetFileFullPath(new PlatformFilePath(filePath.FolderPath, newName));
			File.Move(fileFullPath, fileFullPath2);
		}

		// Token: 0x06000480 RID: 1152 RVA: 0x0000FF61 File Offset: 0x0000E161
		public string GetError()
		{
			return PlatformFileHelperPC.Error;
		}

		// Token: 0x04000168 RID: 360
		private readonly string ApplicationName;

		// Token: 0x04000169 RID: 361
		private static string Error;
	}
}
