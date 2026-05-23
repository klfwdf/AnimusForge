using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using TaleWorlds.GauntletUI.GamepadNavigation;
using TaleWorlds.GauntletUI.Layout;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.BaseTypes;

public class Widget : PropertyOwnerObject
{
	private Color _color = Color.White;

	private List<WidgetComponent> _components;

	private float _rotation;

	private float _pivotX;

	private float _pivotY;

	private Vector2 _topLeft;

	private string _id;

	private bool _isGamepadCursorAreaDirty;

	private Rectangle2D _gamepadCursorAreaRect;

	public Rectangle2D AreaRect;

	private Widget _parent;

	private List<Widget> _children;

	private List<Widget> _childRenderBuffer;

	private bool _isRendering;

	private bool _doNotUseCustomScaleAndChildren;

	protected bool _calculateSizeFirstFrame = true;

	private float _suggestedWidth;

	private float _suggestedHeight;

	private string _hoveredCursorState;

	private bool _alternateClickEventHasSpecialEvent;

	private Vector2 _positionOffset;

	private float _marginTop;

	private float _marginLeft;

	private float _marginBottom;

	private float _marginRight;

	private VerticalAlignment _verticalAlignment;

	private HorizontalAlignment _horizontalAlignment;

	private bool _forcePixelPerfectRenderPlacement;

	private SizePolicy _widthSizePolicy;

	private SizePolicy _heightSizePolicy;

	private Widget _dragWidget;

	private bool _isHovered;

	private bool _isDisabled;

	private bool _isFocusable;

	private bool _isFocused;

	private bool _restartAnimationFirstFrame;

	private bool _doNotPassEventsToChildren;

	private bool _doNotAcceptEvents;

	public Func<Widget, Widget, bool> AcceptDropHandler;

	private bool _isPressed;

	private bool _isHidden;

	private Sprite _sprite;

	private VisualDefinition _visualDefinition;

	private List<string> _states;

	protected float _stateTimer;

	protected VisualState _startVisualState;

	protected VisualStateAnimationState _currentVisualStateAnimationState;

	private bool _updateChildrenStates;

	protected int _seed;

	private bool _seedSet;

	private float _maxWidth;

	private float _maxHeight;

	private float _minWidth;

	private float _minHeight;

	private bool _gotMaxWidth;

	private bool _gotMaxHeight;

	private bool _gotMinWidth;

	private bool _gotMinHeight;

	private bool _isInParallelOperation;

	private List<Action<Widget, string, object[]>> _eventTargets;

	private bool _doNotAcceptNavigation;

	private bool _isUsingNavigation;

	private bool _useSiblingIndexForNavigation;

	protected internal int _gamepadNavigationIndex = -1;

	private GamepadNavigationTypes _usedNavigationMovements;

	public Action<Widget> OnGamepadNavigationFocusGained;

	public float ColorFactor { get; set; } = 1f;

	public float AlphaFactor { get; set; } = 1f;

	public float ValueFactor { get; set; }

	public float SaturationFactor { get; set; }

	public float ExtendLeft { get; set; }

	public float ExtendRight { get; set; }

	public float ExtendTop { get; set; }

	public float ExtendBottom { get; set; }

	public bool VerticalFlip { get; set; }

	public bool HorizontalFlip { get; set; }

	public int NinePatchTop { get; set; }

	public int NinePatchBottom { get; set; }

	public int NinePatchLeft { get; set; }

	public int NinePatchRight { get; set; }

	public ImageFit ImageFit { get; set; }

	public float GlobalRotation
	{
		get
		{
			if (ParentWidget != null)
			{
				return Rotation + ParentWidget.GlobalRotation;
			}
			return Rotation;
		}
	}

	public float Rotation
	{
		get
		{
			return _rotation;
		}
		set
		{
			if (value != _rotation)
			{
				_rotation = value;
				SetMeasureAndLayoutDirty();
				EventManager.SetPositionsDirty();
			}
		}
	}

	public float PivotX
	{
		get
		{
			return _pivotX;
		}
		set
		{
			if (value != _pivotX)
			{
				_pivotX = value;
				SetMeasureAndLayoutDirty();
				EventManager.SetPositionsDirty();
			}
		}
	}

	public float PivotY
	{
		get
		{
			return _pivotY;
		}
		set
		{
			if (value != _pivotY)
			{
				_pivotY = value;
				SetMeasureAndLayoutDirty();
				EventManager.SetPositionsDirty();
			}
		}
	}

	public float Left
	{
		get
		{
			return _topLeft.X;
		}
		private set
		{
			if (value != _topLeft.X)
			{
				_topLeft.X = value;
				EventManager.SetPositionsDirty();
			}
		}
	}

	public float Top
	{
		get
		{
			return _topLeft.Y;
		}
		private set
		{
			if (value != _topLeft.Y)
			{
				_topLeft.Y = value;
				EventManager.SetPositionsDirty();
			}
		}
	}

	public Vector2 Size { get; private set; }

	public bool FrictionEnabled { get; set; }

	public Color Color
	{
		get
		{
			return _color;
		}
		set
		{
			if (_color != value)
			{
				_color = value;
			}
		}
	}

	[Editor(false)]
	public string Id
	{
		get
		{
			return _id;
		}
		set
		{
			if (_id != value)
			{
				_id = value;
				OnPropertyChanged(value, "Id");
			}
		}
	}

	public Vector2 LocalPosition { get; private set; }

	public Vector2 GlobalPosition
	{
		get
		{
			if (ParentWidget != null)
			{
				return LocalPosition + ParentWidget.GlobalPosition;
			}
			return LocalPosition;
		}
	}

	public Rectangle2D GamepadCursorAreaRect
	{
		get
		{
			if (_isGamepadCursorAreaDirty)
			{
				Vector2 localPosition = AreaRect.LocalPosition;
				Vector2 localScale = AreaRect.LocalScale;
				_gamepadCursorAreaRect = AreaRect;
				float num = ExtendCursorAreaLeft * _scaleToUse;
				float num2 = ExtendCursorAreaTop * _scaleToUse;
				float num3 = ExtendCursorAreaRight * _scaleToUse;
				float num4 = ExtendCursorAreaBottom * _scaleToUse;
				_gamepadCursorAreaRect.LocalPosition = new Vector2(localPosition.X - num, localPosition.Y - num2);
				_gamepadCursorAreaRect.LocalScale = new Vector2(localScale.X + num + num3, localScale.Y + num2 + num4);
				if (ParentWidget != null)
				{
					_gamepadCursorAreaRect.CalculateMatrixFrame(in ParentWidget.AreaRect);
				}
				else
				{
					Rectangle2D parentRectangle = Rectangle2D.Invalid;
					_gamepadCursorAreaRect.CalculateMatrixFrame(in parentRectangle);
				}
			}
			return _gamepadCursorAreaRect;
		}
	}

	[Editor(false)]
	public bool DoNotUseCustomScaleAndChildren
	{
		get
		{
			return _doNotUseCustomScaleAndChildren;
		}
		set
		{
			if (_doNotUseCustomScaleAndChildren != value)
			{
				_doNotUseCustomScaleAndChildren = value;
				OnPropertyChanged(value, "DoNotUseCustomScaleAndChildren");
				DoNotUseCustomScale = value;
				ApplyActionToAllChildren(delegate(Widget child)
				{
					child.DoNotUseCustomScaleAndChildren = value;
				});
			}
		}
	}

	public bool DoNotUseCustomScale { get; set; }

	protected float _scaleToUse
	{
		get
		{
			if (!DoNotUseCustomScale)
			{
				return Context.CustomScale;
			}
			return Context.Scale;
		}
	}

	protected float _inverseScaleToUse
	{
		get
		{
			if (!DoNotUseCustomScale)
			{
				return Context.CustomInverseScale;
			}
			return Context.InverseScale;
		}
	}

