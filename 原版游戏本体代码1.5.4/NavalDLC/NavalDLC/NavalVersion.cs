using System.Xml;
using TaleWorlds.Library;

namespace NavalDLC;

public class NavalVersion
{
	public static string GetApplicationVersionBuildNumber()
	{
		XmlDocument xmlDocument = new XmlDocument();
		string fileContent = TaleWorlds.Library.VirtualFolders.GetFileContent("__MODULE_NAME__NavalDLC__MODULE_NAME__/Parameters/Version.xml", typeof(VirtualFolders));
		if (fileContent == "")
		{
			return "";
		}
		xmlDocument.LoadXml(fileContent);
		return xmlDocument.ChildNodes[0].ChildNodes[0].Attributes["Value"].InnerText;
	}
}
