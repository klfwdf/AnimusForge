using System.Collections.Generic;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Mission.MainAgentControlMode;

public class MainAgentControlModeParentWidget : Widget
{
	private List<ButtonWidget> _selectionItems;

	private float _animationTimer = float.MaxValue;

	private bool _isSelectionItemsDirty;

	private bool _isActive;

	private float _animationFirstStepDuration;

	private float _animationSecondStepDuration;

	private string _childItemId;

	private ListPanel _controlModesList;

	private Widget _selectionIndicatorWidget;

	public bool IsActive
	{
		get
		{
			return _isActive;
		}
		set
		{
			if (value != _isActive)
			{
				_isActive = value;
				OnPropertyChanged(value, "IsActive");
			}
		}
	}

	public float AnimationFirstStepDuration
	{
		get
		{
			return _animationFirstStepDuration;
		}
		set
		{
			if (value != _animationFirstStepDuration)
			{
				_animationFirstStepDuration = value;
				OnPropertyChanged(value, "AnimationFirstStepDuration");
			}
		}
	}

	public float AnimationSecondStepDuration
	{
		get
		{
			return _animationSecondStepDuration;
		}
		set
		{
			if (value != _animationSecondStepDuration)
			{
				_animationSecondStepDuration = value;
				OnPropertyChanged(value, "AnimationSecondStepDuration");
			}
		}
	}

	public string ChildItemId
	{
		get
		{
			return _childItemId;
		}
		set
		{
			if (value != _childItemId)
			{
				_childItemId = value;
				OnPropertyChanged(value, "ChildItemId");
			}
		}
	}

	public ListPanel ControlModesList
	{
		get
		{
			return _controlModesList;
		}
		set
		{
			if (value != _controlModesList)
			{
				if (_controlModesList != null)
				{
					_controlModesList.EventFire -= OnControlModesListUpdated;
				}
				_controlModesList = value;
				OnPropertyChanged(value, "ControlModesList");
				if (_controlModesList != null)
				{
					_controlModesList.EventFire += OnControlModesListUpdated;
				}
				_isSelectionItemsDirty = true;
			}
		}
	}

	public Widget SelectionIndicatorWidget
	{
		get
		{
			return _selectionIndicatorWidget;
		}
		set
		{
			if (value != _selectionIndicatorWidget)
			{
				_selectionIndicatorWidget = value;
				OnPropertyChanged(value, "SelectionIndicatorWidget");
			}
		}
	}

	public MainAgentControlModeParentWidget(UIContext context)
		: base(context)
	{
		_selectionItems = new List<ButtonWidget>();
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (_isSelectionItemsDirty && _selectionItems != null && !string.IsNullOrEmpty(ChildItemId))
		{
			UnregisterChildEvents();
			_selectionItems = _controlModesList.FindChildrenWithId<ButtonWidget>(ChildItemId, includeAllChildren: true);
			RegisterChildEvents();
			_isSelectionItemsDirty = false;
		}
		AnimationTick(dt);
	}

	private void OnControlModesListUpdated(Widget widget, string eventName, object[] args)
	{
		if (eventName == "ItemAdd" || eventName == "ItemRemove")
		{
			_isSelectionItemsDirty = true;
		}
	}

	private void OnControlItemUpdated(PropertyOwnerObject widget, string propertyName, bool value)
	{
		if (propertyName == "IsSelected" && !IsActive)
		{
			StartIndicatorAnimation();
		}
	}

	private void StartIndicatorAnimation()
	{
		_animationTimer = 0f;
	}

	private void AnimationTick(float dt)
	{
		if (SelectionIndicatorWidget != null)
		{
			if (_animationTimer < AnimationFirstStepDuration + AnimationSecondStepDuration)
			{
				float value = ((_animationTimer < AnimationFirstStepDuration) ? (_animationTimer / AnimationFirstStepDuration) : ((_animationTimer - AnimationFirstStepDuration) / AnimationSecondStepDuration));
				value = Mathf.Clamp(value, 0f, 1f);
				float end = ((_animationTimer < AnimationFirstStepDuration) ? 1f : 0f);
				float alphaFactor = Mathf.Lerp((_animationTimer < AnimationFirstStepDuration) ? 0f : 1f, end, value);
				SelectionIndicatorWidget.SetGlobalAlphaRecursively(alphaFactor);
				_animationTimer += dt;
			}
			else if (SelectionIndicatorWidget.AlphaFactor > 0f)
			{
				SelectionIndicatorWidget.SetGlobalAlphaRecursively(0f);
			}
		}
	}

	private void RegisterChildEvents()
	{
		for (int i = 0; i < _selectionItems.Count; i++)
		{
			_selectionItems[i].boolPropertyChanged += OnControlItemUpdated;
		}
	}

	private void UnregisterChildEvents()
	{
		for (int i = 0; i < _selectionItems.Count; i++)
		{
			_selectionItems[i].boolPropertyChanged -= OnControlItemUpdated;
		}
	}
}
