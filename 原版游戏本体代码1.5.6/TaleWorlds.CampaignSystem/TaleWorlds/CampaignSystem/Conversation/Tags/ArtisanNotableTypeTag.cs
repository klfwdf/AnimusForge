namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class ArtisanNotableTypeTag : ConversationTag
{
	public const string Id = "ArtisanNotableTypeTag";

	public override string StringId => "ArtisanNotableTypeTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (character.IsHero)
		{
			return character.Occupation == Occupation.Artisan;
		}
		return false;
	}
}
