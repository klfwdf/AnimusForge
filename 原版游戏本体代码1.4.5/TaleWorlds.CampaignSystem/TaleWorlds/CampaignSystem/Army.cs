using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem;

public class Army : ITrackableCampaignObject, ITrackableBase
{
	public enum ArmyTypes
	{
		Besieger,
		Raider,
		Defender,
		Patrolling,
		NumberOfArmyTypes
	}

	public enum ArmyDispersionReason
	{
		Unknown,
		DismissalRequestedWithInfluence,
		NotEnoughParty,
		KingdomChanged,
		CohesionDepleted,
		ObjectiveFinished,
		LeaderPartyRemoved,
		PlayerTakenPrisoner,
		CannotElectNewLeader,
		LeaderCannotArrivePointOnTime,
		ArmyLeaderIsDead,
		FoodProblem,
		NotEnoughTroop,
		NoActiveWar,
		NoShipToUse,
		Inactivity
	}

	private const float CheckingForBoostingCohesionThreshold = 50f;

	private const float DisbandCohesionThreshold = 30f;

	private const float StrengthThresholdRatioForGathering = 0.9f;

	private const float StrengthThresholdRatioForGatheringAfterTimeThreshold = 0.75f;

	[SaveableField(1)]
	private readonly MBList<MobileParty> _parties;

	[SaveableField(19)]
	private CampaignTime _creationTime;

	[SaveableField(7)]
	private float _armyGatheringStartTime;

	[SaveableField(10)]
	private bool _armyIsDispersing;

	[SaveableField(11)]
	private int _numberOfBoosts;

	[SaveableField(15)]
	private Kingdom _kingdom;

	[SaveableField(16)]
	private IMapPoint _aiBehaviorObject;

	[SaveableField(20)]
	private int _inactivityCounter;

	[CachedData]
	private MBCampaignEvent _hourlyTickEvent;

	[CachedData]
	private MBCampaignEvent _tickEvent;

	private float MinimumDistanceToTargetWhileGatheringAsAttackerArmy => Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(LeaderParty.NavigationCapability) * 0.66f;

	public float GatheringPositionMaxDistanceToTheSettlement => Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(LeaderParty.NavigationCapability) * 0.2f;

	public float GatheringPositionMinDistanceToTheSettlement => Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(LeaderParty.NavigationCapability) * 0.1f;

	public MBReadOnlyList<MobileParty> Parties => _parties;

	public TextObject EncyclopediaLinkWithName => ArmyOwner.EncyclopediaLinkWithName;

	[SaveableProperty(3)]
	public ArmyTypes ArmyType { get; set; }

	[SaveableProperty(4)]
	public Hero ArmyOwner { get; set; }

	[SaveableProperty(5)]
	public float Cohesion { get; set; }

	public float DailyCohesionChange => Campaign.Current.Models.ArmyManagementCalculationModel.CalculateDailyCohesionChange(this).ResultNumber;

	public ExplainedNumber DailyCohesionChangeExplanation => Campaign.Current.Models.ArmyManagementCalculationModel.CalculateDailyCohesionChange(this, includeDescriptions: true);

	public int CohesionThresholdForDispersion => Campaign.Current.Models.ArmyManagementCalculationModel.CohesionThresholdForDispersion;

	[SaveableProperty(13)]
	public float Morale { get; private set; }

	[SaveableProperty(14)]
	public MobileParty LeaderParty { get; private set; }

	public int LeaderPartyAndAttachedPartiesCount => LeaderParty.AttachedParties.Count + 1;

	public float EstimatedStrength
	{
		get
		{
			float num = LeaderParty.Party.EstimatedStrength;
			foreach (MobileParty attachedParty in LeaderParty.AttachedParties)
			{
				num += attachedParty.Party.EstimatedStrength;
			}
			return num;
		}
	}

	public Kingdom Kingdom
	{
		get
		{
			return _kingdom;
		}
		set
		{
			if (value != _kingdom)
			{
				_kingdom?.RemoveArmyInternal(this);
				_kingdom = value;
				_kingdom?.AddArmyInternal(this);
			}
		}
	}

	public IMapPoint AiBehaviorObject
	{
		get
		{
			return _aiBehaviorObject;
		}
		set
		{
			if (value != _aiBehaviorObject && Parties.Contains(MobileParty.MainParty) && LeaderParty != MobileParty.MainParty)
			{
				StopTrackingTargetSettlement();
				StartTrackingTargetSettlement(value);
			}
			_aiBehaviorObject = value;
		}
	}

	[SaveableProperty(17)]
	public TextObject Name { get; private set; }

	private float InactivityThreshold => (float)CampaignTime.HoursInDay * 2f;

	public int TotalHealthyMembers => LeaderParty.Party.NumberOfHealthyMembers + LeaderParty.AttachedParties.Sum((MobileParty mobileParty) => mobileParty.Party.NumberOfHealthyMembers);

	public int TotalManCount => LeaderParty.Party.MemberRoster.TotalManCount + LeaderParty.AttachedParties.Sum((MobileParty mobileParty) => mobileParty.Party.MemberRoster.TotalManCount);

	public int TotalRegularCount => LeaderParty.Party.MemberRoster.TotalRegulars + LeaderParty.AttachedParties.Sum((MobileParty mobileParty) => mobileParty.Party.MemberRoster.TotalRegulars);

	public bool IsReady => true;

	public override string ToString()
	{
		return Name.ToString();
	}

	public float CalculateCurrentStrength()
	{
		float num = LeaderParty.Party.CalculateCurrentStrength();
		foreach (MobileParty attachedParty in LeaderParty.AttachedParties)
		{
			num += attachedParty.Party.CalculateCurrentStrength();
		}
		return num;
	}

