using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class SettlementAccessModel : MBGameModel<SettlementAccessModel>
{
	public enum AccessLevel
	{
		NoAccess,
		LimitedAccess,
		FullAccess
	}

	public enum AccessMethod
	{
		None,
		Direct,
		ByRequest
	}

	public enum AccessLimitationReason
	{
		None,
		HostileFaction,
		RelationshipWithOwner,
		CrimeRating,
		VillageIsLooted,
		Disguised,
		ClanTier,
		LocationEmpty
	}

	public enum LimitedAccessSolution
	{
		None,
		Bribe,
		Disguise
	}

	public enum PreliminaryActionObligation
	{
		None,
		Optional
	}

	public enum PreliminaryActionType
	{
		None,
		FaceCharges
	}

	public enum SettlementAction
	{
		RecruitTroops,
		Craft,
		WalkAroundTheArena,
		JoinTournament,
		WatchTournament,
		Trade,
		WaitInSettlement,
		ManageTown
	}

	public struct AccessDetails
	{
		public AccessLevel AccessLevel;

		public AccessMethod AccessMethod;

		public AccessLimitationReason AccessLimitationReason;

		public LimitedAccessSolution LimitedAccessSolution;

		public PreliminaryActionObligation PreliminaryActionObligation;

		public PreliminaryActionType PreliminaryActionType;
	}

	public abstract void CanMainHeroEnterSettlement(Settlement settlement, out AccessDetails accessDetails);

	public abstract void CanMainHeroEnterLordsHall(Settlement settlement, out AccessDetails accessDetails);

	public abstract void CanMainHeroEnterDungeon(Settlement settlement, out AccessDetails accessDetails);

	public abstract bool CanMainHeroAccessLocation(Settlement settlement, string locationId, out bool disableOption, out TextObject disabledText);

	public abstract bool CanMainHeroDoSettlementAction(Settlement settlement, SettlementAction settlementAction, out bool disableOption, out TextObject disabledText);

	public abstract bool IsRequestMeetingOptionAvailable(Settlement settlement, out bool disableOption, out TextObject disabledText);
}
