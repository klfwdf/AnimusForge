using System.Collections.Generic;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Chat;

public class ChatLogWidget : Widget
{
	private List<ChatCollapsableListPanel> _registeredMultilineWidgets = new List<ChatCollapsableListPanel>();

	private bool _isInitialized;

	private float _lerpRatio;

	private bool _isResizing;

	private bool _resizeActualPanel;

	private Vec2 _resizeStartMousePosition;

	private Vec2 _resizeOriginalSize;

	private (SizePolicy, SizePolicy) _innerPanelDefaultSizePolicies;

	private bool _focusOnNextUpdate;

	private bool _isChatDisabled;

	private bool _isMPChatLog;

	private bool _finishedResizing;

	private bool _fullyShowChat;

	private bool _fullyShowChatWithTyping;

	private EditableTextWidget _textInputWidget;

	private ScrollbarWidget _scrollbar;

	private ScrollablePanel _scrollablePanel;

	private Widget _resizerWidget;

	private Widget _resizeFrameWidget;

	private float _sizeX;

	private float _sizeY;

	private ListPanel _messageHistoryList;

	private float _resizeTransitionTime => 0.14f;

	[DataSourceProperty]
	public bool IsChatDisabled
	{
		get
		{
			return _isChatDisabled;
		}
		set
		{
			if (value != _isChatDisabled)
			{
				_isChatDisabled = value;
				OnPropertyChanged(value, "IsChatDisabled");
			}
		}
	}

	[DataSourceProperty]
	public bool FinishedResizing
	{
		get
		{
			return _finishedResizing;
		}
		set
		{
			if (value != _finishedResizing)
			{
				_finishedResizing = value;
				OnPropertyChanged(value, "FinishedResizing");
			}
		}
	}

	[DataSourceProperty]
	public bool FullyShowChat
	{
		get
		{
			return _fullyShowChat;
		}
		set
		{
			if (value != _fullyShowChat)
			{
				_fullyShowChat = value;
			}
		}
	}

	[DataSourceProperty]
	public bool FullyShowChatWithTyping
	{
		get
		{
			return _fullyShowChatWithTyping;
		}
		set
		{
			if (value != _fullyShowChatWithTyping)
			{
				_fullyShowChatWithTyping = value;
				if (!IsChatDisabled && TextInputWidget != null && _fullyShowChatWithTyping)
				{
					_focusOnNextUpdate = true;
				}
				base.EventManager.FocusedWidget = null;
				OnPropertyChanged(value, "FullyShowChatWithTyping");
			}
		}
	}

	[DataSourceProperty]
	public EditableTextWidget TextInputWidget
	{
		get
		{
			return _textInputWidget;
		}
		set
		{
			if (value != _textInputWidget)
			{
				_textInputWidget = value;
				OnPropertyChanged(value, "TextInputWidget");
			}
		}
	}

	[DataSourceProperty]
	public ScrollbarWidget Scrollbar
	{
		get
		{
			return _scrollbar;
		}
		set
		{
			if (value != _scrollbar)
			{
				_scrollbar = value;
				OnPropertyChanged(value, "Scrollbar");
			}
		}
	}

	[DataSourceProperty]
	public ScrollablePanel ScrollablePanel
	{
		get
		{
			return _scrollablePanel;
		}
		set
		{
			if (value != _scrollablePanel)
			{
				_scrollablePanel = value;
				OnPropertyChanged(value, "ScrollablePanel");
			}
		}
	}

	[DataSourceProperty]
	public Widget ResizerWidget
	{
		get
		{
			return _resizerWidget;
		}
		set
		{
			if (value != _resizerWidget)
			{
				_resizerWidget = value;
				OnPropertyChanged(value, "ResizerWidget");
			}
		}
	}

	[DataSourceProperty]
	public Widget ResizeFrameWidget
	{
		get
		{
			return _resizeFrameWidget;
		}
		set
		{
			if (value != _resizeFrameWidget)
			{
				_resizeFrameWidget = value;
				OnPropertyChanged(value, "ResizeFrameWidget");
			}
		}
	}

	[DataSourceProperty]
	public float SizeX
	{
		get
		{
			return _sizeX;
		}
		set
		{
			if (value != _sizeX)
			{
				_sizeX = value;
				base.SuggestedWidth = value;
				OnPropertyChanged(value, "SizeX");
			}
		}
	}

	[DataSourceProperty]
	public float SizeY
	{
		get
		{
			return _sizeY;
		}
		set
		{
			if (value != _sizeY)
			{
				_sizeY = value;
				base.SuggestedHeight = value;
				OnPropertyChanged(value, "SizeY");
			}
		}
	}

	[DataSourceProperty]
	public ListPanel MessageHistoryList
	{
		get
		{
			return _messageHistoryList;
		}
		set
		{
			if (value != _messageHistoryList)
			{
				_messageHistoryList = value;
				OnPropertyChanged(value, "MessageHistoryList");
			}
		}
	}

	[DataSourceProperty]
	public bool IsMPChatLog
	{
		get
		{
			return _isMPChatLog;
		}
		set
		{
			if (value != _isMPChatLog)
			{
				_isMPChatLog = value;
				OnPropertyChanged(value, "IsMPChatLog");
			}
		}
	}

