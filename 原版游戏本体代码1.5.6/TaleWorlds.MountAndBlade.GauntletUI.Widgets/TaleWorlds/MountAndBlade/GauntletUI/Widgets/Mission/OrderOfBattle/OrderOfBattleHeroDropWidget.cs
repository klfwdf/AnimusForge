using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Mission.OrderOfBattle;

public class OrderOfBattleHeroDropWidget : ButtonWidget
{
	private int _formationClass;

	[DataSourceProperty]
	public int FormationClass
	{
		get
		{
			return _formationClass;
		}
		set
		{
			if (value != _formationClass)
			{
				_formationClass = value;
				OnPropertyChanged(value, "FormationClass");
			}
		}
	}

	public OrderOfBattleHeroDropWidget(UIContext context)
		: base(context)
	{
	}

	protected override bool OnPreviewDrop()
	{
		HandleSoundEvent();
		return true;
	}

	protected override void HandleClick()
	{
		HandleSoundEvent();
		base.HandleClick();
	}

	private void HandleSoundEvent()
	{
		switch (FormationClass)
		{
		case 1:
			EventFired("Infantry");
			break;
		case 2:
			EventFired("Archers");
			break;
		case 3:
			EventFired("Cavalry");
			break;
		case 4:
			EventFired("HorseArchers");
			break;
		case 5:
			EventFired("InfantryArchers");
			break;
		case 6:
			EventFired("CavalryHorseArchers");
			break;
		case 0:
			break;
		}
	}

	protected override bool OnPreviewDragHover()
	{
		return true;
	}
}
