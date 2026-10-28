using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem;

internal class DialogFlowLine
{
	internal TextObject Text;

	internal string InputToken;

	internal string OutputToken;

	internal bool ByPlayer;

	internal ConversationSentence.OnConditionDelegate ConditionDelegate;

	internal ConversationSentence.OnClickableConditionDelegate ClickableConditionDelegate;

	internal ConversationSentence.OnConsequenceDelegate ConsequenceDelegate;

	internal ConversationSentence.OnMultipleConversationConsequenceDelegate SpeakerDelegate;

	internal ConversationSentence.OnMultipleConversationConsequenceDelegate ListenerDelegate;

	internal bool IsRepeatable;

	internal bool IsSpecialOption;

	internal bool IsUsedOnce;

	public List<KeyValuePair<TextObject, List<GameTextManager.ChoiceTag>>> Variations { get; private set; }

	public bool HasVariation => Variations.Count > 0;

	internal DialogFlowLine()
	{
		Variations = new List<KeyValuePair<TextObject, List<GameTextManager.ChoiceTag>>>();
	}

	public void AddVariation(TextObject text, List<GameTextManager.ChoiceTag> list)
	{
		Variations.Add(new KeyValuePair<TextObject, List<GameTextManager.ChoiceTag>>(text, list));
	}
}
