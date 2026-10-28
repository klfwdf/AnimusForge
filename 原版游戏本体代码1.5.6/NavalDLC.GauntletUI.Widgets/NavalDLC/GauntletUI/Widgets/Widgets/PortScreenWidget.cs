using System.Collections.Generic;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace NavalDLC.GauntletUI.Widgets.Widgets;

public class PortScreenWidget : Widget
{
	private float _alphaChangeTimeElapsed;

	private float _initialAlpha = 1f;

	private float _targetAlpha;

	private float _currentAlpha = 1f;

	private bool _isTransitioning;

	private bool _isAnyUpgradeSlotSelected;

	private Widget _upgradesPanel;

	private Widget _slotsPanel;

	private bool _isControllingCamera;

	private float _cameraEnabledAlpha = 0.2f;

	private Widget _topPanel;

	private Widget _bottomPanel;

	private Widget _leftPanel;

	private Widget _rightPanel;

	private PortPieceInspectionWidget _inspectionPanelWidget;

	private PortUpgradesPanelArrowWidget _upgradesPanelArrowWidget;

	public float AlphaChangeDuration { get; set; } = 0.15f;

	[Editor(false)]
	public bool IsAnyUpgradeSlotSelected
	{
		get
		{
			return _isAnyUpgradeSlotSelected;
		}
		set
		{
			if (value != _isAnyUpgradeSlotSelected)
			{
				_isAnyUpgradeSlotSelected = value;
				OnPropertyChanged(value, "IsAnyUpgradeSlotSelected");
			}
		}
	}

	[Editor(false)]
	public Widget UpgradesPanel
	{
		get
		{
			return _upgradesPanel;
		}
		set
		{
			if (value != _upgradesPanel)
			{
				_upgradesPanel = value;
				OnPropertyChanged(value, "UpgradesPanel");
			}
		}
	}

	[Editor(false)]
	public Widget SlotsPanel
	{
		get
		{
			return _slotsPanel;
		}
		set
		{
			if (value != _slotsPanel)
			{
				_slotsPanel = value;
				OnPropertyChanged(value, "SlotsPanel");
			}
		}
	}

	[Editor(false)]
	public bool IsControllingCamera
	{
		get
		{
			return _isControllingCamera;
		}
		set
		{
			if (value != _isControllingCamera)
			{
				_isControllingCamera = value;
				OnPropertyChanged(value, "IsControllingCamera");
				OnCameraControlsEnabledChanged();
			}
		}
	}

	[Editor(false)]
	public float CameraEnabledAlpha
	{
		get
		{
			return _cameraEnabledAlpha;
		}
		set
		{
			if (value != _cameraEnabledAlpha)
			{
				_cameraEnabledAlpha = value;
				OnPropertyChanged(value, "CameraEnabledAlpha");
			}
		}
	}

	[Editor(false)]
	public Widget TopPanel
	{
		get
		{
			return _topPanel;
		}
		set
		{
			if (value != _topPanel)
			{
				_topPanel = value;
				OnPropertyChanged(value, "TopPanel");
			}
		}
	}

	[Editor(false)]
	public Widget BottomPanel
	{
		get
		{
			return _bottomPanel;
		}
		set
		{
			if (value != _bottomPanel)
			{
				_bottomPanel = value;
				OnPropertyChanged(value, "BottomPanel");
			}
		}
	}

	[Editor(false)]
	public Widget LeftPanel
	{
		get
		{
			return _leftPanel;
		}
		set
		{
			if (value != _leftPanel)
			{
				_leftPanel = value;
				OnPropertyChanged(value, "LeftPanel");
			}
		}
	}

	[Editor(false)]
	public Widget RightPanel
	{
		get
		{
			return _rightPanel;
		}
		set
		{
			if (value != _rightPanel)
			{
				_rightPanel = value;
				OnPropertyChanged(value, "RightPanel");
			}
		}
	}

	[Editor(false)]
	public PortPieceInspectionWidget InspectionPanelWidget
	{
		get
		{
			return _inspectionPanelWidget;
		}
		set
		{
			if (value != _inspectionPanelWidget)
			{
				_inspectionPanelWidget = value;
				OnPropertyChanged(value, "InspectionPanelWidget");
			}
		}
	}

