using System;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.KillFeed.Personal;

public class MPPersonalKillNotificationItemVM : ViewModel
{
	private enum ItemTypes
	{
		NormalDamage,
		FriendlyFireDamage,
		FriendlyFireKill,
		MountDamage,
		NormalKill,
		Assist,
		GoldChange,
		HeadshotKill
	}

	private Action<MPPersonalKillNotificationItemVM> _onRemoveItem;

	private ItemTypes _itemTypeAsEnum;

	private string _message;

	private int _amount;

	private int _itemType;

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

	public MPPersonalKillNotificationItemVM(int amount, bool isFatal, bool isMountDamage, bool isFriendlyFire, bool isHeadshot, string killedAgentName, Action<MPPersonalKillNotificationItemVM> onRemoveItem)
	{
		_onRemoveItem = onRemoveItem;
		Amount = amount;
		if (isFriendlyFire)
		{
			ItemTypeAsEnum = ((!isFatal) ? ItemTypes.FriendlyFireDamage : ItemTypes.FriendlyFireKill);
			Message = killedAgentName;
		}
		else if (isMountDamage)
		{
			ItemTypeAsEnum = ItemTypes.MountDamage;
			Message = GameTexts.FindText("str_damage_delivered_message").ToString();
		}
		else if (isFatal)
		{
			ItemTypeAsEnum = (isHeadshot ? ItemTypes.HeadshotKill : ItemTypes.NormalKill);
			Message = killedAgentName;
		}
		else
		{
			ItemTypeAsEnum = ItemTypes.NormalDamage;
			Message = GameTexts.FindText("str_damage_delivered_message").ToString();
		}
	}

	public MPPersonalKillNotificationItemVM(int amount, GoldGainFlags reasonType, Action<MPPersonalKillNotificationItemVM> onRemoveItem)
	{
		_onRemoveItem = onRemoveItem;
		ItemTypeAsEnum = ItemTypes.GoldChange;
		switch (reasonType)
		{
		case GoldGainFlags.FirstRangedKill:
			Message = GameTexts.FindText("str_gold_gain_first_ranged_kill").ToString();
			break;
		case GoldGainFlags.FirstMeleeKill:
			Message = GameTexts.FindText("str_gold_gain_first_melee_kill").ToString();
			break;
		case GoldGainFlags.FirstAssist:
			Message = GameTexts.FindText("str_gold_gain_first_assist").ToString();
			break;
		case GoldGainFlags.SecondAssist:
			Message = GameTexts.FindText("str_gold_gain_second_assist").ToString();
			break;
		case GoldGainFlags.ThirdAssist:
			Message = GameTexts.FindText("str_gold_gain_third_assist").ToString();
			break;
		case GoldGainFlags.FifthKill:
			Message = GameTexts.FindText("str_gold_gain_fifth_kill").ToString();
			break;
		case GoldGainFlags.TenthKill:
			Message = GameTexts.FindText("str_gold_gain_tenth_kill").ToString();
			break;
		case GoldGainFlags.DefaultKill:
			Message = GameTexts.FindText("str_gold_gain_default_kill").ToString();
			break;
		case GoldGainFlags.DefaultAssist:
			Message = GameTexts.FindText("str_gold_gain_default_assist").ToString();
			break;
		case GoldGainFlags.ObjectiveCompleted:
			Message = GameTexts.FindText("str_gold_gain_objective_completed").ToString();
			break;
		case GoldGainFlags.ObjectiveDestroyed:
			Message = GameTexts.FindText("str_gold_gain_objective_destroyed").ToString();
			break;
		case GoldGainFlags.PerkBonus:
			Message = GameTexts.FindText("str_gold_gain_perk_bonus").ToString();
			break;
		default:
			Debug.FailedAssert("Undefined gold change type", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection\\KillFeed\\Personal\\MPPersonalKillNotificationItemVM.cs", ".ctor", 117);
			Message = "";
			break;
		}
		Amount = amount;
	}

	public MPPersonalKillNotificationItemVM(string victimAgentName, Action<MPPersonalKillNotificationItemVM> onRemoveItem)
	{
		_onRemoveItem = onRemoveItem;
		Amount = -1;
		Message = victimAgentName;
		ItemTypeAsEnum = ItemTypes.Assist;
	}

	public void ExecuteRemove()
	{
		_onRemoveItem(this);
	}
}
