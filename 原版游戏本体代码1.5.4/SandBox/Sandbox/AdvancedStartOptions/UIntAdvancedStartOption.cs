using TaleWorlds.CampaignSystem.AdvancedStartOptions;

namespace SandBox.AdvancedStartOptions;

public class UIntAdvancedStartOption : AdvancedStartOption
{
	public readonly uint MinValue;

	public readonly uint MaxValue;

	public UIntAdvancedStartOption(string stringId, string categoryId, uint minValue, uint maxValue, AdvancedStartOptionCondition onCondition, uint defaultValue = 0u)
		: base(new AdvancedStartData<uint>(stringId, categoryId, defaultValue), onCondition)
	{
		MinValue = minValue;
		MaxValue = maxValue;
	}
}
