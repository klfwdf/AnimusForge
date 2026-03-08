using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x02000083 RID: 131
	public class DialogFlow
	{
		// Token: 0x060010D9 RID: 4313 RVA: 0x00050388 File Offset: 0x0004E588
		private DialogFlow(string startingToken, int priority = 100)
		{
			this._currentToken = startingToken;
			this.Priority = priority;
		}

		// Token: 0x060010DA RID: 4314 RVA: 0x000503AC File Offset: 0x0004E5AC
		private DialogFlow Line(TextObject text, bool byPlayer, ConversationSentence.OnMultipleConversationConsequenceDelegate speakerDelegate = null, ConversationSentence.OnMultipleConversationConsequenceDelegate listenerDelegate = null, bool isRepeatable = false, string inputToken = null, string outputToken = null)
		{
			string text2 = outputToken ?? Campaign.Current.ConversationManager.CreateToken();
			this.AddLine(text, inputToken ?? this._currentToken, text2, byPlayer, speakerDelegate, listenerDelegate, isRepeatable, false, false);
			this._currentToken = text2;
			return this;
		}

		// Token: 0x060010DB RID: 4315 RVA: 0x000503F4 File Offset: 0x0004E5F4
		public DialogFlow Variation(string text, params object[] propertiesAndWeights)
		{
			return this.Variation(new TextObject(text, null), propertiesAndWeights);
		}

		// Token: 0x060010DC RID: 4316 RVA: 0x00050404 File Offset: 0x0004E604
		public DialogFlow Variation(TextObject text, params object[] propertiesAndWeights)
		{
			for (int i = 0; i < propertiesAndWeights.Length; i += 2)
			{
				string tagName = (string)propertiesAndWeights[i];
				int weight = Convert.ToInt32(propertiesAndWeights[i + 1]);
				List<GameTextManager.ChoiceTag> list = new List<GameTextManager.ChoiceTag>();
				list.Add(new GameTextManager.ChoiceTag(tagName, weight));
				this.Lines[this.Lines.Count - 1].AddVariation(text, list);
			}
			return this;
		}

		// Token: 0x060010DD RID: 4317 RVA: 0x00050466 File Offset: 0x0004E666
		public DialogFlow NpcLine(string npcText, ConversationSentence.OnMultipleConversationConsequenceDelegate speakerDelegate = null, ConversationSentence.OnMultipleConversationConsequenceDelegate listenerDelegate = null, string inputToken = null, string outputToken = null)
		{
			return this.NpcLine(new TextObject(npcText, null), speakerDelegate, listenerDelegate, inputToken, outputToken);
		}

		// Token: 0x060010DE RID: 4318 RVA: 0x0005047B File Offset: 0x0004E67B
		public DialogFlow NpcLine(TextObject npcText, ConversationSentence.OnMultipleConversationConsequenceDelegate speakerDelegate = null, ConversationSentence.OnMultipleConversationConsequenceDelegate listenerDelegate = null, string inputToken = null, string outputToken = null)
		{
			return this.Line(npcText, false, speakerDelegate, listenerDelegate, false, inputToken, outputToken);
		}

		// Token: 0x060010DF RID: 4319 RVA: 0x0005048C File Offset: 0x0004E68C
		public DialogFlow NpcLineWithVariation(string npcText, ConversationSentence.OnMultipleConversationConsequenceDelegate speakerDelegate = null, ConversationSentence.OnMultipleConversationConsequenceDelegate listenerDelegate = null, string inputToken = null, string outputToken = null)
		{
			DialogFlow result = this.Line(TextObject.GetEmpty(), false, speakerDelegate, listenerDelegate, false, inputToken, outputToken);
			List<GameTextManager.ChoiceTag> list = new List<GameTextManager.ChoiceTag>();
			list.Add(new GameTextManager.ChoiceTag("DefaultTag", 1));
			this.Lines[this.Lines.Count - 1].AddVariation(new TextObject(npcText, null), list);
			return result;
		}

		// Token: 0x060010E0 RID: 4320 RVA: 0x000504E8 File Offset: 0x0004E6E8
		public DialogFlow NpcLineWithVariation(TextObject npcText, ConversationSentence.OnMultipleConversationConsequenceDelegate speakerDelegate = null, ConversationSentence.OnMultipleConversationConsequenceDelegate listenerDelegate = null, string inputToken = null, string outputToken = null)
		{
			DialogFlow result = this.Line(TextObject.GetEmpty(), false, speakerDelegate, listenerDelegate, false, inputToken, outputToken);
			List<GameTextManager.ChoiceTag> list = new List<GameTextManager.ChoiceTag>();
			list.Add(new GameTextManager.ChoiceTag("DefaultTag", 1));
			this.Lines[this.Lines.Count - 1].AddVariation(npcText, list);
			return result;
		}

		// Token: 0x060010E1 RID: 4321 RVA: 0x0005053E File Offset: 0x0004E73E
		public DialogFlow PlayerLine(string playerText, ConversationSentence.OnMultipleConversationConsequenceDelegate listenerDelegate = null, string inputToken = null, string outputToken = null)
		{
			return this.Line(new TextObject(playerText, null), true, null, listenerDelegate, false, inputToken, outputToken);
		}

		// Token: 0x060010E2 RID: 4322 RVA: 0x00050554 File Offset: 0x0004E754
		public DialogFlow PlayerLine(TextObject playerText, ConversationSentence.OnMultipleConversationConsequenceDelegate listenerDelegate = null, string inputToken = null, string outputToken = null)
		{
			return this.Line(playerText, true, null, listenerDelegate, false, inputToken, outputToken);
		}

		// Token: 0x060010E3 RID: 4323 RVA: 0x00050564 File Offset: 0x0004E764
		private DialogFlow BeginOptions(bool byPlayer, string inputToken = null, bool optionUsedOnce = false)
		{
			this._curDialogFlowContext = new DialogFlowContext(inputToken ?? this._currentToken, byPlayer, this._curDialogFlowContext, optionUsedOnce);
			return this;
		}

		// Token: 0x060010E4 RID: 4324 RVA: 0x00050585 File Offset: 0x0004E785
		public DialogFlow BeginPlayerOptions(string inputToken = null, bool optionUsedOnce = false)
		{
			return this.BeginOptions(true, inputToken, optionUsedOnce);
		}

		// Token: 0x060010E5 RID: 4325 RVA: 0x00050590 File Offset: 0x0004E790
		public DialogFlow BeginNpcOptions(string inputToken = null, bool optionUsedOnce = false)
		{
			return this.BeginOptions(false, inputToken, optionUsedOnce);
		}

		// Token: 0x060010E6 RID: 4326 RVA: 0x0005059C File Offset: 0x0004E79C
		private DialogFlow Option(TextObject text, bool byPlayer, ConversationSentence.OnMultipleConversationConsequenceDelegate speakerDelegate = null, ConversationSentence.OnMultipleConversationConsequenceDelegate listenerDelegate = null, bool isRepeatable = false, bool isSpecialOption = false, string inputToken = null, string outputToken = null)
		{
			string text2 = outputToken ?? Campaign.Current.ConversationManager.CreateToken();
			this.AddLine(text, inputToken ?? this._curDialogFlowContext.Token, text2, byPlayer, speakerDelegate, listenerDelegate, isRepeatable, isSpecialOption, this._curDialogFlowContext.OptionsUsedOnlyOnce);
			this._currentToken = text2;
			return this;
		}

		// Token: 0x060010E7 RID: 4327 RVA: 0x000505F4 File Offset: 0x0004E7F4
		public DialogFlow PlayerOption(string text, ConversationSentence.OnMultipleConversationConsequenceDelegate listenerDelegate = null, string inputToken = null, string outputToken = null)
		{
			return this.PlayerOption(new TextObject(text, null), listenerDelegate, inputToken, outputToken);
		}

		// Token: 0x060010E8 RID: 4328 RVA: 0x00050608 File Offset: 0x0004E808
		public DialogFlow PlayerOption(TextObject text, ConversationSentence.OnMultipleConversationConsequenceDelegate listenerDelegate = null, string inputToken = null, string outputToken = null)
		{
			this.Option(text, true, null, listenerDelegate, false, false, inputToken, outputToken);
			return this;
		}

		// Token: 0x060010E9 RID: 4329 RVA: 0x00050628 File Offset: 0x0004E828
		public DialogFlow PlayerSpecialOption(TextObject text, ConversationSentence.OnMultipleConversationConsequenceDelegate listenerDelegate = null, string inputToken = null, string outputToken = null)
		{
			this.Option(text, true, null, listenerDelegate, false, true, inputToken, outputToken);
			return this;
		}

		// Token: 0x060010EA RID: 4330 RVA: 0x00050648 File Offset: 0x0004E848
		public DialogFlow PlayerRepeatableOption(TextObject text, ConversationSentence.OnMultipleConversationConsequenceDelegate listenerDelegate = null, string inputToken = null, string outputToken = null)
		{
			this.Option(text, true, null, listenerDelegate, true, false, inputToken, outputToken);
			return this;
		}

		// Token: 0x060010EB RID: 4331 RVA: 0x00050668 File Offset: 0x0004E868
		public DialogFlow NpcOption(string text, ConversationSentence.OnConditionDelegate conditionDelegate, ConversationSentence.OnMultipleConversationConsequenceDelegate speakerDelegate = null, ConversationSentence.OnMultipleConversationConsequenceDelegate listenerDelegate = null, string inputToken = null, string outputToken = null)
		{
			this.Option(new TextObject(text, null), false, speakerDelegate, listenerDelegate, false, false, inputToken, outputToken);
			this._lastLine.ConditionDelegate = conditionDelegate;
			return this;
		}

		// Token: 0x060010EC RID: 4332 RVA: 0x0005069C File Offset: 0x0004E89C
		public DialogFlow NpcOption(TextObject text, ConversationSentence.OnConditionDelegate conditionDelegate, ConversationSentence.OnMultipleConversationConsequenceDelegate speakerDelegate = null, ConversationSentence.OnMultipleConversationConsequenceDelegate listenerDelegate = null, string inputToken = null, string outputToken = null)
		{
			this.Option(text, false, speakerDelegate, listenerDelegate, false, false, inputToken, outputToken);
			this._lastLine.ConditionDelegate = conditionDelegate;
			return this;
		}

		// Token: 0x060010ED RID: 4333 RVA: 0x000506C8 File Offset: 0x0004E8C8
		public DialogFlow NpcOptionWithVariation(string text, ConversationSentence.OnConditionDelegate conditionDelegate, ConversationSentence.OnMultipleConversationConsequenceDelegate speakerDelegate = null, ConversationSentence.OnMultipleConversationConsequenceDelegate listenerDelegate = null, string inputToken = null, string outputToken = null)
		{
			this.NpcOptionWithVariation(new TextObject(text, null), conditionDelegate, speakerDelegate, listenerDelegate, inputToken, outputToken);
			return this;
		}

		// Token: 0x060010EE RID: 4334 RVA: 0x000506E4 File Offset: 0x0004E8E4
		public DialogFlow NpcOptionWithVariation(TextObject text, ConversationSentence.OnConditionDelegate conditionDelegate, ConversationSentence.OnMultipleConversationConsequenceDelegate speakerDelegate = null, ConversationSentence.OnMultipleConversationConsequenceDelegate listenerDelegate = null, string inputToken = null, string outputToken = null)
		{
			this.Option(TextObject.GetEmpty(), false, speakerDelegate, listenerDelegate, false, false, inputToken, outputToken);
			List<GameTextManager.ChoiceTag> list = new List<GameTextManager.ChoiceTag>();
			list.Add(new GameTextManager.ChoiceTag("DefaultTag", 1));
			this._lastLine.AddVariation(text, list);
			this._lastLine.ConditionDelegate = conditionDelegate;
			return this;
		}

		// Token: 0x060010EF RID: 4335 RVA: 0x00050738 File Offset: 0x0004E938
		private DialogFlow EndOptions(bool byPlayer)
		{
			this._curDialogFlowContext = this._curDialogFlowContext.Parent;
			return this;
		}

		// Token: 0x060010F0 RID: 4336 RVA: 0x0005074C File Offset: 0x0004E94C
		public DialogFlow EndPlayerOptions()
		{
			return this.EndOptions(true);
		}

		// Token: 0x060010F1 RID: 4337 RVA: 0x00050755 File Offset: 0x0004E955
		public DialogFlow EndNpcOptions()
		{
			return this.EndOptions(false);
		}

		// Token: 0x060010F2 RID: 4338 RVA: 0x0005075E File Offset: 0x0004E95E
		public DialogFlow Condition(ConversationSentence.OnConditionDelegate conditionDelegate)
		{
			this._lastLine.ConditionDelegate = conditionDelegate;
			return this;
		}

		// Token: 0x060010F3 RID: 4339 RVA: 0x0005076D File Offset: 0x0004E96D
		public DialogFlow ClickableCondition(ConversationSentence.OnClickableConditionDelegate clickableConditionDelegate)
		{
			this._lastLine.ClickableConditionDelegate = clickableConditionDelegate;
			return this;
		}

		// Token: 0x060010F4 RID: 4340 RVA: 0x0005077C File Offset: 0x0004E97C
		public DialogFlow Consequence(ConversationSentence.OnConsequenceDelegate consequenceDelegate)
		{
			this._lastLine.ConsequenceDelegate = consequenceDelegate;
			return this;
		}

		// Token: 0x060010F5 RID: 4341 RVA: 0x0005078B File Offset: 0x0004E98B
		public static DialogFlow CreateDialogFlow(string inputToken = null, int priority = 100)
		{
			return new DialogFlow(inputToken ?? Campaign.Current.ConversationManager.CreateToken(), priority);
		}

		// Token: 0x060010F6 RID: 4342 RVA: 0x000507A8 File Offset: 0x0004E9A8
		private DialogFlowLine AddLine(TextObject text, string inputToken, string outputToken, bool byPlayer, ConversationSentence.OnMultipleConversationConsequenceDelegate speakerDelegate, ConversationSentence.OnMultipleConversationConsequenceDelegate listenerDelegate, bool isRepeatable, bool isSpecialOption = false, bool usedOncePerConversation = false)
		{
			DialogFlowLine dialogFlowLine = new DialogFlowLine();
			dialogFlowLine.Text = text;
			dialogFlowLine.InputToken = inputToken;
			dialogFlowLine.OutputToken = outputToken;
			dialogFlowLine.ByPlayer = byPlayer;
			dialogFlowLine.SpeakerDelegate = speakerDelegate;
			dialogFlowLine.ListenerDelegate = listenerDelegate;
			dialogFlowLine.IsRepeatable = isRepeatable;
			dialogFlowLine.IsSpecialOption = isSpecialOption;
			dialogFlowLine.IsUsedOnce = usedOncePerConversation;
			this.Lines.Add(dialogFlowLine);
			this._lastLine = dialogFlowLine;
			return dialogFlowLine;
		}

		// Token: 0x060010F7 RID: 4343 RVA: 0x00050814 File Offset: 0x0004EA14
		public DialogFlow NpcDefaultOption(string text)
		{
			return this.NpcOption(text, null, null, null, null, null);
		}

		// Token: 0x060010F8 RID: 4344 RVA: 0x00050822 File Offset: 0x0004EA22
		public DialogFlow GenerateToken(out string token)
		{
			token = Campaign.Current.ConversationManager.CreateToken();
			return this;
		}

		// Token: 0x060010F9 RID: 4345 RVA: 0x00050836 File Offset: 0x0004EA36
		public DialogFlow GotoDialogState(string input)
		{
			this._lastLine.OutputToken = input;
			this._currentToken = input;
			return this;
		}

		// Token: 0x060010FA RID: 4346 RVA: 0x0005084C File Offset: 0x0004EA4C
		public DialogFlow GotoDialogStateBranched(string input, ConversationSentence.OnConditionDelegate conditionDelegate, string alternative)
		{
			string text = ((conditionDelegate != null && conditionDelegate()) ? input : alternative);
			this._lastLine.OutputToken = text;
			this._currentToken = text;
			return this;
		}

		// Token: 0x060010FB RID: 4347 RVA: 0x0005087D File Offset: 0x0004EA7D
		public DialogFlow GetOutputToken(out string oState)
		{
			oState = this._lastLine.OutputToken;
			return this;
		}

		// Token: 0x060010FC RID: 4348 RVA: 0x0005088D File Offset: 0x0004EA8D
		public DialogFlow GoBackToDialogState(string iState)
		{
			this._currentToken = iState;
			return this;
		}

		// Token: 0x060010FD RID: 4349 RVA: 0x00050897 File Offset: 0x0004EA97
		public DialogFlow CloseDialog()
		{
			this.GotoDialogState("close_window");
			return this;
		}

		// Token: 0x060010FE RID: 4350 RVA: 0x000508A6 File Offset: 0x0004EAA6
		private ConversationSentence AddDialogLine(ConversationSentence dialogLine)
		{
			Campaign.Current.ConversationManager.AddDialogLine(dialogLine);
			return dialogLine;
		}

		// Token: 0x060010FF RID: 4351 RVA: 0x000508BC File Offset: 0x0004EABC
		public ConversationSentence AddPlayerLine(string id, string inputToken, string outputToken, string text, ConversationSentence.OnConditionDelegate conditionDelegate, ConversationSentence.OnConsequenceDelegate consequenceDelegate, object relatedObject, int priority = 100, ConversationSentence.OnClickableConditionDelegate clickableConditionDelegate = null, ConversationSentence.OnPersuasionOptionDelegate persuasionOptionDelegate = null, ConversationSentence.OnMultipleConversationConsequenceDelegate speakerDelegate = null, ConversationSentence.OnMultipleConversationConsequenceDelegate listenerDelegate = null)
		{
			return this.AddDialogLine(new ConversationSentence(id, new TextObject(text, null), inputToken, outputToken, conditionDelegate, clickableConditionDelegate, consequenceDelegate, 1U, priority, 0, 0, relatedObject, false, speakerDelegate, listenerDelegate, persuasionOptionDelegate));
		}

		// Token: 0x06001100 RID: 4352 RVA: 0x000508F8 File Offset: 0x0004EAF8
		public ConversationSentence AddDialogLine(string id, string inputToken, string outputToken, string text, ConversationSentence.OnConditionDelegate conditionDelegate, ConversationSentence.OnConsequenceDelegate consequenceDelegate, object relatedObject, int priority = 100, ConversationSentence.OnClickableConditionDelegate clickableConditionDelegate = null, ConversationSentence.OnMultipleConversationConsequenceDelegate speakerDelegate = null, ConversationSentence.OnMultipleConversationConsequenceDelegate listenerDelegate = null)
		{
			return this.AddDialogLine(new ConversationSentence(id, new TextObject(text, null), inputToken, outputToken, conditionDelegate, clickableConditionDelegate, consequenceDelegate, 0U, priority, 0, 0, relatedObject, false, speakerDelegate, listenerDelegate, null));
		}

		// Token: 0x04000547 RID: 1351
		internal readonly List<DialogFlowLine> Lines = new List<DialogFlowLine>();

		// Token: 0x04000548 RID: 1352
		internal readonly int Priority;

		// Token: 0x04000549 RID: 1353
		private string _currentToken;

		// Token: 0x0400054A RID: 1354
		private DialogFlowLine _lastLine;

		// Token: 0x0400054B RID: 1355
		private DialogFlowContext _curDialogFlowContext;
	}
}
