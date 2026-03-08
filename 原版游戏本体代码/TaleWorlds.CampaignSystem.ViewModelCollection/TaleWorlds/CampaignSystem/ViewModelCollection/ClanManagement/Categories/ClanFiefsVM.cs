using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.Categories
{
	// Token: 0x02000139 RID: 313
	public class ClanFiefsVM : ViewModel
	{
		// Token: 0x06001D00 RID: 7424 RVA: 0x0006B078 File Offset: 0x00069278
		public ClanFiefsVM(Action onRefresh, Action<ClanCardSelectionInfo> openCardSelectionPopup)
		{
			this._onRefresh = onRefresh;
			this._clan = Hero.MainHero.Clan;
			this._openCardSelectionPopup = openCardSelectionPopup;
			this._teleportationBehavior = Campaign.Current.GetCampaignBehavior<ITeleportationCampaignBehavior>();
			this.Settlements = new MBBindingList<ClanSettlementItemVM>();
			this.Castles = new MBBindingList<ClanSettlementItemVM>();
			List<MBBindingList<ClanSettlementItemVM>> listsToControl = new List<MBBindingList<ClanSettlementItemVM>> { this.Settlements, this.Castles };
			this.SortController = new ClanFiefsSortControllerVM(listsToControl);
			this.RefreshAllLists();
			this.RefreshValues();
		}

		// Token: 0x06001D01 RID: 7425 RVA: 0x0006B116 File Offset: 0x00069316
		protected virtual ClanSettlementItemVM CreateSettlementItem(Settlement settlement, Action<ClanSettlementItemVM> onSelection, Action onShowSendMembers, ITeleportationCampaignBehavior teleportationBehavior)
		{
			return new ClanSettlementItemVM(settlement, onSelection, onShowSendMembers, teleportationBehavior);
		}

		// Token: 0x06001D02 RID: 7426 RVA: 0x0006B124 File Offset: 0x00069324
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.TaxText = GameTexts.FindText("str_tax", null).ToString();
			this.GovernorText = GameTexts.FindText("str_notable_governor", null).ToString();
			this.ProfitText = GameTexts.FindText("str_profit", null).ToString();
			this.NameText = GameTexts.FindText("str_sort_by_name_label", null).ToString();
			this.NoFiefsText = GameTexts.FindText("str_clan_no_fiefs", null).ToString();
			this.NoGovernorText = this._noGovernorTextSource.ToString();
			this.Settlements.ApplyActionOnAllItems(delegate(ClanSettlementItemVM x)
			{
				x.RefreshValues();
			});
			this.Castles.ApplyActionOnAllItems(delegate(ClanSettlementItemVM x)
			{
				x.RefreshValues();
			});
			ClanSettlementItemVM currentSelectedFief = this.CurrentSelectedFief;
			if (currentSelectedFief != null)
			{
				currentSelectedFief.RefreshValues();
			}
			this.SortController.RefreshValues();
		}

		// Token: 0x06001D03 RID: 7427 RVA: 0x0006B226 File Offset: 0x00069426
		public override void OnFinalize()
		{
			base.OnFinalize();
		}

		// Token: 0x06001D04 RID: 7428 RVA: 0x0006B230 File Offset: 0x00069430
		public void RefreshAllLists()
		{
			this.Settlements.Clear();
			this.Castles.Clear();
			this.SortController.ResetAllStates();
			foreach (Settlement settlement in this._clan.Settlements)
			{
				if (settlement.IsTown)
				{
					this.Settlements.Add(this.CreateSettlementItem(settlement, new Action<ClanSettlementItemVM>(this.OnFiefSelection), new Action(this.OnShowSendMembers), this._teleportationBehavior));
				}
				else if (settlement.IsCastle)
				{
					this.Castles.Add(this.CreateSettlementItem(settlement, new Action<ClanSettlementItemVM>(this.OnFiefSelection), new Action(this.OnShowSendMembers), this._teleportationBehavior));
				}
			}
			GameTexts.SetVariable("RANK", GameTexts.FindText("str_towns", null));
			GameTexts.SetVariable("NUMBER", this.Settlements.Count);
			this.TownsText = GameTexts.FindText("str_RANK_with_NUM_between_parenthesis", null).ToString();
			GameTexts.SetVariable("RANK", GameTexts.FindText("str_castles", null));
			GameTexts.SetVariable("NUMBER", this.Castles.Count);
			this.CastlesText = GameTexts.FindText("str_RANK_with_NUM_between_parenthesis", null).ToString();
			this.OnFiefSelection(this.GetDefaultMember());
		}

		// Token: 0x06001D05 RID: 7429 RVA: 0x0006B3A4 File Offset: 0x000695A4
		private ClanSettlementItemVM GetDefaultMember()
		{
			if (!this.Settlements.IsEmpty<ClanSettlementItemVM>())
			{
				return this.Settlements.FirstOrDefault<ClanSettlementItemVM>();
			}
			return this.Castles.FirstOrDefault<ClanSettlementItemVM>();
		}

		// Token: 0x06001D06 RID: 7430 RVA: 0x0006B3CC File Offset: 0x000695CC
		public void SelectFief(Settlement settlement)
		{
			foreach (ClanSettlementItemVM clanSettlementItemVM in this.Settlements)
			{
				if (clanSettlementItemVM.Settlement == settlement)
				{
					this.OnFiefSelection(clanSettlementItemVM);
					break;
				}
			}
		}

		// Token: 0x06001D07 RID: 7431 RVA: 0x0006B424 File Offset: 0x00069624
		private void OnFiefSelection(ClanSettlementItemVM fief)
		{
			if (this.CurrentSelectedFief != null)
			{
				this.CurrentSelectedFief.IsSelected = false;
			}
			this.CurrentSelectedFief = fief;
			TextObject hintText;
			this.CanChangeGovernorOfCurrentFief = this.GetCanChangeGovernor(out hintText);
			this.GovernorActionHint = new HintViewModel(hintText, null);
			if (fief != null)
			{
				fief.IsSelected = true;
				this.GovernorActionText = (fief.HasGovernor ? GameTexts.FindText("str_clan_change_governor", null).ToString() : GameTexts.FindText("str_clan_assign_governor", null).ToString());
			}
		}

		// Token: 0x06001D08 RID: 7432 RVA: 0x0006B4A4 File Offset: 0x000696A4
		private bool GetCanChangeGovernor(out TextObject disabledReason)
		{
			TextObject textObject;
			if (!CampaignUIHelper.GetMapScreenActionIsEnabledWithReason(out textObject))
			{
				disabledReason = textObject;
				return false;
			}
			ClanSettlementItemVM currentSelectedFief = this.CurrentSelectedFief;
			bool flag;
			if (currentSelectedFief == null)
			{
				flag = false;
			}
			else
			{
				HeroVM governor = currentSelectedFief.Governor;
				bool? flag2;
				if (governor == null)
				{
					flag2 = null;
				}
				else
				{
					Hero hero = governor.Hero;
					flag2 = ((hero != null) ? new bool?(hero.IsTraveling) : null);
				}
				bool? flag3 = flag2;
				bool flag4 = true;
				flag = (flag3.GetValueOrDefault() == flag4) & (flag3 != null);
			}
			if (flag)
			{
				disabledReason = new TextObject("{=qbqimqMb}{GOVERNOR.NAME} is on the way to be the new governor of {SETTLEMENT_NAME}", null);
				if (this.CurrentSelectedFief.Governor.Hero.CharacterObject != null)
				{
					StringHelpers.SetCharacterProperties("GOVERNOR", this.CurrentSelectedFief.Governor.Hero.CharacterObject, disabledReason, false);
				}
				TextObject textObject2 = disabledReason;
				string tag = "SETTLEMENT_NAME";
				Settlement settlement = this.CurrentSelectedFief.Settlement;
				string text;
				if (settlement == null)
				{
					text = null;
				}
				else
				{
					TextObject name = settlement.Name;
					text = ((name != null) ? name.ToString() : null);
				}
				textObject2.SetTextVariable(tag, text ?? string.Empty);
				return false;
			}
			ClanSettlementItemVM currentSelectedFief2 = this.CurrentSelectedFief;
			if (((currentSelectedFief2 != null) ? currentSelectedFief2.Settlement.Town : null) == null)
			{
				disabledReason = TextObject.GetEmpty();
				return false;
			}
			disabledReason = TextObject.GetEmpty();
			return true;
		}

		// Token: 0x06001D09 RID: 7433 RVA: 0x0006B5C4 File Offset: 0x000697C4
		public void ExecuteAssignGovernor()
		{
			ClanSettlementItemVM currentSelectedFief = this.CurrentSelectedFief;
			bool flag;
			if (currentSelectedFief == null)
			{
				flag = null != null;
			}
			else
			{
				Settlement settlement = currentSelectedFief.Settlement;
				flag = ((settlement != null) ? settlement.Town : null) != null;
			}
			if (flag)
			{
				ClanCardSelectionInfo obj = new ClanCardSelectionInfo(GameTexts.FindText("str_clan_assign_governor", null).CopyTextObject(), this.GetGovernorCandidates(), new Action<List<object>, Action>(this.OnGovernorSelectionOver), false, 1, 0);
				Action<ClanCardSelectionInfo> openCardSelectionPopup = this._openCardSelectionPopup;
				if (openCardSelectionPopup == null)
				{
					return;
				}
				openCardSelectionPopup(obj);
			}
		}

		// Token: 0x06001D0A RID: 7434 RVA: 0x0006B62E File Offset: 0x0006982E
		private IEnumerable<ClanCardSelectionItemInfo> GetGovernorCandidates()
		{
			yield return new ClanCardSelectionItemInfo(this._noGovernorTextSource.CopyTextObject(), false, null, null);
			foreach (Hero hero in (from h in this._clan.Heroes
				where !h.IsDisabled
				select h).Union(this._clan.Companions))
			{
				if ((hero.IsActive || hero.IsTraveling) && !hero.IsChild && hero != Hero.MainHero)
				{
					Hero hero2 = hero;
					HeroVM governor = this.CurrentSelectedFief.Governor;
					if (hero2 != ((governor != null) ? governor.Hero : null) && hero.CanBeGovernorOrHavePartyRole())
					{
						TextObject disabledReason;
						bool flag = FactionHelper.IsMainClanMemberAvailableForSendingSettlementAsGovernor(hero, this.GetSettlementOfGovernor(hero), out disabledReason);
						SkillObject charm = DefaultSkills.Charm;
						int skillValue = hero.GetSkillValue(charm);
						CharacterImageIdentifier image = new CharacterImageIdentifier(CampaignUIHelper.GetCharacterCode(hero.CharacterObject, false));
						yield return new ClanCardSelectionItemInfo(hero, hero.Name, image, CardSelectionItemSpriteType.Skill, charm.StringId.ToLower(), skillValue.ToString(), this.GetGovernorCandidateProperties(hero), !flag, disabledReason, null);
					}
				}
			}
			IEnumerator<Hero> enumerator = null;
			yield break;
			yield break;
		}

		// Token: 0x06001D0B RID: 7435 RVA: 0x0006B63E File Offset: 0x0006983E
		private IEnumerable<ClanCardSelectionItemPropertyInfo> GetGovernorCandidateProperties(Hero hero)
		{
			GameTexts.SetVariable("newline", "\n");
			TextObject teleportationDelayText = CampaignUIHelper.GetTeleportationDelayText(hero, this.CurrentSelectedFief.Settlement.Party);
			yield return new ClanCardSelectionItemPropertyInfo(teleportationDelayText);
			ValueTuple<TextObject, TextObject> governorEngineeringSkillEffectForHero = PerkHelper.GetGovernorEngineeringSkillEffectForHero(hero);
			yield return new ClanCardSelectionItemPropertyInfo(new TextObject("{=J8ddrAOf}Governor Effects", null), governorEngineeringSkillEffectForHero.Item2);
			List<PerkObject> governorPerksForHero = PerkHelper.GetGovernorPerksForHero(hero);
			TextObject value = new TextObject("{=oSfsqBwJ}No perks", null);
			int num = 0;
			foreach (PerkObject perkObject in governorPerksForHero)
			{
				bool flag = perkObject.PrimaryRole == PartyRole.Governor;
				bool flag2 = perkObject.SecondaryRole == PartyRole.Governor;
				if (flag)
				{
					TextObject perkText = ClanCardSelectionItemPropertyInfo.CreateLabeledValueText(perkObject.Name, perkObject.PrimaryDescription);
					this.SetPerksPropertyText(perkText, ref value, ref num);
				}
				if (flag2)
				{
					TextObject perkText2 = ClanCardSelectionItemPropertyInfo.CreateLabeledValueText(perkObject.Name, perkObject.SecondaryDescription);
					this.SetPerksPropertyText(perkText2, ref value, ref num);
				}
			}
			yield return new ClanCardSelectionItemPropertyInfo(GameTexts.FindText("str_clan_governor_perks", null), value);
			yield break;
		}

		// Token: 0x06001D0C RID: 7436 RVA: 0x0006B658 File Offset: 0x00069858
		private void SetPerksPropertyText(TextObject perkText, ref TextObject perksPropertyText, ref int addedPerkCount)
		{
			if (addedPerkCount == 0)
			{
				perksPropertyText = perkText;
			}
			else
			{
				TextObject textObject = GameTexts.FindText("str_string_newline_newline_string", null);
				textObject.SetTextVariable("STR1", perksPropertyText);
				textObject.SetTextVariable("STR2", perkText);
				perksPropertyText = textObject;
			}
			addedPerkCount++;
		}

		// Token: 0x06001D0D RID: 7437 RVA: 0x0006B6A0 File Offset: 0x000698A0
		private void OnGovernorSelectionOver(List<object> selectedItems, Action closePopup)
		{
			if (selectedItems.Count == 1)
			{
				ClanSettlementItemVM currentSelectedFief = this.CurrentSelectedFief;
				Hero hero;
				if (currentSelectedFief == null)
				{
					hero = null;
				}
				else
				{
					HeroVM governor = currentSelectedFief.Governor;
					hero = ((governor != null) ? governor.Hero : null);
				}
				Hero hero2 = hero;
				Hero newGovernor = selectedItems.FirstOrDefault<object>() as Hero;
				bool isRemoveGovernor = newGovernor == null;
				if (!isRemoveGovernor || hero2 != null)
				{
					ValueTuple<TextObject, TextObject> governorSelectionConfirmationPopupTexts = CampaignUIHelper.GetGovernorSelectionConfirmationPopupTexts(hero2, newGovernor, this.CurrentSelectedFief.Settlement);
					InformationManager.ShowInquiry(new InquiryData(governorSelectionConfirmationPopupTexts.Item1.ToString(), governorSelectionConfirmationPopupTexts.Item2.ToString(), true, true, GameTexts.FindText("str_yes", null).ToString(), GameTexts.FindText("str_no", null).ToString(), delegate()
					{
						Action closePopup4 = closePopup;
						if (closePopup4 != null)
						{
							closePopup4();
						}
						if (isRemoveGovernor)
						{
							ChangeGovernorAction.RemoveGovernorOfIfExists(this.CurrentSelectedFief.Settlement.Town);
						}
						else
						{
							ChangeGovernorAction.Apply(this.CurrentSelectedFief.Settlement.Town, newGovernor);
						}
						Action onRefresh = this._onRefresh;
						if (onRefresh == null)
						{
							return;
						}
						onRefresh();
					}, null, "", 0f, null, null, null), false, false);
					return;
				}
				Action closePopup2 = closePopup;
				if (closePopup2 == null)
				{
					return;
				}
				closePopup2();
				return;
			}
			else
			{
				Action closePopup3 = closePopup;
				if (closePopup3 == null)
				{
					return;
				}
				closePopup3();
				return;
			}
		}

		// Token: 0x06001D0E RID: 7438 RVA: 0x0006B7C4 File Offset: 0x000699C4
		private Settlement GetSettlementOfGovernor(Hero hero)
		{
			foreach (ClanSettlementItemVM clanSettlementItemVM in this.Settlements)
			{
				Hero hero2;
				if (clanSettlementItemVM == null)
				{
					hero2 = null;
				}
				else
				{
					HeroVM governor = clanSettlementItemVM.Governor;
					hero2 = ((governor != null) ? governor.Hero : null);
				}
				if (hero2 == hero)
				{
					return clanSettlementItemVM.Settlement;
				}
			}
			foreach (ClanSettlementItemVM clanSettlementItemVM2 in this.Castles)
			{
				Hero hero3;
				if (clanSettlementItemVM2 == null)
				{
					hero3 = null;
				}
				else
				{
					HeroVM governor2 = clanSettlementItemVM2.Governor;
					hero3 = ((governor2 != null) ? governor2.Hero : null);
				}
				if (hero3 == hero)
				{
					return clanSettlementItemVM2.Settlement;
				}
			}
			return null;
		}

		// Token: 0x06001D0F RID: 7439 RVA: 0x0006B88C File Offset: 0x00069A8C
		private void OnShowSendMembers()
		{
			ClanSettlementItemVM currentSelectedFief = this.CurrentSelectedFief;
			Settlement settlement = ((currentSelectedFief != null) ? currentSelectedFief.Settlement : null);
			if (settlement != null)
			{
				TextObject textObject = GameTexts.FindText("str_send_members", null);
				textObject.SetTextVariable("SETTLEMENT_NAME", settlement.Name);
				ClanCardSelectionInfo obj = new ClanCardSelectionInfo(textObject, this.GetSendMembersCandidates(), new Action<List<object>, Action>(this.OnSendMembersSelectionOver), true, 1, 0);
				Action<ClanCardSelectionInfo> openCardSelectionPopup = this._openCardSelectionPopup;
				if (openCardSelectionPopup == null)
				{
					return;
				}
				openCardSelectionPopup(obj);
			}
		}

		// Token: 0x06001D10 RID: 7440 RVA: 0x0006B8FB File Offset: 0x00069AFB
		private IEnumerable<ClanCardSelectionItemInfo> GetSendMembersCandidates()
		{
			foreach (Hero hero in (from h in this._clan.Heroes
				where !h.IsDisabled
				select h).Union(this._clan.Companions))
			{
				if ((hero.IsActive || hero.IsTraveling) && (hero.CurrentSettlement != this.CurrentSelectedFief.Settlement || hero.PartyBelongedTo != null) && !hero.IsChild && hero != Hero.MainHero)
				{
					TextObject disabledReason;
					bool flag = FactionHelper.IsMainClanMemberAvailableForSendingSettlement(hero, this.CurrentSelectedFief.Settlement, out disabledReason);
					SkillObject charm = DefaultSkills.Charm;
					int skillValue = hero.GetSkillValue(charm);
					CharacterImageIdentifier image = new CharacterImageIdentifier(CampaignUIHelper.GetCharacterCode(hero.CharacterObject, false));
					yield return new ClanCardSelectionItemInfo(hero, hero.Name, image, CardSelectionItemSpriteType.Skill, charm.StringId.ToLower(), skillValue.ToString(), this.GetSendMembersCandidateProperties(hero), !flag, disabledReason, null);
				}
			}
			IEnumerator<Hero> enumerator = null;
			yield break;
			yield break;
		}

		// Token: 0x06001D11 RID: 7441 RVA: 0x0006B90B File Offset: 0x00069B0B
		private IEnumerable<ClanCardSelectionItemPropertyInfo> GetSendMembersCandidateProperties(Hero hero)
		{
			TextObject teleportationDelayText = CampaignUIHelper.GetTeleportationDelayText(hero, this.CurrentSelectedFief.Settlement.Party);
			yield return new ClanCardSelectionItemPropertyInfo(teleportationDelayText);
			TextObject textObject = new TextObject("{=otaUtXMX}+{AMOUNT} relation chance with notables per day.", null);
			int emissaryRelationBonusForMainClan = Campaign.Current.Models.EmissaryModel.EmissaryRelationBonusForMainClan;
			textObject.SetTextVariable("AMOUNT", emissaryRelationBonusForMainClan);
			yield return new ClanCardSelectionItemPropertyInfo(textObject);
			yield break;
		}

		// Token: 0x06001D12 RID: 7442 RVA: 0x0006B924 File Offset: 0x00069B24
		private void OnSendMembersSelectionOver(List<object> selectedItems, Action closePopup)
		{
			if (selectedItems.Count > 0)
			{
				string variableName = "SETTLEMENT_NAME";
				ClanSettlementItemVM currentSelectedFief = this.CurrentSelectedFief;
				string text;
				if (currentSelectedFief == null)
				{
					text = null;
				}
				else
				{
					Settlement settlement = currentSelectedFief.Settlement;
					if (settlement == null)
					{
						text = null;
					}
					else
					{
						TextObject name = settlement.Name;
						text = ((name != null) ? name.ToString() : null);
					}
				}
				MBTextManager.SetTextVariable(variableName, text ?? string.Empty, false);
				InformationManager.ShowInquiry(new InquiryData(GameTexts.FindText("str_send_members", null).ToString(), GameTexts.FindText("str_send_members_inquiry", null).ToString(), true, true, GameTexts.FindText("str_yes", null).ToString(), GameTexts.FindText("str_no", null).ToString(), delegate()
				{
					Action closePopup3 = closePopup;
					if (closePopup3 != null)
					{
						closePopup3();
					}
					using (List<object>.Enumerator enumerator = selectedItems.GetEnumerator())
					{
						while (enumerator.MoveNext())
						{
							Hero heroToBeMoved;
							if ((heroToBeMoved = enumerator.Current as Hero) != null)
							{
								TeleportHeroAction.ApplyDelayedTeleportToSettlement(heroToBeMoved, this.CurrentSelectedFief.Settlement);
							}
						}
					}
					Action onRefresh = this._onRefresh;
					if (onRefresh == null)
					{
						return;
					}
					onRefresh();
				}, null, "", 0f, null, null, null), false, false);
				return;
			}
			Action closePopup2 = closePopup;
			if (closePopup2 == null)
			{
				return;
			}
			closePopup2();
		}

		// Token: 0x170009DB RID: 2523
		// (get) Token: 0x06001D13 RID: 7443 RVA: 0x0006BA14 File Offset: 0x00069C14
		// (set) Token: 0x06001D14 RID: 7444 RVA: 0x0006BA1C File Offset: 0x00069C1C
		[DataSourceProperty]
		public string GovernorActionText
		{
			get
			{
				return this._governorActionText;
			}
			set
			{
				if (value != this._governorActionText)
				{
					this._governorActionText = value;
					base.OnPropertyChangedWithValue<string>(value, "GovernorActionText");
				}
			}
		}

		// Token: 0x170009DC RID: 2524
		// (get) Token: 0x06001D15 RID: 7445 RVA: 0x0006BA3F File Offset: 0x00069C3F
		// (set) Token: 0x06001D16 RID: 7446 RVA: 0x0006BA47 File Offset: 0x00069C47
		[DataSourceProperty]
		public bool CanChangeGovernorOfCurrentFief
		{
			get
			{
				return this._canChangeGovernorOfCurrentFief;
			}
			set
			{
				if (value != this._canChangeGovernorOfCurrentFief)
				{
					this._canChangeGovernorOfCurrentFief = value;
					base.OnPropertyChangedWithValue(value, "CanChangeGovernorOfCurrentFief");
				}
			}
		}

		// Token: 0x170009DD RID: 2525
		// (get) Token: 0x06001D17 RID: 7447 RVA: 0x0006BA65 File Offset: 0x00069C65
		// (set) Token: 0x06001D18 RID: 7448 RVA: 0x0006BA6D File Offset: 0x00069C6D
		[DataSourceProperty]
		public HintViewModel GovernorActionHint
		{
			get
			{
				return this._governorActionHint;
			}
			set
			{
				if (value != this._governorActionHint)
				{
					this._governorActionHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "GovernorActionHint");
				}
			}
		}

		// Token: 0x170009DE RID: 2526
		// (get) Token: 0x06001D19 RID: 7449 RVA: 0x0006BA8B File Offset: 0x00069C8B
		// (set) Token: 0x06001D1A RID: 7450 RVA: 0x0006BA93 File Offset: 0x00069C93
		[DataSourceProperty]
		public bool IsAnyValidFiefSelected
		{
			get
			{
				return this._isAnyValidFiefSelected;
			}
			set
			{
				if (value != this._isAnyValidFiefSelected)
				{
					this._isAnyValidFiefSelected = value;
					base.OnPropertyChangedWithValue(value, "IsAnyValidFiefSelected");
				}
			}
		}

		// Token: 0x170009DF RID: 2527
		// (get) Token: 0x06001D1B RID: 7451 RVA: 0x0006BAB1 File Offset: 0x00069CB1
		// (set) Token: 0x06001D1C RID: 7452 RVA: 0x0006BAB9 File Offset: 0x00069CB9
		[DataSourceProperty]
		public string NameText
		{
			get
			{
				return this._nameText;
			}
			set
			{
				if (value != this._nameText)
				{
					this._nameText = value;
					base.OnPropertyChangedWithValue<string>(value, "NameText");
				}
			}
		}

		// Token: 0x170009E0 RID: 2528
		// (get) Token: 0x06001D1D RID: 7453 RVA: 0x0006BADC File Offset: 0x00069CDC
		// (set) Token: 0x06001D1E RID: 7454 RVA: 0x0006BAE4 File Offset: 0x00069CE4
		[DataSourceProperty]
		public string TaxText
		{
			get
			{
				return this._taxText;
			}
			set
			{
				if (value != this._taxText)
				{
					this._taxText = value;
					base.OnPropertyChangedWithValue<string>(value, "TaxText");
				}
			}
		}

		// Token: 0x170009E1 RID: 2529
		// (get) Token: 0x06001D1F RID: 7455 RVA: 0x0006BB07 File Offset: 0x00069D07
		// (set) Token: 0x06001D20 RID: 7456 RVA: 0x0006BB0F File Offset: 0x00069D0F
		[DataSourceProperty]
		public string GovernorText
		{
			get
			{
				return this._governorText;
			}
			set
			{
				if (value != this._governorText)
				{
					this._governorText = value;
					base.OnPropertyChangedWithValue<string>(value, "GovernorText");
				}
			}
		}

		// Token: 0x170009E2 RID: 2530
		// (get) Token: 0x06001D21 RID: 7457 RVA: 0x0006BB32 File Offset: 0x00069D32
		// (set) Token: 0x06001D22 RID: 7458 RVA: 0x0006BB3A File Offset: 0x00069D3A
		[DataSourceProperty]
		public string ProfitText
		{
			get
			{
				return this._profitText;
			}
			set
			{
				if (value != this._profitText)
				{
					this._profitText = value;
					base.OnPropertyChangedWithValue<string>(value, "ProfitText");
				}
			}
		}

		// Token: 0x170009E3 RID: 2531
		// (get) Token: 0x06001D23 RID: 7459 RVA: 0x0006BB5D File Offset: 0x00069D5D
		// (set) Token: 0x06001D24 RID: 7460 RVA: 0x0006BB65 File Offset: 0x00069D65
		[DataSourceProperty]
		public string TownsText
		{
			get
			{
				return this._townsText;
			}
			set
			{
				if (value != this._townsText)
				{
					this._townsText = value;
					base.OnPropertyChangedWithValue<string>(value, "TownsText");
				}
			}
		}

		// Token: 0x170009E4 RID: 2532
		// (get) Token: 0x06001D25 RID: 7461 RVA: 0x0006BB88 File Offset: 0x00069D88
		// (set) Token: 0x06001D26 RID: 7462 RVA: 0x0006BB90 File Offset: 0x00069D90
		[DataSourceProperty]
		public string CastlesText
		{
			get
			{
				return this._castlesText;
			}
			set
			{
				if (value != this._castlesText)
				{
					this._castlesText = value;
					base.OnPropertyChangedWithValue<string>(value, "CastlesText");
				}
			}
		}

		// Token: 0x170009E5 RID: 2533
		// (get) Token: 0x06001D27 RID: 7463 RVA: 0x0006BBB3 File Offset: 0x00069DB3
		// (set) Token: 0x06001D28 RID: 7464 RVA: 0x0006BBBB File Offset: 0x00069DBB
		[DataSourceProperty]
		public string NoFiefsText
		{
			get
			{
				return this._noFiefsText;
			}
			set
			{
				if (value != this._noFiefsText)
				{
					this._noFiefsText = value;
					base.OnPropertyChangedWithValue<string>(value, "NoFiefsText");
				}
			}
		}

		// Token: 0x170009E6 RID: 2534
		// (get) Token: 0x06001D29 RID: 7465 RVA: 0x0006BBDE File Offset: 0x00069DDE
		// (set) Token: 0x06001D2A RID: 7466 RVA: 0x0006BBE6 File Offset: 0x00069DE6
		[DataSourceProperty]
		public string NoGovernorText
		{
			get
			{
				return this._noGovernorText;
			}
			set
			{
				if (value != this._noGovernorText)
				{
					this._noGovernorText = value;
					base.OnPropertyChangedWithValue<string>(value, "NoGovernorText");
				}
			}
		}

		// Token: 0x170009E7 RID: 2535
		// (get) Token: 0x06001D2B RID: 7467 RVA: 0x0006BC09 File Offset: 0x00069E09
		// (set) Token: 0x06001D2C RID: 7468 RVA: 0x0006BC11 File Offset: 0x00069E11
		[DataSourceProperty]
		public bool IsSelected
		{
			get
			{
				return this._isSelected;
			}
			set
			{
				if (value != this._isSelected)
				{
					this._isSelected = value;
					base.OnPropertyChangedWithValue(value, "IsSelected");
				}
			}
		}

		// Token: 0x170009E8 RID: 2536
		// (get) Token: 0x06001D2D RID: 7469 RVA: 0x0006BC2F File Offset: 0x00069E2F
		// (set) Token: 0x06001D2E RID: 7470 RVA: 0x0006BC37 File Offset: 0x00069E37
		[DataSourceProperty]
		public MBBindingList<ClanSettlementItemVM> Settlements
		{
			get
			{
				return this._settlements;
			}
			set
			{
				if (value != this._settlements)
				{
					this._settlements = value;
					base.OnPropertyChangedWithValue<MBBindingList<ClanSettlementItemVM>>(value, "Settlements");
				}
			}
		}

		// Token: 0x170009E9 RID: 2537
		// (get) Token: 0x06001D2F RID: 7471 RVA: 0x0006BC55 File Offset: 0x00069E55
		// (set) Token: 0x06001D30 RID: 7472 RVA: 0x0006BC5D File Offset: 0x00069E5D
		[DataSourceProperty]
		public MBBindingList<ClanSettlementItemVM> Castles
		{
			get
			{
				return this._castles;
			}
			set
			{
				if (value != this._castles)
				{
					this._castles = value;
					base.OnPropertyChangedWithValue<MBBindingList<ClanSettlementItemVM>>(value, "Castles");
				}
			}
		}

		// Token: 0x170009EA RID: 2538
		// (get) Token: 0x06001D31 RID: 7473 RVA: 0x0006BC7B File Offset: 0x00069E7B
		// (set) Token: 0x06001D32 RID: 7474 RVA: 0x0006BC83 File Offset: 0x00069E83
		[DataSourceProperty]
		public ClanSettlementItemVM CurrentSelectedFief
		{
			get
			{
				return this._currentSelectedFief;
			}
			set
			{
				if (value != this._currentSelectedFief)
				{
					this._currentSelectedFief = value;
					base.OnPropertyChangedWithValue<ClanSettlementItemVM>(value, "CurrentSelectedFief");
					this.IsAnyValidFiefSelected = value != null;
				}
			}
		}

		// Token: 0x170009EB RID: 2539
		// (get) Token: 0x06001D33 RID: 7475 RVA: 0x0006BCAB File Offset: 0x00069EAB
		// (set) Token: 0x06001D34 RID: 7476 RVA: 0x0006BCB3 File Offset: 0x00069EB3
		[DataSourceProperty]
		public ClanFiefsSortControllerVM SortController
		{
			get
			{
				return this._sortController;
			}
			set
			{
				if (value != this._sortController)
				{
					this._sortController = value;
					base.OnPropertyChangedWithValue<ClanFiefsSortControllerVM>(value, "SortController");
				}
			}
		}

		// Token: 0x04000D8A RID: 3466
		private readonly Clan _clan;

		// Token: 0x04000D8B RID: 3467
		private readonly Action _onRefresh;

		// Token: 0x04000D8C RID: 3468
		private readonly Action<ClanCardSelectionInfo> _openCardSelectionPopup;

		// Token: 0x04000D8D RID: 3469
		private readonly ITeleportationCampaignBehavior _teleportationBehavior;

		// Token: 0x04000D8E RID: 3470
		private readonly TextObject _noGovernorTextSource = new TextObject("{=zLFsnaqR}No Governor", null);

		// Token: 0x04000D8F RID: 3471
		private MBBindingList<ClanSettlementItemVM> _settlements;

		// Token: 0x04000D90 RID: 3472
		private MBBindingList<ClanSettlementItemVM> _castles;

		// Token: 0x04000D91 RID: 3473
		private ClanSettlementItemVM _currentSelectedFief;

		// Token: 0x04000D92 RID: 3474
		private bool _isSelected;

		// Token: 0x04000D93 RID: 3475
		private string _nameText;

		// Token: 0x04000D94 RID: 3476
		private string _taxText;

		// Token: 0x04000D95 RID: 3477
		private string _governorText;

		// Token: 0x04000D96 RID: 3478
		private string _profitText;

		// Token: 0x04000D97 RID: 3479
		private string _townsText;

		// Token: 0x04000D98 RID: 3480
		private string _castlesText;

		// Token: 0x04000D99 RID: 3481
		private string _noFiefsText;

		// Token: 0x04000D9A RID: 3482
		private string _noGovernorText;

		// Token: 0x04000D9B RID: 3483
		private bool _isAnyValidFiefSelected;

		// Token: 0x04000D9C RID: 3484
		private bool _canChangeGovernorOfCurrentFief;

		// Token: 0x04000D9D RID: 3485
		private HintViewModel _governorActionHint;

		// Token: 0x04000D9E RID: 3486
		private string _governorActionText;

		// Token: 0x04000D9F RID: 3487
		private ClanFiefsSortControllerVM _sortController;
	}
}
