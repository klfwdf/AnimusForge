using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.Categories
{
	// Token: 0x0200013F RID: 319
	public class ClanPartiesVM : ViewModel
	{
		// Token: 0x17000A26 RID: 2598
		// (get) Token: 0x06001DD6 RID: 7638 RVA: 0x0006DDEB File Offset: 0x0006BFEB
		// (set) Token: 0x06001DD7 RID: 7639 RVA: 0x0006DDF3 File Offset: 0x0006BFF3
		public int TotalExpense { get; private set; }

		// Token: 0x17000A27 RID: 2599
		// (get) Token: 0x06001DD8 RID: 7640 RVA: 0x0006DDFC File Offset: 0x0006BFFC
		// (set) Token: 0x06001DD9 RID: 7641 RVA: 0x0006DE04 File Offset: 0x0006C004
		public int TotalIncome { get; private set; }

		// Token: 0x06001DDA RID: 7642 RVA: 0x0006DE10 File Offset: 0x0006C010
		public ClanPartiesVM(Action onExpenseChange, Action<Hero> openPartyAsManage, Action onRefresh, Action<ClanCardSelectionInfo> openCardSelectionPopup)
		{
			this._onExpenseChange = onExpenseChange;
			this._onRefresh = onRefresh;
			this._disbandBehavior = Campaign.Current.GetCampaignBehavior<IDisbandPartyCampaignBehavior>();
			this._teleportationBehavior = Campaign.Current.GetCampaignBehavior<ITeleportationCampaignBehavior>();
			this._openPartyAsManage = openPartyAsManage;
			this._openCardSelectionPopup = openCardSelectionPopup;
			this._faction = Hero.MainHero.Clan;
			this.Parties = new MBBindingList<ClanPartyItemVM>();
			this.Garrisons = new MBBindingList<ClanPartyItemVM>();
			this.Caravans = new MBBindingList<ClanPartyItemVM>();
			MBBindingList<MBBindingList<ClanPartyItemVM>> listsToControl = new MBBindingList<MBBindingList<ClanPartyItemVM>> { this.Parties, this.Garrisons, this.Caravans };
			this.SortController = new ClanPartiesSortControllerVM(listsToControl);
			this.CreateNewPartyActionHint = new HintViewModel();
			this.RefreshPartiesList();
			this.RefreshValues();
		}

		// Token: 0x06001DDB RID: 7643 RVA: 0x0006DF18 File Offset: 0x0006C118
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.SizeText = GameTexts.FindText("str_clan_party_size", null).ToString();
			this.MoraleText = GameTexts.FindText("str_morale", null).ToString();
			this.LocationText = GameTexts.FindText("str_tooltip_label_location", null).ToString();
			this.NameText = GameTexts.FindText("str_sort_by_name_label", null).ToString();
			this.CreateNewPartyText = GameTexts.FindText("str_clan_create_new_party", null).ToString();
			this.GarrisonsText = GameTexts.FindText("str_clan_garrisons", null).ToString();
			this.CaravansText = GameTexts.FindText("str_clan_caravans", null).ToString();
			this.RefreshPartiesList();
			this.Parties.ApplyActionOnAllItems(delegate(ClanPartyItemVM x)
			{
				x.RefreshValues();
			});
			this.Garrisons.ApplyActionOnAllItems(delegate(ClanPartyItemVM x)
			{
				x.RefreshValues();
			});
			this.Caravans.ApplyActionOnAllItems(delegate(ClanPartyItemVM x)
			{
				x.RefreshValues();
			});
			this.SortController.RefreshValues();
		}

		// Token: 0x06001DDC RID: 7644 RVA: 0x0006E054 File Offset: 0x0006C254
		public void RefreshTotalExpense()
		{
			IEnumerable<ClanPartyItemVM> enumerable = from p in this.Parties.Union(this.Garrisons).Union(this.Caravans)
				where p.ShouldPartyHaveExpense
				select p;
			int totalExpense;
			if (enumerable == null)
			{
				totalExpense = 0;
			}
			else
			{
				totalExpense = enumerable.Sum((ClanPartyItemVM p) => p.Expense);
			}
			this.TotalExpense = totalExpense;
			this.TotalIncome = this.Caravans.Sum((ClanPartyItemVM p) => p.Income);
		}

		// Token: 0x06001DDD RID: 7645 RVA: 0x0006E104 File Offset: 0x0006C304
		public void RefreshPartiesList()
		{
			this.Parties.Clear();
			this.Garrisons.Clear();
			this.Caravans.Clear();
			this.SortController.ResetAllStates();
			foreach (WarPartyComponent warPartyComponent in this._faction.WarPartyComponents)
			{
				if (warPartyComponent.MobileParty == MobileParty.MainParty)
				{
					this.Parties.Insert(0, new ClanPartyItemVM(warPartyComponent.Party, new Action<ClanPartyItemVM>(this.OnPartySelection), new Action(this.OnAnyExpenseChange), new Action(this.OnShowChangeLeaderPopup), ClanPartyItemVM.ClanPartyType.Main, this._disbandBehavior, this._teleportationBehavior));
				}
				else
				{
					this.Parties.Add(new ClanPartyItemVM(warPartyComponent.Party, new Action<ClanPartyItemVM>(this.OnPartySelection), new Action(this.OnAnyExpenseChange), new Action(this.OnShowChangeLeaderPopup), ClanPartyItemVM.ClanPartyType.Member, this._disbandBehavior, this._teleportationBehavior));
				}
			}
			using (IEnumerator<CaravanPartyComponent> enumerator2 = this._faction.Heroes.SelectMany((Hero h) => h.OwnedCaravans).GetEnumerator())
			{
				while (enumerator2.MoveNext())
				{
					CaravanPartyComponent party = enumerator2.Current;
					if (!this.Caravans.Any((ClanPartyItemVM c) => c.Party.MobileParty == party.MobileParty))
					{
						this.Caravans.Add(new ClanPartyItemVM(party.Party, new Action<ClanPartyItemVM>(this.OnPartySelection), new Action(this.OnAnyExpenseChange), new Action(this.OnShowChangeLeaderPopup), ClanPartyItemVM.ClanPartyType.Caravan, this._disbandBehavior, this._teleportationBehavior));
					}
				}
			}
			using (IEnumerator<MobileParty> enumerator3 = (from a in this._faction.Settlements
				where a.Town != null
				select a into s
				select s.Town.GarrisonParty).GetEnumerator())
			{
				while (enumerator3.MoveNext())
				{
					MobileParty garrison = enumerator3.Current;
					if (garrison != null && !this.Garrisons.Any((ClanPartyItemVM c) => c.Party == garrison.Party))
					{
						this.Garrisons.Add(new ClanPartyItemVM(garrison.Party, new Action<ClanPartyItemVM>(this.OnPartySelection), new Action(this.OnAnyExpenseChange), new Action(this.OnShowChangeLeaderPopup), ClanPartyItemVM.ClanPartyType.Garrison, this._disbandBehavior, this._teleportationBehavior));
					}
				}
			}
			int count = this._faction.WarPartyComponents.Count;
			(from h in this._faction.Heroes
				where !h.IsDisabled
				select h).Union(this._faction.Companions).Any((Hero h) => h.IsActive && h.PartyBelongedToAsPrisoner == null && !h.IsChild && h.CanLeadParty() && (h.PartyBelongedTo == null || h.PartyBelongedTo.LeaderHero != h));
			TextObject hintText;
			this.CanCreateNewParty = this.GetCanCreateNewParty(out hintText);
			this.CreateNewPartyActionHint.HintText = hintText;
			GameTexts.SetVariable("CURRENT", count);
			GameTexts.SetVariable("LIMIT", this._faction.CommanderLimit);
			this.PartiesText = GameTexts.FindText("str_clan_parties", null).ToString();
			GameTexts.SetVariable("CURRENT", this.Caravans.Count);
			this.CaravansText = GameTexts.FindText("str_clan_caravans", null).ToString();
			GameTexts.SetVariable("CURRENT", this.Garrisons.Count);
			this.GarrisonsText = GameTexts.FindText("str_clan_garrisons", null).ToString();
			this.OnPartySelection(this.GetDefaultMember());
		}

		// Token: 0x06001DDE RID: 7646 RVA: 0x0006E530 File Offset: 0x0006C730
		private bool GetCanCreateNewParty(out TextObject disabledReason)
		{
			IEnumerable<Hero> source = from h in (from h in this._faction.Heroes
					where !h.IsDisabled
					select h).Union(this._faction.Companions)
				where h.IsActive && h.PartyBelongedToAsPrisoner == null && !h.IsChild && h.CanLeadParty() && (h.PartyBelongedTo == null || h.PartyBelongedTo.LeaderHero != h)
				select h;
			bool flag = !source.IsEmpty<Hero>();
			int partyGoldLowerThreshold = Campaign.Current.Models.ClanFinanceModel.PartyGoldLowerThreshold;
			bool flag2 = source.Any((Hero h) => Hero.MainHero.Gold > partyGoldLowerThreshold - h.Gold);
			TextObject textObject;
			if (!CampaignUIHelper.GetMapScreenActionIsEnabledWithReason(out textObject))
			{
				disabledReason = textObject;
				return false;
			}
			if (MobileParty.MainParty.IsCurrentlyAtSea || MobileParty.MainParty.IsInRaftState)
			{
				disabledReason = GameTexts.FindText("str_cannot_perform_action_while_sailing", null);
				return false;
			}
			if (this._faction.CommanderLimit - this._faction.WarPartyComponents.Count <= 0)
			{
				disabledReason = GameTexts.FindText("str_clan_doesnt_have_empty_party_slots", null);
				return false;
			}
			if (!flag)
			{
				disabledReason = GameTexts.FindText("str_clan_doesnt_have_available_heroes", null);
				return false;
			}
			if (!flag2)
			{
				disabledReason = new TextObject("{=VSUqbvbE}You don't have enough gold to create a new party.", null);
				return false;
			}
			disabledReason = TextObject.GetEmpty();
			return true;
		}

		// Token: 0x06001DDF RID: 7647 RVA: 0x0006E66B File Offset: 0x0006C86B
		private void OnAnyExpenseChange()
		{
			this.RefreshTotalExpense();
			this._onExpenseChange();
		}

		// Token: 0x06001DE0 RID: 7648 RVA: 0x0006E67E File Offset: 0x0006C87E
		private ClanPartyItemVM GetDefaultMember()
		{
			return this.Parties.FirstOrDefault<ClanPartyItemVM>();
		}

		// Token: 0x06001DE1 RID: 7649 RVA: 0x0006E68B File Offset: 0x0006C88B
		public void ExecuteCreateNewParty()
		{
			if (this.CanCreateNewParty)
			{
				if (this.GetNewPartyLeaderCandidates().Any<ClanCardSelectionItemInfo>())
				{
					this.OnShowNewPartyPopup();
					return;
				}
				MBInformationManager.AddQuickInformation(new TextObject("{=qZvNIVGV}There is no one available in your clan who can lead a party right now.", null), 0, null, null, "");
			}
		}

		// Token: 0x06001DE2 RID: 7650 RVA: 0x0006E6C4 File Offset: 0x0006C8C4
		public void SelectParty(PartyBase party)
		{
			foreach (ClanPartyItemVM clanPartyItemVM in this.Parties)
			{
				if (clanPartyItemVM.Party == party)
				{
					this.OnPartySelection(clanPartyItemVM);
					break;
				}
			}
			foreach (ClanPartyItemVM clanPartyItemVM2 in this.Caravans)
			{
				if (clanPartyItemVM2.Party == party)
				{
					this.OnPartySelection(clanPartyItemVM2);
					break;
				}
			}
		}

		// Token: 0x06001DE3 RID: 7651 RVA: 0x0006E764 File Offset: 0x0006C964
		private void OnPartySelection(ClanPartyItemVM party)
		{
			if (this.CurrentSelectedParty != null)
			{
				this.CurrentSelectedParty.IsSelected = false;
			}
			this.CurrentSelectedParty = party;
			if (party != null)
			{
				party.IsSelected = true;
			}
		}

		// Token: 0x06001DE4 RID: 7652 RVA: 0x0006E78C File Offset: 0x0006C98C
		public override void OnFinalize()
		{
			base.OnFinalize();
			this.Parties.ApplyActionOnAllItems(delegate(ClanPartyItemVM p)
			{
				p.OnFinalize();
			});
			this.Garrisons.ApplyActionOnAllItems(delegate(ClanPartyItemVM p)
			{
				p.OnFinalize();
			});
			this.Caravans.ApplyActionOnAllItems(delegate(ClanPartyItemVM p)
			{
				p.OnFinalize();
			});
		}

		// Token: 0x06001DE5 RID: 7653 RVA: 0x0006E820 File Offset: 0x0006CA20
		public void OnShowNewPartyPopup()
		{
			ClanCardSelectionInfo obj = new ClanCardSelectionInfo(new TextObject("{=0Q4Xo2BQ}Select the Leader of the New Party", null), this.GetNewPartyLeaderCandidates(), new Action<List<object>, Action>(this.OnNewPartyCreationOver), false, 1, 0);
			Action<ClanCardSelectionInfo> openCardSelectionPopup = this._openCardSelectionPopup;
			if (openCardSelectionPopup == null)
			{
				return;
			}
			openCardSelectionPopup(obj);
		}

		// Token: 0x06001DE6 RID: 7654 RVA: 0x0006E865 File Offset: 0x0006CA65
		private IEnumerable<ClanCardSelectionItemInfo> GetNewPartyLeaderCandidates()
		{
			int partyGoldLowerThreshold = Campaign.Current.Models.ClanFinanceModel.PartyGoldLowerThreshold;
			foreach (Hero hero in (from h in this._faction.Heroes
				where !h.IsDisabled
				select h).Union(this._faction.Companions))
			{
				if ((hero.IsActive || hero.IsReleased || hero.IsFugitive) && !hero.IsChild && hero != Hero.MainHero && hero.CanBeGovernorOrHavePartyRole())
				{
					bool flag = false;
					TextObject textObject = TextObject.GetEmpty();
					if (hero.PartyBelongedToAsPrisoner != null)
					{
						textObject = new TextObject("{=vOojEcIf}You cannot assign a prisoner member as a new party leader", null);
					}
					else if (hero.IsReleased)
					{
						textObject = new TextObject("{=OhNYkblK}This hero has just escaped from captors and will be available after some time.", null);
					}
					else if (hero.PartyBelongedTo != null && hero.PartyBelongedTo.LeaderHero == hero)
					{
						textObject = new TextObject("{=aFYwbosi}This hero is already leading a party.", null);
					}
					else if (hero.PartyBelongedTo != null && hero.PartyBelongedTo.LeaderHero != Hero.MainHero)
					{
						textObject = new TextObject("{=FjJi1DJb}This hero is already a part of an another party.", null);
					}
					else if (hero.GovernorOf != null)
					{
						textObject = new TextObject("{=Hz8XO8wk}Governors cannot lead a mobile party and be a governor at the same time.", null);
					}
					else if (hero.HeroState == Hero.CharacterStates.Disabled)
					{
						textObject = new TextObject("{=slzfQzl3}This hero is lost", null);
					}
					else if (hero.HeroState == Hero.CharacterStates.Fugitive)
					{
						textObject = new TextObject("{=dD3kRDHi}This hero is a fugitive and running from their captors. They will be available after some time.", null);
					}
					else if (partyGoldLowerThreshold - hero.Gold > Hero.MainHero.Gold)
					{
						textObject = new TextObject("{=xpCdwmlX}You don't have enough gold to make {HERO.NAME} a party leader.", null);
						textObject.SetCharacterProperties("HERO", hero.CharacterObject, false);
					}
					else if (hero.PartyBelongedTo != null && hero.PartyBelongedTo.IsCurrentlyAtSea)
					{
						textObject = new TextObject("{=1ELK1UbN}{HERO.NAME} is currently sailing.", null);
						textObject.SetCharacterProperties("HERO", hero.CharacterObject, false);
					}
					else
					{
						flag = true;
					}
					yield return new ClanCardSelectionItemInfo(hero, hero.Name, new CharacterImageIdentifier(CampaignUIHelper.GetCharacterCode(hero.CharacterObject, false)), CardSelectionItemSpriteType.None, null, null, this.GetNewPartyLeaderCandidateProperties(hero), !flag, textObject, null);
				}
			}
			IEnumerator<Hero> enumerator = null;
			yield break;
			yield break;
		}

		// Token: 0x06001DE7 RID: 7655 RVA: 0x0006E875 File Offset: 0x0006CA75
		private IEnumerable<ClanCardSelectionItemPropertyInfo> GetNewPartyLeaderCandidateProperties(Hero hero)
		{
			yield return new ClanCardSelectionItemPropertyInfo(TextObject.GetEmpty());
			TextObject textObject = new TextObject("{=hwrQqWir}No Skills", null);
			int num = 0;
			foreach (SkillObject skillObject in this._leaderAssignmentRelevantSkills)
			{
				TextObject textObject2 = new TextObject("{=!}{SKILL_VALUE}", null);
				textObject2.SetTextVariable("SKILL_VALUE", hero.GetSkillValue(skillObject));
				TextObject textObject3 = ClanCardSelectionItemPropertyInfo.CreateLabeledValueText(skillObject.Name, textObject2);
				if (num == 0)
				{
					textObject = textObject3;
				}
				else
				{
					TextObject textObject4 = GameTexts.FindText("str_string_newline_newline_string", null);
					textObject4.SetTextVariable("STR1", textObject);
					textObject4.SetTextVariable("STR2", textObject3);
					textObject = textObject4;
				}
				num++;
			}
			yield return new ClanCardSelectionItemPropertyInfo(GameTexts.FindText("str_skills", null), textObject);
			yield break;
		}

		// Token: 0x06001DE8 RID: 7656 RVA: 0x0006E88C File Offset: 0x0006CA8C
		private void OnNewPartyCreationOver(List<object> selectedItems, Action closePopup)
		{
			if (selectedItems.Count == 1)
			{
				Hero newLeader = selectedItems.FirstOrDefault<object>() as Hero;
				int partyGoldLowerThreshold = Campaign.Current.Models.ClanFinanceModel.PartyGoldLowerThreshold;
				if (newLeader.Gold < partyGoldLowerThreshold)
				{
					string titleText = new TextObject("{=DAYoD0aW}Create Party", null).ToString();
					string text = new TextObject("{=fRz2DJf4}Creating the party will cost you {PARTY_COST}{GOLD_ICON}. Are you sure?", null).SetTextVariable("PARTY_COST", partyGoldLowerThreshold - newLeader.Gold).SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">").ToString();
					InformationManager.ShowInquiry(new InquiryData(titleText, text, true, true, new TextObject("{=aeouhelq}Yes", null).ToString(), new TextObject("{=3CpNUnVl}Cancel", null).ToString(), delegate()
					{
						Action closePopup4 = closePopup;
						if (closePopup4 != null)
						{
							closePopup4();
						}
						this.CreateNewClanParty(newLeader, partyGoldLowerThreshold);
					}, null, "", 0f, null, null, null), false, false);
					return;
				}
				Action closePopup2 = closePopup;
				if (closePopup2 != null)
				{
					closePopup2();
				}
				this.CreateNewClanParty(newLeader, partyGoldLowerThreshold);
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

		// Token: 0x06001DE9 RID: 7657 RVA: 0x0006E9D8 File Offset: 0x0006CBD8
		private void CreateNewClanParty(Hero newLeader, int partyGoldLowerThreshold)
		{
			if (newLeader.PartyBelongedTo == MobileParty.MainParty)
			{
				this._openPartyAsManage(newLeader);
				return;
			}
			MobileParty mobileParty = MobilePartyHelper.CreateNewClanMobileParty(newLeader, this._faction);
			if (newLeader.Gold < partyGoldLowerThreshold)
			{
				GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, newLeader, partyGoldLowerThreshold - newLeader.Gold, false);
			}
			mobileParty.SetMoveModeHold();
			this._onRefresh();
		}

		// Token: 0x06001DEA RID: 7658 RVA: 0x0006EA3C File Offset: 0x0006CC3C
		public void OnShowChangeLeaderPopup()
		{
			ClanPartyItemVM currentSelectedParty = this.CurrentSelectedParty;
			bool flag;
			if (currentSelectedParty == null)
			{
				flag = null != null;
			}
			else
			{
				PartyBase party = currentSelectedParty.Party;
				flag = ((party != null) ? party.MobileParty : null) != null;
			}
			if (flag)
			{
				ClanCardSelectionInfo obj = new ClanCardSelectionInfo(GameTexts.FindText("str_change_party_leader", null), this.GetChangeLeaderCandidates(), new Action<List<object>, Action>(this.OnChangeLeaderOver), false, 1, 0);
				Action<ClanCardSelectionInfo> openCardSelectionPopup = this._openCardSelectionPopup;
				if (openCardSelectionPopup == null)
				{
					return;
				}
				openCardSelectionPopup(obj);
			}
		}

		// Token: 0x06001DEB RID: 7659 RVA: 0x0006EAA1 File Offset: 0x0006CCA1
		private IEnumerable<ClanCardSelectionItemInfo> GetChangeLeaderCandidates()
		{
			TextObject disabledReason;
			bool canDisbandParty = this.GetCanDisbandParty(out disabledReason);
			yield return new ClanCardSelectionItemInfo(GameTexts.FindText("str_disband_party", null), !canDisbandParty, disabledReason, null);
			foreach (Hero hero in (from h in this._faction.Heroes
				where !h.IsDisabled
				select h).Union(this._faction.Companions))
			{
				if ((hero.IsActive || hero.IsReleased || hero.IsFugitive || hero.IsTraveling) && !hero.IsChild && hero != Hero.MainHero && hero.CanLeadParty())
				{
					Hero hero2 = hero;
					ClanPartyMemberItemVM leaderMember = this.CurrentSelectedParty.LeaderMember;
					if (hero2 != ((leaderMember != null) ? leaderMember.HeroObject : null))
					{
						TextObject disabledReason2;
						bool flag = FactionHelper.IsMainClanMemberAvailableForPartyLeaderChange(hero, true, this.CurrentSelectedParty.Party.MobileParty, out disabledReason2);
						CharacterImageIdentifier image = new CharacterImageIdentifier(CampaignUIHelper.GetCharacterCode(hero.CharacterObject, false));
						yield return new ClanCardSelectionItemInfo(hero, hero.Name, image, CardSelectionItemSpriteType.None, null, null, this.GetChangeLeaderCandidateProperties(hero), !flag, disabledReason2, null);
					}
				}
			}
			IEnumerator<Hero> enumerator = null;
			yield break;
			yield break;
		}

		// Token: 0x06001DEC RID: 7660 RVA: 0x0006EAB1 File Offset: 0x0006CCB1
		private IEnumerable<ClanCardSelectionItemPropertyInfo> GetChangeLeaderCandidateProperties(Hero hero)
		{
			TextObject teleportationDelayText = CampaignUIHelper.GetTeleportationDelayText(hero, this.CurrentSelectedParty.Party);
			yield return new ClanCardSelectionItemPropertyInfo(teleportationDelayText);
			TextObject textObject = new TextObject("{=hwrQqWir}No Skills", null);
			int num = 0;
			foreach (SkillObject skillObject in this._leaderAssignmentRelevantSkills)
			{
				TextObject textObject2 = new TextObject("{=!}{SKILL_VALUE}", null);
				textObject2.SetTextVariable("SKILL_VALUE", hero.GetSkillValue(skillObject));
				TextObject textObject3 = ClanCardSelectionItemPropertyInfo.CreateLabeledValueText(skillObject.Name, textObject2);
				if (num == 0)
				{
					textObject = textObject3;
				}
				else
				{
					TextObject textObject4 = GameTexts.FindText("str_string_newline_newline_string", null);
					textObject4.SetTextVariable("STR1", textObject);
					textObject4.SetTextVariable("STR2", textObject3);
					textObject = textObject4;
				}
				num++;
			}
			yield return new ClanCardSelectionItemPropertyInfo(GameTexts.FindText("str_skills", null), textObject);
			yield break;
		}

		// Token: 0x06001DED RID: 7661 RVA: 0x0006EAC8 File Offset: 0x0006CCC8
		private void OnChangeLeaderOver(List<object> selectedItems, Action closePopup)
		{
			if (selectedItems.Count == 1)
			{
				Hero newLeader = selectedItems.FirstOrDefault<object>() as Hero;
				bool isDisband = newLeader == null;
				int partyGoldLowerThreshold = Campaign.Current.Models.ClanFinanceModel.PartyGoldLowerThreshold;
				ClanPartyItemVM currentSelectedParty = this.CurrentSelectedParty;
				PartyBase partyBase = ((currentSelectedParty != null) ? currentSelectedParty.Party : null);
				MobileParty mobileParty = ((partyBase != null) ? partyBase.MobileParty : null);
				DelayedTeleportationModel delayedTeleportationModel = Campaign.Current.Models.DelayedTeleportationModel;
				int num = ((!isDisband && mobileParty != null) ? ((int)Math.Ceiling((double)delayedTeleportationModel.GetTeleportationDelayAsHours(newLeader, mobileParty.Party).ResultNumber)) : 0);
				MBTextManager.SetTextVariable("TRAVEL_DURATION", CampaignUIHelper.GetHoursAndDaysTextFromHourValue(num).ToString(), false);
				Hero newLeader2 = newLeader;
				if (((newLeader2 != null) ? newLeader2.CharacterObject : null) != null)
				{
					StringHelpers.SetCharacterProperties("LEADER", newLeader.CharacterObject, null, false);
					MBTextManager.SetTextVariable("PARTY_COST", partyGoldLowerThreshold - newLeader.Gold);
					MBTextManager.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">", false);
					MBTextManager.SetTextVariable("DOES_LEADER_NEED_GOLD", (partyGoldLowerThreshold > newLeader.Gold) ? 1 : 0);
				}
				if (isDisband && partyBase != null && partyBase.Ships.Count > 0)
				{
					MBTextManager.SetTextVariable("DOES_DISBANDING_PARTY_HAVE_SHIP", true);
				}
				object obj = GameTexts.FindText(isDisband ? "str_disband_party" : "str_change_clan_party_leader", null);
				TextObject textObject = GameTexts.FindText(isDisband ? "str_disband_party_inquiry" : ((num == 0) ? "str_change_clan_party_leader_instantly_inquiry" : "str_change_clan_party_leader_inquiry"), null);
				InformationManager.ShowInquiry(new InquiryData(obj.ToString(), textObject.ToString(), true, true, GameTexts.FindText("str_yes", null).ToString(), GameTexts.FindText("str_no", null).ToString(), delegate()
				{
					Action closePopup3 = closePopup;
					if (closePopup3 != null)
					{
						closePopup3();
					}
					this.OnPartyLeaderChanged(newLeader);
					if (isDisband)
					{
						this.OnDisbandCurrentParty();
					}
					else if (newLeader.Gold < partyGoldLowerThreshold)
					{
						GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, newLeader, partyGoldLowerThreshold - newLeader.Gold, false);
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

		// Token: 0x06001DEE RID: 7662 RVA: 0x0006ED00 File Offset: 0x0006CF00
		private void OnPartyLeaderChanged(Hero newLeader)
		{
			ClanPartyItemVM currentSelectedParty = this.CurrentSelectedParty;
			bool flag;
			if (currentSelectedParty == null)
			{
				flag = null != null;
			}
			else
			{
				PartyBase party = currentSelectedParty.Party;
				flag = ((party != null) ? party.LeaderHero : null) != null;
			}
			if (flag)
			{
				if (newLeader == null)
				{
					Hero leaderHero = this.CurrentSelectedParty.Party.LeaderHero;
					this.CurrentSelectedParty.Party.MobileParty.RemovePartyLeader();
					MakeHeroFugitiveAction.Apply(leaderHero, false);
				}
				else
				{
					TeleportHeroAction.ApplyDelayedTeleportToParty(this.CurrentSelectedParty.Party.LeaderHero, MobileParty.MainParty);
				}
			}
			if (newLeader != null)
			{
				TeleportHeroAction.ApplyDelayedTeleportToPartyAsPartyLeader(newLeader, this.CurrentSelectedParty.Party.MobileParty);
			}
		}

		// Token: 0x06001DEF RID: 7663 RVA: 0x0006ED90 File Offset: 0x0006CF90
		private void OnDisbandCurrentParty()
		{
			DisbandPartyAction.StartDisband(this.CurrentSelectedParty.Party.MobileParty);
		}

		// Token: 0x06001DF0 RID: 7664 RVA: 0x0006EDA8 File Offset: 0x0006CFA8
		private bool GetCanDisbandParty(out TextObject cannotDisbandReason)
		{
			bool result = false;
			cannotDisbandReason = TextObject.GetEmpty();
			ClanPartyItemVM currentSelectedParty = this.CurrentSelectedParty;
			MobileParty mobileParty;
			if (currentSelectedParty == null)
			{
				mobileParty = null;
			}
			else
			{
				PartyBase party = currentSelectedParty.Party;
				mobileParty = ((party != null) ? party.MobileParty : null);
			}
			MobileParty mobileParty2 = mobileParty;
			if (mobileParty2 != null)
			{
				TextObject textObject;
				if (!CampaignUIHelper.GetMapScreenActionIsEnabledWithReason(out textObject))
				{
					cannotDisbandReason = textObject;
				}
				else if (mobileParty2.IsMilitia)
				{
					cannotDisbandReason = GameTexts.FindText("str_cannot_disband_milita_party", null);
				}
				else if (mobileParty2.IsGarrison)
				{
					cannotDisbandReason = GameTexts.FindText("str_cannot_disband_garrison_party", null);
				}
				else if (mobileParty2.IsMainParty)
				{
					cannotDisbandReason = GameTexts.FindText("str_cannot_disband_main_party", null);
				}
				else if (this.CurrentSelectedParty.IsDisbanding)
				{
					cannotDisbandReason = GameTexts.FindText("str_cannot_disband_already_disbanding_party", null);
				}
				else if (mobileParty2.MapEvent != null || mobileParty2.SiegeEvent != null)
				{
					cannotDisbandReason = GameTexts.FindText("str_cannot_disband_during_battle", null);
				}
				else
				{
					result = true;
				}
			}
			return result;
		}

		// Token: 0x17000A28 RID: 2600
		// (get) Token: 0x06001DF1 RID: 7665 RVA: 0x0006EE77 File Offset: 0x0006D077
		// (set) Token: 0x06001DF2 RID: 7666 RVA: 0x0006EE7F File Offset: 0x0006D07F
		[DataSourceProperty]
		public HintViewModel CreateNewPartyActionHint
		{
			get
			{
				return this._createNewPartyActionHint;
			}
			set
			{
				if (value != this._createNewPartyActionHint)
				{
					this._createNewPartyActionHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "CreateNewPartyActionHint");
				}
			}
		}

		// Token: 0x17000A29 RID: 2601
		// (get) Token: 0x06001DF3 RID: 7667 RVA: 0x0006EE9D File Offset: 0x0006D09D
		// (set) Token: 0x06001DF4 RID: 7668 RVA: 0x0006EEA5 File Offset: 0x0006D0A5
		[DataSourceProperty]
		public bool IsAnyValidPartySelected
		{
			get
			{
				return this._isAnyValidPartySelected;
			}
			set
			{
				if (value != this._isAnyValidPartySelected)
				{
					this._isAnyValidPartySelected = value;
					base.OnPropertyChangedWithValue(value, "IsAnyValidPartySelected");
				}
			}
		}

		// Token: 0x17000A2A RID: 2602
		// (get) Token: 0x06001DF5 RID: 7669 RVA: 0x0006EEC3 File Offset: 0x0006D0C3
		// (set) Token: 0x06001DF6 RID: 7670 RVA: 0x0006EECB File Offset: 0x0006D0CB
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

		// Token: 0x17000A2B RID: 2603
		// (get) Token: 0x06001DF7 RID: 7671 RVA: 0x0006EEEE File Offset: 0x0006D0EE
		// (set) Token: 0x06001DF8 RID: 7672 RVA: 0x0006EEF6 File Offset: 0x0006D0F6
		[DataSourceProperty]
		public string CaravansText
		{
			get
			{
				return this._caravansText;
			}
			set
			{
				if (value != this._caravansText)
				{
					this._caravansText = value;
					base.OnPropertyChangedWithValue<string>(value, "CaravansText");
				}
			}
		}

		// Token: 0x17000A2C RID: 2604
		// (get) Token: 0x06001DF9 RID: 7673 RVA: 0x0006EF19 File Offset: 0x0006D119
		// (set) Token: 0x06001DFA RID: 7674 RVA: 0x0006EF21 File Offset: 0x0006D121
		[DataSourceProperty]
		public string GarrisonsText
		{
			get
			{
				return this._garrisonsText;
			}
			set
			{
				if (value != this._garrisonsText)
				{
					this._garrisonsText = value;
					base.OnPropertyChangedWithValue<string>(value, "GarrisonsText");
				}
			}
		}

		// Token: 0x17000A2D RID: 2605
		// (get) Token: 0x06001DFB RID: 7675 RVA: 0x0006EF44 File Offset: 0x0006D144
		// (set) Token: 0x06001DFC RID: 7676 RVA: 0x0006EF4C File Offset: 0x0006D14C
		[DataSourceProperty]
		public string PartiesText
		{
			get
			{
				return this._partiesText;
			}
			set
			{
				if (value != this._partiesText)
				{
					this._partiesText = value;
					base.OnPropertyChangedWithValue<string>(value, "PartiesText");
				}
			}
		}

		// Token: 0x17000A2E RID: 2606
		// (get) Token: 0x06001DFD RID: 7677 RVA: 0x0006EF6F File Offset: 0x0006D16F
		// (set) Token: 0x06001DFE RID: 7678 RVA: 0x0006EF77 File Offset: 0x0006D177
		[DataSourceProperty]
		public string MoraleText
		{
			get
			{
				return this._moraleText;
			}
			set
			{
				if (value != this._moraleText)
				{
					this._moraleText = value;
					base.OnPropertyChangedWithValue<string>(value, "MoraleText");
				}
			}
		}

		// Token: 0x17000A2F RID: 2607
		// (get) Token: 0x06001DFF RID: 7679 RVA: 0x0006EF9A File Offset: 0x0006D19A
		// (set) Token: 0x06001E00 RID: 7680 RVA: 0x0006EFA2 File Offset: 0x0006D1A2
		[DataSourceProperty]
		public string LocationText
		{
			get
			{
				return this._locationText;
			}
			set
			{
				if (value != this._locationText)
				{
					this._locationText = value;
					base.OnPropertyChangedWithValue<string>(value, "LocationText");
				}
			}
		}

		// Token: 0x17000A30 RID: 2608
		// (get) Token: 0x06001E01 RID: 7681 RVA: 0x0006EFC5 File Offset: 0x0006D1C5
		// (set) Token: 0x06001E02 RID: 7682 RVA: 0x0006EFCD File Offset: 0x0006D1CD
		[DataSourceProperty]
		public string CreateNewPartyText
		{
			get
			{
				return this._createNewPartyText;
			}
			set
			{
				if (value != this._createNewPartyText)
				{
					this._createNewPartyText = value;
					base.OnPropertyChangedWithValue<string>(value, "CreateNewPartyText");
				}
			}
		}

		// Token: 0x17000A31 RID: 2609
		// (get) Token: 0x06001E03 RID: 7683 RVA: 0x0006EFF0 File Offset: 0x0006D1F0
		// (set) Token: 0x06001E04 RID: 7684 RVA: 0x0006EFF8 File Offset: 0x0006D1F8
		[DataSourceProperty]
		public string SizeText
		{
			get
			{
				return this._sizeText;
			}
			set
			{
				if (value != this._sizeText)
				{
					this._sizeText = value;
					base.OnPropertyChangedWithValue<string>(value, "SizeText");
				}
			}
		}

		// Token: 0x17000A32 RID: 2610
		// (get) Token: 0x06001E05 RID: 7685 RVA: 0x0006F01B File Offset: 0x0006D21B
		// (set) Token: 0x06001E06 RID: 7686 RVA: 0x0006F023 File Offset: 0x0006D223
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

		// Token: 0x17000A33 RID: 2611
		// (get) Token: 0x06001E07 RID: 7687 RVA: 0x0006F041 File Offset: 0x0006D241
		// (set) Token: 0x06001E08 RID: 7688 RVA: 0x0006F049 File Offset: 0x0006D249
		[DataSourceProperty]
		public bool CanCreateNewParty
		{
			get
			{
				return this._canCreateNewParty;
			}
			set
			{
				if (value != this._canCreateNewParty)
				{
					this._canCreateNewParty = value;
					base.OnPropertyChangedWithValue(value, "CanCreateNewParty");
				}
			}
		}

		// Token: 0x17000A34 RID: 2612
		// (get) Token: 0x06001E09 RID: 7689 RVA: 0x0006F067 File Offset: 0x0006D267
		// (set) Token: 0x06001E0A RID: 7690 RVA: 0x0006F06F File Offset: 0x0006D26F
		[DataSourceProperty]
		public MBBindingList<ClanPartyItemVM> Parties
		{
			get
			{
				return this._parties;
			}
			set
			{
				if (value != this._parties)
				{
					this._parties = value;
					base.OnPropertyChangedWithValue<MBBindingList<ClanPartyItemVM>>(value, "Parties");
				}
			}
		}

		// Token: 0x17000A35 RID: 2613
		// (get) Token: 0x06001E0B RID: 7691 RVA: 0x0006F08D File Offset: 0x0006D28D
		// (set) Token: 0x06001E0C RID: 7692 RVA: 0x0006F095 File Offset: 0x0006D295
		[DataSourceProperty]
		public MBBindingList<ClanPartyItemVM> Caravans
		{
			get
			{
				return this._caravans;
			}
			set
			{
				if (value != this._caravans)
				{
					this._caravans = value;
					base.OnPropertyChangedWithValue<MBBindingList<ClanPartyItemVM>>(value, "Caravans");
				}
			}
		}

		// Token: 0x17000A36 RID: 2614
		// (get) Token: 0x06001E0D RID: 7693 RVA: 0x0006F0B3 File Offset: 0x0006D2B3
		// (set) Token: 0x06001E0E RID: 7694 RVA: 0x0006F0BB File Offset: 0x0006D2BB
		[DataSourceProperty]
		public MBBindingList<ClanPartyItemVM> Garrisons
		{
			get
			{
				return this._garrisons;
			}
			set
			{
				if (value != this._garrisons)
				{
					this._garrisons = value;
					base.OnPropertyChangedWithValue<MBBindingList<ClanPartyItemVM>>(value, "Garrisons");
				}
			}
		}

		// Token: 0x17000A37 RID: 2615
		// (get) Token: 0x06001E0F RID: 7695 RVA: 0x0006F0D9 File Offset: 0x0006D2D9
		// (set) Token: 0x06001E10 RID: 7696 RVA: 0x0006F0E1 File Offset: 0x0006D2E1
		[DataSourceProperty]
		public ClanPartyItemVM CurrentSelectedParty
		{
			get
			{
				return this._currentSelectedParty;
			}
			set
			{
				if (value != this._currentSelectedParty)
				{
					this._currentSelectedParty = value;
					base.OnPropertyChangedWithValue<ClanPartyItemVM>(value, "CurrentSelectedParty");
					this.IsAnyValidPartySelected = value != null;
				}
			}
		}

		// Token: 0x17000A38 RID: 2616
		// (get) Token: 0x06001E11 RID: 7697 RVA: 0x0006F109 File Offset: 0x0006D309
		// (set) Token: 0x06001E12 RID: 7698 RVA: 0x0006F111 File Offset: 0x0006D311
		[DataSourceProperty]
		public ClanPartiesSortControllerVM SortController
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
					base.OnPropertyChangedWithValue<ClanPartiesSortControllerVM>(value, "SortController");
				}
			}
		}

		// Token: 0x04000DF5 RID: 3573
		private Action _onExpenseChange;

		// Token: 0x04000DF6 RID: 3574
		private Action<Hero> _openPartyAsManage;

		// Token: 0x04000DF7 RID: 3575
		private Action<ClanCardSelectionInfo> _openCardSelectionPopup;

		// Token: 0x04000DF8 RID: 3576
		private readonly IDisbandPartyCampaignBehavior _disbandBehavior;

		// Token: 0x04000DF9 RID: 3577
		private readonly ITeleportationCampaignBehavior _teleportationBehavior;

		// Token: 0x04000DFA RID: 3578
		private readonly Action _onRefresh;

		// Token: 0x04000DFB RID: 3579
		private readonly Clan _faction;

		// Token: 0x04000DFC RID: 3580
		private readonly IEnumerable<SkillObject> _leaderAssignmentRelevantSkills = new List<SkillObject>
		{
			DefaultSkills.Engineering,
			DefaultSkills.Steward,
			DefaultSkills.Scouting,
			DefaultSkills.Medicine
		};

		// Token: 0x04000DFD RID: 3581
		private MBBindingList<ClanPartyItemVM> _parties;

		// Token: 0x04000DFE RID: 3582
		private MBBindingList<ClanPartyItemVM> _garrisons;

		// Token: 0x04000DFF RID: 3583
		private MBBindingList<ClanPartyItemVM> _caravans;

		// Token: 0x04000E00 RID: 3584
		private ClanPartyItemVM _currentSelectedParty;

		// Token: 0x04000E01 RID: 3585
		private HintViewModel _createNewPartyActionHint;

		// Token: 0x04000E02 RID: 3586
		private bool _canCreateNewParty;

		// Token: 0x04000E03 RID: 3587
		private bool _isSelected;

		// Token: 0x04000E04 RID: 3588
		private string _nameText;

		// Token: 0x04000E05 RID: 3589
		private string _moraleText;

		// Token: 0x04000E06 RID: 3590
		private string _locationText;

		// Token: 0x04000E07 RID: 3591
		private string _sizeText;

		// Token: 0x04000E08 RID: 3592
		private string _createNewPartyText;

		// Token: 0x04000E09 RID: 3593
		private string _partiesText;

		// Token: 0x04000E0A RID: 3594
		private string _caravansText;

		// Token: 0x04000E0B RID: 3595
		private string _garrisonsText;

		// Token: 0x04000E0C RID: 3596
		private bool _isAnyValidPartySelected;

		// Token: 0x04000E0D RID: 3597
		private ClanPartiesSortControllerVM _sortController;
	}
}
