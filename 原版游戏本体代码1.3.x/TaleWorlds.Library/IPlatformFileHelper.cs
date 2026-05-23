using System;
using System.IO;
using System.Threading.Tasks;

namespace TaleWorlds.Library
{
	// Token: 0x02000042 RID: 66
	public interface IPlatformFileHelper
	{
		// Token: 0x0600020E RID: 526
		SaveResult SaveFile(PlatformFilePath path, byte[] data);

		// Token: 0x0600020F RID: 527
		SaveResult SaveFileString(PlatformFilePath path, string data);

		// Token: 0x06000210 RID: 528
		SaveResult AppendLineToFileString(PlatformFilePath path, string data);

		// Token: 0x06000211 RID: 529
		Task<SaveResult> SaveFileAsync(PlatformFilePath path, byte[] data);

		// Token: 0x06000212 RID: 530
		Task<SaveResult> SaveFileStringAsync(PlatformFilePath path, string data);

		// Token: 0x06000213 RID: 531
		bool FileExists(PlatformFilePath path);

		// Token: 0x06000214 RID: 532
		Task<string> GetFileContentStringAsync(PlatformFilePath path);

		// Token: 0x06000215 RID: 533
		string GetFileContentString(PlatformFilePath path);

		// Token: 0x06000216 RID: 534
		byte[] GetFileContent(PlatformFilePath filePath);

		// Token: 0x06000217 RID: 535
		byte[] GetMetaDataContent(PlatformFilePath filePath);

		// Token: 0x06000218 RID: 536
		bool DeleteFile(PlatformFilePath path);

		// Token: 0x06000219 RID: 537
		PlatformFilePath[] GetFiles(PlatformDirectoryPath path, string searchPattern, SearchOption searchOption);

		// Token: 0x0600021A RID: 538
		string GetFileFullPath(PlatformFilePath filePath);

		// Token: 0x0600021B RID: 539
		string GetError();
	}
}
