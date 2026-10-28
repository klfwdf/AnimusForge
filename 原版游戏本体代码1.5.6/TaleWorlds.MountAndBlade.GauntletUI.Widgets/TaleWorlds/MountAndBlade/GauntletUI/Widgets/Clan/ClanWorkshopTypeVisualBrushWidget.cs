using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Clan;

public class ClanWorkshopTypeVisualBrushWidget(UIContext context) : BrushWidget(context)
{
	private string _workshopType = "";

	[Editor(false)]
	public string WorkshopType
	{
		get
		{
			return _workshopType;
		}
		set
		{
			if (_workshopType != value)
			{
				_workshopType = value;
				OnPropertyChanged(value, "WorkshopType");
				SetVisualState(value);
			}
		}
	}

	private void SetVisualState(string type)
	{
		this.RegisterBrushStatesOfWidget();
		SetState(type);
	}
}