	public float GetCustomStrength(BattleSideEnum side, MapEvent.PowerCalculationContext context)
	{
		float num = LeaderParty.Party.GetCustomStrength(side, context);
		foreach (MobileParty attachedParty in LeaderParty.AttachedParties)
		{
			num += attachedParty.Party.GetCustomStrength(side, context);
		}
		return num;
	}

	public Army(Kingdom kingdom, MobileParty leaderParty, ArmyTypes armyType)
	{
		Kingdom = kingdom;
		_parties = new MBList<MobileParty>();
		_armyGatheringStartTime = 0f;
		_creationTime = CampaignTime.Now;
		LeaderParty = leaderParty;
		LeaderParty.Army = this;
		ArmyOwner = LeaderParty.LeaderHero;
		UpdateName();
		ArmyType = armyType;
		AddEventHandlers();
		Cohesion = 100f;
	}

	public void UpdateName()
	{
		Name = new TextObject("{=nbmctMLk}{LEADER_NAME}{.o} Army");
		Name.SetTextVariable("LEADER_NAME", (ArmyOwner != null) ? ArmyOwner.Name : ((LeaderParty.Owner != null) ? LeaderParty.Owner.Name : TextObject.GetEmpty()));
	}

	private void AddEventHandlers()
	{
		if (_creationTime == default(CampaignTime))
		{
			_creationTime = CampaignTime.HoursFromNow(MBRandom.RandomFloat - 2f);
		}
		CampaignTime campaignTime = CampaignTime.Now - _creationTime;
		CampaignTime initialWait = CampaignTime.Hours(1f + (float)(int)campaignTime.ToHours) - campaignTime;
		_hourlyTickEvent = CampaignPeriodicEventManager.CreatePeriodicEvent(CampaignTime.Hours(1f), initialWait);
		_hourlyTickEvent.AddHandler(HourlyTick);
		_tickEvent = CampaignPeriodicEventManager.CreatePeriodicEvent(CampaignTime.Hours(0.1f), CampaignTime.Hours(1f));
		_tickEvent.AddHandler(Tick);
		CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
		CampaignEvents.OnSiegeEventStartedEvent.AddNonSerializedListener(this, OnSiegeStarted);
	}

	private void OnSiegeStarted(SiegeEvent siegeEvent)
	{
		if (IsWaitingForArmyMembers() && AiBehaviorObject is Settlement settlement && settlement == siegeEvent.BesiegedSettlement && LeaderParty.SiegeEvent == null && LeaderParty.MapEvent == null)
		{
			FindBestGatheringSettlementAndMoveTheLeader(settlement);
		}
	}

	private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
	{
		if (IsWaitingForArmyMembers() && AiBehaviorObject is Settlement settlement2 && settlement2 == settlement && settlement.MapFaction != LeaderParty.MapFaction && LeaderParty.SiegeEvent == null && LeaderParty.MapEvent == null)
		{
			FindBestGatheringSettlementAndMoveTheLeader(settlement);
		}
	}

	internal void OnAfterLoad()
	{
		AddEventHandlers();
	}

	public bool DoesLeaderPartyAndAttachedPartiesContain(MobileParty party)
	{
		if (LeaderParty != party)
		{
			return LeaderParty.AttachedParties.IndexOf(party) >= 0;
		}
		return true;
	}

	public void BoostCohesionWithInfluence(float cohesionToGain, int cost)
	{
		if (LeaderParty.LeaderHero.Clan.Influence >= (float)cost)
		{
			ChangeClanInfluenceAction.Apply(LeaderParty.LeaderHero.Clan, -cost);
			Cohesion += cohesionToGain;
			_numberOfBoosts++;
		}
	}

	private void ThinkAboutCohesionBoost()
	{
		float num = 0f;
		foreach (MobileParty party in Parties)
		{
			float partySizeRatio = party.PartySizeRatio;
			num += partySizeRatio;
		}
		float b = num / (float)Parties.Count;
		float num2 = MathF.Min(1f, b);
		float num3 = Campaign.Current.Models.TargetScoreCalculatingModel.CurrentObjectiveValue(LeaderParty);
		if (!(num3 > 0.01f))
		{
			return;
		}
		num3 *= num2;
		num3 *= ((_numberOfBoosts == 0) ? 1f : (1f / MathF.Pow(1f + (float)_numberOfBoosts, 0.7f)));
		ArmyManagementCalculationModel armyManagementCalculationModel = Campaign.Current.Models.ArmyManagementCalculationModel;
		float num4 = MathF.Min(100f, 100f - Cohesion);
		int num5 = armyManagementCalculationModel.CalculateTotalInfluenceCost(this, num4);
		if (!(LeaderParty.Party.Owner.Clan.Influence > (float)num5))
		{
			return;
		}
		float num6 = MathF.Min(9f, MathF.Sqrt(LeaderParty.Party.Owner.Clan.Influence / (float)num5));
		float num7 = ((LeaderParty.BesiegedSettlement != null) ? 2f : 1f);
		if (LeaderParty.BesiegedSettlement == null && LeaderParty.DefaultBehavior == AiBehavior.BesiegeSettlement)
		{
			float estimatedLandRatio;
			float num8 = ((LeaderParty.CurrentSettlement == null) ? Campaign.Current.Models.MapDistanceModel.GetDistance(LeaderParty, LeaderParty.TargetSettlement, LeaderParty.IsTargetingPort, LeaderParty.NavigationCapability, out estimatedLandRatio) : Campaign.Current.Models.MapDistanceModel.GetDistance(LeaderParty.CurrentSettlement, LeaderParty.TargetSettlement, LeaderParty.IsCurrentlyAtSea, LeaderParty.IsTargetingPort, LeaderParty.NavigationCapability));
			float num9 = Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(LeaderParty.NavigationCapability) * 2f;
			if (num8 < num9)
			{
				num7 += (1f - num8 / num9) * (1f - num8 / num9);
			}
		}
		float num10 = num3 * num7 * 0.25f * num6;
		if (MBRandom.RandomFloat < num10)
		{
			BoostCohesionWithInfluence(num4, num5);
		}
	}

