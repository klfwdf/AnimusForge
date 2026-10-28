using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public interface IFacegenCampaignBehavior : ICampaignBehavior
{
	IFaceGeneratorCustomFilter GetFaceGenFilter();
}
