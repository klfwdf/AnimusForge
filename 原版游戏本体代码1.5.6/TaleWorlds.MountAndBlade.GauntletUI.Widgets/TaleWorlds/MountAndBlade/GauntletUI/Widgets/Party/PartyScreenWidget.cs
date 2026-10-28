using System.Collections.Generic;
using System.Linq;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.ExtraWidgets;
using TaleWorlds.GauntletUI.GamepadNavigation;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Party;

public class PartyScreenWidget : Widget
{
	private float _scrollToCharacterInSeconds = -1f;

	private Widget _latestMouseDownWidget;

	private InputKeyVisualWidget _takeAllPrisonersInputKeyVisual;

	private InputKeyVisualWidget _dismissAllPrisonersInputKeyVisual;

	private Widget _upgradePopupParent;

	private Widget _recruitPopupParent;

	private Widget _takeAllPrisonersInputKeyVisualParent;

	private Widget _dismissAllPrisonersInputKeyVisualParent;

	private int _mainPartyTroopSize;

	private bool _isPrisonerWarningEnabled;

	private bool _isTroopWarningEnabled;

	private bool _isOtherTroopWarningEnabled;

	private TextWidget _troopLabel;

	private TextWidget _prisonerLabel;

	private TextWidget _otherTroopLabel;

	private ListPanel _otherMemberList;

	private ListPanel _otherPrisonerList;

	private ListPanel _mainMemberList;

	private ListPanel _mainPrisonerList;

	private bool _scrollToCharacter;

	private bool _isScrollTargetPrisoner;

	private string _scrollCharacterId;

	public ScrollablePanel MainScrollPanel { get; set; }

	public ScrollablePanel OtherScrollPanel { get; set; }

	public InputKeyVisualWidget TransferInputKeyVisual { get; set; }

	public Widget UpgradePopupParent
	{
		get
		{
			return _upgradePopupParent;
		}
		set
		{
			if (value != _upgradePopupParent)
			{
				_upgradePopupParent = value;
			}
		}
	}

	public Widget RecruitPopupParent
	{
		get
		{
			return _recruitPopupParent;
		}
		set
		{
			if (value != _recruitPopupParent)
			{
				_recruitPopupParent = value;
			}
		}
	}

	public Widget TakeAllPrisonersInputKeyVisualParent
	{
		get
		{
			return _takeAllPrisonersInputKeyVisualParent;
		}
		set
		{
			if (value == _takeAllPrisonersInputKeyVisualParent)
			{
				return;
			}
			_takeAllPrisonersInputKeyVisualParent = value;
			if (_takeAllPrisonersInputKeyVisualParent != null)
			{
				_takeAllPrisonersInputKeyVisual = _takeAllPrisonersInputKeyVisualParent.Children.FirstOrDefault((Widget x) => x is InputKeyVisualWidget) as InputKeyVisualWidget;
			}
		}
	}

	public Widget DismissAllPrisonersInputKeyVisualParent
	{
		get
		{
			return _dismissAllPrisonersInputKeyVisualParent;
		}
		set
		{
			if (value == _dismissAllPrisonersInputKeyVisualParent)
			{
				return;
			}
			_dismissAllPrisonersInputKeyVisualParent = value;
			if (_dismissAllPrisonersInputKeyVisualParent != null)
			{
				_dismissAllPrisonersInputKeyVisual = _dismissAllPrisonersInputKeyVisualParent.Children.FirstOrDefault((Widget x) => x is InputKeyVisualWidget) as InputKeyVisualWidget;
			}
		}
	}

	[Editor(false)]
	public int MainPartyTroopSize
	{
		get
		{
			return _mainPartyTroopSize;
		}
		set
		{
			if (_mainPartyTroopSize != value)
			{
				_mainPartyTroopSize = value;
				OnPropertyChanged(value, "MainPartyTroopSize");
			}
		}
	}

	[Editor(false)]
	public bool IsPrisonerWarningEnabled
	{
		get
		{
			return _isPrisonerWarningEnabled;
		}
		set
		{
			if (_isPrisonerWarningEnabled != value)
			{
				_isPrisonerWarningEnabled = value;
				OnPropertyChanged(value, "IsPrisonerWarningEnabled");
				RefreshWarningStatuses();
			}
		}
	}

	[Editor(false)]
	public bool IsOtherTroopWarningEnabled
	{
		get
		{
			return _isOtherTroopWarningEnabled;
		}
		set
		{
			if (_isOtherTroopWarningEnabled != value)
			{
				_isOtherTroopWarningEnabled = value;
				OnPropertyChanged(value, "IsOtherTroopWarningEnabled");
				RefreshWarningStatuses();
			}
		}
	}

	[Editor(false)]
	public bool IsTroopWarningEnabled
	{
		get
		{
			return _isTroopWarningEnabled;
		}
		set
		{
			if (_isTroopWarningEnabled != value)
			{
				_isTroopWarningEnabled = value;
				OnPropertyChanged(value, "IsTroopWarningEnabled");
				RefreshWarningStatuses();
			}
		}
	}

