using System;
using System.Threading.Tasks;
using System.Xml;

namespace TaleWorlds.Library
{
	// Token: 0x02000032 RID: 50
	public static class FileHelperExtensions
	{
		// Token: 0x060001B2 RID: 434 RVA: 0x00006DF4 File Offset: 0x00004FF4
		public static void Load(this XmlDocument document, PlatformFilePath path)
		{
			string fileContentString = FileHelper.GetFileContentString(path);
			if (!string.IsNullOrEmpty(fileContentString))
			{
				document.LoadXml(fileContentString);
			}
		}

		// Token: 0x060001B3 RID: 435 RVA: 0x00006E18 File Offset: 0x00005018
		public static async Task LoadAsync(this XmlDocument document, PlatformFilePath path)
		{
			string text = await FileHelper.GetFileContentStringAsync(path);
			if (!string.IsNullOrEmpty(text))
			{
				document.LoadXml(text);
			}
		}

		// Token: 0x060001B4 RID: 436 RVA: 0x00006E68 File Offset: 0x00005068
		public static void Save(this XmlDocument document, PlatformFilePath path)
		{
			string outerXml = document.OuterXml;
			FileHelper.SaveFileString(path, outerXml);
		}

		// Token: 0x060001B5 RID: 437 RVA: 0x00006E84 File Offset: 0x00005084
		public static async Task SaveAsync(this XmlDocument document, PlatformFilePath path)
		{
			string outerXml = document.OuterXml;
			await FileHelper.SaveFileStringAsync(path, outerXml);
		}
	}
}
