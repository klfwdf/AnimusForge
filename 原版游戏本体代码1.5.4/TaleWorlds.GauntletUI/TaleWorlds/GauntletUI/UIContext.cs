using System;
using System.Collections.Generic;
using System.Numerics;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.GauntletInput;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI;

public class UIContext
{
	public enum MouseCursors
	{
		System,
		Default,
		Attack,
		Move,
		HorizontalResize,
		VerticalResize,
		DiagonalRightResize,
		DiagonalLeftResize,
		Rotate,
		Custom,
		Disabled,
		RightClickLink
	}

	private class DebugWidgetTreeNode
	{
		private readonly TwoDimensionContext _context;

		private readonly Widget _current;

		private readonly List<DebugWidgetTreeNode> _children;

		private readonly string _fullIDPath;

		private readonly string _displayedName;

		private readonly int _depth;

		private bool _isShowingArea;

		private string ID => $"{_depth}.{_current.GetSiblingIndex()}.{_fullIDPath}";

		public DebugWidgetTreeNode(TwoDimensionContext context, Widget current, int depth)
		{
			_context = context;
			_current = current;
			_depth = depth;
			_fullIDPath = _current?.GetFullIDPath() ?? string.Empty;
			int num = _fullIDPath.LastIndexOf('\\');
			if (num != -1)
			{
				_displayedName = _fullIDPath.Substring(num + 1);
			}
			if (string.IsNullOrEmpty(_displayedName))
			{
				_displayedName = _current.Id;
			}
			_children = new List<DebugWidgetTreeNode>();
			AddChildren();
		}

		private void AddChildren()
		{
			foreach (Widget child in _current.Children)
			{
				if (child.ParentWidget == _current)
				{
					DebugWidgetTreeNode item = new DebugWidgetTreeNode(_context, child, _depth + 1);
					_children.Add(item);
				}
			}
		}

		public void DebugDraw()
		{
			if (_context.DrawDebugTreeNode(_displayedName + "###Root." + ID))
			{
				if (_context.IsDebugItemHovered())
				{
					DrawArea();
				}
				_context.DrawCheckbox("Show Area###Area." + ID, ref _isShowingArea);
				if (_isShowingArea)
				{
					DrawArea();
				}
				DrawProperties();
				DrawChildren();
				_context.PopDebugTreeNode();
			}
			else if (_context.IsDebugItemHovered())
			{
				DrawArea();
			}
		}

		private void DrawProperties()
		{
			if (_context.DrawDebugTreeNode("Properties###Properties." + ID))
			{
				_context.DrawDebugText("General");
				string text = (string.IsNullOrEmpty(_current.Id) ? "_No ID_" : _current.Id);
				_context.DrawDebugText("\tID: " + text);
				_context.DrawDebugText("\tPath: " + _current.GetFullIDPath());
				_context.DrawDebugText($"\tVisible: {_current.IsVisible}");
				_context.DrawDebugText($"\tEnabled: {_current.IsEnabled}");
				_context.DrawDebugText("\nSize");
				_context.DrawDebugText($"\tWidth Size Policy: {_current.WidthSizePolicy}");
				_context.DrawDebugText($"\tHeight Size Policy: {_current.HeightSizePolicy}");
				_context.DrawDebugText($"\tSize: {_current.Size}");
				_context.DrawDebugText("\nPosition");
				_context.DrawDebugText($"\tGlobal Position: {_current.GlobalPosition}");
				_context.DrawDebugText($"\tLocal Position: {_current.LocalPosition}");
				_context.DrawDebugText($"\tPosition Offset: <{_current.PositionXOffset}, {_current.PositionYOffset}>");
				_context.DrawDebugText("\nEvents");
				_context.DrawDebugText("\tCurrent State: " + _current.CurrentState);
				_context.DrawDebugText($"\tCan Accept Events: {_current.CanAcceptEvents}");
				_context.DrawDebugText($"\tPasses Events To Children: {!_current.DoNotPassEventsToChildren}");
				_context.DrawDebugText("\nVisuals");
				BrushWidget brushWidget = _current as BrushWidget;
				if (brushWidget != null)
				{
					_context.DrawDebugText("\tBrush: " + brushWidget.Brush.Name);
				}
				if (_current is TextWidget textWidget)
				{
					_context.DrawDebugText("\tText: " + textWidget.Text);
				}
				else if (_current is RichTextWidget richTextWidget)
				{
					_context.DrawDebugText("\tText: " + richTextWidget.Text);
				}
				else if (brushWidget != null)
				{
					_context.DrawDebugText("\tSprite: " + (brushWidget.BrushRenderer?.CurrentStyle?.GetLayer(brushWidget.BrushRenderer.CurrentState)?.Sprite?.Name ?? "None"));
					_context.DrawDebugText("\tColor: " + brushWidget.Brush?.GetLayer(brushWidget.CurrentState));
				}
				else
				{
					_context.DrawDebugText("\tSprite: " + (_current.Sprite?.Name ?? "None"));
					_context.DrawDebugText("\tColor: " + _current.Color.ToString());
				}
				_context.PopDebugTreeNode();
			}
		}

