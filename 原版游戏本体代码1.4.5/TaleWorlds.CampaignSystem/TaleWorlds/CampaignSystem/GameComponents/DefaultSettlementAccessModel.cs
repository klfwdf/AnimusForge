using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultSettlementAccessModel : SettlementAccessModel
{
	public override void CanMainHeroEnterSettlement(Settlement settlement, out AccessDetails accessDetails)
	{
		if (settlement.IsFortification && Hero.MainHero.MapFaction == settlement.MapFaction && (settlement.Town.GarrisonParty == null || settlement.Town.GarrisonParty.Party.NumberOfAllMembers == 0))
		{
			accessDetails = new AccessDetails
			{
				AccessLevel = AccessLevel.FullAccess,
				AccessMethod = AccessMethod.Direct
			};
		}
		else if (settlement.IsTown)
		{
			CanMainHeroEnterTown(settlement, out accessDetails);
		}
		else if (settlement.IsCastle)
		{
			CanMainHeroEnterCastle(settlement, out accessDetails);
		}
		else if (settlement.IsVillage)
		{
			CanMainHeroEnterVillage(settlement, out accessDetails);
		}
		else
		{
			Debug.FailedAssert("Invalid type of settlement", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\GameComponents\\DefaultSettlementAccessModel.cs", "CanMainHeroEnterSettlement", 42);
			accessDetails = new AccessDetails
			{
				AccessLevel = AccessLevel.FullAccess,
				AccessMethod = AccessMethod.Direct
			};
		}
	}

	public override void CanMainHeroEnterDungeon(Settlement settlement, out AccessDetails accessDetails)
	{
		accessDetails = default(AccessDetails);
		CanMainHeroEnterKeepInternal(settlement, out accessDetails);
	}

	public override void CanMainHeroEnterLordsHall(Settlement settlement, out AccessDetails accessDetails)
	{
		accessDetails = default(AccessDetails);
		CanMainHeroEnterKeepInternal(settlement, out accessDetails);
	}

	private void CanMainHeroEnterKeepInternal(Settlement settlement, out AccessDetails accessDetails)
	{
		accessDetails = default(AccessDetails);
		Hero mainHero = Hero.MainHero;
		if (settlement.OwnerClan == mainHero.Clan)
		{
			accessDetails = new AccessDetails
			{
				AccessLevel = AccessLevel.FullAccess,
				AccessMethod = AccessMethod.Direct
			};
		}
		else if (DiplomacyHelper.IsSameFactionAndNotEliminated(mainHero.MapFaction, settlement.MapFaction))
		{
			accessDetails = new AccessDetails
			{
				AccessLevel = AccessLevel.FullAccess,
				AccessMethod = AccessMethod.Direct
			};
		}
		else if (FactionManager.IsNeutralWithFaction(mainHero.MapFaction, settlement.MapFaction))
		{
			if (Campaign.Current.IsMainHeroDisguised)
			{
				accessDetails = new AccessDetails
				{
					AccessLevel = AccessLevel.LimitedAccess,
					LimitedAccessSolution = LimitedAccessSolution.Disguise,
					AccessLimitationReason = AccessLimitationReason.Disguised
				};
			}
			else if (Campaign.Current.Models.CrimeModel.DoesPlayerHaveAnyCrimeRating(settlement.MapFaction))
			{
				accessDetails = new AccessDetails
				{
					AccessLevel = AccessLevel.LimitedAccess,
					LimitedAccessSolution = LimitedAccessSolution.Bribe,
					AccessLimitationReason = AccessLimitationReason.CrimeRating
				};
			}
			else if (mainHero.Clan.Tier < 3)
			{
				accessDetails = new AccessDetails
				{
					AccessLevel = AccessLevel.LimitedAccess,
					LimitedAccessSolution = LimitedAccessSolution.Bribe,
					AccessLimitationReason = AccessLimitationReason.ClanTier
				};
			}
			else
			{
				accessDetails = new AccessDetails
				{
					AccessLevel = AccessLevel.FullAccess,
					AccessMethod = AccessMethod.Direct
				};
			}
		}
		else if (FactionManager.IsAtWarAgainstFaction(mainHero.MapFaction, settlement.MapFaction))
		{
			accessDetails = new AccessDetails
			{
				AccessLevel = AccessLevel.LimitedAccess,
				LimitedAccessSolution = LimitedAccessSolution.Disguise,
				AccessLimitationReason = AccessLimitationReason.Disguised
			};
		}
		if (accessDetails.AccessLevel == AccessLevel.LimitedAccess && (accessDetails.LimitedAccessSolution == LimitedAccessSolution.Bribe || accessDetails.LimitedAccessSolution == LimitedAccessSolution.Disguise) && settlement.LocationComplex.GetListOfCharactersInLocation("lordshall").IsEmpty() && settlement.LocationComplex.GetListOfCharactersInLocation("prison").IsEmpty())
		{
			accessDetails.AccessLevel = AccessLevel.NoAccess;
			accessDetails.AccessLimitationReason = AccessLimitationReason.LocationEmpty;
		}
	}

	public override bool CanMainHeroAccessLocation(Settlement settlement, string locationId, out bool disableOption, out TextObject disabledText)
	{
		disabledText = null;
		disableOption = false;
		bool result = true;
		switch (locationId)
		{
		case "center":
			result = CanMainHeroWalkAroundTownCenter(settlement, out disableOption, out disabledText);
			break;
		case "arena":
			result = CanMainHeroGoToArena(settlement, out disableOption, out disabledText);
			break;
		case "tavern":
			result = CanMainHeroGoToTavern(settlement, out disableOption, out disabledText);
			break;
		case "lordshall":
		{
			CanMainHeroEnterLordsHall(settlement, out var accessDetails);
			result = ((accessDetails.AccessLevel != AccessLevel.LimitedAccess || accessDetails.LimitedAccessSolution != LimitedAccessSolution.Bribe) ? (accessDetails.AccessLevel == AccessLevel.FullAccess) : (Campaign.Current.Models.BribeCalculationModel.GetBribeToEnterLordsHall(settlement) == 0));
			break;
		}
		case "prison":
		{
			CanMainHeroEnterDungeon(settlement, out var accessDetails2);
			result = ((accessDetails2.AccessLevel != AccessLevel.LimitedAccess || accessDetails2.LimitedAccessSolution != LimitedAccessSolution.Bribe) ? (accessDetails2.AccessLevel == AccessLevel.FullAccess) : (Campaign.Current.Models.BribeCalculationModel.GetBribeToEnterDungeon(settlement) == 0));
			break;
		}
		case "house_1":
		case "house_2":
		case "house_3":
		{
			Location locationWithId = settlement.LocationComplex.GetLocationWithId(locationId);
			result = locationWithId.IsReserved && (locationWithId.SpecialItems.Count > 0 || locationWithId.GetCharacterList().Any());
			break;
		}
		case "port":
			disableOption = true;
			disabledText = new TextObject("{=ILnr9eCQ}Door is locked!");
			result = false;
			break;
		default:
			Debug.FailedAssert("invalid location which is not supported by DefaultSettlementAccessModel", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\GameComponents\\DefaultSettlementAccessModel.cs", "CanMainHeroAccessLocation", 207);
			break;
		}
		return result;
	}

	public override bool IsRequestMeetingOptionAvailable(Settlement settlement, out bool disableOption, out TextObject disabledText)
	{
		bool result = true;
		disableOption = false;
		disabledText = null;
		CanMainHeroEnterSettlement(settlement, out var accessDetails);
		if (settlement.OwnerClan == Clan.PlayerClan)
		{
			result = false;
		}
		else if (DiplomacyHelper.IsSameFactionAndNotEliminated(settlement.MapFaction, Clan.PlayerClan.MapFaction) && accessDetails.AccessLevel == AccessLevel.NoAccess)
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
			disabledText = new TextObject("{=bdzZUVxf}Your clan tier is not high enough to request a meeting.");
			result = true;
		}
		else if (TownHelpers.IsThereAnyoneToMeetInTown(settlement))
		{
			result = true;
		}
		else
		{
			disableOption = true;
			disabledText = new TextObject("{=196tGVIm}There are no nobles to meet.");
		}
		return result;
	}

	public override bool CanMainHeroDoSettlementAction(Settlement settlement, SettlementAction settlementAction, out bool disableOption, out TextObject disabledText)
	{
		switch (settlementAction)
		{
		case SettlementAction.RecruitTroops:
			return CanMainHeroRecruitTroops(settlement, out disableOption, out disabledText);
		case SettlementAction.Craft:
			return CanMainHeroCraft(settlement, out disableOption, out disabledText);
		case SettlementAction.JoinTournament:
			return CanMainHeroJoinTournament(settlement, out disableOption, out disabledText);
		case SettlementAction.WatchTournament:
			return CanMainHeroWatchTournament(settlement, out disableOption, out disabledText);
		case SettlementAction.Trade:
			return CanMainHeroTrade(settlement, out disableOption, out disabledText);
		case SettlementAction.WaitInSettlement:
			return CanMainHeroWaitInSettlement(settlement, out disableOption, out disabledText);
		case SettlementAction.ManageTown:
			return CanMainHeroManageTown(settlement, out disableOption, out disabledText);
		case SettlementAction.WalkAroundTheArena:
			return CanMainHeroEnterArena(settlement, out disableOption, out disabledText);
		default:
			Debug.FailedAssert("Invalid Settlement Action", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\GameComponents\\DefaultSettlementAccessModel.cs", "CanMainHeroDoSettlementAction", 276);
			disableOption = false;
			disabledText = null;
			return true;
		}
	}

	private bool CanMainHeroGoToArena(Settlement settlement, out bool disableOption, out TextObject disabledText)
	{
		if (Campaign.Current.IsMainHeroDisguised)
		{
			disabledText = new TextObject("{=brzz79Je}You cannot enter arena while in disguise.");
			disableOption = true;
			return false;
		}
		if (Campaign.Current.IsDay)
		{
			disabledText = null;
			disableOption = false;
			return true;
		}
		disabledText = new TextObject("{=wsbkjJhz}Arena is closed at night.");
		disableOption = true;
		return false;
	}

	private bool CanMainHeroGoToTavern(Settlement settlement, out bool disableOption, out TextObject disabledText)
	{
		disabledText = null;
		disableOption = false;
		return true;
	}

	private bool CanMainHeroEnterArena(Settlement settlement, out bool disableOption, out TextObject disabledText)
	{
		disableOption = false;
		disabledText = null;
		return true;
	}

	private void CanMainHeroEnterVillage(Settlement settlement, out AccessDetails accessDetails)
	{
		Hero mainHero = Hero.MainHero;
		accessDetails = new AccessDetails
		{
			AccessLevel = AccessLevel.NoAccess,
			AccessLimitationReason = AccessLimitationReason.None,
			PreliminaryActionObligation = PreliminaryActionObligation.None,
			PreliminaryActionType = PreliminaryActionType.None
		};
		MobileParty partyBelongedTo = mainHero.PartyBelongedTo;
		if (partyBelongedTo != null && (partyBelongedTo.Army == null || partyBelongedTo.Army.LeaderParty == partyBelongedTo))
		{
			accessDetails.AccessLevel = AccessLevel.FullAccess;
			accessDetails.AccessMethod = AccessMethod.Direct;
		}
		if (settlement.Village.VillageState == Village.VillageStates.Looted)
		{
			accessDetails.AccessLevel = AccessLevel.NoAccess;
			accessDetails.AccessLimitationReason = AccessLimitationReason.VillageIsLooted;
		}
	}

	private bool CanMainHeroManageTown(Settlement settlement, out bool disableOption, out TextObject disabledText)
	{
		disabledText = null;
		disableOption = false;
		if (settlement.IsTown)
		{
			return settlement.OwnerClan.Leader == Hero.MainHero;
		}
		return false;
	}

	private void CanMainHeroEnterCastle(Settlement settlement, out AccessDetails accessDetails)
	{
		Hero mainHero = Hero.MainHero;
		accessDetails = default(AccessDetails);
		if (settlement.OwnerClan == mainHero.Clan)
		{
			accessDetails = new AccessDetails
			{
				AccessLevel = AccessLevel.FullAccess,
				AccessMethod = AccessMethod.Direct
			};
		}
		else if (DiplomacyHelper.IsSameFactionAndNotEliminated(mainHero.MapFaction, settlement.MapFaction))
		{
			accessDetails = new AccessDetails
			{
				AccessLevel = AccessLevel.FullAccess,
				AccessMethod = AccessMethod.ByRequest
			};
			if (!settlement.Town.IsOwnerUnassigned && settlement.OwnerClan.Leader.GetRelationWithPlayer() < -4f && Hero.MainHero.MapFaction.Leader != Hero.MainHero)
			{
				accessDetails.AccessLevel = AccessLevel.NoAccess;
				accessDetails.AccessLimitationReason = AccessLimitationReason.RelationshipWithOwner;
			}
		}
		else if (FactionManager.IsNeutralWithFaction(mainHero.MapFaction, settlement.MapFaction))
		{
			accessDetails = new AccessDetails
			{
				AccessLevel = AccessLevel.FullAccess,
				AccessMethod = AccessMethod.ByRequest
			};
			if (Campaign.Current.Models.CrimeModel.DoesPlayerHaveAnyCrimeRating(settlement.MapFaction))
			{
				accessDetails.AccessLevel = AccessLevel.NoAccess;
				accessDetails.AccessLimitationReason = AccessLimitationReason.CrimeRating;
			}
			else if (settlement.OwnerClan.Leader.GetRelationWithPlayer() < 0f)
			{
				accessDetails.AccessLevel = AccessLevel.NoAccess;
				accessDetails.AccessLimitationReason = AccessLimitationReason.RelationshipWithOwner;
			}
		}
		else if (FactionManager.IsAtWarAgainstFaction(mainHero.MapFaction, settlement.MapFaction))
		{
			accessDetails = new AccessDetails
			{
				AccessLevel = AccessLevel.NoAccess,
				AccessMethod = AccessMethod.ByRequest,
				AccessLimitationReason = AccessLimitationReason.HostileFaction
			};
		}
	}

	private void CanMainHeroEnterTown(Settlement settlement, out AccessDetails accessDetails)
	{
		Hero mainHero = Hero.MainHero;
		accessDetails = default(AccessDetails);
		if (settlement.OwnerClan == mainHero.Clan)
		{
			accessDetails = new AccessDetails
			{
				AccessLevel = AccessLevel.FullAccess,
				AccessMethod = AccessMethod.Direct
			};
		}
		else if (DiplomacyHelper.IsSameFactionAndNotEliminated(mainHero.MapFaction, settlement.MapFaction))
		{
			accessDetails = new AccessDetails
			{
				AccessLevel = AccessLevel.FullAccess,
				AccessMethod = AccessMethod.Direct
			};
			if (Campaign.Current.Models.CrimeModel.IsPlayerCrimeRatingModerate(settlement.MapFaction) || Campaign.Current.Models.CrimeModel.IsPlayerCrimeRatingSevere(settlement.MapFaction))
			{
				accessDetails.PreliminaryActionType = PreliminaryActionType.FaceCharges;
				accessDetails.PreliminaryActionObligation = PreliminaryActionObligation.Optional;
			}
		}
		else if (FactionManager.IsNeutralWithFaction(mainHero.MapFaction, settlement.MapFaction))
		{
			accessDetails = new AccessDetails
			{
				AccessLevel = AccessLevel.FullAccess,
				AccessMethod = AccessMethod.Direct
			};
			if (Campaign.Current.Models.CrimeModel.IsPlayerCrimeRatingModerate(settlement.MapFaction) || Campaign.Current.Models.CrimeModel.IsPlayerCrimeRatingSevere(settlement.MapFaction))
			{
				accessDetails.AccessLevel = AccessLevel.LimitedAccess;
				accessDetails.AccessMethod = AccessMethod.None;
				accessDetails.LimitedAccessSolution = LimitedAccessSolution.Disguise;
				accessDetails.AccessLimitationReason = AccessLimitationReason.CrimeRating;
			}
		}
		else if (FactionManager.IsAtWarAgainstFaction(mainHero.MapFaction, settlement.MapFaction))
		{
			accessDetails = new AccessDetails
			{
				AccessLevel = AccessLevel.LimitedAccess,
				LimitedAccessSolution = LimitedAccessSolution.Disguise,
				AccessLimitationReason = AccessLimitationReason.HostileFaction
			};
		}
	}

	private bool CanMainHeroWalkAroundTownCenter(Settlement settlement, out bool disableOption, out TextObject disabledText)
	{
		disabledText = null;
		disableOption = false;
		if (!settlement.IsTown)
		{
			return settlement.IsCastle;
		}
		return true;
	}

	private bool CanMainHeroRecruitTroops(Settlement settlement, out bool disableOption, out TextObject disabledText)
	{
		disabledText = null;
		disableOption = false;
		if (settlement.IsVillage && settlement.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
		{
			disabledText = new TextObject("{=nHlqkdUC}You cannot recruit troops from a hostile village.");
			disableOption = true;
			return false;
		}
		if (Settlement.CurrentSettlement.IsVillage)
		{
			return Settlement.CurrentSettlement.Village.VillageState == Village.VillageStates.Normal;
		}
		return true;
	}

	private bool CanMainHeroCraft(Settlement settlement, out bool disableOption, out TextObject disabledText)
	{
		disableOption = false;
		disabledText = null;
		return Campaign.Current.IsCraftingEnabled;
	}

	private bool CanMainHeroJoinTournament(Settlement settlement, out bool disableOption, out TextObject disabledText)
	{
		bool num = settlement.Town.HasTournament && Campaign.Current.IsDay;
		disableOption = false;
		disabledText = null;
		if (!num)
		{
			return false;
		}
		if (Campaign.Current.IsMainHeroDisguised)
		{
			disableOption = true;
			disabledText = new TextObject("{=mu6Xl4RS}You cannot enter the tournament while disguised.");
			return false;
		}
		if (Hero.MainHero.IsWounded)
		{
			disableOption = true;
			disabledText = new TextObject("{=68rmPu7Z}Your health is too low to fight.");
			return false;
		}
		return true;
	}

	private bool CanMainHeroWatchTournament(Settlement settlement, out bool disableOption, out TextObject disabledText)
	{
		disableOption = false;
		disabledText = null;
		if (settlement.Town.HasTournament)
		{
			return Campaign.Current.IsDay;
		}
		return false;
	}

	private bool CanMainHeroTrade(Settlement settlement, out bool disableOption, out TextObject disabledText)
	{
		if (settlement.IsVillage)
		{
			Village village = Settlement.CurrentSettlement.Village;
			if (village.VillageState == Village.VillageStates.BeingRaided)
			{
				disabledText = null;
				disableOption = false;
				return false;
			}
			bool flag = settlement.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction);
			TextObject textObject = new TextObject("{=vYHVlydb}You cannot trade with a hostile village.");
			if (village.VillageState == Village.VillageStates.Normal && village.Owner.ItemRoster.Count > 0)
			{
				disableOption = flag;
				disabledText = (disableOption ? textObject : null);
				return !disableOption;
			}
			if (village.Gold > 0)
			{
				disableOption = flag;
				disabledText = (disableOption ? textObject : new TextObject("{=FbowXAC0}There are no available products right now."));
				return false;
			}
			disableOption = flag;
			disabledText = (disableOption ? textObject : new TextObject("{=bmfo7CaO}Village shop is not available right now."));
			return false;
		}
		if (Campaign.Current.IsMainHeroDisguised && !Hero.MainHero.GetPerkValue(DefaultPerks.Roguery.SmugglerConnections))
		{
			disableOption = true;
			TextObject textObject2 = new TextObject("{=HVUHHuVA}{PERK_NAME} perk required to trade while in disguise.");
			textObject2.SetTextVariable("PERK_NAME", DefaultPerks.Roguery.SmugglerConnections.Name);
			disabledText = textObject2;
			return false;
		}
		disableOption = false;
		disabledText = null;
		return true;
	}

	private bool CanMainHeroWaitInSettlement(Settlement settlement, out bool disableOption, out TextObject disabledText)
	{
		disableOption = false;
		disabledText = null;
		if (Campaign.Current.IsMainHeroDisguised)
		{
			disableOption = true;
			disabledText = new TextObject("{=dN5Qc9vN}You cannot wait in town while in disguise.");
			return false;
		}
		if (settlement.IsVillage)
		{
			if (settlement.Party.MapEvent != null)
			{
				disableOption = true;
				disabledText = new TextObject("{=dN5Qc7vN}You cannot wait in village while it is being raided.");
				return false;
			}
			if (settlement.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
			{
				disableOption = true;
				disabledText = new TextObject("{=*}You cannot wait in a hostile village.");
				return false;
			}
		}
		if (MobileParty.MainParty.Army != null && MobileParty.MainParty.Army.LeaderParty != MobileParty.MainParty)
		{
			disableOption = true;
			disabledText = new TextObject("{=tjsld7WT}You cannot wait in town while you're a member of an army.");
			return false;
		}
		return true;
	}
}
