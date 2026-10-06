using StoryMode.StoryModeObjects;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation.Tags;

namespace StoryMode;

public class IsStoryModeMentorTag : ConversationTag
{
	public const string Id = "IsStoryModeMentorTag";

	public override string StringId => "IsStoryModeMentorTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (StoryModeHeroes.AntiImperialMentor.CharacterObject != character)
		{
			return StoryModeHeroes.ImperialMentor.CharacterObject == character;
		}
		return true;
	}
}
