using System;
using System.Numerics;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.BannerBuilder;

public class BannerBuilderEditableAreaWidget : Widget
{
	private enum BuilderMode
	{
		None,
		Rotating,
		Positioning,
		HorizontalResizing,
		VerticalResizing,
		RightCornerResizing
	}

	private enum WidgetPlacementType
	{
		Horizontal,
		Vertical,
		Max
	}

	private enum EdgeResizeType
	{
		Top,
		Right
	}

	private bool _initialized;

	private Vec2 _centerOfSigil;

	private Vec2 _sizeOfSigil;

	private float _positionLimitMin;

	private float _positionLimitMax;

	private float _areaScale;

	private const int _sizeLimitMin = 2;

	private int _sizeLimitMax;

	private BuilderMode _currentMode;

	private Vector2 _latestMousePosition;

	private Vector2 _resizeStartMousePosition;

	private Widget _resizeStartWidget;

	private Vec2 _resizeStartSize;

	private bool _isLayerPattern;

	private Vec2 _positionValue;

	private Vec2 _sizeValue;

	private float _rotationValue;

	private int _editableAreaSize;

	private int _totalAreaSize;

	public ButtonWidget DragWidgetTopRight { get; set; }

	public ButtonWidget DragWidgetRight { get; set; }

	public ButtonWidget DragWidgetTop { get; set; }

	public ButtonWidget RotateWidget { get; set; }

	public BannerTableauWidget BannerTableauWidget { get; set; }

	public Widget EditableAreaVisualWidget { get; set; }

	public int LayerIndex { get; set; }

	public bool IsMirrorActive { get; set; }

	[Editor(false)]
	public bool IsLayerPattern
	{
		get
		{
			return _isLayerPattern;
		}
		set
		{
			if (_isLayerPattern != value)
			{
				_isLayerPattern = value;
				OnPropertyChanged(value, "IsLayerPattern");
				OnIsLayerPatternChanged(value);
			}
		}
	}

	[Editor(false)]
	public Vec2 PositionValue
	{
		get
		{
			return _positionValue;
		}
		set
		{
			if (_positionValue != value)
			{
				_positionValue = value;
				OnPropertyChanged(value, "PositionValue");
				OnPositionChanged(value);
			}
		}
	}

	[Editor(false)]
	public Vec2 SizeValue
	{
		get
		{
			return _sizeValue;
		}
		set
		{
			if (_sizeValue != value)
			{
				_sizeValue = value;
				OnPropertyChanged(value, "SizeValue");
				OnSizeChanged(value);
			}
		}
	}

	[Editor(false)]
	public float RotationValue
	{
		get
		{
			return _rotationValue;
		}
		set
		{
			if (_rotationValue != value)
			{
				_rotationValue = value;
				OnPropertyChanged(value, "RotationValue");
				OnRotationChanged(value);
			}
		}
	}

	[Editor(false)]
	public int EditableAreaSize
	{
		get
		{
			return _editableAreaSize;
		}
		set
		{
			if (_editableAreaSize != value)
			{
				_editableAreaSize = value;
				OnPropertyChanged(value, "EditableAreaSize");
			}
		}
	}

	[Editor(false)]
	public int TotalAreaSize
	{
		get
		{
			return _totalAreaSize;
		}
		set
		{
			if (_totalAreaSize != value)
			{
				_totalAreaSize = value;
				OnPropertyChanged(value, "TotalAreaSize");
			}
		}
	}

	public BannerBuilderEditableAreaWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		BannerTableauWidget.MeshIndexToUpdate = LayerIndex;
		if (!_initialized)
		{
			Initialize();
		}
		UpdateRequiredValues();
		UpdateEditableAreaVisual();
		HandleCursor();
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (!Input.IsKeyDown(InputKey.LeftMouseButton))
		{
			BuilderMode currentMode = _currentMode;
			if (currentMode != BuilderMode.None && (uint)(currentMode - 1) <= 4u)
			{
				EventFired("RefreshBanner");
			}
		}
		HandleRotation();
		HandlePositioning();
		HandleForEdge(EdgeResizeType.Right);
		HandleForEdge(EdgeResizeType.Top);
		HandleForCorner();
		_latestMousePosition = base.EventManager.MousePosition;
	}

	private void Initialize()
	{
		_centerOfSigil = new Vec2(0f, 0f);
		_sizeOfSigil = new Vec2(0f, 0f);
		_initialized = true;
		OnIsLayerPatternChanged(isLayerPattern: false);
	}

	private void UpdateRequiredValues()
	{
		float x = _positionValue.X / (float)TotalAreaSize * base.Size.X;
		float y = _positionValue.Y / (float)TotalAreaSize * base.Size.Y;
		_centerOfSigil.x = x;
		_centerOfSigil.y = y;
		float x2 = _sizeValue.X / (float)TotalAreaSize * base.Size.X;
		float y2 = _sizeValue.Y / (float)TotalAreaSize * base.Size.Y;
		_sizeOfSigil.x = x2;
		_sizeOfSigil.y = y2;
		_positionLimitMin = 0f;
		_positionLimitMax = _positionLimitMin + (float)TotalAreaSize;
		_sizeLimitMax = TotalAreaSize;
		_areaScale = (float)TotalAreaSize / base.Size.X;
	}

	private void HandlePositioning()
	{
		if (Input.IsKeyDown(InputKey.LeftMouseButton))
		{
			if (_currentMode == BuilderMode.Positioning)
			{
				Vector2 vector = base.EventManager.MousePosition - _latestMousePosition;
				vector *= (float)TotalAreaSize / base.Size.X;
				Vector2 vector2 = new Vector2(PositionValue.X, PositionValue.Y);
				vector2 += vector;
				vector2 = new Vector2(TaleWorlds.Library.MathF.Clamp(vector2.X, _positionLimitMin, _positionLimitMax), TaleWorlds.Library.MathF.Clamp(vector2.Y, _positionLimitMin, _positionLimitMax));
				PositionValue = vector2;
				BannerTableauWidget.UpdatePositionValueManual = PositionValue;
			}
			if (_currentMode != BuilderMode.Positioning && base.EventManager.HoveredWidget == this)
			{
				_currentMode = BuilderMode.Positioning;
			}
		}
		else
		{
			_currentMode = BuilderMode.None;
		}
	}

	private void HandleRotation()
	{
		if (Input.IsKeyDown(InputKey.LeftMouseButton))
		{
			if (_currentMode == BuilderMode.Rotating)
			{
				Vec2 vec = base.GlobalPosition + _centerOfSigil;
				Vector2 vector = base.EventManager.MousePosition - new Vector2(vec.X, vec.y);
				vector.Y *= -1f;
				float num = AngleFromDir(vector);
				RotationValue = (float)Math.Round(num, 3);
				BannerTableauWidget.UpdateRotationValueManualWithMirror = (RotationValue, IsMirrorActive);
			}
			if (_currentMode != BuilderMode.Rotating && base.EventManager.HoveredWidget == RotateWidget)
			{
				_currentMode = BuilderMode.Rotating;
			}
		}
		else
		{
			_currentMode = BuilderMode.None;
		}
		UpdatePositionOfWidget(RotateWidget, WidgetPlacementType.Vertical, RotationValue, 55f, 30f);
	}

	private void HandleForEdge(EdgeResizeType resizeType)
	{
		ButtonWidget widgetFor = GetWidgetFor(resizeType);
		if (Input.IsKeyDown(InputKey.LeftMouseButton))
		{
			if (_currentMode == BuilderMode.HorizontalResizing || _currentMode == BuilderMode.VerticalResizing)
			{
				Vector2 a = base.EventManager.MousePosition - _resizeStartMousePosition;
				a.Y *= -1f;
				Vec2 b = DirFromAngle(RotationValue);
				b.y *= -1f;
				a = TransformToParent(a, b);
				a.X *= -1f;
				a.Y *= -1f;
				switch (_currentMode)
				{
				case BuilderMode.VerticalResizing:
					a.X = 0f;
					break;
				case BuilderMode.HorizontalResizing:
					a.Y = 0f;
					break;
				}
				a *= (float)TotalAreaSize / base.Size.X * 2f;
				Vec2 vec = new Vec2(_resizeStartSize.X, _resizeStartSize.Y);
				vec += (Vec2)a;
				vec = new Vector2((int)TaleWorlds.Library.MathF.Clamp((int)vec.X, 2f, _sizeLimitMax), (int)TaleWorlds.Library.MathF.Clamp((int)vec.Y, 2f, _sizeLimitMax));
				Vec2 vec2 = _resizeStartSize - vec;
				if (vec2.x != 0f || vec2.y != 0f)
				{
					BannerTableauWidget.UpdateSizeValueManual = vec;
					SizeValue = vec;
				}
			}
			if ((_currentMode != BuilderMode.HorizontalResizing || _currentMode != BuilderMode.VerticalResizing) && base.EventManager.HoveredWidget == widgetFor)
			{
				_resizeStartMousePosition = base.EventManager.MousePosition;
				_resizeStartWidget = base.EventManager.HoveredWidget;
				_resizeStartSize = SizeValue;
				_currentMode = ((base.EventManager.HoveredWidget == GetWidgetFor(EdgeResizeType.Right)) ? BuilderMode.HorizontalResizing : BuilderMode.VerticalResizing);
			}
		}
		else
		{
			_resizeStartWidget = null;
			_resizeStartSize = Vec2.Zero;
			_currentMode = BuilderMode.None;
		}
		UpdatePositionOfWidget(widgetFor, (resizeType != EdgeResizeType.Right) ? WidgetPlacementType.Vertical : WidgetPlacementType.Horizontal, RotationValue + AngleOffsetForEdge(resizeType), 15f);
	}

	private void HandleForCorner()
	{
		ButtonWidget dragWidgetTopRight = DragWidgetTopRight;
		if (Input.IsKeyDown(InputKey.LeftMouseButton))
		{
			bool flag = Input.IsKeyDown(InputKey.LeftShift) || Input.IsKeyDown(InputKey.RightShift);
			if (_currentMode == BuilderMode.RightCornerResizing)
			{
				Vector2 a = base.EventManager.MousePosition - _resizeStartMousePosition;
				a.Y *= -1f;
				a *= (float)TotalAreaSize / base.Size.X * 2f;
				Vec2 b = DirFromAngle(RotationValue);
				b.y *= -1f;
				a = TransformToParent(a, b);
				a.X *= -1f;
				a.Y *= -1f;
				Vec2 vec = new Vec2(_resizeStartSize.X, _resizeStartSize.Y);
				if (flag)
				{
					Vector2 vector = new Vector2(_centerOfSigil.X, _centerOfSigil.Y) + base.GlobalPosition;
					float num = (vector - _resizeStartMousePosition).Length();
					bool flag2 = (vector - base.EventManager.MousePosition).Length() < num;
					float num2 = a.Length() * _areaScale * (float)((!flag2) ? 1 : (-1));
					float length = _resizeStartSize.Length;
					float num3 = num2 / length;
					vec += num3 * vec * _areaScale / 4f;
				}
				else
				{
					vec += (Vec2)a;
				}
				vec = new Vector2((int)TaleWorlds.Library.MathF.Clamp((int)vec.X, 2f, _sizeLimitMax), (int)TaleWorlds.Library.MathF.Clamp((int)vec.Y, 2f, _sizeLimitMax));
				Vec2 vec2 = _resizeStartSize - vec;
				if (vec2.x != 0f || vec2.y != 0f)
				{
					BannerTableauWidget.UpdateSizeValueManual = vec;
					SizeValue = vec;
				}
			}
			if (_currentMode != BuilderMode.RightCornerResizing && base.EventManager.HoveredWidget == dragWidgetTopRight)
			{
				if (!flag || _resizeStartWidget == null)
				{
					_resizeStartMousePosition = base.EventManager.MousePosition;
					_resizeStartWidget = base.EventManager.HoveredWidget;
					_resizeStartSize = SizeValue;
				}
				_currentMode = BuilderMode.RightCornerResizing;
			}
		}
		else
		{
			_resizeStartWidget = null;
			_resizeStartSize = Vec2.Zero;
			_currentMode = BuilderMode.None;
		}
		UpdatePositionOfWidget(dragWidgetTopRight, WidgetPlacementType.Max, RotationValue + AngleOffsetForCorner(), 20f);
	}

	private void HandleCursor()
	{
		if (_currentMode == BuilderMode.Rotating || base.EventManager.HoveredWidget == RotateWidget)
		{
			base.Context.ActiveCursorOfContext = UIContext.MouseCursors.Rotate;
		}
		else if (_currentMode == BuilderMode.Positioning || base.EventManager.HoveredWidget == this)
		{
			base.Context.ActiveCursorOfContext = UIContext.MouseCursors.Move;
		}
		else if (_currentMode == BuilderMode.HorizontalResizing || base.EventManager.HoveredWidget == GetWidgetFor(EdgeResizeType.Right))
		{
			base.Context.ActiveCursorOfContext = UIContext.MouseCursors.HorizontalResize;
		}
		else if (_currentMode == BuilderMode.VerticalResizing || base.EventManager.HoveredWidget == GetWidgetFor(EdgeResizeType.Top))
		{
			base.Context.ActiveCursorOfContext = UIContext.MouseCursors.VerticalResize;
		}
		else if (_currentMode == BuilderMode.RightCornerResizing || base.EventManager.HoveredWidget == DragWidgetTopRight)
		{
			base.Context.ActiveCursorOfContext = UIContext.MouseCursors.DiagonalRightResize;
		}
		else
		{
			base.Context.ActiveCursorOfContext = UIContext.MouseCursors.Default;
		}
	}

	private void UpdateEditableAreaVisual()
	{
		EditableAreaVisualWidget.HorizontalAlignment = HorizontalAlignment.Center;
		EditableAreaVisualWidget.VerticalAlignment = VerticalAlignment.Center;
		EditableAreaVisualWidget.WidthSizePolicy = SizePolicy.Fixed;
		EditableAreaVisualWidget.HeightSizePolicy = SizePolicy.Fixed;
		float num = (float)EditableAreaSize / (float)TotalAreaSize;
		EditableAreaVisualWidget.ScaledSuggestedWidth = base.Size.X * num;
		EditableAreaVisualWidget.ScaledSuggestedHeight = base.Size.Y * num;
	}

	private ButtonWidget GetWidgetFor(EdgeResizeType edgeResizeType)
	{
		if (edgeResizeType != EdgeResizeType.Top)
		{
			return DragWidgetRight;
		}
		return DragWidgetTop;
	}

	private void UpdatePositionOfWidget(Widget widget, WidgetPlacementType placementType, float directionFromCenter, float distanceFromCenterModifier, float distanceFromEdgesModifier = 0f)
	{
		Vec2 vec = DirFromAngle(directionFromCenter);
		vec.y *= -1f;
		float num = 0f;
		switch (placementType)
		{
		case WidgetPlacementType.Horizontal:
			num = _sizeOfSigil.X;
			break;
		case WidgetPlacementType.Vertical:
			num = _sizeOfSigil.Y;
			break;
		case WidgetPlacementType.Max:
			num = _sizeOfSigil.Length;
			break;
		}
		float num2 = (num * base._inverseScaleToUse + distanceFromCenterModifier) * 0.5f * base._scaleToUse;
		Vec2 pos = _centerOfSigil + vec * num2;
		pos.x -= widget.Size.X / 2f;
		pos.y -= widget.Size.Y / 2f;
		ApplyPositionOffsetToWidget(widget, pos, distanceFromEdgesModifier * base._scaleToUse);
	}

	private void ApplyPositionOffsetToWidget(Widget widget, Vec2 pos, float additionalModifier = 0f)
	{
		widget.ScaledPositionXOffset = TaleWorlds.Library.MathF.Clamp(pos.x, 12f + additionalModifier, base.Size.X - (12f + additionalModifier));
		widget.ScaledPositionYOffset = TaleWorlds.Library.MathF.Clamp(pos.y, 12f + additionalModifier, base.Size.Y - (12f + additionalModifier));
	}

	private void OnIsLayerPatternChanged(bool isLayerPattern)
	{
	}

	private void OnPositionChanged(Vec2 newPosition)
	{
	}

	private void OnSizeChanged(Vec2 newSize)
	{
	}

	private void OnRotationChanged(float newRotation)
	{
	}

	private static Vec2 DirFromAngle(float angle)
	{
		float x = angle * (System.MathF.PI * 2f);
		return new Vec2(0f - TaleWorlds.Library.MathF.Sin(x), TaleWorlds.Library.MathF.Cos(x));
	}

	private static float AngleFromDir(Vec2 directionVector)
	{
		float num = ((!(directionVector.X < 0f)) ? (360f - (float)Math.Atan2(directionVector.X, directionVector.Y) * 57.29578f) : ((float)Math.Atan2(directionVector.X, directionVector.Y) * 57.29578f * -1f));
		return num / 360f;
	}

	private static float AngleOffsetForEdge(EdgeResizeType edge)
	{
		return 1f - (float)edge * 0.25f;
	}

	private static float AngleOffsetForCorner()
	{
		return 0.875f;
	}

	private static Vec2 TransformToParent(Vec2 a, Vec2 b)
	{
		return new Vec2(b.Y * a.X + b.X * a.Y, (0f - b.X) * a.X + b.Y * a.Y);
	}

	private static Vector2 TransformToParent(Vector2 a, Vec2 b)
	{
		return new Vector2(b.Y * a.X + b.X * a.Y, (0f - b.X) * a.X + b.Y * a.Y);
	}

	private static Vector2 TransformToParent(Vec2 a, Vector2 b)
	{
		return new Vector2(b.Y * a.X + b.X * a.Y, (0f - b.X) * a.X + b.Y * a.Y);
	}

	private static Vector2 TransformToParent(Vector2 a, Vector2 b)
	{
		return new Vector2(b.Y * a.X + b.X * a.Y, (0f - b.X) * a.Y + b.Y * a.Y);
	}
}