	[Editor(false)]
	public float SuggestedWidth
	{
		get
		{
			return _suggestedWidth;
		}
		set
		{
			if (_suggestedWidth != value)
			{
				SetMeasureAndLayoutDirty();
				_suggestedWidth = value;
				OnPropertyChanged(value, "SuggestedWidth");
			}
		}
	}

	[Editor(false)]
	public float SuggestedHeight
	{
		get
		{
			return _suggestedHeight;
		}
		set
		{
			if (_suggestedHeight != value)
			{
				SetMeasureAndLayoutDirty();
				_suggestedHeight = value;
				OnPropertyChanged(value, "SuggestedHeight");
			}
		}
	}

	public float ScaledSuggestedWidth
	{
		get
		{
			return _scaleToUse * SuggestedWidth;
		}
		set
		{
			SuggestedWidth = value * _inverseScaleToUse;
		}
	}

	public float ScaledSuggestedHeight
	{
		get
		{
			return _scaleToUse * SuggestedHeight;
		}
		set
		{
			SuggestedHeight = value * _inverseScaleToUse;
		}
	}

	[Editor(false)]
	public string HoveredCursorState
	{
		get
		{
			return _hoveredCursorState;
		}
		set
		{
			if (_hoveredCursorState != value)
			{
				_ = _hoveredCursorState;
				_hoveredCursorState = value;
			}
		}
	}

	[Editor(false)]
	public bool AlternateClickEventHasSpecialEvent
	{
		get
		{
			return _alternateClickEventHasSpecialEvent;
		}
		set
		{
			if (_alternateClickEventHasSpecialEvent != value)
			{
				_ = _alternateClickEventHasSpecialEvent;
				_alternateClickEventHasSpecialEvent = value;
			}
		}
	}

	public Vector2 PosOffset
	{
		get
		{
			return _positionOffset;
		}
		set
		{
			if (!_positionOffset.X.ApproximatelyEqualsTo(value.X) || !_positionOffset.Y.ApproximatelyEqualsTo(value.Y))
			{
				SetLayoutDirty();
				_positionOffset = value;
				OnPropertyChanged(value, "PosOffset");
			}
		}
	}

	public Vector2 ScaledPositionOffset => _positionOffset * _scaleToUse;

	[Editor(false)]
	public float PositionXOffset
	{
		get
		{
			return _positionOffset.X;
		}
		set
		{
			if (!_positionOffset.X.ApproximatelyEqualsTo(value))
			{
				SetLayoutDirty();
				_positionOffset.X = value;
				OnPropertyChanged(value, "PositionXOffset");
			}
		}
	}

	[Editor(false)]
	public float PositionYOffset
	{
		get
		{
			return _positionOffset.Y;
		}
		set
		{
			if (!_positionOffset.Y.ApproximatelyEqualsTo(value))
			{
				SetLayoutDirty();
				_positionOffset.Y = value;
				OnPropertyChanged(value, "PositionYOffset");
			}
		}
	}

	public float ScaledPositionXOffset
	{
		get
		{
			return _positionOffset.X * _scaleToUse;
		}
		set
		{
			float num = value * _inverseScaleToUse;
			if (!num.ApproximatelyEqualsTo(_positionOffset.X))
			{
				SetLayoutDirty();
				_positionOffset.X = num;
			}
		}
	}

	public float ScaledPositionYOffset
	{
		get
		{
			return _positionOffset.Y * _scaleToUse;
		}
		set
		{
			float num = value * _inverseScaleToUse;
			if (!num.ApproximatelyEqualsTo(_positionOffset.Y))
			{
				SetLayoutDirty();
				_positionOffset.Y = num;
			}
		}
	}

	public Widget ParentWidget
	{
		get
		{
			return _parent;
		}
		set
		{
			if (ParentWidget == value)
			{
				return;
			}
			if (_parent != null)
			{
				_parent.OnBeforeChildRemoved(this);
				if (ConnectedToRoot)
				{
					EventManager.OnWidgetDisconnectedFromRoot(this);
				}
				int childIndex = _parent.GetChildIndex(this);
				_parent._children.Remove(this);
				_parent.OnAfterChildRemoved(this, childIndex);
			}
			_parent = value;
			if (_parent != null)
			{
				_parent._children.Add(this);
				if (ConnectedToRoot)
				{
					EventManager.OnWidgetConnectedToRoot(this);
				}
				_parent.OnChildAdded(this);
			}
			SetMeasureAndLayoutDirty();
		}
	}

	public EventManager EventManager => Context.EventManager;

	public IGamepadNavigationContext GamepadNavigationContext => Context.GamepadNavigation;

	public UIContext Context { get; private set; }

	public Vector2 MeasuredSize { get; private set; }

	[Editor(false)]
	public float MarginTop
	{
		get
		{
			return _marginTop;
		}
		set
		{
			if (_marginTop != value)
			{
				SetMeasureAndLayoutDirty();
				_marginTop = value;
				OnPropertyChanged(value, "MarginTop");
			}
		}
	}

	[Editor(false)]
	public float MarginLeft
	{
		get
		{
			return _marginLeft;
		}
		set
		{
			if (_marginLeft != value)
			{
				SetMeasureAndLayoutDirty();
				_marginLeft = value;
				OnPropertyChanged(value, "MarginLeft");
			}
		}
	}

	[Editor(false)]
	public float MarginBottom
	{
		get
		{
			return _marginBottom;
		}
		set
		{
			if (_marginBottom != value)
			{
				SetMeasureAndLayoutDirty();
				_marginBottom = value;
				OnPropertyChanged(value, "MarginBottom");
			}
		}
	}

	[Editor(false)]
	public float MarginRight
	{
		get
		{
			return _marginRight;
		}
		set
		{
			if (_marginRight != value)
			{
				SetMeasureAndLayoutDirty();
				_marginRight = value;
				OnPropertyChanged(value, "MarginRight");
			}
		}
	}

	public float ScaledMarginTop => _scaleToUse * MarginTop;

	public float ScaledMarginLeft => _scaleToUse * MarginLeft;

	public float ScaledMarginBottom => _scaleToUse * MarginBottom;

	public float ScaledMarginRight => _scaleToUse * MarginRight;

	[Editor(false)]
	public VerticalAlignment VerticalAlignment
	{
		get
		{
			return _verticalAlignment;
		}
		set
		{
			if (_verticalAlignment != value)
			{
				SetMeasureAndLayoutDirty();
				_verticalAlignment = value;
				switch (value)
				{
				case VerticalAlignment.Top:
					OnPropertyChanged("Top", "VerticalAlignment");
					break;
				case VerticalAlignment.Center:
					OnPropertyChanged("Center", "VerticalAlignment");
					break;
				case VerticalAlignment.Bottom:
					OnPropertyChanged("Bottom", "VerticalAlignment");
					break;
				}
			}
		}
	}

	[Editor(false)]
	public HorizontalAlignment HorizontalAlignment
	{
		get
		{
			return _horizontalAlignment;
		}
		set
		{
			if (_horizontalAlignment != value)
			{
				SetMeasureAndLayoutDirty();
				_horizontalAlignment = value;
				switch (value)
				{
				case HorizontalAlignment.Left:
					OnPropertyChanged("Left", "HorizontalAlignment");
					break;
				case HorizontalAlignment.Center:
					OnPropertyChanged("Center", "HorizontalAlignment");
					break;
				case HorizontalAlignment.Right:
					OnPropertyChanged("Right", "HorizontalAlignment");
					break;
				}
			}
		}
	}

	public float Right => _topLeft.X + Size.X;

	public float Bottom => _topLeft.Y + Size.Y;

	public int ChildCount => _children.Count;

