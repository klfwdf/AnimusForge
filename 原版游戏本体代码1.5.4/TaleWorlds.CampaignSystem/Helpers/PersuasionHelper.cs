using TaleWorlds.CampaignSystem.Conversation.Persuasion;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace Helpers;

public static class PersuasionHelper
{
	public static TextObject ShowSuccess(PersuasionOptionArgs optionArgs, bool showToPlayer = true)
	{
		return TextObject.GetEmpty();
	}

	public static TextObject GetDefaultPersuasionOptionReaction(PersuasionOptionResult optionResult)
	{
		switch (optionResult)
		{
		case PersuasionOptionResult.CriticalSuccess:
			return new TextObject("{=yNSqDwse}Well... I can't argue with that.");
		case PersuasionOptionResult.Failure:
		case PersuasionOptionResult.Miss:
			return new TextObject("{=mZmCmC6q}I don't think so.");
		case PersuasionOptionResult.CriticalFailure:
			return new TextObject("{=zqapPfSK}No.. No.");
		default:
			return (MBRandom.RandomFloat > 0.5f) ? new TextObject("{=AmBEgOyq}I see...") : new TextObject("{=hq13B7Ok}Yes.. You might be correct.");
		}
	}
}