	public void RecalculateArmyMorale()
	{
		float num = 0f;
		foreach (MobileParty party in Parties)
		{
			num += party.Morale;
		}
		Morale = num / (float)Parties.Count;
	}

	private void HourlyTick(MBCampaignEvent campaignEvent, object[] delegateParams)
	{
		bool flag = LeaderParty.CurrentSettlement != null && LeaderParty.CurrentSettlement.SiegeEvent != null;
		if (LeaderParty.MapEvent != null || flag)
		{
			return;
		}
		RecalculateArmyMorale();
		Cohesion += DailyCohesionChange / (float)CampaignTime.HoursInDay;
		if (LeaderParty != MobileParty.MainParty)
		{
			if (_armyGatheringStartTime == 0f)
			{
				CheckAndSetArmyGatheringTime();
			}
			MoveLeaderToGatheringLocationIfNeeded();
			if (Cohesion < 50f)
			{
				ThinkAboutCohesionBoost();
				if (Cohesion < 30f && LeaderParty.MapEvent == null && LeaderParty.SiegeEvent == null)
				{
					DisbandArmyAction.ApplyByCohesionDepleted(this);
					return;
				}
			}
			if (LeaderParty.DefaultBehavior == AiBehavior.BesiegeSettlement && IsAnotherEnemyBesiegingTarget())
			{
				FinishArmyObjective();
			}
		}
		CheckArmyDispersion();
		ApplyHostileActionInfluenceAwards();
	}

	private void CheckAndSetArmyGatheringTime()
	{
		if (AiBehaviorObject is Settlement toSettlement && LeaderParty.DefaultBehavior == AiBehavior.GoToPoint && ((LeaderParty.CurrentSettlement != null) ? Campaign.Current.Models.MapDistanceModel.GetDistance(LeaderParty.CurrentSettlement, toSettlement, isFromPort: false, isTargetingPort: false, LeaderParty.DesiredAiNavigationType) : Campaign.Current.Models.MapDistanceModel.GetDistance(LeaderParty, toSettlement, isTargetingPort: false, LeaderParty.DesiredAiNavigationType, out var _)) < GatheringPositionMaxDistanceToTheSettlement * 2f)
		{
			_armyGatheringStartTime = Campaign.CurrentTime;
		}
	}

	private void Tick(MBCampaignEvent campaignEvent, object[] delegateParams)
	{
		foreach (MobileParty party in _parties)
		{
			if (party.AttachedTo != null || party.Army == null || party.ShortTermTargetParty != LeaderParty || party.MapEvent != null || party.IsCurrentlyAtSea != LeaderParty.IsCurrentlyAtSea)
			{
				continue;
			}
			float num = (party.IsCurrentlyAtSea ? Campaign.Current.Models.EncounterModel.MaximumAllowedNavalDistanceForEncounteringMobilePartyInArmy : Campaign.Current.Models.EncounterModel.MaximumAllowedLandDistanceForEncounteringMobilePartyInArmy);
			if ((party.Position - LeaderParty.Position).LengthSquared < num)
			{
				AddPartyToMergedParties(party);
				if (party.IsMainParty)
				{
					Campaign.Current.CameraFollowParty = LeaderParty.Party;
				}
				CampaignEventDispatcher.Instance.OnArmyOverlaySetDirty();
			}
		}
	}

	private void CheckArmyDispersion()
	{
		if (LeaderParty == MobileParty.MainParty)
		{
			if (Cohesion <= 0.1f)
			{
				DisbandArmyAction.ApplyByCohesionDepleted(this);
			}
			return;
		}
		int num = (LeaderParty.Party.IsStarving ? 1 : 0);
		foreach (MobileParty attachedParty in LeaderParty.AttachedParties)
		{
			if (attachedParty.Party.IsStarving)
			{
				num++;
			}
		}
		if ((float)num / (float)LeaderPartyAndAttachedPartiesCount > 0.5f)
		{
			DisbandArmyAction.ApplyByFoodProblem(this);
			return;
		}
		if (MBRandom.RandomFloat < 0.25f && !LeaderParty.MapFaction.FactionsAtWarWith.AnyQ((IFaction x) => x.Fiefs.Any()))
		{
			DisbandArmyAction.ApplyByNoActiveWar(this);
			return;
		}
		if (Cohesion <= 0.1f)
		{
			DisbandArmyAction.ApplyByCohesionDepleted(this);
			return;
		}
		if (!LeaderParty.IsActive)
		{
			DisbandArmyAction.ApplyByUnknownReason(this);
		}
		CheckInactivity();
	}

