using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.ExtraWidgets;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Map.Menu.Overlay;

public class ArmyOverlayCohesionFillBarWidget : FillBarWidget
{
	private bool _isWarningDirty = true;

	private bool _isCohesionWarningEnabled;

	private bool _isArmyLeader;

	[Editor(false)]
	public bool IsCohesionWarningEnabled
	{
		get
		{
			return _isCohesionWarningEnabled;
		}
		set
		{
			if (value != _isCohesionWarningEnabled)
			{
				_isCohesionWarningEnabled = value;
				OnPropertyChanged(value, "IsCohesionWarningEnabled");
				DetermineBarAnimState();
				_isWarningDirty = true;
			}
		}
	}

	[Editor(false)]
	public bool IsArmyLeader
	{
		get
		{
			return _isArmyLeader;
		}
		set
		{
			if (value != _isArmyLeader)
			{
				_isArmyLeader = value;
				OnPropertyChanged(value, "IsArmyLeader");
				DetermineBarAnimState();
				_isWarningDirty = true;
			}
		}
	}

	public ArmyOverlayCohesionFillBarWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (_isWarningDirty)
		{
			DetermineBarAnimState();
			_isWarningDirty = false;
		}
	}

	private void DetermineBarAnimState()
	{
		if (base.FillWidget == null || !(base.FillWidget is BrushWidget brushWidget))
		{
			return;
		}
		brushWidget.RegisterBrushStatesOfWidget();
		if (IsCohesionWarningEnabled)
		{
			if (brushWidget.CurrentState == "WarningLeader")
			{
				brushWidget.BrushRenderer.RestartAnimation();
			}
			else if (IsArmyLeader)
			{
				brushWidget.SetState("WarningLeader");
			}
			else
			{
				brushWidget.SetState("WarningNormal");
			}
		}
		else if (brushWidget.CurrentState == "Default")
		{
			brushWidget.BrushRenderer.RestartAnimation();
		}
		else
		{
			brushWidget.SetState("Default");
		}
	}
}
