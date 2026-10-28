using StoryMode.StoryModeObjects;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation.Tags;

namespace StoryMode;

public class IsIstianaTag : ConversationTag
{
	public const string Id = "IsIstianaTag";

	public override string StringId => "IsIstianaTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		return StoryModeHeroes.ImperialMentor.CharacterObject == character;
	}
}