	private void CheckInactivity()
	{
		if (!IsWaitingForArmyMembers())
		{
			switch (LeaderParty.DefaultBehavior)
			{
			case AiBehavior.Hold:
				_inactivityCounter++;
				break;
			case AiBehavior.GoToSettlement:
				if (!LeaderParty.TargetSettlement.MapFaction.IsAtWarWith(LeaderParty.MapFaction))
				{
					_inactivityCounter++;
				}
				break;
			case AiBehavior.AssaultSettlement:
			case AiBehavior.RaidSettlement:
			case AiBehavior.BesiegeSettlement:
			case AiBehavior.DefendSettlement:
				_inactivityCounter -= 2;
				break;
			case AiBehavior.PatrolAroundPoint:
				_inactivityCounter++;
				break;
			}
			AiBehavior shortTermBehavior = LeaderParty.ShortTermBehavior;
			if (shortTermBehavior == AiBehavior.EngageParty)
			{
				_inactivityCounter--;
			}
		}
		_inactivityCounter = MBMath.ClampInt(_inactivityCounter, 0, (int)InactivityThreshold);
		if ((float)_inactivityCounter >= InactivityThreshold)
		{
			DisbandArmyAction.ApplyByInactivity(this);
		}
	}

	private void MoveLeaderToGatheringLocationIfNeeded()
	{
		if (AiBehaviorObject != null && LeaderParty.SiegeEvent == null && LeaderParty.MapEvent == null && LeaderParty.ShortTermBehavior == AiBehavior.Hold && IsWaitingForArmyMembers())
		{
			Settlement settlement = AiBehaviorObject as Settlement;
			if (!settlement.IsUnderSiege && !settlement.IsUnderRaid)
			{
				CampaignVec2 centerPosition = (LeaderParty.IsTargetingPort ? settlement.PortPosition : settlement.GatePosition);
				SendLeaderPartyToReachablePointAroundPosition(centerPosition, 6f, 3f);
			}
			else
			{
				FindBestGatheringSettlementAndMoveTheLeader(settlement);
			}
		}
	}

	private void ApplyHostileActionInfluenceAwards()
	{
		if (LeaderParty.LeaderHero != null && LeaderParty.MapEvent != null)
		{
			if (LeaderParty.MapEvent.IsRaid && LeaderParty.MapEvent.DefenderSide.TroopCount == 0)
			{
				float hourlyInfluenceAwardForRaidingEnemyVillage = Campaign.Current.Models.DiplomacyModel.GetHourlyInfluenceAwardForRaidingEnemyVillage(LeaderParty);
				GainKingdomInfluenceAction.ApplyForRaidingEnemyVillage(LeaderParty, hourlyInfluenceAwardForRaidingEnemyVillage);
			}
			else if (LeaderParty.BesiegedSettlement != null && LeaderParty.MapFaction.IsAtWarWith(LeaderParty.BesiegedSettlement.MapFaction))
			{
				float hourlyInfluenceAwardForBesiegingEnemyFortification = Campaign.Current.Models.DiplomacyModel.GetHourlyInfluenceAwardForBesiegingEnemyFortification(LeaderParty);
				GainKingdomInfluenceAction.ApplyForBesiegingEnemySettlement(LeaderParty, hourlyInfluenceAwardForBesiegingEnemyFortification);
			}
		}
	}

	public TextObject GetNotificationText()
	{
		if (LeaderParty != MobileParty.MainParty)
		{
			TextObject textObject = GameTexts.FindText("str_army_gather");
			StringHelpers.SetCharacterProperties("ARMY_LEADER", LeaderParty.LeaderHero.CharacterObject, textObject);
			textObject.SetTextVariable("SETTLEMENT_NAME", AiBehaviorObject.Name);
			return textObject;
		}
		return null;
	}

	public TextObject GetLongTermBehaviorText(bool setWithLink = false)
	{
		if (LeaderParty.IsMainParty)
		{
			return GetLongTermBehaviorTextForPlayerParty();
		}
		return GetLongTermBehaviorTextForAILeadedParty(setWithLink);
	}

	private TextObject GetLongTermBehaviorTextForPlayerParty()
	{
		TextObject textObject;
		if (MobileParty.MainParty.TargetSettlement != null && MobileParty.MainParty.CurrentSettlement != MobileParty.MainParty.TargetSettlement)
		{
			textObject = GameTexts.FindText("str_army_going_to_settlement");
			textObject.SetTextVariable("SETTLEMENT_NAME", LeaderParty.Ai.AiBehaviorPartyBase.Name);
		}
		else if (MobileParty.MainParty.CurrentSettlement != null)
		{
			textObject = GameTexts.FindText("str_army_waiting_in_settlement");
			textObject.SetTextVariable("SETTLEMENT_NAME", MobileParty.MainParty.CurrentSettlement.Name);
		}
		else if (MobileParty.MainParty.TargetParty == null)
		{
			textObject = ((!MobileParty.MainParty.IsMoving) ? new TextObject("{=RClxLG6N}Holding.") : new TextObject("{=b9TbdM9A}Moving to a point."));
		}
		else
		{
			textObject = new TextObject("{=P4QFKVSU}Moving to {TARGET_PARTY}.");
			textObject.SetTextVariable("TARGET_PARTY", MobileParty.MainParty.TargetParty.Name);
		}
		return textObject;
	}

