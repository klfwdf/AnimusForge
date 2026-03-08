using System;
using TaleWorlds.CampaignSystem.Conversation.Persuasion;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace Helpers
{
	// Token: 0x0200001E RID: 30
	public static class PersuasionHelper
	{
		// Token: 0x06000109 RID: 265 RVA: 0x0000D3F8 File Offset: 0x0000B5F8
		public static TextObject ShowSuccess(PersuasionOptionArgs optionArgs, bool showToPlayer = true)
		{
			return TextObject.GetEmpty();
		}

		// Token: 0x0600010A RID: 266 RVA: 0x0000D400 File Offset: 0x0000B600
		public static TextObject GetDefaultPersuasionOptionReaction(PersuasionOptionResult optionResult)
		{
			TextObject result;
			if (optionResult == PersuasionOptionResult.CriticalSuccess)
			{
				result = new TextObject("{=yNSqDwse}Well... I can't argue with that.", null);
			}
			else if (optionResult == PersuasionOptionResult.Failure || optionResult == PersuasionOptionResult.Miss)
			{
				result = new TextObject("{=mZmCmC6q}I don't think so.", null);
			}
			else if (optionResult == PersuasionOptionResult.CriticalFailure)
			{
				result = new TextObject("{=zqapPfSK}No.. No.", null);
			}
			else
			{
				result = ((MBRandom.RandomFloat > 0.5f) ? new TextObject("{=AmBEgOyq}I see...", null) : new TextObject("{=hq13B7Ok}Yes.. You might be correct.", null));
			}
			return result;
		}
	}
}
