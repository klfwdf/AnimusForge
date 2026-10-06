using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.Order;

public class OrderTroopItemFormationClassVM : ViewModel
{
	public readonly FormationClass FormationClass;

	private readonly Formation _formation;

	private int _formationClassValue;

	private int _troopCount;

	[DataSourceProperty]
	public int FormationClassValue
	{
		get
		{
			return _formationClassValue;
		}
		set
		{
			if (value != _formationClassValue)
			{
				_formationClassValue = value;
				OnPropertyChangedWithValue(value, "FormationClassValue");
			}
		}
	}

	[DataSourceProperty]
	public int TroopCount
	{
		get
		{
			return _troopCount;
		}
		set
		{
			if (value != _troopCount)
			{
				_troopCount = value;
				OnPropertyChangedWithValue(value, "TroopCount");
			}
		}
	}

	public OrderTroopItemFormationClassVM(Formation formation, FormationClass formationClass)
	{
		_formation = formation;
		FormationClass = formationClass;
		FormationClassValue = (int)formationClass;
	}

	public void UpdateTroopCount()
	{
		switch (FormationClass)
		{
		case FormationClass.Infantry:
			TroopCount = _formation.GetCountOfUnitsBelongingToLogicalClass(FormationClass.Infantry);
			break;
		case FormationClass.Ranged:
			TroopCount = _formation.GetCountOfUnitsBelongingToLogicalClass(FormationClass.Ranged);
			break;
		case FormationClass.Cavalry:
			TroopCount = _formation.GetCountOfUnitsBelongingToLogicalClass(FormationClass.Cavalry);
			break;
		case FormationClass.HorseArcher:
			TroopCount = _formation.GetCountOfUnitsBelongingToLogicalClass(FormationClass.HorseArcher);
			break;
		}
	}
}