	private TextObject GetLongTermBehaviorTextForAILeadedParty(bool setWithLink)
	{
		switch (LeaderParty.DefaultBehavior)
		{
		case AiBehavior.Hold:
		case AiBehavior.GoToPoint:
			if (IsWaitingForArmyMembers() && AiBehaviorObject != null)
			{
				TextObject textObject = GameTexts.FindText("str_army_gathering");
				textObject.SetTextVariable("SETTLEMENT_NAME", setWithLink ? ((Settlement)AiBehaviorObject).EncyclopediaLinkWithName : AiBehaviorObject.Name);
				return textObject;
			}
			break;
		case AiBehavior.GoToSettlement:
		{
			TextObject textObject = ((LeaderParty.CurrentSettlement == null) ? GameTexts.FindText("str_army_going_to_settlement") : GameTexts.FindText("str_army_waiting_in_settlement"));
			textObject.SetTextVariable("SETTLEMENT_NAME", (setWithLink && AiBehaviorObject is Settlement) ? ((Settlement)AiBehaviorObject).EncyclopediaLinkWithName : (AiBehaviorObject.Name ?? LeaderParty.Ai.AiBehaviorPartyBase.Name));
			return textObject;
		}
		case AiBehavior.BesiegeSettlement:
		{
			Settlement settlement = (Settlement)AiBehaviorObject;
			TextObject textObject = GameTexts.FindText((LeaderParty.SiegeEvent != null) ? "str_army_besieging" : "str_army_besieging_travelling");
			if (settlement.IsVillage)
			{
				textObject = GameTexts.FindText("str_army_patrolling_travelling");
			}
			textObject.SetTextVariable("SETTLEMENT_NAME", setWithLink ? settlement.EncyclopediaLinkWithName : AiBehaviorObject.Name);
			return textObject;
		}
		case AiBehavior.RaidSettlement:
		{
			Settlement settlement2 = (Settlement)AiBehaviorObject;
			TextObject textObject = ((LeaderParty.MapEvent != null && LeaderParty.MapEvent.IsRaid) ? GameTexts.FindText("str_army_raiding") : GameTexts.FindText("str_army_raiding_travelling"));
			textObject.SetTextVariable("SETTLEMENT_NAME", setWithLink ? settlement2.EncyclopediaLinkWithName : AiBehaviorObject.Name);
			return textObject;
		}
		case AiBehavior.DefendSettlement:
		{
			Settlement settlement = (Settlement)AiBehaviorObject;
			TextObject textObject = ((LeaderParty.Position.Distance(settlement.Position) > Campaign.Current.EstimatedAverageLordPartySpeed * (float)CampaignTime.HoursInDay * 0.33f) ? GameTexts.FindText("str_army_defending_travelling") : GameTexts.FindText("str_army_defending"));
			textObject.SetTextVariable("SETTLEMENT_NAME", setWithLink ? settlement.EncyclopediaLinkWithName : AiBehaviorObject.Name);
			return textObject;
		}
		case AiBehavior.PatrolAroundPoint:
		{
			TextObject textObject = GameTexts.FindText("str_army_patrolling_travelling");
			textObject.SetTextVariable("SETTLEMENT_NAME", setWithLink ? ((Settlement)AiBehaviorObject).EncyclopediaLinkWithName : AiBehaviorObject.Name);
			return textObject;
		}
		}
		if (LeaderParty.MapEvent != null)
		{
			TextObject textObject;
			if (LeaderParty.MapEvent.MapEventSettlement != null)
			{
				textObject = ((LeaderParty.MapEventSide == LeaderParty.MapEvent.DefenderSide) ? new TextObject("{=rGy8vjOv}Defending {TARGET_SETTLEMENT}.") : new TextObject("{=exnL6SS7}Attacking {TARGET_SETTLEMENT}."));
				textObject.SetTextVariable("TARGET_SETTLEMENT", LeaderParty.MapEvent.MapEventSettlement.Name);
				return textObject;
			}
			textObject = new TextObject("{=5bzk75Ql}Engaging {TARGET_PARTY}.");
			textObject.SetTextVariable("TARGET_PARTY", (LeaderParty.MapEventSide == LeaderParty.MapEvent.DefenderSide) ? LeaderParty.MapEvent.AttackerSide.LeaderParty.Name : LeaderParty.MapEvent.DefenderSide.LeaderParty.Name);
			return textObject;
		}
		if (LeaderParty.SiegeEvent != null)
		{
			TextObject textObject = new TextObject("{=JTxI3sW2}Besieging {TARGET_SETTLEMENT}.");
			textObject.SetTextVariable("TARGET_SETTLEMENT", LeaderParty.BesiegedSettlement.Name);
			return textObject;
		}
		return TextObject.GetEmpty();
	}

	public void Gather(Settlement initialHostileSettlement, MBReadOnlyList<MobileParty> partiesToCallToArmy = null)
	{
		Settlement gatheringPoint = null;
		if (LeaderParty != MobileParty.MainParty)
		{
			FindBestGatheringSettlementAndMoveTheLeader(initialHostileSettlement);
			if (partiesToCallToArmy != null)
			{
				foreach (MobileParty item in partiesToCallToArmy)
				{
					item.Army = this;
				}
			}
		}
		else
		{
			gatheringPoint = SettlementHelper.FindNearestSettlementToMobileParty(MobileParty.MainParty, MobileParty.MainParty.NavigationCapability, (Settlement x) => x.IsFortification || x.IsVillage) ?? SettlementHelper.FindNearestSettlementToPoint(MobileParty.MainParty.Position);
		}
		GatherArmyAction.Apply(LeaderParty, gatheringPoint);
	}

