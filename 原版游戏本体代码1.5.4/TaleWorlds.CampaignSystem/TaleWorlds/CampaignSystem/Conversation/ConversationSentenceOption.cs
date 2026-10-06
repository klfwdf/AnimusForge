using TaleWorlds.CampaignSystem.Conversation.Persuasion;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Conversation;

public struct ConversationSentenceOption
{
	public int SentenceNo;

	public string Id;

	public object RepeatObject;

	public TextObject Text;

	public string DebugInfo;

	public bool IsClickable;

	public bool HasPersuasion;

	public string SkillName;

	public string TraitName;

	public bool IsSpecial;

	public bool IsUsedOnce;

	public TextObject HintText;

	public PersuasionOptionArgs PersuationOptionArgs;
}
