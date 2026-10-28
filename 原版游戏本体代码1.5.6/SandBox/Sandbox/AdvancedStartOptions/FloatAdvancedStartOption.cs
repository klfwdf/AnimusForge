using TaleWorlds.CampaignSystem.AdvancedStartOptions;
using TaleWorlds.Library;

namespace SandBox.AdvancedStartOptions;

public class FloatAdvancedStartOption : AdvancedStartOption
{
	public readonly float MinValue;

	public readonly float MaxValue;

	public override bool HasValueChanged()
	{
		return !GetValue<float>().ApproximatelyEqualsTo(GetDefaultValue<float>());
	}

	public FloatAdvancedStartOption(string stringId, string categoryId, float minValue, float maxValue, AdvancedStartOptionCondition onCondition, float defaultValue = 0f)
		: base(new AdvancedStartData<float>(stringId, categoryId, defaultValue), onCondition)
	{
		MinValue = minValue;
		MaxValue = maxValue;
	}
}
