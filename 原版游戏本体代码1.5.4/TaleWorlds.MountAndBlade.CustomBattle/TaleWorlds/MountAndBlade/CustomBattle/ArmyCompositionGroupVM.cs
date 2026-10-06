using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.CustomBattle.CustomBattle;

namespace TaleWorlds.MountAndBlade.CustomBattle;

public class ArmyCompositionGroupVM : ViewModel
{
	public int[] CompositionValues;

	private bool _updatingSliders;

	private BasicCultureObject _selectedCulture;

	private readonly MBReadOnlyList<SkillObject> _allSkills = Game.Current.ObjectManager.GetObjectTypeList<SkillObject>();

	private readonly List<BasicCharacterObject> _allCharacterObjects = new List<BasicCharacterObject>();

	private ArmyCompositionItemVM _meleeInfantryComposition;

	private ArmyCompositionItemVM _rangedInfantryComposition;

	private ArmyCompositionItemVM _meleeCavalryComposition;

	private ArmyCompositionItemVM _rangedCavalryComposition;

	private int _armySize;

	private int _maxArmySize;

	private int _minArmySize;

	private string _armySizeTitle;

	[DataSourceProperty]
	public ArmyCompositionItemVM MeleeInfantryComposition
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
	public ArmyCompositionItemVM RangedInfantryComposition
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
	public ArmyCompositionItemVM MeleeCavalryComposition
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
	public ArmyCompositionItemVM RangedCavalryComposition
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
			}
		}
	}

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

	public ArmyCompositionGroupVM(TroopTypeSelectionPopUpVM troopTypeSelectionPopUp)
	{
		MinArmySize = 1;
		MaxArmySize = BannerlordConfig.MaxBattleSize;
		foreach (BasicCharacterObject item in from c in Game.Current.ObjectManager.GetObjectTypeList<BasicCharacterObject>()
			where c.IsSoldier && !c.IsObsolete
			select c)
		{
			_allCharacterObjects.Add(item);
		}
		CompositionValues = new int[4];
		CompositionValues[0] = 25;
		CompositionValues[1] = 25;
		CompositionValues[2] = 25;
		CompositionValues[3] = 25;
		MeleeInfantryComposition = new ArmyCompositionItemVM(ArmyCompositionItemVM.CompositionType.MeleeInfantry, _allCharacterObjects, _allSkills, UpdateSliders, troopTypeSelectionPopUp, CompositionValues);
		RangedInfantryComposition = new ArmyCompositionItemVM(ArmyCompositionItemVM.CompositionType.RangedInfantry, _allCharacterObjects, _allSkills, UpdateSliders, troopTypeSelectionPopUp, CompositionValues);
		MeleeCavalryComposition = new ArmyCompositionItemVM(ArmyCompositionItemVM.CompositionType.MeleeCavalry, _allCharacterObjects, _allSkills, UpdateSliders, troopTypeSelectionPopUp, CompositionValues);
		RangedCavalryComposition = new ArmyCompositionItemVM(ArmyCompositionItemVM.CompositionType.RangedCavalry, _allCharacterObjects, _allSkills, UpdateSliders, troopTypeSelectionPopUp, CompositionValues);
		ArmySize = BannerlordConfig.GetRealBattleSize() / 5;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		ArmySizeTitle = GameTexts.FindText("str_army_size").ToString();
		MeleeInfantryComposition.RefreshValues();
		RangedInfantryComposition.RefreshValues();
		MeleeCavalryComposition.RefreshValues();
		RangedCavalryComposition.RefreshValues();
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

	private void SetArmyCompositionValue(int index, int value, ArmyCompositionItemVM composition)
	{
		CompositionValues[index] = value;
		composition.RefreshCompositionValue();
	}

	public void ExecuteRandomize(ArmyCompositionGroupVM oppositeSide = null)
	{
		if (oppositeSide == null)
		{
			ArmySize = MBRandom.RandomInt(MinArmySize, MaxArmySize);
		}
		else
		{
			float amount = MBRandom.RandomFloatGaussian((float)(oppositeSide.ArmySize - oppositeSide.MinArmySize) / (float)(oppositeSide.MaxArmySize - oppositeSide.MinArmySize), 0.2f, 0f, 1f);
			ArmySize = MathF.Round(MathF.Lerp(MinArmySize, MaxArmySize, amount));
		}
		int num = MBRandom.RandomInt(100);
		int num2 = MBRandom.RandomInt(100);
		int num3 = MBRandom.RandomInt(100);
		int num4 = MBRandom.RandomInt(100);
		int num5 = num + num2 + num3 + num4;
		int num6 = MathF.Round(100f * ((float)num / (float)num5));
		int num7 = MathF.Round(100f * ((float)num2 / (float)num5));
		int num8 = MathF.Round(100f * ((float)num3 / (float)num5));
		int compositionValue = 100 - (num6 + num7 + num8);
		MeleeInfantryComposition.ExecuteRandomize(num6);
		RangedInfantryComposition.ExecuteRandomize(num7);
		MeleeCavalryComposition.ExecuteRandomize(num8);
		RangedCavalryComposition.ExecuteRandomize(compositionValue);
	}

	public void OnPlayerTypeChange(CustomBattlePlayerType playerType)
	{
		MinArmySize = ((playerType == CustomBattlePlayerType.Commander) ? 1 : 2);
		ArmySize = (int)MathF.Clamp(ArmySize, MinArmySize, MaxArmySize);
	}
}
