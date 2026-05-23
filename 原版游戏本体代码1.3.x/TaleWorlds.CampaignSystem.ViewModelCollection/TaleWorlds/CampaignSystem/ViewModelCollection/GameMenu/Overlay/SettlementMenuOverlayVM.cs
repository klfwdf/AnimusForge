using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Events;
using TaleWorlds.CampaignSystem.ViewModelCollection.Quests;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Core.ViewModelCollection.Tutorial;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Overlay
{
	// Token: 0x020000BD RID: 189
	[MenuOverlay("SettlementMenuOverlay")]
	public class SettlementMenuOverlayVM : GameMenuOverlay
	{
		// Token: 0x06001273 RID: 4723 RVA: 0x0004A52C File Offset: 0x0004872C
		public SettlementMenuOverlayVM(GameMenu.MenuOverlayType type)
		{
			this._type = type;
			this._overlayTalkItem = null;
			base.IsInitializationOver = false;
			this._settlement = Settlement.CurrentSettlement;
			this.CharacterList = new MBBindingList<GameMenuPartyItemVM>();
			this.PartyList = new MBBindingList<GameMenuPartyItemVM>();
			this.IssueList = new MBBindingList<StringItemWithHintVM>();
			base.CurrentOverlayType = 0;
			this.CrimeHint = new BasicTooltipViewModel(() => this.GetCrimeTooltip());
			if (Settlement.CurrentSettlement.IsFortification)
			{
				this.RemainingFoodHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetTownFoodTooltip(this._settlement.Town));
				this.LoyaltyHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetTownLoyaltyTooltip(this._settlement.Town));
				this.MilitasHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetTownMilitiaTooltip(this._settlement.Town));
				this.ProsperityHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetTownProsperityTooltip(this._settlement.Town));
				this.WallsHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetTownWallsTooltip(this._settlement.Town));
				this.GarrisonHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetTownGarrisonTooltip(this._settlement.Town));
				this.SecurityHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetTownSecurityTooltip(this._settlement.Town));
			}
			else
			{
				this.MilitasHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetVillageMilitiaTooltip(this._settlement.Village));
				this.LoyaltyHint = new BasicTooltipViewModel();
				this.WallsHint = new BasicTooltipViewModel();
				this.ProsperityHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetVillageProsperityTooltip(this._settlement.Village));
			}
			this.UpdateSettlementOwnerBanner();
			this._contextMenuItem = null;
			base.IsInitializationOver = true;
			CampaignEvents.AfterSettlementEntered.AddNonSerializedListener(this, new Action<MobileParty, Settlement, Hero>(this.OnSettlementEntered));
			CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, new Action<MobileParty, Settlement>(this.OnSettlementLeft));
			CampaignEvents.OnQuestCompletedEvent.AddNonSerializedListener(this, new Action<QuestBase, QuestBase.QuestCompleteDetails>(this.OnQuestCompleted));
			CampaignEvents.WarDeclared.AddNonSerializedListener(this, new Action<IFaction, IFaction, DeclareWarAction.DeclareWarDetail>(this.OnWarDeclared));
			CampaignEvents.MakePeace.AddNonSerializedListener(this, new Action<IFaction, IFaction, MakePeaceAction.MakePeaceDetail>(this.OnPeaceDeclared));
			CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, new Action<Settlement, bool, Hero, Hero, Hero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail>(this.OnSettlementOwnerChanged));
			CampaignEvents.TownRebelliosStateChanged.AddNonSerializedListener(this, new Action<Town, bool>(this.OnTownRebelliousStateChanged));
			Game.Current.EventManager.RegisterEvent<TutorialNotificationElementChangeEvent>(new Action<TutorialNotificationElementChangeEvent>(this.OnTutorialNotificationElementIDChange));
			this.RefreshValues();
		}

		// Token: 0x06001274 RID: 4724 RVA: 0x0004A775 File Offset: 0x00048975
		private List<TooltipProperty> GetCrimeTooltip()
		{
			Game game = Game.Current;
			if (game != null)
			{
				game.EventManager.TriggerEvent<CrimeValueInspectedInSettlementOverlayEvent>(new CrimeValueInspectedInSettlementOverlayEvent());
			}
			return CampaignUIHelper.GetCrimeTooltip(this._settlement);
		}

		// Token: 0x06001275 RID: 4725 RVA: 0x0004A79C File Offset: 0x0004899C
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.PartyFilterHint = new HintViewModel(GameTexts.FindText("str_parties", null), null);
			this.CharacterFilterHint = new HintViewModel(GameTexts.FindText("str_characters", null), null);
			this.Refresh();
		}

		// Token: 0x06001276 RID: 4726 RVA: 0x0004A7D8 File Offset: 0x000489D8
		protected override void ExecuteOnSetAsActiveContextMenuItem(GameMenuPartyItemVM troop)
		{
			base.ExecuteOnSetAsActiveContextMenuItem(troop);
			base.ContextList.Clear();
			this.IssueList.Clear();
			if (this._contextMenuItem.Character != null && (!this._contextMenuItem.Character.IsHero || !this._contextMenuItem.Character.HeroObject.IsPrisoner))
			{
				bool isEnabled = true;
				TextObject hint = TextObject.GetEmpty();
				this._mostRecentOverlayTalkPermission = null;
				Game.Current.EventManager.TriggerEvent<SettlementOverlayTalkPermissionEvent>(new SettlementOverlayTalkPermissionEvent(this._contextMenuItem.Character.HeroObject, new Action<bool, TextObject>(this.OnSettlementOverlayTalkPermissionResult)));
				if (this._mostRecentOverlayTalkPermission != null)
				{
					isEnabled = this._mostRecentOverlayTalkPermission.Item1;
					hint = this._mostRecentOverlayTalkPermission.Item2;
				}
				this._overlayTalkItem = new GameMenuOverlayActionVM(new Action<object>(base.ExecuteTroopAction), GameTexts.FindText("str_menu_overlay_context_list", "Conversation").ToString(), isEnabled, GameMenuOverlay.MenuOverlayContextList.Conversation, hint);
				base.ContextList.Add(this._overlayTalkItem);
				bool isEnabled2 = true;
				TextObject hint2 = TextObject.GetEmpty();
				this._mostRecentOverlayQuickTalkPermission = null;
				Game.Current.EventManager.TriggerEvent<SettlementOverylayQuickTalkPermissionEvent>(new SettlementOverylayQuickTalkPermissionEvent(this._contextMenuItem.Character.HeroObject, new Action<bool, TextObject>(this.OnSettlementOverlayQuickTalkPermissionResult)));
				if (this._mostRecentOverlayQuickTalkPermission != null)
				{
					isEnabled2 = this._mostRecentOverlayQuickTalkPermission.Item1;
					hint2 = this._mostRecentOverlayQuickTalkPermission.Item2;
				}
				this._overlayQuickTalkItem = new GameMenuOverlayActionVM(new Action<object>(base.ExecuteTroopAction), GameTexts.FindText("str_menu_overlay_context_list", "QuickConversation").ToString(), isEnabled2, GameMenuOverlay.MenuOverlayContextList.QuickConversation, hint2);
				base.ContextList.Add(this._overlayQuickTalkItem);
				foreach (QuestMarkerVM questMarkerVM in troop.Quests)
				{
					if (questMarkerVM.IssueQuestFlag != CampaignUIHelper.IssueQuestFlags.None)
					{
						GameTexts.SetVariable("STR2", questMarkerVM.QuestTitle);
						string content = string.Empty;
						if (questMarkerVM.IssueQuestFlag == CampaignUIHelper.IssueQuestFlags.ActiveIssue)
						{
							content = "{=!}<img src=\"General\\Icons\\icon_issue_active_square\" extend=\"4\">";
						}
						else if (questMarkerVM.IssueQuestFlag == CampaignUIHelper.IssueQuestFlags.AvailableIssue)
						{
							content = "{=!}<img src=\"General\\Icons\\icon_issue_available_square\" extend=\"4\">";
						}
						else if (questMarkerVM.IssueQuestFlag == CampaignUIHelper.IssueQuestFlags.ActiveStoryQuest)
						{
							content = "{=!}<img src=\"General\\Icons\\icon_story_quest_active_square\" extend=\"4\">";
						}
						else if (questMarkerVM.IssueQuestFlag == CampaignUIHelper.IssueQuestFlags.TrackedIssue)
						{
							content = "{=!}<img src=\"General\\Icons\\issue_target_icon\" extend=\"4\">";
						}
						else if (questMarkerVM.IssueQuestFlag == CampaignUIHelper.IssueQuestFlags.TrackedStoryQuest)
						{
							content = "{=!}<img src=\"General\\Icons\\quest_target_icon\" extend=\"4\">";
						}
						GameTexts.SetVariable("STR1", content);
						string text = GameTexts.FindText("str_STR1_STR2", null).ToString();
						this.IssueList.Add(new StringItemWithHintVM(text, questMarkerVM.QuestHint.HintText));
					}
				}
				if (this._contextMenuItem.Character.IsHero)
				{
					MobileParty partyBelongedTo = this._contextMenuItem.Character.HeroObject.PartyBelongedTo;
					if (((partyBelongedTo != null) ? partyBelongedTo.Army : null) != null && this._contextMenuItem.Character.HeroObject.PartyBelongedTo.Army.LeaderParty == this._contextMenuItem.Character.HeroObject.PartyBelongedTo && MobileParty.MainParty.Army == null && DiplomacyHelper.IsSameFactionAndNotEliminated(this._contextMenuItem.Character.HeroObject.MapFaction, Hero.MainHero.MapFaction))
					{
						GameMenuOverlayActionVM item = new GameMenuOverlayActionVM(new Action<object>(base.ExecuteTroopAction), GameTexts.FindText("str_menu_overlay_context_list", "JoinArmy").ToString(), true, GameMenuOverlay.MenuOverlayContextList.JoinArmy, null);
						base.ContextList.Add(item);
					}
				}
				if (this._contextMenuItem.Character.IsHero && this._contextMenuItem.Character.HeroObject.PartyBelongedTo == null && this._contextMenuItem.Character.HeroObject.Clan == Clan.PlayerClan && this._contextMenuItem.Character.HeroObject.Age > (float)Campaign.Current.Models.AgeModel.HeroComesOfAge && !Campaign.Current.GetCampaignBehavior<IAlleyCampaignBehavior>().IsHeroAlleyLeaderOfAnyPlayerAlley(this._contextMenuItem.Character.HeroObject))
				{
					GameMenuOverlayActionVM item2 = new GameMenuOverlayActionVM(new Action<object>(base.ExecuteTroopAction), GameTexts.FindText("str_menu_overlay_context_list", "TakeToParty").ToString(), true, GameMenuOverlay.MenuOverlayContextList.TakeToParty, null);
					base.ContextList.Add(item2);
				}
				CampaignEventDispatcher.Instance.OnCharacterPortraitPopUpOpened(this._contextMenuItem.Character);
				return;
			}
			if (this._contextMenuItem.Party != null)
			{
				Hero owner = this._contextMenuItem.Party.Owner;
				if (((owner != null) ? owner.Clan : null) == Hero.MainHero.Clan)
				{
					MobileParty mobileParty = this._contextMenuItem.Party.MobileParty;
					if (mobileParty != null && !mobileParty.IsMainParty)
					{
						MobileParty mobileParty2 = this._contextMenuItem.Party.MobileParty;
						if (mobileParty2 != null && mobileParty2.IsGarrison)
						{
							this._overlayTalkItem = new GameMenuOverlayActionVM(new Action<object>(base.ExecuteTroopAction), GameTexts.FindText("str_menu_overlay_context_list", "ManageGarrison").ToString(), true, GameMenuOverlay.MenuOverlayContextList.ManageGarrison, null);
							base.ContextList.Add(this._overlayTalkItem);
							goto IL_685;
						}
					}
				}
				if (this._contextMenuItem.Party.MapFaction == Hero.MainHero.MapFaction)
				{
					MobileParty mobileParty3 = this._contextMenuItem.Party.MobileParty;
					if (mobileParty3 != null && !mobileParty3.IsMainParty && (this._contextMenuItem.Party.MobileParty == null || (!this._contextMenuItem.Party.MobileParty.IsVillager && !this._contextMenuItem.Party.MobileParty.IsCaravan && !this._contextMenuItem.Party.MobileParty.IsPatrolParty && !this._contextMenuItem.Party.MobileParty.IsMilitia)))
					{
						if (this._contextMenuItem.Party.MobileParty.ActualClan == Clan.PlayerClan)
						{
							this._overlayTalkItem = new GameMenuOverlayActionVM(new Action<object>(base.ExecuteTroopAction), GameTexts.FindText("str_menu_overlay_context_list", "ManageTroops").ToString(), true, GameMenuOverlay.MenuOverlayContextList.ManageTroops, null);
							base.ContextList.Add(this._overlayTalkItem);
						}
						else
						{
							this._overlayTalkItem = new GameMenuOverlayActionVM(new Action<object>(base.ExecuteTroopAction), GameTexts.FindText("str_menu_overlay_context_list", "DonateTroops").ToString(), true, GameMenuOverlay.MenuOverlayContextList.DonateTroops, null);
							base.ContextList.Add(this._overlayTalkItem);
						}
					}
				}
				IL_685:
				if (this._contextMenuItem.Party.LeaderHero != null && this._contextMenuItem.Party.LeaderHero != Hero.MainHero)
				{
					bool flag = this.CharacterList.Any((GameMenuPartyItemVM c) => c.Character == this._contextMenuItem.Party.LeaderHero.CharacterObject);
					TextObject hintText = ((!flag) ? GameTexts.FindText("str_menu_overlay_cant_talk_to_party_leader", null) : TextObject.GetEmpty());
					base.ContextList.Add(new StringItemWithEnabledAndHintVM(new Action<object>(base.ExecuteTroopAction), GameTexts.FindText("str_menu_overlay_context_list", "ConverseWithLeader").ToString(), flag, GameMenuOverlay.MenuOverlayContextList.ConverseWithLeader, hintText));
				}
				CharacterObject visualPartyLeader = CampaignUIHelper.GetVisualPartyLeader(this._contextMenuItem.Party);
				if (visualPartyLeader != null)
				{
					CampaignEventDispatcher.Instance.OnCharacterPortraitPopUpOpened(visualPartyLeader);
				}
			}
		}

		// Token: 0x06001277 RID: 4727 RVA: 0x0004AF34 File Offset: 0x00049134
		private void OnSettlementOverlayTalkPermissionResult(bool isAvailable, TextObject reasonStr)
		{
			this._mostRecentOverlayTalkPermission = new Tuple<bool, TextObject>(isAvailable, reasonStr);
		}

		// Token: 0x06001278 RID: 4728 RVA: 0x0004AF43 File Offset: 0x00049143
		private void OnSettlementOverlayQuickTalkPermissionResult(bool isAvailable, TextObject reasonStr)
		{
			this._mostRecentOverlayQuickTalkPermission = new Tuple<bool, TextObject>(isAvailable, reasonStr);
		}

		// Token: 0x06001279 RID: 4729 RVA: 0x0004AF52 File Offset: 0x00049152
		private void OnSettlementOverlayLeaveCharacterPermissionResult(bool isAvailable, TextObject reasonStr)
		{
			this._mostRecentOverlayLeaveCharacterPermission = new Tuple<bool, TextObject>(isAvailable, reasonStr);
		}

		// Token: 0x0600127A RID: 4730 RVA: 0x0004AF61 File Offset: 0x00049161
		public override void ExecuteOnOverlayClosed()
		{
			base.ExecuteOnOverlayClosed();
			this.InitLists();
			base.ContextList.Clear();
		}

		// Token: 0x0600127B RID: 4731 RVA: 0x0004AF7A File Offset: 0x0004917A
		private void ExecuteCloseTooltip()
		{
			MBInformationManager.HideInformations();
		}

		// Token: 0x0600127C RID: 4732 RVA: 0x0004AF81 File Offset: 0x00049181
		private void ExecuteOpenTooltip()
		{
			InformationManager.ShowTooltip(typeof(Settlement), new object[] { this._settlement, true });
		}

		// Token: 0x0600127D RID: 4733 RVA: 0x0004AFAA File Offset: 0x000491AA
		private void ExecuteSettlementLink()
		{
			Campaign.Current.EncyclopediaManager.GoToLink(this._settlement.EncyclopediaLink);
		}

		// Token: 0x0600127E RID: 4734 RVA: 0x0004AFC8 File Offset: 0x000491C8
		private bool Contains(MBBindingList<GameMenuPartyItemVM> list, CharacterObject character)
		{
			using (IEnumerator<GameMenuPartyItemVM> enumerator = list.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (enumerator.Current.Character == character)
					{
						return true;
					}
				}
			}
			return false;
		}

		// Token: 0x0600127F RID: 4735 RVA: 0x0004B018 File Offset: 0x00049218
		public override void UpdateOverlayType(GameMenu.MenuOverlayType newType)
		{
			this._type = newType;
			base.UpdateOverlayType(newType);
		}

		// Token: 0x06001280 RID: 4736 RVA: 0x0004B028 File Offset: 0x00049228
		private void InitLists()
		{
			this.UpdateCharacterList();
			this.UpdatePartyList();
		}

		// Token: 0x06001281 RID: 4737 RVA: 0x0004B038 File Offset: 0x00049238
		private void UpdateCharacterList()
		{
			if (this._type == GameMenu.MenuOverlayType.SettlementWithCharacters || this._type == GameMenu.MenuOverlayType.SettlementWithBoth)
			{
				Dictionary<Hero, bool> dictionary = new Dictionary<Hero, bool>();
				foreach (LocationCharacter locationCharacter in Campaign.Current.GameMenuManager.MenuLocations.SelectMany((Location l) => l.GetCharacterList()))
				{
					if (Campaign.Current.Models.HeroAgentLocationModel.WillBeListedInOverlay(locationCharacter) && !dictionary.ContainsKey(locationCharacter.Character.HeroObject))
					{
						dictionary.Add(locationCharacter.Character.HeroObject, locationCharacter.UseCivilianEquipment);
					}
				}
				for (int i = this.CharacterList.Count - 1; i >= 0; i--)
				{
					GameMenuPartyItemVM gameMenuPartyItemVM = this.CharacterList[i];
					if (!dictionary.ContainsKey(gameMenuPartyItemVM.Character.HeroObject))
					{
						this.CharacterList.RemoveAt(i);
					}
				}
				using (Dictionary<Hero, bool>.Enumerator enumerator2 = dictionary.GetEnumerator())
				{
					while (enumerator2.MoveNext())
					{
						KeyValuePair<Hero, bool> heroKvp = enumerator2.Current;
						if (!this.CharacterList.Any((GameMenuPartyItemVM x) => x.Character == heroKvp.Key.CharacterObject))
						{
							GameMenuPartyItemVM item = new GameMenuPartyItemVM(new Action<GameMenuPartyItemVM>(this.ExecuteOnSetAsActiveContextMenuItem), heroKvp.Key.CharacterObject, heroKvp.Value);
							this.CharacterList.Add(item);
						}
					}
				}
				this.CharacterList.Sort(new SettlementMenuOverlayVM.CharacterComparer());
				return;
			}
			this.CharacterList.Clear();
		}

		// Token: 0x06001282 RID: 4738 RVA: 0x0004B208 File Offset: 0x00049408
		private void UpdatePartyList()
		{
			if (this._type == GameMenu.MenuOverlayType.SettlementWithBoth || this._type == GameMenu.MenuOverlayType.SettlementWithParties)
			{
				SettlementMenuOverlayVM.<>c__DisplayClass22_0 CS$<>8__locals1 = new SettlementMenuOverlayVM.<>c__DisplayClass22_0();
				Settlement settlement = MobileParty.MainParty.CurrentSettlement ?? MobileParty.MainParty.LastVisitedSettlement;
				CS$<>8__locals1.partiesInSettlement = new List<MobileParty>();
				foreach (MobileParty mobileParty in settlement.Parties)
				{
					if (this.WillBeListed(mobileParty))
					{
						CS$<>8__locals1.partiesInSettlement.Add(mobileParty);
					}
				}
				for (int j = this.PartyList.Count - 1; j >= 0; j--)
				{
					GameMenuPartyItemVM gameMenuPartyItemVM = this.PartyList[j];
					if (!CS$<>8__locals1.partiesInSettlement.Contains(gameMenuPartyItemVM.Party.MobileParty))
					{
						this.PartyList.RemoveAt(j);
					}
				}
				int i2;
				int i;
				for (i = 0; i < CS$<>8__locals1.partiesInSettlement.Count; i = i2 + 1)
				{
					if (!this.PartyList.Any((GameMenuPartyItemVM x) => x.Party == CS$<>8__locals1.partiesInSettlement[i].Party))
					{
						GameMenuPartyItemVM item = new GameMenuPartyItemVM(new Action<GameMenuPartyItemVM>(this.ExecuteOnSetAsActiveContextMenuItem), CS$<>8__locals1.partiesInSettlement[i].Party, false);
						this.PartyList.Add(item);
					}
					i2 = i;
				}
				this.PartyList.Sort(new SettlementMenuOverlayVM.PartyComparer());
				return;
			}
			this.PartyList.Clear();
		}

		// Token: 0x06001283 RID: 4739 RVA: 0x0004B3B0 File Offset: 0x000495B0
		private void UpdateList<TListItem, TElement>(MBBindingList<TListItem> listToUpdate, IEnumerable<TElement> listInSettlement, IComparer<TListItem> comparer, Func<TListItem, TElement> getElementFromListItem, Func<TElement, bool> doesSettlementHasElement, Func<TElement, TListItem> createListItem)
		{
			HashSet<TElement> hashSet = new HashSet<TElement>();
			for (int i = 0; i < listToUpdate.Count; i++)
			{
				TListItem arg = listToUpdate[i];
				TElement telement = getElementFromListItem(arg);
				if (doesSettlementHasElement(telement))
				{
					hashSet.Add(telement);
				}
				else
				{
					listToUpdate.RemoveAt(i);
					i--;
				}
			}
			foreach (TElement telement2 in listInSettlement)
			{
				if (!hashSet.Contains(telement2))
				{
					listToUpdate.Add(createListItem(telement2));
					hashSet.Add(telement2);
				}
			}
			listToUpdate.Sort(comparer);
		}

		// Token: 0x06001284 RID: 4740 RVA: 0x0004B468 File Offset: 0x00049668
		private bool WillBeListed(MobileParty mobileParty)
		{
			return mobileParty != null && mobileParty.IsActive;
		}

		// Token: 0x06001285 RID: 4741 RVA: 0x0004B478 File Offset: 0x00049678
		private bool WillBeListed(CharacterObject character)
		{
			Settlement settlement = ((MobileParty.MainParty.CurrentSettlement != null) ? MobileParty.MainParty.CurrentSettlement : MobileParty.MainParty.LastVisitedSettlement);
			return character.IsHero && character.HeroObject.PartyBelongedTo != MobileParty.MainParty && character.HeroObject.CurrentSettlement == settlement;
		}

		// Token: 0x06001286 RID: 4742 RVA: 0x0004B4D4 File Offset: 0x000496D4
		private void UpdateSettlementOwnerBanner()
		{
			Banner banner = null;
			IFaction mapFaction = this._settlement.MapFaction;
			if (mapFaction != null && mapFaction.IsKingdomFaction && ((Kingdom)this._settlement.MapFaction).RulingClan == this._settlement.OwnerClan)
			{
				banner = this._settlement.OwnerClan.Kingdom.Banner;
			}
			else
			{
				Clan ownerClan = this._settlement.OwnerClan;
				if (((ownerClan != null) ? ownerClan.Banner : null) != null)
				{
					banner = this._settlement.OwnerClan.Banner;
				}
			}
			if (banner != null)
			{
				this.SettlementOwnerBanner = new BannerImageIdentifierVM(banner, true);
				return;
			}
			this.SettlementOwnerBanner = new BannerImageIdentifierVM(null, false);
		}

		// Token: 0x06001287 RID: 4743 RVA: 0x0004B580 File Offset: 0x00049780
		private void UpdateProperties()
		{
			Settlement currentSettlement = ((MobileParty.MainParty.CurrentSettlement != null) ? MobileParty.MainParty.CurrentSettlement : MobileParty.MainParty.LastVisitedSettlement);
			this.IsFortification = currentSettlement.IsFortification;
			IFaction mapFaction = currentSettlement.MapFaction;
			this.IsCrimeEnabled = mapFaction != null && mapFaction.MainHeroCrimeRating > 0f;
			IFaction mapFaction2 = currentSettlement.MapFaction;
			this.CrimeLbl = ((int)((mapFaction2 != null) ? new float?(mapFaction2.MainHeroCrimeRating) : null).Value).ToString();
			IFaction mapFaction3 = currentSettlement.MapFaction;
			this.CrimeChangeAmount = (int)((mapFaction3 != null) ? new float?(mapFaction3.DailyCrimeRatingChange) : null).Value;
			this.RemainingFoodText = (currentSettlement.IsFortification ? ((int)currentSettlement.Town.FoodStocks).ToString() : "-");
			this.FoodChangeAmount = ((currentSettlement.Town != null) ? ((int)currentSettlement.Town.FoodChange) : 0);
			this.MilitasLbl = ((int)currentSettlement.Militia).ToString();
			Town town = currentSettlement.Town;
			int num;
			if (town == null)
			{
				Village village = currentSettlement.Village;
				num = (int)((village != null) ? village.MilitiaChange : 0f);
			}
			else
			{
				num = (int)town.MilitiaChange;
			}
			this.MilitiaChangeAmount = num;
			this.IsLoyaltyRebellionWarning = currentSettlement.IsTown && currentSettlement.Town.Loyalty < (float)Campaign.Current.Models.SettlementLoyaltyModel.RebelliousStateStartLoyaltyThreshold;
			if (currentSettlement.IsFortification)
			{
				this.ProsperityLbl = ((int)currentSettlement.Town.Prosperity).ToString();
				this.ProsperityChangeAmount = (int)currentSettlement.Town.ProsperityChange;
				MobileParty garrisonParty = currentSettlement.Town.GarrisonParty;
				this.GarrisonLbl = ((garrisonParty != null) ? garrisonParty.Party.NumberOfAllMembers.ToString() : null) ?? "0";
				this.GarrisonChangeAmount = (int)SettlementHelper.GetGarrisonChangeExplainedNumber(currentSettlement.Town).ResultNumber;
				MobileParty garrisonParty2 = currentSettlement.Town.GarrisonParty;
				this.GarrisonAmount = ((garrisonParty2 != null) ? garrisonParty2.Party.NumberOfAllMembers : 0);
				this.IsNoGarrisonWarning = this.GarrisonAmount < 1;
				this.WallsLbl = currentSettlement.Town.GetWallLevel().ToString();
				this.WallsLevel = currentSettlement.Town.GetWallLevel();
				this.LoyaltyLbl = ((int)currentSettlement.Town.Loyalty).ToString();
				this.LoyaltyChangeAmount = (int)currentSettlement.Town.LoyaltyChange;
				this.SecurityLbl = ((int)currentSettlement.Town.Security).ToString();
				this.SecurityChangeAmount = (int)currentSettlement.Town.SecurityChange;
			}
			else
			{
				this.GarrisonLbl = "-";
				this.GarrisonChangeAmount = 0;
				this.WallsLbl = "-";
				this.WallsLevel = 1;
				this.LoyaltyLbl = "-";
				this.LoyaltyChangeAmount = 0;
				this.SecurityLbl = "-";
				this.SecurityChangeAmount = 0;
				if (currentSettlement.IsVillage)
				{
					this.ProsperityLbl = ((int)currentSettlement.Village.Hearth).ToString();
					this.ProsperityChangeAmount = (int)currentSettlement.Village.HearthChange;
				}
			}
			this.SettlementNameLbl = currentSettlement.Name + ((currentSettlement.IsVillage && currentSettlement.Village.VillageState != Village.VillageStates.Normal) ? ("(" + currentSettlement.Village.VillageState.ToString() + ")") : "");
			Game.Current.EventManager.TriggerEvent<SettlementOverlayLeaveCharacterPermissionEvent>(new SettlementOverlayLeaveCharacterPermissionEvent(new Action<bool, TextObject>(this.OnSettlementOverlayLeaveCharacterPermissionResult)));
			if (currentSettlement.IsVillage)
			{
				this.CanLeaveMembers = false;
				this.LeaveMembersHint = new HintViewModel(new TextObject("{=y2M014jI}Cannot leave members in a village.", null), null);
				return;
			}
			if (this._mostRecentOverlayLeaveCharacterPermission != null)
			{
				this.CanLeaveMembers = this._mostRecentOverlayLeaveCharacterPermission.Item1;
				this.LeaveMembersHint = (this.CanLeaveMembers ? new HintViewModel(new TextObject("{=aGFxIvqx}Leave Member(s)", null), null) : new HintViewModel(this._mostRecentOverlayLeaveCharacterPermission.Item2, null));
				return;
			}
			this.CanLeaveMembers = Clan.PlayerClan.Heroes.Any((Hero hero) => currentSettlement == hero.StayingInSettlement || (!hero.CharacterObject.IsPlayerCharacter && MobileParty.MainParty.MemberRoster.Contains(hero.CharacterObject)));
			if (!this.CanLeaveMembers)
			{
				this.LeaveMembersHint = new HintViewModel(new TextObject("{=d2K6gMsZ}Leave members. Need at least 1 companion.", null), null);
				return;
			}
			this.LeaveMembersHint = new HintViewModel(new TextObject("{=aGFxIvqx}Leave Member(s)", null), null);
		}

		// Token: 0x06001288 RID: 4744 RVA: 0x0004BAA4 File Offset: 0x00049CA4
		private void OnTutorialNotificationElementIDChange(TutorialNotificationElementChangeEvent obj)
		{
			this._latestTutorialElementID = obj.NewNotificationElementID;
			if (this._latestTutorialElementID != null)
			{
				if (this._latestTutorialElementID != "")
				{
					if (this._latestTutorialElementID == "ApplicapleCompanion" && !this._isCompanionHighlightApplied)
					{
						this._isCompanionHighlightApplied = this.SetPartyItemHighlightState(this._latestTutorialElementID, true);
					}
					else if (this._latestTutorialElementID != "ApplicapleCompanion" && this._isCompanionHighlightApplied)
					{
						this._isCompanionHighlightApplied = this.SetPartyItemHighlightState("ApplicapleCompanion", false);
					}
					if (this._latestTutorialElementID == "ApplicableQuestGivers" && !this._isQuestGiversHighlightApplied)
					{
						this._isQuestGiversHighlightApplied = this.SetPartyItemHighlightState(this._latestTutorialElementID, true);
					}
					else if (this._latestTutorialElementID != "ApplicableQuestGivers" && this._isQuestGiversHighlightApplied)
					{
						this._isCompanionHighlightApplied = this.SetPartyItemHighlightState("ApplicableQuestGivers", false);
					}
					if (this._latestTutorialElementID == "ApplicableNotable" && !this._isNotableHighlightApplied)
					{
						this._isNotableHighlightApplied = this.SetPartyItemHighlightState(this._latestTutorialElementID, true);
					}
					else if (this._latestTutorialElementID != "ApplicableNotable" && this._isNotableHighlightApplied)
					{
						this._isNotableHighlightApplied = this.SetPartyItemHighlightState("ApplicableNotable", false);
					}
					if (this._latestTutorialElementID == "CrimeLabel" && !this.IsCrimeLabelHighlightEnabled)
					{
						this.IsCrimeLabelHighlightEnabled = true;
					}
					else if (this._latestTutorialElementID != "CrimeLabel" && this.IsCrimeLabelHighlightEnabled)
					{
						this.IsCrimeLabelHighlightEnabled = false;
					}
					if (this._latestTutorialElementID == "OverlayTalkButton" && !this._isTalkItemHighlightApplied)
					{
						if (this._overlayTalkItem != null)
						{
							this._overlayTalkItem.IsHiglightEnabled = true;
							this._isTalkItemHighlightApplied = true;
							return;
						}
					}
					else if (this._latestTutorialElementID != "OverlayTalkButton" && this._isTalkItemHighlightApplied && this._overlayTalkItem != null)
					{
						this._overlayTalkItem.IsHiglightEnabled = false;
						this._isTalkItemHighlightApplied = true;
						return;
					}
				}
				else
				{
					if (this._isCompanionHighlightApplied)
					{
						this._isCompanionHighlightApplied = !this.SetPartyItemHighlightState("ApplicapleCompanion", false);
					}
					if (this._isNotableHighlightApplied)
					{
						this._isNotableHighlightApplied = !this.SetPartyItemHighlightState("ApplicableNotable", false);
					}
					if (this._isQuestGiversHighlightApplied)
					{
						this._isQuestGiversHighlightApplied = !this.SetPartyItemHighlightState("ApplicableQuestGivers", false);
					}
					if (this.IsCrimeLabelHighlightEnabled)
					{
						this.IsCrimeLabelHighlightEnabled = false;
					}
					if (this._isTalkItemHighlightApplied && this._overlayTalkItem != null)
					{
						this._overlayTalkItem.IsHiglightEnabled = false;
						this._isTalkItemHighlightApplied = false;
						return;
					}
				}
			}
			else
			{
				if (this._isCompanionHighlightApplied)
				{
					this._isCompanionHighlightApplied = !this.SetPartyItemHighlightState("ApplicapleCompanion", false);
				}
				if (this._isNotableHighlightApplied)
				{
					this._isNotableHighlightApplied = !this.SetPartyItemHighlightState("ApplicableNotable", false);
				}
				if (this._isQuestGiversHighlightApplied)
				{
					this._isQuestGiversHighlightApplied = !this.SetPartyItemHighlightState("ApplicableQuestGivers", false);
				}
				if (this._isTalkItemHighlightApplied && this._overlayTalkItem != null)
				{
					this._overlayTalkItem.IsHiglightEnabled = false;
					this._isTalkItemHighlightApplied = false;
				}
				if (this.IsCrimeLabelHighlightEnabled)
				{
					this.IsCrimeLabelHighlightEnabled = false;
				}
			}
		}

		// Token: 0x06001289 RID: 4745 RVA: 0x0004BDC4 File Offset: 0x00049FC4
		private bool SetPartyItemHighlightState(string condition, bool state)
		{
			bool result = false;
			foreach (GameMenuPartyItemVM gameMenuPartyItemVM in this.CharacterList)
			{
				if (condition == "ApplicapleCompanion" && gameMenuPartyItemVM.Character.IsHero && gameMenuPartyItemVM.Character.HeroObject.IsWanderer && !gameMenuPartyItemVM.Character.HeroObject.IsPlayerCompanion)
				{
					gameMenuPartyItemVM.IsHighlightEnabled = state;
					result = true;
				}
				else if (condition == "ApplicableNotable" && gameMenuPartyItemVM.Character.IsHero && gameMenuPartyItemVM.Character.HeroObject.IsNotable && !gameMenuPartyItemVM.Character.HeroObject.IsPlayerCompanion)
				{
					gameMenuPartyItemVM.IsHighlightEnabled = state;
					result = true;
				}
			}
			return result;
		}

		// Token: 0x0600128A RID: 4746 RVA: 0x0004BEA4 File Offset: 0x0004A0A4
		public override void Refresh()
		{
			base.IsInitializationOver = false;
			this.InitLists();
			this.UpdateProperties();
			foreach (GameMenuPartyItemVM gameMenuPartyItemVM in this.CharacterList)
			{
				gameMenuPartyItemVM.RefreshProperties();
			}
			foreach (GameMenuPartyItemVM gameMenuPartyItemVM2 in this.PartyList)
			{
				gameMenuPartyItemVM2.RefreshProperties();
			}
			base.IsInitializationOver = true;
			base.Refresh();
		}

		// Token: 0x0600128B RID: 4747 RVA: 0x0004BF48 File Offset: 0x0004A148
		public void ExecuteAddCompanion()
		{
			List<InquiryElement> list = new List<InquiryElement>();
			foreach (TroopRosterElement troopRosterElement in from m in MobileParty.MainParty.MemberRoster.GetTroopRoster()
				where m.Character.IsHero && m.Character.HeroObject.CanMoveToSettlement()
				select m)
			{
				if (!troopRosterElement.Character.IsPlayerCharacter)
				{
					list.Add(new InquiryElement(troopRosterElement.Character.HeroObject, troopRosterElement.Character.Name.ToString(), new CharacterImageIdentifier(CampaignUIHelper.GetCharacterCode(troopRosterElement.Character, false))));
				}
			}
			MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(new TextObject("{=aGFxIvqx}Leave Member(s)", null).ToString(), string.Empty, list, true, 1, 0, new TextObject("{=FBYFcrWo}Leave in settlement", null).ToString(), new TextObject("{=3CpNUnVl}Cancel", null).ToString(), new Action<List<InquiryElement>>(this.OnLeaveMembersInSettlement), new Action<List<InquiryElement>>(this.OnLeaveMembersInSettlement), "", false), false, false);
		}

		// Token: 0x0600128C RID: 4748 RVA: 0x0004C068 File Offset: 0x0004A268
		private void OnLeaveMembersInSettlement(List<InquiryElement> leftMembers)
		{
			Settlement settlement = ((MobileParty.MainParty.CurrentSettlement != null) ? MobileParty.MainParty.CurrentSettlement : MobileParty.MainParty.LastVisitedSettlement);
			foreach (InquiryElement inquiryElement in leftMembers)
			{
				Hero hero = inquiryElement.Identifier as Hero;
				PartyBase.MainParty.MemberRoster.RemoveTroop(hero.CharacterObject, 1, default(UniqueTroopDescriptor), 0);
				if (hero.CharacterObject.IsHero && !settlement.HeroesWithoutParty.Contains(hero.CharacterObject.HeroObject))
				{
					EnterSettlementAction.ApplyForCharacterOnly(hero.CharacterObject.HeroObject, settlement);
				}
			}
			if (leftMembers.Count > 0)
			{
				this.InitLists();
			}
		}

		// Token: 0x0600128D RID: 4749 RVA: 0x0004C144 File Offset: 0x0004A344
		public override void OnFinalize()
		{
			base.OnFinalize();
			CampaignEventDispatcher.Instance.RemoveListeners(this);
			Game.Current.EventManager.UnregisterEvent<TutorialNotificationElementChangeEvent>(new Action<TutorialNotificationElementChangeEvent>(this.OnTutorialNotificationElementIDChange));
		}

		// Token: 0x0600128E RID: 4750 RVA: 0x0004C174 File Offset: 0x0004A374
		private void OnSettlementEntered(MobileParty arg1, Settlement arg2, Hero arg3)
		{
			Settlement settlement = ((MobileParty.MainParty.CurrentSettlement != null) ? MobileParty.MainParty.CurrentSettlement : MobileParty.MainParty.LastVisitedSettlement);
			if (arg2 == settlement)
			{
				this.InitLists();
			}
		}

		// Token: 0x0600128F RID: 4751 RVA: 0x0004C1B0 File Offset: 0x0004A3B0
		private void OnSettlementLeft(MobileParty arg1, Settlement arg2)
		{
			Settlement settlement = ((MobileParty.MainParty.CurrentSettlement != null) ? MobileParty.MainParty.CurrentSettlement : MobileParty.MainParty.LastVisitedSettlement);
			if (arg2 == settlement)
			{
				this.InitLists();
			}
		}

		// Token: 0x06001290 RID: 4752 RVA: 0x0004C1EC File Offset: 0x0004A3EC
		private void OnQuestCompleted(QuestBase arg1, QuestBase.QuestCompleteDetails arg2)
		{
			Settlement settlement = ((MobileParty.MainParty.CurrentSettlement != null) ? MobileParty.MainParty.CurrentSettlement : MobileParty.MainParty.LastVisitedSettlement);
			Hero questGiver = arg1.QuestGiver;
			if (((questGiver != null) ? questGiver.CurrentSettlement : null) != null && arg1.QuestGiver.CurrentSettlement == settlement)
			{
				this.Refresh();
			}
		}

		// Token: 0x06001291 RID: 4753 RVA: 0x0004C244 File Offset: 0x0004A444
		private void OnPeaceDeclared(IFaction faction1, IFaction faction2, MakePeaceAction.MakePeaceDetail detail)
		{
			this.OnPeaceOrWarDeclared(faction1, faction2);
		}

		// Token: 0x06001292 RID: 4754 RVA: 0x0004C24E File Offset: 0x0004A44E
		private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail reason)
		{
			this.OnPeaceOrWarDeclared(faction1, faction2);
		}

		// Token: 0x06001293 RID: 4755 RVA: 0x0004C258 File Offset: 0x0004A458
		private void OnPeaceOrWarDeclared(IFaction arg1, IFaction arg2)
		{
			Hero mainHero = Hero.MainHero;
			bool flag;
			if (mainHero == null)
			{
				flag = null != null;
			}
			else
			{
				Settlement currentSettlement = mainHero.CurrentSettlement;
				flag = ((currentSettlement != null) ? currentSettlement.MapFaction : null) != null;
			}
			bool flag2;
			if (flag)
			{
				Hero mainHero2 = Hero.MainHero;
				if (((mainHero2 != null) ? mainHero2.CurrentSettlement.MapFaction : null) != arg1)
				{
					Hero mainHero3 = Hero.MainHero;
					flag2 = ((mainHero3 != null) ? mainHero3.CurrentSettlement.MapFaction : null) == arg2;
				}
				else
				{
					flag2 = true;
				}
			}
			else
			{
				flag2 = false;
			}
			Hero mainHero4 = Hero.MainHero;
			bool flag3;
			if (((mainHero4 != null) ? mainHero4.MapFaction : null) != arg1)
			{
				Hero mainHero5 = Hero.MainHero;
				flag3 = ((mainHero5 != null) ? mainHero5.MapFaction : null) == arg2;
			}
			else
			{
				flag3 = true;
			}
			bool flag4 = flag3;
			if (flag2 || flag4)
			{
				this.InitLists();
			}
		}

		// Token: 0x06001294 RID: 4756 RVA: 0x0004C2F2 File Offset: 0x0004A4F2
		private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero previousOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
		{
			if (settlement == this._settlement || (this._settlement.IsVillage && settlement.BoundVillages.Contains(this._settlement.Village)))
			{
				this.UpdateSettlementOwnerBanner();
			}
		}

		// Token: 0x06001295 RID: 4757 RVA: 0x0004C328 File Offset: 0x0004A528
		private void OnTownRebelliousStateChanged(Town town, bool isRebellious)
		{
			if (this._settlement.IsTown && this._settlement.Town == town)
			{
				this.IsLoyaltyRebellionWarning = isRebellious || town.Loyalty < (float)Campaign.Current.Models.SettlementLoyaltyModel.RebelliousStateStartLoyaltyThreshold;
			}
		}

		// Token: 0x1700060C RID: 1548
		// (get) Token: 0x06001296 RID: 4758 RVA: 0x0004C379 File Offset: 0x0004A579
		// (set) Token: 0x06001297 RID: 4759 RVA: 0x0004C381 File Offset: 0x0004A581
		[DataSourceProperty]
		public string RemainingFoodText
		{
			get
			{
				return this._remainingFoodText;
			}
			set
			{
				if (value != this._remainingFoodText)
				{
					this._remainingFoodText = value;
					base.OnPropertyChangedWithValue<string>(value, "RemainingFoodText");
				}
			}
		}

		// Token: 0x1700060D RID: 1549
		// (get) Token: 0x06001298 RID: 4760 RVA: 0x0004C3A4 File Offset: 0x0004A5A4
		// (set) Token: 0x06001299 RID: 4761 RVA: 0x0004C3AC File Offset: 0x0004A5AC
		[DataSourceProperty]
		public int ProsperityChangeAmount
		{
			get
			{
				return this._prosperityChangeAmount;
			}
			set
			{
				if (value != this._prosperityChangeAmount)
				{
					this._prosperityChangeAmount = value;
					base.OnPropertyChangedWithValue(value, "ProsperityChangeAmount");
				}
			}
		}

		// Token: 0x1700060E RID: 1550
		// (get) Token: 0x0600129A RID: 4762 RVA: 0x0004C3CA File Offset: 0x0004A5CA
		// (set) Token: 0x0600129B RID: 4763 RVA: 0x0004C3D2 File Offset: 0x0004A5D2
		[DataSourceProperty]
		public int MilitiaChangeAmount
		{
			get
			{
				return this._militiaChangeAmount;
			}
			set
			{
				if (value != this._militiaChangeAmount)
				{
					this._militiaChangeAmount = value;
					base.OnPropertyChangedWithValue(value, "MilitiaChangeAmount");
				}
			}
		}

		// Token: 0x1700060F RID: 1551
		// (get) Token: 0x0600129C RID: 4764 RVA: 0x0004C3F0 File Offset: 0x0004A5F0
		// (set) Token: 0x0600129D RID: 4765 RVA: 0x0004C3F8 File Offset: 0x0004A5F8
		[DataSourceProperty]
		public int GarrisonChangeAmount
		{
			get
			{
				return this._garrisonChangeAmount;
			}
			set
			{
				if (value != this._garrisonChangeAmount)
				{
					this._garrisonChangeAmount = value;
					base.OnPropertyChangedWithValue(value, "GarrisonChangeAmount");
				}
			}
		}

		// Token: 0x17000610 RID: 1552
		// (get) Token: 0x0600129E RID: 4766 RVA: 0x0004C416 File Offset: 0x0004A616
		// (set) Token: 0x0600129F RID: 4767 RVA: 0x0004C41E File Offset: 0x0004A61E
		[DataSourceProperty]
		public int GarrisonAmount
		{
			get
			{
				return this._garrisonAmount;
			}
			set
			{
				if (value != this._garrisonAmount)
				{
					this._garrisonAmount = value;
					base.OnPropertyChangedWithValue(value, "GarrisonAmount");
				}
			}
		}

		// Token: 0x17000611 RID: 1553
		// (get) Token: 0x060012A0 RID: 4768 RVA: 0x0004C43C File Offset: 0x0004A63C
		// (set) Token: 0x060012A1 RID: 4769 RVA: 0x0004C444 File Offset: 0x0004A644
		[DataSourceProperty]
		public int CrimeChangeAmount
		{
			get
			{
				return this._crimeChangeAmount;
			}
			set
			{
				if (value != this._crimeChangeAmount)
				{
					this._crimeChangeAmount = value;
					base.OnPropertyChangedWithValue(value, "CrimeChangeAmount");
				}
			}
		}

		// Token: 0x17000612 RID: 1554
		// (get) Token: 0x060012A2 RID: 4770 RVA: 0x0004C462 File Offset: 0x0004A662
		// (set) Token: 0x060012A3 RID: 4771 RVA: 0x0004C46A File Offset: 0x0004A66A
		[DataSourceProperty]
		public int LoyaltyChangeAmount
		{
			get
			{
				return this._loyaltyChangeAmount;
			}
			set
			{
				if (value != this._loyaltyChangeAmount)
				{
					this._loyaltyChangeAmount = value;
					base.OnPropertyChangedWithValue(value, "LoyaltyChangeAmount");
				}
			}
		}

		// Token: 0x17000613 RID: 1555
		// (get) Token: 0x060012A4 RID: 4772 RVA: 0x0004C488 File Offset: 0x0004A688
		// (set) Token: 0x060012A5 RID: 4773 RVA: 0x0004C490 File Offset: 0x0004A690
		[DataSourceProperty]
		public int SecurityChangeAmount
		{
			get
			{
				return this._securityChangeAmount;
			}
			set
			{
				if (value != this._securityChangeAmount)
				{
					this._securityChangeAmount = value;
					base.OnPropertyChangedWithValue(value, "SecurityChangeAmount");
				}
			}
		}

		// Token: 0x17000614 RID: 1556
		// (get) Token: 0x060012A6 RID: 4774 RVA: 0x0004C4AE File Offset: 0x0004A6AE
		// (set) Token: 0x060012A7 RID: 4775 RVA: 0x0004C4B6 File Offset: 0x0004A6B6
		[DataSourceProperty]
		public int FoodChangeAmount
		{
			get
			{
				return this._foodChangeAmount;
			}
			set
			{
				if (value != this._foodChangeAmount)
				{
					this._foodChangeAmount = value;
					base.OnPropertyChangedWithValue(value, "FoodChangeAmount");
				}
			}
		}

		// Token: 0x17000615 RID: 1557
		// (get) Token: 0x060012A8 RID: 4776 RVA: 0x0004C4D4 File Offset: 0x0004A6D4
		// (set) Token: 0x060012A9 RID: 4777 RVA: 0x0004C4DC File Offset: 0x0004A6DC
		[DataSourceProperty]
		public BasicTooltipViewModel RemainingFoodHint
		{
			get
			{
				return this._remainingFoodHint;
			}
			set
			{
				if (value != this._remainingFoodHint)
				{
					this._remainingFoodHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "RemainingFoodHint");
				}
			}
		}

		// Token: 0x17000616 RID: 1558
		// (get) Token: 0x060012AA RID: 4778 RVA: 0x0004C4FA File Offset: 0x0004A6FA
		// (set) Token: 0x060012AB RID: 4779 RVA: 0x0004C502 File Offset: 0x0004A702
		[DataSourceProperty]
		public BasicTooltipViewModel SecurityHint
		{
			get
			{
				return this._securityHint;
			}
			set
			{
				if (value != this._securityHint)
				{
					this._securityHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "SecurityHint");
				}
			}
		}

		// Token: 0x17000617 RID: 1559
		// (get) Token: 0x060012AC RID: 4780 RVA: 0x0004C520 File Offset: 0x0004A720
		// (set) Token: 0x060012AD RID: 4781 RVA: 0x0004C528 File Offset: 0x0004A728
		[DataSourceProperty]
		public HintViewModel PartyFilterHint
		{
			get
			{
				return this._partyFilterHint;
			}
			set
			{
				if (value != this._partyFilterHint)
				{
					this._partyFilterHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "PartyFilterHint");
				}
			}
		}

		// Token: 0x17000618 RID: 1560
		// (get) Token: 0x060012AE RID: 4782 RVA: 0x0004C546 File Offset: 0x0004A746
		// (set) Token: 0x060012AF RID: 4783 RVA: 0x0004C54E File Offset: 0x0004A74E
		[DataSourceProperty]
		public HintViewModel CharacterFilterHint
		{
			get
			{
				return this._characterFilterHint;
			}
			set
			{
				if (value != this._characterFilterHint)
				{
					this._characterFilterHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "CharacterFilterHint");
				}
			}
		}

		// Token: 0x17000619 RID: 1561
		// (get) Token: 0x060012B0 RID: 4784 RVA: 0x0004C56C File Offset: 0x0004A76C
		// (set) Token: 0x060012B1 RID: 4785 RVA: 0x0004C574 File Offset: 0x0004A774
		[DataSourceProperty]
		public BasicTooltipViewModel MilitasHint
		{
			get
			{
				return this._militasHint;
			}
			set
			{
				if (value != this._militasHint)
				{
					this._militasHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "MilitasHint");
				}
			}
		}

		// Token: 0x1700061A RID: 1562
		// (get) Token: 0x060012B2 RID: 4786 RVA: 0x0004C592 File Offset: 0x0004A792
		// (set) Token: 0x060012B3 RID: 4787 RVA: 0x0004C59A File Offset: 0x0004A79A
		[DataSourceProperty]
		public BasicTooltipViewModel GarrisonHint
		{
			get
			{
				return this._garrisonHint;
			}
			set
			{
				if (value != this._garrisonHint)
				{
					this._garrisonHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "GarrisonHint");
				}
			}
		}

		// Token: 0x1700061B RID: 1563
		// (get) Token: 0x060012B4 RID: 4788 RVA: 0x0004C5B8 File Offset: 0x0004A7B8
		// (set) Token: 0x060012B5 RID: 4789 RVA: 0x0004C5C0 File Offset: 0x0004A7C0
		[DataSourceProperty]
		public BasicTooltipViewModel ProsperityHint
		{
			get
			{
				return this._prosperityHint;
			}
			set
			{
				if (value != this._prosperityHint)
				{
					this._prosperityHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "ProsperityHint");
				}
			}
		}

		// Token: 0x1700061C RID: 1564
		// (get) Token: 0x060012B6 RID: 4790 RVA: 0x0004C5DE File Offset: 0x0004A7DE
		// (set) Token: 0x060012B7 RID: 4791 RVA: 0x0004C5E6 File Offset: 0x0004A7E6
		[DataSourceProperty]
		public BasicTooltipViewModel LoyaltyHint
		{
			get
			{
				return this._loyaltyHint;
			}
			set
			{
				if (value != this._loyaltyHint)
				{
					this._loyaltyHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "LoyaltyHint");
				}
			}
		}

		// Token: 0x1700061D RID: 1565
		// (get) Token: 0x060012B8 RID: 4792 RVA: 0x0004C604 File Offset: 0x0004A804
		// (set) Token: 0x060012B9 RID: 4793 RVA: 0x0004C60C File Offset: 0x0004A80C
		[DataSourceProperty]
		public BasicTooltipViewModel WallsHint
		{
			get
			{
				return this._wallsHint;
			}
			set
			{
				if (value != this._wallsHint)
				{
					this._wallsHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "WallsHint");
				}
			}
		}

		// Token: 0x1700061E RID: 1566
		// (get) Token: 0x060012BA RID: 4794 RVA: 0x0004C62A File Offset: 0x0004A82A
		// (set) Token: 0x060012BB RID: 4795 RVA: 0x0004C632 File Offset: 0x0004A832
		[DataSourceProperty]
		public BasicTooltipViewModel CrimeHint
		{
			get
			{
				return this._crimeHint;
			}
			set
			{
				if (value != this._crimeHint)
				{
					this._crimeHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "CrimeHint");
				}
			}
		}

		// Token: 0x1700061F RID: 1567
		// (get) Token: 0x060012BC RID: 4796 RVA: 0x0004C650 File Offset: 0x0004A850
		// (set) Token: 0x060012BD RID: 4797 RVA: 0x0004C658 File Offset: 0x0004A858
		[DataSourceProperty]
		public HintViewModel LeaveMembersHint
		{
			get
			{
				return this._leaveMembersHint;
			}
			set
			{
				if (value != this._leaveMembersHint)
				{
					this._leaveMembersHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "LeaveMembersHint");
				}
			}
		}

		// Token: 0x17000620 RID: 1568
		// (get) Token: 0x060012BE RID: 4798 RVA: 0x0004C676 File Offset: 0x0004A876
		// (set) Token: 0x060012BF RID: 4799 RVA: 0x0004C67E File Offset: 0x0004A87E
		[DataSourceProperty]
		public BannerImageIdentifierVM SettlementOwnerBanner
		{
			get
			{
				return this._settlementOwnerBanner;
			}
			set
			{
				if (value != this._settlementOwnerBanner)
				{
					this._settlementOwnerBanner = value;
					base.OnPropertyChangedWithValue<BannerImageIdentifierVM>(value, "SettlementOwnerBanner");
				}
			}
		}

		// Token: 0x17000621 RID: 1569
		// (get) Token: 0x060012C0 RID: 4800 RVA: 0x0004C69C File Offset: 0x0004A89C
		// (set) Token: 0x060012C1 RID: 4801 RVA: 0x0004C6A4 File Offset: 0x0004A8A4
		[DataSourceProperty]
		public MBBindingList<GameMenuPartyItemVM> CharacterList
		{
			get
			{
				return this._characterList;
			}
			set
			{
				if (value != this._characterList)
				{
					this._characterList = value;
					base.OnPropertyChangedWithValue<MBBindingList<GameMenuPartyItemVM>>(value, "CharacterList");
				}
			}
		}

		// Token: 0x17000622 RID: 1570
		// (get) Token: 0x060012C2 RID: 4802 RVA: 0x0004C6C2 File Offset: 0x0004A8C2
		// (set) Token: 0x060012C3 RID: 4803 RVA: 0x0004C6CA File Offset: 0x0004A8CA
		[DataSourceProperty]
		public MBBindingList<GameMenuPartyItemVM> PartyList
		{
			get
			{
				return this._partyList;
			}
			set
			{
				if (value != this._partyList)
				{
					this._partyList = value;
					base.OnPropertyChangedWithValue<MBBindingList<GameMenuPartyItemVM>>(value, "PartyList");
				}
			}
		}

		// Token: 0x17000623 RID: 1571
		// (get) Token: 0x060012C4 RID: 4804 RVA: 0x0004C6E8 File Offset: 0x0004A8E8
		// (set) Token: 0x060012C5 RID: 4805 RVA: 0x0004C6F0 File Offset: 0x0004A8F0
		[DataSourceProperty]
		public MBBindingList<StringItemWithHintVM> IssueList
		{
			get
			{
				return this._issueList;
			}
			set
			{
				if (value != this._issueList)
				{
					this._issueList = value;
					base.OnPropertyChangedWithValue<MBBindingList<StringItemWithHintVM>>(value, "IssueList");
				}
			}
		}

		// Token: 0x17000624 RID: 1572
		// (get) Token: 0x060012C6 RID: 4806 RVA: 0x0004C70E File Offset: 0x0004A90E
		// (set) Token: 0x060012C7 RID: 4807 RVA: 0x0004C716 File Offset: 0x0004A916
		[DataSourceProperty]
		public string MilitasLbl
		{
			get
			{
				return this._militasLbl;
			}
			set
			{
				if (value != this._militasLbl)
				{
					this._militasLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "MilitasLbl");
				}
			}
		}

		// Token: 0x17000625 RID: 1573
		// (get) Token: 0x060012C8 RID: 4808 RVA: 0x0004C739 File Offset: 0x0004A939
		// (set) Token: 0x060012C9 RID: 4809 RVA: 0x0004C741 File Offset: 0x0004A941
		[DataSourceProperty]
		public string GarrisonLbl
		{
			get
			{
				return this._garrisonLbl;
			}
			set
			{
				if (value != this._garrisonLbl)
				{
					this._garrisonLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "GarrisonLbl");
				}
			}
		}

		// Token: 0x17000626 RID: 1574
		// (get) Token: 0x060012CA RID: 4810 RVA: 0x0004C764 File Offset: 0x0004A964
		// (set) Token: 0x060012CB RID: 4811 RVA: 0x0004C76C File Offset: 0x0004A96C
		[DataSourceProperty]
		public string CrimeLbl
		{
			get
			{
				return this._crimeLbl;
			}
			set
			{
				if (value != this._crimeLbl)
				{
					this._crimeLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "CrimeLbl");
				}
			}
		}

		// Token: 0x17000627 RID: 1575
		// (get) Token: 0x060012CC RID: 4812 RVA: 0x0004C78F File Offset: 0x0004A98F
		// (set) Token: 0x060012CD RID: 4813 RVA: 0x0004C797 File Offset: 0x0004A997
		[DataSourceProperty]
		public bool CanLeaveMembers
		{
			get
			{
				return this._canLeaveMembers;
			}
			set
			{
				if (value != this._canLeaveMembers)
				{
					this._canLeaveMembers = value;
					base.OnPropertyChangedWithValue(value, "CanLeaveMembers");
				}
			}
		}

		// Token: 0x17000628 RID: 1576
		// (get) Token: 0x060012CE RID: 4814 RVA: 0x0004C7B5 File Offset: 0x0004A9B5
		// (set) Token: 0x060012CF RID: 4815 RVA: 0x0004C7BD File Offset: 0x0004A9BD
		[DataSourceProperty]
		public string ProsperityLbl
		{
			get
			{
				return this._prosperityLbl;
			}
			set
			{
				if (value != this._prosperityLbl)
				{
					this._prosperityLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "ProsperityLbl");
				}
			}
		}

		// Token: 0x17000629 RID: 1577
		// (get) Token: 0x060012D0 RID: 4816 RVA: 0x0004C7E0 File Offset: 0x0004A9E0
		// (set) Token: 0x060012D1 RID: 4817 RVA: 0x0004C7E8 File Offset: 0x0004A9E8
		[DataSourceProperty]
		public string LoyaltyLbl
		{
			get
			{
				return this._loyaltyLbl;
			}
			set
			{
				if (value != this._loyaltyLbl)
				{
					this._loyaltyLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "LoyaltyLbl");
				}
			}
		}

		// Token: 0x1700062A RID: 1578
		// (get) Token: 0x060012D2 RID: 4818 RVA: 0x0004C80B File Offset: 0x0004AA0B
		// (set) Token: 0x060012D3 RID: 4819 RVA: 0x0004C813 File Offset: 0x0004AA13
		[DataSourceProperty]
		public string SecurityLbl
		{
			get
			{
				return this._securityLbl;
			}
			set
			{
				if (value != this._securityLbl)
				{
					this._securityLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "SecurityLbl");
				}
			}
		}

		// Token: 0x1700062B RID: 1579
		// (get) Token: 0x060012D4 RID: 4820 RVA: 0x0004C836 File Offset: 0x0004AA36
		// (set) Token: 0x060012D5 RID: 4821 RVA: 0x0004C83E File Offset: 0x0004AA3E
		[DataSourceProperty]
		public string WallsLbl
		{
			get
			{
				return this._wallsLbl;
			}
			set
			{
				if (value != this._wallsLbl)
				{
					this._wallsLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "WallsLbl");
				}
			}
		}

		// Token: 0x1700062C RID: 1580
		// (get) Token: 0x060012D6 RID: 4822 RVA: 0x0004C861 File Offset: 0x0004AA61
		// (set) Token: 0x060012D7 RID: 4823 RVA: 0x0004C869 File Offset: 0x0004AA69
		[DataSourceProperty]
		public int WallsLevel
		{
			get
			{
				return this._wallsLevel;
			}
			set
			{
				if (value != this._wallsLevel)
				{
					this._wallsLevel = value;
					base.OnPropertyChangedWithValue(value, "WallsLevel");
				}
			}
		}

		// Token: 0x1700062D RID: 1581
		// (get) Token: 0x060012D8 RID: 4824 RVA: 0x0004C887 File Offset: 0x0004AA87
		// (set) Token: 0x060012D9 RID: 4825 RVA: 0x0004C88F File Offset: 0x0004AA8F
		[DataSourceProperty]
		public string SettlementNameLbl
		{
			get
			{
				return this._settlementNameLbl;
			}
			set
			{
				if (value != this._settlementNameLbl)
				{
					this._settlementNameLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "SettlementNameLbl");
				}
			}
		}

		// Token: 0x1700062E RID: 1582
		// (get) Token: 0x060012DA RID: 4826 RVA: 0x0004C8B2 File Offset: 0x0004AAB2
		// (set) Token: 0x060012DB RID: 4827 RVA: 0x0004C8BA File Offset: 0x0004AABA
		[DataSourceProperty]
		public bool IsFortification
		{
			get
			{
				return this._isFortification;
			}
			set
			{
				if (value != this._isFortification)
				{
					this._isFortification = value;
					base.OnPropertyChangedWithValue(value, "IsFortification");
				}
			}
		}

		// Token: 0x1700062F RID: 1583
		// (get) Token: 0x060012DC RID: 4828 RVA: 0x0004C8D8 File Offset: 0x0004AAD8
		// (set) Token: 0x060012DD RID: 4829 RVA: 0x0004C8E0 File Offset: 0x0004AAE0
		[DataSourceProperty]
		public bool IsCrimeEnabled
		{
			get
			{
				return this._isCrimeEnabled;
			}
			set
			{
				if (value != this._isCrimeEnabled)
				{
					this._isCrimeEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsCrimeEnabled");
				}
			}
		}

		// Token: 0x17000630 RID: 1584
		// (get) Token: 0x060012DE RID: 4830 RVA: 0x0004C8FE File Offset: 0x0004AAFE
		// (set) Token: 0x060012DF RID: 4831 RVA: 0x0004C906 File Offset: 0x0004AB06
		[DataSourceProperty]
		public bool IsNoGarrisonWarning
		{
			get
			{
				return this._isNoGarrisonWarning;
			}
			set
			{
				if (value != this._isNoGarrisonWarning)
				{
					this._isNoGarrisonWarning = value;
					base.OnPropertyChangedWithValue(value, "IsNoGarrisonWarning");
				}
			}
		}

		// Token: 0x17000631 RID: 1585
		// (get) Token: 0x060012E0 RID: 4832 RVA: 0x0004C924 File Offset: 0x0004AB24
		// (set) Token: 0x060012E1 RID: 4833 RVA: 0x0004C92C File Offset: 0x0004AB2C
		[DataSourceProperty]
		public bool IsCrimeLabelHighlightEnabled
		{
			get
			{
				return this._isCrimeLabelHighlightEnabled;
			}
			set
			{
				if (value != this._isCrimeLabelHighlightEnabled)
				{
					this._isCrimeLabelHighlightEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsCrimeLabelHighlightEnabled");
				}
			}
		}

		// Token: 0x17000632 RID: 1586
		// (get) Token: 0x060012E2 RID: 4834 RVA: 0x0004C94A File Offset: 0x0004AB4A
		// (set) Token: 0x060012E3 RID: 4835 RVA: 0x0004C952 File Offset: 0x0004AB52
		[DataSourceProperty]
		public bool IsLoyaltyRebellionWarning
		{
			get
			{
				return this._isLoyaltyRebellionWarning;
			}
			set
			{
				if (value != this._isLoyaltyRebellionWarning)
				{
					this._isLoyaltyRebellionWarning = value;
					base.OnPropertyChangedWithValue(value, "IsLoyaltyRebellionWarning");
				}
			}
		}

		// Token: 0x17000633 RID: 1587
		// (get) Token: 0x060012E4 RID: 4836 RVA: 0x0004C970 File Offset: 0x0004AB70
		// (set) Token: 0x060012E5 RID: 4837 RVA: 0x0004C978 File Offset: 0x0004AB78
		[DataSourceProperty]
		public bool IsShipyardEnabled
		{
			get
			{
				return this._isShipyardEnabled;
			}
			set
			{
				if (value != this._isShipyardEnabled)
				{
					this._isShipyardEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsShipyardEnabled");
				}
			}
		}

		// Token: 0x17000634 RID: 1588
		// (get) Token: 0x060012E6 RID: 4838 RVA: 0x0004C996 File Offset: 0x0004AB96
		// (set) Token: 0x060012E7 RID: 4839 RVA: 0x0004C99E File Offset: 0x0004AB9E
		[DataSourceProperty]
		public string ShipyardLbl
		{
			get
			{
				return this._shipyardLbl;
			}
			set
			{
				if (value != this._shipyardLbl)
				{
					this._shipyardLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "ShipyardLbl");
				}
			}
		}

		// Token: 0x17000635 RID: 1589
		// (get) Token: 0x060012E8 RID: 4840 RVA: 0x0004C9C1 File Offset: 0x0004ABC1
		// (set) Token: 0x060012E9 RID: 4841 RVA: 0x0004C9C9 File Offset: 0x0004ABC9
		[DataSourceProperty]
		public BasicTooltipViewModel ShipyardHint
		{
			get
			{
				return this._shipyardHint;
			}
			set
			{
				if (value != this._shipyardHint)
				{
					this._shipyardHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "ShipyardHint");
				}
			}
		}

		// Token: 0x0400086D RID: 2157
		protected readonly Settlement _settlement;

		// Token: 0x0400086E RID: 2158
		private GameMenu.MenuOverlayType _type;

		// Token: 0x0400086F RID: 2159
		private GameMenuOverlayActionVM _overlayTalkItem;

		// Token: 0x04000870 RID: 2160
		private GameMenuOverlayActionVM _overlayQuickTalkItem;

		// Token: 0x04000871 RID: 2161
		private Tuple<bool, TextObject> _mostRecentOverlayTalkPermission;

		// Token: 0x04000872 RID: 2162
		private Tuple<bool, TextObject> _mostRecentOverlayQuickTalkPermission;

		// Token: 0x04000873 RID: 2163
		private Tuple<bool, TextObject> _mostRecentOverlayLeaveCharacterPermission;

		// Token: 0x04000874 RID: 2164
		private string _latestTutorialElementID;

		// Token: 0x04000875 RID: 2165
		private bool _isCompanionHighlightApplied;

		// Token: 0x04000876 RID: 2166
		private bool _isQuestGiversHighlightApplied;

		// Token: 0x04000877 RID: 2167
		private bool _isNotableHighlightApplied;

		// Token: 0x04000878 RID: 2168
		private bool _isTalkItemHighlightApplied;

		// Token: 0x04000879 RID: 2169
		private string _militasLbl;

		// Token: 0x0400087A RID: 2170
		private string _garrisonLbl;

		// Token: 0x0400087B RID: 2171
		private bool _isNoGarrisonWarning;

		// Token: 0x0400087C RID: 2172
		private bool _isLoyaltyRebellionWarning;

		// Token: 0x0400087D RID: 2173
		private bool _isCrimeLabelHighlightEnabled;

		// Token: 0x0400087E RID: 2174
		private string _crimeLbl;

		// Token: 0x0400087F RID: 2175
		private string _prosperityLbl;

		// Token: 0x04000880 RID: 2176
		private string _loyaltyLbl;

		// Token: 0x04000881 RID: 2177
		private string _securityLbl;

		// Token: 0x04000882 RID: 2178
		private string _wallsLbl;

		// Token: 0x04000883 RID: 2179
		private string _settlementNameLbl;

		// Token: 0x04000884 RID: 2180
		private string _remainingFoodText = "";

		// Token: 0x04000885 RID: 2181
		private int _wallsLevel;

		// Token: 0x04000886 RID: 2182
		private int _prosperityChangeAmount;

		// Token: 0x04000887 RID: 2183
		private int _militiaChangeAmount;

		// Token: 0x04000888 RID: 2184
		private int _garrisonChangeAmount;

		// Token: 0x04000889 RID: 2185
		private int _garrisonAmount;

		// Token: 0x0400088A RID: 2186
		private int _crimeChangeAmount;

		// Token: 0x0400088B RID: 2187
		private int _loyaltyChangeAmount;

		// Token: 0x0400088C RID: 2188
		private int _securityChangeAmount;

		// Token: 0x0400088D RID: 2189
		private int _foodChangeAmount;

		// Token: 0x0400088E RID: 2190
		private MBBindingList<GameMenuPartyItemVM> _characterList;

		// Token: 0x0400088F RID: 2191
		private MBBindingList<GameMenuPartyItemVM> _partyList;

		// Token: 0x04000890 RID: 2192
		private MBBindingList<StringItemWithHintVM> _issueList;

		// Token: 0x04000891 RID: 2193
		private bool _isFortification;

		// Token: 0x04000892 RID: 2194
		private bool _isCrimeEnabled;

		// Token: 0x04000893 RID: 2195
		private bool _canLeaveMembers;

		// Token: 0x04000894 RID: 2196
		private BasicTooltipViewModel _remainingFoodHint;

		// Token: 0x04000895 RID: 2197
		private BasicTooltipViewModel _militasHint;

		// Token: 0x04000896 RID: 2198
		private BasicTooltipViewModel _garrisonHint;

		// Token: 0x04000897 RID: 2199
		private BasicTooltipViewModel _prosperityHint;

		// Token: 0x04000898 RID: 2200
		private BasicTooltipViewModel _loyaltyHint;

		// Token: 0x04000899 RID: 2201
		private BasicTooltipViewModel _securityHint;

		// Token: 0x0400089A RID: 2202
		private BasicTooltipViewModel _wallsHint;

		// Token: 0x0400089B RID: 2203
		private BasicTooltipViewModel _crimeHint;

		// Token: 0x0400089C RID: 2204
		private HintViewModel _characterFilterHint;

		// Token: 0x0400089D RID: 2205
		private HintViewModel _partyFilterHint;

		// Token: 0x0400089E RID: 2206
		private HintViewModel _leaveMembersHint;

		// Token: 0x0400089F RID: 2207
		private BannerImageIdentifierVM _settlementOwnerBanner;

		// Token: 0x040008A0 RID: 2208
		private bool _isShipyardEnabled;

		// Token: 0x040008A1 RID: 2209
		private string _shipyardLbl;

		// Token: 0x040008A2 RID: 2210
		private BasicTooltipViewModel _shipyardHint;

		// Token: 0x02000231 RID: 561
		private class CharacterComparer : IComparer<GameMenuPartyItemVM>
		{
			// Token: 0x060024A5 RID: 9381 RVA: 0x0007FE46 File Offset: 0x0007E046
			public int Compare(GameMenuPartyItemVM x, GameMenuPartyItemVM y)
			{
				return CampaignUIHelper.GetHeroCompareSortIndex(x.Character.HeroObject, y.Character.HeroObject);
			}
		}

		// Token: 0x02000232 RID: 562
		private class PartyComparer : IComparer<GameMenuPartyItemVM>
		{
			// Token: 0x060024A7 RID: 9383 RVA: 0x0007FE6B File Offset: 0x0007E06B
			public int Compare(GameMenuPartyItemVM x, GameMenuPartyItemVM y)
			{
				return CampaignUIHelper.MobilePartyPrecedenceComparerInstance.Compare(x.Party.MobileParty, y.Party.MobileParty);
			}
		}
	}
}
