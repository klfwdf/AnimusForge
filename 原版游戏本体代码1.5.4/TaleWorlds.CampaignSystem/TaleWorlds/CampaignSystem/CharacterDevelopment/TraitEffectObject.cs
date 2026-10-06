using System;
using Helpers;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CharacterDevelopment;

public class TraitEffectObject : PropertyObject
{
	private float[] _effectBonuses;

	public TraitObject Trait { get; private set; }

	public EffectIncrementType IncrementType { get; private set; }

	public bool IsPositive { get; private set; }

	public TraitEffectObject(string stringId)
		: base(stringId)
	{
	}

	public void Initialize(string description, TraitObject trait, float[] effectBonuses, bool isPositiveEffect, EffectIncrementType incrementType)
	{
		Initialize(new TextObject("{=!}" + base.StringId), new TextObject(description));
		Trait = trait;
		IncrementType = incrementType;
		IsPositive = isPositiveEffect;
		_effectBonuses = effectBonuses;
		AfterInitialized();
	}

	public float GetBonus(int level)
	{
		return _effectBonuses[Math.Abs(Trait.MinValue) + level];
	}

	public string GetBonusText(int level)
	{
		string effectIncrementTypeBonusText = StringHelpers.GetEffectIncrementTypeBonusText(GetBonus(level), IncrementType);
		if (IncrementType == EffectIncrementType.AddFactor)
		{
			GameTexts.SetVariable("NUMBER", effectIncrementTypeBonusText);
			return GameTexts.FindText("str_NUMBER_percent").ToString();
		}
		return effectIncrementTypeBonusText;
	}
}
