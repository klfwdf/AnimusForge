using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.CampaignSystem.ViewModelCollection.Quests;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu;

public class GameMenuItemVM : ViewModel
{
	public readonly struct GameMenuItemCreationData
	{
		public readonly MenuContext MenuContext;

		public readonly int Index;

		public readonly TextObject Text;

		public readonly TextObject Text2;

		public readonly TextObject Tooltip;

		public readonly TaleWorlds.CampaignSystem.GameMenus.GameMenu.MenuAndOptionType Type;

		public readonly GameMenuOption.IssueQuestFlags OptionQuestData;

		public readonly GameMenuOption GameMenuOption;

		public readonly GameKey ShortcutKey;

		public string OptionID => GameMenuOption.IdString;

		public GameMenuItemCreationData(MenuContext menuContext, int index, TextObject text, TextObject text2, TextObject tooltip, TaleWorlds.CampaignSystem.GameMenus.GameMenu.MenuAndOptionType type, GameMenuOption.IssueQuestFlags questFlags, GameMenuOption gameMenuOption, GameKey shortcutKey)
		{
			OptionQuestData = questFlags;
			MenuContext = menuContext;
			Index = index;
			Text = text;
			Text2 = text2;
			Tooltip = tooltip;
			Type = type;
			GameMenuOption = gameMenuOption;
			ShortcutKey = shortcutKey;
		}
	}

	private MenuContext _menuContext;

	public int Index;

	private TextObject _nonWaitText;

	private TextObject _waitText;

	private TextObject _tooltip;

	private GameMenuOption.IssueQuestFlags _questFlags;

	private MBBindingList<QuestMarkerVM> _quests;

	private int _itemType = -1;

	private bool _isWaitActive;

	private bool _isEnabled;

	private HintViewModel _itemHint;

	private HintViewModel _questHint;

	private HintViewModel _issueHint;

	private bool _isHighlightEnabled;

	private string _optionLeaveType;

	private string _gameMenuStringId;

	private string _item;

	private int _battleSize = -1;

	private bool _isNavalBattle;

	private InputKeyItemVM _shortcutKey;

	public string OptionID { get; private set; }

	public GameMenuOption GameMenuOption { get; private set; }

	[DataSourceProperty]
	public MBBindingList<QuestMarkerVM> Quests
	{
		get
		{
			return _quests;
		}
		set
		{
			if (value != _quests)
			{
				_quests = value;
				OnPropertyChangedWithValue(value, "Quests");
			}
		}
	}

	[DataSourceProperty]
	public string OptionLeaveType
	{
		get
		{
			return _optionLeaveType;
		}
		set
		{
			if (value != _optionLeaveType)
			{
				_optionLeaveType = value;
				OnPropertyChangedWithValue(value, "OptionLeaveType");
			}
		}
	}

	[DataSourceProperty]
	public int ItemType
	{
		get
		{
			return _itemType;
		}
		set
		{
			if (value != _itemType)
			{
				_itemType = value;
				OnPropertyChangedWithValue(value, "ItemType");
			}
		}
	}

	[DataSourceProperty]
	public bool IsWaitActive
	{
		get
		{
			return _isWaitActive;
		}
		set
		{
			if (value != _isWaitActive)
			{
				_isWaitActive = value;
				OnPropertyChangedWithValue(value, "IsWaitActive");
				Item = (value ? _waitText.ToString() : _nonWaitText.ToString());
			}
		}
	}

