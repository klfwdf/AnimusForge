using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Mission;

public class FormationMarkerTeamTypeBrushWidget : BrushWidget
{
	private int _teamType;

	public int TeamType
	{
		get
		{
			return _teamType;
		}
		set
		{
			if (_teamType != value)
			{
				_teamType = value;
				OnPropertyChanged(value, "TeamType");
				UpdateState();
			}
		}
	}

	public FormationMarkerTeamTypeBrushWidget(UIContext context)
		: base(context)
	{
	}

	private void UpdateState()
	{
		this.RegisterBrushStatesOfWidget();
		if (TeamType == 0)
		{
			SetState("Player");
		}
		else if (TeamType == 1)
		{
			SetState("Ally");
		}
		else
		{
			SetState("Enemy");
		}
	}
}
