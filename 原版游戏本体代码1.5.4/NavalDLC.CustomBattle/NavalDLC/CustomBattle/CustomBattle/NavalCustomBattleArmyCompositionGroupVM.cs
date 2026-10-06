using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.CustomBattle.CustomBattle;

public class NavalCustomBattleArmyCompositionGroupVM : ViewModel
{
	public int[] CompositionValues;

	private bool _updatingSliders;

	private BasicCultureObject _selectedCulture;

	private float _cachedArmySizeRatio;

	private int _cachedLandArmyCount;

	private readonly MBReadOnlyList<SkillObject> _allSkills = Game.Current.ObjectManager.GetObjectTypeList<SkillObject>();

	private readonly List<BasicCharacterObject> _allCharacterObjects = new List<BasicCharacterObject>();

	private NavalCustomBattleArmyCompositionItemVM _meleeInfantryComposition;

	private NavalCustomBattleArmyCompositionItemVM _rangedInfantryComposition;

	private NavalCustomBattleArmyCompositionItemVM _meleeCavalryComposition;

	private NavalCustomBattleArmyCompositionItemVM _rangedCavalryComposition;

	private int _armySize;

	private int _maxArmySize;

	private int _minArmySize;

	private int _skeletalSize;

	private int _deckSize;

	private string _armySizeTitle;

	private string _warningText;

	private bool _isWarned;

	private bool _isLand;

	[DataSourceProperty]
	public NavalCustomBattleArmyCompositionItemVM MeleeInfantryComposition
	{
		get
		{
			return _meleeInfantryComposition;
		}
		set
		{
			if (value != _meleeInfantryComposition)
			{
				_meleeInfantryComposition = value;
				OnPropertyChangedWithValue(value, "MeleeInfantryComposition");
			}
		}
	}

	[DataSourceProperty]
	public NavalCustomBattleArmyCompositionItemVM RangedInfantryComposition
	{
		get
		{
			return _rangedInfantryComposition;
		}
		set
		{
			if (value != _rangedInfantryComposition)
			{
				_rangedInfantryComposition = value;
				OnPropertyChangedWithValue(value, "RangedInfantryComposition");
			}
		}
	}

	[DataSourceProperty]
	public NavalCustomBattleArmyCompositionItemVM MeleeCavalryComposition
	{
		get
		{
			return _meleeCavalryComposition;
		}
		set
		{
			if (value != _meleeCavalryComposition)
			{
				_meleeCavalryComposition = value;
				OnPropertyChangedWithValue(value, "MeleeCavalryComposition");
			}
		}
	}

	[DataSourceProperty]
	public NavalCustomBattleArmyCompositionItemVM RangedCavalryComposition
	{
		get
		{
			return _rangedCavalryComposition;
		}
		set
		{
			if (value != _rangedCavalryComposition)
			{
				_rangedCavalryComposition = value;
				OnPropertyChangedWithValue(value, "RangedCavalryComposition");
			}
		}
	}

	[DataSourceProperty]
	public string ArmySizeTitle
	{
		get
		{
			return _armySizeTitle;
		}
		set
		{
			if (value != _armySizeTitle)
			{
				_armySizeTitle = value;
				OnPropertyChangedWithValue(value, "ArmySizeTitle");
			}
		}
	}

	[DataSourceProperty]
	public string WarningText
	{
		get
		{
			return _warningText;
		}
		set
		{
			if (value != _warningText)
			{
				_warningText = value;
				OnPropertyChangedWithValue(value, "WarningText");
			}
		}
	}

	[DataSourceProperty]
	public bool IsWarned
	{
		get
		{
			return _isWarned;
		}
		set
		{
			if (value != _isWarned)
			{
				_isWarned = value;
				OnPropertyChangedWithValue(value, "IsWarned");
			}
		}
	}

	[DataSourceProperty]
	public int ArmySize
	{
		get
		{
			return _armySize;
		}
		set
		{
			value = (int)MathF.Clamp(value, MinArmySize, MaxArmySize);
			if (_armySize != value)
			{
				_armySize = value;
				OnPropertyChangedWithValue(value, "ArmySize");
				if (!IsLand)
				{
					_cachedArmySizeRatio = (float)(value - MinArmySize) / (float)(MaxArmySize - MinArmySize);
				}
				else
				{
					_cachedLandArmyCount = value;
				}
				UpdateIsWarned();
			}
		}
	}

	[DataSourceProperty]
	public int MaxArmySize
	{
		get
		{
			return _maxArmySize;
		}
		set
		{
			if (_maxArmySize != value)
			{
				_maxArmySize = value;
				OnPropertyChangedWithValue(value, "MaxArmySize");
			}
		}
	}

	[DataSourceProperty]
	public int MinArmySize
	{
		get
		{
			return _minArmySize;
		}
		set
		{
			if (_minArmySize != value)
			{
				_minArmySize = value;
				OnPropertyChangedWithValue(value, "MinArmySize");
			}
		}
	}

