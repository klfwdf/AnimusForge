using System;
using System.Collections.Generic;
using System.Xml;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.Diamond.MultiplayerBadges;

public class BadgeCondition
{
	public ConditionType Type { get; private set; }

	public ConditionGroupType GroupType { get; private set; }

	public TextObject Description { get; private set; }

	public string StringId { get; private set; }

	public IReadOnlyDictionary<string, string> Parameters { get; private set; }

	public BadgeCondition(int index, XmlNode node)
	{
		if (!Enum.TryParse<ConditionType>(node.Attributes?["type"].Value, ignoreCase: true, out var result))
		{
			result = ConditionType.Custom;
			Debug.FailedAssert("No 'type' was provided for a condition", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Diamond\\MultiplayerBadges\\BadgeCondition.cs", ".ctor", 47);
		}
		Type = result;
		ConditionGroupType result2 = ConditionGroupType.Any;
		if (node.Attributes?["group_type"]?.Value != null && !Enum.TryParse<ConditionGroupType>(node.Attributes["group_type"].Value, ignoreCase: true, out result2))
		{
			Debug.FailedAssert("Provided 'group_type' was wrong for a condition", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Diamond\\MultiplayerBadges\\BadgeCondition.cs", ".ctor", 54);
		}
		GroupType = result2;
		Description = new TextObject(node.Attributes?["description"].Value);
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		foreach (XmlNode childNode in node.ChildNodes)
		{
			if (childNode.Name == "Parameter")
			{
				string key = childNode.Attributes["name"].Value.Trim();
				string value = childNode.Attributes["value"].Value.Trim();
				dictionary[key] = value;
			}
		}
		Parameters = dictionary;
		StringId = node.Attributes?["id"]?.Value;
		if (StringId == null && Parameters.TryGetValue("property", out var value2))
		{
			StringId = value2 + ((GroupType == ConditionGroupType.Party) ? ".Party" : ((GroupType == ConditionGroupType.Solo) ? ".Solo" : ""));
		}
		if (StringId == null)
		{
			StringId = "condition." + index;
		}
	}

	public bool Check(string value)
	{
		ConditionType type = Type;
		if (type == ConditionType.PlayerData)
		{
			if (!Parameters.TryGetValue("value", out var value2))
			{
				Debug.FailedAssert("Given condition doesn't have a value parameter", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Diamond\\MultiplayerBadges\\BadgeCondition.cs", "Check", 94);
				return false;
			}
			return value == value2;
		}
		return false;
	}

	public bool Check(int value)
	{
		ConditionType type = Type;
		if ((uint)(type - 2) <= 2u)
		{
			if (Parameters.TryGetValue("value", out var value2))
			{
				if (!int.TryParse(value2, out var result))
				{
					Debug.FailedAssert("Given condition value parameter is not valid number", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Diamond\\MultiplayerBadges\\BadgeCondition.cs", "Check", 115);
					return false;
				}
				return value == result;
			}
			string value3;
			bool flag = Parameters.TryGetValue("min_value", out value3);
			string value4;
			bool flag2 = Parameters.TryGetValue("max_value", out value4);
			int result2 = int.MinValue;
			int result3 = int.MaxValue;
			if (flag && !int.TryParse(value3, out result2))
			{
				Debug.FailedAssert("Given condition min_value parameter is not valid number", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Diamond\\MultiplayerBadges\\BadgeCondition.cs", "Check", 129);
				return false;
			}
			if (flag2 && !int.TryParse(value4, out result3))
			{
				Debug.FailedAssert("Given condition max_value parameter is not valid number", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Diamond\\MultiplayerBadges\\BadgeCondition.cs", "Check", 134);
				return false;
			}
			if ((flag || flag2) && value >= result2)
			{
				return value <= result3;
			}
			return false;
		}
		return false;
	}
}
