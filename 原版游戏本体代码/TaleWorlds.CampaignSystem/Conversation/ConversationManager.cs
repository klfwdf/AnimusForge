using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Helpers;
using TaleWorlds.CampaignSystem.Conversation.Persuasion;
using TaleWorlds.CampaignSystem.Conversation.Tags;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ModuleManager;

namespace TaleWorlds.CampaignSystem.Conversation
{
	// Token: 0x02000234 RID: 564
	public class ConversationManager
	{
		// Token: 0x060021F2 RID: 8690 RVA: 0x00095D2A File Offset: 0x00093F2A
		public int CreateConversationSentenceIndex()
		{
			int numConversationSentencesCreated = this._numConversationSentencesCreated;
			this._numConversationSentencesCreated++;
			return numConversationSentencesCreated;
		}

		// Token: 0x17000865 RID: 2149
		// (get) Token: 0x060021F3 RID: 8691 RVA: 0x00095D40 File Offset: 0x00093F40
		public string CurrentSentenceText
		{
			get
			{
				TextObject textObject = this._currentSentenceText;
				if (this.OneToOneConversationCharacter != null)
				{
					textObject = this.FindMatchingTextOrNull(textObject.GetID(), this.OneToOneConversationCharacter);
					if (textObject == null)
					{
						textObject = this._currentSentenceText;
					}
				}
				return MBTextManager.DiscardAnimationTagsAndCheckAnimationTagPositions(textObject.CopyTextObject().ToString());
			}
		}

		// Token: 0x17000866 RID: 2150
		// (get) Token: 0x060021F4 RID: 8692 RVA: 0x00095D8F File Offset: 0x00093F8F
		private int DialogRepeatCount
		{
			get
			{
				if (this._dialogRepeatObjects.Count > 0)
				{
					return this._dialogRepeatObjects[this._currentRepeatedDialogSetIndex].Count;
				}
				return 1;
			}
		}

		// Token: 0x17000867 RID: 2151
		// (get) Token: 0x060021F5 RID: 8693 RVA: 0x00095DB7 File Offset: 0x00093FB7
		public bool IsConversationFlowActive
		{
			get
			{
				return this._isActive;
			}
		}

		// Token: 0x060021F6 RID: 8694 RVA: 0x00095DC0 File Offset: 0x00093FC0
		public ConversationManager()
		{
			this._sentences = new List<ConversationSentence>();
			this.stateMap = new Dictionary<string, int>();
			this.stateMap.Add("start", 0);
			this.stateMap.Add("event_triggered", 1);
			this.stateMap.Add("member_chat", 2);
			this.stateMap.Add("prisoner_chat", 3);
			this.stateMap.Add("close_window", 4);
			this._numberOfStateIndices = 5;
			this._isActive = false;
			this._executeDoOptionContinue = false;
			this.InitializeTags();
			this.ConversationAnimationManager = new ConversationAnimationManager();
		}

		// Token: 0x17000868 RID: 2152
		// (get) Token: 0x060021F7 RID: 8695 RVA: 0x00095E9B File Offset: 0x0009409B
		// (set) Token: 0x060021F8 RID: 8696 RVA: 0x00095EA3 File Offset: 0x000940A3
		public List<ConversationSentenceOption> CurOptions { get; protected set; }

		// Token: 0x060021F9 RID: 8697 RVA: 0x00095EAC File Offset: 0x000940AC
		public void StartNew(int startingToken, bool setActionsInstantly)
		{
			this._usedIndices.Clear();
			this.ActiveToken = startingToken;
			this._currentSentence = -1;
			this.ResetRepeatedDialogSystem();
			this._lastSelectedDialogObject = null;
			Debug.Print("--------------- Conversation Start --------------- ", 0, Debug.DebugColor.White, 4503599627370496UL);
			Debug.Print(string.Concat(new object[]
			{
				"Conversation character name: ",
				this.OneToOneConversationCharacter.Name,
				"\nid: ",
				this.OneToOneConversationCharacter.StringId,
				"\nculture:",
				this.OneToOneConversationCharacter.Culture,
				"\npersona:",
				this.OneToOneConversationCharacter.GetPersona().Name
			}), 0, Debug.DebugColor.White, 17592186044416UL);
			if (CampaignMission.Current != null)
			{
				foreach (IAgent agent in this.ConversationAgents)
				{
					CampaignMission.Current.OnConversationStart(agent, setActionsInstantly);
				}
			}
			this.ProcessPartnerSentence();
		}

		// Token: 0x060021FA RID: 8698 RVA: 0x00095FC4 File Offset: 0x000941C4
		private bool ProcessPartnerSentence()
		{
			List<ConversationSentenceOption> sentenceOptions = this.GetSentenceOptions(false, false);
			bool result = false;
			if (sentenceOptions.Count > 0)
			{
				this.ProcessSentence(sentenceOptions[0]);
				result = true;
			}
			IConversationStateHandler handler = this.Handler;
			if (handler != null)
			{
				handler.OnConversationContinue();
			}
			return result;
		}

		// Token: 0x060021FB RID: 8699 RVA: 0x00096008 File Offset: 0x00094208
		public void ProcessSentence(ConversationSentenceOption conversationSentenceOption)
		{
			ConversationSentence conversationSentence = this._sentences[conversationSentenceOption.SentenceNo];
			Debug.Print(conversationSentenceOption.DebugInfo, 0, Debug.DebugColor.White, 4503599627370496UL);
			this.ActiveToken = conversationSentence.OutputToken;
			this.UpdateSpeakerAndListenerAgents(conversationSentence);
			if (CampaignMission.Current != null)
			{
				CampaignMission.Current.OnProcessSentence();
			}
			this._lastSelectedDialogObject = conversationSentenceOption.RepeatObject;
			this._currentSentence = conversationSentenceOption.SentenceNo;
			if (Game.Current == null)
			{
				throw new MBNullParameterException("Game");
			}
			this.UpdateCurrentSentenceText();
			int count = this._sentences.Count;
			conversationSentence.RunConsequence(Game.Current);
			if (conversationSentence.IsUsedOnce)
			{
				this._usedIndices.Add(conversationSentence.Index);
			}
			if (CampaignMission.Current != null)
			{
				string[] conversationAnimations = MBTextManager.GetConversationAnimations(this._currentSentenceText);
				string soundPath = "";
				VoiceObject voiceObject;
				string text;
				if (MBTextManager.TryGetVoiceObject(this._currentSentenceText, out voiceObject, out text))
				{
					soundPath = Campaign.Current.Models.VoiceOverModel.GetSoundPathForCharacter((CharacterObject)this.SpeakerAgent.Character, voiceObject);
				}
				CampaignMission.Current.OnConversationPlay(conversationAnimations[0], conversationAnimations[1], conversationAnimations[2], conversationAnimations[3], soundPath);
			}
			if (0 > this._currentSentence || this._currentSentence >= count)
			{
				Debug.FailedAssert("CurrentSentence is not valid.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Conversation\\ConversationManager.cs", "ProcessSentence", 415);
			}
		}