	[DataSourceProperty]
	public bool IsEnabled
	{
		get
		{
			return _isEnabled;
		}
		set
		{
			if (value != _isEnabled)
			{
				_isEnabled = value;
				OnPropertyChangedWithValue(value, "IsEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsHighlightEnabled
	{
		get
		{
			return _isHighlightEnabled;
		}
		set
		{
			if (value != _isHighlightEnabled)
			{
				_isHighlightEnabled = value;
				OnPropertyChangedWithValue(value, "IsHighlightEnabled");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel ItemHint
	{
		get
		{
			return _itemHint;
		}
		set
		{
			if (value != _itemHint)
			{
				_itemHint = value;
				OnPropertyChangedWithValue(value, "ItemHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel QuestHint
	{
		get
		{
			return _questHint;
		}
		set
		{
			if (value != _questHint)
			{
				_questHint = value;
				OnPropertyChangedWithValue(value, "QuestHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel IssueHint
	{
		get
		{
			return _issueHint;
		}
		set
		{
			if (value != _issueHint)
			{
				_issueHint = value;
				OnPropertyChangedWithValue(value, "IssueHint");
			}
		}
	}

	[DataSourceProperty]
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
				OnPropertyChangedWithValue(value, "GameMenuStringId");
			}
		}
	}

	[DataSourceProperty]
	public string Item
	{
		get
		{
			return _item;
		}
		set
		{
			if (value != _item)
			{
				_item = value;
				OnPropertyChangedWithValue(value, "Item");
			}
		}
	}

	[DataSourceProperty]
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
				OnPropertyChangedWithValue(value, "BattleSize");
			}
		}
	}

	[DataSourceProperty]
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
				OnPropertyChangedWithValue(value, "IsNavalBattle");
			}
		}
	}

	public InputKeyItemVM ShortcutKey
	{
		get
		{
			return _shortcutKey;
		}
		set
		{
			if (value != _shortcutKey)
			{
				_shortcutKey = value;
				OnPropertyChangedWithValue(value, "ShortcutKey");
			}
		}
	}

	public GameMenuItemVM()
	{
		ItemHint = new HintViewModel();
		Quests = new MBBindingList<QuestMarkerVM>();
	}

	public void InitializeWith(in GameMenuItemCreationData data)
	{
		GameMenuOption = data.GameMenuOption;
		Index = data.Index;
		_menuContext = data.MenuContext;
		_itemType = (int)data.Type;
		_tooltip = data.Tooltip;
		_nonWaitText = data.Text;
		_waitText = data.Text2;
		Item = _nonWaitText.ToString();
		ItemHint.HintText = _tooltip;
		OptionLeaveType = data.GameMenuOption.OptionLeaveType.ToString();
		OptionID = data.GameMenuOption.IdString;
		if (data.OptionQuestData != _questFlags)
		{
			Quests.Clear();
			for (int i = 0; i < GameMenuOption.IssueQuestFlagsValues.Length; i++)
			{
				GameMenuOption.IssueQuestFlags issueQuestFlags = GameMenuOption.IssueQuestFlagsValues[i];
				if (issueQuestFlags != GameMenuOption.IssueQuestFlags.None && (data.OptionQuestData & issueQuestFlags) != GameMenuOption.IssueQuestFlags.None)
				{
					CampaignUIHelper.IssueQuestFlags issueQuestFlag = (CampaignUIHelper.IssueQuestFlags)issueQuestFlags;
					Quests.Add(new QuestMarkerVM(issueQuestFlag));
				}
			}
			_questFlags = data.OptionQuestData;
		}
		ShortcutKey = ((data.ShortcutKey != null) ? InputKeyItemVM.CreateFromGameKey(data.ShortcutKey, isConsoleOnly: true) : null);
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Refresh();
	}

	public void ExecuteAction()
	{
		_menuContext?.InvokeConsequence(Index);
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		if (ShortcutKey != null)
		{
			ShortcutKey.OnFinalize();
		}
	}

	public void Refresh()
	{
		int itemType = _itemType;
		if (itemType != 0)
		{
			_ = itemType - 1;
			_ = 2;
		}
		IsWaitActive = Campaign.Current.GameMenuManager.GetVirtualMenuIsWaitActive(_menuContext);
		IsEnabled = Campaign.Current.GameMenuManager.GetVirtualMenuOptionIsEnabled(_menuContext, Index);
		ItemHint.HintText = Campaign.Current.GameMenuManager.GetVirtualMenuOptionTooltip(_menuContext, Index);
		GameMenuStringId = _menuContext.GameMenu.StringId;
		if (PlayerEncounter.Battle != null)
		{
			BattleSize = PlayerEncounter.Battle.AttackerSide.TroopCount + PlayerEncounter.Battle.DefenderSide.TroopCount;
		}
		else
		{
			BattleSize = -1;
		}
		IsNavalBattle = PlayerEncounter.Battle?.IsNavalMapEvent ?? false;
	}

	public void UpdateWith(GameMenuItemVM newItem)
	{
		Item = newItem.Item;
		OptionLeaveType = newItem.OptionLeaveType;
		ItemHint = newItem.ItemHint;
		Quests = newItem.Quests;
		Index = newItem.Index;
		GameMenuOption = newItem.GameMenuOption;
		Refresh();
	}
}
