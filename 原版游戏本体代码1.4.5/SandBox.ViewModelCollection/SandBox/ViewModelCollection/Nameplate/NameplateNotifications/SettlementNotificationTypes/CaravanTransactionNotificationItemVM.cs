using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;

namespace SandBox.ViewModelCollection.Nameplate.NameplateNotifications.SettlementNotificationTypes;

public class CaravanTransactionNotificationItemVM : SettlementNotificationItemBaseVM
{
	private List<(EquipmentElement, int)> _items;

	private bool _isSelling;

	public MobileParty CaravanParty { get; private set; }

	public CaravanTransactionNotificationItemVM(Action<SettlementNotificationItemBaseVM> onRemove, MobileParty caravanParty, List<(EquipmentElement, int)> items, int createdTick)
		: base(onRemove, createdTick)
	{
		_items = items;
		List<(EquipmentElement, int)> list = Extensions.DistinctBy(_items, ((EquipmentElement, int) i) => i.Item1).ToList();
		if (list.Count < _items.Count)
		{
			_items = list;
		}
		CaravanParty = caravanParty;
		_isSelling = _items.Count > 0 && _items[0].Item2 > 0;
		base.Text = SandBoxUIHelper.GetItemsTradedNotificationText(_items, _isSelling);
		base.CharacterName = caravanParty.LeaderHero?.Name.ToString() ?? caravanParty.Name.ToString();
		if (caravanParty.Party.Owner != null)
		{
			base.CharacterVisual = new CharacterImageIdentifierVM(SandBoxUIHelper.GetCharacterCode(CaravanParty.Party.Owner.CharacterObject));
		}
		else
		{
			CharacterObject visualPartyLeader = PartyBaseHelper.GetVisualPartyLeader(CaravanParty.Party);
			if (visualPartyLeader != null)
			{
				base.CharacterVisual = new CharacterImageIdentifierVM(SandBoxUIHelper.GetCharacterCode(visualPartyLeader));
			}
		}
		base.RelationType = 0;
		if (caravanParty != null)
		{
			IFaction mapFaction = caravanParty.MapFaction;
			base.RelationType = ((mapFaction == null || !mapFaction.IsAtWarWith(Hero.MainHero.Clan)) ? 1 : (-1));
		}
	}

	public void AddNewItems(List<(EquipmentElement, int)> newItems)
	{
		int i;
		for (i = 0; i < newItems.Count; i++)
		{
			(EquipmentElement, int) tuple = _items.FirstOrDefault(((EquipmentElement, int) t) => t.Item1.Equals(newItems[i].Item1));
			if (!tuple.Item1.IsEmpty)
			{
				int index = _items.IndexOf(tuple);
				tuple.Item2 += newItems[i].Item2;
				_items[index] = tuple;
				if (_items[index].Item2 == 0)
				{
					_items.RemoveAt(index);
				}
			}
			else
			{
				_items.Add((newItems[i].Item1, newItems[i].Item2));
			}
		}
		_isSelling = _items.Count > 0 && _items[0].Item2 > 0;
		base.Text = SandBoxUIHelper.GetItemsTradedNotificationText(_items, _isSelling);
	}
}
