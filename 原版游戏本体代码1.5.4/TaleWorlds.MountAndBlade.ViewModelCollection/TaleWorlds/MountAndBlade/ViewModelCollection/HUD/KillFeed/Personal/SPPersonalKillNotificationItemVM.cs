using System;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.HUD.KillFeed.Personal;

public class SPPersonalKillNotificationItemVM : ViewModel
{
	private enum ItemTypes
	{
		NormalDamage,
		FriendlyFireDamage,
		FriendlyFireKill,
		MountDamage,
		NormalKill,
		Assist,
		MakeUnconscious,
		NormalKillHeadshot,
		MakeUnconsciousHeadshot,
		Message
	}

	private Action<SPPersonalKillNotificationItemVM> _onRemoveItem;

	private ItemTypes _itemTypeAsEnum;

	private string _message;

	private string _victimType;

	private int _amount;

	private int _itemType;

	private bool _isPaused;

	private ItemTypes ItemTypeAsEnum
	{
		get
		{
			return _itemTypeAsEnum;
		}
		set
		{
			_itemType = (int)value;
			_itemTypeAsEnum = value;
		}
	}

	[DataSourceProperty]
	public string VictimType
	{
		get
		{
			return _victimType;
		}
		set
		{
			if (value != _victimType)
			{
				_victimType = value;
				OnPropertyChangedWithValue(value, "VictimType");
			}
		}
	}

	[DataSourceProperty]
	public string Message
	{
		get
		{
			return _message;
		}
		set
		{
			if (value != _message)
			{
				_message = value;
				OnPropertyChangedWithValue(value, "Message");
			}
		}
	}

	[DataSourceProperty]
	public int ItemType
	{
		get
		{
			return _itemType;
		}
		set
		{
			if (value != _itemType)
			{
				_itemType = value;
				OnPropertyChangedWithValue(value, "ItemType");
			}
		}
	}

	[DataSourceProperty]
	public int Amount
	{
		get
		{
			return _amount;
		}
		set
		{
			if (value != _amount)
			{
				_amount = value;
				OnPropertyChangedWithValue(value, "Amount");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPaused
	{
		get
		{
			return _isPaused;
		}
		set
		{
			if (value != _isPaused)
			{
				_isPaused = value;
				OnPropertyChangedWithValue(value, "IsPaused");
			}
		}
	}

	public SPPersonalKillNotificationItemVM(int damageAmount, bool isMountDamage, bool isFriendlyFire, bool isHeadshot, string killedAgentName, bool isUnconscious, Action<SPPersonalKillNotificationItemVM> onRemoveItem)
	{
		_onRemoveItem = onRemoveItem;
		Amount = damageAmount;
		if (isFriendlyFire)
		{
			ItemTypeAsEnum = ItemTypes.FriendlyFireKill;
			Message = killedAgentName;
		}
		else if (isMountDamage)
		{
			ItemTypeAsEnum = ItemTypes.MountDamage;
			Message = GameTexts.FindText("str_damage_delivered_message").ToString();
		}
		else
		{
			ItemTypeAsEnum = ((!isUnconscious) ? (isHeadshot ? ItemTypes.NormalKillHeadshot : ItemTypes.NormalKill) : (isHeadshot ? ItemTypes.MakeUnconsciousHeadshot : ItemTypes.MakeUnconscious));
			Message = killedAgentName;
		}
	}

	public SPPersonalKillNotificationItemVM(int amount, bool isMountDamage, bool isFriendlyFire, string killedAgentName, Action<SPPersonalKillNotificationItemVM> onRemoveItem)
	{
		_onRemoveItem = onRemoveItem;
		Amount = amount;
		if (isFriendlyFire)
		{
			ItemTypeAsEnum = ItemTypes.FriendlyFireDamage;
			Message = killedAgentName;
		}
		else if (isMountDamage)
		{
			ItemTypeAsEnum = ItemTypes.MountDamage;
			Message = GameTexts.FindText("str_damage_delivered_message").ToString();
		}
		else
		{
			ItemTypeAsEnum = ItemTypes.NormalDamage;
			Message = GameTexts.FindText("str_damage_delivered_message").ToString();
		}
	}

	public SPPersonalKillNotificationItemVM(string victimAgentName, Action<SPPersonalKillNotificationItemVM> onRemoveItem)
	{
		_onRemoveItem = onRemoveItem;
		Amount = -1;
		Message = victimAgentName;
		ItemTypeAsEnum = ItemTypes.Message;
	}

	public void ExecuteRemove()
	{
		_onRemoveItem(this);
	}
}
