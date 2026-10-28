using TaleWorlds.CampaignSystem.AdvancedStartOptions;

namespace SandBox.AdvancedStartOptions;

public class IntAdvancedStartOption : AdvancedStartOption
{
	public readonly int MinValue;

	public readonly int MaxValue;

	public IntAdvancedStartOption(string stringId, string categoryId, int minValue, int maxValue, AdvancedStartOptionCondition onCondition, int defaultValue = 0)
		: base(new AdvancedStartData<int>(stringId, categoryId, defaultValue), onCondition)
	{
		MinValue = minValue;
		MaxValue = maxValue;
	}
}
