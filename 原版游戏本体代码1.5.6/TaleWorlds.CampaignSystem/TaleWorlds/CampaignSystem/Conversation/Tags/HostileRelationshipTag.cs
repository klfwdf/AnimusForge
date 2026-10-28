using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class HostileRelationshipTag : ConversationTag
{
	public const string Id = "HostileRelationshipTag";

	public override string StringId => "HostileRelationshipTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (!character.IsHero)
		{
			return false;
		}
		float unmodifiedClanLeaderRelationshipWithPlayer = character.HeroObject.GetUnmodifiedClanLeaderRelationshipWithPlayer();
		int num = ConversationTagHelper.TraitCompatibility(character.HeroObject, Hero.MainHero, DefaultTraits.Mercy);
		int num2 = ConversationTagHelper.TraitCompatibility(character.HeroObject, Hero.MainHero, DefaultTraits.Honor);
		int num3 = ConversationTagHelper.TraitCompatibility(character.HeroObject, Hero.MainHero, DefaultTraits.Valor);
		if (num + num2 + num3 < -1 && unmodifiedClanLeaderRelationshipWithPlayer <= -5f)
		{
			return true;
		}
		if (unmodifiedClanLeaderRelationshipWithPlayer <= -20f)
		{
			return true;
		}
		return false;
	}
}
