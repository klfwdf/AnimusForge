using System;
using System.Globalization;
using System.Xml;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.Diamond.MultiplayerBadges;

public class Badge
{
	public int Index { get; }

	public BadgeType Type { get; }

	public string StringId { get; private set; }

	public string GroupId { get; private set; }

	public TextObject Name { get; private set; }

	public TextObject Description { get; private set; }

	public bool IsVisibleOnlyWhenEarned { get; private set; }

	public DateTime PeriodStart { get; private set; }

	public DateTime PeriodEnd { get; private set; }

	public bool IsActive
	{
		get
		{
			if (DateTime.UtcNow >= PeriodStart)
			{
				return DateTime.UtcNow <= PeriodEnd;
			}
			return false;
		}
	}

	public bool IsTimed
	{
		get
		{
			if (!(PeriodStart > DateTime.MinValue))
			{
				return PeriodEnd < DateTime.MaxValue;
			}
			return true;
		}
	}

	public Badge(int index, BadgeType badgeType)
	{
		Index = index;
		Type = badgeType;
	}

	public virtual void Deserialize(XmlNode node)
	{
		StringId = node.Attributes["id"].Value;
		string text = node.Attributes?["group_id"]?.Value;
		GroupId = (string.IsNullOrWhiteSpace(text) ? null : text);
		string value = node.Attributes["name"].Value;
		string value2 = node.Attributes["description"].Value;
		IsVisibleOnlyWhenEarned = Convert.ToBoolean(node.Attributes["is_visible_only_when_earned"]?.Value);
		PeriodStart = (DateTime.TryParse(node.Attributes["period_start"]?.Value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result) ? DateTime.SpecifyKind(result, DateTimeKind.Utc) : DateTime.MinValue);
		PeriodEnd = (DateTime.TryParse(node.Attributes["period_end"]?.Value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result2) ? DateTime.SpecifyKind(result2, DateTimeKind.Utc) : DateTime.MaxValue);
		Name = new TextObject(value);
		Description = new TextObject(value2);
	}
}
