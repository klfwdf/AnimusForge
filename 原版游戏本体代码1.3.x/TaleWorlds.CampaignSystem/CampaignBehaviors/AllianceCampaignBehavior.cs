using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x020003CD RID: 973
	public class AllianceCampaignBehavior : CampaignBehaviorBase, IAllianceCampaignBehavior
	{
		// Token: 0x060039BE RID: 14782 RVA: 0x000EBE80 File Offset: 0x000EA080
		public override void RegisterEvents()
		{
			CampaignEvents.DailyTickClanEvent.AddNonSerializedListener(this, new Action<Clan>(this.DailyTickClan));
			CampaignEvents.WarDeclared.AddNonSerializedListener(this, new Action<IFaction, IFaction, DeclareWarAction.DeclareWarDetail>(this.OnWarDeclared));
			CampaignEvents.MakePeace.AddNonSerializedListener(this, new Action<IFaction, IFaction, MakePeaceAction.MakePeaceDetail>(this.OnMakePeace));
			CampaignEvents.KingdomDestroyedEvent.AddNonSerializedListener(this, new Action<Kingdom>(this.OnKingdomDestroyed));
		}

		// Token: 0x060039BF RID: 14783 RVA: 0x000EBEE9 File Offset: 0x000EA0E9
		public override void SyncData(IDataStore dataStore)
		{
			dataStore.SyncData<List<AllianceCampaignBehavior.Alliance>>("_alliances", ref this._alliances);
			dataStore.SyncData<List<AllianceCampaignBehavior.CallToWarAgreement>>("_callToWarAgreements", ref this._callToWarAgreements);
		}

		// Token: 0x060039C0 RID: 14784 RVA: 0x000EBF10 File Offset: 0x000EA110
		void IAllianceCampaignBehavior.OnAllianceOfferedToPlayer(Kingdom offeringKingdom)
		{
			if (Clan.PlayerClan.Kingdom.Clans.Count == 1)
			{
				object obj = new TextObject("{=ho5EndaV}Decision", null);
				TextObject textObject = new TextObject("{=eAhgrwkZ}As {RULER_NAME_AND_TITLE}, you must decide if an alliance will be formed with the {KINGDOM_NAME}.", null);
				TextObject textObject2 = GameTexts.FindText("str_faction_ruler_name_with_title", Hero.MainHero.MapFaction.Culture.StringId);
				textObject2.SetCharacterProperties("RULER", Hero.MainHero.CharacterObject, false);
				textObject.SetTextVariable("RULER_NAME_AND_TITLE", textObject2);
				textObject.SetTextVariable("KINGDOM_NAME", offeringKingdom.Name);
				TextObject textObject3 = new TextObject("{=Y94H6XnK}Accept", null);
				TextObject textObject4 = new TextObject("{=cOgmdp9e}Decline", null);
				InformationManager.ShowInquiry(new InquiryData(obj.ToString(), textObject.ToString(), true, true, textObject3.ToString(), textObject4.ToString(), delegate()
				{
					this.AcceptStartingAlliance(offeringKingdom);
				}, null, "", 0f, null, null, null), false, false);
				return;
			}
			object obj2 = new TextObject("{=ho5EndaV}Decision", null);
			TextObject textObject5 = new TextObject("{=eTylgLCc}A courier has arrived from the {KINGDOM_NAME}. They offer you an alliance. Your kingdom will vote whether to accept the offer.", null);
			textObject5.SetTextVariable("KINGDOM_NAME", offeringKingdom.Name);
			TextObject textObject6 = new TextObject("{=oHaWR73d}Ok", null);
			InformationManager.ShowInquiry(new InquiryData(obj2.ToString(), textObject5.ToString(), true, false, textObject6.ToString(), textObject6.ToString(), delegate()
			{
				this.ConfirmAllianceOffer(offeringKingdom);
			}, null, "", 0f, null, null, null), false, false);
		}

		// Token: 0x060039C1 RID: 14785 RVA: 0x000EC094 File Offset: 0x000EA294
		void IAllianceCampaignBehavior.OnAllianceOfferedToPlayerKingdom(Kingdom offeringKingdom)
		{
			if (Clan.PlayerClan.Kingdom.Clans.Count == 1)
			{
				TextObject textObject = new TextObject("{=1V8f9vRM}A courier bearing an alliance offer from the {PROPOSER_KINGDOM} has arrived at the court of your realm.", null);
				textObject.SetTextVariable("PROPOSER_KINGDOM", offeringKingdom.InformalName);
				Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new AllianceOfferMapNotification(offeringKingdom, textObject));
				return;
			}
			KingdomDecision kingdomDecision = Clan.PlayerClan.Kingdom.UnresolvedDecisions.FirstOrDefault(delegate(KingdomDecision s)
			{
				StartAllianceDecision startAllianceDecision;
				return (startAllianceDecision = s as StartAllianceDecision) != null && startAllianceDecision.KingdomToStartAllianceWith == offeringKingdom;
			});
			if (kingdomDecision != null)
			{
				Clan.PlayerClan.Kingdom.RemoveDecision(kingdomDecision);
			}
			Clan.PlayerClan.Kingdom.AddDecision(new StartAllianceDecision(StartAllianceDecision.GetProposerClanForPlayerKingdom(offeringKingdom), offeringKingdom), true);
		}

		// Token: 0x060039C2 RID: 14786 RVA: 0x000EC160 File Offset: 0x000EA360
		void IAllianceCampaignBehavior.OnCallToWarAgreementProposedToPlayer(Kingdom proposerKingdom, Kingdom kingdomToCallToWarAgainst)
		{
			if (Clan.PlayerClan.Kingdom.Clans.Count == 1)
			{
				int callToWarCost = Campaign.Current.Models.AllianceModel.GetCallToWarCost(proposerKingdom, Clan.PlayerClan.Kingdom, kingdomToCallToWarAgainst);
				object obj = new TextObject("{=ho5EndaV}Decision", null);
				TextObject textObject = new TextObject("{=L81DPSom}As {RULER_NAME_AND_TITLE}, you must decide if your realm will answer the call of the {CALLING_KINGDOM} and declare war on the {KINGDOM_TO_CALL_TO_WAR_AGAINST} for {CALL_TO_WAR_COST}{GOLD_ICON}.", null);
				TextObject textObject2 = GameTexts.FindText("str_faction_ruler_name_with_title", Hero.MainHero.MapFaction.Culture.StringId);
				textObject2.SetCharacterProperties("RULER", Hero.MainHero.CharacterObject, false);
				textObject.SetTextVariable("RULER_NAME_AND_TITLE", textObject2);
				textObject.SetTextVariable("CALLING_KINGDOM", proposerKingdom.Name);
				textObject.SetTextVariable("KINGDOM_TO_CALL_TO_WAR_AGAINST", kingdomToCallToWarAgainst.Name);
				textObject.SetTextVariable("CALL_TO_WAR_COST", callToWarCost);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
				TextObject textObject3 = new TextObject("{=Y94H6XnK}Accept", null);
				TextObject textObject4 = new TextObject("{=cOgmdp9e}Decline", null);
				InformationManager.ShowInquiry(new InquiryData(obj.ToString(), textObject.ToString(), true, true, textObject3.ToString(), textObject4.ToString(), delegate()
				{
					this.AcceptCallToWarAgreement(proposerKingdom, kingdomToCallToWarAgainst, callToWarCost);
				}, null, "", 0f, null, null, null), false, false);
				return;
			}
			object obj2 = new TextObject("{=ho5EndaV}Decision", null);
			TextObject textObject5 = new TextObject("{=FuNbouTu}A courier has arrived from the {KINGDOM_NAME}. They call your kingdom to war against {KINGDOM_TO_CALL_TO_WAR_AGAINST}. Your kingdom will vote whether to accept the offer.", null);
			textObject5.SetTextVariable("KINGDOM_NAME", proposerKingdom.Name);
			textObject5.SetTextVariable("KINGDOM_TO_CALL_TO_WAR_AGAINST", kingdomToCallToWarAgainst.Name);
			TextObject textObject6 = new TextObject("{=oHaWR73d}Ok", null);
			InformationManager.ShowInquiry(new InquiryData(obj2.ToString(), textObject5.ToString(), true, false, textObject6.ToString(), textObject6.ToString(), delegate()
			{
				this.ConfirmCallToWarAgreementOffer(proposerKingdom, kingdomToCallToWarAgainst);
			}, null, "", 0f, null, null, null), false, false);
		}

		// Token: 0x060039C3 RID: 14787 RVA: 0x000EC390 File Offset: 0x000EA590
		void IAllianceCampaignBehavior.OnCallToWarAgreementProposedToPlayerKingdom(Kingdom proposerKingdom, Kingdom kingdomToCallToWarAgainst)
		{
			if (Clan.PlayerClan.Kingdom.Clans.Count == 1)
			{
				TextObject textObject = new TextObject("{=PneX4Ayw}A courier bearing a call to war offer from the {KINGDOM_NAME} against {KINGDOM_TO_CALL_TO_WAR_AGAINST} has arrived at the court of your realm.", null);
				textObject.SetTextVariable("KINGDOM_NAME", proposerKingdom.Name);
				textObject.SetTextVariable("KINGDOM_TO_CALL_TO_WAR_AGAINST", kingdomToCallToWarAgainst.Name);
				Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new AcceptCallToWarOfferMapNotification(proposerKingdom, kingdomToCallToWarAgainst, textObject));
				return;
			}
			KingdomDecision kingdomDecision = Clan.PlayerClan.Kingdom.UnresolvedDecisions.FirstOrDefault(delegate(KingdomDecision s)
			{
				AcceptCallToWarAgreementDecision acceptCallToWarAgreementDecision;
				return (acceptCallToWarAgreementDecision = s as AcceptCallToWarAgreementDecision) != null && acceptCallToWarAgreementDecision.CallingKingdom == proposerKingdom;
			});
			if (kingdomDecision != null)
			{
				Clan.PlayerClan.Kingdom.RemoveDecision(kingdomDecision);
			}
			AcceptCallToWarAgreementDecision kingdomDecision2 = new AcceptCallToWarAgreementDecision(Clan.PlayerClan, proposerKingdom, kingdomToCallToWarAgainst);
			Clan.PlayerClan.Kingdom.AddDecision(kingdomDecision2, true);
		}

		// Token: 0x060039C4 RID: 14788 RVA: 0x000EC46C File Offset: 0x000EA66C
		void IAllianceCampaignBehavior.OnCallToWarAgreementProposedByPlayer(Kingdom proposedKingdom, Kingdom kingdomToCallToWarAgainst)
		{
			if (Clan.PlayerClan.Kingdom.Clans.Count == 1)
			{
				int callToWarCost = Campaign.Current.Models.AllianceModel.GetCallToWarCost(Clan.PlayerClan.Kingdom, proposedKingdom, kingdomToCallToWarAgainst);
				if (callToWarCost <= Clan.PlayerClan.Gold)
				{
					object obj = new TextObject("{=ho5EndaV}Decision", null);
					TextObject textObject = new TextObject("{=AwCnrOan}As {RULER_NAME_AND_TITLE}, you must decide if the {CALLED_KINGDOM} will be called to war against the {KINGDOM_TO_CALL_TO_WAR_AGAINST} for {CALL_TO_WAR_COST}{GOLD_ICON}.", null);
					TextObject textObject2 = GameTexts.FindText("str_faction_ruler_name_with_title", Hero.MainHero.MapFaction.Culture.StringId);
					textObject2.SetCharacterProperties("RULER", Hero.MainHero.CharacterObject, false);
					textObject.SetTextVariable("RULER_NAME_AND_TITLE", textObject2);
					textObject.SetTextVariable("CALLED_KINGDOM", proposedKingdom.Name);
					textObject.SetTextVariable("KINGDOM_TO_CALL_TO_WAR_AGAINST", kingdomToCallToWarAgainst.Name);
					textObject.SetTextVariable("CALL_TO_WAR_COST", callToWarCost);
					textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
					TextObject textObject3 = new TextObject("{=Y94H6XnK}Accept", null);
					TextObject textObject4 = new TextObject("{=cOgmdp9e}Decline", null);
					InformationManager.ShowInquiry(new InquiryData(obj.ToString(), textObject.ToString(), true, true, textObject3.ToString(), textObject4.ToString(), delegate()
					{
						this.AcceptProposalOfCallToWarAgreement(proposedKingdom, kingdomToCallToWarAgainst, callToWarCost);
					}, null, "", 0f, null, null, null), false, false);
					return;
				}
			}
			else
			{
				object obj2 = new TextObject("{=ho5EndaV}Decision", null);
				TextObject textObject5 = new TextObject("{=qgN9o2ip}It is time to call our ally the {KINGDOM_NAME} to war againts {KINGDOM_TO_CALL_TO_WAR_AGAINST}!. Your kingdom will vote whether to propose a call to war agreement to them.", null);
				textObject5.SetTextVariable("KINGDOM_NAME", proposedKingdom.Name);
				textObject5.SetTextVariable("KINGDOM_TO_CALL_TO_WAR_AGAINST", kingdomToCallToWarAgainst.Name);
				TextObject textObject6 = new TextObject("{=oHaWR73d}Ok", null);
				InformationManager.ShowInquiry(new InquiryData(obj2.ToString(), textObject5.ToString(), true, false, textObject6.ToString(), textObject6.ToString(), delegate()
				{
					this.ConfirmCallToWarAgreementProposalOffer(proposedKingdom, kingdomToCallToWarAgainst);
				}, null, "", 0f, null, null, null), false, false);
			}
		}

		// Token: 0x060039C5 RID: 14789 RVA: 0x000EC6B4 File Offset: 0x000EA8B4
		CampaignTime IAllianceCampaignBehavior.GetAllianceEndDate(Kingdom kingdom1, Kingdom kingdom2)
		{
			AllianceCampaignBehavior.Alliance alliance;
			if (!this.TryGetAlliance(kingdom1, kingdom2, out alliance))
			{
				Debug.FailedAssert("Cant find alliance", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\AllianceCampaignBehavior.cs", "GetAllianceEndDate", 271);
				return CampaignTime.Zero;
			}
			return alliance.EndTime;
		}

		// Token: 0x060039C6 RID: 14790 RVA: 0x000EC6F4 File Offset: 0x000EA8F4
		public void OnCallToWarAgreementProposedByPlayerKingdom(Kingdom proposedKingdom, Kingdom kingdomToCallToWarAgainst)
		{
			if (Clan.PlayerClan.Kingdom.Clans.Count == 1)
			{
				TextObject textObject = new TextObject("{=dDsJyerw}Call the {CALLED_KINGDOM} to War Against the {KINGDOM_TO_CALL_TO_WAR_AGAINST}.", null);
				textObject.SetTextVariable("CALLED_KINGDOM", proposedKingdom.Name);
				textObject.SetTextVariable("KINGDOM_TO_CALL_TO_WAR_AGAINST", kingdomToCallToWarAgainst.Name);
				Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new ProposeCallToWarOfferMapNotification(proposedKingdom, kingdomToCallToWarAgainst, textObject));
				return;
			}
			KingdomDecision kingdomDecision = Clan.PlayerClan.Kingdom.UnresolvedDecisions.FirstOrDefault(delegate(KingdomDecision s)
			{
				ProposeCallToWarAgreementDecision proposeCallToWarAgreementDecision;
				return (proposeCallToWarAgreementDecision = s as ProposeCallToWarAgreementDecision) != null && proposeCallToWarAgreementDecision.CalledKingdom == proposedKingdom;
			});
			if (kingdomDecision != null)
			{
				Clan.PlayerClan.Kingdom.RemoveDecision(kingdomDecision);
			}
			ProposeCallToWarAgreementDecision kingdomDecision2 = new ProposeCallToWarAgreementDecision(Clan.PlayerClan, proposedKingdom, kingdomToCallToWarAgainst);
			Clan.PlayerClan.Kingdom.AddDecision(kingdomDecision2, true);
		}

		// Token: 0x060039C7 RID: 14791 RVA: 0x000EC7D0 File Offset: 0x000EA9D0
		public bool IsAllyWithKingdom(Kingdom kingdom1, Kingdom kingdom2)
		{
			AllianceCampaignBehavior.Alliance alliance;
			return kingdom1 != null && kingdom2 != null && kingdom1 != kingdom2 && !kingdom1.IsEliminated && !kingdom2.IsEliminated && this.TryGetAlliance(kingdom1, kingdom2, out alliance);
		}

		// Token: 0x060039C8 RID: 14792 RVA: 0x000EC804 File Offset: 0x000EAA04
		public void StartAlliance(Kingdom proposerKingdom, Kingdom receiverKingdom)
		{
			if (!this.IsAllyWithKingdom(proposerKingdom, receiverKingdom))
			{
				StanceLink stanceWith = proposerKingdom.GetStanceWith(receiverKingdom);
				if (stanceWith.GetDailyTributeToPay(proposerKingdom) != 0)
				{
					stanceWith.SetDailyTributePaid(proposerKingdom, 0, 0);
				}
				if (stanceWith.GetDailyTributeToPay(proposerKingdom) != 0)
				{
					stanceWith.SetDailyTributePaid(proposerKingdom, 0, 0);
				}
				foreach (IFaction faction in proposerKingdom.FactionsAtWarWith)
				{
					if (faction.IsKingdomFaction && !faction.IsAtWarWith(receiverKingdom))
					{
						if (proposerKingdom == Clan.PlayerClan.Kingdom)
						{
							Campaign.Current.GetCampaignBehavior<IAllianceCampaignBehavior>().OnCallToWarAgreementProposedByPlayerKingdom(receiverKingdom, (Kingdom)faction);
						}
						else
						{
							ProposeCallToWarAgreementDecision kingdomDecision = new ProposeCallToWarAgreementDecision(proposerKingdom.RulingClan, receiverKingdom, (Kingdom)faction);
							proposerKingdom.AddDecision(kingdomDecision, true);
						}
					}
				}
				foreach (IFaction faction2 in receiverKingdom.FactionsAtWarWith)
				{
					if (faction2.IsKingdomFaction && !faction2.IsAtWarWith(proposerKingdom))
					{
						if (receiverKingdom == Clan.PlayerClan.Kingdom)
						{
							Campaign.Current.GetCampaignBehavior<IAllianceCampaignBehavior>().OnCallToWarAgreementProposedByPlayerKingdom(proposerKingdom, (Kingdom)faction2);
						}
						else
						{
							ProposeCallToWarAgreementDecision kingdomDecision2 = new ProposeCallToWarAgreementDecision(receiverKingdom.RulingClan, proposerKingdom, (Kingdom)faction2);
							receiverKingdom.AddDecision(kingdomDecision2, true);
						}
					}
				}
				this.AddAlliance(proposerKingdom, receiverKingdom);
				CampaignEventDispatcher.Instance.OnAllianceStarted(proposerKingdom, receiverKingdom);
			}
		}

		// Token: 0x060039C9 RID: 14793 RVA: 0x000EC984 File Offset: 0x000EAB84
		public void EndAlliance(Kingdom kingdom1, Kingdom kingdom2)
		{
			foreach (AllianceCampaignBehavior.CallToWarAgreement callToWarAgreement in this.GetCallToWarAgreements(kingdom1, kingdom2))
			{
				this.EndCallToWarAgreement(callToWarAgreement.CallingKingdom, callToWarAgreement.CalledKingdom, callToWarAgreement.KingdomToCallToWarAgainst);
			}
			this.RemoveAlliance(kingdom1, kingdom2);
			CampaignEventDispatcher.Instance.OnAllianceEnded(kingdom1, kingdom2);
		}

		// Token: 0x060039CA RID: 14794 RVA: 0x000ECA00 File Offset: 0x000EAC00
		public bool HasCalledToWar(Kingdom callingKingdom, Kingdom calledKingdom)
		{
			return callingKingdom != null && calledKingdom != null && callingKingdom != calledKingdom && !callingKingdom.IsEliminated && !calledKingdom.IsEliminated && calledKingdom.IsAllyWith(callingKingdom) && this._callToWarAgreements.AnyQ((AllianceCampaignBehavior.CallToWarAgreement c) => c.CallingKingdom == callingKingdom && c.CalledKingdom == calledKingdom);
		}

		// Token: 0x060039CB RID: 14795 RVA: 0x000ECA88 File Offset: 0x000EAC88
		public bool IsAtWarByCallToWarAgreement(Kingdom calledKingdom, Kingdom kingdomToCallToWarAgainst)
		{
			return kingdomToCallToWarAgainst != null && calledKingdom != null && kingdomToCallToWarAgainst != calledKingdom && !kingdomToCallToWarAgainst.IsEliminated && !calledKingdom.IsEliminated && this._callToWarAgreements.AnyQ((AllianceCampaignBehavior.CallToWarAgreement c) => c.CalledKingdom == calledKingdom && c.KingdomToCallToWarAgainst == kingdomToCallToWarAgainst);
		}

		// Token: 0x060039CC RID: 14796 RVA: 0x000ECAFC File Offset: 0x000EACFC
		public void StartCallToWarAgreement(Kingdom callingKingdom, Kingdom calledKingdom, Kingdom kingdomToCallToWarAgainst, int callToWarCost, bool isPlayerPaying = false)
		{
			if (this.IsAllyWithKingdom(callingKingdom, calledKingdom) && !calledKingdom.IsAtWarWith(kingdomToCallToWarAgainst))
			{
				AllianceCampaignBehavior.CallToWarAgreement callToWarAgreement = this.AddCallToWarAgreement(callingKingdom, calledKingdom, kingdomToCallToWarAgainst);
				AllianceCampaignBehavior.Alliance alliance;
				if (this.TryGetAlliance(callingKingdom, calledKingdom, out alliance) && alliance.EndTime < callToWarAgreement.EndTime)
				{
					alliance.EndTime = callToWarAgreement.EndTime;
				}
				if (isPlayerPaying)
				{
					Hero.MainHero.ChangeHeroGold(-callToWarCost);
					calledKingdom.CallToWarWallet += callToWarCost;
				}
				else
				{
					callingKingdom.CallToWarWallet -= callToWarCost;
					calledKingdom.CallToWarWallet += callToWarCost;
				}
				CampaignEventDispatcher.Instance.OnCallToWarAgreementStarted(callingKingdom, calledKingdom, kingdomToCallToWarAgainst);
				DeclareWarAction.ApplyByCallToWarAgreement(calledKingdom, kingdomToCallToWarAgainst);
			}
		}

		// Token: 0x060039CD RID: 14797 RVA: 0x000ECBAC File Offset: 0x000EADAC
		public void EndCallToWarAgreement(Kingdom callingKingdom, Kingdom calledKingdom, Kingdom kingdomToCallToWarAgainst)
		{
			this.RemoveCallToWarAgreement(callingKingdom, calledKingdom, kingdomToCallToWarAgainst);
			CampaignEventDispatcher.Instance.OnCallToWarAgreementEnded(callingKingdom, calledKingdom, kingdomToCallToWarAgainst);
		}

		// Token: 0x060039CE RID: 14798 RVA: 0x000ECBC4 File Offset: 0x000EADC4
		public List<Kingdom> GetKingdomsToCallToWarAgainst(Kingdom callingKingdom, Kingdom calledKingdom)
		{
			if (callingKingdom != calledKingdom)
			{
				return this._callToWarAgreements.WhereQ((AllianceCampaignBehavior.CallToWarAgreement c) => c.CallingKingdom == callingKingdom && c.CalledKingdom == calledKingdom).SelectQ((AllianceCampaignBehavior.CallToWarAgreement x) => x.KingdomToCallToWarAgainst).ToListQ<Kingdom>();
			}
			return new List<Kingdom>();
		}

		// Token: 0x060039CF RID: 14799 RVA: 0x000ECC3C File Offset: 0x000EAE3C
		private AllianceCampaignBehavior.Alliance AddAlliance(Kingdom kingdom1, Kingdom kingdom2)
		{
			AllianceCampaignBehavior.Alliance alliance = new AllianceCampaignBehavior.Alliance(kingdom1, kingdom2, CampaignTime.Now + Campaign.Current.Models.AllianceModel.MaxDurationOfAlliance);
			this._alliances.Add(alliance);
			kingdom1.UpdateAlliedKingdoms();
			kingdom2.UpdateAlliedKingdoms();
			return alliance;
		}

		// Token: 0x060039D0 RID: 14800 RVA: 0x000ECC8C File Offset: 0x000EAE8C
		private void RemoveAlliance(Kingdom kingdom1, Kingdom kingdom2)
		{
			int num = this._alliances.Count - 1;
			while (-1 < num)
			{
				AllianceCampaignBehavior.Alliance alliance = this._alliances[num];
				if ((alliance.Kingdom1 == kingdom1 && alliance.Kingdom2 == kingdom2) || (alliance.Kingdom2 == kingdom1 && alliance.Kingdom1 == kingdom2))
				{
					this._alliances.RemoveAt(num);
					break;
				}
				num--;
			}
			kingdom1.UpdateAlliedKingdoms();
			kingdom2.UpdateAlliedKingdoms();
		}

		// Token: 0x060039D1 RID: 14801 RVA: 0x000ECCFC File Offset: 0x000EAEFC
		private AllianceCampaignBehavior.CallToWarAgreement AddCallToWarAgreement(Kingdom callingKingdom, Kingdom calledKingdom, Kingdom kingdomToCallToWarAgainst)
		{
			AllianceCampaignBehavior.CallToWarAgreement callToWarAgreement = new AllianceCampaignBehavior.CallToWarAgreement(callingKingdom, calledKingdom, kingdomToCallToWarAgainst, CampaignTime.Now + Campaign.Current.Models.AllianceModel.MaxDurationOfWarParticipation);
			this._callToWarAgreements.Add(callToWarAgreement);
			return callToWarAgreement;
		}

		// Token: 0x060039D2 RID: 14802 RVA: 0x000ECD40 File Offset: 0x000EAF40
		private void RemoveCallToWarAgreement(Kingdom callingKingdom, Kingdom calledKingdom, Kingdom kingdomToCallToWarAgainst)
		{
			int num = this._callToWarAgreements.Count - 1;
			while (-1 < num)
			{
				AllianceCampaignBehavior.CallToWarAgreement callToWarAgreement = this._callToWarAgreements[num];
				if (callToWarAgreement.CallingKingdom == callingKingdom && callToWarAgreement.CalledKingdom == calledKingdom && callToWarAgreement.KingdomToCallToWarAgainst == kingdomToCallToWarAgainst)
				{
					this._callToWarAgreements.RemoveAt(num);
					return;
				}
				num--;
			}
		}

		// Token: 0x060039D3 RID: 14803 RVA: 0x000ECD9C File Offset: 0x000EAF9C
		private List<AllianceCampaignBehavior.CallToWarAgreement> GetCallToWarAgreements(Kingdom kingdom1, Kingdom kingdom2)
		{
			if (kingdom1 != kingdom2)
			{
				return (from c in this._callToWarAgreements
					where (c.CallingKingdom == kingdom1 && c.CalledKingdom == kingdom2) || (c.CallingKingdom == kingdom2 && c.CalledKingdom == kingdom1)
					select c).ToListQ<AllianceCampaignBehavior.CallToWarAgreement>();
			}
			return new List<AllianceCampaignBehavior.CallToWarAgreement>();
		}

		// Token: 0x060039D4 RID: 14804 RVA: 0x000ECDF0 File Offset: 0x000EAFF0
		private bool TryGetAlliance(Kingdom kingdom1, Kingdom kingdom2, out AllianceCampaignBehavior.Alliance foundAlliance)
		{
			bool result = false;
			foundAlliance = default(AllianceCampaignBehavior.Alliance);
			foreach (AllianceCampaignBehavior.Alliance alliance in this._alliances)
			{
				if ((alliance.Kingdom1 == kingdom1 && alliance.Kingdom2 == kingdom2) || (alliance.Kingdom2 == kingdom1 && alliance.Kingdom1 == kingdom2))
				{
					foundAlliance = alliance;
					result = true;
					break;
				}
			}
			return result;
		}

		// Token: 0x060039D5 RID: 14805 RVA: 0x000ECE78 File Offset: 0x000EB078
		private void AcceptProposalOfCallToWarAgreement(Kingdom proposedKingdom, Kingdom kingdomToCallToWarAgainst, int callToWarCost)
		{
			Campaign.Current.GetCampaignBehavior<IAllianceCampaignBehavior>().StartCallToWarAgreement(Clan.PlayerClan.Kingdom, proposedKingdom, kingdomToCallToWarAgainst, callToWarCost, false);
		}

		// Token: 0x060039D6 RID: 14806 RVA: 0x000ECE97 File Offset: 0x000EB097
		private void AcceptCallToWarAgreement(Kingdom proposerKingdom, Kingdom kingdomToCallToWarAgainst, int callToWarCost)
		{
			Campaign.Current.GetCampaignBehavior<IAllianceCampaignBehavior>().StartCallToWarAgreement(proposerKingdom, Clan.PlayerClan.Kingdom, kingdomToCallToWarAgainst, callToWarCost, false);
		}

		// Token: 0x060039D7 RID: 14807 RVA: 0x000ECEB6 File Offset: 0x000EB0B6
		private void AcceptStartingAlliance(Kingdom proposerKingdom)
		{
			Campaign.Current.GetCampaignBehavior<IAllianceCampaignBehavior>().StartAlliance(proposerKingdom, Clan.PlayerClan.Kingdom);
		}

		// Token: 0x060039D8 RID: 14808 RVA: 0x000ECED4 File Offset: 0x000EB0D4
		private void ConfirmAllianceOffer(Kingdom proposerKingdom)
		{
			if (Clan.PlayerClan.IsUnderMercenaryService)
			{
				this.AcceptStartingAlliance(proposerKingdom);
				return;
			}
			KingdomDecision kingdomDecision = Clan.PlayerClan.Kingdom.UnresolvedDecisions.FirstOrDefault(delegate(KingdomDecision s)
			{
				StartAllianceDecision startAllianceDecision;
				return (startAllianceDecision = s as StartAllianceDecision) != null && startAllianceDecision.KingdomToStartAllianceWith == proposerKingdom;
			});
			if (kingdomDecision != null)
			{
				Clan.PlayerClan.Kingdom.RemoveDecision(kingdomDecision);
			}
			Clan.PlayerClan.Kingdom.AddDecision(new StartAllianceDecision(StartAllianceDecision.GetProposerClanForPlayerKingdom(proposerKingdom), proposerKingdom), true);
		}

		// Token: 0x060039D9 RID: 14809 RVA: 0x000ECF64 File Offset: 0x000EB164
		private void ConfirmCallToWarAgreementProposalOffer(Kingdom proposedKingdom, Kingdom kingdomToCallToWarAgainst)
		{
			ProposeCallToWarAgreementDecision proposeCallToWarAgreementDecision = new ProposeCallToWarAgreementDecision(Clan.PlayerClan, proposedKingdom, kingdomToCallToWarAgainst);
			int callToWarCost = proposeCallToWarAgreementDecision.CallToWarCost;
			if (Clan.PlayerClan.IsUnderMercenaryService)
			{
				this.AcceptProposalOfCallToWarAgreement(proposedKingdom, kingdomToCallToWarAgainst, callToWarCost);
				return;
			}
			KingdomDecision kingdomDecision = Clan.PlayerClan.Kingdom.UnresolvedDecisions.FirstOrDefault(delegate(KingdomDecision s)
			{
				ProposeCallToWarAgreementDecision proposeCallToWarAgreementDecision2;
				return (proposeCallToWarAgreementDecision2 = s as ProposeCallToWarAgreementDecision) != null && proposeCallToWarAgreementDecision2.CalledKingdom == proposedKingdom;
			});
			if (kingdomDecision != null)
			{
				Clan.PlayerClan.Kingdom.RemoveDecision(kingdomDecision);
			}
			Clan.PlayerClan.Kingdom.AddDecision(proposeCallToWarAgreementDecision, true);
		}

		// Token: 0x060039DA RID: 14810 RVA: 0x000ECFF8 File Offset: 0x000EB1F8
		private void ConfirmCallToWarAgreementOffer(Kingdom proposerKingdom, Kingdom kingdomToCallToWarAgainst)
		{
			AcceptCallToWarAgreementDecision acceptCallToWarAgreementDecision = new AcceptCallToWarAgreementDecision(Clan.PlayerClan, proposerKingdom, kingdomToCallToWarAgainst);
			int callToWarCost = acceptCallToWarAgreementDecision.CallToWarCost;
			if (Clan.PlayerClan.IsUnderMercenaryService)
			{
				this.AcceptCallToWarAgreement(proposerKingdom, kingdomToCallToWarAgainst, callToWarCost);
				return;
			}
			KingdomDecision kingdomDecision = Clan.PlayerClan.Kingdom.UnresolvedDecisions.FirstOrDefault(delegate(KingdomDecision s)
			{
				AcceptCallToWarAgreementDecision acceptCallToWarAgreementDecision2;
				return (acceptCallToWarAgreementDecision2 = s as AcceptCallToWarAgreementDecision) != null && acceptCallToWarAgreementDecision2.CallingKingdom == proposerKingdom;
			});
			if (kingdomDecision != null)
			{
				Clan.PlayerClan.Kingdom.RemoveDecision(kingdomDecision);
			}
			Clan.PlayerClan.Kingdom.AddDecision(acceptCallToWarAgreementDecision, true);
		}

		// Token: 0x060039DB RID: 14811 RVA: 0x000ED08C File Offset: 0x000EB28C
		private void DailyTickClan(Clan clan)
		{
			if (!clan.IsEliminated)
			{
				clan.Aggressiveness -= 1f;
				if (clan.Kingdom != null && clan.Kingdom.RulingClan == clan)
				{
					Kingdom kingdom = clan.Kingdom;
					if (!kingdom.AlliedKingdoms.IsEmpty<Kingdom>())
					{
						for (int i = kingdom.AlliedKingdoms.Count - 1; i > -1; i--)
						{
							Kingdom kingdom2 = kingdom.AlliedKingdoms[i];
							AllianceCampaignBehavior.Alliance alliance;
							if (this.TryGetAlliance(kingdom2, kingdom, out alliance))
							{
								List<AllianceCampaignBehavior.CallToWarAgreement> callToWarAgreements = this.GetCallToWarAgreements(kingdom, kingdom2);
								for (int j = callToWarAgreements.Count - 1; j > -1; j--)
								{
									AllianceCampaignBehavior.CallToWarAgreement callToWarAgreement = callToWarAgreements[j];
									if (callToWarAgreement.EndTime.IsPast)
									{
										this.EndCallToWarAgreement(callToWarAgreement.CallingKingdom, callToWarAgreement.CalledKingdom, callToWarAgreement.KingdomToCallToWarAgainst);
									}
								}
								if (alliance.EndTime.IsPast)
								{
									this.EndAlliance(kingdom, kingdom2);
								}
							}
						}
					}
				}
			}
		}

		// Token: 0x060039DC RID: 14812 RVA: 0x000ED18C File Offset: 0x000EB38C
		private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
		{
			if (faction1.IsKingdomFaction && faction2.IsKingdomFaction)
			{
				Kingdom kingdom = (Kingdom)faction1;
				Kingdom kingdom2 = (Kingdom)faction2;
				if (kingdom.IsAllyWith(kingdom2))
				{
					this.EndAlliance(kingdom, kingdom2);
				}
				foreach (Kingdom kingdom3 in kingdom.AlliedKingdoms.ToList<Kingdom>())
				{
					if (!kingdom3.IsAtWarWith(kingdom2))
					{
						if (kingdom == Clan.PlayerClan.Kingdom)
						{
							Campaign.Current.GetCampaignBehavior<IAllianceCampaignBehavior>().OnCallToWarAgreementProposedByPlayerKingdom(kingdom3, kingdom2);
						}
						else
						{
							ProposeCallToWarAgreementDecision kingdomDecision = new ProposeCallToWarAgreementDecision(kingdom.RulingClan, kingdom3, kingdom2);
							kingdom.AddDecision(kingdomDecision, true);
						}
					}
				}
				foreach (Kingdom kingdom4 in kingdom2.AlliedKingdoms.ToList<Kingdom>())
				{
					if (!kingdom4.IsAtWarWith(kingdom))
					{
						if (kingdom2 == Clan.PlayerClan.Kingdom)
						{
							Campaign.Current.GetCampaignBehavior<IAllianceCampaignBehavior>().OnCallToWarAgreementProposedByPlayerKingdom(kingdom4, kingdom);
						}
						else
						{
							ProposeCallToWarAgreementDecision kingdomDecision2 = new ProposeCallToWarAgreementDecision(kingdom2.RulingClan, kingdom4, kingdom);
							kingdom2.AddDecision(kingdomDecision2, true);
						}
					}
				}
			}
		}

		// Token: 0x060039DD RID: 14813 RVA: 0x000ED2D8 File Offset: 0x000EB4D8
		private void OnMakePeace(IFaction faction1, IFaction faction2, MakePeaceAction.MakePeaceDetail detail)
		{
			if (faction1.IsKingdomFaction && faction2.IsKingdomFaction)
			{
				Kingdom kingdom1 = (Kingdom)faction1;
				Kingdom kingdom2 = (Kingdom)faction2;
				using (List<Kingdom>.Enumerator enumerator = kingdom1.AlliedKingdoms.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						Kingdom ally = enumerator.Current;
						if (this._callToWarAgreements.AnyQ((AllianceCampaignBehavior.CallToWarAgreement c) => c.CallingKingdom == kingdom1 && c.CalledKingdom == ally && c.KingdomToCallToWarAgainst == kingdom2))
						{
							this.EndCallToWarAgreement(kingdom1, ally, kingdom2);
						}
					}
				}
				using (List<Kingdom>.Enumerator enumerator = kingdom2.AlliedKingdoms.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						Kingdom ally = enumerator.Current;
						if (this._callToWarAgreements.AnyQ((AllianceCampaignBehavior.CallToWarAgreement c) => c.CallingKingdom == kingdom2 && c.CalledKingdom == ally && c.KingdomToCallToWarAgainst == kingdom1))
						{
							this.EndCallToWarAgreement(kingdom2, ally, kingdom1);
						}
					}
				}
			}
		}

		// Token: 0x060039DE RID: 14814 RVA: 0x000ED438 File Offset: 0x000EB638
		private void OnKingdomDestroyed(Kingdom kingdom)
		{
			IEnumerable<AllianceCampaignBehavior.Alliance> alliances = this._alliances;
			Func<AllianceCampaignBehavior.Alliance, bool> <>9__0;
			Func<AllianceCampaignBehavior.Alliance, bool> predicate;
			if ((predicate = <>9__0) == null)
			{
				predicate = (<>9__0 = (AllianceCampaignBehavior.Alliance a) => a.Kingdom1 == kingdom || a.Kingdom2 == kingdom);
			}
			foreach (AllianceCampaignBehavior.Alliance alliance in alliances.Where(predicate).ToList<AllianceCampaignBehavior.Alliance>())
			{
				this.EndAlliance(alliance.Kingdom1, alliance.Kingdom2);
			}
		}

		// Token: 0x04001203 RID: 4611
		private List<AllianceCampaignBehavior.Alliance> _alliances = new List<AllianceCampaignBehavior.Alliance>();

		// Token: 0x04001204 RID: 4612
		private List<AllianceCampaignBehavior.CallToWarAgreement> _callToWarAgreements = new List<AllianceCampaignBehavior.CallToWarAgreement>();

		// Token: 0x02000797 RID: 1943
		public class AllianceCampaignBehaviorTypeDefiner : SaveableTypeDefiner
		{
			// Token: 0x06006201 RID: 25089 RVA: 0x001BA7C7 File Offset: 0x001B89C7
			public AllianceCampaignBehaviorTypeDefiner()
				: base(312270)
			{
			}

			// Token: 0x06006202 RID: 25090 RVA: 0x001BA7D4 File Offset: 0x001B89D4
			protected override void DefineStructTypes()
			{
				base.AddStructDefinition(typeof(AllianceCampaignBehavior.Alliance), 1, null);
				base.AddStructDefinition(typeof(AllianceCampaignBehavior.CallToWarAgreement), 2, null);
			}

			// Token: 0x06006203 RID: 25091 RVA: 0x001BA7FA File Offset: 0x001B89FA
			protected override void DefineContainerDefinitions()
			{
				base.ConstructContainerDefinition(typeof(List<AllianceCampaignBehavior.Alliance>));
				base.ConstructContainerDefinition(typeof(List<AllianceCampaignBehavior.CallToWarAgreement>));
			}
		}

		// Token: 0x02000798 RID: 1944
		internal struct Alliance
		{
			// Token: 0x06006204 RID: 25092 RVA: 0x001BA81C File Offset: 0x001B8A1C
			public Alliance(Kingdom kingdom1, Kingdom kingdom2, CampaignTime endTime)
			{
				this.Kingdom1 = kingdom1;
				this.Kingdom2 = kingdom2;
				this.EndTime = endTime;
			}

			// Token: 0x06006205 RID: 25093 RVA: 0x001BA834 File Offset: 0x001B8A34
			public static void AutoGeneratedStaticCollectObjectsAlliance(object o, List<object> collectedObjects)
			{
				((AllianceCampaignBehavior.Alliance)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
			}

			// Token: 0x06006206 RID: 25094 RVA: 0x001BA850 File Offset: 0x001B8A50
			private void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
			{
				collectedObjects.Add(this.Kingdom1);
				collectedObjects.Add(this.Kingdom2);
				CampaignTime.AutoGeneratedStaticCollectObjectsCampaignTime(this.EndTime, collectedObjects);
			}

			// Token: 0x06006207 RID: 25095 RVA: 0x001BA87B File Offset: 0x001B8A7B
			internal static object AutoGeneratedGetMemberValueKingdom1(object o)
			{
				return ((AllianceCampaignBehavior.Alliance)o).Kingdom1;
			}

			// Token: 0x06006208 RID: 25096 RVA: 0x001BA888 File Offset: 0x001B8A88
			internal static object AutoGeneratedGetMemberValueKingdom2(object o)
			{
				return ((AllianceCampaignBehavior.Alliance)o).Kingdom2;
			}

			// Token: 0x06006209 RID: 25097 RVA: 0x001BA895 File Offset: 0x001B8A95
			internal static object AutoGeneratedGetMemberValueEndTime(object o)
			{
				return ((AllianceCampaignBehavior.Alliance)o).EndTime;
			}

			// Token: 0x04001E8D RID: 7821
			[SaveableField(0)]
			public readonly Kingdom Kingdom1;

			// Token: 0x04001E8E RID: 7822
			[SaveableField(1)]
			public readonly Kingdom Kingdom2;

			// Token: 0x04001E8F RID: 7823
			[SaveableField(2)]
			public CampaignTime EndTime;
		}

		// Token: 0x02000799 RID: 1945
		internal struct CallToWarAgreement
		{
			// Token: 0x0600620A RID: 25098 RVA: 0x001BA8A7 File Offset: 0x001B8AA7
			public CallToWarAgreement(Kingdom callingKingdom, Kingdom calledKingdom, Kingdom kingdomToCallToWarAgainst, CampaignTime endTime)
			{
				this.CallingKingdom = callingKingdom;
				this.CalledKingdom = calledKingdom;
				this.KingdomToCallToWarAgainst = kingdomToCallToWarAgainst;
				this.EndTime = endTime;
			}

			// Token: 0x0600620B RID: 25099 RVA: 0x001BA8C8 File Offset: 0x001B8AC8
			public static void AutoGeneratedStaticCollectObjectsCallToWarAgreement(object o, List<object> collectedObjects)
			{
				((AllianceCampaignBehavior.CallToWarAgreement)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
			}

			// Token: 0x0600620C RID: 25100 RVA: 0x001BA8E4 File Offset: 0x001B8AE4
			private void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
			{
				collectedObjects.Add(this.CallingKingdom);
				collectedObjects.Add(this.CalledKingdom);
				collectedObjects.Add(this.KingdomToCallToWarAgainst);
				CampaignTime.AutoGeneratedStaticCollectObjectsCampaignTime(this.EndTime, collectedObjects);
			}

			// Token: 0x0600620D RID: 25101 RVA: 0x001BA91B File Offset: 0x001B8B1B
			internal static object AutoGeneratedGetMemberValueCallingKingdom(object o)
			{
				return ((AllianceCampaignBehavior.CallToWarAgreement)o).CallingKingdom;
			}

			// Token: 0x0600620E RID: 25102 RVA: 0x001BA928 File Offset: 0x001B8B28
			internal static object AutoGeneratedGetMemberValueCalledKingdom(object o)
			{
				return ((AllianceCampaignBehavior.CallToWarAgreement)o).CalledKingdom;
			}

			// Token: 0x0600620F RID: 25103 RVA: 0x001BA935 File Offset: 0x001B8B35
			internal static object AutoGeneratedGetMemberValueKingdomToCallToWarAgainst(object o)
			{
				return ((AllianceCampaignBehavior.CallToWarAgreement)o).KingdomToCallToWarAgainst;
			}

			// Token: 0x06006210 RID: 25104 RVA: 0x001BA942 File Offset: 0x001B8B42
			internal static object AutoGeneratedGetMemberValueEndTime(object o)
			{
				return ((AllianceCampaignBehavior.CallToWarAgreement)o).EndTime;
			}

			// Token: 0x04001E90 RID: 7824
			[SaveableField(0)]
			public readonly Kingdom CallingKingdom;

			// Token: 0x04001E91 RID: 7825
			[SaveableField(1)]
			public readonly Kingdom CalledKingdom;

			// Token: 0x04001E92 RID: 7826
			[SaveableField(2)]
			public readonly Kingdom KingdomToCallToWarAgainst;

			// Token: 0x04001E93 RID: 7827
			[SaveableField(3)]
			public CampaignTime EndTime;
		}
	}
}
