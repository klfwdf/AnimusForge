namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class MerchantNotableTypeTag : ConversationTag
{
	public const string Id = "MerchantNotableTypeTag";

	public override string StringId => "MerchantNotableTypeTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (character.IsHero)
		{
			return character.Occupation == Occupation.Merchant;
		}
		return false;
	}
}
