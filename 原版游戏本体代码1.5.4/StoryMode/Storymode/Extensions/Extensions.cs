using TaleWorlds.CampaignSystem.Settlements;

namespace StoryMode.Extensions;

public static class Extensions
{
	public static bool IsTrainingField(this Settlement settlement)
	{
		return settlement.SettlementComponent is TrainingField;
	}

	public static TrainingField TrainingField(this Settlement settlement)
	{
		return settlement.SettlementComponent as TrainingField;
	}
}
