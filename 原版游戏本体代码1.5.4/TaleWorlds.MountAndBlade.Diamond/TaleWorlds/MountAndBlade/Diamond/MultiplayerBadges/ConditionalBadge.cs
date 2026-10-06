using System.Collections.Generic;
using System.Xml;

namespace TaleWorlds.MountAndBlade.Diamond.MultiplayerBadges;

public class ConditionalBadge : Badge
{
	public IReadOnlyList<BadgeCondition> BadgeConditions { get; private set; }

	public ConditionalBadge(int index, BadgeType badgeType)
		: base(index, badgeType)
	{
	}

	public override void Deserialize(XmlNode node)
	{
		base.Deserialize(node);
		List<BadgeCondition> list = new List<BadgeCondition>();
		foreach (XmlNode childNode in node.ChildNodes)
		{
			if (childNode.Name == "Condition")
			{
				BadgeCondition item = new BadgeCondition(list.Count, childNode);
				list.Add(item);
			}
		}
		BadgeConditions = list;
	}
}