	[Editor(false)]
	public bool ForcePixelPerfectRenderPlacement
	{
		get
		{
			return _forcePixelPerfectRenderPlacement;
		}
		set
		{
			if (_forcePixelPerfectRenderPlacement != value)
			{
				_forcePixelPerfectRenderPlacement = value;
				OnPropertyChanged(value, "ForcePixelPerfectRenderPlacement");
			}
		}
	}

	public bool UseGlobalTimeForAnimation { get; set; }

	public bool UseSpriteDimensions { get; set; }

	[Editor(false)]
	public SizePolicy WidthSizePolicy
	{
		get
		{
			return _widthSizePolicy;
		}
		set
		{
			if (value != _widthSizePolicy)
			{
				SetMeasureAndLayoutDirty();
				_widthSizePolicy = value;
			}
		}
	}

	[Editor(false)]
	public SizePolicy HeightSizePolicy
	{
		get
		{
			return _heightSizePolicy;
		}
		set
		{
			if (value != _heightSizePolicy)
			{
				SetMeasureAndLayoutDirty();
				_heightSizePolicy = value;
			}
		}
	}

	[Editor(false)]
	public bool AcceptDrag { get; set; }

	[Editor(false)]
	public bool AcceptDrop { get; set; }

	[Editor(false)]
	public bool HideOnDrag { get; set; } = true;

	[Editor(false)]
	public Widget DragWidget
	{
		get
		{
			return _dragWidget;
		}
		set
		{
			if (_dragWidget != value)
			{
				if (value != null)
				{
					_dragWidget = value;
					_dragWidget.IsVisible = false;
				}
				else
				{
					_dragWidget = null;
				}
			}
		}
	}

	[Editor(false)]
	public bool ClipContents
	{
		get
		{
			if (ClipVerticalContent)
			{
				return ClipHorizontalContent;
			}
			return false;
		}
		set
		{
			ClipHorizontalContent = value;
			ClipVerticalContent = value;
		}
	}

	[Editor(false)]
	public bool ClipHorizontalContent { get; set; }

	[Editor(false)]
	public bool ClipVerticalContent { get; set; }

	[Editor(false)]
	public bool CircularClipEnabled { get; set; }

	[Editor(false)]
	public float CircularClipRadius { get; set; }

	[Editor(false)]
	public bool IsCircularClipRadiusHalfOfWidth { get; set; }

	[Editor(false)]
	public bool IsCircularClipRadiusHalfOfHeight { get; set; }

	[Editor(false)]
	public float CircularClipSmoothingRadius { get; set; }

	[Editor(false)]
	public float CircularClipXOffset { get; set; }

	[Editor(false)]
	public float CircularClipYOffset { get; set; }

	[Editor(false)]
	public bool RenderLate { get; set; }

	[Editor(false)]
	public bool DoNotRenderIfNotFullyInsideScissor { get; set; }

	public bool FixedWidth => WidthSizePolicy == SizePolicy.Fixed;

	public bool FixedHeight => HeightSizePolicy == SizePolicy.Fixed;

	public bool IsHovered
	{
		get
		{
			return _isHovered;
		}
		private set
		{
			if (_isHovered != value)
			{
				_isHovered = value;
				RefreshState();
				OnPropertyChanged(value, "IsHovered");
			}
		}
	}

	[Editor(false)]
	public bool IsDisabled
	{
		get
		{
			return _isDisabled;
		}
		set
		{
			if (_isDisabled != value)
			{
				_isDisabled = value;
				OnPropertyChanged(value, "IsDisabled");
				RefreshState();
			}
		}
	}

	[Editor(false)]
	public bool IsFocusable
	{
		get
		{
			return _isFocusable;
		}
		set
		{
			if (_isFocusable != value)
			{
				_isFocusable = value;
				if (ConnectedToRoot)
				{
					OnPropertyChanged(value, "IsFocusable");
					RefreshState();
				}
			}
		}
	}

	public bool IsFocused
	{
		get
		{
			return _isFocused;
		}
		private set
		{
			if (_isFocused != value)
			{
				_isFocused = value;
				RefreshState();
			}
		}
	}

	[Editor(false)]
	public bool IsEnabled
	{
		get
		{
			return !IsDisabled;
		}
		set
		{
			if (value == IsDisabled)
			{
				IsDisabled = !value;
				OnPropertyChanged(value, "IsEnabled");
			}
		}
	}

	[Editor(false)]
	public bool RestartAnimationFirstFrame
	{
		get
		{
			return _restartAnimationFirstFrame;
		}
		set
		{
			if (_restartAnimationFirstFrame != value)
			{
				_restartAnimationFirstFrame = value;
			}
		}
	}

	[Editor(false)]
	public bool DoNotPassEventsToChildren
	{
		get
		{
			return _doNotPassEventsToChildren;
		}
		set
		{
			if (_doNotPassEventsToChildren != value)
			{
				_doNotPassEventsToChildren = value;
				OnPropertyChanged(value, "DoNotPassEventsToChildren");
			}
		}
	}

	[Editor(false)]
	public bool DoNotAcceptEvents
	{
		get
		{
			return _doNotAcceptEvents;
		}
		set
		{
			if (_doNotAcceptEvents != value)
			{
				_doNotAcceptEvents = value;
				OnPropertyChanged(value, "DoNotAcceptEvents");
			}
		}
	}

	[Editor(false)]
	public bool CanAcceptEvents
	{
		get
		{
			return !DoNotAcceptEvents;
		}
		set
		{
			DoNotAcceptEvents = !value;
		}
	}

	public bool IsPressed
	{
		get
		{
			return _isPressed;
		}
		internal set
		{
			if (_isPressed != value)
			{
				_isPressed = value;
				RefreshState();
			}
		}
	}

	[Editor(false)]
	public bool IsHidden
	{
		get
		{
			return _isHidden;
		}
		set
		{
			if (_isHidden != value)
			{
				SetMeasureAndLayoutDirty();
				_isHidden = value;
				RefreshState();
				OnPropertyChanged(value, "IsHidden");
				OnPropertyChanged(!value, "IsVisible");
				if (this.OnVisibilityChanged != null)
				{
					this.OnVisibilityChanged(this);
				}
			}
		}
	}

	[Editor(false)]
	public bool IsVisible
	{
		get
		{
			return !_isHidden;
		}
		set
		{
			if (value == _isHidden)
			{
				IsHidden = !value;
			}
		}
	}

	[Editor(false)]
	public Sprite Sprite
	{
		get
		{
			return _sprite;
		}
		set
		{
			if (value != _sprite)
			{
				_sprite = value;
			}
		}
	}

	[Editor(false)]
	public VisualDefinition VisualDefinition
	{
		get
		{
			return _visualDefinition;
		}
		set
		{
			if (_visualDefinition != value)
			{
				VisualDefinition visualDefinition = _visualDefinition;
				_visualDefinition = value;
				_stateTimer = 0f;
				if (ConnectedToRoot && (visualDefinition == null || _visualDefinition == null))
				{
					EventManager.OnWidgetVisualDefinitionChanged(this);
				}
			}
		}
	}

	public string CurrentState { get; protected set; } = "";

	[Editor(false)]
	public bool UpdateChildrenStates
	{
		get
		{
			return _updateChildrenStates;
		}
		set
		{
			if (_updateChildrenStates != value)
			{
				_updateChildrenStates = value;
				OnPropertyChanged(value, "UpdateChildrenStates");
				if (value && ChildCount > 0)
				{
					SetState(CurrentState);
				}
			}
		}
	}

	public object Tag { get; set; }

	public ILayout LayoutImp { get; protected set; }

	[Editor(false)]
	public bool DropEventHandledManually { get; set; }

	internal WidgetInfo WidgetInfo { get; private set; }

	public List<Widget> Children => _children;

	public bool ConnectedToRoot
	{
		get
		{
			if (Id == "Root")
			{
				return true;
			}
			if (ParentWidget == null)
			{
				return false;
			}
			return ParentWidget.ConnectedToRoot;
		}
	}

