using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Clan;

public class ClanWorkshopTypeVisualBrushWidget : BrushWidget
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

	public ClanWorkshopTypeVisualBrushWidget(UIContext context)
		: base(context)
	{
	}

	private void SetVisualState(string type)
	{
		this.RegisterBrushStatesOfWidget();
		SetState(type);
	}
}
