using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public interface IGarrisonRecruitmentBehavior
{
	ExplainedNumber GetGarrisonChangeExplainedNumber(Town town);
}
