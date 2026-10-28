using System;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby;

public class MPLobbyCosmeticSigilItemVM : MPLobbySigilItemVM
{
	public readonly string CosmeticID;

	private static Action<MPLobbyCosmeticSigilItemVM> _onSelection;

	private static Action<MPLobbyCosmeticSigilItemVM> _onObtainRequested;

	private bool _isUnlocked;

	private bool _isUsed;

	private int _rarity;

	private int _cost;

	[DataSourceProperty]
	public bool IsUnlocked
	{
		get
		{
			return _isUnlocked;
		}
		set
		{
			if (value != _isUnlocked)
			{
				_isUnlocked = value;
				OnPropertyChangedWithValue(value, "IsUnlocked");
			}
		}
	}

	[DataSourceProperty]
	public bool IsUsed
	{
		get
		{
			return _isUsed;
		}
		set
		{
			if (value != _isUsed)
			{
				_isUsed = value;
				OnPropertyChangedWithValue(value, "IsUsed");
			}
		}
	}

	[DataSourceProperty]
	public int Rarity
	{
		get
		{
			return _rarity;
		}
		set
		{
			if (value != _rarity)
			{
				_rarity = value;
				OnPropertyChangedWithValue(value, "Rarity");
			}
		}
	}

	[DataSourceProperty]
	public int Cost
	{
		get
		{
			return _cost;
		}
		set
		{
			if (value != _cost)
			{
				_cost = value;
				OnPropertyChangedWithValue(value, "Cost");
			}
		}
	}

	public MPLobbyCosmeticSigilItemVM(int iconID, int rarity, int cost, string cosmeticID)
		: base(iconID, null)
	{
		Rarity = rarity;
		Cost = cost;
		CosmeticID = cosmeticID;
	}

	public static void SetOnSelectionCallback(Action<MPLobbyCosmeticSigilItemVM> onSelection)
	{
		_onSelection = onSelection;
	}

	public static void ResetOnSelectionCallback()
	{
		_onSelection = null;
	}

	public static void SetOnObtainRequestedCallback(Action<MPLobbyCosmeticSigilItemVM> onObtainRequested)
	{
		_onObtainRequested = onObtainRequested;
	}

	public static void ResetOnObtainRequestedCallback()
	{
		_onObtainRequested = null;
	}

	private void ExecuteSelection()
	{
		if (IsUnlocked)
		{
			_onSelection?.Invoke(this);
		}
		else
		{
			_onObtainRequested?.Invoke(this);
		}
	}
}