	public int SkeletalSize
	{
		get
		{
			return _skeletalSize;
		}
		set
		{
			if (_skeletalSize != value)
			{
				_skeletalSize = value;
				OnPropertyChangedWithValue(value, "SkeletalSize");
			}
		}
	}

	public int DeckSize
	{
		get
		{
			return _deckSize;
		}
		set
		{
			if (_deckSize != value)
			{
				_deckSize = value;
				OnPropertyChangedWithValue(value, "DeckSize");
			}
		}
	}

	public bool IsLand
	{
		get
		{
			return _isLand;
		}
		set
		{
			if (_isLand != value)
			{
				_isLand = value;
				OnPropertyChangedWithValue(value, "IsLand");
				RefreshValues();
				MeleeInfantryComposition.IsLand = value;
				RangedInfantryComposition.IsLand = value;
				MeleeCavalryComposition.IsLand = value;
				RangedCavalryComposition.IsLand = value;
			}
		}
	}

	public NavalCustomBattleArmyCompositionGroupVM(NavalCustomBattleTroopTypeSelectionPopUpVM troopTypeSelectionPopUp)
	{
		foreach (BasicCharacterObject item in from c in Game.Current.ObjectManager.GetObjectTypeList<BasicCharacterObject>()
			where c.IsSoldier && !c.IsObsolete
			select c)
		{
			_allCharacterObjects.Add(item);
		}
		CompositionValues = new int[4];
		CompositionValues[0] = 50;
		CompositionValues[1] = 50;
		CompositionValues[2] = 0;
		CompositionValues[3] = 0;
		MeleeInfantryComposition = new NavalCustomBattleArmyCompositionItemVM(NavalCustomBattleArmyCompositionItemVM.CompositionType.MeleeInfantry, _allCharacterObjects, _allSkills, UpdateSliders, troopTypeSelectionPopUp, CompositionValues);
		RangedInfantryComposition = new NavalCustomBattleArmyCompositionItemVM(NavalCustomBattleArmyCompositionItemVM.CompositionType.RangedInfantry, _allCharacterObjects, _allSkills, UpdateSliders, troopTypeSelectionPopUp, CompositionValues);
		MeleeCavalryComposition = new NavalCustomBattleArmyCompositionItemVM(NavalCustomBattleArmyCompositionItemVM.CompositionType.MeleeCavalry, _allCharacterObjects, _allSkills, UpdateSliders, troopTypeSelectionPopUp, CompositionValues);
		RangedCavalryComposition = new NavalCustomBattleArmyCompositionItemVM(NavalCustomBattleArmyCompositionItemVM.CompositionType.RangedCavalry, _allCharacterObjects, _allSkills, UpdateSliders, troopTypeSelectionPopUp, CompositionValues);
		_cachedArmySizeRatio = 0.725f;
		_cachedLandArmyCount = BannerlordConfig.GetRealBattleSizeForNaval() / 5;
		UpdateTroopCountLimits(1, BannerlordConfig.MaxBattleSize, 1, 1);
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		ArmySizeTitle = (IsLand ? GameTexts.FindText("str_army_size").ToString() : new TextObject("{=EQLbYxec}Crew Count").ToString());
		MeleeInfantryComposition.RefreshValues();
		RangedInfantryComposition.RefreshValues();
		MeleeCavalryComposition.RefreshValues();
		RangedCavalryComposition.RefreshValues();
		UpdateIsWarned();
	}

	private static int SumOfValues(int[] array, bool[] enabledArray, int excludedIndex = -1)
	{
		int num = 0;
		for (int i = 0; i < array.Length; i++)
		{
			if (enabledArray[i] && excludedIndex != i)
			{
				num += array[i];
			}
		}
		return num;
	}

	public void SetCurrentSelectedCulture(BasicCultureObject selectedCulture)
	{
		if (_selectedCulture != selectedCulture)
		{
			MeleeInfantryComposition.SetCurrentSelectedCulture(selectedCulture);
			RangedInfantryComposition.SetCurrentSelectedCulture(selectedCulture);
			MeleeCavalryComposition.SetCurrentSelectedCulture(selectedCulture);
			RangedCavalryComposition.SetCurrentSelectedCulture(selectedCulture);
			_selectedCulture = selectedCulture;
		}
	}

