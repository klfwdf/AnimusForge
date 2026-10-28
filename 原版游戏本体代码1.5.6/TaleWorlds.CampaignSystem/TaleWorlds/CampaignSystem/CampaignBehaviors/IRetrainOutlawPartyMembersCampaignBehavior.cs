namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public interface IRetrainOutlawPartyMembersCampaignBehavior : ICampaignBehavior
{
	int GetRetrainedNumber(CharacterObject character);

	void SetRetrainedNumber(CharacterObject character, int number);
}
