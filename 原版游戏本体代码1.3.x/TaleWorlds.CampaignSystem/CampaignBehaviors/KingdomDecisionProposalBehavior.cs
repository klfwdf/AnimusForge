using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x02000412 RID: 1042
	public class KingdomDecisionProposalBehavior : CampaignBehaviorBase
	{
		// Token: 0x17000E1B RID: 3611
		// (get) Token: 0x060040E0 RID: 16608 RVA: 0x0012E325 File Offset: 0x0012C525
		public ITradeAgreementsCampaignBehavior TradeAgreementsCampaignBehavior
		{
			get
			{
				if (this._tradeAgreementsBehavior == null)
				{
					this._tradeAgreementsBehavior = Campaign.Current.GetCampaignBehavior<ITradeAgreementsCampaignBehavior>();
				}
				return this._tradeAgreementsBehavior;
			}
		}

		// Token: 0x060040E1 RID: 16609 RVA: 0x0012E348 File Offset: 0x0012C548
		public override void RegisterEvents()
		{
			CampaignEvents.DailyTickClanEvent.AddNonSerializedListener(this, new Action<Clan>(this.DailyTickClan));
			CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, new Action(this.HourlyTick));
			CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, new Action(this.DailyTick));
			CampaignEvents.MakePeace.AddNonSerializedListener(this, new Action<IFaction, IFaction, MakePeaceAction.MakePeaceDetail>(this.OnPeaceMade));
			CampaignEvents.WarDeclared.AddNonSerializedListener(this, new Action<IFaction, IFaction, DeclareWarAction.DeclareWarDetail>(this.OnWarDeclared));
			CampaignEvents.KingdomDestroyedEvent.AddNonSerializedListener(this, new Action<Kingdom>(this.OnKingdomDestroyed));
			CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, new Action<Clan, Kingdom, Kingdom, ChangeKingdomAction.ChangeKingdomActionDetail, bool>(this.OnClanChangedKingdom));
			CampaignEvents.KingdomDecisionAdded.AddNonSerializedListener(this, new Action<KingdomDecision, bool>(this.OnKingdomDecisionAdded));
		}

		// Token: 0x060040E2 RID: 16610 RVA: 0x0012E40D File Offset: 0x0012C60D
		private void OnKingdomDestroyed(Kingdom kingdom)
		{
			this.UpdateKingdomDecisions(kingdom);
		}

		// Token: 0x060040E3 RID: 16611 RVA: 0x0012E418 File Offset: 0x0012C618
		private void DailyTickClan(Clan clan)
		{
			if ((float)((int)Campaign.Current.Models.CampaignTimeModel.CampaignStartTime.ElapsedDaysUntilNow) < 5f)
			{
				return;
			}
			if (clan.IsEliminated)
			{
				return;
			}
			if (clan == Clan.PlayerClan || clan.CurrentTotalStrength <= 0f)
			{
				return;
			}
			if (clan.IsBanditFaction)
			{
				return;
			}
			if (clan.Kingdom == null)
			{
				return;
			}
			if (clan.Influence < 100f)
			{
				return;
			}
			KingdomDecision kingdomDecision = null;
			float randomFloat = MBRandom.RandomFloat;
			int num = ((Kingdom)clan.MapFaction).Clans.Count((Clan x) => x.Influence > 100f);
			float num2 = MathF.Min(0.33f, 1f / ((float)num + 2f));
			num2 *= ((clan.Kingdom == Hero.MainHero.MapFaction && !Hero.MainHero.Clan.IsUnderMercenaryService) ? ((clan.Kingdom.Leader == Hero.MainHero) ? 0.5f : 0.75f) : 1f);
			DiplomacyModel diplomacyModel = Campaign.Current.Models.DiplomacyModel;
			AllianceModel allianceModel = Campaign.Current.Models.AllianceModel;
			if (randomFloat < num2 && clan.Influence > (float)diplomacyModel.GetInfluenceCostOfProposingPeace(clan))
			{
				kingdomDecision = KingdomDecisionProposalBehavior.GetRandomPeaceDecision(clan);
			}
			else if (randomFloat < num2 * 2f && clan.Influence > (float)diplomacyModel.GetInfluenceCostOfProposingWar(clan))
			{
				kingdomDecision = this.GetRandomWarDecision(clan);
			}
			else if (randomFloat < num2 * 2.5f)
			{
				kingdomDecision = ((MBRandom.RandomFloat < 0.5f) ? this.GetRandomTradeAgreementDecision(clan) : this.GetRandomStartingAllianceDecision(clan));
			}
			else if (randomFloat < num2 * 2.75f && clan.Influence > (float)(diplomacyModel.GetInfluenceCostOfPolicyProposalAndDisavowal(clan) * 4))
			{
				kingdomDecision = this.GetRandomPolicyDecision(clan);
			}
			else if (randomFloat < num2 * 3f && clan.Influence > 700f)
			{
				kingdomDecision = this.GetRandomAnnexationDecision(clan);
			}
			if (kingdomDecision != null)
			{
				bool flag = false;
				if (kingdomDecision is MakePeaceKingdomDecision && ((MakePeaceKingdomDecision)kingdomDecision).FactionToMakePeaceWith == Hero.MainHero.MapFaction)
				{
					foreach (KingdomDecision kingdomDecision2 in this._kingdomDecisionsList)
					{
						if (kingdomDecision2 is MakePeaceKingdomDecision && kingdomDecision2.Kingdom == Hero.MainHero.MapFaction && ((MakePeaceKingdomDecision)kingdomDecision2).FactionToMakePeaceWith == clan.Kingdom && kingdomDecision2.TriggerTime.IsFuture)
						{
							flag = true;
							break;
						}
						if (kingdomDecision2 is MakePeaceKingdomDecision && kingdomDecision2.Kingdom == clan.Kingdom && ((MakePeaceKingdomDecision)kingdomDecision2).FactionToMakePeaceWith == Hero.MainHero.MapFaction && kingdomDecision2.TriggerTime.IsFuture)
						{
							flag = true;
							break;
						}
					}
				}
				if (!flag)
				{
					bool flag2 = false;
					foreach (KingdomDecision kingdomDecision3 in this._kingdomDecisionsList)
					{
						DeclareWarDecision declareWarDecision;
						DeclareWarDecision declareWarDecision2;
						if ((declareWarDecision = kingdomDecision3 as DeclareWarDecision) != null && (declareWarDecision2 = kingdomDecision as DeclareWarDecision) != null && declareWarDecision.FactionToDeclareWarOn == declareWarDecision2.FactionToDeclareWarOn && declareWarDecision.ProposerClan.MapFaction == declareWarDecision2.ProposerClan.MapFaction)
						{
							flag2 = true;
							break;
						}
						MakePeaceKingdomDecision makePeaceKingdomDecision;
						MakePeaceKingdomDecision makePeaceKingdomDecision2;
						if ((makePeaceKingdomDecision = kingdomDecision3 as MakePeaceKingdomDecision) != null && (makePeaceKingdomDecision2 = kingdomDecision as MakePeaceKingdomDecision) != null && makePeaceKingdomDecision.FactionToMakePeaceWith == makePeaceKingdomDecision2.FactionToMakePeaceWith && makePeaceKingdomDecision.ProposerClan.MapFaction == makePeaceKingdomDecision2.ProposerClan.MapFaction)
						{
							flag2 = true;
							break;
						}
					}
					if (!flag2)
					{
						clan.Kingdom.AddDecision(kingdomDecision, false);
						return;
					}
				}
			}
			else
			{
				this.UpdateKingdomDecisions(clan.Kingdom);
			}
		}

		// Token: 0x060040E4 RID: 16612 RVA: 0x0012E804 File Offset: 0x0012CA04
		private void HourlyTick()
		{
			if (Clan.PlayerClan.Kingdom != null)
			{
				this.UpdateKingdomDecisions(Clan.PlayerClan.Kingdom);
			}
		}

		// Token: 0x060040E5 RID: 16613 RVA: 0x0012E824 File Offset: 0x0012CA24
		private void DailyTick()
		{
			for (int i = this._kingdomDecisionsList.Count - 1; i >= 0; i--)
			{
				if (this._kingdomDecisionsList[i].TriggerTime.ElapsedDaysUntilNow > 5f)
				{
					this._kingdomDecisionsList.RemoveAt(i);
				}
			}
		}

		// Token: 0x060040E6 RID: 16614 RVA: 0x0012E878 File Offset: 0x0012CA78
		public void UpdateKingdomDecisions(Kingdom kingdom)
		{
			List<KingdomDecision> list = new List<KingdomDecision>();
			List<KingdomDecision> list2 = new List<KingdomDecision>();
			foreach (KingdomDecision kingdomDecision in kingdom.UnresolvedDecisions)
			{
				if (kingdomDecision.ShouldBeCancelled())
				{
					list.Add(kingdomDecision);
				}
				else if (!kingdomDecision.IsPlayerParticipant || (kingdomDecision.TriggerTime.IsPast && !kingdomDecision.NeedsPlayerResolution))
				{
					list2.Add(kingdomDecision);
				}
			}
			foreach (KingdomDecision kingdomDecision2 in list)
			{
				kingdom.RemoveDecision(kingdomDecision2);
				bool flag;
				if (!kingdomDecision2.DetermineChooser().Leader.IsHumanPlayerCharacter)
				{
					flag = kingdomDecision2.DetermineSupporters().Any((Supporter x) => x.IsPlayer);
				}
				else
				{
					flag = true;
				}
				bool isPlayerInvolved = flag;
				CampaignEventDispatcher.Instance.OnKingdomDecisionCancelled(kingdomDecision2, isPlayerInvolved);
			}
			foreach (KingdomDecision decision in list2)
			{
				new KingdomElection(decision).StartElectionWithoutPlayer();
			}
		}

		// Token: 0x060040E7 RID: 16615 RVA: 0x0012E9D8 File Offset: 0x0012CBD8
		private void OnPeaceMade(IFaction side1Faction, IFaction side2Faction, MakePeaceAction.MakePeaceDetail detail)
		{
			this.HandleDiplomaticChangeBetweenFactions(side1Faction, side2Faction);
		}

		// Token: 0x060040E8 RID: 16616 RVA: 0x0012E9E2 File Offset: 0x0012CBE2
		private void OnWarDeclared(IFaction side1Faction, IFaction side2Faction, DeclareWarAction.DeclareWarDetail detail)
		{
			this.HandleDiplomaticChangeBetweenFactions(side1Faction, side2Faction);
		}

		// Token: 0x060040E9 RID: 16617 RVA: 0x0012E9EC File Offset: 0x0012CBEC
		private void HandleDiplomaticChangeBetweenFactions(IFaction side1Faction, IFaction side2Faction)
		{
			if (side1Faction.IsKingdomFaction && side2Faction.IsKingdomFaction)
			{
				this.UpdateKingdomDecisions((Kingdom)side1Faction);
				this.UpdateKingdomDecisions((Kingdom)side2Faction);
			}
		}

		// Token: 0x060040EA RID: 16618 RVA: 0x0012EA18 File Offset: 0x0012CC18
		private KingdomDecision GetRandomStartingAllianceDecision(Clan clan)
		{
			Kingdom kingdom = clan.Kingdom;
			KingdomDecision kingdomDecision = null;
			if (kingdom.UnresolvedDecisions.AnyQ((KingdomDecision x) => x is StartAllianceDecision) || clan.Influence < (float)Campaign.Current.Models.AllianceModel.GetInfluenceCostOfProposingStartingAlliance(clan))
			{
				return null;
			}
			Kingdom randomElementWithPredicate = Kingdom.All.GetRandomElementWithPredicate((Kingdom x) => !x.IsEliminated && x != kingdom);
			if (randomElementWithPredicate != null)
			{
				kingdomDecision = new StartAllianceDecision(clan, randomElementWithPredicate);
				TextObject textObject;
				if (!kingdomDecision.CanMakeDecision(out textObject, false))
				{
					kingdomDecision = null;
				}
			}
			return kingdomDecision;
		}

		// Token: 0x060040EB RID: 16619 RVA: 0x0012EABC File Offset: 0x0012CCBC
		private KingdomDecision GetRandomWarDecision(Clan clan)
		{
			KingdomDecision result = null;
			Kingdom kingdom = clan.Kingdom;
			if (kingdom.UnresolvedDecisions.FirstOrDefault((KingdomDecision x) => x is DeclareWarDecision) != null)
			{
				return null;
			}
			Kingdom randomElementWithPredicate = Kingdom.All.GetRandomElementWithPredicate((Kingdom x) => !x.IsEliminated && x != kingdom && !x.IsAtWarWith(kingdom) && x.GetStanceWith(kingdom).PeaceDeclarationDate.ElapsedDaysUntilNow > 20f);
			if (randomElementWithPredicate != null)
			{
				if ((float)new DeclareWarBarterable(kingdom, randomElementWithPredicate).GetValueForFaction(clan) < Campaign.Current.Models.DiplomacyModel.GetDecisionMakingThreshold(randomElementWithPredicate))
				{
					return null;
				}
				if (this.ConsiderWar(clan, kingdom, randomElementWithPredicate))
				{
					result = new DeclareWarDecision(clan, randomElementWithPredicate);
				}
			}
			return result;
		}

		// Token: 0x060040EC RID: 16620 RVA: 0x0012EB70 File Offset: 0x0012CD70
		private static KingdomDecision GetRandomPeaceDecision(Clan clan)
		{
			KingdomDecision result = null;
			Kingdom kingdom = clan.Kingdom;
			if (kingdom.UnresolvedDecisions.FirstOrDefault((KingdomDecision x) => x is MakePeaceKingdomDecision) != null)
			{
				return null;
			}
			IAllianceCampaignBehavior allianceCampaignBehavior = Campaign.Current.GetCampaignBehavior<IAllianceCampaignBehavior>();
			Kingdom randomElementWithPredicate = Kingdom.All.GetRandomElementWithPredicate(delegate(Kingdom x)
			{
				if (x.IsAtWarWith(kingdom) && !x.IsAtConstantWarWith(kingdom))
				{
					IAllianceCampaignBehavior allianceCampaignBehavior = allianceCampaignBehavior;
					if (allianceCampaignBehavior == null || !allianceCampaignBehavior.IsAtWarByCallToWarAgreement(kingdom, x))
					{
						IAllianceCampaignBehavior allianceCampaignBehavior2 = allianceCampaignBehavior;
						return allianceCampaignBehavior2 == null || !allianceCampaignBehavior2.IsAtWarByCallToWarAgreement(x, kingdom);
					}
				}
				return false;
			});
			MakePeaceKingdomDecision makePeaceKingdomDecision;
			if (randomElementWithPredicate != null && KingdomDecisionProposalBehavior.ConsiderPeace(clan, randomElementWithPredicate.RulingClan, randomElementWithPredicate, out makePeaceKingdomDecision))
			{
				result = makePeaceKingdomDecision;
			}
			return result;
		}

		// Token: 0x060040ED RID: 16621 RVA: 0x0012EC04 File Offset: 0x0012CE04
		private bool ConsiderWar(Clan clan, Kingdom kingdom, IFaction otherFaction)
		{
			int num = Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfProposingWar(clan) / 2;
			if (clan.Influence < (float)num)
			{
				return false;
			}
			DeclareWarDecision declareWarDecision = new DeclareWarDecision(clan, otherFaction);
			if (declareWarDecision.CalculateSupport(clan) > 50f)
			{
				KingdomElection kingdomElection = new KingdomElection(declareWarDecision);
				float num2 = 0f;
				using (List<DecisionOutcome>.Enumerator enumerator = kingdomElection.PossibleOutcomes.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						DeclareWarDecision.DeclareWarDecisionOutcome declareWarDecisionOutcome;
						if ((declareWarDecisionOutcome = enumerator.Current as DeclareWarDecision.DeclareWarDecisionOutcome) != null && declareWarDecisionOutcome.ShouldWarBeDeclared)
						{
							num2 = declareWarDecisionOutcome.Likelihood;
							break;
						}
					}
				}
				if (MBRandom.RandomFloat < 1.4f * num2 - 0.55f)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x060040EE RID: 16622 RVA: 0x0012ECC8 File Offset: 0x0012CEC8
		private float GetKingdomSupportForWar(Clan clan, Kingdom kingdom, IFaction otherFaction)
		{
			return new KingdomElection(new DeclareWarDecision(clan, otherFaction)).GetLikelihoodForSponsor(clan);
		}

		// Token: 0x060040EF RID: 16623 RVA: 0x0012ECDC File Offset: 0x0012CEDC
		private static bool ConsiderPeace(Clan clan, Clan otherClan, IFaction otherFaction, out MakePeaceKingdomDecision decision)
		{
			if (!Campaign.Current.Models.DiplomacyModel.IsPeaceSuitable(clan.MapFaction, otherFaction))
			{
				decision = null;
				return false;
			}
			if (Campaign.Current.Models.DiplomacyModel.GetScoreOfDeclaringPeace(clan.MapFaction, otherFaction) < Campaign.Current.Models.DiplomacyModel.GetDecisionMakingThreshold(clan.Kingdom))
			{
				decision = null;
				return false;
			}
			int dailyTributeDurationInDays;
			int dailyTributeToPay = Campaign.Current.Models.DiplomacyModel.GetDailyTributeToPay(clan, otherClan, out dailyTributeDurationInDays);
			if (dailyTributeToPay < 0)
			{
				decision = null;
				return false;
			}
			MakePeaceKingdomDecision makePeaceKingdomDecision = new MakePeaceKingdomDecision(clan, otherFaction, dailyTributeToPay, dailyTributeDurationInDays, true, false);
			DecisionOutcome possibleOutcome = makePeaceKingdomDecision.DetermineInitialCandidates().First(delegate(DecisionOutcome x)
			{
				MakePeaceKingdomDecision.MakePeaceDecisionOutcome makePeaceDecisionOutcome;
				return (makePeaceDecisionOutcome = x as MakePeaceKingdomDecision.MakePeaceDecisionOutcome) != null && makePeaceDecisionOutcome.ShouldPeaceBeDeclared;
			});
			if (makePeaceKingdomDecision.DetermineSupport(clan, possibleOutcome) <= 0f)
			{
				decision = null;
				return false;
			}
			decision = makePeaceKingdomDecision;
			return true;
		}

		// Token: 0x060040F0 RID: 16624 RVA: 0x0012EDB8 File Offset: 0x0012CFB8
		private KingdomDecision GetRandomPolicyDecision(Clan clan)
		{
			KingdomDecision result = null;
			Kingdom kingdom = clan.Kingdom;
			if (kingdom.UnresolvedDecisions.FirstOrDefault((KingdomDecision x) => x is KingdomPolicyDecision) != null)
			{
				return null;
			}
			if (clan.Influence < 200f)
			{
				return null;
			}
			PolicyObject randomElement = PolicyObject.All.GetRandomElement<PolicyObject>();
			bool flag = kingdom.ActivePolicies.Contains(randomElement);
			if (this.ConsiderPolicy(clan, kingdom, randomElement, flag))
			{
				result = new KingdomPolicyDecision(clan, randomElement, flag);
			}
			return result;
		}

		// Token: 0x060040F1 RID: 16625 RVA: 0x0012EE3C File Offset: 0x0012D03C
		private bool ConsiderPolicy(Clan clan, Kingdom kingdom, PolicyObject policy, bool invert)
		{
			int influenceCostOfPolicyProposalAndDisavowal = Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfPolicyProposalAndDisavowal(clan);
			if (clan.Influence < (float)influenceCostOfPolicyProposalAndDisavowal)
			{
				return false;
			}
			KingdomPolicyDecision kingdomPolicyDecision = new KingdomPolicyDecision(clan, policy, invert);
			if (kingdomPolicyDecision.CalculateSupport(clan) > 50f)
			{
				KingdomElection kingdomElection = new KingdomElection(kingdomPolicyDecision);
				float num = 0f;
				using (List<DecisionOutcome>.Enumerator enumerator = kingdomElection.PossibleOutcomes.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						KingdomPolicyDecision.PolicyDecisionOutcome policyDecisionOutcome;
						if ((policyDecisionOutcome = enumerator.Current as KingdomPolicyDecision.PolicyDecisionOutcome) != null && policyDecisionOutcome.ShouldDecisionBeEnforced)
						{
							num = policyDecisionOutcome.Likelihood;
							break;
						}
					}
				}
				if ((double)MBRandom.RandomFloat < (double)num - 0.55)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x060040F2 RID: 16626 RVA: 0x0012EF00 File Offset: 0x0012D100
		private float GetKingdomSupportForPolicy(Clan clan, Kingdom kingdom, PolicyObject policy, bool invert)
		{
			Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfPolicyProposalAndDisavowal(clan);
			return new KingdomElection(new KingdomPolicyDecision(clan, policy, invert)).GetLikelihoodForSponsor(clan);
		}

		// Token: 0x060040F3 RID: 16627 RVA: 0x0012EF2C File Offset: 0x0012D12C
		private KingdomDecision GetRandomAnnexationDecision(Clan clan)
		{
			KingdomDecision result = null;
			Kingdom kingdom = clan.Kingdom;
			if (kingdom.UnresolvedDecisions.FirstOrDefault((KingdomDecision x) => x is KingdomPolicyDecision) != null)
			{
				return null;
			}
			if (clan.Influence < 300f)
			{
				return null;
			}
			Clan randomElement = kingdom.Clans.GetRandomElement<Clan>();
			if (randomElement != null && randomElement != clan && randomElement.GetRelationWithClan(clan) < -25)
			{
				if (randomElement.Fiefs.Count == 0)
				{
					return null;
				}
				Town randomElement2 = randomElement.Fiefs.GetRandomElement<Town>();
				if (this.ConsiderAnnex(clan, randomElement2))
				{
					result = new SettlementClaimantPreliminaryDecision(clan, randomElement2.Settlement);
				}
			}
			return result;
		}

		// Token: 0x060040F4 RID: 16628 RVA: 0x0012EFD0 File Offset: 0x0012D1D0
		private bool ConsiderAnnex(Clan clan, Town targetSettlement)
		{
			int influenceCostOfAnnexation = Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfAnnexation(clan);
			if (clan.Influence < (float)influenceCostOfAnnexation)
			{
				return false;
			}
			SettlementClaimantPreliminaryDecision settlementClaimantPreliminaryDecision = new SettlementClaimantPreliminaryDecision(clan, targetSettlement.Settlement);
			if (settlementClaimantPreliminaryDecision.CalculateSupport(clan) > 50f)
			{
				float num = 0f;
				using (List<DecisionOutcome>.Enumerator enumerator = new KingdomElection(settlementClaimantPreliminaryDecision).PossibleOutcomes.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						SettlementClaimantPreliminaryDecision.SettlementClaimantPreliminaryOutcome settlementClaimantPreliminaryOutcome;
						if ((settlementClaimantPreliminaryOutcome = enumerator.Current as SettlementClaimantPreliminaryDecision.SettlementClaimantPreliminaryOutcome) != null && settlementClaimantPreliminaryOutcome.ShouldSettlementOwnerChange)
						{
							num = settlementClaimantPreliminaryOutcome.Likelihood;
							break;
						}
					}
				}
				if ((double)MBRandom.RandomFloat < (double)num - 0.6)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x060040F5 RID: 16629 RVA: 0x0012F098 File Offset: 0x0012D298
		private KingdomDecision GetRandomTradeAgreementDecision(Clan clan)
		{
			KingdomDecision result = null;
			Kingdom kingdom = clan.Kingdom;
			if (kingdom.UnresolvedDecisions.FirstOrDefault((KingdomDecision x) => x is TradeAgreementDecision) != null || clan.Influence < (float)Campaign.Current.Models.TradeAgreementModel.GetInfluenceCostOfProposingTradeAgreement(clan))
			{
				return null;
			}
			Kingdom randomElementWithPredicate = Kingdom.All.GetRandomElementWithPredicate((Kingdom x) => x != kingdom);
			if (randomElementWithPredicate != null && this.ConsiderTradeAgreement(clan, kingdom, randomElementWithPredicate))
			{
				result = new TradeAgreementDecision(clan, randomElementWithPredicate);
			}
			return result;
		}

		// Token: 0x060040F6 RID: 16630 RVA: 0x0012F140 File Offset: 0x0012D340
		private bool ConsiderTradeAgreement(Clan clan, Kingdom kingdom, Kingdom otherKingdom)
		{
			TextObject textObject;
			if (Campaign.Current.Models.TradeAgreementModel.CanMakeTradeAgreement(kingdom, otherKingdom, Clan.PlayerClan.Kingdom != otherKingdom, out textObject, false))
			{
				TradeAgreementDecision tradeAgreementDecision = new TradeAgreementDecision(clan, otherKingdom);
				if (tradeAgreementDecision.CalculateSupport(clan) > 50f)
				{
					KingdomElection kingdomElection = new KingdomElection(tradeAgreementDecision);
					float num = 0f;
					using (List<DecisionOutcome>.Enumerator enumerator = kingdomElection.PossibleOutcomes.GetEnumerator())
					{
						while (enumerator.MoveNext())
						{
							TradeAgreementDecision.TradeAgreementDecisionOutcome tradeAgreementDecisionOutcome;
							if ((tradeAgreementDecisionOutcome = enumerator.Current as TradeAgreementDecision.TradeAgreementDecisionOutcome) != null && tradeAgreementDecisionOutcome.ShouldTradeAgreementStart)
							{
								num = tradeAgreementDecisionOutcome.Likelihood;
								break;
							}
						}
					}
					if (MBRandom.RandomFloat < num)
					{
						return true;
					}
				}
			}
			return false;
		}

		// Token: 0x060040F7 RID: 16631 RVA: 0x0012F200 File Offset: 0x0012D400
		public override void SyncData(IDataStore dataStore)
		{
			dataStore.SyncData<List<KingdomDecision>>("_kingdomDecisionsList", ref this._kingdomDecisionsList);
			if (dataStore.IsLoading && MBSaveLoad.LastLoadedGameVersion.IsOlderThan(ApplicationVersion.FromString("v1.3.0", 0)) && this._kingdomDecisionsList == null)
			{
				this._kingdomDecisionsList = new List<KingdomDecision>();
			}
		}

		// Token: 0x060040F8 RID: 16632 RVA: 0x0012F254 File Offset: 0x0012D454
		private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
		{
			if (clan == Clan.PlayerClan && oldKingdom != null && detail != ChangeKingdomAction.ChangeKingdomActionDetail.LeaveByKingdomDestruction)
			{
				this.UpdateKingdomDecisions(oldKingdom);
			}
		}

		// Token: 0x060040F9 RID: 16633 RVA: 0x0012F26D File Offset: 0x0012D46D
		private void OnKingdomDecisionAdded(KingdomDecision decision, bool isPlayerInvolved)
		{
			this._kingdomDecisionsList.Add(decision);
		}

		// Token: 0x040012DA RID: 4826
		private const float DaysBetweenSameProposal = 5f;

		// Token: 0x040012DB RID: 4827
		private List<KingdomDecision> _kingdomDecisionsList = new List<KingdomDecision>();

		// Token: 0x040012DC RID: 4828
		private ITradeAgreementsCampaignBehavior _tradeAgreementsBehavior;

		// Token: 0x0200080E RID: 2062
		// (Invoke) Token: 0x0600656F RID: 25967
		private delegate KingdomDecision KingdomDecisionCreatorDelegate(Clan sponsorClan);
	}
}
