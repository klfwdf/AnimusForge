using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Party;

public class PartyCompositionVM : ViewModel
{
	private int _infantryCount;

	private int _rangedCount;

	private int _cavalryCount;

	private int _horseArcherCount;

	private HintViewModel _infantryHint;

	private HintViewModel _rangedHint;

	private HintViewModel _cavalryHint;

	private HintViewModel _horseArcherHint;

	[DataSourceProperty]
	public int InfantryCount
	{
		get
		{
			return _infantryCount;
		}
		set
		{
			if (value != _infantryCount)
			{
				_infantryCount = value;
				OnPropertyChangedWithValue(value, "InfantryCount");
			}
		}
	}

	[DataSourceProperty]
	public int RangedCount
	{
		get
		{
			return _rangedCount;
		}
		set
		{
			if (value != _rangedCount)
			{
				_rangedCount = value;
				OnPropertyChangedWithValue(value, "RangedCount");
			}
		}
	}

	[DataSourceProperty]
	public int CavalryCount
	{
		get
		{
			return _cavalryCount;
		}
		set
		{
			if (value != _cavalryCount)
			{
				_cavalryCount = value;
				OnPropertyChangedWithValue(value, "CavalryCount");
			}
		}
	}

	[DataSourceProperty]
	public int HorseArcherCount
	{
		get
		{
			return _horseArcherCount;
		}
		set
		{
			if (value != _horseArcherCount)
			{
				_horseArcherCount = value;
				OnPropertyChangedWithValue(value, "HorseArcherCount");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel InfantryHint
	{
		get
		{
			return _infantryHint;
		}
		set
		{
			if (value != _infantryHint)
			{
				_infantryHint = value;
				OnPropertyChangedWithValue(value, "InfantryHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel RangedHint
	{
		get
		{
			return _rangedHint;
		}
		set
		{
			if (value != _rangedHint)
			{
				_rangedHint = value;
				OnPropertyChangedWithValue(value, "RangedHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel CavalryHint
	{
		get
		{
			return _cavalryHint;
		}
		set
		{
			if (value != _cavalryHint)
			{
				_cavalryHint = value;
				OnPropertyChangedWithValue(value, "CavalryHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel HorseArcherHint
	{
		get
		{
			return _horseArcherHint;
		}
		set
		{
			if (value != _horseArcherHint)
			{
				_horseArcherHint = value;
				OnPropertyChangedWithValue(value, "HorseArcherHint");
			}
		}
	}

	public PartyCompositionVM()
	{
		InfantryHint = new HintViewModel(new TextObject("{=1Bm1Wk1v}Infantry"));
		RangedHint = new HintViewModel(new TextObject("{=bIiBytSB}Archers"));
		CavalryHint = new HintViewModel(new TextObject("{=YVGtcLHF}Cavalry"));
		HorseArcherHint = new HintViewModel(new TextObject("{=I1CMeL9R}Mounted Archers"));
	}

	public void OnTroopRemoved(FormationClass formationClass, int count)
	{
		if (IsInfantry(formationClass))
		{
			InfantryCount -= count;
		}
		if (IsRanged(formationClass))
		{
			RangedCount -= count;
		}
		if (IsCavalry(formationClass))
		{
			CavalryCount -= count;
		}
		if (IsHorseArcher(formationClass))
		{
			HorseArcherCount -= count;
		}
	}

	public void OnTroopAdded(FormationClass formationClass, int count)
	{
		if (IsInfantry(formationClass))
		{
			InfantryCount += count;
		}
		if (IsRanged(formationClass))
		{
			RangedCount += count;
		}
		if (IsCavalry(formationClass))
		{
			CavalryCount += count;
		}
		if (IsHorseArcher(formationClass))
		{
			HorseArcherCount += count;
		}
	}

	public void RefreshCounts(MBBindingList<PartyCharacterVM> list)
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		for (int i = 0; i < list.Count; i++)
		{
			TroopRosterElement troop = list[i].Troop;
			FormationClass defaultFormationClass = list[i].Troop.Character.DefaultFormationClass;
			if (IsInfantry(defaultFormationClass))
			{
				num += troop.Number;
			}
			if (IsRanged(defaultFormationClass))
			{
				num2 += troop.Number;
			}
			if (IsCavalry(defaultFormationClass))
			{
				num3 += troop.Number;
			}
			if (IsHorseArcher(defaultFormationClass))
			{
				num4 += troop.Number;
			}
		}
		InfantryCount = num;
		RangedCount = num2;
		CavalryCount = num3;
		HorseArcherCount = num4;
	}

	private bool IsInfantry(FormationClass formationClass)
	{
		if (formationClass != FormationClass.Infantry && formationClass != FormationClass.HeavyInfantry)
		{
			return formationClass == FormationClass.NumberOfDefaultFormations;
		}
		return true;
	}

	private bool IsRanged(FormationClass formationClass)
	{
		return formationClass == FormationClass.Ranged;
	}

	private bool IsCavalry(FormationClass formationClass)
	{
		if (formationClass != FormationClass.Cavalry && formationClass != FormationClass.LightCavalry)
		{
			return formationClass == FormationClass.HeavyCavalry;
		}
		return true;
	}

	private bool IsHorseArcher(FormationClass formationClass)
	{
		return formationClass == FormationClass.HorseArcher;
	}
}
