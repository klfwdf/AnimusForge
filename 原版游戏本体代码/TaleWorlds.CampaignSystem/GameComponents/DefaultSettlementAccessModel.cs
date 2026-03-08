using System;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x02000147 RID: 327
	public class DefaultSettlementAccessModel : SettlementAccessModel
	{
		// Token: 0x0600198C RID: 6540 RVA: 0x0007FCE8 File Offset: 0x0007DEE8
		public override void CanMainHeroEnterSettlement(Settlement settlement, out SettlementAccessModel.AccessDetails accessDetails)
		{
			if (settlement.IsFortification && Hero.MainHero.MapFaction == settlement.MapFaction && (settlement.Town.GarrisonParty == null || settlement.Town.GarrisonParty.Party.NumberOfAllMembers == 0))
			{
				accessDetails = new SettlementAccessModel.AccessDetails
				{
					AccessLevel = SettlementAccessModel.AccessLevel.FullAccess,
					AccessMethod = SettlementAccessModel.AccessMethod.Direct
				};
				return;
			}
			if (settlement.IsTown)
			{
				this.CanMainHeroEnterTown(settlement, out accessDetails);
				return;
			}
			if (settlement.IsCastle)
			{
				this.CanMainHeroEnterCastle(settlement, out accessDetails);
				return;
			}
			if (settlement.IsVillage)
			{
				this.CanMainHeroEnterVillage(settlement, out accessDetails);
				return;
			}
			Debug.FailedAssert("Invalid type of settlement", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\GameComponents\\DefaultSettlementAccessModel.cs", "CanMainHeroEnterSettlement", 41);
			accessDetails = new SettlementAccessModel.AccessDetails
			{
				AccessLevel = SettlementAccessModel.AccessLevel.FullAccess,
				AccessMethod = SettlementAccessModel.AccessMethod.Direct
			};
		}

		// Token: 0x0600198D RID: 6541 RVA: 0x0007FDBB File Offset: 0x0007DFBB
		public override void CanMainHeroEnterDungeon(Settlement settlement, out SettlementAccessModel.AccessDetails accessDetails)
		{
			accessDetails = default(SettlementAccessModel.AccessDetails);
			this.CanMainHeroEnterKeepInternal(settlement, out accessDetails);
		}

		// Token: 0x0600198E RID: 6542 RVA: 0x0007FDCC File Offset: 0x0007DFCC
		public override void CanMainHeroEnterLordsHall(Settlement settlement, out SettlementAccessModel.AccessDetails accessDetails)
		{
			accessDetails = default(SettlementAccessModel.AccessDetails);
			this.CanMainHeroEnterKeepInternal(settlement, out accessDetails);
		}

		// Token: 0x0600198F RID: 6543 RVA: 0x0007FDE0 File Offset: 0x0007DFE0
		private void CanMainHeroEnterKeepInternal(Settlement settlement, out SettlementAccessModel.AccessDetails accessDetails)
		{
			accessDetails = default(SettlementAccessModel.AccessDetails);
			Hero mainHero = Hero.MainHero;
			if (settlement.OwnerClan == mainHero.Clan)
			{
				accessDetails = new SettlementAccessModel.AccessDetails
				{
					AccessLevel = SettlementAccessModel.AccessLevel.FullAccess,
					AccessMethod = SettlementAccessModel.AccessMethod.Direct
				};
			}
			else if (DiplomacyHelper.IsSameFactionAndNotEliminated(mainHero.MapFaction, settlement.MapFaction))
			{
				accessDetails = new SettlementAccessModel.AccessDetails
				{
					AccessLevel = SettlementAccessModel.AccessLevel.FullAccess,
					AccessMethod = SettlementAccessModel.AccessMethod.Direct
				};
			}
			else if (FactionManager.IsNeutralWithFaction(mainHero.MapFaction, settlement.MapFaction))
			{
				if (Campaign.Current.IsMainHeroDisguised)
				{
					accessDetails = new SettlementAccessModel.AccessDetails
					{
						AccessLevel = SettlementAccessModel.AccessLevel.LimitedAccess,
						LimitedAccessSolution = SettlementAccessModel.LimitedAccessSolution.Disguise,
						AccessLimitationReason = SettlementAccessModel.AccessLimitationReason.Disguised
					};
				}
				else if (Campaign.Current.Models.CrimeModel.DoesPlayerHaveAnyCrimeRating(settlement.MapFaction))
				{
					accessDetails = new SettlementAccessModel.AccessDetails
					{
						AccessLevel = SettlementAccessModel.AccessLevel.LimitedAccess,
						LimitedAccessSolution = SettlementAccessModel.LimitedAccessSolution.Bribe,
						AccessLimitationReason = SettlementAccessModel.AccessLimitationReason.CrimeRating
					};
				}
				else if (mainHero.Clan.Tier < 3)
				{
					accessDetails = new SettlementAccessModel.AccessDetails
					{
						AccessLevel = SettlementAccessModel.AccessLevel.LimitedAccess,
						LimitedAccessSolution = SettlementAccessModel.LimitedAccessSolution.Bribe,
						AccessLimitationReason = SettlementAccessModel.AccessLimitationReason.ClanTier
					};
				}
				else
				{
					accessDetails = new SettlementAccessModel.AccessDetails
					{
						AccessLevel = SettlementAccessModel.AccessLevel.FullAccess,
						AccessMethod = SettlementAccessModel.AccessMethod.Direct
					};
				}
			}
			else if (FactionManager.IsAtWarAgainstFaction(mainHero.MapFaction, settlement.MapFaction))
			{
				accessDetails = new SettlementAccessModel.AccessDetails
				{
					AccessLevel = SettlementAccessModel.AccessLevel.LimitedAccess,
					LimitedAccessSolution = SettlementAccessModel.LimitedAccessSolution.Disguise,
					AccessLimitationReason = SettlementAccessModel.AccessLimitationReason.Disguised
				};
			}
			if (accessDetails.AccessLevel == SettlementAccessModel.AccessLevel.LimitedAccess && (accessDetails.LimitedAccessSolution == SettlementAccessModel.LimitedAccessSolution.Bribe || accessDetails.LimitedAccessSolution == SettlementAccessModel.LimitedAccessSolution.Disguise) && settlement.LocationComplex.GetListOfCharactersInLocation("lordshall").IsEmpty<LocationCharacter>() && settlement.LocationComplex.GetListOfCharactersInLocation("prison").IsEmpty<LocationCharacter>())
			{
				accessDetails.AccessLevel = SettlementAccessModel.AccessLevel.NoAccess;
				accessDetails.AccessLimitationReason = SettlementAccessModel.AccessLimitationReason.LocationEmpty;
			}
		}

		// Token: 0x06001990 RID: 6544 RVA: 0x0007FFE4 File Offset: 0x0007E1E4
		public override bool CanMainHeroAccessLocation(Settlement settlement, string locationId, out bool disableOption, out TextObject disabledText)
		{
			disabledText = null;
			disableOption = false;
			bool result = true;
			if (locationId == "center")
			{
				result = this.CanMainHeroWalkAroundTownCenter(settlement, out disableOption, out disabledText);
			}
			else if (locationId == "arena")
			{
				result = this.CanMainHeroGoToArena(settlement, out disableOption, out disabledText);
			}
			else if (locationId == "tavern")
			{
				result = this.CanMainHeroGoToTavern(settlement, out disableOption, out disabledText);
			}
			else if (locationId == "lordshall")
			{
				SettlementAccessModel.AccessDetails accessDetails;
				this.CanMainHeroEnterLordsHall(settlement, out accessDetails);
				if (accessDetails.AccessLevel == SettlementAccessModel.AccessLevel.LimitedAccess && accessDetails.LimitedAccessSolution == SettlementAccessModel.LimitedAccessSolution.Bribe)
				{
					result = Campaign.Current.Models.BribeCalculationModel.GetBribeToEnterLordsHall(settlement) == 0;
				}
				else
				{
					result = accessDetails.AccessLevel == SettlementAccessModel.AccessLevel.FullAccess;
				}
			}
			else if (locationId == "prison")
			{
				SettlementAccessModel.AccessDetails accessDetails2;
				this.CanMainHeroEnterDungeon(settlement, out accessDetails2);
				if (accessDetails2.AccessLevel == SettlementAccessModel.AccessLevel.LimitedAccess && accessDetails2.LimitedAccessSolution == SettlementAccessModel.LimitedAccessSolution.Bribe)
				{
					result = Campaign.Current.Models.BribeCalculationModel.GetBribeToEnterDungeon(settlement) == 0;
				}
				else
				{
					result = accessDetails2.AccessLevel == SettlementAccessModel.AccessLevel.FullAccess;
				}
			}
			else if (locationId == "house_1" || locationId == "house_2" || locationId == "house_3")
			{
				Location locationWithId = settlement.LocationComplex.GetLocationWithId(locationId);
				result = locationWithId.IsReserved && (locationWithId.SpecialItems.Count > 0 || locationWithId.GetCharacterList().Any<LocationCharacter>());
			}
			else if (locationId == "port")
			{
				disableOption = true;
				disabledText = new TextObject("{=ILnr9eCQ}Door is locked!", null);
				result = false;
			}
			else
			{
				Debug.FailedAssert("invalid location which is not supported by DefaultSettlementAccessModel", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\GameComponents\\DefaultSettlementAccessModel.cs", "CanMainHeroAccessLocation", 206);
			}
			return result;
		}

		// Token: 0x06001991 RID: 6545 RVA: 0x00080198 File Offset: 0x0007E398
		public override bool IsRequestMeetingOptionAvailable(Settlement settlement, out bool disableOption, out TextObject disabledText)
		{
			bool result = true;
			disableOption = false;
			disabledText = null;
			SettlementAccessModel.AccessDetails accessDetails;
			this.CanMainHeroEnterSettlement(settlement, out accessDetails);
			if (settlement.OwnerClan == Clan.PlayerClan)
			{
				result = false;
			}
			else if (DiplomacyHelper.IsSameFactionAndNotEliminated(settlement.MapFaction, Clan.PlayerClan.MapFaction) && accessDetails.AccessLevel == SettlementAccessModel.AccessLevel.NoAccess)
			{
				result = TownHelpers.IsThereAnyoneToMeetInTown(settlement);
			}
			else if (settlement.IsTown && FactionManager.IsNeutralWithFaction(Hero.MainHero.MapFaction, settlement.MapFaction) && Campaign.Current.Models.CrimeModel.IsPlayerCrimeRatingMild(settlement.MapFaction))
			{
				result = false;
			}
			else if (Clan.PlayerClan.Tier < 3)
			{
				disableOption = true;
				disabledText = new TextObject("{=bdzZUVxf}Your clan tier is not high enough to request a meeting.", null);
				result = true;
			}
			else if (TownHelpers.IsThereAnyoneToMeetInTown(settlement))
			{
				result = true;
			}
			else
			{
				disableOption = true;
				disabledText = new TextObject("{=196tGVIm}There are no nobles to meet.", null);
			}
			return result;
		}

		// Token: 0x06001992 RID: 6546 RVA: 0x00080270 File Offset: 0x0007E470
		public override bool CanMainHeroDoSettlementAction(Settlement settlement, SettlementAccessModel.SettlementAction settlementAction, out bool disableOption, out TextObject disabledText)
		{
			switch (settlementAction)
			{
			case SettlementAccessModel.SettlementAction.RecruitTroops:
				return this.CanMainHeroRecruitTroops(settlement, out disableOption, out disabledText);
			case SettlementAccessModel.SettlementAction.Craft:
				return this.CanMainHeroCraft(settlement, out disableOption, out disabledText);
			case SettlementAccessModel.SettlementAction.WalkAroundTheArena:
				return this.CanMainHeroEnterArena(settlement, out disableOption, out disabledText);
			case SettlementAccessModel.SettlementAction.JoinTournament:
				return this.CanMainHeroJoinTournament(settlement, out disableOption, out disabledText);
			case SettlementAccessModel.SettlementAction.WatchTournament:
				return this.CanMainHeroWatchTournament(settlement, out disableOption, out disabledText);
			case SettlementAccessModel.SettlementAction.Trade:
				return this.CanMainHeroTrade(settlement, out disableOption, out disabledText);
			case SettlementAccessModel.SettlementAction.WaitInSettlement:
				return this.CanMainHeroWaitInSettlement(settlement, out disableOption, out disabledText);
			case SettlementAccessModel.SettlementAction.ManageTown:
				return this.CanMainHeroManageTown(settlement, out disableOption, out disabledText);
			default:
				Debug.FailedAssert("Invalid Settlement Action", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\GameComponents\\DefaultSettlementAccessModel.cs", "CanMainHeroDoSettlementAction", 275);
				disableOption = false;
				disabledText = null;
				return true;
			}
		}

		// Token: 0x06001993 RID: 6547 RVA: 0x00080320 File Offset: 0x0007E520
		private bool CanMainHeroGoToArena(Settlement settlement, out bool disableOption, out TextObject disabledText)
		{
			if (Campaign.Current.IsMainHeroDisguised)
			{
				disabledText = new TextObject("{=brzz79Je}You cannot enter arena while in disguise.", null);
				disableOption = true;
				return false;
			}
			if (Campaign.Current.IsDay)
			{
				disabledText = null;
				disableOption = false;
				return true;
			}
			disabledText = new TextObject("{=wsbkjJhz}Arena is closed at night.", null);
			disableOption = true;
			return false;
		}

		// Token: 0x06001994 RID: 6548 RVA: 0x00080370 File Offset: 0x0007E570
		private bool CanMainHeroGoToTavern(Settlement settlement, out bool disableOption, out TextObject disabledText)
		{
			disabledText = null;
			disableOption = false;
			return true;
		}

		// Token: 0x06001995 RID: 6549 RVA: 0x00080379 File Offset: 0x0007E579
		private bool CanMainHeroEnterArena(Settlement settlement, out bool disableOption, out TextObject disabledText)
		{
			disableOption = false;
			disabledText = null;
			return true;
		}

		// Token: 0x06001996 RID: 6550 RVA: 0x00080384 File Offset: 0x0007E584
		private void CanMainHeroEnterVillage(Settlement settlement, out SettlementAccessModel.AccessDetails accessDetails)
		{
			Hero mainHero = Hero.MainHero;
			accessDetails = new SettlementAccessModel.AccessDetails
			{
				AccessLevel = SettlementAccessModel.AccessLevel.NoAccess,
				AccessLimitationReason = SettlementAccessModel.AccessLimitationReason.None,
				PreliminaryActionObligation = SettlementAccessModel.PreliminaryActionObligation.None,
				PreliminaryActionType = SettlementAccessModel.PreliminaryActionType.None
			};
			MobileParty partyBelongedTo = mainHero.PartyBelongedTo;
			if (partyBelongedTo != null && (partyBelongedTo.Army == null || partyBelongedTo.Army.LeaderParty == partyBelongedTo))
			{
				accessDetails.AccessLevel = SettlementAccessModel.AccessLevel.FullAccess;
				accessDetails.AccessMethod = SettlementAccessModel.AccessMethod.Direct;
			}
			if (settlement.Village.VillageState == Village.VillageStates.Looted)
			{
				accessDetails.AccessLevel = SettlementAccessModel.AccessLevel.NoAccess;
				accessDetails.AccessLimitationReason = SettlementAccessModel.AccessLimitationReason.VillageIsLooted;
			}
		}

		// Token: 0x06001997 RID: 6551 RVA: 0x0008040E File Offset: 0x0007E60E
		private bool CanMainHeroManageTown(Settlement settlement, out bool disableOption, out TextObject disabledText)
		{
			disabledText = null;
			disableOption = false;
			return settlement.IsTown && settlement.OwnerClan.Leader == Hero.MainHero;
		}

		// Token: 0x06001998 RID: 6552 RVA: 0x00080434 File Offset: 0x0007E634
		private void CanMainHeroEnterCastle(Settlement settlement, out SettlementAccessModel.AccessDetails accessDetails)
		{
			Hero mainHero = Hero.MainHero;
			accessDetails = default(SettlementAccessModel.AccessDetails);
			if (settlement.OwnerClan == mainHero.Clan)
			{
				accessDetails = new SettlementAccessModel.AccessDetails
				{
					AccessLevel = SettlementAccessModel.AccessLevel.FullAccess,
					AccessMethod = SettlementAccessModel.AccessMethod.Direct
				};
				return;
			}
			if (DiplomacyHelper.IsSameFactionAndNotEliminated(mainHero.MapFaction, settlement.MapFaction))
			{
				accessDetails = new SettlementAccessModel.AccessDetails
				{
					AccessLevel = SettlementAccessModel.AccessLevel.FullAccess,
					AccessMethod = SettlementAccessModel.AccessMethod.ByRequest
				};
				if (!settlement.Town.IsOwnerUnassigned && settlement.OwnerClan.Leader.GetRelationWithPlayer() < -4f && Hero.MainHero.MapFaction.Leader != Hero.MainHero)
				{
					accessDetails.AccessLevel = SettlementAccessModel.AccessLevel.NoAccess;
					accessDetails.AccessLimitationReason = SettlementAccessModel.AccessLimitationReason.RelationshipWithOwner;
					return;
				}
			}
			else if (FactionManager.IsNeutralWithFaction(mainHero.MapFaction, settlement.MapFaction))
			{
				accessDetails = new SettlementAccessModel.AccessDetails
				{
					AccessLevel = SettlementAccessModel.AccessLevel.FullAccess,
					AccessMethod = SettlementAccessModel.AccessMethod.ByRequest
				};
				if (Campaign.Current.Models.CrimeModel.DoesPlayerHaveAnyCrimeRating(settlement.MapFaction))
				{
					accessDetails.AccessLevel = SettlementAccessModel.AccessLevel.NoAccess;
					accessDetails.AccessLimitationReason = SettlementAccessModel.AccessLimitationReason.CrimeRating;
					return;
				}
				if (settlement.OwnerClan.Leader.GetRelationWithPlayer() < 0f)
				{
					accessDetails.AccessLevel = SettlementAccessModel.AccessLevel.NoAccess;
					accessDetails.AccessLimitationReason = SettlementAccessModel.AccessLimitationReason.RelationshipWithOwner;
					return;
				}
			}
			else if (FactionManager.IsAtWarAgainstFaction(mainHero.MapFaction, settlement.MapFaction))
			{
				accessDetails = new SettlementAccessModel.AccessDetails
				{
					AccessLevel = SettlementAccessModel.AccessLevel.NoAccess,
					AccessMethod = SettlementAccessModel.AccessMethod.ByRequest,
					AccessLimitationReason = SettlementAccessModel.AccessLimitationReason.HostileFaction
				};
			}
		}

		// Token: 0x06001999 RID: 6553 RVA: 0x000805C0 File Offset: 0x0007E7C0
		private void CanMainHeroEnterTown(Settlement settlement, out SettlementAccessModel.AccessDetails accessDetails)
		{
			Hero mainHero = Hero.MainHero;
			accessDetails = default(SettlementAccessModel.AccessDetails);
			if (settlement.OwnerClan == mainHero.Clan)
			{
				accessDetails = new SettlementAccessModel.AccessDetails
				{
					AccessLevel = SettlementAccessModel.AccessLevel.FullAccess,
					AccessMethod = SettlementAccessModel.AccessMethod.Direct
				};
				return;
			}
			if (DiplomacyHelper.IsSameFactionAndNotEliminated(mainHero.MapFaction, settlement.MapFaction))
			{
				accessDetails = new SettlementAccessModel.AccessDetails
				{
					AccessLevel = SettlementAccessModel.AccessLevel.FullAccess,
					AccessMethod = SettlementAccessModel.AccessMethod.Direct
				};
				if (Campaign.Current.Models.CrimeModel.IsPlayerCrimeRatingModerate(settlement.MapFaction) || Campaign.Current.Models.CrimeModel.IsPlayerCrimeRatingSevere(settlement.MapFaction))
				{
					accessDetails.PreliminaryActionType = SettlementAccessModel.PreliminaryActionType.FaceCharges;
					accessDetails.PreliminaryActionObligation = SettlementAccessModel.PreliminaryActionObligation.Optional;
					return;
				}
			}
			else if (FactionManager.IsNeutralWithFaction(mainHero.MapFaction, settlement.MapFaction))
			{
				accessDetails = new SettlementAccessModel.AccessDetails
				{
					AccessLevel = SettlementAccessModel.AccessLevel.FullAccess,
					AccessMethod = SettlementAccessModel.AccessMethod.Direct
				};
				if (Campaign.Current.Models.CrimeModel.IsPlayerCrimeRatingModerate(settlement.MapFaction) || Campaign.Current.Models.CrimeModel.IsPlayerCrimeRatingSevere(settlement.MapFaction))
				{
					accessDetails.AccessLevel = SettlementAccessModel.AccessLevel.LimitedAccess;
					accessDetails.AccessMethod = SettlementAccessModel.AccessMethod.None;
					accessDetails.LimitedAccessSolution = SettlementAccessModel.LimitedAccessSolution.Disguise;
					accessDetails.AccessLimitationReason = SettlementAccessModel.AccessLimitationReason.CrimeRating;
					return;
				}
			}
			else if (FactionManager.IsAtWarAgainstFaction(mainHero.MapFaction, settlement.MapFaction))
			{
				accessDetails = new SettlementAccessModel.AccessDetails
				{
					AccessLevel = SettlementAccessModel.AccessLevel.LimitedAccess,
					LimitedAccessSolution = SettlementAccessModel.LimitedAccessSolution.Disguise,
					AccessLimitationReason = SettlementAccessModel.AccessLimitationReason.HostileFaction
				};
			}
		}

		// Token: 0x0600199A RID: 6554 RVA: 0x00080745 File Offset: 0x0007E945
		private bool CanMainHeroWalkAroundTownCenter(Settlement settlement, out bool disableOption, out TextObject disabledText)
		{
			disabledText = null;
			disableOption = false;
			return settlement.IsTown || settlement.IsCastle;
		}

		// Token: 0x0600199B RID: 6555 RVA: 0x0008075D File Offset: 0x0007E95D
		private bool CanMainHeroRecruitTroops(Settlement settlement, out bool disableOption, out TextObject disabledText)
		{
			disabledText = null;
			disableOption = false;
			return true;
		}

		// Token: 0x0600199C RID: 6556 RVA: 0x00080766 File Offset: 0x0007E966
		private bool CanMainHeroCraft(Settlement settlement, out bool disableOption, out TextObject disabledText)
		{
			disableOption = false;
			disabledText = null;
			return Campaign.Current.IsCraftingEnabled;
		}

		// Token: 0x0600199D RID: 6557 RVA: 0x00080778 File Offset: 0x0007E978
		private bool CanMainHeroJoinTournament(Settlement settlement, out bool disableOption, out TextObject disabledText)
		{
			bool flag = settlement.Town.HasTournament && Campaign.Current.IsDay;
			disableOption = false;
			disabledText = null;
			if (!flag)
			{
				return false;
			}
			if (Campaign.Current.IsMainHeroDisguised)
			{
				disableOption = true;
				disabledText = new TextObject("{=mu6Xl4RS}You cannot enter the tournament while disguised.", null);
				return false;
			}
			if (Hero.MainHero.IsWounded)
			{
				disableOption = true;
				disabledText = new TextObject("{=68rmPu7Z}Your health is too low to fight.", null);
				return false;
			}
			return true;
		}

		// Token: 0x0600199E RID: 6558 RVA: 0x000807E6 File Offset: 0x0007E9E6
		private bool CanMainHeroWatchTournament(Settlement settlement, out bool disableOption, out TextObject disabledText)
		{
			disableOption = false;
			disabledText = null;
			return settlement.Town.HasTournament && Campaign.Current.IsDay;
		}

		// Token: 0x0600199F RID: 6559 RVA: 0x00080807 File Offset: 0x0007EA07
		private bool CanMainHeroTrade(Settlement settlement, out bool disableOption, out TextObject disabledText)
		{
			if (Campaign.Current.IsMainHeroDisguised)
			{
				disableOption = true;
				disabledText = new TextObject("{=shU7OlQT}You cannot trade while in disguise.", null);
				return false;
			}
			disableOption = false;
			disabledText = null;
			return true;
		}

		// Token: 0x060019A0 RID: 6560 RVA: 0x00080830 File Offset: 0x0007EA30
		private bool CanMainHeroWaitInSettlement(Settlement settlement, out bool disableOption, out TextObject disabledText)
		{
			disableOption = false;
			disabledText = null;
			if (Campaign.Current.IsMainHeroDisguised)
			{
				disableOption = true;
				disabledText = new TextObject("{=dN5Qc9vN}You cannot wait in town while disguised.", null);
				return false;
			}
			if (settlement.IsVillage && settlement.Party.MapEvent != null)
			{
				disableOption = true;
				disabledText = new TextObject("{=dN5Qc7vN}You cannot wait in village while it is being raided.", null);
				return false;
			}
			return MobileParty.MainParty.Army == null || MobileParty.MainParty.Army.LeaderParty == MobileParty.MainParty;
		}
	}
}