	public ChatLogWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (!IsChatDisabled && TextInputWidget != null && FullyShowChatWithTyping && _focusOnNextUpdate)
		{
			base.EventManager.FocusedWidget = TextInputWidget;
			_focusOnNextUpdate = false;
		}
		if (!FullyShowChat)
		{
			ScrollablePanel.ResetTweenSpeed();
			Scrollbar.ValueFloat = Scrollbar.MaxValue;
		}
		base.ParentWidget.DoNotPassEventsToChildren = !FullyShowChat;
		if (ResizerWidget != null && ResizeFrameWidget != null)
		{
			UpdateResize(dt);
		}
	}

	private void UpdateResize(float dt)
	{
		if (Input.IsKeyPressed(InputKey.LeftMouseButton) && base.EventManager.HoveredWidget == ResizerWidget)
		{
			_isResizing = true;
			_resizeStartMousePosition = Input.MousePositionPixel;
			_resizeOriginalSize = new Vec2(SizeX, SizeY);
			ResizeFrameWidget.IsVisible = true;
			ResizeFrameWidget.WidthSizePolicy = SizePolicy.Fixed;
			ResizeFrameWidget.HeightSizePolicy = SizePolicy.Fixed;
			ResizeFrameWidget.SuggestedHeight = SizeY;
			ResizeFrameWidget.SuggestedWidth = SizeX;
			_innerPanelDefaultSizePolicies = (ScrollablePanel.InnerPanel.WidthSizePolicy, ScrollablePanel.InnerPanel.HeightSizePolicy);
			ScrollablePanel.InnerPanel.WidthSizePolicy = SizePolicy.Fixed;
			ScrollablePanel.InnerPanel.HeightSizePolicy = SizePolicy.Fixed;
			ScrollablePanel.InnerPanel.SuggestedWidth = ScrollablePanel.InnerPanel.Size.X;
			ScrollablePanel.InnerPanel.SuggestedHeight = ScrollablePanel.InnerPanel.Size.Y;
		}
		else if (Input.IsKeyReleased(InputKey.LeftMouseButton))
		{
			if (_isResizing)
			{
				ResizeFrameWidget.IsVisible = false;
				_resizeActualPanel = true;
				_lerpRatio = 0f;
			}
			_isResizing = false;
		}
		if (_isResizing)
		{
			Vec2 vec = _resizeOriginalSize + new Vec2((Input.MousePositionPixel - _resizeStartMousePosition).X, 0f - (Input.MousePositionPixel - _resizeStartMousePosition).Y);
			ResizeFrameWidget.SuggestedWidth = Mathf.Clamp(vec.X, base.MinWidth, base.MaxWidth);
			ResizeFrameWidget.SuggestedHeight = Mathf.Clamp(vec.Y, base.MinHeight, base.MaxHeight) - ResizeFrameWidget.MarginBottom;
		}
		else if (_resizeActualPanel)
		{
			_lerpRatio = Mathf.Clamp(_lerpRatio + dt / _resizeTransitionTime, 0f, 1f);
			SizeX = Mathf.Lerp(_resizeOriginalSize.x, ResizeFrameWidget.SuggestedWidth, _lerpRatio);
			SizeY = Mathf.Lerp(_resizeOriginalSize.y, ResizeFrameWidget.SuggestedHeight + ResizeFrameWidget.MarginBottom, _lerpRatio);
			if (SizeX.ApproximatelyEqualsTo(ResizeFrameWidget.SuggestedWidth, 0.01f) && SizeY.ApproximatelyEqualsTo(ResizeFrameWidget.SuggestedHeight + ResizeFrameWidget.MarginBottom, 0.01f))
			{
				SizeX = ResizeFrameWidget.SuggestedWidth;
				SizeY = ResizeFrameWidget.SuggestedHeight + ResizeFrameWidget.MarginBottom;
				ResizeFrameWidget.WidthSizePolicy = SizePolicy.StretchToParent;
				ResizeFrameWidget.HeightSizePolicy = SizePolicy.StretchToParent;
				ScrollablePanel.InnerPanel.WidthSizePolicy = _innerPanelDefaultSizePolicies.Item1;
				ScrollablePanel.InnerPanel.HeightSizePolicy = _innerPanelDefaultSizePolicies.Item2;
				_resizeActualPanel = false;
				EventFired("FinishResize");
			}
		}
		else if (!_isInitialized)
		{
			SizeX = base.SuggestedWidth;
			SizeY = base.SuggestedHeight;
			_isInitialized = true;
		}
	}

	public void RegisterMultiLineElement(ChatCollapsableListPanel element)
	{
		if (!_registeredMultilineWidgets.Contains(element))
		{
			_registeredMultilineWidgets.Add(element);
		}
	}

	public void RemoveMultiLineElement(ChatCollapsableListPanel element)
	{
		if (_registeredMultilineWidgets.Contains(element))
		{
			_registeredMultilineWidgets.Remove(element);
		}
	}
}
