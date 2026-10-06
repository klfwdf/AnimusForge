using System.Collections.Generic;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem.BattleWreckages;

public sealed class BattleWreckage : MBObjectBase, IInteractablePoint
{
	public enum WreckageType
	{
		Invalid,
		Small,
		Normal,
		Epic
	}

	[SaveableField(0)]
	public readonly CampaignVec2 Position;

	[SaveableField(1)]
	public readonly CampaignTime DestroyTime;

	[SaveableField(2)]
	public readonly WreckageType WreckageTypeCategory;

	[SaveableField(3)]
	private bool _isInvestigated;

	[SaveableField(4)]
	public readonly TextObject AttackerLeaderPartyName;

	[SaveableField(5)]
	public readonly TextObject DefenderLeaderPartyName;

	[SaveableField(6)]
	public readonly IFaction AttackerFaction;

	[SaveableField(7)]
	public readonly IFaction DefenderFaction;

	[SaveableField(8)]
	public readonly BattleSideEnum WinnerSide;

	[SaveableField(9)]
	public readonly CampaignTime BattleStartTime;

	[SaveableField(10)]
	public readonly int AttackerHealthyTroopCountAtStart;

	[SaveableField(11)]
	public readonly int DefenderHealthyTroopCountAtStart;

	[SaveableField(12)]
	public readonly TroopRoster AttackerDiedInBattle;

	[SaveableField(13)]
	public readonly TroopRoster DefenderDiedInBattle;

	[SaveableField(14)]
	public readonly TroopRoster AttackerWoundedInBattle;

	[SaveableField(15)]
	public readonly TroopRoster DefenderWoundedInBattle;

	[SaveableField(16)]
	public readonly Hero AttackerLeaderHero;

	[SaveableField(17)]
	public readonly Hero DefenderLeaderHero;

	public bool IsVisible { get; private set; }

	public TextObject Name { get; private set; }

	public bool IsInvestigated => _isInvestigated;

	public bool IsWreckageDestroyable => !DestroyTime.IsFuture;

	public int TotalNumberOfWoundedInBattle => AttackerWoundedInBattle.TotalRegulars + DefenderWoundedInBattle.TotalRegulars;

	public int TotalNumberOfDiedInBattle => AttackerDiedInBattle.TotalRegulars + DefenderDiedInBattle.TotalRegulars;

	public int TotalCasualtyCountInBattle => TotalNumberOfWoundedInBattle + TotalNumberOfDiedInBattle;

	private BattleWreckage(MapEvent mapEvent, WreckageType wreckageType, CampaignTime destroyTime)
	{
		base.StringId = Campaign.Current.CampaignObjectManager.FindNextUniqueStringId<BattleWreckage>("wreckage_1");
		Position = mapEvent.Position;
		WreckageTypeCategory = wreckageType;
		DestroyTime = destroyTime;
		PartyBase leaderParty = mapEvent.AttackerSide.LeaderParty;
		AttackerLeaderHero = ((leaderParty != null && leaderParty.MobileParty?.IsLordParty == true) ? mapEvent.AttackerSide.LeaderParty.Owner : null);
		PartyBase leaderParty2 = mapEvent.DefenderSide.LeaderParty;
		DefenderLeaderHero = ((leaderParty2 != null && leaderParty2.MobileParty?.IsLordParty == true) ? mapEvent.DefenderSide.LeaderParty.Owner : null);
		AttackerLeaderPartyName = mapEvent.AttackerSide.LeaderParty.Name;
		DefenderLeaderPartyName = mapEvent.DefenderSide.LeaderParty.Name;
		AttackerFaction = mapEvent.AttackerSide.MapFaction;
		DefenderFaction = mapEvent.DefenderSide.MapFaction;
		WinnerSide = mapEvent.WinningSide;
		BattleStartTime = mapEvent.BattleStartTime;
		AttackerHealthyTroopCountAtStart = mapEvent.AttackerSide.Parties.SumQ((MapEventParty x) => x.HealthyManCountAtStart);
		DefenderHealthyTroopCountAtStart = mapEvent.DefenderSide.Parties.SumQ((MapEventParty x) => x.HealthyManCountAtStart);
		AttackerDiedInBattle = TroopRoster.CreateDummyTroopRoster();
		DefenderDiedInBattle = TroopRoster.CreateDummyTroopRoster();
		AttackerWoundedInBattle = TroopRoster.CreateDummyTroopRoster();
		DefenderWoundedInBattle = TroopRoster.CreateDummyTroopRoster();
		Name = GetNameOfWreckage();
		foreach (MapEventParty party in mapEvent.AttackerSide.Parties)
		{
			AttackerWoundedInBattle.Add(party.WoundedInBattle);
			AttackerDiedInBattle.Add(party.DiedInBattle);
		}
		foreach (MapEventParty party2 in mapEvent.DefenderSide.Parties)
		{
			DefenderWoundedInBattle.Add(party2.WoundedInBattle);
			DefenderDiedInBattle.Add(party2.DiedInBattle);
		}
		UpdateVisibility();
	}