	[Editor(false)]
	public float MaxWidth
	{
		get
		{
			return _maxWidth;
		}
		set
		{
			if (_maxWidth != value)
			{
				_maxWidth = value;
				_gotMaxWidth = true;
				OnPropertyChanged(value, "MaxWidth");
			}
		}
	}

	[Editor(false)]
	public float MaxHeight
	{
		get
		{
			return _maxHeight;
		}
		set
		{
			if (_maxHeight != value)
			{
				_maxHeight = value;
				_gotMaxHeight = true;
				OnPropertyChanged(value, "MaxHeight");
			}
		}
	}

	[Editor(false)]
	public float MinWidth
	{
		get
		{
			return _minWidth;
		}
		set
		{
			if (_minWidth != value)
			{
				_minWidth = value;
				_gotMinWidth = true;
				OnPropertyChanged(value, "MinWidth");
			}
		}
	}

	[Editor(false)]
	public float MinHeight
	{
		get
		{
			return _minHeight;
		}
		set
		{
			if (_minHeight != value)
			{
				_minHeight = value;
				_gotMinHeight = true;
				OnPropertyChanged(value, "MinHeight");
			}
		}
	}

	public float ScaledMaxWidth => _scaleToUse * _maxWidth;

	public float ScaledMaxHeight => _scaleToUse * _maxHeight;

	public float ScaledMinWidth => _scaleToUse * _minWidth;

	public float ScaledMinHeight => _scaleToUse * _minHeight;

	public bool DisableRender { get; set; }

	public float ExtendCursorAreaTop { get; set; }

	public float ExtendCursorAreaRight { get; set; }

	public float ExtendCursorAreaBottom { get; set; }

	public float ExtendCursorAreaLeft { get; set; }

	public float CursorAreaXOffset { get; set; }

	public float CursorAreaYOffset { get; set; }

	public bool AcceptNavigation
	{
		get
		{
			return !DoNotAcceptNavigation;
		}
		set
		{
			if (value == DoNotAcceptNavigation)
			{
				DoNotAcceptNavigation = !value;
			}
		}
	}

	public bool DoNotAcceptNavigation
	{
		get
		{
			return _doNotAcceptNavigation;
		}
		set
		{
			if (value != _doNotAcceptNavigation)
			{
				_doNotAcceptNavigation = value;
				GamepadNavigationContext.OnWidgetNavigationStatusChanged(this);
			}
		}
	}

	public bool IsUsingNavigation
	{
		get
		{
			return _isUsingNavigation;
		}
		set
		{
			if (value != _isUsingNavigation)
			{
				_isUsingNavigation = value;
				OnPropertyChanged(value, "IsUsingNavigation");
			}
		}
	}

	public bool UseSiblingIndexForNavigation
	{
		get
		{
			return _useSiblingIndexForNavigation;
		}
		set
		{
			if (value != _useSiblingIndexForNavigation)
			{
				_useSiblingIndexForNavigation = value;
				if (value)
				{
					GamepadNavigationIndex = GetSiblingIndex();
				}
			}
		}
	}

	public int GamepadNavigationIndex
	{
		get
		{
			return _gamepadNavigationIndex;
		}
		set
		{
			if (value != _gamepadNavigationIndex)
			{
				_gamepadNavigationIndex = value;
				GamepadNavigationContext.OnWidgetNavigationIndexUpdated(this);
				OnGamepadNavigationIndexUpdated(value);
				OnPropertyChanged(value, "GamepadNavigationIndex");
			}
		}
	}

	public GamepadNavigationTypes UsedNavigationMovements
	{
		get
		{
			return _usedNavigationMovements;
		}
		set
		{
			if (value != _usedNavigationMovements)
			{
				_usedNavigationMovements = value;
				Context.GamepadNavigation.OnWidgetUsedNavigationMovementsUpdated(this);
			}
		}
	}

	public event Action<Widget, string, object[]> EventFire
	{
		add
		{
			if (_eventTargets == null)
			{
				_eventTargets = new List<Action<Widget, string, object[]>>();
			}
			_eventTargets.Add(value);
		}
		remove
		{
			if (_eventTargets != null)
			{
				_eventTargets.Remove(value);
			}
		}
	}

	public event Action<Widget> OnVisibilityChanged;

	public List<Widget> GetAllChildrenAndThisRecursive()
	{
		List<Widget> list = new List<Widget>();
		list.Add(this);
		GetAllChildrenAux(list);
		return list;
	}

	public void ApplyActionToAllChildren(Action<Widget> action)
	{
		for (int i = 0; i < _children.Count; i++)
		{
			action(_children[i]);
		}
	}

	public void ApplyActionToAllChildrenRecursive(Action<Widget> action)
	{
		for (int i = 0; i < _children.Count; i++)
		{
			action(_children[i]);
			_children[i].ApplyActionToAllChildrenRecursive(action);
		}
	}

	public List<TWidget> GetAllChildrenOfTypeRecursive<TWidget>(Func<TWidget, bool> predicate = null) where TWidget : Widget
	{
		List<TWidget> list = new List<TWidget>();
		GetChildrenOfTypeAux(list, predicate);
		return list;
	}

	private void GetChildrenOfTypeAux<TWidget>(List<TWidget> list, Func<TWidget, bool> predicate = null)
	{
		for (int i = 0; i < _children.Count; i++)
		{
			if (_children[i] is TWidget val && (predicate == null || predicate(val)))
			{
				list.Add(val);
			}
			_children[i].GetChildrenOfTypeAux(list, predicate);
		}
	}

	public List<Widget> GetAllChildrenRecursive(Func<Widget, bool> predicate = null)
	{
		List<Widget> list = new List<Widget>();
		GetAllChildrenAux(list, predicate);
		return list;
	}

	private void GetAllChildrenAux(List<Widget> list, Func<Widget, bool> predicate = null)
	{
		for (int i = 0; i < _children.Count; i++)
		{
			if (predicate == null || predicate(_children[i]))
			{
				list.Add(_children[i]);
			}
			_children[i].GetAllChildrenAux(list);
		}
	}

	public List<Widget> GetAllParents()
	{
		List<Widget> list = new List<Widget>();
		for (Widget parentWidget = ParentWidget; parentWidget != null; parentWidget = parentWidget.ParentWidget)
		{
			list.Add(parentWidget);
		}
		return list;
	}

	public Widget(UIContext context)
	{
		DropEventHandledManually = true;
		LayoutImp = new DefaultLayout();
		_children = new List<Widget>();
		_childRenderBuffer = new List<Widget>();
		Context = context;
		_states = new List<string>();
		WidgetInfo = WidgetInfo.GetWidgetInfo(GetType());
		Sprite = null;
		ImageFit = new ImageFit();
		AreaRect = Rectangle2D.Create();
		_isGamepadCursorAreaDirty = true;
		_stateTimer = 0f;
		_currentVisualStateAnimationState = VisualStateAnimationState.None;
		_isFocusable = false;
		_seed = 0;
		_components = new List<WidgetComponent>();
		AddState("Default");
		SetState("Default");
	}

	public T GetComponent<T>() where T : WidgetComponent
	{
		for (int i = 0; i < _components.Count; i++)
		{
			WidgetComponent widgetComponent = _components[i];
			if (widgetComponent is T)
			{
				return (T)widgetComponent;
			}
		}
		return null;
	}

	public void AddComponent(WidgetComponent component)
	{
		_components.Add(component);
	}

	protected void SetMeasureAndLayoutDirty()
	{
		SetMeasureDirty();
		SetLayoutDirty();
	}

	protected void SetMeasureDirty()
	{
		EventManager.SetMeasureDirty();
	}

	protected void SetLayoutDirty()
	{
		EventManager.SetLayoutDirty();
	}

