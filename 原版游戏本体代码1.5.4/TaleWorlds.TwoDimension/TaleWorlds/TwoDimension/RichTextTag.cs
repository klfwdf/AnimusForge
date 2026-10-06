using System.Collections.Generic;

namespace TaleWorlds.TwoDimension;

public class RichTextTag
{
	private Dictionary<string, string> _attributes;

	public string Name { get; private set; }

	public RichTextTagType Type { get; set; }

	public RichTextTag(string name)
	{
		Name = name;
		_attributes = new Dictionary<string, string>();
	}

	public void AddAtrribute(string key, string value)
	{
		_attributes.Add(key, value);
	}

	public string GetAttribute(string key)
	{
		if (_attributes.ContainsKey(key))
		{
			return _attributes[key];
		}
		return "";
	}
}