	private void FindBestGatheringSettlementAndMoveTheLeader(Settlement focusSettlement)
	{
		Settlement settlement = null;
		Hero leaderHero = LeaderParty.LeaderHero;
		float num = float.MinValue;
		if (leaderHero != null && leaderHero.IsActive)
		{
			foreach (Settlement settlement2 in Kingdom.Settlements)
			{
				if (!settlement2.IsFortification || settlement2.IsUnderSiege)
				{
					continue;
				}
				float num2 = float.MaxValue;
				num2 = ((LeaderParty.CurrentSettlement != null) ? Campaign.Current.Models.MapDistanceModel.GetDistance(LeaderParty.CurrentSettlement, settlement2, isFromPort: false, isTargetingPort: false, LeaderParty.NavigationCapability, out var landRatio) : Campaign.Current.Models.MapDistanceModel.GetDistance(LeaderParty, settlement2, isTargetingPort: false, LeaderParty.NavigationCapability, out landRatio));
				if (!(num2 < Campaign.MapDiagonalSquared))
				{
					continue;
				}
				float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(focusSettlement, settlement2, isFromPort: false, isTargetingPort: false, LeaderParty.NavigationCapability);
				if (!(distance < Campaign.MapDiagonalSquared) || !(distance > MinimumDistanceToTargetWhileGatheringAsAttackerArmy))
				{
					continue;
				}
				float num3 = 0f;
				if (settlement == null)
				{
					num3 += 0.001f;
				}
				if (settlement2 == focusSettlement || settlement2.Party.MapEvent != null)
				{
					continue;
				}
				if (settlement2.MapFaction == Kingdom)
				{
					num3 += 10f;
				}
				else if (!FactionManager.IsAtWarAgainstFaction(settlement2.MapFaction, Kingdom))
				{
					num3 += 2f;
				}
				bool flag = false;
				foreach (Army army in Kingdom.Armies)
				{
					if (army != this && army.AiBehaviorObject == settlement2)
					{
						flag = true;
					}
				}
				if (!flag)
				{
					num3 += 10f;
				}
				float num4 = distance / (Campaign.MapDiagonalSquared * 0.1f);
				float num5 = 20f * (1f - num4);
				float num6 = (settlement2.Position.ToVec2() - LeaderParty.Position.ToVec2()).Length / (Campaign.MapDiagonalSquared * 0.1f);
				float num7 = 5f * (1f - num6);
				float num8 = num3 + num5 * 0.5f + num7 * 0.1f;
				if (num8 > num)
				{
					num = num8;
					settlement = settlement2;
				}
			}
		}
		else
		{
			settlement = (Settlement)AiBehaviorObject;
		}
		if (settlement == null)
		{
			settlement = SettlementHelper.FindNearestFortificationToMobileParty(LeaderParty, LeaderParty.NavigationCapability);
		}
		AiBehaviorObject = settlement;
		CampaignVec2 gatePosition = settlement.GatePosition;
		SendLeaderPartyToReachablePointAroundPosition(gatePosition, GatheringPositionMaxDistanceToTheSettlement, GatheringPositionMinDistanceToTheSettlement);
	}

	public bool IsWaitingForArmyMembers()
	{
		if (_armyGatheringStartTime > 0f)
		{
			bool flag = false;
			float num = Campaign.CurrentTime - _armyGatheringStartTime;
			float num2 = EstimatedStrength / Parties.SumQ((MobileParty x) => x.Party.EstimatedStrength);
			if (num < Campaign.Current.Models.ArmyManagementCalculationModel.MaximumWaitTime)
			{
				flag = num2 > 0.9f;
			}
			else
			{
				float num3 = (num - Campaign.Current.Models.ArmyManagementCalculationModel.MaximumWaitTime) * 0.01f;
				flag = num2 > 0.75f - num3;
			}
			return !flag;
		}
		return true;
	}

	private bool IsAnotherEnemyBesiegingTarget()
	{
		Settlement settlement = (Settlement)AiBehaviorObject;
		if (ArmyType == ArmyTypes.Besieger && settlement.IsUnderSiege)
		{
			return settlement.SiegeEvent.BesiegerCamp.MapFaction.IsAtWarWith(LeaderParty.MapFaction);
		}
		return false;
	}

	public void FinishArmyObjective()
	{
		LeaderParty.SetMoveModeHold();
		AiBehaviorObject = null;
	}

	internal void DisperseInternal(ArmyDispersionReason reason = ArmyDispersionReason.Unknown)
	{
		if (_armyIsDispersing)
		{
			return;
		}
		CampaignEventDispatcher.Instance.OnArmyDispersed(this, reason, Parties.Contains(MobileParty.MainParty));
		_armyIsDispersing = true;
		int num = 0;
		for (int num2 = Parties.Count - 1; num2 >= num; num2--)
		{
			MobileParty mobileParty = Parties[num2];
			bool num3 = mobileParty.AttachedTo == LeaderParty;
			mobileParty.Army = null;
			if (num3 && mobileParty.CurrentSettlement == null && mobileParty.IsActive && (!LeaderParty.IsCurrentlyAtSea || mobileParty.HasNavalNavigationCapability))
			{
				MobileParty.NavigationType navigationCapability = ((!mobileParty.IsCurrentlyAtSea) ? MobileParty.NavigationType.Default : MobileParty.NavigationType.Naval);
				mobileParty.Position = NavigationHelper.FindReachablePointAroundPosition(LeaderParty.Position, navigationCapability, 1f);
				mobileParty.SetMoveModeHold();
			}
		}
		_parties.Clear();
		Kingdom = null;
		if (LeaderParty == MobileParty.MainParty && Game.Current.GameStateManager.ActiveState is MapState mapState)
		{
			mapState.OnDispersePlayerLeadedArmy();
		}
		_hourlyTickEvent.DeletePeriodicEvent();
		_tickEvent.DeletePeriodicEvent();
		_armyIsDispersing = false;
	}