	internal void LayoutUpdated()
	{
		OnLayoutUpdated();
		for (int i = 0; i < ChildCount; i++)
		{
			GetChild(i)?.LayoutUpdated();
		}
	}

	protected virtual void OnLayoutUpdated()
	{
	}

	public void AddState(string stateName)
	{
		if (!_states.Contains(stateName))
		{
			_states.Add(stateName);
		}
	}

	public bool ContainsState(string stateName)
	{
		return _states.Contains(stateName);
	}

	public virtual void SetState(string stateName)
	{
		if (CurrentState != stateName)
		{
			CurrentState = stateName;
			_stateTimer = 0f;
			if (_currentVisualStateAnimationState != VisualStateAnimationState.None)
			{
				_startVisualState = new VisualState("@StartState");
				_startVisualState.FillFromWidget(this);
			}
			_currentVisualStateAnimationState = VisualStateAnimationState.PlayingBasicTranisition;
		}
		if (!UpdateChildrenStates)
		{
			return;
		}
		for (int i = 0; i < ChildCount; i++)
		{
			Widget child = GetChild(i);
			if (!(child is ImageWidget) || !((ImageWidget)child).OverrideDefaultStateSwitchingEnabled)
			{
				child.SetState(CurrentState);
			}
		}
	}

	public Widget FindChild(BindingPath path)
	{
		string firstNode = path.FirstNode;
		BindingPath subPath = path.SubPath;
		if (firstNode == "..")
		{
			return ParentWidget.FindChild(subPath);
		}
		if (firstNode == ".")
		{
			return this;
		}
		for (int i = 0; i < _children.Count; i++)
		{
			Widget widget = _children[i];
			if (!string.IsNullOrEmpty(widget.Id) && widget.Id == firstNode)
			{
				if (subPath == null)
				{
					return widget;
				}
				return widget.FindChild(subPath);
			}
		}
		return null;
	}

	public Widget FindChild(string singlePathNode)
	{
		if (singlePathNode == "..")
		{
			return ParentWidget;
		}
		if (singlePathNode == ".")
		{
			return this;
		}
		for (int i = 0; i < _children.Count; i++)
		{
			Widget widget = _children[i];
			if (!string.IsNullOrEmpty(widget.Id) && widget.Id == singlePathNode)
			{
				return widget;
			}
		}
		return null;
	}

	public Widget FindChild(WidgetSearchDelegate widgetSearchDelegate)
	{
		for (int i = 0; i < _children.Count; i++)
		{
			Widget widget = _children[i];
			if (widgetSearchDelegate(widget))
			{
				return widget;
			}
		}
		return null;
	}

	public Widget FindChild(string id, bool includeAllChildren = false)
	{
		List<Widget> list = (includeAllChildren ? GetAllChildrenRecursive() : _children);
		for (int i = 0; i < list.Count; i++)
		{
			Widget widget = list[i];
			if (!string.IsNullOrEmpty(widget.Id) && widget.Id == id)
			{
				return widget;
			}
		}
		return null;
	}

	public Widget GetFirstInChildrenAndThisRecursive(Func<Widget, bool> predicate)
	{
		if (predicate(this))
		{
			return this;
		}
		for (int i = 0; i < _children.Count; i++)
		{
			Widget firstInChildrenAndThisRecursive = _children[i].GetFirstInChildrenAndThisRecursive(predicate);
			if (firstInChildrenAndThisRecursive != null)
			{
				return firstInChildrenAndThisRecursive;
			}
		}
		return null;
	}

	public Widget GetFirstInChildrenRecursive(Func<Widget, bool> predicate)
	{
		for (int i = 0; i < _children.Count; i++)
		{
			if (predicate(_children[i]))
			{
				return _children[i];
			}
			Widget firstInChildrenRecursive = _children[i].GetFirstInChildrenRecursive(predicate);
			if (firstInChildrenRecursive != null)
			{
				return firstInChildrenRecursive;
			}
		}
		return null;
	}

	public List<T> FindChildrenWithId<T>(string id, bool includeAllChildren = false) where T : Widget
	{
		List<Widget> list = (includeAllChildren ? GetAllChildrenRecursive() : _children);
		List<T> list2 = new List<T>();
		for (int i = 0; i < list.Count; i++)
		{
			if (!string.IsNullOrEmpty(list[i].Id) && list[i] is T item && list[i].Id == id)
			{
				list2.Add(item);
			}
		}
		return list2;
	}

	public List<T> FindChildrenWithType<T>(bool includeAllChildren = false) where T : Widget
	{
		List<Widget> list = (includeAllChildren ? GetAllChildrenRecursive() : _children);
		List<T> list2 = new List<T>();
		for (int i = 0; i < list.Count; i++)
		{
			if (!string.IsNullOrEmpty(list[i].Id) && list[i] is T item)
			{
				list2.Add(item);
			}
		}
		return list2;
	}

	public void RemoveAllChildren()
	{
		while (_children.Count > 0)
		{
			_children[0].ParentWidget = null;
		}
	}

	private static float GetEaseOutBack(float t)
	{
		float num = 0.5f;
		float num2 = num + 1f;
		return 1f + num2 * TaleWorlds.Library.MathF.Pow(t - 1f, 3f) + num * TaleWorlds.Library.MathF.Pow(t - 1f, 2f);
	}

	internal void UpdateVisualDefinitions(float dt)
	{
		if (VisualDefinition != null && _currentVisualStateAnimationState == VisualStateAnimationState.PlayingBasicTranisition)
		{
			if (_startVisualState == null)
			{
				_startVisualState = new VisualState("@StartState");
				_startVisualState.FillFromWidget(this);
			}
			VisualState visualState = VisualDefinition.GetVisualState(CurrentState);
			if (visualState != null)
			{
				float num = (visualState.GotTransitionDuration ? visualState.TransitionDuration : VisualDefinition.TransitionDuration);
				float delayOnBegin = VisualDefinition.DelayOnBegin;
				if (_stateTimer < num)
				{
					if (_stateTimer >= delayOnBegin)
					{
						float ratio = (_stateTimer - delayOnBegin) / (num - delayOnBegin);
						ratio = AnimationInterpolation.Ease(VisualDefinition.EaseType, VisualDefinition.EaseFunction, ratio);
						PositionXOffset = (visualState.GotPositionXOffset ? Mathf.Lerp(_startVisualState.PositionXOffset, visualState.PositionXOffset, ratio) : PositionXOffset);
						PositionYOffset = (visualState.GotPositionYOffset ? Mathf.Lerp(_startVisualState.PositionYOffset, visualState.PositionYOffset, ratio) : PositionYOffset);
						SuggestedWidth = (visualState.GotSuggestedWidth ? Mathf.Lerp(_startVisualState.SuggestedWidth, visualState.SuggestedWidth, ratio) : SuggestedWidth);
						SuggestedHeight = (visualState.GotSuggestedHeight ? Mathf.Lerp(_startVisualState.SuggestedHeight, visualState.SuggestedHeight, ratio) : SuggestedHeight);
						MarginTop = (visualState.GotMarginTop ? Mathf.Lerp(_startVisualState.MarginTop, visualState.MarginTop, ratio) : MarginTop);
						MarginBottom = (visualState.GotMarginBottom ? Mathf.Lerp(_startVisualState.MarginBottom, visualState.MarginBottom, ratio) : MarginBottom);
						MarginLeft = (visualState.GotMarginLeft ? Mathf.Lerp(_startVisualState.MarginLeft, visualState.MarginLeft, ratio) : MarginLeft);
						MarginRight = (visualState.GotMarginRight ? Mathf.Lerp(_startVisualState.MarginRight, visualState.MarginRight, ratio) : MarginRight);
					}
				}
				else
				{
					PositionXOffset = (visualState.GotPositionXOffset ? visualState.PositionXOffset : PositionXOffset);
					PositionYOffset = (visualState.GotPositionYOffset ? visualState.PositionYOffset : PositionYOffset);
					SuggestedWidth = (visualState.GotSuggestedWidth ? visualState.SuggestedWidth : SuggestedWidth);
					SuggestedHeight = (visualState.GotSuggestedHeight ? visualState.SuggestedHeight : SuggestedHeight);
					MarginTop = (visualState.GotMarginTop ? visualState.MarginTop : MarginTop);
					MarginBottom = (visualState.GotMarginBottom ? visualState.MarginBottom : MarginBottom);
					MarginLeft = (visualState.GotMarginLeft ? visualState.MarginLeft : MarginLeft);
					MarginRight = (visualState.GotMarginRight ? visualState.MarginRight : MarginRight);
					_startVisualState = visualState;
					_currentVisualStateAnimationState = VisualStateAnimationState.None;
				}
			}
			else
			{
				_currentVisualStateAnimationState = VisualStateAnimationState.None;
			}
		}
		_stateTimer += dt;
	}

