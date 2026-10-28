using System;
using System.Xml.Serialization;

namespace MCM.Abstractions.Xml;

[Serializable]
[XmlType("PropertyDropdown")]
public class PropertyDropdownXmlModel : PropertyBaseXmlModel, IPropertyDefinitionDropdown, IPropertyDefinitionBase
{
	[XmlAttribute("SelectedIndex")]
	public int SelectedIndex { get; set; }

	[XmlArray("Values")]
	public string[] Values { get; set; } = Array.Empty<string>();
}
