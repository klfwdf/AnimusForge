using StoryMode.StoryModeObjects;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation.Tags;

namespace StoryMode;

public class IsArzagosTag : ConversationTag
{
	public const string Id = "IsArzagosTag";

	public override string StringId => "IsArzagosTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		return StoryModeHeroes.AntiImperialMentor.CharacterObject == character;
	}
}