		private void DrawChildren()
		{
			if (_children.Count <= 0 || !_context.DrawDebugTreeNode("Children###Children." + ID))
			{
				return;
			}
			foreach (DebugWidgetTreeNode child in _children)
			{
				child.DebugDraw();
			}
			_context.PopDebugTreeNode();
		}

		private void DrawArea()
		{
			float x = _current.GlobalPosition.X;
			float y = _current.GlobalPosition.Y;
			float num = _current.GlobalPosition.X + _current.Size.X;
			float num2 = _current.GlobalPosition.Y + _current.Size.Y;
			if (x != num && y != num2 && _current.Size.X != 0f && _current.Size.Y != 0f)
			{
				float num3 = 2f;
				_ = num3 / 2f;
				_ = num3 / 2f;
				_ = num3 / 2f;
				_ = num3 / 2f;
				_ = num3 / 2f;
				_ = num3 / 2f;
				_ = num3 / 2f;
				_ = num3 / 2f;
			}
		}
	}

	private readonly float ReferenceHeight;

	private readonly float InverseReferenceHeight;

	private readonly float ReferenceAspectRatio;

	private const float ReferenceAspectRatioCoeff = 0.98f;

	private readonly GauntletInputContext _uiInputContext;

	private readonly IInputContext _inputContext;

	private bool _initializedWithExistingResources;

	private bool _previousFrameMouseEnabled;

	private bool _isMouseEnabled;

	private bool IsDebugWidgetInformationFroze;

	private DebugWidgetTreeNode _currentRootNode;

	public MouseCursors ActiveCursorOfContext { get; set; }

	public bool IsDynamicScaleEnabled { get; set; } = true;

	public float ScaleModifier { get; set; } = 1f;

	public string Name { get; set; }

	public bool IsActive { get; private set; }

	public float ContextAlpha { get; set; } = 1f;

	public float Scale { get; private set; }

	public float CustomScale { get; private set; }

	public float CustomInverseScale { get; private set; }

	public string CurrentLanugageCode { get; private set; }

	public Random UIRandom { get; private set; }

	public float InverseScale { get; private set; }

	public EventManager EventManager { get; private set; }

	public Widget Root => EventManager.Root;

	public ResourceDepot ResourceDepot => TwoDimensionContext.ResourceDepot;

	public TwoDimensionContext TwoDimensionContext { get; private set; }

	public IEnumerable<Brush> Brushes => BrushFactory.Brushes;

	public Brush DefaultBrush => BrushFactory.DefaultBrush;

	public SpriteData SpriteData { get; private set; }

	public BrushFactory BrushFactory { get; private set; }

	public FontFactory FontFactory { get; private set; }

	public IReadonlyInputContext InputContext => _uiInputContext;

	public IGamepadNavigationContext GamepadNavigation { get; private set; }

	public ulong LocalFrameNumber { get; private set; }