	public static void CreateWreckage(MapEvent mapEvent, WreckageType wreckageType, CampaignTime destroyTime)
	{
		BattleWreckage battleWreckage = new BattleWreckage(mapEvent, wreckageType, destroyTime);
		Campaign.Current.CampaignObjectManager.AddWreckage(battleWreckage);
		CampaignEventDispatcher.Instance.OnMapInteractableCreated(battleWreckage);
		LogEntry.AddLogEntry(new WreckageCreatedLogEntry(battleWreckage));
	}

	protected override void AfterLoad()
	{
		Name = GetNameOfWreckage();
	}

	private TextObject GetNameOfWreckage()
	{
		bool num = AttackerLeaderHero != null && DefenderLeaderHero != null;
		bool isOnLand = Position.IsOnLand;
		if (num && WreckageTypeCategory != WreckageType.Small)
		{
			TextObject textObject = ((!isOnLand) ? new TextObject("{=DStYh5rg}Aftermath of the naval battle of {CLOSEST_SETTLEMENT_NAME}") : new TextObject("{=Um9djvw8}Aftermath of the battle of {CLOSEST_SETTLEMENT_NAME}"));
			Settlement settlement = Campaign.Current.Models.MapDistanceModel.GetClosestEntranceToFace(Position.Face, isOnLand ? MobileParty.NavigationType.Default : MobileParty.NavigationType.Naval).Item1;
			if (settlement == null)
			{
				settlement = Campaign.Current.Settlements.WhereQ((Settlement x) => isOnLand || x.HasPort).MinBy((Settlement x) => x.Position.Distance(MobileParty.MainParty.Position));
			}
			textObject.SetTextVariable("CLOSEST_SETTLEMENT_NAME", settlement.Name);
			return textObject;
		}
		if (WreckageTypeCategory == WreckageType.Normal)
		{
			if (!isOnLand)
			{
				return new TextObject("{=3tiEeycT}Battle Wreckage");
			}
			return new TextObject("{=xg6aoZsH}Battleground");
		}
		if (!isOnLand)
		{
			return new TextObject("{=Fxg41jrF}Wreckage");
		}
		return new TextObject("{=QG3JTWa8}Skirmish Site");
	}

	public MBList<TroopRosterElement> GetTotalWoundedInBattle()
	{
		MBList<TroopRosterElement> mBList = new MBList<TroopRosterElement>(AttackerWoundedInBattle.GetTroopRoster());
		mBList.AddRange(DefenderWoundedInBattle.GetTroopRoster());
		return mBList;
	}

	public MBList<TroopRosterElement> GetTotalDiedInBattle()
	{
		MBList<TroopRosterElement> mBList = new MBList<TroopRosterElement>(AttackerDiedInBattle.GetTroopRoster());
		mBList.AddRange(DefenderDiedInBattle.GetTroopRoster());
		return mBList;
	}

	public TextObject GetWinnerPartyName()
	{
		if (WinnerSide != BattleSideEnum.Attacker)
		{
			return DefenderLeaderPartyName;
		}
		return AttackerLeaderPartyName;
	}

	public TextObject GetDefeatedPartyName()
	{
		if (WinnerSide != BattleSideEnum.Attacker)
		{
			return AttackerLeaderPartyName;
		}
		return DefenderLeaderPartyName;
	}

	public IFaction GetWinnerFaction()
	{
		if (WinnerSide != BattleSideEnum.Attacker)
		{
			return DefenderFaction;
		}
		return AttackerFaction;
	}

	public IFaction GetDefeatedFaction()
	{
		if (WinnerSide != BattleSideEnum.Attacker)
		{
			return AttackerFaction;
		}
		return DefenderFaction;
	}

	public bool CanPartyInteract(MobileParty mobileParty, float dt)
	{
		if (mobileParty.IsMainParty && (!IsInvestigated || WreckageTypeCategory == WreckageType.Epic) && MobileParty.MainParty.Position.IsOnLand == Position.IsOnLand)
		{
			float num = mobileParty.Position.Distance(GetInteractionPosition(mobileParty));
			if (mobileParty.IsCurrentlyAtSea)
			{
				return num < Campaign.Current.Models.EncounterModel.NeededMaximumNavalDistanceForEncounteringMobileParty;
			}
			return num < Campaign.Current.Models.EncounterModel.NeededMaximumLandDistanceForEncounteringMobileParty;
		}
		return false;
	}

	public CampaignVec2 GetInteractionPosition(MobileParty interactingParty)
	{
		return Position;
	}

	public void OnPartyInteraction(MobileParty mobileParty)
	{
		Campaign.Current.GetCampaignBehavior<BattleWreckageCampaignBehavior>()?.SetCurrentEncounteredBattleWreckage(this);
	}

