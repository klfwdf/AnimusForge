using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.Core;

public sealed class BannerEffect : PropertyObject
{
	private readonly float[] _levelBonuses = new float[3];

	public EffectIncrementType IncrementType { get; private set; }

	public BannerEffect(string stringId)
		: base(stringId)
	{
	}

	public void Initialize(string name, string description, float level1Bonus, float level2Bonus, float level3Bonus, EffectIncrementType incrementType)
	{
		TextObject description2 = new TextObject(description);
		_levelBonuses[0] = level1Bonus;
		_levelBonuses[1] = level2Bonus;
		_levelBonuses[2] = level3Bonus;
		IncrementType = incrementType;
		Initialize(new TextObject(name), description2);
		AfterInitialized();
	}

	public float GetBonusAtLevel(int bannerLevel)
	{
		int value = bannerLevel - 1;
		value = MBMath.ClampIndex(value, 0, _levelBonuses.Length);
		return _levelBonuses[value];
	}

	public string GetBonusStringAtLevel(int bannerLevel)
	{
		float bonusAtLevel = GetBonusAtLevel(bannerLevel);
		return $"{bonusAtLevel:P2}";
	}

	public TextObject GetDescription(int bannerLevel)
	{
		float bonusAtLevel = GetBonusAtLevel(bannerLevel);
		if (bonusAtLevel > 0f)
		{
			TextObject textObject = new TextObject("{=Ffwgecvr}{PLUS_OR_MINUS}{BONUSEFFECT}");
			textObject.SetTextVariable("BONUSEFFECT", bonusAtLevel);
			textObject.SetTextVariable("PLUS_OR_MINUS", "{=eTw2aNV5}+");
			return base.Description.SetTextVariable("BONUS_AMOUNT", textObject);
		}
		return base.Description.SetTextVariable("BONUS_AMOUNT", bonusAtLevel);
	}

	public override string ToString()
	{
		return base.Name.ToString();
	}
}