	public UIContext(TwoDimensionContext twoDimensionContext, IInputContext inputContext, SpriteData spriteData, FontFactory fontFactory, BrushFactory brushFactory)
	{
		_isMouseEnabled = true;
		_inputContext = inputContext;
		_initializedWithExistingResources = true;
		_uiInputContext = new GauntletInputContext(inputContext);
		TwoDimensionContext = twoDimensionContext;
		GamepadNavigation = new EmptyGamepadNavigationContext();
		SpriteData = spriteData;
		FontFactory = fontFactory;
		BrushFactory = brushFactory;
		ReferenceHeight = twoDimensionContext.Platform.ReferenceHeight;
		InverseReferenceHeight = 1f / ReferenceHeight;
		ReferenceAspectRatio = twoDimensionContext.Platform.ReferenceWidth / twoDimensionContext.Platform.ReferenceHeight;
	}

	public UIContext(TwoDimensionContext twoDimensionContext, IInputContext inputContext)
	{
		_isMouseEnabled = true;
		_initializedWithExistingResources = false;
		_inputContext = inputContext;
		_uiInputContext = new GauntletInputContext(inputContext);
		TwoDimensionContext = twoDimensionContext;
		GamepadNavigation = new EmptyGamepadNavigationContext();
		ReferenceHeight = twoDimensionContext.Platform.ReferenceHeight;
		InverseReferenceHeight = 1f / ReferenceHeight;
		ReferenceAspectRatio = twoDimensionContext.Platform.ReferenceWidth / twoDimensionContext.Platform.ReferenceHeight;
	}

	public void Initialize()
	{
		if (!_initializedWithExistingResources)
		{
			SpriteData = new SpriteData("SpriteData");
			SpriteData.Load(ResourceDepot);
			FontFactory = new FontFactory(ResourceDepot);
			FontFactory.LoadAllFonts(SpriteData);
			BrushFactory = new BrushFactory(ResourceDepot, "Brushes", SpriteData, FontFactory);
			BrushFactory.Initialize();
		}
		EventManager = new EventManager(this);
		Widget root = Root;
		root.WidthSizePolicy = SizePolicy.Fixed;
		root.HeightSizePolicy = SizePolicy.Fixed;
		root.SuggestedWidth = TwoDimensionContext.Width;
		root.SuggestedHeight = TwoDimensionContext.Height;
		UIRandom = new Random();
		UpdateScale();
	}

	public Brush GetBrush(string name)
	{
		return BrushFactory.GetBrush(name);
	}

	public void RefreshResources(SpriteData spriteData, FontFactory fontFactory, BrushFactory brushFactory)
	{
		SpriteData = spriteData;
		FontFactory = fontFactory;
		BrushFactory = brushFactory;
	}

	public void OnFinalize()
	{
		GamepadNavigation.OnFinalize();
		EventManager.OnFinalize();
		GamepadNavigation.OnFinalize();
	}

	public void Deactivate()
	{
		IsActive = false;
		EventManager.OnContextDeactivated();
	}

	public void Activate()
	{
		IsActive = true;
		EventManager.OnContextActivated();
	}

	public void Update(float dt)
	{
		ActiveCursorOfContext = MouseCursors.Default;
		if (!_initializedWithExistingResources)
		{
			BrushFactory.CheckForUpdates();
		}
		if (IsDynamicScaleEnabled)
		{
			UpdateScale();
		}
		Widget root = Root;
		root.SuggestedWidth = TwoDimensionContext.Width;
		root.SuggestedHeight = TwoDimensionContext.Height;
		EventManager.Update(dt);
		LocalFrameNumber++;
	}

	public void LateUpdate(float dt)
	{
		Vector2 pageSize = new Vector2(TwoDimensionContext.Width, TwoDimensionContext.Height);
		EventManager.CalculateCanvas(pageSize, dt);
		EventManager.LateUpdate(dt);
		EventManager.RecalculateCanvas();
		EventManager.DefragContainers();
	}

	public void RenderTick(float dt)
	{
		EventManager.UpdateBrushes(dt);
		EventManager.Render(TwoDimensionContext);
	}

