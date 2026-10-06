namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class NonviolentProfessionTag : ConversationTag
{
	public const string Id = "NonviolentProfessionTag";

	public override string StringId => "NonviolentProfessionTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (character.IsHero)
		{
			if (character.Occupation != Occupation.Artisan && character.Occupation != Occupation.Merchant)
			{
				return character.Occupation == Occupation.Headman;
			}
			return true;
		}
		return false;
	}
}
