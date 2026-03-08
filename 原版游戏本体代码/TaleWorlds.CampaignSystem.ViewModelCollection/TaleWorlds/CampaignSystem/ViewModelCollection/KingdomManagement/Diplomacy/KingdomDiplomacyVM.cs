using System;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Diplomacy
{
	// Token: 0x0200006F RID: 111
	public class KingdomDiplomacyVM : KingdomCategoryVM
	{
		// Token: 0x060008EC RID: 2284 RVA: 0x00027AD4 File Offset: 0x00025CD4
		public KingdomDiplomacyVM(Action<KingdomDecision> forceDecision)
		{
			this._forceDecision = forceDecision;
			this._playerKingdom = Hero.MainHero.MapFaction as Kingdom;
			this.PlayerWars = new MBBindingList<KingdomWarItemVM>();
			this.PlayerTruces = new MBBindingList<KingdomTruceItemVM>();
			this.WarsSortController = new KingdomWarSortControllerVM(ref this._playerWars);
			this.Actions = new MBBindingList<KingdomDiplomacyProposalActionItemVM>();
			this.ExecuteShowStatComparisons();
			this.RefreshValues();
			this.SetDefaultSelectedItem();
		}

		// Token: 0x060008ED RID: 2285 RVA: 0x00027B48 File Offset: 0x00025D48
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.BehaviorSelection = new SelectorVM<SelectorItemVM>(0, new Action<SelectorVM<SelectorItemVM>>(this.OnBehaviorSelectionChanged));
			this.BehaviorSelection.AddItem(new SelectorItemVM(GameTexts.FindText("str_kingdom_war_strategy_balanced", null), GameTexts.FindText("str_kingdom_war_strategy_balanced_desc", null)));
			this.BehaviorSelection.AddItem(new SelectorItemVM(GameTexts.FindText("str_kingdom_war_strategy_defensive", null), GameTexts.FindText("str_kingdom_war_strategy_defensive_desc", null)));
			this.BehaviorSelection.AddItem(new SelectorItemVM(GameTexts.FindText("str_kingdom_war_strategy_offensive", null), GameTexts.FindText("str_kingdom_war_strategy_offensive_desc", null)));
			this.RefreshDiplomacyList();
			this.BehaviorSelectionTitle = GameTexts.FindText("str_kingdom_war_strategy", null).ToString();
			base.NoItemSelectedText = GameTexts.FindText("str_kingdom_no_war_selected", null).ToString();
			this.PlayerWarsText = GameTexts.FindText("str_kingdom_at_war", null).ToString();
			this.PlayerTrucesText = GameTexts.FindText("str_kingdom_at_peace", null).ToString();
			this.WarsText = GameTexts.FindText("str_diplomatic_group", null).ToString();
			this.ShowStatBarsHint = new HintViewModel(GameTexts.FindText("str_kingdom_war_show_comparison_bars", null), null);
			this.ShowWarLogsHint = new HintViewModel(GameTexts.FindText("str_kingdom_war_show_war_logs", null), null);
			this.PlayerWars.ApplyActionOnAllItems(delegate(KingdomWarItemVM x)
			{
				x.RefreshValues();
			});
			this.PlayerTruces.ApplyActionOnAllItems(delegate(KingdomTruceItemVM x)
			{
				x.RefreshValues();
			});
			KingdomDiplomacyItemVM currentSelectedDiplomacyItem = this.CurrentSelectedDiplomacyItem;
			if (currentSelectedDiplomacyItem != null)
			{
				currentSelectedDiplomacyItem.RefreshValues();
			}
			this.Actions.ApplyActionOnAllItems(delegate(KingdomDiplomacyProposalActionItemVM x)
			{
				x.RefreshValues();
			});
		}

		// Token: 0x060008EE RID: 2286 RVA: 0x00027D18 File Offset: 0x00025F18
		public void RefreshDiplomacyList()
		{
			Kingdom kingdom = Clan.PlayerClan.Kingdom;
			int notificationCount;
			if (kingdom == null)
			{
				notificationCount = 0;
			}
			else
			{
				notificationCount = kingdom.UnresolvedDecisions.Count((KingdomDecision d) => !d.ShouldBeCancelled());
			}
			base.NotificationCount = notificationCount;
			this.PlayerWars.Clear();
			this.PlayerTruces.Clear();
			foreach (StanceLink stanceLink in from x in this._playerKingdom.FactionsAtWarWith
				select this._playerKingdom.GetStanceWith(x) into w
				orderby w.Faction1.Name.ToString() + w.Faction2.Name.ToString()
				select w)
			{
				if (stanceLink.Faction1.IsKingdomFaction && stanceLink.Faction2.IsKingdomFaction)
				{
					this.PlayerWars.Add(new KingdomWarItemVM(stanceLink, new Action<KingdomWarItemVM>(this.OnDiplomacyItemSelection)));
				}
			}
			foreach (Kingdom kingdom2 in Kingdom.All)
			{
				if (kingdom2 != this._playerKingdom && !kingdom2.IsEliminated && (DiplomacyHelper.IsSameFactionAndNotEliminated(kingdom2, this._playerKingdom) || FactionManager.IsNeutralWithFaction(kingdom2, this._playerKingdom)))
				{
					this.PlayerTruces.Add(new KingdomTruceItemVM(this._playerKingdom, kingdom2, new Action<KingdomDiplomacyItemVM>(this.OnDiplomacyItemSelection)));
				}
			}
			GameTexts.SetVariable("STR", this.PlayerWars.Count);
			this.NumOfPlayerWarsText = GameTexts.FindText("str_STR_in_parentheses", null).ToString();
			GameTexts.SetVariable("STR", this.PlayerTruces.Count);
			this.NumOfPlayerTrucesText = GameTexts.FindText("str_STR_in_parentheses", null).ToString();
			this.SetDefaultSelectedItem();
		}

		// Token: 0x060008EF RID: 2287 RVA: 0x00027F0C File Offset: 0x0002610C
		public void SelectKingdom(Kingdom kingdom)
		{
			bool flag = false;
			foreach (KingdomWarItemVM kingdomWarItemVM in this.PlayerWars)
			{
				if (kingdomWarItemVM.Faction1 == kingdom || kingdomWarItemVM.Faction2 == kingdom)
				{
					this.OnSetCurrentDiplomacyItem(kingdomWarItemVM);
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				foreach (KingdomTruceItemVM kingdomTruceItemVM in this.PlayerTruces)
				{
					if (kingdomTruceItemVM.Faction1 == kingdom || kingdomTruceItemVM.Faction2 == kingdom)
					{
						this.OnSetCurrentDiplomacyItem(kingdomTruceItemVM);
						flag = true;
						break;
					}
				}
			}
		}

		// Token: 0x060008F0 RID: 2288 RVA: 0x00027FCC File Offset: 0x000261CC
		private void OnSetCurrentDiplomacyItem(KingdomDiplomacyItemVM item)
		{
			this.Actions.Clear();
			if (item is KingdomWarItemVM)
			{
				this.OnSetWarItem(item as KingdomWarItemVM);
			}
			else if (item is KingdomTruceItemVM)
			{
				this.OnSetPeaceItem(item as KingdomTruceItemVM);
			}
			this.RefreshCurrentWarVisuals(item);
			this.UpdateBehaviorSelection();
		}

		// Token: 0x060008F1 RID: 2289 RVA: 0x0002801C File Offset: 0x0002621C
		private void OnSetWarItem(KingdomWarItemVM item)
		{
			KingdomDiplomacyVM.<>c__DisplayClass8_0 CS$<>8__locals1 = new KingdomDiplomacyVM.<>c__DisplayClass8_0();
			CS$<>8__locals1.item = item;
			CS$<>8__locals1.<>4__this = this;
			CS$<>8__locals1.unresolvedPeaceDecision = Clan.PlayerClan.Kingdom.UnresolvedDecisions.FirstOrDefault(delegate(KingdomDecision d)
			{
				MakePeaceKingdomDecision makePeaceKingdomDecision;
				return (makePeaceKingdomDecision = d as MakePeaceKingdomDecision) != null && makePeaceKingdomDecision.FactionToMakePeaceWith == CS$<>8__locals1.item.Faction2 && !d.ShouldBeCancelled();
			});
			if (CS$<>8__locals1.unresolvedPeaceDecision != null)
			{
				TextObject hintText;
				this.Actions.Add(new KingdomDiplomacyProposalActionItemVM(GameTexts.FindText("str_resolve", null), GameTexts.FindText("str_resolve_explanation", null), 0, this.GetAreProposalActionsEnabledWithReason(0f, out hintText), hintText, delegate()
				{
					CS$<>8__locals1.<>4__this._forceDecision(CS$<>8__locals1.unresolvedPeaceDecision);
				}));
				return;
			}
			int durationInDays;
			int dailyPeaceTributeToPay = Campaign.Current.Models.DiplomacyModel.GetDailyTributeToPay(Clan.PlayerClan, CS$<>8__locals1.item.Faction2.Leader.Clan, out durationInDays);
			dailyPeaceTributeToPay = 10 * (dailyPeaceTributeToPay / 10);
			TextObject textObject = ((dailyPeaceTributeToPay == 0) ? GameTexts.FindText("str_propose_peace_explanation", null) : ((dailyPeaceTributeToPay > 0) ? GameTexts.FindText("str_propose_peace_explanation_pay_tribute", null) : GameTexts.FindText("str_propose_peace_explanation_get_tribute", null)));
			textObject.SetTextVariable("SUPPORT", this.CalculatePeaceSupport(CS$<>8__locals1.item.Faction2, dailyPeaceTributeToPay, durationInDays)).SetTextVariable("TRIBUTE_AMOUNT", MathF.Abs(dailyPeaceTributeToPay)).SetTextVariable("TRIBUTE_DURATION", durationInDays);
			int influenceCostOfProposingPeace = Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfProposingPeace(Clan.PlayerClan);
			TextObject hintText2;
			this.Actions.Add(new KingdomDiplomacyProposalActionItemVM((this._playerKingdom.Clans.Count > 1) ? GameTexts.FindText("str_policy_propose", null) : GameTexts.FindText("str_policy_enact", null), textObject, influenceCostOfProposingPeace, this.GetIsProposingPeaceEnabledWithReason(CS$<>8__locals1.item, (float)influenceCostOfProposingPeace, out hintText2), hintText2, delegate()
			{
				CS$<>8__locals1.<>4__this.OnDeclarePeace(CS$<>8__locals1.item, dailyPeaceTributeToPay, durationInDays);
			}));
		}

		// Token: 0x060008F2 RID: 2290 RVA: 0x00028214 File Offset: 0x00026414
		private void OnSetPeaceItem(KingdomTruceItemVM item)
		{
			KingdomDecision unresolvedAllianceDecision = Clan.PlayerClan.Kingdom.UnresolvedDecisions.FirstOrDefault(delegate(KingdomDecision d)
			{
				StartAllianceDecision startAllianceDecision;
				return (startAllianceDecision = d as StartAllianceDecision) != null && startAllianceDecision.KingdomToStartAllianceWith == item.Faction2 && !d.ShouldBeCancelled();
			});
			if (unresolvedAllianceDecision != null)
			{
				TextObject hintText;
				this.Actions.Add(new KingdomDiplomacyProposalActionItemVM(GameTexts.FindText("str_resolve", null), GameTexts.FindText("str_resolve_explanation", null), 0, this.GetAreProposalActionsEnabledWithReason(0f, out hintText), hintText, delegate()
				{
					this._forceDecision(unresolvedAllianceDecision);
				}));
			}
			else if (!Campaign.Current.GetCampaignBehavior<IAllianceCampaignBehavior>().IsAllyWithKingdom(item.Faction1 as Kingdom, item.Faction2 as Kingdom))
			{
				int influenceCostOfProposingStartingAlliance = Campaign.Current.Models.AllianceModel.GetInfluenceCostOfProposingStartingAlliance(Clan.PlayerClan);
				TextObject hintText2;
				this.Actions.Add(new KingdomDiplomacyProposalActionItemVM((this._playerKingdom.Clans.Count > 1) ? GameTexts.FindText("str_policy_propose", null) : GameTexts.FindText("str_policy_enact", null), GameTexts.FindText("str_propose_alliance_explanation", null).SetTextVariable("SUPPORT", this.CalculateAllianceSupport(item.Faction2)), influenceCostOfProposingStartingAlliance, this.GetIsProposingAllianceEnabledWithReason(item, (float)influenceCostOfProposingStartingAlliance, out hintText2), hintText2, delegate()
				{
					this.OnStartAlliance(item);
				}));
			}
			KingdomDecision unresolvedWarDecision = Clan.PlayerClan.Kingdom.UnresolvedDecisions.FirstOrDefault(delegate(KingdomDecision d)
			{
				DeclareWarDecision declareWarDecision;
				return (declareWarDecision = d as DeclareWarDecision) != null && declareWarDecision.FactionToDeclareWarOn == item.Faction2 && !d.ShouldBeCancelled();
			});
			if (unresolvedWarDecision != null)
			{
				TextObject hintText3;
				this.Actions.Add(new KingdomDiplomacyProposalActionItemVM(GameTexts.FindText("str_resolve", null), GameTexts.FindText("str_resolve_explanation", null), 0, this.GetAreProposalActionsEnabledWithReason(0f, out hintText3), hintText3, delegate()
				{
					this._forceDecision(unresolvedWarDecision);
				}));
			}
			else
			{
				int influenceCostOfProposingWar = Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfProposingWar(Clan.PlayerClan);
				TextObject hintText4;
				this.Actions.Add(new KingdomDiplomacyProposalActionItemVM((this._playerKingdom.Clans.Count > 1) ? GameTexts.FindText("str_policy_propose", null) : GameTexts.FindText("str_policy_enact", null), GameTexts.FindText("str_propose_war_explanation", null).SetTextVariable("SUPPORT", this.CalculateWarSupport(item.Faction2)), influenceCostOfProposingWar, this.GetIsProposingWarEnabledWithReason(item, (float)influenceCostOfProposingWar, out hintText4), hintText4, delegate()
				{
					this.OnDeclareWar(item);
				}));
			}
			KingdomDecision unresolvedTradeAgreementDecision = Clan.PlayerClan.Kingdom.UnresolvedDecisions.FirstOrDefault(delegate(KingdomDecision d)
			{
				TradeAgreementDecision tradeAgreementDecision;
				return (tradeAgreementDecision = d as TradeAgreementDecision) != null && tradeAgreementDecision.TargetKingdom == item.Faction2 && !d.ShouldBeCancelled();
			});
			if (unresolvedTradeAgreementDecision != null)
			{
				TextObject hintText5;
				this.Actions.Add(new KingdomDiplomacyProposalActionItemVM(GameTexts.FindText("str_resolve", null), GameTexts.FindText("str_resolve_explanation", null), 0, this.GetAreProposalActionsEnabledWithReason(0f, out hintText5), hintText5, delegate()
				{
					this._forceDecision(unresolvedTradeAgreementDecision);
				}));
				return;
			}
			if (!Campaign.Current.GetCampaignBehavior<ITradeAgreementsCampaignBehavior>().HasTradeAgreement(item.Faction1 as Kingdom, item.Faction2 as Kingdom))
			{
				int influenceCostOfProposingTradeAgreement = Campaign.Current.Models.TradeAgreementModel.GetInfluenceCostOfProposingTradeAgreement(Clan.PlayerClan);
				TextObject hintText6;
				this.Actions.Add(new KingdomDiplomacyProposalActionItemVM((this._playerKingdom.Clans.Count > 1) ? GameTexts.FindText("str_policy_propose", null) : GameTexts.FindText("str_policy_enact", null), GameTexts.FindText("str_propose_trade_agreement_explanation", null).SetTextVariable("SUPPORT", this.CalculateTradeAgreementSupport(item.Faction2)), influenceCostOfProposingTradeAgreement, this.GetIsProposingTradeAgreementEnabledWithReason(item, (float)influenceCostOfProposingTradeAgreement, out hintText6), hintText6, delegate()
				{
					this.OnStartTradeAgreement(item);
				}));
			}
		}

		// Token: 0x060008F3 RID: 2291 RVA: 0x000285CC File Offset: 0x000267CC
		private bool GetIsProposingWarEnabledWithReason(KingdomTruceItemVM item, float actionInfluenceCost, out TextObject disabledReason)
		{
			TextObject textObject;
			if (!this.GetAreProposalActionsEnabledWithReason(actionInfluenceCost, out textObject))
			{
				disabledReason = textObject;
				return false;
			}
			TextObject textObject2;
			if (!Campaign.Current.Models.KingdomDecisionPermissionModel.IsWarDecisionAllowedBetweenKingdoms(item.Faction1 as Kingdom, item.Faction2 as Kingdom, out textObject2))
			{
				disabledReason = textObject2;
				return false;
			}
			disabledReason = TextObject.GetEmpty();
			return true;
		}

		// Token: 0x060008F4 RID: 2292 RVA: 0x00028624 File Offset: 0x00026824
		private bool GetIsProposingPeaceEnabledWithReason(KingdomWarItemVM item, float actionInfluenceCost, out TextObject disabledReason)
		{
			TextObject textObject;
			if (!this.GetAreProposalActionsEnabledWithReason(actionInfluenceCost, out textObject))
			{
				disabledReason = textObject;
				return false;
			}
			TextObject textObject2;
			if (!Campaign.Current.Models.KingdomDecisionPermissionModel.IsPeaceDecisionAllowedBetweenKingdoms(item.Faction1 as Kingdom, item.Faction2 as Kingdom, out textObject2))
			{
				disabledReason = textObject2;
				return false;
			}
			disabledReason = TextObject.GetEmpty();
			return true;
		}

		// Token: 0x060008F5 RID: 2293 RVA: 0x0002867C File Offset: 0x0002687C
		private bool GetIsProposingAllianceEnabledWithReason(KingdomTruceItemVM item, float actionInfluenceCost, out TextObject disabledReason)
		{
			TextObject textObject;
			if (!this.GetAreProposalActionsEnabledWithReason(actionInfluenceCost, out textObject))
			{
				disabledReason = textObject;
				return false;
			}
			TextObject textObject2;
			if (!new StartAllianceDecision(Clan.PlayerClan, item.Faction2 as Kingdom).CanMakeDecision(out textObject2, true))
			{
				disabledReason = textObject2;
				return false;
			}
			disabledReason = TextObject.GetEmpty();
			return true;
		}

		// Token: 0x060008F6 RID: 2294 RVA: 0x000286C8 File Offset: 0x000268C8
		private bool GetIsProposingTradeAgreementEnabledWithReason(KingdomTruceItemVM item, float actionInfluenceCost, out TextObject disabledReason)
		{
			TextObject textObject;
			if (!this.GetAreProposalActionsEnabledWithReason(actionInfluenceCost, out textObject))
			{
				disabledReason = textObject;
				return false;
			}
			TextObject textObject2;
			if (!new TradeAgreementDecision(Clan.PlayerClan, item.Faction2 as Kingdom).CanMakeDecision(out textObject2, true))
			{
				disabledReason = textObject2;
				return false;
			}
			disabledReason = TextObject.GetEmpty();
			return true;
		}

		// Token: 0x060008F7 RID: 2295 RVA: 0x00028714 File Offset: 0x00026914
		private bool GetAreProposalActionsEnabledWithReason(float actionInfluenceCost, out TextObject disabledReason)
		{
			TextObject textObject;
			if (!CampaignUIHelper.GetMapScreenActionIsEnabledWithReason(out textObject))
			{
				disabledReason = textObject;
				return false;
			}
			if (actionInfluenceCost > 0f && Clan.PlayerClan.Influence < actionInfluenceCost)
			{
				disabledReason = GameTexts.FindText("str_warning_you_dont_have_enough_influence", null);
				return false;
			}
			if (Clan.PlayerClan.IsUnderMercenaryService)
			{
				disabledReason = GameTexts.FindText("str_cannot_propose_war_truce_while_mercenary", null);
				return false;
			}
			disabledReason = TextObject.GetEmpty();
			return true;
		}

		// Token: 0x060008F8 RID: 2296 RVA: 0x00028776 File Offset: 0x00026976
		private void RefreshCurrentWarVisuals(KingdomDiplomacyItemVM item)
		{
			if (item != null)
			{
				if (this.CurrentSelectedDiplomacyItem != null)
				{
					this.CurrentSelectedDiplomacyItem.IsSelected = false;
				}
				this.CurrentSelectedDiplomacyItem = item;
				if (this.CurrentSelectedDiplomacyItem != null)
				{
					this.CurrentSelectedDiplomacyItem.IsSelected = true;
				}
			}
		}

		// Token: 0x060008F9 RID: 2297 RVA: 0x000287AA File Offset: 0x000269AA
		private void OnDiplomacyItemSelection(KingdomDiplomacyItemVM item)
		{
			if (this.CurrentSelectedDiplomacyItem != item)
			{
				if (this.CurrentSelectedDiplomacyItem != null)
				{
					this.CurrentSelectedDiplomacyItem.IsSelected = false;
				}
				this.CurrentSelectedDiplomacyItem = item;
				base.IsAcceptableItemSelected = item != null;
				this.OnSetCurrentDiplomacyItem(item);
			}
		}

		// Token: 0x060008FA RID: 2298 RVA: 0x000287E4 File Offset: 0x000269E4
		private void OnDeclareWar(KingdomTruceItemVM item)
		{
			DeclareWarDecision declareWarDecision = new DeclareWarDecision(Clan.PlayerClan, item.Faction2);
			Clan.PlayerClan.Kingdom.AddDecision(declareWarDecision, false);
			this._forceDecision(declareWarDecision);
		}

		// Token: 0x060008FB RID: 2299 RVA: 0x00028820 File Offset: 0x00026A20
		private void OnDeclarePeace(KingdomWarItemVM item, int tributeToPay, int tributeDurationInDays)
		{
			MakePeaceKingdomDecision makePeaceKingdomDecision = new MakePeaceKingdomDecision(Clan.PlayerClan, item.Faction2 as Kingdom, tributeToPay, tributeDurationInDays, true, false);
			Clan.PlayerClan.Kingdom.AddDecision(makePeaceKingdomDecision, false);
			this._forceDecision(makePeaceKingdomDecision);
		}

		// Token: 0x060008FC RID: 2300 RVA: 0x00028864 File Offset: 0x00026A64
		private void OnStartAlliance(KingdomTruceItemVM item)
		{
			if (item.Faction2.IsKingdomFaction)
			{
				StartAllianceDecision startAllianceDecision = new StartAllianceDecision(Clan.PlayerClan, (Kingdom)item.Faction2);
				Clan.PlayerClan.Kingdom.AddDecision(startAllianceDecision, false);
				this._forceDecision(startAllianceDecision);
			}
		}

		// Token: 0x060008FD RID: 2301 RVA: 0x000288B4 File Offset: 0x00026AB4
		private void OnStartTradeAgreement(KingdomTruceItemVM item)
		{
			if (item.Faction2.IsKingdomFaction)
			{
				TradeAgreementDecision tradeAgreementDecision = new TradeAgreementDecision(Clan.PlayerClan, (Kingdom)item.Faction2);
				Clan.PlayerClan.Kingdom.AddDecision(tradeAgreementDecision, false);
				this._forceDecision(tradeAgreementDecision);
			}
		}

		// Token: 0x060008FE RID: 2302 RVA: 0x00028901 File Offset: 0x00026B01
		private void ExecuteShowWarLogs()
		{
			this.IsDisplayingWarLogs = true;
			this.IsDisplayingStatComparisons = false;
		}

		// Token: 0x060008FF RID: 2303 RVA: 0x00028911 File Offset: 0x00026B11
		private void ExecuteShowStatComparisons()
		{
			this.IsDisplayingWarLogs = false;
			this.IsDisplayingStatComparisons = true;
		}

		// Token: 0x06000900 RID: 2304 RVA: 0x00028924 File Offset: 0x00026B24
		private void SetDefaultSelectedItem()
		{
			KingdomDiplomacyItemVM kingdomDiplomacyItemVM = this.PlayerWars.FirstOrDefault<KingdomWarItemVM>();
			KingdomDiplomacyItemVM kingdomDiplomacyItemVM2 = this.PlayerTruces.FirstOrDefault<KingdomTruceItemVM>();
			this.OnDiplomacyItemSelection(kingdomDiplomacyItemVM ?? kingdomDiplomacyItemVM2);
		}

		// Token: 0x06000901 RID: 2305 RVA: 0x00028958 File Offset: 0x00026B58
		private void UpdateBehaviorSelection()
		{
			if (Hero.MainHero.MapFaction.IsKingdomFaction && Hero.MainHero.MapFaction.Leader == Hero.MainHero && this.CurrentSelectedDiplomacyItem != null)
			{
				StanceLink stanceWith = Hero.MainHero.MapFaction.GetStanceWith(this.CurrentSelectedDiplomacyItem.Faction2);
				this.BehaviorSelection.SelectedIndex = stanceWith.BehaviorPriority;
			}
		}

		// Token: 0x06000902 RID: 2306 RVA: 0x000289C0 File Offset: 0x00026BC0
		private void OnBehaviorSelectionChanged(SelectorVM<SelectorItemVM> s)
		{
			if (!this._isChangingDiplomacyItem && Hero.MainHero.MapFaction.IsKingdomFaction && Hero.MainHero.MapFaction.Leader == Hero.MainHero && this.CurrentSelectedDiplomacyItem != null)
			{
				Hero.MainHero.MapFaction.GetStanceWith(this.CurrentSelectedDiplomacyItem.Faction2).BehaviorPriority = s.SelectedIndex;
			}
		}

		// Token: 0x06000903 RID: 2307 RVA: 0x00028A29 File Offset: 0x00026C29
		private int CalculateWarSupport(IFaction faction)
		{
			return MathF.Round(new KingdomElection(new DeclareWarDecision(Clan.PlayerClan, faction)).GetLikelihoodForSponsor(Clan.PlayerClan) * 100f);
		}

		// Token: 0x06000904 RID: 2308 RVA: 0x00028A50 File Offset: 0x00026C50
		private int CalculateAllianceSupport(IFaction faction)
		{
			return MathF.Round(new KingdomElection(new StartAllianceDecision(Clan.PlayerClan, faction as Kingdom)).GetLikelihoodForSponsor(Clan.PlayerClan) * 100f);
		}

		// Token: 0x06000905 RID: 2309 RVA: 0x00028A7C File Offset: 0x00026C7C
		private int CalculatePeaceSupport(IFaction faction, int dailyTributeToBePaid, int durationInDays)
		{
			return MathF.Round(new KingdomElection(new MakePeaceKingdomDecision(Clan.PlayerClan, faction, dailyTributeToBePaid, durationInDays, true, false)).GetLikelihoodForSponsor(Clan.PlayerClan) * 100f);
		}

		// Token: 0x06000906 RID: 2310 RVA: 0x00028AA7 File Offset: 0x00026CA7
		private int CalculateTradeAgreementSupport(IFaction faction)
		{
			return MathF.Round(new KingdomElection(new TradeAgreementDecision(Clan.PlayerClan, faction as Kingdom)).GetLikelihoodForSponsor(Clan.PlayerClan) * 100f);
		}

		// Token: 0x170002A0 RID: 672
		// (get) Token: 0x06000907 RID: 2311 RVA: 0x00028AD3 File Offset: 0x00026CD3
		// (set) Token: 0x06000908 RID: 2312 RVA: 0x00028ADB File Offset: 0x00026CDB
		[DataSourceProperty]
		public MBBindingList<KingdomWarItemVM> PlayerWars
		{
			get
			{
				return this._playerWars;
			}
			set
			{
				if (value != this._playerWars)
				{
					this._playerWars = value;
					base.OnPropertyChangedWithValue<MBBindingList<KingdomWarItemVM>>(value, "PlayerWars");
				}
			}
		}

		// Token: 0x170002A1 RID: 673
		// (get) Token: 0x06000909 RID: 2313 RVA: 0x00028AF9 File Offset: 0x00026CF9
		// (set) Token: 0x0600090A RID: 2314 RVA: 0x00028B01 File Offset: 0x00026D01
		[DataSourceProperty]
		public bool IsDisplayingWarLogs
		{
			get
			{
				return this._isDisplayingWarLogs;
			}
			set
			{
				if (value != this._isDisplayingWarLogs)
				{
					this._isDisplayingWarLogs = value;
					base.OnPropertyChangedWithValue(value, "IsDisplayingWarLogs");
				}
			}
		}

		// Token: 0x170002A2 RID: 674
		// (get) Token: 0x0600090B RID: 2315 RVA: 0x00028B1F File Offset: 0x00026D1F
		// (set) Token: 0x0600090C RID: 2316 RVA: 0x00028B27 File Offset: 0x00026D27
		[DataSourceProperty]
		public bool IsDisplayingStatComparisons
		{
			get
			{
				return this._isDisplayingStatComparisons;
			}
			set
			{
				if (value != this._isDisplayingStatComparisons)
				{
					this._isDisplayingStatComparisons = value;
					base.OnPropertyChangedWithValue(value, "IsDisplayingStatComparisons");
				}
			}
		}

		// Token: 0x170002A3 RID: 675
		// (get) Token: 0x0600090D RID: 2317 RVA: 0x00028B45 File Offset: 0x00026D45
		// (set) Token: 0x0600090E RID: 2318 RVA: 0x00028B4D File Offset: 0x00026D4D
		[DataSourceProperty]
		public bool IsWar
		{
			get
			{
				return this._isWar;
			}
			set
			{
				if (value != this._isWar)
				{
					this._isWar = value;
					if (!value)
					{
						this.ExecuteShowStatComparisons();
					}
					base.OnPropertyChangedWithValue(value, "IsWar");
				}
			}
		}

		// Token: 0x170002A4 RID: 676
		// (get) Token: 0x0600090F RID: 2319 RVA: 0x00028B74 File Offset: 0x00026D74
		// (set) Token: 0x06000910 RID: 2320 RVA: 0x00028B7C File Offset: 0x00026D7C
		[DataSourceProperty]
		public string BehaviorSelectionTitle
		{
			get
			{
				return this._behaviorSelectionTitle;
			}
			set
			{
				if (value != this._behaviorSelectionTitle)
				{
					this._behaviorSelectionTitle = value;
					base.OnPropertyChangedWithValue<string>(value, "BehaviorSelectionTitle");
				}
			}
		}

		// Token: 0x170002A5 RID: 677
		// (get) Token: 0x06000911 RID: 2321 RVA: 0x00028B9F File Offset: 0x00026D9F
		// (set) Token: 0x06000912 RID: 2322 RVA: 0x00028BA7 File Offset: 0x00026DA7
		[DataSourceProperty]
		public MBBindingList<KingdomTruceItemVM> PlayerTruces
		{
			get
			{
				return this._playerTruces;
			}
			set
			{
				if (value != this._playerTruces)
				{
					this._playerTruces = value;
					base.OnPropertyChangedWithValue<MBBindingList<KingdomTruceItemVM>>(value, "PlayerTruces");
				}
			}
		}

		// Token: 0x170002A6 RID: 678
		// (get) Token: 0x06000913 RID: 2323 RVA: 0x00028BC5 File Offset: 0x00026DC5
		// (set) Token: 0x06000914 RID: 2324 RVA: 0x00028BCD File Offset: 0x00026DCD
		[DataSourceProperty]
		public KingdomDiplomacyItemVM CurrentSelectedDiplomacyItem
		{
			get
			{
				return this._currentSelectedItem;
			}
			set
			{
				if (value != this._currentSelectedItem)
				{
					this._isChangingDiplomacyItem = true;
					this._currentSelectedItem = value;
					this.IsWar = value is KingdomWarItemVM;
					base.OnPropertyChangedWithValue<KingdomDiplomacyItemVM>(value, "CurrentSelectedDiplomacyItem");
					this._isChangingDiplomacyItem = false;
				}
			}
		}

		// Token: 0x170002A7 RID: 679
		// (get) Token: 0x06000915 RID: 2325 RVA: 0x00028C08 File Offset: 0x00026E08
		// (set) Token: 0x06000916 RID: 2326 RVA: 0x00028C10 File Offset: 0x00026E10
		[DataSourceProperty]
		public KingdomWarSortControllerVM WarsSortController
		{
			get
			{
				return this._warsSortController;
			}
			set
			{
				if (value != this._warsSortController)
				{
					this._warsSortController = value;
					base.OnPropertyChangedWithValue<KingdomWarSortControllerVM>(value, "WarsSortController");
				}
			}
		}

		// Token: 0x170002A8 RID: 680
		// (get) Token: 0x06000917 RID: 2327 RVA: 0x00028C2E File Offset: 0x00026E2E
		// (set) Token: 0x06000918 RID: 2328 RVA: 0x00028C36 File Offset: 0x00026E36
		[DataSourceProperty]
		public string PlayerWarsText
		{
			get
			{
				return this._playerWarsText;
			}
			set
			{
				if (value != this._playerWarsText)
				{
					this._playerWarsText = value;
					base.OnPropertyChangedWithValue<string>(value, "PlayerWarsText");
				}
			}
		}

		// Token: 0x170002A9 RID: 681
		// (get) Token: 0x06000919 RID: 2329 RVA: 0x00028C59 File Offset: 0x00026E59
		// (set) Token: 0x0600091A RID: 2330 RVA: 0x00028C61 File Offset: 0x00026E61
		[DataSourceProperty]
		public string WarsText
		{
			get
			{
				return this._warsText;
			}
			set
			{
				if (value != this._warsText)
				{
					this._warsText = value;
					base.OnPropertyChangedWithValue<string>(value, "WarsText");
				}
			}
		}

		// Token: 0x170002AA RID: 682
		// (get) Token: 0x0600091B RID: 2331 RVA: 0x00028C84 File Offset: 0x00026E84
		// (set) Token: 0x0600091C RID: 2332 RVA: 0x00028C8C File Offset: 0x00026E8C
		[DataSourceProperty]
		public string NumOfPlayerWarsText
		{
			get
			{
				return this._numOfPlayerWarsText;
			}
			set
			{
				if (value != this._numOfPlayerWarsText)
				{
					this._numOfPlayerWarsText = value;
					base.OnPropertyChangedWithValue<string>(value, "NumOfPlayerWarsText");
				}
			}
		}

		// Token: 0x170002AB RID: 683
		// (get) Token: 0x0600091D RID: 2333 RVA: 0x00028CAF File Offset: 0x00026EAF
		// (set) Token: 0x0600091E RID: 2334 RVA: 0x00028CB7 File Offset: 0x00026EB7
		[DataSourceProperty]
		public string PlayerTrucesText
		{
			get
			{
				return this._otherWarsText;
			}
			set
			{
				if (value != this._otherWarsText)
				{
					this._otherWarsText = value;
					base.OnPropertyChangedWithValue<string>(value, "PlayerTrucesText");
				}
			}
		}

		// Token: 0x170002AC RID: 684
		// (get) Token: 0x0600091F RID: 2335 RVA: 0x00028CDA File Offset: 0x00026EDA
		// (set) Token: 0x06000920 RID: 2336 RVA: 0x00028CE2 File Offset: 0x00026EE2
		[DataSourceProperty]
		public string NumOfPlayerTrucesText
		{
			get
			{
				return this._numOfOtherWarsText;
			}
			set
			{
				if (value != this._numOfOtherWarsText)
				{
					this._numOfOtherWarsText = value;
					base.OnPropertyChangedWithValue<string>(value, "NumOfPlayerTrucesText");
				}
			}
		}

		// Token: 0x170002AD RID: 685
		// (get) Token: 0x06000921 RID: 2337 RVA: 0x00028D05 File Offset: 0x00026F05
		// (set) Token: 0x06000922 RID: 2338 RVA: 0x00028D0D File Offset: 0x00026F0D
		[DataSourceProperty]
		public SelectorVM<SelectorItemVM> BehaviorSelection
		{
			get
			{
				return this._behaviorSelection;
			}
			set
			{
				if (value != this._behaviorSelection)
				{
					this._behaviorSelection = value;
					base.OnPropertyChangedWithValue<SelectorVM<SelectorItemVM>>(value, "BehaviorSelection");
				}
			}
		}

		// Token: 0x170002AE RID: 686
		// (get) Token: 0x06000923 RID: 2339 RVA: 0x00028D2B File Offset: 0x00026F2B
		// (set) Token: 0x06000924 RID: 2340 RVA: 0x00028D33 File Offset: 0x00026F33
		[DataSourceProperty]
		public HintViewModel ShowStatBarsHint
		{
			get
			{
				return this._showStatBarsHint;
			}
			set
			{
				if (value != this._showStatBarsHint)
				{
					this._showStatBarsHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "ShowStatBarsHint");
				}
			}
		}

		// Token: 0x170002AF RID: 687
		// (get) Token: 0x06000925 RID: 2341 RVA: 0x00028D51 File Offset: 0x00026F51
		// (set) Token: 0x06000926 RID: 2342 RVA: 0x00028D59 File Offset: 0x00026F59
		[DataSourceProperty]
		public HintViewModel ShowWarLogsHint
		{
			get
			{
				return this._showWarLogsHint;
			}
			set
			{
				if (value != this._showWarLogsHint)
				{
					this._showWarLogsHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "ShowWarLogsHint");
				}
			}
		}

		// Token: 0x170002B0 RID: 688
		// (get) Token: 0x06000927 RID: 2343 RVA: 0x00028D77 File Offset: 0x00026F77
		// (set) Token: 0x06000928 RID: 2344 RVA: 0x00028D7F File Offset: 0x00026F7F
		[DataSourceProperty]
		public MBBindingList<KingdomDiplomacyProposalActionItemVM> Actions
		{
			get
			{
				return this._actions;
			}
			set
			{
				if (value != this._actions)
				{
					this._actions = value;
					base.OnPropertyChangedWithValue<MBBindingList<KingdomDiplomacyProposalActionItemVM>>(value, "Actions");
				}
			}
		}

		// Token: 0x040003EC RID: 1004
		private readonly Action<KingdomDecision> _forceDecision;

		// Token: 0x040003ED RID: 1005
		private readonly Kingdom _playerKingdom;

		// Token: 0x040003EE RID: 1006
		private bool _isChangingDiplomacyItem;

		// Token: 0x040003EF RID: 1007
		private MBBindingList<KingdomWarItemVM> _playerWars;

		// Token: 0x040003F0 RID: 1008
		private MBBindingList<KingdomTruceItemVM> _playerTruces;

		// Token: 0x040003F1 RID: 1009
		private KingdomWarSortControllerVM _warsSortController;

		// Token: 0x040003F2 RID: 1010
		private KingdomDiplomacyItemVM _currentSelectedItem;

		// Token: 0x040003F3 RID: 1011
		private SelectorVM<SelectorItemVM> _behaviorSelection;

		// Token: 0x040003F4 RID: 1012
		private HintViewModel _showStatBarsHint;

		// Token: 0x040003F5 RID: 1013
		private HintViewModel _showWarLogsHint;

		// Token: 0x040003F6 RID: 1014
		private string _playerWarsText;

		// Token: 0x040003F7 RID: 1015
		private string _numOfPlayerWarsText;

		// Token: 0x040003F8 RID: 1016
		private string _otherWarsText;

		// Token: 0x040003F9 RID: 1017
		private string _numOfOtherWarsText;

		// Token: 0x040003FA RID: 1018
		private string _warsText;

		// Token: 0x040003FB RID: 1019
		private string _behaviorSelectionTitle;

		// Token: 0x040003FC RID: 1020
		private bool _isDisplayingWarLogs;

		// Token: 0x040003FD RID: 1021
		private bool _isDisplayingStatComparisons;

		// Token: 0x040003FE RID: 1022
		private bool _isWar;

		// Token: 0x040003FF RID: 1023
		private MBBindingList<KingdomDiplomacyProposalActionItemVM> _actions;
	}
}