	public Vec2 GetRelativePositionForParty(MobileParty mobileParty, Vec2 armyFacing)
	{
		float num = 0.5f;
		int num2 = -1;
		int num3 = 0;
		for (int i = 0; i < LeaderParty.AttachedParties.Count; i++)
		{
			MobileParty mobileParty2 = LeaderParty.AttachedParties[i];
			if (!LeaderParty.IsCurrentlyAtSea || mobileParty2.Ships.Count > 0)
			{
				if (mobileParty2 == mobileParty)
				{
					num2 = num3;
				}
				num3++;
			}
		}
		if (LeaderParty.IsCurrentlyAtSea && LeaderParty.Ships.Count == 0)
		{
			num2--;
		}
		float num4 = (float)MathF.Ceiling(-1f + MathF.Sqrt(1f + 8f * (float)(num3 - 1))) / 4f * num * 0.5f + num;
		int num5 = MathF.Ceiling((-1f + MathF.Sqrt(1f + 8f * (float)(num2 + 2))) / 2f) - 1;
		int num6 = num2 + 1 - num5 * (num5 + 1) / 2;
		bool flag = (num5 & 1) != 0;
		num6 = ((((num6 & 1) != 0) ? (-1 - num6) : num6) >> 1) * ((!flag) ? 1 : (-1));
		float num7 = 1.25f;
		CampaignVec2 campaignVec = new CampaignVec2(LeaderParty.VisualPosition2DWithoutError + -armyFacing * 0.1f * num3, !LeaderParty.IsCurrentlyAtSea);
		Vec2 vec = campaignVec.ToVec2() - MathF.Sign((float)num6 - (((num5 & 1) != 0) ? 0.5f : 0f)) * armyFacing.LeftVec() * num4;
		int[] invalidTerrainTypesForNavigationType = Campaign.Current.Models.PartyNavigationModel.GetInvalidTerrainTypesForNavigationType((!LeaderParty.IsCurrentlyAtSea) ? MobileParty.NavigationType.Default : MobileParty.NavigationType.Naval);
		Vec2 lastPointOnNavigationMeshFromPositionToDestination = Campaign.Current.MapSceneWrapper.GetLastPointOnNavigationMeshFromPositionToDestination(campaignVec.Face, campaignVec.ToVec2(), vec, invalidTerrainTypesForNavigationType);
		if ((vec - lastPointOnNavigationMeshFromPositionToDestination).LengthSquared > 2.25E-06f)
		{
			num = num * (campaignVec - lastPointOnNavigationMeshFromPositionToDestination).Length / num4;
			num7 = num7 * (campaignVec - lastPointOnNavigationMeshFromPositionToDestination).Length / (num4 / 1.5f);
		}
		if (LeaderParty.IsCurrentlyAtSea)
		{
			num7 *= 3f;
			num *= 3f;
		}
		return new Vec2((flag ? ((0f - num) * 0.5f) : 0f) + (float)num6 * num + mobileParty.Party.RandomFloat(-0.25f, 0.25f) * 0.6f * num, ((float)(-num5) + mobileParty.Party.RandomFloatWithSeed(1u, -0.25f, 0.25f)) * num7 * 0.3f);
	}

	public void AddPartyToMergedParties(MobileParty mobileParty)
	{
		mobileParty.AttachedTo = LeaderParty;
		if (mobileParty.IsMainParty)
		{
			(GameStateManager.Current.ActiveState as MapState)?.OnJoinArmy();
			Hero leaderHero = LeaderParty.LeaderHero;
			if (leaderHero != null && leaderHero != Hero.MainHero && !leaderHero.HasMet)
			{
				leaderHero.SetHasMet();
			}
		}
	}

	internal void OnRemovePartyInternal(MobileParty mobileParty)
	{
		mobileParty.Ai.SetInitiative(1f, 1f, 24f);
		_parties.Remove(mobileParty);
		CampaignEventDispatcher.Instance.OnPartyRemovedFromArmy(mobileParty);
		if (this == MobileParty.MainParty.Army && !_armyIsDispersing)
		{
			CampaignEventDispatcher.Instance.OnArmyOverlaySetDirty();
		}
		mobileParty.AttachedTo = null;
		if (LeaderParty == mobileParty && !_armyIsDispersing)
		{
			DisbandArmyAction.ApplyByLeaderPartyRemoved(this);
		}
		if (mobileParty == MobileParty.MainParty)
		{
			Campaign.Current.CameraFollowParty = MobileParty.MainParty.Party;
			StopTrackingTargetSettlement();
		}
		if (mobileParty.Army?.LeaderParty == mobileParty)
		{
			FinishArmyObjective();
			if (!_armyIsDispersing)
			{
				if (mobileParty.Army?.LeaderParty.LeaderHero == null)
				{
					DisbandArmyAction.ApplyByArmyLeaderIsDead(mobileParty.Army);
				}
				else
				{
					DisbandArmyAction.ApplyByObjectiveFinished(mobileParty.Army);
				}
			}
		}
		else if (Parties.Count == 0 && !_armyIsDispersing)
		{
			if (mobileParty.Army != null && MobileParty.MainParty.Army != null && mobileParty.Army == MobileParty.MainParty.Army && Hero.MainHero.IsPrisoner)
			{
				DisbandArmyAction.ApplyByPlayerTakenPrisoner(this);
			}
			else
			{
				DisbandArmyAction.ApplyByNotEnoughParty(this);
			}
		}
		mobileParty.Party.SetVisualAsDirty();
		mobileParty.Party.UpdateVisibilityAndInspected(MobileParty.MainParty.Position);
	}