		// Token: 0x060021FC RID: 8700 RVA: 0x00096158 File Offset: 0x00094358
		private void UpdateSpeakerAndListenerAgents(ConversationSentence sentence)
		{
			if (sentence.IsSpeaker != null)
			{
				if (sentence.IsSpeaker(this._mainAgent))
				{
					this.SetSpeakerAgent(this._mainAgent);
					goto IL_8B;
				}
				using (IEnumerator<IAgent> enumerator = this.ConversationAgents.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						IAgent agent = enumerator.Current;
						if (sentence.IsSpeaker(agent))
						{
							this.SetSpeakerAgent(agent);
							break;
						}
					}
					goto IL_8B;
				}
			}
			this.SetSpeakerAgent((!sentence.IsPlayer) ? this.ConversationAgents[0] : this._mainAgent);
			IL_8B:
			if (sentence.IsListener != null)
			{
				if (sentence.IsListener(this._mainAgent))
				{
					this.SetListenerAgent(this._mainAgent);
					return;
				}
				using (IEnumerator<IAgent> enumerator = this.ConversationAgents.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						IAgent agent2 = enumerator.Current;
						if (sentence.IsListener(agent2))
						{
							this.SetListenerAgent(agent2);
							break;
						}
					}
					return;
				}
			}
			this.SetListenerAgent((!sentence.IsPlayer) ? this._mainAgent : this.ConversationAgents[0]);
		}

		// Token: 0x060021FD RID: 8701 RVA: 0x00096298 File Offset: 0x00094498
		private void SetSpeakerAgent(IAgent agent)
		{
			if (this._speakerAgent != agent)
			{
				this._speakerAgent = agent;
				if (this._speakerAgent != null && this._speakerAgent.Character is CharacterObject)
				{
					StringHelpers.SetCharacterProperties("SPEAKER", agent.Character as CharacterObject, null, false);
				}
			}
		}

		// Token: 0x060021FE RID: 8702 RVA: 0x000962E8 File Offset: 0x000944E8
		private void SetListenerAgent(IAgent agent)
		{
			if (this._listenerAgent != agent)
			{
				this._listenerAgent = agent;
				if (this._listenerAgent != null && this._listenerAgent.Character is CharacterObject)
				{
					StringHelpers.SetCharacterProperties("LISTENER", agent.Character as CharacterObject, null, false);
				}
			}
		}

		// Token: 0x060021FF RID: 8703 RVA: 0x00096338 File Offset: 0x00094538
		public void UpdateCurrentSentenceText()
		{
			TextObject currentSentenceText;
			if (this._currentSentence >= 0)
			{
				currentSentenceText = this._sentences[this._currentSentence].Text;
			}
			else
			{
				if (Campaign.Current == null)
				{
					throw new MBNullParameterException("Campaign");
				}
				currentSentenceText = GameTexts.FindText("str_error_string", null);
			}
			this._currentSentenceText = currentSentenceText;
		}

		// Token: 0x06002200 RID: 8704 RVA: 0x0009638C File Offset: 0x0009458C
		public bool IsConversationEnded()
		{
			return this.ActiveToken == 4;
		}

		// Token: 0x06002201 RID: 8705 RVA: 0x00096397 File Offset: 0x00094597
		public void ClearCurrentOptions()
		{
			if (this.CurOptions == null)
			{
				this.CurOptions = new List<ConversationSentenceOption>();
			}
			this.CurOptions.Clear();
		}

		// Token: 0x06002202 RID: 8706 RVA: 0x000963B8 File Offset: 0x000945B8
		public void AddToCurrentOptions(TextObject text, string id, bool isClickable, TextObject hintText)
		{
			ConversationSentenceOption item = new ConversationSentenceOption
			{
				SentenceNo = 0,
				Text = text,
				Id = id,
				RepeatObject = null,
				DebugInfo = null,
				IsClickable = isClickable,
				HintText = hintText
			};
			this.CurOptions.Add(item);
		}

		// Token: 0x06002203 RID: 8707 RVA: 0x00096414 File Offset: 0x00094614
		public void GetPlayerSentenceOptions()
		{
			this.CurOptions = this.GetSentenceOptions(true, true);
			if (this.CurOptions.Count > 0)
			{
				ConversationSentenceOption conversationSentenceOption = this.CurOptions[0];
				foreach (ConversationSentenceOption conversationSentenceOption2 in this.CurOptions)
				{
					if (this._sentences[conversationSentenceOption2.SentenceNo].IsListener != null)
					{
						conversationSentenceOption = conversationSentenceOption2;
						break;
					}
				}
				this.UpdateSpeakerAndListenerAgents(this._sentences[conversationSentenceOption.SentenceNo]);
			}
		}

		// Token: 0x06002204 RID: 8708 RVA: 0x000964BC File Offset: 0x000946BC
		public int GetStateIndex(string str)
		{
			int result;
			if (this.stateMap.ContainsKey(str))
			{
				result = this.stateMap[str];
			}
			else
			{
				result = this._numberOfStateIndices;
				Dictionary<string, int> dictionary = this.stateMap;
				int numberOfStateIndices = this._numberOfStateIndices;
				this._numberOfStateIndices = numberOfStateIndices + 1;
				dictionary.Add(str, numberOfStateIndices);
			}
			return result;
		}

		// Token: 0x06002205 RID: 8709 RVA: 0x0009650B File Offset: 0x0009470B
		internal void Build()
		{
			this.SortSentences();
		}

		// Token: 0x06002206 RID: 8710 RVA: 0x00096513 File Offset: 0x00094713
		public void DisableSentenceSort()
		{
			this._sortSentenceIsDisabled = true;
		}

		// Token: 0x06002207 RID: 8711 RVA: 0x0009651C File Offset: 0x0009471C
		public void EnableSentenceSort()
		{
			this._sortSentenceIsDisabled = false;
			this.SortSentences();
		}

		// Token: 0x06002208 RID: 8712 RVA: 0x0009652B File Offset: 0x0009472B
		private void SortSentences()
		{
			this._sentences = (from pair in this._sentences
				orderby pair.Priority descending
				select pair).ToList<ConversationSentence>();
		}

		// Token: 0x06002209 RID: 8713 RVA: 0x00096564 File Offset: 0x00094764
		private void SortLastSentence()
		{
			int num = this._sentences.Count - 1;
			ConversationSentence conversationSentence = this._sentences[num];
			int priority = conversationSentence.Priority;
			int num2 = num - 1;
			while (num2 >= 0 && this._sentences[num2].Priority < priority)
			{
				this._sentences[num2 + 1] = this._sentences[num2];
				num = num2;
				num2--;
			}
			this._sentences[num] = conversationSentence;
			if (this.CurOptions != null)
			{
				for (int i = 0; i < this.CurOptions.Count; i++)
				{
					if (this.CurOptions[i].SentenceNo >= num)
					{
						ConversationSentenceOption value = this.CurOptions[i];
						value.SentenceNo = this.CurOptions[i].SentenceNo + 1;
						this.CurOptions[i] = value;
					}
				}
			}
		}

		// Token: 0x0600220A RID: 8714 RVA: 0x00096650 File Offset: 0x00094850
		private List<ConversationSentenceOption> GetSentenceOptions(bool onlyPlayer, bool processAfterOneOption)
		{
			List<ConversationSentenceOption> list = new List<ConversationSentenceOption>();
			ConversationManager.SetupTextVariables();
			for (int i = 0; i < this._sentences.Count; i++)
			{
				if (this.GetSentenceMatch(i, onlyPlayer))
				{
					ConversationSentence conversationSentence = this._sentences[i];
					int num = 1;
					this._dialogRepeatLines.Clear();
					this._currentRepeatIndex = 0;
					if (conversationSentence.IsRepeatable)
					{
						num = this.DialogRepeatCount;
					}
					for (int j = 0; j < num; j++)
					{
						this._dialogRepeatLines.Add(conversationSentence.Text.CopyTextObject());
						if (conversationSentence.RunCondition())
						{
							conversationSentence.IsClickable = conversationSentence.RunClickableCondition();
							if (conversationSentence.IsWithVariation)
							{
								TextObject content = this.FindMatchingTextOrNull(conversationSentence.Id, this.OneToOneConversationCharacter);
								GameTexts.SetVariable("VARIATION_TEXT_TAGGED_LINE", content);
							}
							string debugInfo = (conversationSentence.IsPlayer ? "P  -> (" : "AI -> (") + conversationSentence.Id + ") - ";
							ConversationSentenceOption item = new ConversationSentenceOption
							{
								SentenceNo = i,
								Text = this.GetCurrentDialogLine(),
								Id = conversationSentence.Id,
								RepeatObject = this.GetCurrentProcessedRepeatObject(),
								DebugInfo = debugInfo,
								IsClickable = conversationSentence.IsClickable,
								HasPersuasion = conversationSentence.HasPersuasion,
								SkillName = conversationSentence.SkillName,
								TraitName = conversationSentence.TraitName,
								IsSpecial = conversationSentence.IsSpecial,
								IsUsedOnce = conversationSentence.IsUsedOnce,
								HintText = conversationSentence.HintText,
								PersuationOptionArgs = conversationSentence.PersuationOptionArgs
							};
							list.Add(item);
							if (conversationSentence.IsRepeatable)
							{
								this._currentRepeatIndex++;
							}
							if (!processAfterOneOption)
							{
								return list;
							}
						}
					}
				}
			}
			return list;
		}

		// Token: 0x0600220B RID: 8715 RVA: 0x0009682C File Offset: 0x00094A2C
		private bool GetSentenceMatch(int sentenceIndex, bool onlyPlayer)
		{
			if (0 > sentenceIndex || sentenceIndex >= this._sentences.Count)
			{
				throw new MBOutOfRangeException("Sentence index is not valid.");
			}
			bool flag = this._sentences[sentenceIndex].InputToken != this.ActiveToken;
			if (!flag && onlyPlayer)
			{
				flag = !this._sentences[sentenceIndex].IsPlayer;
			}
			if (!flag)
			{
				flag = this._sentences[sentenceIndex].IsUsedOnce && this._usedIndices.Contains(this._sentences[sentenceIndex].Index);
			}
			return !flag;
		}

		// Token: 0x0600220C RID: 8716 RVA: 0x000968CA File Offset: 0x00094ACA
		internal object GetCurrentProcessedRepeatObject()
		{
			if (this._dialogRepeatObjects.Count <= 0)
			{
				return null;
			}
			return this._dialogRepeatObjects[this._currentRepeatedDialogSetIndex][this._currentRepeatIndex];
		}

		// Token: 0x0600220D RID: 8717 RVA: 0x000968F8 File Offset: 0x00094AF8
		internal TextObject GetCurrentDialogLine()
		{
			if (this._dialogRepeatLines.Count <= this._currentRepeatIndex)
			{
				return null;
			}
			return this._dialogRepeatLines[this._currentRepeatIndex];
		}

		// Token: 0x0600220E RID: 8718 RVA: 0x00096920 File Offset: 0x00094B20
		internal object GetSelectedRepeatObject()
		{
			return this._lastSelectedDialogObject;
		}

		// Token: 0x0600220F RID: 8719 RVA: 0x00096928 File Offset: 0x00094B28
		internal void SetDialogRepeatCount(IReadOnlyList<object> dialogRepeatObjects, int maxRepeatedDialogsInConversation)
		{
			this._dialogRepeatObjects.Clear();
			bool flag = dialogRepeatObjects.Count > maxRepeatedDialogsInConversation + 1;
			List<object> list = new List<object>(maxRepeatedDialogsInConversation);
			for (int i = 0; i < dialogRepeatObjects.Count; i++)
			{
				object item = dialogRepeatObjects[i];
				if (flag && i % maxRepeatedDialogsInConversation == 0)
				{
					list = new List<object>(maxRepeatedDialogsInConversation);
					this._dialogRepeatObjects.Add(list);
				}
				list.Add(item);
			}
			if (!flag && !list.IsEmpty<object>())
			{
				this._dialogRepeatObjects.Add(list);
			}
			this._currentRepeatedDialogSetIndex = 0;
			this._currentRepeatIndex = 0;
		}

		// Token: 0x06002210 RID: 8720 RVA: 0x000969B4 File Offset: 0x00094BB4
		internal static void DialogRepeatContinueListing()
		{
			Campaign campaign = Campaign.Current;
			ConversationManager conversationManager = ((campaign != null) ? campaign.ConversationManager : null);
			if (conversationManager != null)
			{
				conversationManager._currentRepeatedDialogSetIndex++;
				if (conversationManager._currentRepeatedDialogSetIndex >= conversationManager._dialogRepeatObjects.Count)
				{
					conversationManager._currentRepeatedDialogSetIndex = 0;
				}
				conversationManager._currentRepeatIndex = 0;
			}
		}

		// Token: 0x06002211 RID: 8721 RVA: 0x00096A08 File Offset: 0x00094C08
		internal static bool IsThereMultipleRepeatablePages()
		{
			Campaign campaign = Campaign.Current;
			if (campaign == null)
			{
				return false;
			}
			ConversationManager conversationManager = campaign.ConversationManager;
			int? num = ((conversationManager != null) ? new int?(conversationManager._dialogRepeatObjects.Count) : null);
			int num2 = 1;
			return (num.GetValueOrDefault() > num2) & (num != null);
		}

		// Token: 0x06002212 RID: 8722 RVA: 0x00096A58 File Offset: 0x00094C58
		private void ResetRepeatedDialogSystem()
		{
			this._currentRepeatedDialogSetIndex = 0;
			this._currentRepeatIndex = 0;
			this._dialogRepeatObjects.Clear();
			this._dialogRepeatLines.Clear();
		}

		// Token: 0x06002213 RID: 8723 RVA: 0x00096A7E File Offset: 0x00094C7E
		internal ConversationSentence AddDialogLine(ConversationSentence dialogLine)
		{
			this._sentences.Add(dialogLine);
			if (!this._sortSentenceIsDisabled)
			{
				this.SortLastSentence();
			}
			return dialogLine;
		}

		// Token: 0x06002214 RID: 8724 RVA: 0x00096A9C File Offset: 0x00094C9C
		public void AddDialogFlow(DialogFlow dialogFlow, object relatedObject = null)
		{
			foreach (DialogFlowLine dialogFlowLine in dialogFlow.Lines)
			{
				string text = this.CreateId();
				uint flags = (dialogFlowLine.ByPlayer ? 1U : 0U) | (dialogFlowLine.IsRepeatable ? 2U : 0U) | (dialogFlowLine.IsSpecialOption ? 4U : 0U) | (dialogFlowLine.IsUsedOnce ? 8U : 0U);
				this.AddDialogLine(new ConversationSentence(text, dialogFlowLine.HasVariation ? new TextObject("{=!}{VARIATION_TEXT_TAGGED_LINE}", null) : dialogFlowLine.Text, dialogFlowLine.InputToken, dialogFlowLine.OutputToken, dialogFlowLine.ConditionDelegate, dialogFlowLine.ClickableConditionDelegate, dialogFlowLine.ConsequenceDelegate, flags, dialogFlow.Priority, 0, 0, relatedObject, dialogFlowLine.HasVariation, dialogFlowLine.SpeakerDelegate, dialogFlowLine.ListenerDelegate, null));
				GameText gameText = Game.Current.GameTextManager.AddGameText(text);
				foreach (KeyValuePair<TextObject, List<GameTextManager.ChoiceTag>> keyValuePair in dialogFlowLine.Variations)
				{
					gameText.AddVariationWithId("", keyValuePair.Key, keyValuePair.Value);
				}
			}
		}

		// Token: 0x06002215 RID: 8725 RVA: 0x00096C14 File Offset: 0x00094E14
		public ConversationSentence AddDialogLineMultiAgent(string id, string inputToken, string outputToken, TextObject text, ConversationSentence.OnConditionDelegate conditionDelegate, ConversationSentence.OnConsequenceDelegate consequenceDelegate, int agentIndex, int nextAgentIndex, int priority = 100, ConversationSentence.OnClickableConditionDelegate clickableConditionDelegate = null)
		{
			return this.AddDialogLine(new ConversationSentence(id, text, inputToken, outputToken, conditionDelegate, clickableConditionDelegate, consequenceDelegate, 0U, priority, agentIndex, nextAgentIndex, null, false, null, null, null));
		}

		// Token: 0x06002216 RID: 8726 RVA: 0x00096C43 File Offset: 0x00094E43
		internal string CreateToken()
		{
			string result = string.Format("atk:{0}", this._autoToken);
			this._autoToken++;
			return result;
		}

		// Token: 0x06002217 RID: 8727 RVA: 0x00096C68 File Offset: 0x00094E68
		private string CreateId()
		{
			string result = string.Format("adg:{0}", this._autoId);
			this._autoId++;
			return result;
		}

		// Token: 0x06002218 RID: 8728 RVA: 0x00096C8D File Offset: 0x00094E8D
		internal void SetupGameStringsForConversation()
		{
			StringHelpers.SetCharacterProperties("PLAYER", Hero.MainHero.CharacterObject, null, false);
		}

		// Token: 0x06002219 RID: 8729 RVA: 0x00096CA6 File Offset: 0x00094EA6
		internal void OnConsequence(ConversationSentence sentence)
		{
			Action<ConversationSentence> consequenceRunned = this.ConsequenceRunned;
			if (consequenceRunned == null)
			{
				return;
			}
			consequenceRunned(sentence);
		}

		// Token: 0x0600221A RID: 8730 RVA: 0x00096CB9 File Offset: 0x00094EB9
		internal void OnCondition(ConversationSentence sentence)
		{
			Action<ConversationSentence> conditionRunned = this.ConditionRunned;
			if (conditionRunned == null)
			{
				return;
			}
			conditionRunned(sentence);
		}

		// Token: 0x0600221B RID: 8731 RVA: 0x00096CCC File Offset: 0x00094ECC
		internal void OnClickableCondition(ConversationSentence sentence)
		{
			Action<ConversationSentence> clickableConditionRunned = this.ClickableConditionRunned;
			if (clickableConditionRunned == null)
			{
				return;
			}
			clickableConditionRunned(sentence);
		}

		// Token: 0x14000008 RID: 8
		// (add) Token: 0x0600221C RID: 8732 RVA: 0x00096CE0 File Offset: 0x00094EE0
		// (remove) Token: 0x0600221D RID: 8733 RVA: 0x00096D18 File Offset: 0x00094F18
		public event Action<ConversationSentence> ConsequenceRunned;

		// Token: 0x14000009 RID: 9
		// (add) Token: 0x0600221E RID: 8734 RVA: 0x00096D50 File Offset: 0x00094F50
		// (remove) Token: 0x0600221F RID: 8735 RVA: 0x00096D88 File Offset: 0x00094F88
		public event Action<ConversationSentence> ConditionRunned;

		// Token: 0x1400000A RID: 10
		// (add) Token: 0x06002220 RID: 8736 RVA: 0x00096DC0 File Offset: 0x00094FC0
		// (remove) Token: 0x06002221 RID: 8737 RVA: 0x00096DF8 File Offset: 0x00094FF8
		public event Action<ConversationSentence> ClickableConditionRunned;

		// Token: 0x17000869 RID: 2153
		// (get) Token: 0x06002222 RID: 8738 RVA: 0x00096E2D File Offset: 0x0009502D
		public IReadOnlyList<IAgent> ConversationAgents
		{
			get
			{
				return this._conversationAgents;
			}
		}

		// Token: 0x1700086A RID: 2154
		// (get) Token: 0x06002223 RID: 8739 RVA: 0x00096E35 File Offset: 0x00095035
		public IAgent OneToOneConversationAgent
		{
			get
			{
				if (this.ConversationAgents.IsEmpty<IAgent>() || this.ConversationAgents.Count > 1)
				{
					return null;
				}
				return this.ConversationAgents[0];
			}
		}

		// Token: 0x1700086B RID: 2155
		// (get) Token: 0x06002224 RID: 8740 RVA: 0x00096E60 File Offset: 0x00095060
		public IAgent SpeakerAgent
		{
			get
			{
				if (this.ConversationAgents != null)
				{
					return this._speakerAgent;
				}
				return null;
			}
		}

		// Token: 0x1700086C RID: 2156
		// (get) Token: 0x06002225 RID: 8741 RVA: 0x00096E72 File Offset: 0x00095072
		public IAgent ListenerAgent
		{
			get
			{
				if (this.ConversationAgents != null)
				{
					return this._listenerAgent;
				}
				return null;
			}
		}

		// Token: 0x1700086D RID: 2157
		// (get) Token: 0x06002226 RID: 8742 RVA: 0x00096E84 File Offset: 0x00095084
		// (set) Token: 0x06002227 RID: 8743 RVA: 0x00096E8C File Offset: 0x0009508C
		public bool IsConversationInProgress { get; private set; }

		// Token: 0x1700086E RID: 2158
		// (get) Token: 0x06002228 RID: 8744 RVA: 0x00096E95 File Offset: 0x00095095
		public Hero OneToOneConversationHero
		{
			get
			{
				if (this.OneToOneConversationCharacter != null)
				{
					return this.OneToOneConversationCharacter.HeroObject;
				}
				return null;
			}
		}

		// Token: 0x1700086F RID: 2159
		// (get) Token: 0x06002229 RID: 8745 RVA: 0x00096EAC File Offset: 0x000950AC
		public CharacterObject OneToOneConversationCharacter
		{
			get
			{
				if (this.OneToOneConversationAgent != null)
				{
					return (CharacterObject)this.OneToOneConversationAgent.Character;
				}
				return null;
			}
		}

		// Token: 0x17000870 RID: 2160
		// (get) Token: 0x0600222A RID: 8746 RVA: 0x00096EC8 File Offset: 0x000950C8
		public IEnumerable<CharacterObject> ConversationCharacters
		{
			get
			{
				new List<CharacterObject>();
				foreach (IAgent agent in this.ConversationAgents)
				{
					yield return (CharacterObject)agent.Character;
				}
				IEnumerator<IAgent> enumerator = null;
				yield break;
				yield break;
			}
		}

		// Token: 0x0600222B RID: 8747 RVA: 0x00096ED8 File Offset: 0x000950D8
		public bool IsAgentInConversation(IAgent agent)
		{
			return this.ConversationAgents.Contains(agent);
		}

		// Token: 0x17000871 RID: 2161
		// (get) Token: 0x0600222C RID: 8748 RVA: 0x00096EE6 File Offset: 0x000950E6
		public MobileParty ConversationParty
		{
			get
			{
				return this._conversationParty;
			}
		}

		// Token: 0x17000872 RID: 2162
		// (get) Token: 0x0600222D RID: 8749 RVA: 0x00096EEE File Offset: 0x000950EE
		// (set) Token: 0x0600222E RID: 8750 RVA: 0x00096EF6 File Offset: 0x000950F6
		public bool NeedsToActivateForMapConversation { get; private set; }

		// Token: 0x1400000B RID: 11
		// (add) Token: 0x0600222F RID: 8751 RVA: 0x00096F00 File Offset: 0x00095100
		// (remove) Token: 0x06002230 RID: 8752 RVA: 0x00096F38 File Offset: 0x00095138
		public event Action ConversationSetup;

		// Token: 0x1400000C RID: 12
		// (add) Token: 0x06002231 RID: 8753 RVA: 0x00096F70 File Offset: 0x00095170
		// (remove) Token: 0x06002232 RID: 8754 RVA: 0x00096FA8 File Offset: 0x000951A8
		public event Action ConversationBegin;

		// Token: 0x1400000D RID: 13
		// (add) Token: 0x06002233 RID: 8755 RVA: 0x00096FE0 File Offset: 0x000951E0
		// (remove) Token: 0x06002234 RID: 8756 RVA: 0x00097018 File Offset: 0x00095218
		public event Action ConversationEnd;

		// Token: 0x1400000E RID: 14
		// (add) Token: 0x06002235 RID: 8757 RVA: 0x00097050 File Offset: 0x00095250
		// (remove) Token: 0x06002236 RID: 8758 RVA: 0x00097088 File Offset: 0x00095288
		public event Action ConversationEndOneShot;

		// Token: 0x1400000F RID: 15
		// (add) Token: 0x06002237 RID: 8759 RVA: 0x000970C0 File Offset: 0x000952C0
		// (remove) Token: 0x06002238 RID: 8760 RVA: 0x000970F8 File Offset: 0x000952F8
		public event Action ConversationContinued;

		// Token: 0x06002239 RID: 8761 RVA: 0x0009712D File Offset: 0x0009532D
		private void SetupConversation()
		{
			this.IsConversationInProgress = true;
			IConversationStateHandler handler = this.Handler;
			if (handler == null)
			{
				return;
			}
			handler.OnConversationInstall();
		}

		// Token: 0x0600223A RID: 8762 RVA: 0x00097146 File Offset: 0x00095346
		public void BeginConversation()
		{
			this.IsConversationInProgress = true;
			if (this.ConversationSetup != null)
			{
				this.ConversationSetup();
			}
			if (this.ConversationBegin != null)
			{
				this.ConversationBegin();
			}
			this.NeedsToActivateForMapConversation = false;
		}

		// Token: 0x0600223B RID: 8763 RVA: 0x0009717C File Offset: 0x0009537C
		public void EndConversation()
		{
			Debug.Print("--------------- Conversation End --------------- ", 0, Debug.DebugColor.White, 4503599627370496UL);
			if (CampaignMission.Current != null)
			{
				foreach (IAgent agent in this.ConversationAgents)
				{
					CampaignMission.Current.OnConversationEnd(agent);
				}
			}
			this._conversationParty = null;
			if (this.ConversationEndOneShot != null)
			{
				this.ConversationEndOneShot();
				this.ConversationEndOneShot = null;
			}
			if (this.ConversationEnd != null)
			{
				this.ConversationEnd();
			}
			this.IsConversationInProgress = false;
			foreach (IAgent agent2 in this.ConversationAgents)
			{
				agent2.SetAsConversationAgent(false);
			}
			Campaign.Current.CurrentConversationContext = ConversationContext.Default;
			CampaignEventDispatcher.Instance.OnConversationEnded(this.ConversationCharacters);
			if (ConversationManager.GetPersuasionIsActive())
			{
				ConversationManager.EndPersuasion();
			}
			this._conversationAgents.Clear();
			this._speakerAgent = null;
			this._listenerAgent = null;
			this._mainAgent = null;
			if (this.IsConversationFlowActive)
			{
				this.OnConversationDeactivate();
			}
			IConversationStateHandler handler = this.Handler;
			if (handler == null)
			{
				return;
			}
			handler.OnConversationUninstall();
		}

		// Token: 0x0600223C RID: 8764 RVA: 0x000972C4 File Offset: 0x000954C4
		public void DoOption(int optionIndex)
		{
			this.LastSelectedButtonIndex = optionIndex;
			this.ProcessSentence(this.CurOptions[optionIndex]);
			if (this._isActive)
			{
				this.DoOptionContinue();
				return;
			}
			this._executeDoOptionContinue = true;
		}

		// Token: 0x0600223D RID: 8765 RVA: 0x000972F8 File Offset: 0x000954F8
		public void DoOption(string optionID)
		{
			int count = Campaign.Current.ConversationManager.CurOptions.Count;
			for (int i = 0; i < count; i++)
			{
				if (this.CurOptions[i].Id == optionID)
				{
					this.DoOption(i);
					return;
				}
			}
		}

		// Token: 0x0600223E RID: 8766 RVA: 0x00097347 File Offset: 0x00095547
		public void DoConversationContinuedCallback()
		{
			if (this.ConversationContinued != null)
			{
				this.ConversationContinued();
			}
		}

		// Token: 0x0600223F RID: 8767 RVA: 0x0009735C File Offset: 0x0009555C
		public void DoOptionContinue()
		{
			if (this.IsConversationEnded() && this._sentences[this._currentSentence].IsPlayer)
			{
				this.EndConversation();
				return;
			}
			this.ProcessPartnerSentence();
			this.DoConversationContinuedCallback();
		}

		// Token: 0x06002240 RID: 8768 RVA: 0x00097394 File Offset: 0x00095594
		public void ContinueConversation()
		{
			if (this.CurOptions.Count <= 1)
			{
				if (this.IsConversationEnded())
				{
					this.EndConversation();
					return;
				}
				if (!this.ProcessPartnerSentence() && this.ListenerAgent.Character == Hero.MainHero.CharacterObject)
				{
					this.EndConversation();
					return;
				}
				this.DoConversationContinuedCallback();
				if (CampaignMission.Current != null)
				{
					CampaignMission.Current.OnConversationContinue();
				}
			}
		}

		// Token: 0x06002241 RID: 8769 RVA: 0x000973FC File Offset: 0x000955FC
		public void SetupAndStartMissionConversation(IAgent agent, IAgent mainAgent, bool setActionsInstantly)
		{
			this.SetupConversation();
			this._mainAgent = mainAgent;
			this._conversationAgents.Clear();
			this.AddConversationAgent(agent);
			this._conversationParty = null;
			this.StartNew(0, setActionsInstantly);
			if (!this.IsConversationFlowActive)
			{
				this.OnConversationActivate();
			}
			this.BeginConversation();
		}

		// Token: 0x06002242 RID: 8770 RVA: 0x0009744C File Offset: 0x0009564C
		public void SetupAndStartMissionConversationWithMultipleAgents(IEnumerable<IAgent> agents, IAgent mainAgent)
		{
			this.SetupConversation();
			this._mainAgent = mainAgent;
			this._conversationAgents.Clear();
			this.AddConversationAgents(agents, true);
			this._conversationParty = null;
			this.StartNew(0, true);
			if (!this.IsConversationFlowActive)
			{
				this.OnConversationActivate();
			}
			this.BeginConversation();
		}

		// Token: 0x06002243 RID: 8771 RVA: 0x0009749C File Offset: 0x0009569C
		public void SetupAndStartMapConversation(MobileParty party, IAgent agent, IAgent mainAgent)
		{
			this._conversationParty = party;
			this._mainAgent = mainAgent;
			this._conversationAgents.Clear();
			this.AddConversationAgent(agent);
			this.SetupConversation();
			this.StartNew(0, true);
			this.NeedsToActivateForMapConversation = true;
			if (!this.IsConversationFlowActive)
			{
				this.OnConversationActivate();
			}
		}

		// Token: 0x06002244 RID: 8772 RVA: 0x000974EC File Offset: 0x000956EC
		public void AddConversationAgents(IEnumerable<IAgent> agents, bool setActionsInstantly)
		{
			foreach (IAgent agent in agents)
			{
				if (agent.IsActive() && !this.ConversationAgents.Contains(agent))
				{
					this.AddConversationAgent(agent);
					CampaignMission.Current.OnConversationStart(agent, setActionsInstantly);
				}
			}
		}

		// Token: 0x06002245 RID: 8773 RVA: 0x00097558 File Offset: 0x00095758
		public void RemoveConversationAgent(IAgent agent)
		{
			if (agent.IsActive() && this.ConversationAgents.Contains(agent) && this.ConversationAgents.Count > 1)
			{
				CampaignMission.Current.OnConversationEnd(agent);
				agent.SetAsConversationAgent(false);
				this._conversationAgents.Remove(agent);
				return;
			}
			Debug.FailedAssert("Failed to remove conversation agent.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Conversation\\ConversationManager.cs", "RemoveConversationAgent", 1247);
		}

		// Token: 0x06002246 RID: 8774 RVA: 0x000975C2 File Offset: 0x000957C2
		private void AddConversationAgent(IAgent agent)
		{
			this._conversationAgents.Add(agent);
			agent.SetAsConversationAgent(true);
			CampaignEventDispatcher.Instance.OnAgentJoinedConversation(agent);
		}

		// Token: 0x06002247 RID: 8775 RVA: 0x000975E2 File Offset: 0x000957E2
		public bool IsConversationAgent(IAgent agent)
		{
			return this.ConversationAgents != null && this.ConversationAgents.Contains(agent);
		}

		// Token: 0x06002248 RID: 8776 RVA: 0x000975FC File Offset: 0x000957FC
		public void RemoveRelatedLines(object o)
		{
			this._sentences.RemoveAll((ConversationSentence s) => s.RelatedObject == o);
		}

		// Token: 0x17000873 RID: 2163
		// (get) Token: 0x06002249 RID: 8777 RVA: 0x0009762E File Offset: 0x0009582E
		// (set) Token: 0x0600224A RID: 8778 RVA: 0x00097636 File Offset: 0x00095836
		public IConversationStateHandler Handler { get; set; }

		// Token: 0x0600224B RID: 8779 RVA: 0x0009763F File Offset: 0x0009583F
		public void OnConversationDeactivate()
		{
			this._isActive = false;
			IConversationStateHandler handler = this.Handler;
			if (handler == null)
			{
				return;
			}
			handler.OnConversationDeactivate();
		}

		// Token: 0x0600224C RID: 8780 RVA: 0x00097658 File Offset: 0x00095858
		public void OnConversationActivate()
		{
			this._isActive = true;
			if (this._executeDoOptionContinue)
			{
				this._executeDoOptionContinue = false;
				this.DoOptionContinue();
			}
			IConversationStateHandler handler = this.Handler;
			if (handler == null)
			{
				return;
			}
			handler.OnConversationActivate();
		}

		// Token: 0x0600224D RID: 8781 RVA: 0x00097688 File Offset: 0x00095888
		public TextObject FindMatchingTextOrNull(string id, CharacterObject character)
		{
			float num = -2.1474836E+09f;
			TextObject result = null;
			GameText gameText = Game.Current.GameTextManager.GetGameText(id);
			if (gameText != null)
			{
				foreach (GameText.GameTextVariation gameTextVariation in gameText.Variations)
				{
					float num2 = this.FindMatchingScore(character, gameTextVariation.Tags);
					if (num2 > num)
					{
						result = gameTextVariation.Text;
						num = num2;
					}
				}
			}
			return result;
		}

		// Token: 0x0600224E RID: 8782 RVA: 0x0009770C File Offset: 0x0009590C
		private float FindMatchingScore(CharacterObject character, GameTextManager.ChoiceTag[] choiceTags)
		{
			float num = 0f;
			foreach (GameTextManager.ChoiceTag choiceTag in choiceTags)
			{
				if (choiceTag.TagName != "DefaultTag")
				{
					if (this.IsTagApplicable(choiceTag.TagName, character) == choiceTag.IsTagReversed)
					{
						return -2.1474836E+09f;
					}
					uint weight = choiceTag.Weight;
					num += weight;
				}
			}
			return num;
		}

		// Token: 0x0600224F RID: 8783 RVA: 0x00097778 File Offset: 0x00095978
		private void InitializeTags()
		{
			this._tags = new Dictionary<string, ConversationTag>();
			string name = typeof(ConversationTag).Assembly.GetName().Name;
			foreach (Assembly assembly in ModuleHelper.GetActiveGameAssemblies())
			{
				bool flag = false;
				if (name == assembly.GetName().Name)
				{
					flag = true;
				}
				else
				{
					AssemblyName[] referencedAssemblies = assembly.GetReferencedAssemblies();
					for (int i = 0; i < referencedAssemblies.Length; i++)
					{
						if (referencedAssemblies[i].Name == name)
						{
							flag = true;
							break;
						}
					}
				}
				if (flag)
				{
					foreach (Type type in assembly.GetTypesSafe(null))
					{
						if (type.IsSubclassOf(typeof(ConversationTag)))
						{
							ConversationTag conversationTag = Activator.CreateInstance(type) as ConversationTag;
							this._tags.Add(conversationTag.StringId, conversationTag);
						}
					}
				}
			}
		}

		// Token: 0x06002250 RID: 8784 RVA: 0x000978B0 File Offset: 0x00095AB0
		private static void SetupTextVariables()
		{
			StringHelpers.SetCharacterProperties("PLAYER", Hero.MainHero.CharacterObject, null, false);
			int num = 1;
			foreach (CharacterObject character in CharacterObject.ConversationCharacters)
			{
				string str = ((num == 1) ? "" : ("_" + num));
				StringHelpers.SetCharacterProperties("CONVERSATION_CHARACTER" + str, character, null, false);
			}
			MBTextManager.SetTextVariable("CURRENT_SETTLEMENT_NAME", (Settlement.CurrentSettlement == null) ? TextObject.GetEmpty() : Settlement.CurrentSettlement.Name, false);
			ConversationHelper.ConversationTroopCommentShown = false;
		}

		// Token: 0x06002251 RID: 8785 RVA: 0x00097968 File Offset: 0x00095B68
		public IEnumerable<string> GetApplicableTagNames(CharacterObject character)
		{
			foreach (ConversationTag conversationTag in this._tags.Values)
			{
				if (conversationTag.IsApplicableTo(character))
				{
					yield return conversationTag.StringId;
				}
			}
			Dictionary<string, ConversationTag>.ValueCollection.Enumerator enumerator = default(Dictionary<string, ConversationTag>.ValueCollection.Enumerator);
			yield break;
			yield break;
		}

		// Token: 0x06002252 RID: 8786 RVA: 0x00097980 File Offset: 0x00095B80
		public bool IsTagApplicable(string tagId, CharacterObject character)
		{
			ConversationTag conversationTag;
			if (this._tags.TryGetValue(tagId, out conversationTag))
			{
				return conversationTag.IsApplicableTo(character);
			}
			Debug.FailedAssert("Asking for a nonexistent tag: " + tagId, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Conversation\\ConversationManager.cs", "IsTagApplicable", 1482);
			return false;
		}

		// Token: 0x06002253 RID: 8787 RVA: 0x000979C8 File Offset: 0x00095BC8
		public void OpenMapConversation(ConversationCharacterData playerCharacterData, ConversationCharacterData conversationPartnerData)
		{
			GameStateManager gameStateManager = GameStateManager.Current;
			(((gameStateManager != null) ? gameStateManager.ActiveState : null) as MapState).OnMapConversationStarts(playerCharacterData, conversationPartnerData);
			PartyBase party = conversationPartnerData.Party;
			this.SetupAndStartMapConversation((party != null) ? party.MobileParty : null, new MapConversationAgent(conversationPartnerData.Character), new MapConversationAgent(CharacterObject.PlayerCharacter));
		}

		// Token: 0x06002254 RID: 8788 RVA: 0x00097A1F File Offset: 0x00095C1F
		public static void StartPersuasion(float goalValue, float successValue, float failValue, float criticalSuccessValue, float criticalFailValue, float initialProgress = -1f, PersuasionDifficulty difficulty = PersuasionDifficulty.Medium)
		{
			ConversationManager._persuasion = new Persuasion(goalValue, successValue, failValue, criticalSuccessValue, criticalFailValue, initialProgress, difficulty);
		}

		// Token: 0x06002255 RID: 8789 RVA: 0x00097A35 File Offset: 0x00095C35
		public static void EndPersuasion()
		{
			ConversationManager._persuasion = null;
		}

		// Token: 0x06002256 RID: 8790 RVA: 0x00097A3D File Offset: 0x00095C3D
		public static void PersuasionCommitProgress(PersuasionOptionArgs persuasionOptionArgs)
		{
			ConversationManager._persuasion.CommitProgress(persuasionOptionArgs);
		}

		// Token: 0x06002257 RID: 8791 RVA: 0x00097A4A File Offset: 0x00095C4A
		public static void Clear()
		{
			ConversationManager._persuasion = null;
		}

		// Token: 0x06002258 RID: 8792 RVA: 0x00097A52 File Offset: 0x00095C52
		public void GetPersuasionChanceValues(out float successValue, out float critSuccessValue, out float critFailValue)
		{
			successValue = ConversationManager._persuasion.SuccessValue;
			critSuccessValue = ConversationManager._persuasion.CriticalSuccessValue;
			critFailValue = ConversationManager._persuasion.CriticalFailValue;
		}

		// Token: 0x06002259 RID: 8793 RVA: 0x00097A78 File Offset: 0x00095C78
		public static bool GetPersuasionIsActive()
		{
			return ConversationManager._persuasion != null;
		}

		// Token: 0x0600225A RID: 8794 RVA: 0x00097A82 File Offset: 0x00095C82
		public static bool GetPersuasionProgressSatisfied()
		{
			return ConversationManager._persuasion.Progress >= ConversationManager._persuasion.GoalValue;
		}

		// Token: 0x0600225B RID: 8795 RVA: 0x00097A9D File Offset: 0x00095C9D
		public static bool GetPersuasionIsFailure()
		{
			return ConversationManager._persuasion.Progress < 0f;
		}

		// Token: 0x0600225C RID: 8796 RVA: 0x00097AB0 File Offset: 0x00095CB0
		public static float GetPersuasionProgress()
		{
			return ConversationManager._persuasion.Progress;
		}

		// Token: 0x0600225D RID: 8797 RVA: 0x00097ABC File Offset: 0x00095CBC
		public static float GetPersuasionGoalValue()
		{
			return ConversationManager._persuasion.GoalValue;
		}

		// Token: 0x0600225E RID: 8798 RVA: 0x00097AC8 File Offset: 0x00095CC8
		public static IEnumerable<Tuple<PersuasionOptionArgs, PersuasionOptionResult>> GetPersuasionChosenOptions()
		{
			return ConversationManager._persuasion.GetChosenOptions();
		}

		// Token: 0x0600225F RID: 8799 RVA: 0x00097AD4 File Offset: 0x00095CD4
		public void GetPersuasionChances(ConversationSentenceOption conversationSentenceOption, out float successChance, out float critSuccessChance, out float critFailChance, out float failChance)
		{
			ConversationSentence conversationSentence = this._sentences[conversationSentenceOption.SentenceNo];
			if (conversationSentenceOption.HasPersuasion)
			{
				Campaign.Current.Models.PersuasionModel.GetChances(conversationSentence.PersuationOptionArgs, out successChance, out critSuccessChance, out critFailChance, out failChance, ConversationManager._persuasion.DifficultyMultiplier);
				return;
			}
			successChance = 0f;
			critSuccessChance = 0f;
			critFailChance = 0f;
			failChance = 0f;
		}

		// Token: 0x040009F5 RID: 2549
		private int _currentRepeatedDialogSetIndex;

		// Token: 0x040009F6 RID: 2550
		private int _currentRepeatIndex;

		// Token: 0x040009F7 RID: 2551
		private int _autoId;

		// Token: 0x040009F8 RID: 2552
		private int _autoToken;

		// Token: 0x040009F9 RID: 2553
		private HashSet<int> _usedIndices = new HashSet<int>();

		// Token: 0x040009FA RID: 2554
		private int _numConversationSentencesCreated;

		// Token: 0x040009FB RID: 2555
		private List<ConversationSentence> _sentences;

		// Token: 0x040009FC RID: 2556
		private int _numberOfStateIndices;

		// Token: 0x040009FD RID: 2557
		public int ActiveToken;

		// Token: 0x040009FE RID: 2558
		private int _currentSentence;

		// Token: 0x040009FF RID: 2559
		private TextObject _currentSentenceText;

		// Token: 0x04000A00 RID: 2560
		public List<Tuple<string, CharacterObject>> DetailedDebugLog = new List<Tuple<string, CharacterObject>>();

		// Token: 0x04000A01 RID: 2561
		public string CurrentFaceAnimationRecord;

		// Token: 0x04000A02 RID: 2562
		private object _lastSelectedDialogObject;

		// Token: 0x04000A03 RID: 2563
		private readonly List<List<object>> _dialogRepeatObjects = new List<List<object>>();

		// Token: 0x04000A04 RID: 2564
		private readonly List<TextObject> _dialogRepeatLines = new List<TextObject>();

		// Token: 0x04000A05 RID: 2565
		private bool _isActive;

		// Token: 0x04000A06 RID: 2566
		private bool _executeDoOptionContinue;

		// Token: 0x04000A07 RID: 2567
		public int LastSelectedButtonIndex;

		// Token: 0x04000A08 RID: 2568
		public ConversationAnimationManager ConversationAnimationManager;

		// Token: 0x04000A09 RID: 2569
		private IAgent _mainAgent;

		// Token: 0x04000A0A RID: 2570
		private IAgent _speakerAgent;

		// Token: 0x04000A0B RID: 2571
		private IAgent _listenerAgent;

		// Token: 0x04000A0C RID: 2572
		private Dictionary<string, ConversationTag> _tags;

		// Token: 0x04000A0D RID: 2573
		private bool _sortSentenceIsDisabled;

		// Token: 0x04000A0E RID: 2574
		private Dictionary<string, int> stateMap;

		// Token: 0x04000A13 RID: 2579
		private List<IAgent> _conversationAgents = new List<IAgent>();

		// Token: 0x04000A15 RID: 2581
		public bool CurrentConversationIsFirst;

		// Token: 0x04000A16 RID: 2582
		private MobileParty _conversationParty;

		// Token: 0x04000A1E RID: 2590
		private static Persuasion _persuasion;

		// Token: 0x02000638 RID: 1592
		public class TaggedString
		{
			// Token: 0x0400194C RID: 6476
			public TextObject Text;

			// Token: 0x0400194D RID: 6477
			public List<GameTextManager.ChoiceTag> ChoiceTags = new List<GameTextManager.ChoiceTag>();

			// Token: 0x0400194E RID: 6478
			public int FacialAnimation;
		}
	}
}