	internal void Update(float dt)
	{
		OnUpdate(dt);
	}

	internal void LateUpdate(float dt)
	{
		OnLateUpdate(dt);
	}

	internal void ParallelUpdate(float dt)
	{
		if (!_isInParallelOperation)
		{
			_isInParallelOperation = true;
			OnParallelUpdate(dt);
			_isInParallelOperation = false;
		}
	}

	protected virtual void OnUpdate(float dt)
	{
	}

	protected virtual void OnParallelUpdate(float dt)
	{
	}

	protected virtual void OnLateUpdate(float dt)
	{
	}

	protected virtual void RefreshState()
	{
	}

	public virtual void UpdateAnimationPropertiesSubTask(float alphaFactor)
	{
		AlphaFactor = alphaFactor;
		for (int i = 0; i < _children.Count; i++)
		{
			_children[i].UpdateAnimationPropertiesSubTask(alphaFactor);
		}
	}

	public void Measure(Vector2 measureSpec)
	{
		if (IsHidden)
		{
			MeasuredSize = Vector2.Zero;
		}
		else
		{
			OnMeasure(measureSpec);
		}
	}

	private Vector2 ProcessSizeWithBoundaries(Vector2 input)
	{
		Vector2 result = input;
		if (_gotMinWidth && input.X < ScaledMinWidth)
		{
			result.X = ScaledMinWidth;
		}
		if (_gotMinHeight && input.Y < ScaledMinHeight)
		{
			result.Y = ScaledMinHeight;
		}
		if (_gotMaxWidth && input.X > ScaledMaxWidth)
		{
			result.X = ScaledMaxWidth;
		}
		if (_gotMaxHeight && input.Y > ScaledMaxHeight)
		{
			result.Y = ScaledMaxHeight;
		}
		return result;
	}

	private void OnMeasure(Vector2 measureSpec)
	{
		if (UseSpriteDimensions)
		{
			WidthSizePolicy = SizePolicy.Fixed;
			HeightSizePolicy = SizePolicy.Fixed;
			SuggestedWidth = ((float?)Sprite?.Width) ?? 0f;
			SuggestedHeight = ((float?)Sprite?.Height) ?? 0f;
		}
		if (WidthSizePolicy == SizePolicy.Fixed)
		{
			measureSpec.X = ScaledSuggestedWidth;
		}
		else if (WidthSizePolicy == SizePolicy.StretchToParent)
		{
			measureSpec.X -= ScaledMarginLeft + ScaledMarginRight;
		}
		else
		{
			_ = WidthSizePolicy;
			_ = 2;
		}
		if (HeightSizePolicy == SizePolicy.Fixed)
		{
			measureSpec.Y = ScaledSuggestedHeight;
		}
		else if (HeightSizePolicy == SizePolicy.StretchToParent)
		{
			measureSpec.Y -= ScaledMarginTop + ScaledMarginBottom;
		}
		else
		{
			_ = HeightSizePolicy;
			_ = 2;
		}
		measureSpec = ProcessSizeWithBoundaries(measureSpec);
		Vector2 vector = MeasureChildren(measureSpec);
		Vector2 input = new Vector2(0f, 0f);
		if (WidthSizePolicy == SizePolicy.Fixed)
		{
			input.X = ScaledSuggestedWidth;
		}
		else if (WidthSizePolicy == SizePolicy.CoverChildren)
		{
			input.X = vector.X;
		}
		else if (WidthSizePolicy == SizePolicy.StretchToParent)
		{
			input.X = measureSpec.X;
		}
		if (HeightSizePolicy == SizePolicy.Fixed)
		{
			input.Y = ScaledSuggestedHeight;
		}
		else if (HeightSizePolicy == SizePolicy.CoverChildren)
		{
			input.Y = vector.Y;
		}
		else if (HeightSizePolicy == SizePolicy.StretchToParent)
		{
			input.Y = measureSpec.Y;
		}
		input = ProcessSizeWithBoundaries(input);
		MeasuredSize = input;
	}

	public bool CheckIsMyChildRecursive(Widget child)
	{
		for (Widget widget = child?.ParentWidget; widget != null; widget = widget.ParentWidget)
		{
			if (widget == this)
			{
				return true;
			}
		}
		return false;
	}

	private Vector2 MeasureChildren(Vector2 measureSpec)
	{
		return LayoutImp.MeasureChildren(this, measureSpec, Context.SpriteData, _scaleToUse);
	}

	public void AddChild(Widget widget)
	{
		widget.ParentWidget = this;
	}

	public void AddChildAtIndex(Widget widget, int index)
	{
		widget.ParentWidget = this;
		widget.SetSiblingIndex(index);
	}

	public void SwapChildren(Widget widget1, Widget widget2)
	{
		int index = _children.IndexOf(widget1);
		int index2 = _children.IndexOf(widget2);
		Widget value = _children[index];
		_children[index] = _children[index2];
		_children[index2] = value;
	}

	protected virtual void OnChildAdded(Widget child)
	{
		EventFired("ItemAdd", child);
		if (DoNotUseCustomScaleAndChildren)
		{
			child.DoNotUseCustomScaleAndChildren = true;
		}
		if (UpdateChildrenStates && (!(child is ImageWidget) || !((ImageWidget)child).OverrideDefaultStateSwitchingEnabled))
		{
			child.SetState(CurrentState);
		}
	}

	public void RemoveChild(Widget widget)
	{
		widget.ParentWidget = null;
	}

	public virtual void OnBeforeRemovedChild(Widget widget)
	{
		if (IsHovered)
		{
			EventFired("HoverEnd");
		}
		for (int i = 0; i < _children.Count; i++)
		{
			_children[i].OnBeforeRemovedChild(widget);
		}
	}

	public bool HasChild(Widget widget)
	{
		return _children.Contains(widget);
	}

	protected virtual void OnBeforeChildRemoved(Widget child)
	{
		EventFired("ItemRemove", child);
	}

	protected virtual void OnAfterChildRemoved(Widget child, int previousIndexOfChild)
	{
		EventFired("AfterItemRemove", child);
	}

	public virtual void UpdateBrushes(float dt)
	{
	}

	public int GetChildIndex(Widget child)
	{
		return _children.IndexOf(child);
	}

	public int GetVisibleChildIndex(Widget child)
	{
		int result = -1;
		List<Widget> list = _children.Where((Widget c) => c.IsVisible).ToList();
		if (list.Count > 0)
		{
			result = list.IndexOf(child);
		}
		return result;
	}

	public int GetFilterChildIndex(Widget child, Func<Widget, bool> childrenFilter)
	{
		int result = -1;
		List<Widget> list = _children.Where((Widget c) => childrenFilter(c)).ToList();
		if (list.Count > 0)
		{
			result = list.IndexOf(child);
		}
		return result;
	}

