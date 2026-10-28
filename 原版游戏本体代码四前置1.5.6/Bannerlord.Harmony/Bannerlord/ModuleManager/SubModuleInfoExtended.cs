using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml;

namespace Bannerlord.ModuleManager;

internal record SubModuleInfoExtended
{
	public string Name { get; set; } = string.Empty;

	public string DLLName { get; set; } = string.Empty;

	public IReadOnlyList<string> Assemblies { get; set; } = Array.Empty<string>();

	public string SubModuleClassType { get; set; } = string.Empty;

	public IReadOnlyDictionary<string, IReadOnlyList<string>> Tags { get; set; } = new Dictionary<string, IReadOnlyList<string>>();

	public SubModuleInfoExtended()
	{
	}

	public SubModuleInfoExtended(string name, string dllName, IReadOnlyList<string> assemblies, string subModuleClassType, IReadOnlyDictionary<string, IReadOnlyList<string>> tags)
	{
		Name = name;
		DLLName = dllName;
		Assemblies = assemblies;
		SubModuleClassType = subModuleClassType;
		Tags = tags;
	}

	public static SubModuleInfoExtended? FromXml(XmlNode? subModuleNode)
	{
		if (subModuleNode == null)
		{
			return null;
		}
		string name = subModuleNode.SelectSingleNode("Name")?.Attributes?["value"]?.InnerText ?? string.Empty;
		string dLLName = subModuleNode.SelectSingleNode("DLLName")?.Attributes?["value"]?.InnerText ?? string.Empty;
		string subModuleClassType = subModuleNode.SelectSingleNode("SubModuleClassType")?.Attributes?["value"]?.InnerText ?? string.Empty;
		string[] array = Array.Empty<string>();
		if (subModuleNode.SelectSingleNode("Assemblies") != null)
		{
			XmlNodeList xmlNodeList = subModuleNode.SelectSingleNode("Assemblies")?.SelectNodes("Assembly");
			array = new string[xmlNodeList?.Count ?? 0];
			for (int i = 0; i < xmlNodeList?.Count; i++)
			{
				string[] array2 = array;
				int num = i;
				string text = xmlNodeList?[i]?.Attributes?["value"]?.InnerText;
				array2[num] = ((text != null) ? text : string.Empty);
			}
		}
		XmlNodeList xmlNodeList2 = subModuleNode.SelectSingleNode("Tags")?.SelectNodes("Tag");
		Dictionary<string, List<string>> dictionary = new Dictionary<string, List<string>>();
		for (int j = 0; j < xmlNodeList2?.Count; j++)
		{
			string text2 = xmlNodeList2?[j]?.Attributes?["key"]?.InnerText;
			if (text2 == null)
			{
				continue;
			}
			string text3 = xmlNodeList2[j]?.Attributes?["value"]?.InnerText;
			if (text3 != null)
			{
				if (dictionary.TryGetValue(text2, out var value))
				{
					value.Add(text3);
					continue;
				}
				dictionary[text2] = new List<string> { text3 };
			}
		}
		return new SubModuleInfoExtended
		{
			Name = name,
			DLLName = dLLName,
			Assemblies = array,
			SubModuleClassType = subModuleClassType,
			Tags = new ReadOnlyDictionary<string, IReadOnlyList<string>>(((IEnumerable<KeyValuePair<string, List<string>>>)dictionary).ToDictionary((Func<KeyValuePair<string, List<string>>, string>)((KeyValuePair<string, List<string>> x) => x.Key), (Func<KeyValuePair<string, List<string>>, IReadOnlyList<string>>)((KeyValuePair<string, List<string>> x) => new ReadOnlyCollection<string>(x.Value))))
		};
	}

	public override string ToString()
	{
		return Name + " - " + DLLName;
	}

	public virtual bool Equals(SubModuleInfoExtended? other)
	{
		if ((object)other == null)
		{
			return false;
		}
		if ((object)this == other)
		{
			return true;
		}
		return Name == other.Name;
	}

	public override int GetHashCode()
	{
		return Name.GetHashCode();
	}
}