	public void DestroyWreckage()
	{
		Campaign.Current.CampaignObjectManager.RemoveWreckage(this);
		CampaignEventDispatcher.Instance.OnMapInteractableDestroyed(this);
	}

	public void UpdateVisibility()
	{
		if (Hero.MainHero.IsActive || Hero.MainHero.IsPrisoner)
		{
			float num = MobileParty.MainParty.SeeingRange;
			if (num <= 0f)
			{
				IsVisible = false;
				return;
			}
			if (IsInvestigated)
			{
				num *= 2f;
			}
			float num2 = Hero.MainHero.GetCampaignPosition().Distance(Position);
			IsVisible = num2 <= num;
		}
		else
		{
			IsVisible = false;
		}
	}

	public void OnWreckageInvestigated()
	{
		_isInvestigated = true;
	}

	internal static void AutoGeneratedStaticCollectObjectsBattleWreckage(object o, List<object> collectedObjects)
	{
		((BattleWreckage)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
	}

	protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
	{
		base.AutoGeneratedInstanceCollectObjects(collectedObjects);
		CampaignVec2.AutoGeneratedStaticCollectObjectsCampaignVec2(Position, collectedObjects);
		CampaignTime.AutoGeneratedStaticCollectObjectsCampaignTime(DestroyTime, collectedObjects);
		collectedObjects.Add(AttackerLeaderPartyName);
		collectedObjects.Add(DefenderLeaderPartyName);
		collectedObjects.Add(AttackerFaction);
		collectedObjects.Add(DefenderFaction);
		CampaignTime.AutoGeneratedStaticCollectObjectsCampaignTime(BattleStartTime, collectedObjects);
		collectedObjects.Add(AttackerDiedInBattle);
		collectedObjects.Add(DefenderDiedInBattle);
		collectedObjects.Add(AttackerWoundedInBattle);
		collectedObjects.Add(DefenderWoundedInBattle);
		collectedObjects.Add(AttackerLeaderHero);
		collectedObjects.Add(DefenderLeaderHero);
	}

	internal static object AutoGeneratedGetMemberValuePosition(object o)
	{
		return ((BattleWreckage)o).Position;
	}

	internal static object AutoGeneratedGetMemberValueDestroyTime(object o)
	{
		return ((BattleWreckage)o).DestroyTime;
	}

	internal static object AutoGeneratedGetMemberValueWreckageTypeCategory(object o)
	{
		return ((BattleWreckage)o).WreckageTypeCategory;
	}

	internal static object AutoGeneratedGetMemberValueAttackerLeaderPartyName(object o)
	{
		return ((BattleWreckage)o).AttackerLeaderPartyName;
	}

	internal static object AutoGeneratedGetMemberValueDefenderLeaderPartyName(object o)
	{
		return ((BattleWreckage)o).DefenderLeaderPartyName;
	}

	internal static object AutoGeneratedGetMemberValueAttackerFaction(object o)
	{
		return ((BattleWreckage)o).AttackerFaction;
	}

	internal static object AutoGeneratedGetMemberValueDefenderFaction(object o)
	{
		return ((BattleWreckage)o).DefenderFaction;
	}

	internal static object AutoGeneratedGetMemberValueWinnerSide(object o)
	{
		return ((BattleWreckage)o).WinnerSide;
	}

	internal static object AutoGeneratedGetMemberValueBattleStartTime(object o)
	{
		return ((BattleWreckage)o).BattleStartTime;
	}

	internal static object AutoGeneratedGetMemberValueAttackerHealthyTroopCountAtStart(object o)
	{
		return ((BattleWreckage)o).AttackerHealthyTroopCountAtStart;
	}

	internal static object AutoGeneratedGetMemberValueDefenderHealthyTroopCountAtStart(object o)
	{
		return ((BattleWreckage)o).DefenderHealthyTroopCountAtStart;
	}

	internal static object AutoGeneratedGetMemberValueAttackerDiedInBattle(object o)
	{
		return ((BattleWreckage)o).AttackerDiedInBattle;
	}

	internal static object AutoGeneratedGetMemberValueDefenderDiedInBattle(object o)
	{
		return ((BattleWreckage)o).DefenderDiedInBattle;
	}

	internal static object AutoGeneratedGetMemberValueAttackerWoundedInBattle(object o)
	{
		return ((BattleWreckage)o).AttackerWoundedInBattle;
	}

	internal static object AutoGeneratedGetMemberValueDefenderWoundedInBattle(object o)
	{
		return ((BattleWreckage)o).DefenderWoundedInBattle;
	}

	internal static object AutoGeneratedGetMemberValueAttackerLeaderHero(object o)
	{
		return ((BattleWreckage)o).AttackerLeaderHero;
	}

	internal static object AutoGeneratedGetMemberValueDefenderLeaderHero(object o)
	{
		return ((BattleWreckage)o).DefenderLeaderHero;
	}

	internal static object AutoGeneratedGetMemberValue_isInvestigated(object o)
	{
		return ((BattleWreckage)o)._isInvestigated;
	}
}