	public Widget GetChild(int i)
	{
		if (i < _children.Count)
		{
			return _children[i];
		}
		return null;
	}

	public void Layout(float left, float bottom, float right, float top)
	{
		if (IsVisible)
		{
			SetLayout(left, bottom, right, top);
			Vector2 scaledPositionOffset = ScaledPositionOffset;
			Left += scaledPositionOffset.X;
			Top += scaledPositionOffset.Y;
			OnLayout(Left, Bottom, Right, Top);
		}
	}

	private void SetLayout(float left, float bottom, float right, float top)
	{
		left += ScaledMarginLeft;
		right -= ScaledMarginRight;
		top += ScaledMarginTop;
		bottom -= ScaledMarginBottom;
		float num = right - left;
		float num2 = bottom - top;
		float left2 = ((HorizontalAlignment == HorizontalAlignment.Left) ? left : ((HorizontalAlignment != HorizontalAlignment.Center) ? (right - MeasuredSize.X) : (left + num / 2f - MeasuredSize.X / 2f)));
		float top2 = ((VerticalAlignment == VerticalAlignment.Top) ? top : ((VerticalAlignment != VerticalAlignment.Center) ? (bottom - MeasuredSize.Y) : (top + num2 / 2f - MeasuredSize.Y / 2f)));
		Left = left2;
		Top = top2;
		Size = MeasuredSize;
	}

	private void OnLayout(float left, float bottom, float right, float top)
	{
		LayoutImp.OnLayout(this, left, bottom, right, top);
	}

	private void ParallelUpdateChildPositions()
	{
		TWParallel.ForWithoutRenderThread(0, _children.Count, UpdateChildPositionMT);
		void UpdateChildPositionMT(int startInclusive, int endExclusive)
		{
			for (int i = startInclusive; i < endExclusive; i++)
			{
				_children[i].UpdatePosition();
			}
		}
	}

	internal void UpdatePosition()
	{
		if (IsVisible)
		{
			LocalPosition = new Vector2(Left, Top);
			OnUpdatePosition();
			_isGamepadCursorAreaDirty = true;
			OnUpdateChildPositions();
		}
	}

	protected virtual void OnUpdatePosition()
	{
		Rectangle2D parentRectangle = ParentWidget?.AreaRect ?? EventManager.AreaRectangle;
		AreaRect.LocalPosition = new Vector2(LocalPosition.X, LocalPosition.Y);
		AreaRect.LocalPivot = new Vector2(PivotX, PivotY);
		AreaRect.LocalScale = new Vector2(Size.X, Size.Y);
		AreaRect.LocalRotation = Rotation;
		AreaRect.CalculateMatrixFrame(in parentRectangle);
	}

	protected virtual void OnUpdateChildPositions()
	{
		if (_children.Count >= 64)
		{
			ParallelUpdateChildPositions();
			return;
		}
		for (int i = 0; i < _children.Count; i++)
		{
			_children[i].UpdatePosition();
		}
	}

	public virtual void HandleInput(IReadOnlyList<int> lastKeysPressed)
	{
	}

	public bool IsPointInsideMeasuredArea(Vector2 p)
	{
		return AreaRect.IsPointInside(in p);
	}

	public bool IsPointInsideGamepadCursorArea(Vector2 p)
	{
		return GamepadCursorAreaRect.IsPointInside(in p);
	}

	public void Hide()
	{
		IsHidden = true;
	}

	public void Show()
	{
		IsHidden = false;
	}

	public Vector2 GetLocalPoint(Vector2 globalPoint)
	{
		return globalPoint - GlobalPosition;
	}

	public void SetSiblingIndex(int index, bool force = false)
	{
		int siblingIndex = GetSiblingIndex();
		if (siblingIndex != index || force)
		{
			ParentWidget._children.RemoveAt(siblingIndex);
			ParentWidget._children.Insert(index, this);
			SetMeasureAndLayoutDirty();
			EventFired("SiblingIndexChanged");
		}
	}

	public int GetSiblingIndex()
	{
		return ParentWidget?.GetChildIndex(this) ?? (-1);
	}

	public int GetVisibleSiblingIndex()
	{
		return ParentWidget.GetVisibleChildIndex(this);
	}

	public void Render(TwoDimensionContext twoDimensionContext, TwoDimensionDrawContext drawContext)
	{
		if (!IsHidden && !DisableRender)
		{
			bool flag = ClipHorizontalContent || ClipVerticalContent;
			if (flag)
			{
				drawContext.PushScissor(in AreaRect);
			}
			if (CircularClipEnabled)
			{
				if (IsCircularClipRadiusHalfOfHeight)
				{
					CircularClipRadius = Size.Y * 0.5f * _inverseScaleToUse;
				}
				else if (IsCircularClipRadiusHalfOfWidth)
				{
					CircularClipRadius = Size.X * 0.5f * _inverseScaleToUse;
				}
				Vector2 center = AreaRect.GetCenter();
				Vector2 position = new Vector2(center.X + CircularClipXOffset * _scaleToUse, center.Y + CircularClipYOffset * _scaleToUse);
				drawContext.SetCircualMask(position, CircularClipRadius * _scaleToUse, CircularClipSmoothingRadius * _scaleToUse);
			}
			bool flag2 = false;
			if (drawContext.ScissorTestEnabled)
			{
				if (_calculateSizeFirstFrame || !drawContext.IsDiscardedByAnyScissor(in AreaRect))
				{
					flag2 = !DoNotRenderIfNotFullyInsideScissor || AreaRect.IsSubRectOf(in EventManager.AreaRectangle);
				}
			}
			else if (_calculateSizeFirstFrame || AreaRect.IsCollide(in EventManager.AreaRectangle))
			{
				flag2 = true;
			}
			if (flag2)
			{
				_isRendering = true;
				OnRender(twoDimensionContext, drawContext);
				for (int i = 0; i < _children.Count; i++)
				{
					Widget widget = _children[i];
					if (widget != null)
					{
						_childRenderBuffer.Add(widget);
					}
				}
				for (int j = 0; j < _childRenderBuffer.Count; j++)
				{
					Widget widget2 = _childRenderBuffer[j];
					if (!widget2.RenderLate)
					{
						widget2.Render(twoDimensionContext, drawContext);
					}
				}
				for (int k = 0; k < _childRenderBuffer.Count; k++)
				{
					Widget widget3 = _childRenderBuffer[k];
					if (widget3.RenderLate)
					{
						widget3.Render(twoDimensionContext, drawContext);
					}
				}
				_childRenderBuffer.Clear();
				_isRendering = false;
			}
			if (CircularClipEnabled)
			{
				drawContext.ClearCircualMask();
			}
			if (flag)
			{
				drawContext.PopScissor();
			}
		}
		_calculateSizeFirstFrame = false;
	}

