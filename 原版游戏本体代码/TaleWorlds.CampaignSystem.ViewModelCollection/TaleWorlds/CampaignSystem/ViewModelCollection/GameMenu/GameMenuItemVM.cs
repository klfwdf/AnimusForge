using System;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.CampaignSystem.ViewModelCollection.Quests;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu
{
	// Token: 0x0200009C RID: 156
	public class GameMenuItemVM : ViewModel
	{
		// Token: 0x170004D7 RID: 1239
		// (get) Token: 0x06000F05 RID: 3845 RVA: 0x0003E52A File Offset: 0x0003C72A
		// (set) Token: 0x06000F06 RID: 3846 RVA: 0x0003E532 File Offset: 0x0003C732
		public string OptionID { get; private set; }

		// Token: 0x170004D8 RID: 1240
		// (get) Token: 0x06000F07 RID: 3847 RVA: 0x0003E53B File Offset: 0x0003C73B
		// (set) Token: 0x06000F08 RID: 3848 RVA: 0x0003E543 File Offset: 0x0003C743
		public GameMenuOption GameMenuOption { get; private set; }

		// Token: 0x06000F09 RID: 3849 RVA: 0x0003E54C File Offset: 0x0003C74C
		public GameMenuItemVM()
		{
			this.ItemHint = new HintViewModel();
			this.Quests = new MBBindingList<QuestMarkerVM>();
		}

		// Token: 0x06000F0A RID: 3850 RVA: 0x0003E578 File Offset: 0x0003C778
		public void InitializeWith(in GameMenuItemVM.GameMenuItemCreationData data)
		{
			this.GameMenuOption = data.GameMenuOption;
			this.Index = data.Index;
			this._menuContext = data.MenuContext;
			this._itemType = (int)data.Type;
			this._tooltip = data.Tooltip;
			this._nonWaitText = data.Text;
			this._waitText = data.Text2;
			this.Item = this._nonWaitText.ToString();
			this.ItemHint.HintText = this._tooltip;
			this.OptionLeaveType = data.GameMenuOption.OptionLeaveType.ToString();
			this.OptionID = data.GameMenuOption.IdString;
			if (data.OptionQuestData != this._questFlags)
			{
				this.Quests.Clear();
				for (int i = 0; i < GameMenuOption.IssueQuestFlagsValues.Length; i++)
				{
					GameMenuOption.IssueQuestFlags issueQuestFlags = GameMenuOption.IssueQuestFlagsValues[i];
					if (issueQuestFlags != GameMenuOption.IssueQuestFlags.None && (data.OptionQuestData & issueQuestFlags) != GameMenuOption.IssueQuestFlags.None)
					{
						CampaignUIHelper.IssueQuestFlags issueQuestFlag = (CampaignUIHelper.IssueQuestFlags)issueQuestFlags;
						this.Quests.Add(new QuestMarkerVM(issueQuestFlag, null, null));
					}
				}
				this._questFlags = data.OptionQuestData;
			}
			this.ShortcutKey = ((data.ShortcutKey != null) ? InputKeyItemVM.CreateFromGameKey(data.ShortcutKey, true) : null);
			this.RefreshValues();
		}

		// Token: 0x06000F0B RID: 3851 RVA: 0x0003E6AF File Offset: 0x0003C8AF
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.Refresh();
		}

		// Token: 0x06000F0C RID: 3852 RVA: 0x0003E6BD File Offset: 0x0003C8BD
		public void ExecuteAction()
		{
			MenuContext menuContext = this._menuContext;
			if (menuContext == null)
			{
				return;
			}
			menuContext.InvokeConsequence(this.Index);
		}

		// Token: 0x06000F0D RID: 3853 RVA: 0x0003E6D5 File Offset: 0x0003C8D5
		public override void OnFinalize()
		{
			base.OnFinalize();
			if (this.ShortcutKey != null)
			{
				this.ShortcutKey.OnFinalize();
			}
		}

		// Token: 0x06000F0E RID: 3854 RVA: 0x0003E6F0 File Offset: 0x0003C8F0
		public void Refresh()
		{
			int itemType = this._itemType;
			if (itemType != 0)
			{
				int num = itemType - 1;
			}
			this.IsWaitActive = Campaign.Current.GameMenuManager.GetVirtualMenuIsWaitActive(this._menuContext);
			this.IsEnabled = Campaign.Current.GameMenuManager.GetVirtualMenuOptionIsEnabled(this._menuContext, this.Index);
			this.ItemHint.HintText = Campaign.Current.GameMenuManager.GetVirtualMenuOptionTooltip(this._menuContext, this.Index);
			this.GameMenuStringId = this._menuContext.GameMenu.StringId;
			if (PlayerEncounter.Battle != null)
			{
				this.BattleSize = PlayerEncounter.Battle.AttackerSide.TroopCount + PlayerEncounter.Battle.DefenderSide.TroopCount;
			}
			else
			{
				this.BattleSize = -1;
			}
			MapEvent battle = PlayerEncounter.Battle;
			this.IsNavalBattle = battle != null && battle.IsNavalMapEvent;
		}

		// Token: 0x06000F0F RID: 3855 RVA: 0x0003E7D4 File Offset: 0x0003C9D4
		public void UpdateWith(GameMenuItemVM newItem)
		{
			this.Item = newItem.Item;
			this.OptionLeaveType = newItem.OptionLeaveType;
			this.ItemHint = newItem.ItemHint;
			this.Quests = newItem.Quests;
			this.Index = newItem.Index;
			this.GameMenuOption = newItem.GameMenuOption;
			this.Refresh();
		}

		// Token: 0x170004D9 RID: 1241
		// (get) Token: 0x06000F10 RID: 3856 RVA: 0x0003E82F File Offset: 0x0003CA2F
		// (set) Token: 0x06000F11 RID: 3857 RVA: 0x0003E837 File Offset: 0x0003CA37
		[DataSourceProperty]
		public MBBindingList<QuestMarkerVM> Quests
		{
			get
			{
				return this._quests;
			}
			set
			{
				if (value != this._quests)
				{
					this._quests = value;
					base.OnPropertyChangedWithValue<MBBindingList<QuestMarkerVM>>(value, "Quests");
				}
			}
		}

		// Token: 0x170004DA RID: 1242
		// (get) Token: 0x06000F12 RID: 3858 RVA: 0x0003E855 File Offset: 0x0003CA55
		// (set) Token: 0x06000F13 RID: 3859 RVA: 0x0003E85D File Offset: 0x0003CA5D
		[DataSourceProperty]
		public string OptionLeaveType
		{
			get
			{
				return this._optionLeaveType;
			}
			set
			{
				if (value != this._optionLeaveType)
				{
					this._optionLeaveType = value;
					base.OnPropertyChangedWithValue<string>(value, "OptionLeaveType");
				}
			}
		}

		// Token: 0x170004DB RID: 1243
		// (get) Token: 0x06000F14 RID: 3860 RVA: 0x0003E880 File Offset: 0x0003CA80
		// (set) Token: 0x06000F15 RID: 3861 RVA: 0x0003E888 File Offset: 0x0003CA88
		[DataSourceProperty]
		public int ItemType
		{
			get
			{
				return this._itemType;
			}
			set
			{
				if (value != this._itemType)
				{
					this._itemType = value;
					base.OnPropertyChangedWithValue(value, "ItemType");
				}
			}
		}

		// Token: 0x170004DC RID: 1244
		// (get) Token: 0x06000F16 RID: 3862 RVA: 0x0003E8A6 File Offset: 0x0003CAA6
		// (set) Token: 0x06000F17 RID: 3863 RVA: 0x0003E8AE File Offset: 0x0003CAAE
		[DataSourceProperty]
		public bool IsWaitActive
		{
			get
			{
				return this._isWaitActive;
			}
			set
			{
				if (value != this._isWaitActive)
				{
					this._isWaitActive = value;
					base.OnPropertyChangedWithValue(value, "IsWaitActive");
					this.Item = (value ? this._waitText.ToString() : this._nonWaitText.ToString());
				}
			}
		}

		// Token: 0x170004DD RID: 1245
		// (get) Token: 0x06000F18 RID: 3864 RVA: 0x0003E8ED File Offset: 0x0003CAED
		// (set) Token: 0x06000F19 RID: 3865 RVA: 0x0003E8F5 File Offset: 0x0003CAF5
		[DataSourceProperty]
		public bool IsEnabled
		{
			get
			{
				return this._isEnabled;
			}
			set
			{
				if (value != this._isEnabled)
				{
					this._isEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsEnabled");
				}
			}
		}

		// Token: 0x170004DE RID: 1246
		// (get) Token: 0x06000F1A RID: 3866 RVA: 0x0003E913 File Offset: 0x0003CB13
		// (set) Token: 0x06000F1B RID: 3867 RVA: 0x0003E91B File Offset: 0x0003CB1B
		[DataSourceProperty]
		public bool IsHighlightEnabled
		{
			get
			{
				return this._isHighlightEnabled;
			}
			set
			{
				if (value != this._isHighlightEnabled)
				{
					this._isHighlightEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsHighlightEnabled");
				}
			}
		}

		// Token: 0x170004DF RID: 1247
		// (get) Token: 0x06000F1C RID: 3868 RVA: 0x0003E939 File Offset: 0x0003CB39
		// (set) Token: 0x06000F1D RID: 3869 RVA: 0x0003E941 File Offset: 0x0003CB41
		[DataSourceProperty]
		public HintViewModel ItemHint
		{
			get
			{
				return this._itemHint;
			}
			set
			{
				if (value != this._itemHint)
				{
					this._itemHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "ItemHint");
				}
			}
		}

		// Token: 0x170004E0 RID: 1248
		// (get) Token: 0x06000F1E RID: 3870 RVA: 0x0003E95F File Offset: 0x0003CB5F
		// (set) Token: 0x06000F1F RID: 3871 RVA: 0x0003E967 File Offset: 0x0003CB67
		[DataSourceProperty]
		public HintViewModel QuestHint
		{
			get
			{
				return this._questHint;
			}
			set
			{
				if (value != this._questHint)
				{
					this._questHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "QuestHint");
				}
			}
		}

		// Token: 0x170004E1 RID: 1249
		// (get) Token: 0x06000F20 RID: 3872 RVA: 0x0003E985 File Offset: 0x0003CB85
		// (set) Token: 0x06000F21 RID: 3873 RVA: 0x0003E98D File Offset: 0x0003CB8D
		[DataSourceProperty]
		public HintViewModel IssueHint
		{
			get
			{
				return this._issueHint;
			}
			set
			{
				if (value != this._issueHint)
				{
					this._issueHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "IssueHint");
				}
			}
		}

		// Token: 0x170004E2 RID: 1250
		// (get) Token: 0x06000F22 RID: 3874 RVA: 0x0003E9AB File Offset: 0x0003CBAB
		// (set) Token: 0x06000F23 RID: 3875 RVA: 0x0003E9B3 File Offset: 0x0003CBB3
		[DataSourceProperty]
		public string GameMenuStringId
		{
			get
			{
				return this._gameMenuStringId;
			}
			set
			{
				if (value != this._gameMenuStringId)
				{
					this._gameMenuStringId = value;
					base.OnPropertyChangedWithValue<string>(value, "GameMenuStringId");
				}
			}
		}

		// Token: 0x170004E3 RID: 1251
		// (get) Token: 0x06000F24 RID: 3876 RVA: 0x0003E9D6 File Offset: 0x0003CBD6
		// (set) Token: 0x06000F25 RID: 3877 RVA: 0x0003E9DE File Offset: 0x0003CBDE
		[DataSourceProperty]
		public string Item
		{
			get
			{
				return this._item;
			}
			set
			{
				if (value != this._item)
				{
					this._item = value;
					base.OnPropertyChangedWithValue<string>(value, "Item");
				}
			}
		}

		// Token: 0x170004E4 RID: 1252
		// (get) Token: 0x06000F26 RID: 3878 RVA: 0x0003EA01 File Offset: 0x0003CC01
		// (set) Token: 0x06000F27 RID: 3879 RVA: 0x0003EA09 File Offset: 0x0003CC09
		[DataSourceProperty]
		public int BattleSize
		{
			get
			{
				return this._battleSize;
			}
			set
			{
				if (value != this._battleSize)
				{
					this._battleSize = value;
					base.OnPropertyChangedWithValue(value, "BattleSize");
				}
			}
		}

		// Token: 0x170004E5 RID: 1253
		// (get) Token: 0x06000F28 RID: 3880 RVA: 0x0003EA27 File Offset: 0x0003CC27
		// (set) Token: 0x06000F29 RID: 3881 RVA: 0x0003EA2F File Offset: 0x0003CC2F
		[DataSourceProperty]
		public bool IsNavalBattle
		{
			get
			{
				return this._isNavalBattle;
			}
			set
			{
				if (value != this._isNavalBattle)
				{
					this._isNavalBattle = value;
					base.OnPropertyChangedWithValue(value, "IsNavalBattle");
				}
			}
		}

		// Token: 0x170004E6 RID: 1254
		// (get) Token: 0x06000F2A RID: 3882 RVA: 0x0003EA4D File Offset: 0x0003CC4D
		// (set) Token: 0x06000F2B RID: 3883 RVA: 0x0003EA55 File Offset: 0x0003CC55
		public InputKeyItemVM ShortcutKey
		{
			get
			{
				return this._shortcutKey;
			}
			set
			{
				if (value != this._shortcutKey)
				{
					this._shortcutKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "ShortcutKey");
				}
			}
		}

		// Token: 0x040006C9 RID: 1737
		private MenuContext _menuContext;

		// Token: 0x040006CA RID: 1738
		public int Index;

		// Token: 0x040006CB RID: 1739
		private TextObject _nonWaitText;

		// Token: 0x040006CC RID: 1740
		private TextObject _waitText;

		// Token: 0x040006CD RID: 1741
		private TextObject _tooltip;

		// Token: 0x040006CF RID: 1743
		private GameMenuOption.IssueQuestFlags _questFlags;

		// Token: 0x040006D0 RID: 1744
		private MBBindingList<QuestMarkerVM> _quests;

		// Token: 0x040006D1 RID: 1745
		private int _itemType = -1;

		// Token: 0x040006D2 RID: 1746
		private bool _isWaitActive;

		// Token: 0x040006D3 RID: 1747
		private bool _isEnabled;

		// Token: 0x040006D4 RID: 1748
		private HintViewModel _itemHint;

		// Token: 0x040006D5 RID: 1749
		private HintViewModel _questHint;

		// Token: 0x040006D6 RID: 1750
		private HintViewModel _issueHint;

		// Token: 0x040006D7 RID: 1751
		private bool _isHighlightEnabled;

		// Token: 0x040006D8 RID: 1752
		private string _optionLeaveType;

		// Token: 0x040006D9 RID: 1753
		private string _gameMenuStringId;

		// Token: 0x040006DA RID: 1754
		private string _item;

		// Token: 0x040006DB RID: 1755
		private int _battleSize = -1;

		// Token: 0x040006DC RID: 1756
		private bool _isNavalBattle;

		// Token: 0x040006DD RID: 1757
		private InputKeyItemVM _shortcutKey;

		// Token: 0x0200020F RID: 527
		public readonly struct GameMenuItemCreationData
		{
			// Token: 0x17000B9C RID: 2972
			// (get) Token: 0x06002434 RID: 9268 RVA: 0x0007F618 File Offset: 0x0007D818
			public string OptionID
			{
				get
				{
					return this.GameMenuOption.IdString;
				}
			}

			// Token: 0x06002435 RID: 9269 RVA: 0x0007F628 File Offset: 0x0007D828
			public GameMenuItemCreationData(MenuContext menuContext, int index, TextObject text, TextObject text2, TextObject tooltip, GameMenu.MenuAndOptionType type, GameMenuOption.IssueQuestFlags questFlags, GameMenuOption gameMenuOption, GameKey shortcutKey)
			{
				this.OptionQuestData = questFlags;
				this.MenuContext = menuContext;
				this.Index = index;
				this.Text = text;
				this.Text2 = text2;
				this.Tooltip = tooltip;
				this.Type = type;
				this.GameMenuOption = gameMenuOption;
				this.ShortcutKey = shortcutKey;
			}

			// Token: 0x0400119D RID: 4509
			public readonly MenuContext MenuContext;

			// Token: 0x0400119E RID: 4510
			public readonly int Index;

			// Token: 0x0400119F RID: 4511
			public readonly TextObject Text;

			// Token: 0x040011A0 RID: 4512
			public readonly TextObject Text2;

			// Token: 0x040011A1 RID: 4513
			public readonly TextObject Tooltip;

			// Token: 0x040011A2 RID: 4514
			public readonly GameMenu.MenuAndOptionType Type;

			// Token: 0x040011A3 RID: 4515
			public readonly GameMenuOption.IssueQuestFlags OptionQuestData;

			// Token: 0x040011A4 RID: 4516
			public readonly GameMenuOption GameMenuOption;

			// Token: 0x040011A5 RID: 4517
			public readonly GameKey ShortcutKey;
		}
	}
}
