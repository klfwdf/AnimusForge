using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x02000081 RID: 129
	internal class DialogFlowLine
	{
		// Token: 0x1700044A RID: 1098
		// (get) Token: 0x060010D4 RID: 4308 RVA: 0x00050324 File Offset: 0x0004E524
		// (set) Token: 0x060010D3 RID: 4307 RVA: 0x0005031B File Offset: 0x0004E51B
		public List<KeyValuePair<TextObject, List<GameTextManager.ChoiceTag>>> Variations { get; private set; }

		// Token: 0x1700044B RID: 1099
		// (get) Token: 0x060010D5 RID: 4309 RVA: 0x0005032C File Offset: 0x0004E52C
		public bool HasVariation
		{
			get
			{
				return this.Variations.Count > 0;
			}
		}

		// Token: 0x060010D6 RID: 4310 RVA: 0x0005033C File Offset: 0x0004E53C
		internal DialogFlowLine()
		{
			this.Variations = new List<KeyValuePair<TextObject, List<GameTextManager.ChoiceTag>>>();
		}

		// Token: 0x060010D7 RID: 4311 RVA: 0x0005034F File Offset: 0x0004E54F
		public void AddVariation(TextObject text, List<GameTextManager.ChoiceTag> list)
		{
			this.Variations.Add(new KeyValuePair<TextObject, List<GameTextManager.ChoiceTag>>(text, list));
		}

		// Token: 0x04000536 RID: 1334
		internal TextObject Text;

		// Token: 0x04000537 RID: 1335
		internal string InputToken;

		// Token: 0x04000538 RID: 1336
		internal string OutputToken;

		// Token: 0x04000539 RID: 1337
		internal bool ByPlayer;

		// Token: 0x0400053A RID: 1338
		internal ConversationSentence.OnConditionDelegate ConditionDelegate;

		// Token: 0x0400053B RID: 1339
		internal ConversationSentence.OnClickableConditionDelegate ClickableConditionDelegate;

		// Token: 0x0400053C RID: 1340
		internal ConversationSentence.OnConsequenceDelegate ConsequenceDelegate;

		// Token: 0x0400053D RID: 1341
		internal ConversationSentence.OnMultipleConversationConsequenceDelegate SpeakerDelegate;

		// Token: 0x0400053E RID: 1342
		internal ConversationSentence.OnMultipleConversationConsequenceDelegate ListenerDelegate;

		// Token: 0x0400053F RID: 1343
		internal bool IsRepeatable;

		// Token: 0x04000540 RID: 1344
		internal bool IsSpecialOption;

		// Token: 0x04000541 RID: 1345
		internal bool IsUsedOnce;
	}
}