	internal void OnAddPartyInternal(MobileParty mobileParty)
	{
		_parties.Add(mobileParty);
		mobileParty.Ai.RethinkAtNextHourlyTick = true;
		CampaignEventDispatcher.Instance.OnPartyJoinedArmy(mobileParty);
		if (this == MobileParty.MainParty.Army && LeaderParty != MobileParty.MainParty)
		{
			StartTrackingTargetSettlement(AiBehaviorObject);
			CampaignEventDispatcher.Instance.OnArmyOverlaySetDirty();
		}
		if (!mobileParty.IsMainParty)
		{
			mobileParty.Ai.RethinkAtNextHourlyTick = true;
		}
		if (mobileParty != MobileParty.MainParty && LeaderParty != MobileParty.MainParty && LeaderParty.LeaderHero != null)
		{
			int num = -Campaign.Current.Models.ArmyManagementCalculationModel.CalculatePartyInfluenceCost(LeaderParty, mobileParty);
			ChangeClanInfluenceAction.Apply(LeaderParty.LeaderHero.Clan, num);
		}
	}

	private void SendLeaderPartyToReachablePointAroundPosition(CampaignVec2 centerPosition, float distanceLimit, float innerCenterMinimumDistanceLimit = 0f)
	{
		LeaderParty.SetMoveGoToPoint(NavigationHelper.FindReachablePointAroundPosition(centerPosition, MobileParty.NavigationType.Default, distanceLimit, innerCenterMinimumDistanceLimit), LeaderParty.NavigationCapability);
	}

	private void StartTrackingTargetSettlement(IMapPoint targetObject)
	{
		if (targetObject is Settlement trackableObject)
		{
			Campaign.Current.VisualTrackerManager.RegisterObject(trackableObject);
		}
	}

	private void StopTrackingTargetSettlement()
	{
		if (AiBehaviorObject is Settlement trackableObject)
		{
			Campaign.Current.VisualTrackerManager.RemoveTrackedObject(trackableObject);
		}
	}

	public void SetPositionAfterMapChange(CampaignVec2 newPosition)
	{
		LeaderParty.SetPositionAfterMapChange(newPosition);
		foreach (MobileParty attachedParty in LeaderParty.AttachedParties)
		{
			attachedParty.SetPositionAfterMapChange(newPosition);
		}
	}

	public void CheckPositionsForMapChangeAndUpdateIfNeeded()
	{
		if (NavigationHelper.IsPositionValidForNavigationType(LeaderParty.Position, LeaderParty.NavigationCapability))
		{
			return;
		}
		CampaignVec2 closestNavMeshFaceCenterPositionForPosition = NavigationHelper.GetClosestNavMeshFaceCenterPositionForPosition(LeaderParty.Position, Campaign.Current.Models.PartyNavigationModel.GetInvalidTerrainTypesForNavigationType(LeaderParty.NavigationCapability));
		LeaderParty.Position = NavigationHelper.FindReachablePointAroundPosition(closestNavMeshFaceCenterPositionForPosition, LeaderParty.NavigationCapability, 8f, 1f);
		foreach (MobileParty attachedParty in LeaderParty.AttachedParties)
		{
			attachedParty.SetPositionAfterMapChange(LeaderParty.Position);
		}
	}

	Banner ITrackableCampaignObject.GetBanner()
	{
		return LeaderParty.Banner;
	}

	TextObject ITrackableBase.GetName()
	{
		return Name;
	}

	Vec3 ITrackableBase.GetPosition()
	{
		return LeaderParty.GetPositionAsVec3();
	}

	internal static void AutoGeneratedStaticCollectObjectsArmy(object o, List<object> collectedObjects)
	{
		((Army)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
	}

	protected virtual void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
	{
		collectedObjects.Add(_parties);
		CampaignTime.AutoGeneratedStaticCollectObjectsCampaignTime(_creationTime, collectedObjects);
		collectedObjects.Add(_kingdom);
		collectedObjects.Add(_aiBehaviorObject);
		collectedObjects.Add(ArmyOwner);
		collectedObjects.Add(LeaderParty);
		collectedObjects.Add(Name);
	}

	internal static object AutoGeneratedGetMemberValueArmyType(object o)
	{
		return ((Army)o).ArmyType;
	}

	internal static object AutoGeneratedGetMemberValueArmyOwner(object o)
	{
		return ((Army)o).ArmyOwner;
	}

	internal static object AutoGeneratedGetMemberValueCohesion(object o)
	{
		return ((Army)o).Cohesion;
	}

	internal static object AutoGeneratedGetMemberValueMorale(object o)
	{
		return ((Army)o).Morale;
	}

	internal static object AutoGeneratedGetMemberValueLeaderParty(object o)
	{
		return ((Army)o).LeaderParty;
	}

	internal static object AutoGeneratedGetMemberValueName(object o)
	{
		return ((Army)o).Name;
	}

	internal static object AutoGeneratedGetMemberValue_parties(object o)
	{
		return ((Army)o)._parties;
	}

	internal static object AutoGeneratedGetMemberValue_creationTime(object o)
	{
		return ((Army)o)._creationTime;
	}

	internal static object AutoGeneratedGetMemberValue_armyGatheringStartTime(object o)
	{
		return ((Army)o)._armyGatheringStartTime;
	}

	internal static object AutoGeneratedGetMemberValue_armyIsDispersing(object o)
	{
		return ((Army)o)._armyIsDispersing;
	}

	internal static object AutoGeneratedGetMemberValue_numberOfBoosts(object o)
	{
		return ((Army)o)._numberOfBoosts;
	}

	internal static object AutoGeneratedGetMemberValue_kingdom(object o)
	{
		return ((Army)o)._kingdom;
	}

	internal static object AutoGeneratedGetMemberValue_aiBehaviorObject(object o)
	{
		return ((Army)o)._aiBehaviorObject;
	}

	internal static object AutoGeneratedGetMemberValue_inactivityCounter(object o)
	{
		return ((Army)o)._inactivityCounter;
	}
}
