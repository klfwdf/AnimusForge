using System.Xml;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;

namespace MCM.UI.UIExtenderEx;

[PrefabExtension("Options", "descendant::TabControl[@Id='TabControl']/Children/*[5]", "Options")]
internal sealed class OptionsPrefabExtension2 : PrefabExtensionInsertPatch
{
	private readonly XmlDocument _xmlDocument = new XmlDocument();

	public override InsertType Type => InsertType.Prepend;

	public OptionsPrefabExtension2()
	{
		_xmlDocument.LoadXml("<ModOptionsPageView Id=\"ModOptionsPage\" DataSource=\"{ModOptions}\" />");
	}

	[PrefabExtensionXmlDocument(false)]
	public XmlDocument GetPrefabExtension()
	{
		return _xmlDocument;
	}
}
