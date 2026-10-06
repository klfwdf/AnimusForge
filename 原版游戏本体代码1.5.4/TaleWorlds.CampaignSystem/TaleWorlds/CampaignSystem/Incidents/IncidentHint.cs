using System;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Incidents;

public class IncidentHint
{
	public TextObject Text { get; }

	public IncidentHintType Type { get; }

	public float Chance { get; private set; } = 1f;

	public IncidentHint[] Children { get; }

	public IncidentHint(TextObject text, IncidentHintType type = IncidentHintType.Effect)
		: this(text, type, Array.Empty<IncidentHint>())
	{
	}

	public IncidentHint(TextObject text, IncidentHintType type, IncidentHint[] children)
	{
		Text = text;
		Type = type;
		Children = children ?? Array.Empty<IncidentHint>();
	}

	public IncidentHint WithChance(float chance)
	{
		Chance = chance;
		return this;
	}
}