	public void OnOnScreenkeyboardTextInputDone(string inputText)
	{
		if (EventManager.FocusedWidget is EditableTextWidget editableTextWidget)
		{
			editableTextWidget.SetAllText(inputText);
		}
		CancelMouseClick();
	}

	public void InitializeGamepadNavigation(IGamepadNavigationContext context)
	{
		GamepadNavigation = context;
	}

	private void UpdateScale()
	{
		float num = 1f;
		if (TwoDimensionContext != null)
		{
			num = TwoDimensionContext.Height * InverseReferenceHeight;
			float num2 = TwoDimensionContext.Width / TwoDimensionContext.Height;
			if (num2 < ReferenceAspectRatio * 0.98f)
			{
				float num3 = num2 / (ReferenceAspectRatio * 0.98f);
				num *= num3;
			}
		}
		else
		{
			num = 1f;
		}
		if (Scale != num || CustomScale != Scale * ScaleModifier)
		{
			Scale = num;
			CustomScale = Scale * ScaleModifier;
			InverseScale = 1f / num;
			CustomInverseScale = 1f / CustomScale;
			EventManager.UpdateLayout();
		}
	}

	public void OnOnScreenKeyboardCanceled()
	{
		CancelMouseClick();
	}

	public bool HitTest(Widget root, Vector2 position)
	{
		return EventManager.HitTest(root, position);
	}

	public bool HitTest(Widget root)
	{
		if (root == null)
		{
			return false;
		}
		return EventManager.HitTest(root, _uiInputContext.GetMousePosition());
	}

	public bool FocusTest(Widget root)
	{
		return EventManager.FocusTest(root);
	}

	public void SetIsMouseEnabled(bool isMouseEnabled)
	{
		_isMouseEnabled = isMouseEnabled;
	}

	public void UpdateInput(InputType handleInputs)
	{
		if (_isMouseEnabled || EventManager.FocusedWidget != null)
		{
			if (handleInputs.HasAnyFlag(InputType.MouseButton))
			{
				EventManager.MouseMove();
				InputKey[] clickKeys = _inputContext.GetClickKeys();
				foreach (InputKey key in clickKeys)
				{
					if (_inputContext.IsKeyPressed(key))
					{
						EventManager.MouseDown();
						break;
					}
				}
				foreach (InputKey key2 in clickKeys)
				{
					if (_inputContext.IsKeyReleased(key2))
					{
						EventManager.MouseUp();
						break;
					}
				}
				if (_inputContext.IsKeyPressed(InputKey.RightMouseButton))
				{
					EventManager.MouseAlternateDown();
				}
				if (_inputContext.IsKeyReleased(InputKey.RightMouseButton))
				{
					EventManager.MouseAlternateUp();
				}
			}
			if (handleInputs.HasAnyFlag(InputType.MouseWheel))
			{
				EventManager.MouseScroll();
			}
			EventManager.RightStickMovement();
			_previousFrameMouseEnabled = true;
		}
		else if (_previousFrameMouseEnabled)
		{
			CancelMouseClick();
			_previousFrameMouseEnabled = false;
		}
	}

	public void OnMovieLoaded(string movieName)
	{
		GamepadNavigation.OnMovieLoaded(movieName);
	}

	public void OnMovieReleased(string movieName)
	{
		GamepadNavigation.OnMovieReleased(movieName);
	}

	public void CancelMouseClick()
	{
		EventManager.MouseUp(isFromInput: false);
		EventManager.MouseAlternateUp(isFromInput: false);
		EventManager.ClearFocus();
	}

	public void DrawWidgetDebugInfo()
	{
		if (Input.IsKeyDown(InputKey.LeftShift) && Input.IsKeyPressed(InputKey.F))
		{
			IsDebugWidgetInformationFroze = !IsDebugWidgetInformationFroze;
			_currentRootNode = new DebugWidgetTreeNode(TwoDimensionContext, Root, 0);
		}
		if (IsDebugWidgetInformationFroze)
		{
			_currentRootNode?.DebugDraw();
		}
	}
}
