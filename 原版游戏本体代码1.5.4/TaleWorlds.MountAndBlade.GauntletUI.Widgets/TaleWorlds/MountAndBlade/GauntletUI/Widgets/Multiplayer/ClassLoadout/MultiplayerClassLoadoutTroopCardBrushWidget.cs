using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.ClassLoadout;

public class MultiplayerClassLoadoutTroopCardBrushWidget : BrushWidget
{
	private string _cultureID;

	private BrushWidget _border;

	private BrushWidget _classBorder;

	private BrushWidget _classFrame;

	[Editor(false)]
	public string CultureID
	{
		get
		{
			return _cultureID;
		}
		set
		{
			if (value != _cultureID)
			{
				_cultureID = value;
				OnPropertyChanged(value, "CultureID");
				OnCultureIDUpdated();
			}
		}
	}

	[Editor(false)]
	public BrushWidget Border
	{
		get
		{
			return _border;
		}
		set
		{
			if (value != _border)
			{
				_border = value;
				OnPropertyChanged(value, "Border");
				OnCultureIDUpdated();
			}
		}
	}

	[Editor(false)]
	public BrushWidget ClassBorder
	{
		get
		{
			return _classBorder;
		}
		set
		{
			if (value != _classBorder)
			{
				_classBorder = value;
				OnPropertyChanged(value, "ClassBorder");
				OnCultureIDUpdated();
			}
		}
	}

	[Editor(false)]
	public BrushWidget ClassFrame
	{
		get
		{
			return _classFrame;
		}
		set
		{
			if (value != _classFrame)
			{
				_classFrame = value;
				OnPropertyChanged(value, "ClassFrame");
				OnPropertyChanged(value, "ClassFrame");
			}
		}
	}

	public MultiplayerClassLoadoutTroopCardBrushWidget(UIContext context)
		: base(context)
	{
	}

	private void OnCultureIDUpdated()
	{
		if (CultureID != null)
		{
			SetState(CultureID);
			Border?.SetState(CultureID);
			ClassBorder?.SetState(CultureID);
			ClassFrame?.SetState(CultureID);
		}
	}
}