	[Editor(false)]
	public TextWidget TroopLabel
	{
		get
		{
			return _troopLabel;
		}
		set
		{
			if (_troopLabel != value)
			{
				_troopLabel = value;
				OnPropertyChanged(value, "TroopLabel");
				RefreshWarningStatuses();
			}
		}
	}

	[Editor(false)]
	public TextWidget PrisonerLabel
	{
		get
		{
			return _prisonerLabel;
		}
		set
		{
			if (_prisonerLabel != value)
			{
				_prisonerLabel = value;
				OnPropertyChanged(value, "PrisonerLabel");
				RefreshWarningStatuses();
			}
		}
	}

	[Editor(false)]
	public TextWidget OtherTroopLabel
	{
		get
		{
			return _otherTroopLabel;
		}
		set
		{
			if (_otherTroopLabel != value)
			{
				_otherTroopLabel = value;
				OnPropertyChanged(value, "OtherTroopLabel");
				RefreshWarningStatuses();
			}
		}
	}

	[Editor(false)]
	public ListPanel OtherMemberList
	{
		get
		{
			return _otherMemberList;
		}
		set
		{
			if (_otherMemberList != value)
			{
				_otherMemberList = value;
			}
		}
	}

	[Editor(false)]
	public ListPanel OtherPrisonerList
	{
		get
		{
			return _otherPrisonerList;
		}
		set
		{
			if (_otherPrisonerList != value)
			{
				_otherPrisonerList = value;
			}
		}
	}

	[Editor(false)]
	public ListPanel MainMemberList
	{
		get
		{
			return _mainMemberList;
		}
		set
		{
			if (_mainMemberList != value)
			{
				_mainMemberList = value;
			}
		}
	}

	[Editor(false)]
	public ListPanel MainPrisonerList
	{
		get
		{
			return _mainPrisonerList;
		}
		set
		{
			if (_mainPrisonerList != value)
			{
				_mainPrisonerList = value;
			}
		}
	}

	[Editor(false)]
	public bool ScrollToCharacter
	{
		get
		{
			return _scrollToCharacter;
		}
		set
		{
			if (value != _scrollToCharacter)
			{
				_scrollToCharacter = value;
				OnPropertyChanged(value, "ScrollToCharacter");
			}
		}
	}

	[Editor(false)]
	public string ScrollCharacterId
	{
		get
		{
			return _scrollCharacterId;
		}
		set
		{
			if (value != _scrollCharacterId)
			{
				_scrollCharacterId = value;
				OnPropertyChanged(value, "ScrollCharacterId");
			}
		}
	}

	[Editor(false)]
	public bool IsScrollTargetPrisoner
	{
		get
		{
			return _isScrollTargetPrisoner;
		}
		set
		{
			if (value != _isScrollTargetPrisoner)
			{
				_isScrollTargetPrisoner = value;
				OnPropertyChanged(value, "IsScrollTargetPrisoner");
			}
		}
	}

	public PartyScreenWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnConnectedToRoot()
	{
		base.Context.EventManager.OnDragStarted += OnDragStarted;
	}

	protected override void OnDisconnectedFromRoot()
	{
		base.Context.EventManager.OnDragStarted -= OnDragStarted;
	}

	private Widget GetItemWithId(ListPanel charactersListPanel, string id)
	{
		return charactersListPanel?.FindChild((Widget x) => (x as PartyTroopTupleButtonWidget).CharacterID == id);
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (_latestMouseDownWidget != base.EventManager.LatestMouseDownWidget)
		{
			_latestMouseDownWidget = base.EventManager.LatestMouseDownWidget;
			bool flag = _latestMouseDownWidget != null && (_latestMouseDownWidget is PartyTroopTupleButtonWidget || _latestMouseDownWidget.GetAllParents().Any((Widget x) => x is PartyTroopTupleButtonWidget));
			if (_latestMouseDownWidget == null || (!flag && IsWidgetChildOfType<PartyFormationDropdownWidget>(_latestMouseDownWidget) == null))
			{
				EventFired("OnEmptyClick");
			}
		}
		UpdateInputKeyVisualsVisibility();
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (ScrollToCharacter)
		{
			_scrollToCharacterInSeconds = 0.2f;
			ScrollToCharacter = false;
		}
		if (!(_scrollToCharacterInSeconds >= 0f))
		{
			return;
		}
		_scrollToCharacterInSeconds -= dt;
		if (_scrollToCharacterInSeconds <= 0f)
		{
			ScrollablePanel.AutoScrollParameters scrollParameters = new ScrollablePanel.AutoScrollParameters(100f, 100f, 0f, 0f, -1f, -1f, 0.35f);
			if (IsScrollTargetPrisoner)
			{
				OtherPrisonerList.FindParentPanel()?.ScrollToChild(GetItemWithId(OtherPrisonerList, ScrollCharacterId), scrollParameters);
				MainPrisonerList.FindParentPanel()?.ScrollToChild(GetItemWithId(MainPrisonerList, ScrollCharacterId), scrollParameters);
			}
			else
			{
				OtherMemberList.FindParentPanel()?.ScrollToChild(GetItemWithId(OtherMemberList, ScrollCharacterId), scrollParameters);
				MainMemberList.FindParentPanel()?.ScrollToChild(GetItemWithId(MainMemberList, ScrollCharacterId), scrollParameters);
			}
		}
	}

