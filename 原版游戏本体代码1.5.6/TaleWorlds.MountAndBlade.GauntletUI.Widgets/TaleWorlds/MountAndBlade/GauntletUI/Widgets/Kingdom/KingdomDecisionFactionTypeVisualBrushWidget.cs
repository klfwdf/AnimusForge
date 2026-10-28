using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Kingdom;

public class KingdomDecisionFactionTypeVisualBrushWidget(UIContext context) : BrushWidget(context)
{
	private string _factionName = "";

	[Editor(false)]
	public string FactionName
	{
		get
		{
			return _factionName;
		}
		set
		{
			if (_factionName != value)
			{
				_factionName = value;
				OnPropertyChanged(value, "FactionName");
				if (value != null)
				{
					SetVisualState(value);
				}
			}
		}
	}

	private void SetVisualState(string type)
	{
		this.RegisterBrushStatesOfWidget();
		SetState(type);
	}
}
