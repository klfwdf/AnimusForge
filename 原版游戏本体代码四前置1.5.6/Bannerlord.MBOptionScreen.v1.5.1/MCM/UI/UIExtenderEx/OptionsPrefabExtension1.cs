using System.Xml;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;

namespace MCM.UI.UIExtenderEx;

[PrefabExtension("Options", "descendant::ListPanel[@Id='TabToggleList']/Children/OptionsTabToggle[5]", "Options")]
internal sealed class OptionsPrefabExtension1 : PrefabExtensionInsertPatch
{
	private readonly XmlDocument _xmlDocument = new XmlDocument();

	public override InsertType Type => InsertType.Prepend;

	public OptionsPrefabExtension1()
	{
		_xmlDocument.LoadXml("<OptionsTabToggle DataSource=\"{ModOptions}\" PositionYOffset=\"2\" Parameter.ButtonBrush=\"Header.Tab.Center\" Parameter.TabName=\"ModOptionsPage\" />");
	}

	[PrefabExtensionXmlNode(false)]
	public XmlNode GetPrefabExtension()
	{
		return _xmlDocument;
	}
}
