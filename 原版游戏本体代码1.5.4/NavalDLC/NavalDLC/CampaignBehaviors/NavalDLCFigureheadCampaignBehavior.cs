using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace NavalDLC.CampaignBehaviors;

public class NavalDLCFigureheadCampaignBehavior : CampaignBehaviorBase
{
	private MBReadOnlyList<Figurehead> _allFigureheadsCache;

	private CampaignTime _lastFigureheadLootTime = CampaignTime.Zero;

	private Dictionary<Hero, Figurehead> _aiLordSelectedFigureheads = new Dictionary<Hero, Figurehead>();

	public CampaignTime LastFigureheadLootTime => _lastFigureheadLootTime;

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_lastFigureheadLootTime", ref _lastFigureheadLootTime);
		dataStore.SyncData("_aiLordSelectedFigureheads", ref _aiLordSelectedFigureheads);
	}

	public override void RegisterEvents()
	{
		CampaignEvents.OnShipOwnerChangedEvent.AddNonSerializedListener(this, OnShipOwnerChanged);
		CampaignEvents.OnFigureheadUnlockedEvent.AddNonSerializedListener(this, OnFigureheadUnlocked);
	}

	private void OnFigureheadUnlocked(Figurehead figurehead)
	{
		TextObject textObject = new TextObject("{=jBGi3saG}New figurehead \"{FIGUREHEAD_NAME}\" unlocked!");
		textObject.SetTextVariable("FIGUREHEAD_NAME", figurehead.Name);
		MBInformationManager.AddQuickInformation(textObject, 0, null, null, "event:/ui/notification/quest_update");
		InformationManager.DisplayMessage(new InformationMessage(textObject.ToString(), new Color(0f, 1f, 0f)));
		_lastFigureheadLootTime = CampaignTime.Now;
	}

	private MBReadOnlyList<Figurehead> GetAllFigureheads()
	{
		if (_allFigureheadsCache == null || _allFigureheadsCache.IsEmpty())
		{
			_allFigureheadsCache = MBObjectManager.Instance.GetObjectTypeList<Figurehead>();
		}
		return _allFigureheadsCache;
	}

	private void OnShipOwnerChanged(Ship ship, PartyBase oldOwner, ChangeShipOwnerAction.ShipOwnerChangeDetail changeDetail)
	{
		if (!ship.CanEquipFigurehead)
		{
			return;
		}
		if (ship.Figurehead == null)
		{
			PartyBase owner = ship.Owner;
			if (owner == null || !owner.IsMobile || ship.Owner.MobileParty.LeaderHero == null || ship.Owner.MobileParty.ActualClan == Clan.PlayerClan)
			{
				return;
			}
			if (_aiLordSelectedFigureheads.TryGetValue(ship.Owner.MobileParty.LeaderHero, out var value))
			{
				ship.ChangeFigurehead(value);
				return;
			}
			List<(Figurehead, float)> list = new List<(Figurehead, float)>();
			foreach (Figurehead allFigurehead in GetAllFigureheads())
			{
				if (allFigurehead.Culture == ship.Owner.MobileParty.LeaderHero.Culture)
				{
					list.Add((allFigurehead, 0.2f));
				}
				else
				{
					list.Add((allFigurehead, 0.1f));
				}
			}
			Figurehead figurehead = MBRandom.ChooseWeighted(list);
			ship.ChangeFigurehead(figurehead);
			_aiLordSelectedFigureheads.Add(ship.Owner.MobileParty.LeaderHero, figurehead);
			return;
		}
		PartyBase owner2 = ship.Owner;
		if (owner2 == null || !owner2.IsSettlement)
		{
			PartyBase owner3 = ship.Owner;
			if (owner3 == null || !owner3.IsMobile || ship.Owner.MobileParty.ActualClan != Clan.PlayerClan || oldOwner?.MobileParty?.ActualClan == Clan.PlayerClan)
			{
				return;
			}
		}
		if (changeDetail != ChangeShipOwnerAction.ShipOwnerChangeDetail.ApplyByStashing && changeDetail != ChangeShipOwnerAction.ShipOwnerChangeDetail.ApplyByUnstashing && changeDetail != ChangeShipOwnerAction.ShipOwnerChangeDetail.ApplyByTemporarilyRemovingShipsFromPlayer && changeDetail != ChangeShipOwnerAction.ShipOwnerChangeDetail.ApplyByGivingBackShipsToPlayer)
		{
			ship.ChangeFigurehead(null);
		}
	}
}