	private void UpdateInputKeyVisualsVisibility()
	{
		if (base.EventManager.IsControllerActive)
		{
			bool flag = false;
			if (base.EventManager.HoveredWidget is PartyTroopTupleButtonWidget partyTroopTupleButtonWidget)
			{
				TransferInputKeyVisual.IsVisible = partyTroopTupleButtonWidget.IsTransferable;
				flag = true;
				if (partyTroopTupleButtonWidget.IsTupleLeftSide)
				{
					TransferInputKeyVisual.KeyID = _takeAllPrisonersInputKeyVisual.KeyID;
					TransferInputKeyVisual.ScaledPositionXOffset = partyTroopTupleButtonWidget.GlobalPosition.X + partyTroopTupleButtonWidget.Size.X - 65f * base._scaleToUse;
					TransferInputKeyVisual.ScaledPositionYOffset = partyTroopTupleButtonWidget.GlobalPosition.Y - 13f * base._scaleToUse;
				}
				else
				{
					TransferInputKeyVisual.KeyID = _dismissAllPrisonersInputKeyVisual.KeyID;
					TransferInputKeyVisual.ScaledPositionXOffset = partyTroopTupleButtonWidget.GlobalPosition.X + 5f * base._scaleToUse;
					TransferInputKeyVisual.ScaledPositionYOffset = partyTroopTupleButtonWidget.GlobalPosition.Y - 13f * base._scaleToUse;
				}
			}
			else
			{
				TransferInputKeyVisual.IsVisible = false;
				TransferInputKeyVisual.KeyID = "";
			}
			bool isVisible = !IsAnyPopupOpen() && !flag && !MainScrollPanel.InnerPanel.IsHovered && !OtherScrollPanel.InnerPanel.IsHovered && !GauntletGamepadNavigationManager.Instance.IsCursorMovingForNavigation;
			TakeAllPrisonersInputKeyVisualParent.IsVisible = isVisible;
			DismissAllPrisonersInputKeyVisualParent.IsVisible = isVisible;
		}
		else
		{
			TransferInputKeyVisual.IsVisible = false;
			TakeAllPrisonersInputKeyVisualParent.IsVisible = true;
			DismissAllPrisonersInputKeyVisualParent.IsVisible = true;
		}
	}

	private void RefreshWarningStatuses()
	{
		PrisonerLabel?.SetState(IsPrisonerWarningEnabled ? "OverLimit" : "Default");
		TroopLabel?.SetState(IsTroopWarningEnabled ? "OverLimit" : "Default");
		OtherTroopLabel?.SetState(IsOtherTroopWarningEnabled ? "OverLimit" : "Default");
	}

	private PartyTroopTupleButtonWidget FindTupleWithTroopIDInList(string troopID, bool searchMainList, bool isPrisoner)
	{
		IEnumerable<PartyTroopTupleButtonWidget> enumerable = null;
		enumerable = ((!searchMainList) ? (isPrisoner ? OtherPrisonerList.Children.Cast<PartyTroopTupleButtonWidget>() : OtherMemberList.Children.Cast<PartyTroopTupleButtonWidget>()) : (isPrisoner ? MainPrisonerList.Children.Cast<PartyTroopTupleButtonWidget>() : MainMemberList.Children.Cast<PartyTroopTupleButtonWidget>()));
		return enumerable.SingleOrDefault((PartyTroopTupleButtonWidget i) => i.CharacterID == troopID);
	}

	private void OnDragStarted()
	{
		EventFired("OnEmptyClick");
		RemoveZeroCountItems();
	}

	private void RemoveZeroCountItems()
	{
		EventFired("RemoveZeroCounts");
	}

	private bool IsAnyPopupOpen()
	{
		Widget recruitPopupParent = RecruitPopupParent;
		if (recruitPopupParent == null || !recruitPopupParent.IsVisible)
		{
			return UpgradePopupParent?.IsVisible ?? false;
		}
		return true;
	}

	private T IsWidgetChildOfType<T>(Widget currentWidget) where T : Widget
	{
		while (currentWidget != null)
		{
			if (currentWidget is T result)
			{
				return result;
			}
			currentWidget = currentWidget.ParentWidget;
		}
		return null;
	}
}