	[Editor(false)]
	public PortUpgradesPanelArrowWidget UpgradesPanelArrowWidget
	{
		get
		{
			return _upgradesPanelArrowWidget;
		}
		set
		{
			if (value != _upgradesPanelArrowWidget)
			{
				_upgradesPanelArrowWidget = value;
				OnPropertyChanged(value, "UpgradesPanelArrowWidget");
			}
		}
	}

	public PortScreenWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (!IsAnyUpgradeSlotSelected)
		{
			return;
		}
		Widget upgradesPanel = UpgradesPanel;
		if (upgradesPanel == null || !upgradesPanel.IsPointInsideMeasuredArea(base.EventManager.MousePosition))
		{
			Widget slotsPanel = SlotsPanel;
			if (slotsPanel == null || !slotsPanel.IsPointInsideMeasuredArea(base.EventManager.MousePosition))
			{
				HandleClickOutside();
			}
		}
	}

	private void HandleClickOutside()
	{
		InputKey[] clickKeys = base.Context.InputContext.GetClickKeys();
		for (int i = 0; i < clickKeys.Length; i++)
		{
			if (Input.IsKeyPressed(clickKeys[i]))
			{
				EventFired("DeselectSlot");
				break;
			}
		}
	}

	protected override void OnLateUpdate(float dt)
	{
		if (_isTransitioning)
		{
			if (_alphaChangeTimeElapsed < AlphaChangeDuration)
			{
				_currentAlpha = MathF.Lerp(_initialAlpha, _targetAlpha, _alphaChangeTimeElapsed / AlphaChangeDuration);
				TopPanel?.SetGlobalAlphaRecursively(_currentAlpha);
				BottomPanel?.SetGlobalAlphaRecursively(_currentAlpha);
				LeftPanel?.SetGlobalAlphaRecursively(_currentAlpha);
				RightPanel?.SetGlobalAlphaRecursively(_currentAlpha);
				_alphaChangeTimeElapsed += dt;
			}
			else
			{
				_currentAlpha = _targetAlpha;
				TopPanel?.SetGlobalAlphaRecursively(_currentAlpha);
				BottomPanel?.SetGlobalAlphaRecursively(_currentAlpha);
				LeftPanel?.SetGlobalAlphaRecursively(_currentAlpha);
				RightPanel?.SetGlobalAlphaRecursively(_currentAlpha);
				_isTransitioning = false;
			}
		}
		if (InspectionPanelWidget != null)
		{
			UpdateInspectionPanelWidget();
		}
		if (UpgradesPanel != null && UpgradesPanelArrowWidget != null)
		{
			UpdateUpgradesPanelArrowWidget();
		}
	}

	private void UpdateInspectionPanelWidget()
	{
		List<Widget> mouseOveredWidgets = base.EventManager.MouseOveredWidgets;
		for (int i = 0; i < mouseOveredWidgets.Count; i++)
		{
			if (mouseOveredWidgets[i] is PortInspectionParentWidget targetPiece)
			{
				InspectionPanelWidget.SetTargetPiece(targetPiece);
				break;
			}
		}
	}

	private void UpdateUpgradesPanelArrowWidget()
	{
		Widget targetSlot = null;
		List<PortInspectionParentWidget> allChildrenOfTypeRecursive = SlotsPanel.GetAllChildrenOfTypeRecursive<PortInspectionParentWidget>();
		for (int i = 0; i < allChildrenOfTypeRecursive.Count; i++)
		{
			PortInspectionParentWidget portInspectionParentWidget = allChildrenOfTypeRecursive[i];
			if (portInspectionParentWidget.GetFirstInChildrenRecursive((Widget x) => x is ButtonWidget buttonWidget && buttonWidget.IsSelected) != null)
			{
				targetSlot = portInspectionParentWidget;
				break;
			}
		}
		UpgradesPanelArrowWidget.SetTargetSlot(targetSlot);
	}

	private void OnCameraControlsEnabledChanged()
	{
		_alphaChangeTimeElapsed = 0f;
		_targetAlpha = (IsControllingCamera ? CameraEnabledAlpha : 1f);
		_initialAlpha = _currentAlpha;
		_isTransitioning = true;
	}
}
