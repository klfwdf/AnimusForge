using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Kingdom;

public class KingdomClanTypeVisualBrushWidget : BrushWidget
{
	private int _type = -1;

	[Editor(false)]
	public int Type
	{
		get
		{
			return _type;
		}
		set
		{
			if (_type != value)
			{
				_type = value;
				OnPropertyChanged(value, "Type");
				UpdateTypeVisual();
			}
		}
	}

	public KingdomClanTypeVisualBrushWidget(UIContext context)
		: base(context)
	{
	}

	private void UpdateTypeVisual()
	{
		if (Type == 0)
		{
			SetState("Normal");
		}
		else if (Type == 1)
		{
			SetState("Leader");
		}
		else if (Type == 2)
		{
			SetState("Mercenary");
		}
		else
		{
			Debug.FailedAssert("This clan type is not defined in widget", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.GauntletUI.Widgets\\Kingdom\\KingdomClanTypeVisualBrushWidget.cs", "UpdateTypeVisual", 37);
		}
	}
}
