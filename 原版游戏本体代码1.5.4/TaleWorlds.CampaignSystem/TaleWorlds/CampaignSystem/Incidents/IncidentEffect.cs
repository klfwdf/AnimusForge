using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Incidents;

public class IncidentEffect
{
	private readonly Func<bool> _condition;

	private readonly Func<List<TextObject>> _consequence;

	private readonly Func<IncidentEffect, IncidentHint> _hint;

	public float ChanceToOccur { get; private set; } = 1f;

	public IncidentEffect(Func<bool> condition, Func<List<TextObject>> consequence, Func<IncidentEffect, IncidentHint> hint)
	{
		_condition = condition;
		_consequence = consequence;
		_hint = hint;
	}

	public bool Condition()
	{
		if (_condition != null)
		{
			return _condition();
		}
		return true;
	}

	public List<TextObject> Consequence()
	{
		List<TextObject> result = new List<TextObject>();
		if (MBRandom.RandomFloat <= ChanceToOccur)
		{
			result = _consequence?.Invoke();
		}
		return result;
	}

	public IncidentHint GetHint()
	{
		return _hint?.Invoke(this).WithChance(ChanceToOccur);
	}

	public IncidentEffect WithChance(float chance)
	{
		ChanceToOccur = chance;
		return this;
	}
}
