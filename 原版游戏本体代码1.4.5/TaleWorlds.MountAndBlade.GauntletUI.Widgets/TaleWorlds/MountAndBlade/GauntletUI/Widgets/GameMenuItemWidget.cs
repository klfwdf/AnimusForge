using System;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class GameMenuItemWidget : Widget
{
	public Action OnOptionStateChanged;

	private string _latestTextWidgetState = "";

	private int _itemType;

	private bool _isWaitActive;

	private bool _isMainStoryQuest;

	private BrushWidget _waitStateWidget;

	private BrushWidget _leaveTypeIcon;

	private string _leaveType;

	private int _questType = -1;

	private int _issueType = -1;

	private BrushWidget _questIconWidget;

	private BrushWidget _issueIconWidget;

	private ButtonWidget _parentButton;

	private string _gameMenuStringId;

	private int _battleSize;

	private bool _isNavalBattle;

	public Brush DefaultTextBrush { get; set; }

	public Brush HoveredTextBrush { get; set; }

	public Brush PressedTextBrush { get; set; }

	public Brush DisabledTextBrush { get; set; }

	public Brush NormalQuestBrush { get; set; }

	public Brush MainStoryQuestBrush { get; set; }

	public RichTextWidget ItemRichTextWidget { get; set; }

	public int ItemType
	{
		get
		{
			return _itemType;
		}
		set
		{
			if (_itemType != value)
			{
				_itemType = value;
				OnPropertyChanged(value, "ItemType");
			}
		}
	}

	public BrushWidget QuestIconWidget
	{
		get
		{
			return _questIconWidget;
		}
		set
		{
			if (_questIconWidget != value)
			{
				_questIconWidget = value;
				OnPropertyChanged(value, "QuestIconWidget");
			}
		}
	}

	public BrushWidget IssueIconWidget
	{
		get
		{
			return _issueIconWidget;
		}
		set
		{
			if (_issueIconWidget != value)
			{
				_issueIconWidget = value;
				OnPropertyChanged(value, "IssueIconWidget");
			}
		}
	}

	public string LeaveType
	{
		get
		{
			return _leaveType;
		}
		set
		{
			if (_leaveType != value)
			{
				_leaveType = value;
				OnPropertyChanged(value, "LeaveType");
				UpdateLeaveTypeIcon();
				UpdateLeaveTypeSound();
			}
		}
	}

	public bool IsMainStoryQuest
	{
		get
		{
			return _isMainStoryQuest;
		}
		set
		{
			if (_isMainStoryQuest != value)
			{
				_isMainStoryQuest = value;
				OnPropertyChanged(value, "IsMainStoryQuest");
				SetProgressIconType(QuestType, QuestIconWidget);
			}
		}
	}

	public int QuestType
	{
		get
		{
			return _questType;
		}
		set
		{
			if (_questType != value)
			{
				_questType = value;
				OnPropertyChanged(value, "QuestType");
				SetProgressIconType(value, QuestIconWidget);
			}
		}
	}

	public int IssueType
	{
		get
		{
			return _issueType;
		}
		set
		{
			if (_issueType != value)
			{
				_issueType = value;
				OnPropertyChanged(value, "IssueType");
				SetProgressIconType(value, IssueIconWidget);
			}
		}
	}

	public bool IsWaitActive
	{
		get
		{
			return _isWaitActive;
		}
		set
		{
			if (_isWaitActive != value)
			{
				_isWaitActive = value;
				OnPropertyChanged(value, "IsWaitActive");
			}
		}
	}

	public BrushWidget LeaveTypeIcon
	{
		get
		{
			return _leaveTypeIcon;
		}
		set
		{
			if (_leaveTypeIcon != value)
			{
				_leaveTypeIcon = value;
				OnPropertyChanged(value, "LeaveTypeIcon");
				if (value != null)
				{
					LeaveTypeIcon.IsVisible = false;
				}
			}
		}
	}

	public BrushWidget WaitStateWidget
	{
		get
		{
			return _waitStateWidget;
		}
		set
		{
			if (_waitStateWidget != value)
			{
				_waitStateWidget = value;
				OnPropertyChanged(value, "WaitStateWidget");
			}
		}
	}

	public ButtonWidget ParentButton
	{
		get
		{
			return _parentButton;
		}
		set
		{
			if (value != _parentButton)
			{
				_parentButton = value;
				OnPropertyChanged(value, "ParentButton");
				_parentButton.boolPropertyChanged += ParentButton_PropertyChanged;
			}
		}
	}

	public string GameMenuStringId
	{
		get
		{
			return _gameMenuStringId;
		}
		set
		{
			if (value != _gameMenuStringId)
			{
				_gameMenuStringId = value;
				OnPropertyChanged(value, "GameMenuStringId");
				UpdateLeaveTypeSound();
			}
		}
	}

	public int BattleSize
	{
		get
		{
			return _battleSize;
		}
		set
		{
			if (value != _battleSize)
			{
				_battleSize = value;
				OnPropertyChanged(value, "BattleSize");
				UpdateLeaveTypeSound();
			}
		}
	}

	public bool IsNavalBattle
	{
		get
		{
			return _isNavalBattle;
		}
		set
		{
			if (value != _isNavalBattle)
			{
				_isNavalBattle = value;
				OnPropertyChanged(value, "IsNavalBattle");
				UpdateLeaveTypeSound();
			}
		}
	}

	public GameMenuItemWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (_latestTextWidgetState != ItemRichTextWidget.CurrentState)
		{
			if (ItemRichTextWidget.CurrentState == "Default")
			{
				ItemRichTextWidget.Brush = DefaultTextBrush;
			}
			else if (ItemRichTextWidget.CurrentState == "Hovered")
			{
				ItemRichTextWidget.Brush = HoveredTextBrush;
			}
			else if (ItemRichTextWidget.CurrentState == "Pressed")
			{
				ItemRichTextWidget.Brush = PressedTextBrush;
			}
			else if (ItemRichTextWidget.CurrentState == "Disabled")
			{
				ItemRichTextWidget.Brush = DisabledTextBrush;
			}
			_latestTextWidgetState = ItemRichTextWidget.CurrentState;
		}
	}

	private void UpdateLeaveTypeIcon()
	{
		if (!string.IsNullOrEmpty(LeaveType))
		{
			Sprite sprite = LeaveTypeIcon.ReadOnlyBrush.GetLayer(LeaveType)?.Sprite;
			bool flag = sprite != null;
			LeaveTypeIcon.IsVisible = flag;
			if (flag)
			{
				LeaveTypeIcon.Brush.Sprite = sprite;
				LeaveTypeIcon.Brush.DefaultLayer.Sprite = sprite;
			}
		}
	}

	private void UpdateLeaveTypeSound()
	{
		AudioProperty audioProperty = ParentButton?.Brush.SoundProperties.GetEventAudioProperty("Click");
		if (audioProperty == null)
		{
			return;
		}
		audioProperty.AudioName = "default";
		switch (LeaveType)
		{
		case "Mission":
			if (GameMenuStringId == "menu_siege_strategies")
			{
				audioProperty.AudioName = "panels/siege/sally_out";
			}
			break;
		case "LeaveTroopsAndFlee":
			if (GameMenuStringId == "encounter" || GameMenuStringId == "encounter_interrupted_siege_preparations" || GameMenuStringId == "menu_siege_strategies")
			{
				audioProperty.AudioName = "panels/battle/retreat";
			}
			break;
		case "HostileAction":
			if (!(GameMenuStringId == "encounter"))
			{
				break;
			}
			if (!IsNavalBattle)
			{
				if (BattleSize < 50)
				{
					audioProperty.AudioName = "panels/battle/attack_small";
				}
				else if (BattleSize < 100)
				{
					audioProperty.AudioName = "panels/battle/attack_medium";
				}
				else
				{
					audioProperty.AudioName = "panels/battle/attack_large";
				}
			}
			else
			{
				audioProperty.AudioName = "panels/battle/naval_attack_large";
			}
			break;
		case "Surrender":
			if (GameMenuStringId == "encounter")
			{
				audioProperty.AudioName = "panels/battle/retreat";
			}
			break;
		case "Devastate":
		case "Pillage":
			audioProperty.AudioName = "panels/siege/raid";
			break;
		case "BesiegeTown":
			audioProperty.AudioName = "panels/siege/besiege";
			break;
		case "LeadAssault":
			audioProperty.AudioName = "panels/siege/lead_assault";
			break;
		case "VisitPort":
			audioProperty.AudioName = "panels/panel_visit_port";
			break;
		case "CallFleet":
			audioProperty.AudioName = "panels/panel_call_fleet";
			break;
		case "ManageFleet":
			audioProperty.AudioName = "panels/panel_manage_fleet";
			break;
		case "RepairShips":
			audioProperty.AudioName = "repair_all_ships";
			break;
		}
	}

	private void SetProgressIconType(int type, Widget progressWidget)
	{
		string empty = string.Empty;
		empty = type switch
		{
			0 => "Default", 
			1 => "Available", 
			2 => "Active", 
			3 => "Completed", 
			_ => "", 
		};
		if (progressWidget == QuestIconWidget)
		{
			QuestIconWidget.Brush = (IsMainStoryQuest ? MainStoryQuestBrush : NormalQuestBrush);
		}
		if (!string.IsNullOrEmpty(empty) && type != 0)
		{
			progressWidget.SetState(empty);
			progressWidget.IsVisible = true;
		}
	}

	private void ParentButton_PropertyChanged(PropertyOwnerObject widget, string propertyName, bool propertyValue)
	{
		if (propertyName == "IsDisabled" || propertyName == "IsHighlightEnabled")
		{
			OnOptionStateChanged?.Invoke();
		}
	}
}