	protected virtual void OnRender(TwoDimensionContext twoDimensionContext, TwoDimensionDrawContext drawContext)
	{
		if (_sprite?.Texture != null)
		{
			ImageFitResult fittedRectangle = ImageFit.GetFittedRectangle(Size, new Vector2(Sprite.Width, Sprite.Height));
			float num = LocalPosition.X;
			float num2 = LocalPosition.Y;
			if (ForcePixelPerfectRenderPlacement)
			{
				num = TaleWorlds.Library.MathF.Round(num);
				num2 = TaleWorlds.Library.MathF.Round(num2);
			}
			SimpleMaterial simpleMaterial = drawContext.CreateSimpleMaterial();
			simpleMaterial.OverlayEnabled = false;
			simpleMaterial.CircularMaskingEnabled = false;
			simpleMaterial.Texture = _sprite.Texture;
			simpleMaterial.NinePatchParameters = _sprite.NinePatchParameters;
			if (NinePatchLeft != 0 || NinePatchRight != 0 || NinePatchTop != 0 || NinePatchBottom != 0)
			{
				simpleMaterial.NinePatchParameters = new SpriteNinePatchParameters(NinePatchLeft, NinePatchRight, NinePatchTop, NinePatchBottom);
			}
			simpleMaterial.Color = Color;
			simpleMaterial.ColorFactor = ColorFactor;
			simpleMaterial.AlphaFactor = AlphaFactor * Context.ContextAlpha;
			simpleMaterial.HueFactor = 0f;
			simpleMaterial.SaturationFactor = SaturationFactor;
			simpleMaterial.ValueFactor = ValueFactor;
			float num3 = ExtendLeft;
			if (HorizontalFlip)
			{
				num3 = ExtendRight;
			}
			float width = fittedRectangle.Width;
			width += (ExtendRight + ExtendLeft) * _scaleToUse;
			num -= num3 * _scaleToUse;
			float height = fittedRectangle.Height;
			float num4 = ExtendTop;
			if (VerticalFlip)
			{
				num4 = ExtendBottom;
			}
			height += (ExtendTop + ExtendBottom) * _scaleToUse;
			num2 -= num4 * _scaleToUse;
			width = (HorizontalFlip ? (0f - width) : width);
			height = (VerticalFlip ? (0f - height) : height);
			float scaleX = ((width == 0f) ? 1f : (width / Size.X));
			float scaleY = ((height == 0f) ? 1f : (height / Size.Y));
			AreaRect.SetVisualOffset(num - LocalPosition.X + fittedRectangle.OffsetX, num2 - LocalPosition.Y + fittedRectangle.OffsetY);
			AreaRect.SetVisualScale(scaleX, scaleY);
			AreaRect.ValidateVisuals();
			drawContext.DrawSprite(_sprite, simpleMaterial, in AreaRect, _scaleToUse);
		}
	}

	protected void EventFired(string eventName, params object[] args)
	{
		if (_eventTargets != null)
		{
			for (int i = 0; i < _eventTargets.Count; i++)
			{
				_eventTargets[i](this, eventName, args);
			}
		}
	}

	public bool IsRecursivelyVisible()
	{
		for (Widget widget = this; widget != null; widget = widget.ParentWidget)
		{
			if (!widget.IsVisible)
			{
				return false;
			}
		}
		return true;
	}

	internal void HandleOnDisconnectedFromRoot()
	{
		OnDisconnectedFromRoot();
		if (IsHovered)
		{
			EventFired("HoverEnd");
		}
	}

	internal void HandleOnConnectedToRoot()
	{
		if (!_seedSet)
		{
			_seed = GetSiblingIndex();
			_seedSet = true;
		}
		OnConnectedToRoot();
	}

	protected virtual void OnDisconnectedFromRoot()
	{
	}

	protected virtual void OnConnectedToRoot()
	{
		EventFired("ConnectedToRoot");
	}

	public override string ToString()
	{
		return this.GetFullIDPath();
	}

	protected virtual void OnGamepadNavigationIndexUpdated(int newIndex)
	{
	}

	public void OnGamepadNavigationFocusGain()
	{
		OnGamepadNavigationFocusGained?.Invoke(this);
	}

	internal bool PreviewEvent(GauntletEvent gauntletEvent)
	{
		bool result = false;
		switch (gauntletEvent)
		{
		case GauntletEvent.MousePressed:
			result = OnPreviewMousePressed();
			break;
		case GauntletEvent.MouseReleased:
			result = OnPreviewMouseReleased();
			break;
		case GauntletEvent.MouseAlternatePressed:
			result = OnPreviewMouseAlternatePressed();
			break;
		case GauntletEvent.MouseAlternateReleased:
			result = OnPreviewMouseAlternateReleased();
			break;
		case GauntletEvent.MouseMove:
			result = OnPreviewMouseMove();
			break;
		case GauntletEvent.DragHover:
			result = OnPreviewDragHover();
			break;
		case GauntletEvent.DragBegin:
			result = OnPreviewDragBegin();
			break;
		case GauntletEvent.DragEnd:
			result = OnPreviewDragEnd();
			break;
		case GauntletEvent.Drop:
			result = OnPreviewDrop();
			break;
		case GauntletEvent.MouseScroll:
			result = OnPreviewMouseScroll();
			break;
		case GauntletEvent.RightStickMovement:
			result = OnPreviewRightStickMovement();
			break;
		}
		return result;
	}

	protected virtual bool OnPreviewMousePressed()
	{
		return true;
	}

	protected virtual bool OnPreviewMouseReleased()
	{
		return true;
	}

	protected virtual bool OnPreviewMouseAlternatePressed()
	{
		return true;
	}

	protected virtual bool OnPreviewMouseAlternateReleased()
	{
		return true;
	}

	protected virtual bool OnPreviewDragBegin()
	{
		return AcceptDrag;
	}

	protected virtual bool OnPreviewDragEnd()
	{
		return AcceptDrag;
	}

	protected virtual bool OnPreviewDrop()
	{
		return AcceptDrop;
	}

	protected virtual bool OnPreviewMouseScroll()
	{
		return false;
	}

	protected virtual bool OnPreviewRightStickMovement()
	{
		return false;
	}

	protected virtual bool OnPreviewMouseMove()
	{
		return true;
	}

	protected virtual bool OnPreviewDragHover()
	{
		return false;
	}

	protected internal virtual void OnMousePressed()
	{
		IsPressed = true;
		EventFired("MouseDown");
	}

	protected internal virtual void OnMouseReleased(bool isFromInput)
	{
		IsPressed = false;
		EventFired("MouseUp");
	}

	protected internal virtual void OnMouseAlternatePressed()
	{
		EventFired("MouseAlternateDown");
	}

	protected internal virtual void OnMouseAlternateReleased(bool isFromInput)
	{
		EventFired("MouseAlternateUp");
	}

	protected internal virtual void OnMouseMove()
	{
		EventFired("MouseMove");
	}

	protected internal virtual void OnHoverBegin()
	{
		IsHovered = true;
		EventFired("HoverBegin");
	}

	protected internal virtual void OnHoverEnd()
	{
		EventFired("HoverEnd");
		IsHovered = false;
	}

	protected internal virtual void OnDragBegin()
	{
		EventManager.BeginDragging(this);
		EventFired("DragBegin");
	}

	protected internal virtual void OnDragEnd()
	{
		EventFired("DragEnd");
	}

	protected internal virtual bool OnDrop()
	{
		if (AcceptDrop)
		{
			bool flag = true;
			if (AcceptDropHandler != null)
			{
				flag = AcceptDropHandler(this, EventManager.DraggedWidget);
			}
			if (flag)
			{
				Widget widget = EventManager.ReleaseDraggedWidget();
				int num = -1;
				if (!DropEventHandledManually)
				{
					widget.ParentWidget = this;
				}
				EventFired("Drop", widget, num);
				return true;
			}
		}
		return false;
	}

	protected internal virtual void OnMouseScroll()
	{
		EventFired("MouseScroll");
	}

	protected internal virtual void OnRightStickMovement()
	{
	}

	protected internal virtual void OnDragHoverBegin()
	{
		EventFired("DragHoverBegin");
	}

	protected internal virtual void OnDragHoverEnd()
	{
		EventFired("DragHoverEnd");
	}

	protected internal virtual void OnGainFocus()
	{
		IsFocused = true;
		EventFired("FocusGained");
	}

	protected internal virtual void OnLoseFocus()
	{
		IsFocused = false;
		EventFired("FocusLost");
	}

	protected internal virtual void OnMouseOverBegin()
	{
		EventFired("MouseOverBegin");
	}

	protected internal virtual void OnMouseOverEnd()
	{
		EventFired("MouseOverEnd");
	}

	protected internal virtual void OnContextActivated()
	{
	}

	protected internal virtual void OnContextDeactivated()
	{
	}
}
