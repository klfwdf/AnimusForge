using System.IO;
using System.Xml;
using Newtonsoft.Json;

namespace MCM.Abstractions.Utils;

internal static class SerializationUtils
{
	public static T? DeserializeXml<T>(Stream xmlStream)
	{
		using StreamReader streamReader = new StreamReader(xmlStream);
		string xml = streamReader.ReadToEnd();
		XmlDocument xmlDocument = new XmlDocument();
		xmlDocument.LoadXml(xml);
		string text = JsonConvert.SerializeXmlNode(xmlDocument);
		return (T?)((text != null) ? ((object)JsonConvert.DeserializeObject<T>(text)) : ((object)default(T)));
	}
}