	private void UpdateSliders(int value, int changedSliderIndex)
	{
		if (_updatingSliders)
		{
			return;
		}
		_updatingSliders = true;
		bool[] array = new bool[4]
		{
			!MeleeInfantryComposition.IsLocked,
			!RangedInfantryComposition.IsLocked,
			!MeleeCavalryComposition.IsLocked,
			!RangedCavalryComposition.IsLocked
		};
		int[] array2 = new int[4]
		{
			CompositionValues[0],
			CompositionValues[1],
			CompositionValues[2],
			CompositionValues[3]
		};
		int[] array3 = new int[4]
		{
			CompositionValues[0],
			CompositionValues[1],
			CompositionValues[2],
			CompositionValues[3]
		};
		int num = array.Count((bool s) => s);
		if (array[changedSliderIndex])
		{
			num--;
		}
		if (num > 0)
		{
			int num2 = SumOfValues(array2, array);
			array[changedSliderIndex] = false;
			if (value >= num2)
			{
				value = num2;
			}
			int num3 = value - array2[changedSliderIndex];
			if (num3 != 0)
			{
				array3[changedSliderIndex] = value;
				int num4 = -num3;
				int num5 = num4 / num;
				for (int num6 = 0; num6 < array.Length; num6++)
				{
					if (array[num6])
					{
						array3[num6] += num5;
						num4 -= num5;
					}
				}
				for (int num7 = 0; num7 < array.Length; num7++)
				{
					if (array[num7] && array3[num7] < 0)
					{
						num4 += array3[num7];
						array3[num7] = 0;
					}
				}
				if (num4 > 0)
				{
					while (num4 != 0)
					{
						int num8 = int.MaxValue;
						int num9 = -1;
						for (int num10 = 0; num10 < array.Length; num10++)
						{
							if (array[num10] && array3[num10] < num8)
							{
								num8 = array3[num10];
								num9 = num10;
							}
						}
						array3[num9]++;
						num4--;
					}
				}
				else if (num4 < 0)
				{
					for (; num4 != 0; num4++)
					{
						int num11 = int.MinValue;
						int num12 = -1;
						for (int num13 = 0; num13 < array.Length; num13++)
						{
							if (array[num13] && array3[num13] > num11)
							{
								num11 = array3[num13];
								num12 = num13;
							}
						}
						array3[num12]--;
					}
				}
			}
		}
		SetArmyCompositionValue(0, array3[0], MeleeInfantryComposition);
		SetArmyCompositionValue(1, array3[1], RangedInfantryComposition);
		SetArmyCompositionValue(2, array3[2], MeleeCavalryComposition);
		SetArmyCompositionValue(3, array3[3], RangedCavalryComposition);
		_updatingSliders = false;
	}

	private void SetArmyCompositionValue(int index, int value, NavalCustomBattleArmyCompositionItemVM composition)
	{
		CompositionValues[index] = value;
		composition.RefreshCompositionValue();
	}

	public void ExecuteRandomize(int targetDeckSize)
	{
		if (IsLand)
		{
			int num = MBRandom.RandomInt(100);
			MeleeInfantryComposition.ExecuteRandomize(num);
			RangedInfantryComposition.ExecuteRandomize(100 - num);
			ArmySize = targetDeckSize;
			return;
		}
		int num2 = MBRandom.RandomInt(100);
		int num3 = MBRandom.RandomInt(100);
		int num4 = MBRandom.RandomInt(100);
		int num5 = MBRandom.RandomInt(100);
		int num6 = num2 + num3 + num4 + num5;
		int num7 = MathF.Round(100f * ((float)num2 / (float)num6));
		int num8 = MathF.Round(100f * ((float)num3 / (float)num6));
		int num9 = MathF.Round(100f * ((float)num4 / (float)num6));
		int compositionValue = 100 - (num7 + num8 + num9);
		MeleeInfantryComposition.ExecuteRandomize(num7);
		RangedInfantryComposition.ExecuteRandomize(num8);
		MeleeCavalryComposition.ExecuteRandomize(num9);
		RangedCavalryComposition.ExecuteRandomize(compositionValue);
	}

	public void UpdateTroopCountLimits(int minTroopCount, int maxTroopCount, int skeletalSize, int deckSize)
	{
		MinArmySize = MathF.Max(1, minTroopCount);
		MaxArmySize = MathF.Min(BannerlordConfig.MaxBattleSize, maxTroopCount);
		if (MaxArmySize < MinArmySize)
		{
			Debug.FailedAssert("Max army size is less than min army size!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC.CustomBattle\\CustomBattle\\NavalCustomBattleArmyCompositionGroupVM.cs", "UpdateTroopCountLimits", 261);
			MaxArmySize = MinArmySize;
		}
		SkeletalSize = skeletalSize;
		DeckSize = deckSize;
		if (IsLand)
		{
			ArmySize = _cachedLandArmyCount;
		}
		else
		{
			float cachedArmySizeRatio = _cachedArmySizeRatio;
			ArmySize = MathF.Round(MathF.Lerp(MinArmySize, MaxArmySize, cachedArmySizeRatio));
			_cachedArmySizeRatio = cachedArmySizeRatio;
		}
		UpdateIsWarned();
	}

	private void UpdateIsWarned()
	{
		if (IsLand)
		{
			IsWarned = false;
			WarningText = null;
			return;
		}
		IsWarned = ArmySize < SkeletalSize;
		if (IsWarned)
		{
			WarningText = new TextObject("{=nkIeNadI}Ships may be undercrewed!").ToString();
		}
		else if (ArmySize > DeckSize)
		{
			WarningText = new TextObject("{=JaFgzRhS}{AMOUNT} troops in reserve").SetTextVariable("AMOUNT", ArmySize - DeckSize).ToString();
		}
		else
		{
			WarningText = null;
		}
	}
}
