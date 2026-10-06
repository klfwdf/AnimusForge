using System;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Party.PartyTroopManagerPopUp;

public class PartyTroopManagerItemVM : ViewModel
{
	private bool _isFocused;

	private PartyCharacterVM _partyCharacter;

	public Action<PartyTroopManagerItemVM> SetFocused { get; private set; }

	[DataSourceProperty]
	public bool IsFocused
	{
		get
		{
			return _isFocused;
		}
		set
		{
			if (value != _isFocused)
			{
				_isFocused = value;
				OnPropertyChangedWithValue(value, "IsFocused");
			}
		}
	}

	[DataSourceProperty]
	public PartyCharacterVM PartyCharacter
	{
		get
		{
			return _partyCharacter;
		}
		set
		{
			if (value != _partyCharacter)
			{
				_partyCharacter = value;
				OnPropertyChangedWithValue(value, "PartyCharacter");
			}
		}
	}

	[DataSourceProperty]
	public bool IsTroopUpgradable
	{
		get
		{
			return PartyCharacter.IsTroopUpgradable;
		}
		set
		{
			if (value != PartyCharacter.IsTroopUpgradable)
			{
				PartyCharacter.IsTroopUpgradable = value;
				OnPropertyChangedWithValue(value, "IsTroopUpgradable");
			}
		}
	}

	[DataSourceProperty]
	public bool IsTroopRecruitable
	{
		get
		{
			return PartyCharacter.IsTroopRecruitable;
		}
		set
		{
			if (value != PartyCharacter.IsTroopRecruitable)
			{
				PartyCharacter.IsTroopRecruitable = value;
				OnPropertyChangedWithValue(value, "IsTroopRecruitable");
			}
		}
	}

	public PartyTroopManagerItemVM(PartyCharacterVM baseTroop, Action<PartyTroopManagerItemVM> setFocused)
	{
		PartyCharacter = baseTroop;
		SetFocused = setFocused;
	}

	public void ExecuteSetFocused()
	{
		if (PartyCharacter.Character != null)
		{
			SetFocused?.Invoke(this);
			IsFocused = true;
		}
	}

	public void ExecuteSetUnfocused()
	{
		SetFocused?.Invoke(null);
		IsFocused = false;
	}

	public void ExecuteOpenTroopEncyclopedia()
	{
		PartyCharacter.ExecuteOpenTroopEncyclopedia();
	}
}
