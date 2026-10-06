using System.Threading.Tasks;
using System.Xml;

namespace TaleWorlds.Library;

public static class FileHelperExtensions
{
	public static void Load(this XmlDocument document, PlatformFilePath path)
	{
		string fileContentString = FileHelper.GetFileContentString(path);
		if (!string.IsNullOrEmpty(fileContentString))
		{
			document.LoadXml(fileContentString);
		}
	}

	public static async Task LoadAsync(this XmlDocument document, PlatformFilePath path)
	{
		string text = await FileHelper.GetFileContentStringAsync(path);
		if (!string.IsNullOrEmpty(text))
		{
			document.LoadXml(text);
		}
	}

	public static void Save(this XmlDocument document, PlatformFilePath path)
	{
		string outerXml = document.OuterXml;
		FileHelper.SaveFileString(path, outerXml);
	}

	public static async Task SaveAsync(this XmlDocument document, PlatformFilePath path)
	{
		string outerXml = document.OuterXml;
		await FileHelper.SaveFileStringAsync(path, outerXml);
	}
}
