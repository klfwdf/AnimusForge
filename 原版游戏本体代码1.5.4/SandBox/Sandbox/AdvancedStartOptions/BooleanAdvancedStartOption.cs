using TaleWorlds.CampaignSystem.AdvancedStartOptions;

namespace SandBox.AdvancedStartOptions;

public class BooleanAdvancedStartOption : AdvancedStartOption
{
	public BooleanAdvancedStartOption(string stringId, string categoryId, AdvancedStartOptionCondition onCondition, bool defaultValue = false)
		: base(new AdvancedStartData<bool>(stringId, categoryId, defaultValue), onCondition)
	{
	}
}
