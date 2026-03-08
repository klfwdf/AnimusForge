using System;
using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Conversation.Persuasion;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Encyclopedia;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Conversation
{
	// Token: 0x02000118 RID: 280
	public class MissionConversationVM : ViewModel
	{
		// Token: 0x17000886 RID: 2182
		// (get) Token: 0x0600196F RID: 6511 RVA: 0x00060233 File Offset: 0x0005E433
		// (set) Token: 0x06001970 RID: 6512 RVA: 0x0006023B File Offset: 0x0005E43B
		public bool SelectedAnOptionOrLinkThisFrame { get; set; }

		// Token: 0x06001971 RID: 6513 RVA: 0x00060244 File Offset: 0x0005E444
		public MissionConversationVM(Func<string> getContinueInputText, bool isLinksDisabled = false)
		{
			this.AnswerList = new MBBindingList<ConversationItemVM>();
			this.AttackerParties = new MBBindingList<ConversationAggressivePartyItemVM>();
			this.DefenderParties = new MBBindingList<ConversationAggressivePartyItemVM>();
			this._conversationManager = Campaign.Current.ConversationManager;
			this._getContinueInputText = getContinueInputText;
			this._isLinksDisabled = isLinksDisabled;
			Input.OnGamepadActiveStateChanged = (Action)Delegate.Combine(Input.OnGamepadActiveStateChanged, new Action(this.RefreshValues));
			CampaignEvents.PersuasionProgressCommittedEvent.AddNonSerializedListener(this, new Action<Tuple<PersuasionOptionArgs, PersuasionOptionResult>>(this.OnPersuasionProgress));
			this.Persuasion = new PersuasionVM(this._conversationManager);
			if (this._conversationManager.SpeakerAgent != null && (CharacterObject)this._conversationManager.SpeakerAgent.Character != null && ((CharacterObject)this._conversationManager.SpeakerAgent.Character).IsHero && this._conversationManager.SpeakerAgent.Character != CharacterObject.PlayerCharacter)
			{
				Hero heroObject = ((CharacterObject)this._conversationManager.SpeakerAgent.Character).HeroObject;
				this.Relation = (int)heroObject.GetRelationWithPlayer();
			}
			this.IsAggressive = Campaign.Current.CurrentConversationContext == ConversationContext.PartyEncounter && this._conversationManager.ConversationParty != null && FactionManager.IsAtWarAgainstFaction(this._conversationManager.ConversationParty.MapFaction, Hero.MainHero.MapFaction);
			if (this.IsAggressive)
			{
				List<MobileParty> list = new List<MobileParty>();
				List<MobileParty> list2 = new List<MobileParty>();
				MobileParty conversationParty = this._conversationManager.ConversationParty;
				MobileParty mainParty = MobileParty.MainParty;
				if (PlayerEncounter.PlayerIsAttacker)
				{
					list2.Add(mainParty);
					list.Add(conversationParty);
					PlayerEncounter.Current.FindAllNpcPartiesWhoWillJoinEvent(list2, list);
				}
				else
				{
					list2.Add(conversationParty);
					list.Add(mainParty);
					PlayerEncounter.Current.FindAllNpcPartiesWhoWillJoinEvent(list, list2);
				}
				this.AttackerLeader = new ConversationAggressivePartyItemVM(PlayerEncounter.PlayerIsAttacker ? mainParty : conversationParty, null);
				this.DefenderLeader = new ConversationAggressivePartyItemVM(PlayerEncounter.PlayerIsAttacker ? conversationParty : mainParty, null);
				double num = 0.0;
				double num2 = 0.0;
				num += (double)this.DefenderLeader.Party.Party.CalculateCurrentStrength();
				num2 += (double)this.AttackerLeader.Party.Party.CalculateCurrentStrength();
				foreach (MobileParty mobileParty in list)
				{
					if (mobileParty != conversationParty && mobileParty != mainParty)
					{
						num += (double)mobileParty.Party.CalculateCurrentStrength();
						this.DefenderParties.Add(new ConversationAggressivePartyItemVM(mobileParty, null));
					}
				}
				foreach (MobileParty mobileParty2 in list2)
				{
					if (mobileParty2 != conversationParty && mobileParty2 != mainParty)
					{
						num2 += (double)mobileParty2.Party.CalculateCurrentStrength();
						this.AttackerParties.Add(new ConversationAggressivePartyItemVM(mobileParty2, null));
					}
				}
				string defenderColor;
				if (this.DefenderLeader.Party.MapFaction != null && this.DefenderLeader.Party.MapFaction is Kingdom)
				{
					defenderColor = Color.FromUint(((Kingdom)this.DefenderLeader.Party.MapFaction).PrimaryBannerColor).ToString();
				}
				else
				{
					defenderColor = Color.FromUint(this.DefenderLeader.Party.MapFaction.Banner.GetPrimaryColor()).ToString();
				}
				string attackerColor;
				if (this.AttackerLeader.Party.MapFaction != null && this.AttackerLeader.Party.MapFaction is Kingdom)
				{
					attackerColor = Color.FromUint(((Kingdom)this.AttackerLeader.Party.MapFaction).PrimaryBannerColor).ToString();
				}
				else
				{
					attackerColor = Color.FromUint(this.AttackerLeader.Party.MapFaction.Banner.GetPrimaryColor()).ToString();
				}
				if (!list2.AnyQ((MobileParty p) => p.IsInfoHidden))
				{
					if (!list.AnyQ((MobileParty p) => p.IsInfoHidden))
					{
						goto IL_485;
					}
				}
				if (PlayerEncounter.PlayerIsAttacker)
				{
					num2 = 0.0;
					num = 1.0;
				}
				else
				{
					num2 = 1.0;
					num = 0.0;
				}
				IL_485:
				this.PowerComparer = new PowerLevelComparer(num, num2);
				this.PowerComparer.SetColors(defenderColor, attackerColor);
			}
			else
			{
				this.DefenderLeader = new ConversationAggressivePartyItemVM(null, null);
				this.AttackerLeader = new ConversationAggressivePartyItemVM(null, null);
			}
			this.ExecuteSetCurrentAnswer(null);
			this.RefreshValues();
		}

		// Token: 0x06001972 RID: 6514 RVA: 0x0006073C File Offset: 0x0005E93C
		private void OnPersuasionProgress(Tuple<PersuasionOptionArgs, PersuasionOptionResult> result)
		{
			PersuasionVM persuasion = this.Persuasion;
			if (persuasion != null)
			{
				persuasion.OnPersuasionProgress(result);
			}
			this.AnswerList.ApplyActionOnAllItems(delegate(ConversationItemVM a)
			{
				a.OnPersuasionProgress(result);
			});
		}

		// Token: 0x06001973 RID: 6515 RVA: 0x00060784 File Offset: 0x0005E984
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.ContinueText = this._getContinueInputText();
			this.MoreOptionText = GameTexts.FindText("str_more_brackets", null).ToString();
			this.PersuasionText = GameTexts.FindText("str_persuasion", null).ToString();
			this.RelationHint = new HintViewModel(GameTexts.FindText("str_tooltip_label_relation", null), null);
			this.GoldHint = new HintViewModel(new TextObject("{=o5G8A8ZH}Your Denars", null), null);
			this._answerList.ApplyActionOnAllItems(delegate(ConversationItemVM x)
			{
				x.RefreshValues();
			});
			this._defenderParties.ApplyActionOnAllItems(delegate(ConversationAggressivePartyItemVM x)
			{
				x.RefreshValues();
			});
			this._attackerParties.ApplyActionOnAllItems(delegate(ConversationAggressivePartyItemVM x)
			{
				x.RefreshValues();
			});
			this._defenderLeader.RefreshValues();
			this._attackerLeader.RefreshValues();
			this._currentSelectedAnswer.RefreshValues();
		}

		// Token: 0x06001974 RID: 6516 RVA: 0x000608A4 File Offset: 0x0005EAA4
		public void Tick(float dt)
		{
			this.IsAggressive = Campaign.Current.CurrentConversationContext == ConversationContext.PartyEncounter && this._conversationManager.ConversationParty != null && FactionManager.IsAtWarAgainstFaction(this._conversationManager.ConversationParty.MapFaction, Hero.MainHero.MapFaction);
		}

		// Token: 0x06001975 RID: 6517 RVA: 0x000608F4 File Offset: 0x0005EAF4
		private void OnAgressiveStateUpdated()
		{
			if (this.IsAggressive)
			{
				List<MobileParty> list = new List<MobileParty>();
				List<MobileParty> list2 = new List<MobileParty>();
				MobileParty conversationParty = this._conversationManager.ConversationParty;
				MobileParty mainParty = MobileParty.MainParty;
				if (PlayerEncounter.PlayerIsAttacker)
				{
					list2.Add(mainParty);
					list.Add(conversationParty);
					PlayerEncounter.Current.FindAllNpcPartiesWhoWillJoinEvent(list2, list);
				}
				else
				{
					list2.Add(conversationParty);
					list.Add(mainParty);
					PlayerEncounter.Current.FindAllNpcPartiesWhoWillJoinEvent(list, list2);
				}
				this.AttackerLeader = new ConversationAggressivePartyItemVM(PlayerEncounter.PlayerIsAttacker ? mainParty : conversationParty, null);
				this.DefenderLeader = new ConversationAggressivePartyItemVM(PlayerEncounter.PlayerIsAttacker ? conversationParty : mainParty, null);
				double num = 0.0;
				double num2 = 0.0;
				num += (double)this.DefenderLeader.Party.Party.CalculateCurrentStrength();
				num2 += (double)this.AttackerLeader.Party.Party.CalculateCurrentStrength();
				foreach (MobileParty mobileParty in list)
				{
					if (mobileParty != conversationParty && mobileParty != mainParty)
					{
						num += (double)mobileParty.Party.CalculateCurrentStrength();
						this.DefenderParties.Add(new ConversationAggressivePartyItemVM(mobileParty, null));
					}
				}
				foreach (MobileParty mobileParty2 in list2)
				{
					if (mobileParty2 != conversationParty && mobileParty2 != mainParty)
					{
						num2 += (double)mobileParty2.Party.CalculateCurrentStrength();
						this.AttackerParties.Add(new ConversationAggressivePartyItemVM(mobileParty2, null));
					}
				}
				string defenderColor;
				if (this.DefenderLeader.Party.MapFaction != null && this.DefenderLeader.Party.MapFaction is Kingdom)
				{
					defenderColor = Color.FromUint(((Kingdom)this.DefenderLeader.Party.MapFaction).PrimaryBannerColor).ToString();
				}
				else
				{
					defenderColor = Color.FromUint(this.DefenderLeader.Party.MapFaction.Banner.GetPrimaryColor()).ToString();
				}
				string attackerColor;
				if (this.AttackerLeader.Party.MapFaction != null && this.AttackerLeader.Party.MapFaction is Kingdom)
				{
					attackerColor = Color.FromUint(((Kingdom)this.AttackerLeader.Party.MapFaction).PrimaryBannerColor).ToString();
				}
				else
				{
					attackerColor = Color.FromUint(this.AttackerLeader.Party.MapFaction.Banner.GetPrimaryColor()).ToString();
				}
				this.PowerComparer = new PowerLevelComparer(num, num2);
				this.PowerComparer.SetColors(defenderColor, attackerColor);
				return;
			}
			this.DefenderLeader = new ConversationAggressivePartyItemVM(null, null);
			this.AttackerLeader = new ConversationAggressivePartyItemVM(null, null);
		}

		// Token: 0x06001976 RID: 6518 RVA: 0x00060C04 File Offset: 0x0005EE04
		public void OnConversationContinue()
		{
			if (ConversationManager.GetPersuasionIsActive() && (!ConversationManager.GetPersuasionIsActive() || this.IsPersuading))
			{
				List<ConversationSentenceOption> curOptions = this._conversationManager.CurOptions;
				if (((curOptions != null) ? curOptions.Count : 0) > 1)
				{
					return;
				}
			}
			this.Refresh();
		}

		// Token: 0x06001977 RID: 6519 RVA: 0x00060C3C File Offset: 0x0005EE3C
		public void ExecuteLink(string link)
		{
			if (!this._isLinksDisabled)
			{
				Campaign.Current.EncyclopediaManager.GoToLink(link);
			}
		}

		// Token: 0x06001978 RID: 6520 RVA: 0x00060C58 File Offset: 0x0005EE58
		public void ExecuteConversedHeroLink()
		{
			CharacterObject characterObject;
			if (!this._isLinksDisabled && (characterObject = this._currentDialogCharacter as CharacterObject) != null)
			{
				EncyclopediaManager encyclopediaManager = Campaign.Current.EncyclopediaManager;
				Hero heroObject = characterObject.HeroObject;
				encyclopediaManager.GoToLink(((heroObject != null) ? heroObject.EncyclopediaLink : null) ?? characterObject.EncyclopediaLink);
				this.SelectedAnOptionOrLinkThisFrame = true;
			}
		}

		// Token: 0x06001979 RID: 6521 RVA: 0x00060CB0 File Offset: 0x0005EEB0
		public void Refresh()
		{
			this.ExecuteCloseTooltip();
			this._isProcessingOption = false;
			this.IsLoadingOver = false;
			IReadOnlyList<IAgent> conversationAgents = this._conversationManager.ConversationAgents;
			if (conversationAgents != null && conversationAgents.Count > 0)
			{
				this._currentDialogCharacter = this._conversationManager.SpeakerAgent.Character;
				this.CurrentCharacterNameLbl = this._currentDialogCharacter.Name.ToString();
				this.IsCurrentCharacterValidInEncyclopedia = false;
				if (((CharacterObject)this._currentDialogCharacter).IsHero && this._currentDialogCharacter != CharacterObject.PlayerCharacter)
				{
					this.MinRelation = Campaign.Current.Models.DiplomacyModel.MinRelationLimit;
					this.MaxRelation = Campaign.Current.Models.DiplomacyModel.MaxRelationLimit;
					Hero heroObject = ((CharacterObject)this._currentDialogCharacter).HeroObject;
					if (heroObject.IsLord && !heroObject.IsMinorFactionHero)
					{
						Clan clan = heroObject.Clan;
						if (((clan != null) ? clan.Leader : null) == heroObject)
						{
							Clan clan2 = heroObject.Clan;
							if (((clan2 != null) ? clan2.Kingdom : null) != null)
							{
								string stringId = heroObject.MapFaction.Culture.StringId;
								TextObject textObject;
								if (GameTexts.TryGetText("str_faction_noble_name_with_title", out textObject, stringId))
								{
									if (heroObject.Clan.Kingdom.Leader == heroObject)
									{
										textObject = GameTexts.FindText("str_faction_ruler_name_with_title", stringId);
									}
									StringHelpers.SetCharacterProperties("RULER", (CharacterObject)this._currentDialogCharacter, null, false);
									this.CurrentCharacterNameLbl = textObject.ToString();
								}
							}
						}
					}
					this.IsRelationEnabled = true;
					this.Relation = Hero.MainHero.GetRelation(heroObject);
					GameTexts.SetVariable("NUM", this.Relation.ToString());
					if (this.Relation > 0)
					{
						this.RelationText = "+" + this.Relation;
					}
					else if (this.Relation < 0)
					{
						this.RelationText = "-" + MathF.Abs(this.Relation);
					}
					else
					{
						this.RelationText = this.Relation.ToString();
					}
					if (heroObject.Clan == null)
					{
						this.ConversedHeroBanner = new BannerImageIdentifierVM(null, false);
						this.IsRelationEnabled = false;
						this.IsBannerEnabled = false;
					}
					else
					{
						this.ConversedHeroBanner = ((heroObject != null) ? new BannerImageIdentifierVM(heroObject.ClanBanner, false) : new BannerImageIdentifierVM(null, false));
						TextObject hintText = ((heroObject != null) ? heroObject.Clan.Name : TextObject.GetEmpty());
						this.FactionHint = new HintViewModel(hintText, null);
						this.IsBannerEnabled = true;
					}
					this.IsCurrentCharacterValidInEncyclopedia = Campaign.Current.EncyclopediaManager.GetPageOf(typeof(Hero)).IsValidEncyclopediaItem(heroObject);
				}
				else
				{
					this.ConversedHeroBanner = new BannerImageIdentifierVM(null, false);
					this.IsRelationEnabled = false;
					this.IsBannerEnabled = false;
					this.IsCurrentCharacterValidInEncyclopedia = Campaign.Current.EncyclopediaManager.GetPageOf(typeof(CharacterObject)).IsValidEncyclopediaItem((CharacterObject)this._conversationManager.SpeakerAgent.Character);
				}
			}
			this.DialogText = this._conversationManager.CurrentSentenceText;
			this.AnswerList.Clear();
			MissionConversationVM._isCurrentlyPlayerSpeaking = this._currentDialogCharacter == Hero.MainHero.CharacterObject;
			this._conversationManager.GetPlayerSentenceOptions();
			List<ConversationSentenceOption> curOptions = this._conversationManager.CurOptions;
			int num = ((curOptions != null) ? curOptions.Count : 0);
			if (num > 0 && !MissionConversationVM._isCurrentlyPlayerSpeaking)
			{
				for (int i = 0; i < num; i++)
				{
					this.AnswerList.Add(new ConversationItemVM(new Action<int>(this.OnSelectOption), new Action(this.OnReadyToContinue), new Action<ConversationItemVM>(this.ExecuteSetCurrentAnswer), i));
				}
			}
			this.GoldText = CampaignUIHelper.GetAbbreviatedValueTextFromValue(Hero.MainHero.Gold);
			this.IsPersuading = ConversationManager.GetPersuasionIsActive();
			if (this.IsPersuading)
			{
				this.CurrentSelectedAnswer = new ConversationItemVM();
			}
			this.IsLoadingOver = true;
			PersuasionVM persuasion = this.Persuasion;
			if (persuasion == null)
			{
				return;
			}
			persuasion.RefreshPersusasion();
		}

		// Token: 0x0600197A RID: 6522 RVA: 0x000610A1 File Offset: 0x0005F2A1
		private void OnReadyToContinue()
		{
			this.Refresh();
		}

		// Token: 0x0600197B RID: 6523 RVA: 0x000610AC File Offset: 0x0005F2AC
		private void ExecuteDefenderTooltip()
		{
			if (PlayerEncounter.PlayerIsDefender)
			{
				InformationManager.ShowTooltip(typeof(List<MobileParty>), new object[] { 0 });
				return;
			}
			InformationManager.ShowTooltip(typeof(List<MobileParty>), new object[] { 1 });
		}

		// Token: 0x0600197C RID: 6524 RVA: 0x000610FD File Offset: 0x0005F2FD
		public void ExecuteCloseTooltip()
		{
			MBInformationManager.HideInformations();
		}

		// Token: 0x0600197D RID: 6525 RVA: 0x00061104 File Offset: 0x0005F304
		public void ExecuteHeroTooltip()
		{
			CharacterObject characterObject = (CharacterObject)this._currentDialogCharacter;
			if (characterObject != null && characterObject.IsHero)
			{
				InformationManager.ShowTooltip(typeof(Hero), new object[] { characterObject.HeroObject, true });
			}
		}

		// Token: 0x0600197E RID: 6526 RVA: 0x00061150 File Offset: 0x0005F350
		private void ExecuteAttackerTooltip()
		{
			if (PlayerEncounter.PlayerIsAttacker)
			{
				InformationManager.ShowTooltip(typeof(List<MobileParty>), new object[] { 0 });
				return;
			}
			InformationManager.ShowTooltip(typeof(List<MobileParty>), new object[] { 1 });
		}

		// Token: 0x0600197F RID: 6527 RVA: 0x000611A4 File Offset: 0x0005F3A4
		private void ExecuteHeroInfo()
		{
			if (this._conversationManager.ListenerAgent.Character == Hero.MainHero.CharacterObject)
			{
				Campaign.Current.EncyclopediaManager.GoToLink(Hero.MainHero.EncyclopediaLink);
				return;
			}
			if (CharacterObject.OneToOneConversationCharacter.IsHero)
			{
				Campaign.Current.EncyclopediaManager.GoToLink(CharacterObject.OneToOneConversationCharacter.HeroObject.EncyclopediaLink);
				return;
			}
			Campaign.Current.EncyclopediaManager.GoToLink(CharacterObject.OneToOneConversationCharacter.EncyclopediaLink);
		}

		// Token: 0x06001980 RID: 6528 RVA: 0x0006122B File Offset: 0x0005F42B
		private void OnSelectOption(int optionIndex)
		{
			if (!this._isProcessingOption)
			{
				this._isProcessingOption = true;
				this._conversationManager.DoOption(optionIndex);
				PersuasionVM persuasion = this.Persuasion;
				if (persuasion != null)
				{
					persuasion.RefreshPersusasion();
				}
				this.SelectedAnOptionOrLinkThisFrame = true;
			}
		}

		// Token: 0x06001981 RID: 6529 RVA: 0x00061260 File Offset: 0x0005F460
		public void ExecuteFinalizeSelection()
		{
			this.Refresh();
		}

		// Token: 0x06001982 RID: 6530 RVA: 0x00061268 File Offset: 0x0005F468
		public void ExecuteContinue()
		{
			Debug.Print("ExecuteContinue", 0, Debug.DebugColor.White, 17592186044416UL);
			this._conversationManager.ContinueConversation();
			this._isProcessingOption = false;
		}

		// Token: 0x06001983 RID: 6531 RVA: 0x00061292 File Offset: 0x0005F492
		private void ExecuteSetCurrentAnswer(ConversationItemVM _answer)
		{
			this.Persuasion.SetCurrentOption((_answer != null) ? _answer.PersuasionItem : null);
			if (_answer != null)
			{
				this.CurrentSelectedAnswer = _answer;
				return;
			}
			this.CurrentSelectedAnswer = new ConversationItemVM();
		}

		// Token: 0x06001984 RID: 6532 RVA: 0x000612C4 File Offset: 0x0005F4C4
		public override void OnFinalize()
		{
			base.OnFinalize();
			CampaignEvents.PersuasionProgressCommittedEvent.ClearListeners(this);
			Input.OnGamepadActiveStateChanged = (Action)Delegate.Remove(Input.OnGamepadActiveStateChanged, new Action(this.RefreshValues));
			PersuasionVM persuasion = this.Persuasion;
			if (persuasion == null)
			{
				return;
			}
			persuasion.OnFinalize();
		}

		// Token: 0x17000887 RID: 2183
		// (get) Token: 0x06001985 RID: 6533 RVA: 0x00061313 File Offset: 0x0005F513
		// (set) Token: 0x06001986 RID: 6534 RVA: 0x0006131B File Offset: 0x0005F51B
		[DataSourceProperty]
		public PersuasionVM Persuasion
		{
			get
			{
				return this._persuasion;
			}
			set
			{
				if (value != this._persuasion)
				{
					this._persuasion = value;
					base.OnPropertyChangedWithValue<PersuasionVM>(value, "Persuasion");
				}
			}
		}

		// Token: 0x17000888 RID: 2184
		// (get) Token: 0x06001987 RID: 6535 RVA: 0x00061339 File Offset: 0x0005F539
		// (set) Token: 0x06001988 RID: 6536 RVA: 0x00061341 File Offset: 0x0005F541
		[DataSourceProperty]
		public PowerLevelComparer PowerComparer
		{
			get
			{
				return this._powerComparer;
			}
			set
			{
				if (value != this._powerComparer)
				{
					this._powerComparer = value;
					base.OnPropertyChangedWithValue<PowerLevelComparer>(value, "PowerComparer");
				}
			}
		}

		// Token: 0x17000889 RID: 2185
		// (get) Token: 0x06001989 RID: 6537 RVA: 0x0006135F File Offset: 0x0005F55F
		// (set) Token: 0x0600198A RID: 6538 RVA: 0x00061367 File Offset: 0x0005F567
		[DataSourceProperty]
		public int Relation
		{
			get
			{
				return this._relation;
			}
			set
			{
				if (this._relation != value)
				{
					this._relation = value;
					base.OnPropertyChangedWithValue(value, "Relation");
				}
			}
		}

		// Token: 0x1700088A RID: 2186
		// (get) Token: 0x0600198B RID: 6539 RVA: 0x00061385 File Offset: 0x0005F585
		// (set) Token: 0x0600198C RID: 6540 RVA: 0x0006138D File Offset: 0x0005F58D
		[DataSourceProperty]
		public int MinRelation
		{
			get
			{
				return this._minRelation;
			}
			set
			{
				if (this._minRelation != value)
				{
					this._minRelation = value;
					base.OnPropertyChangedWithValue(value, "MinRelation");
				}
			}
		}

		// Token: 0x1700088B RID: 2187
		// (get) Token: 0x0600198D RID: 6541 RVA: 0x000613AB File Offset: 0x0005F5AB
		// (set) Token: 0x0600198E RID: 6542 RVA: 0x000613B3 File Offset: 0x0005F5B3
		[DataSourceProperty]
		public int MaxRelation
		{
			get
			{
				return this._maxRelation;
			}
			set
			{
				if (this._maxRelation != value)
				{
					this._maxRelation = value;
					base.OnPropertyChangedWithValue(value, "MaxRelation");
				}
			}
		}

		// Token: 0x1700088C RID: 2188
		// (get) Token: 0x0600198F RID: 6543 RVA: 0x000613D1 File Offset: 0x0005F5D1
		// (set) Token: 0x06001990 RID: 6544 RVA: 0x000613D9 File Offset: 0x0005F5D9
		[DataSourceProperty]
		public ConversationAggressivePartyItemVM DefenderLeader
		{
			get
			{
				return this._defenderLeader;
			}
			set
			{
				if (value != this._defenderLeader)
				{
					this._defenderLeader = value;
					base.OnPropertyChangedWithValue<ConversationAggressivePartyItemVM>(value, "DefenderLeader");
				}
			}
		}

		// Token: 0x1700088D RID: 2189
		// (get) Token: 0x06001991 RID: 6545 RVA: 0x000613F7 File Offset: 0x0005F5F7
		// (set) Token: 0x06001992 RID: 6546 RVA: 0x000613FF File Offset: 0x0005F5FF
		[DataSourceProperty]
		public ConversationAggressivePartyItemVM AttackerLeader
		{
			get
			{
				return this._attackerLeader;
			}
			set
			{
				if (value != this._attackerLeader)
				{
					this._attackerLeader = value;
					base.OnPropertyChangedWithValue<ConversationAggressivePartyItemVM>(value, "AttackerLeader");
				}
			}
		}

		// Token: 0x1700088E RID: 2190
		// (get) Token: 0x06001993 RID: 6547 RVA: 0x0006141D File Offset: 0x0005F61D
		// (set) Token: 0x06001994 RID: 6548 RVA: 0x00061425 File Offset: 0x0005F625
		[DataSourceProperty]
		public MBBindingList<ConversationAggressivePartyItemVM> AttackerParties
		{
			get
			{
				return this._attackerParties;
			}
			set
			{
				if (value != this._attackerParties)
				{
					this._attackerParties = value;
					base.OnPropertyChangedWithValue<MBBindingList<ConversationAggressivePartyItemVM>>(value, "AttackerParties");
				}
			}
		}

		// Token: 0x1700088F RID: 2191
		// (get) Token: 0x06001995 RID: 6549 RVA: 0x00061443 File Offset: 0x0005F643
		// (set) Token: 0x06001996 RID: 6550 RVA: 0x0006144B File Offset: 0x0005F64B
		[DataSourceProperty]
		public MBBindingList<ConversationAggressivePartyItemVM> DefenderParties
		{
			get
			{
				return this._defenderParties;
			}
			set
			{
				if (value != this._defenderParties)
				{
					this._defenderParties = value;
					base.OnPropertyChangedWithValue<MBBindingList<ConversationAggressivePartyItemVM>>(value, "DefenderParties");
				}
			}
		}

		// Token: 0x17000890 RID: 2192
		// (get) Token: 0x06001997 RID: 6551 RVA: 0x00061469 File Offset: 0x0005F669
		// (set) Token: 0x06001998 RID: 6552 RVA: 0x00061471 File Offset: 0x0005F671
		[DataSourceProperty]
		public string MoreOptionText
		{
			get
			{
				return this._moreOptionText;
			}
			set
			{
				if (this._moreOptionText != value)
				{
					this._moreOptionText = value;
					base.OnPropertyChangedWithValue<string>(value, "MoreOptionText");
				}
			}
		}

		// Token: 0x17000891 RID: 2193
		// (get) Token: 0x06001999 RID: 6553 RVA: 0x00061494 File Offset: 0x0005F694
		// (set) Token: 0x0600199A RID: 6554 RVA: 0x0006149C File Offset: 0x0005F69C
		[DataSourceProperty]
		public string GoldText
		{
			get
			{
				return this._goldText;
			}
			set
			{
				if (this._goldText != value)
				{
					this._goldText = value;
					base.OnPropertyChangedWithValue<string>(value, "GoldText");
				}
			}
		}

		// Token: 0x17000892 RID: 2194
		// (get) Token: 0x0600199B RID: 6555 RVA: 0x000614BF File Offset: 0x0005F6BF
		// (set) Token: 0x0600199C RID: 6556 RVA: 0x000614C7 File Offset: 0x0005F6C7
		[DataSourceProperty]
		public string PersuasionText
		{
			get
			{
				return this._persuasionText;
			}
			set
			{
				if (this._persuasionText != value)
				{
					this._persuasionText = value;
					base.OnPropertyChangedWithValue<string>(value, "PersuasionText");
				}
			}
		}

		// Token: 0x17000893 RID: 2195
		// (get) Token: 0x0600199D RID: 6557 RVA: 0x000614EA File Offset: 0x0005F6EA
		// (set) Token: 0x0600199E RID: 6558 RVA: 0x000614F2 File Offset: 0x0005F6F2
		[DataSourceProperty]
		public bool IsCurrentCharacterValidInEncyclopedia
		{
			get
			{
				return this._isCurrentCharacterValidInEncyclopedia;
			}
			set
			{
				if (this._isCurrentCharacterValidInEncyclopedia != value)
				{
					this._isCurrentCharacterValidInEncyclopedia = value;
					base.OnPropertyChangedWithValue(value, "IsCurrentCharacterValidInEncyclopedia");
				}
			}
		}

		// Token: 0x17000894 RID: 2196
		// (get) Token: 0x0600199F RID: 6559 RVA: 0x00061510 File Offset: 0x0005F710
		// (set) Token: 0x060019A0 RID: 6560 RVA: 0x00061518 File Offset: 0x0005F718
		[DataSourceProperty]
		public bool IsLoadingOver
		{
			get
			{
				return this._isLoadingOver;
			}
			set
			{
				if (this._isLoadingOver != value)
				{
					this._isLoadingOver = value;
					base.OnPropertyChangedWithValue(value, "IsLoadingOver");
				}
			}
		}

		// Token: 0x17000895 RID: 2197
		// (get) Token: 0x060019A1 RID: 6561 RVA: 0x00061536 File Offset: 0x0005F736
		// (set) Token: 0x060019A2 RID: 6562 RVA: 0x0006153E File Offset: 0x0005F73E
		[DataSourceProperty]
		public bool IsPersuading
		{
			get
			{
				return this._isPersuading;
			}
			set
			{
				if (this._isPersuading != value)
				{
					this._isPersuading = value;
					base.OnPropertyChangedWithValue(value, "IsPersuading");
				}
			}
		}

		// Token: 0x17000896 RID: 2198
		// (get) Token: 0x060019A3 RID: 6563 RVA: 0x0006155C File Offset: 0x0005F75C
		// (set) Token: 0x060019A4 RID: 6564 RVA: 0x00061564 File Offset: 0x0005F764
		[DataSourceProperty]
		public string ContinueText
		{
			get
			{
				return this._continueText;
			}
			set
			{
				if (this._continueText != value)
				{
					this._continueText = value;
					base.OnPropertyChangedWithValue<string>(value, "ContinueText");
				}
			}
		}

		// Token: 0x17000897 RID: 2199
		// (get) Token: 0x060019A5 RID: 6565 RVA: 0x00061587 File Offset: 0x0005F787
		// (set) Token: 0x060019A6 RID: 6566 RVA: 0x0006158F File Offset: 0x0005F78F
		[DataSourceProperty]
		public string CurrentCharacterNameLbl
		{
			get
			{
				return this._currentCharacterNameLbl;
			}
			set
			{
				if (this._currentCharacterNameLbl != value)
				{
					this._currentCharacterNameLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "CurrentCharacterNameLbl");
				}
			}
		}

		// Token: 0x17000898 RID: 2200
		// (get) Token: 0x060019A7 RID: 6567 RVA: 0x000615B2 File Offset: 0x0005F7B2
		// (set) Token: 0x060019A8 RID: 6568 RVA: 0x000615BA File Offset: 0x0005F7BA
		[DataSourceProperty]
		public MBBindingList<ConversationItemVM> AnswerList
		{
			get
			{
				return this._answerList;
			}
			set
			{
				if (this._answerList != value)
				{
					this._answerList = value;
					base.OnPropertyChangedWithValue<MBBindingList<ConversationItemVM>>(value, "AnswerList");
				}
			}
		}

		// Token: 0x17000899 RID: 2201
		// (get) Token: 0x060019A9 RID: 6569 RVA: 0x000615D8 File Offset: 0x0005F7D8
		// (set) Token: 0x060019AA RID: 6570 RVA: 0x000615E0 File Offset: 0x0005F7E0
		[DataSourceProperty]
		public string DialogText
		{
			get
			{
				return this._dialogText;
			}
			set
			{
				if (this._dialogText != value)
				{
					this._dialogText = value;
					base.OnPropertyChangedWithValue<string>(value, "DialogText");
				}
			}
		}

		// Token: 0x1700089A RID: 2202
		// (get) Token: 0x060019AB RID: 6571 RVA: 0x00061603 File Offset: 0x0005F803
		// (set) Token: 0x060019AC RID: 6572 RVA: 0x0006160B File Offset: 0x0005F80B
		[DataSourceProperty]
		public bool IsAggressive
		{
			get
			{
				return this._isAggressive;
			}
			set
			{
				if (value != this._isAggressive)
				{
					this._isAggressive = value;
					base.OnPropertyChangedWithValue(value, "IsAggressive");
					this.OnAgressiveStateUpdated();
				}
			}
		}

		// Token: 0x1700089B RID: 2203
		// (get) Token: 0x060019AD RID: 6573 RVA: 0x0006162F File Offset: 0x0005F82F
		// (set) Token: 0x060019AE RID: 6574 RVA: 0x00061637 File Offset: 0x0005F837
		[DataSourceProperty]
		public int SelectedSide
		{
			get
			{
				return this._selectedSide;
			}
			set
			{
				if (value != this._selectedSide)
				{
					this._selectedSide = value;
					base.OnPropertyChangedWithValue(value, "SelectedSide");
				}
			}
		}

		// Token: 0x1700089C RID: 2204
		// (get) Token: 0x060019AF RID: 6575 RVA: 0x00061655 File Offset: 0x0005F855
		// (set) Token: 0x060019B0 RID: 6576 RVA: 0x0006165D File Offset: 0x0005F85D
		[DataSourceProperty]
		public string RelationText
		{
			get
			{
				return this._relationText;
			}
			set
			{
				if (this._relationText != value)
				{
					this._relationText = value;
					base.OnPropertyChangedWithValue<string>(value, "RelationText");
				}
			}
		}

		// Token: 0x1700089D RID: 2205
		// (get) Token: 0x060019B1 RID: 6577 RVA: 0x00061680 File Offset: 0x0005F880
		// (set) Token: 0x060019B2 RID: 6578 RVA: 0x00061688 File Offset: 0x0005F888
		[DataSourceProperty]
		public bool IsRelationEnabled
		{
			get
			{
				return this._isRelationEnabled;
			}
			set
			{
				if (value != this._isRelationEnabled)
				{
					this._isRelationEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsRelationEnabled");
				}
			}
		}

		// Token: 0x1700089E RID: 2206
		// (get) Token: 0x060019B3 RID: 6579 RVA: 0x000616A6 File Offset: 0x0005F8A6
		// (set) Token: 0x060019B4 RID: 6580 RVA: 0x000616AE File Offset: 0x0005F8AE
		[DataSourceProperty]
		public bool IsBannerEnabled
		{
			get
			{
				return this._isBannerEnabled;
			}
			set
			{
				if (value != this._isBannerEnabled)
				{
					this._isBannerEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsBannerEnabled");
				}
			}
		}

		// Token: 0x1700089F RID: 2207
		// (get) Token: 0x060019B5 RID: 6581 RVA: 0x000616CC File Offset: 0x0005F8CC
		// (set) Token: 0x060019B6 RID: 6582 RVA: 0x000616D4 File Offset: 0x0005F8D4
		[DataSourceProperty]
		public ConversationItemVM CurrentSelectedAnswer
		{
			get
			{
				return this._currentSelectedAnswer;
			}
			set
			{
				if (this._currentSelectedAnswer != value)
				{
					this._currentSelectedAnswer = value;
					base.OnPropertyChangedWithValue<ConversationItemVM>(value, "CurrentSelectedAnswer");
				}
			}
		}

		// Token: 0x170008A0 RID: 2208
		// (get) Token: 0x060019B7 RID: 6583 RVA: 0x000616F2 File Offset: 0x0005F8F2
		// (set) Token: 0x060019B8 RID: 6584 RVA: 0x000616FA File Offset: 0x0005F8FA
		[DataSourceProperty]
		public BannerImageIdentifierVM ConversedHeroBanner
		{
			get
			{
				return this._conversedHeroBanner;
			}
			set
			{
				if (this._conversedHeroBanner != value)
				{
					this._conversedHeroBanner = value;
					base.OnPropertyChangedWithValue<BannerImageIdentifierVM>(value, "ConversedHeroBanner");
				}
			}
		}

		// Token: 0x170008A1 RID: 2209
		// (get) Token: 0x060019B9 RID: 6585 RVA: 0x00061718 File Offset: 0x0005F918
		// (set) Token: 0x060019BA RID: 6586 RVA: 0x00061720 File Offset: 0x0005F920
		[DataSourceProperty]
		public HintViewModel RelationHint
		{
			get
			{
				return this._relationHint;
			}
			set
			{
				if (this._relationHint != value)
				{
					this._relationHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "RelationHint");
				}
			}
		}

		// Token: 0x170008A2 RID: 2210
		// (get) Token: 0x060019BB RID: 6587 RVA: 0x0006173E File Offset: 0x0005F93E
		// (set) Token: 0x060019BC RID: 6588 RVA: 0x00061746 File Offset: 0x0005F946
		[DataSourceProperty]
		public HintViewModel FactionHint
		{
			get
			{
				return this._factionHint;
			}
			set
			{
				if (this._factionHint != value)
				{
					this._factionHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "FactionHint");
				}
			}
		}

		// Token: 0x170008A3 RID: 2211
		// (get) Token: 0x060019BD RID: 6589 RVA: 0x00061764 File Offset: 0x0005F964
		// (set) Token: 0x060019BE RID: 6590 RVA: 0x0006176C File Offset: 0x0005F96C
		[DataSourceProperty]
		public HintViewModel GoldHint
		{
			get
			{
				return this._goldHint;
			}
			set
			{
				if (this._goldHint != value)
				{
					this._goldHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "GoldHint");
				}
			}
		}

		// Token: 0x04000BB3 RID: 2995
		private readonly ConversationManager _conversationManager;

		// Token: 0x04000BB4 RID: 2996
		private readonly bool _isLinksDisabled;

		// Token: 0x04000BB5 RID: 2997
		private static bool _isCurrentlyPlayerSpeaking;

		// Token: 0x04000BB6 RID: 2998
		private bool _isProcessingOption;

		// Token: 0x04000BB7 RID: 2999
		private BasicCharacterObject _currentDialogCharacter;

		// Token: 0x04000BB8 RID: 3000
		private Func<string> _getContinueInputText;

		// Token: 0x04000BB9 RID: 3001
		private MBBindingList<ConversationItemVM> _answerList;

		// Token: 0x04000BBA RID: 3002
		private string _dialogText;

		// Token: 0x04000BBB RID: 3003
		private string _currentCharacterNameLbl;

		// Token: 0x04000BBC RID: 3004
		private string _continueText;

		// Token: 0x04000BBD RID: 3005
		private string _relationText;

		// Token: 0x04000BBE RID: 3006
		private string _persuasionText;

		// Token: 0x04000BBF RID: 3007
		private bool _isLoadingOver;

		// Token: 0x04000BC0 RID: 3008
		private string _moreOptionText;

		// Token: 0x04000BC1 RID: 3009
		private string _goldText;

		// Token: 0x04000BC2 RID: 3010
		private ConversationAggressivePartyItemVM _defenderLeader;

		// Token: 0x04000BC3 RID: 3011
		private ConversationAggressivePartyItemVM _attackerLeader;

		// Token: 0x04000BC4 RID: 3012
		private MBBindingList<ConversationAggressivePartyItemVM> _defenderParties;

		// Token: 0x04000BC5 RID: 3013
		private MBBindingList<ConversationAggressivePartyItemVM> _attackerParties;

		// Token: 0x04000BC6 RID: 3014
		private BannerImageIdentifierVM _conversedHeroBanner;

		// Token: 0x04000BC7 RID: 3015
		private bool _isAggressive;

		// Token: 0x04000BC8 RID: 3016
		private bool _isRelationEnabled;

		// Token: 0x04000BC9 RID: 3017
		private bool _isBannerEnabled;

		// Token: 0x04000BCA RID: 3018
		private bool _isPersuading;

		// Token: 0x04000BCB RID: 3019
		private bool _isCurrentCharacterValidInEncyclopedia;

		// Token: 0x04000BCC RID: 3020
		private int _selectedSide;

		// Token: 0x04000BCD RID: 3021
		private int _relation;

		// Token: 0x04000BCE RID: 3022
		private int _minRelation;

		// Token: 0x04000BCF RID: 3023
		private int _maxRelation;

		// Token: 0x04000BD0 RID: 3024
		private PowerLevelComparer _powerComparer;

		// Token: 0x04000BD1 RID: 3025
		private ConversationItemVM _currentSelectedAnswer;

		// Token: 0x04000BD2 RID: 3026
		private PersuasionVM _persuasion;

		// Token: 0x04000BD3 RID: 3027
		private HintViewModel _relationHint;

		// Token: 0x04000BD4 RID: 3028
		private HintViewModel _factionHint;

		// Token: 0x04000BD5 RID: 3029
		private HintViewModel _goldHint;
	}
}
