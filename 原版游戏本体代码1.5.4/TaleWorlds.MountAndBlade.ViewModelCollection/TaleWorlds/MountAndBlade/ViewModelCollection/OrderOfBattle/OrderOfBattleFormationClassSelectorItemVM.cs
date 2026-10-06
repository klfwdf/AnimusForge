using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.OrderOfBattle;

public class OrderOfBattleFormationClassSelectorItemVM : SelectorItemVM
{
	public readonly DeploymentFormationClass FormationClass;

	private int _formationClassInt;

	[DataSourceProperty]
	public int FormationClassInt
	{
		get
		{
			return _formationClassInt;
		}
		set
		{
			if (value != _formationClassInt)
			{
				_formationClassInt = value;
				OnPropertyChangedWithValue(value, "FormationClassInt");
			}
		}
	}

	public OrderOfBattleFormationClassSelectorItemVM(DeploymentFormationClass formationClass)
		: base(formationClass.ToString())
	{
		FormationClass = formationClass;
		FormationClassInt = (int)formationClass;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.Hint = new HintViewModel(FormationClass.GetClassName());
	}
}
