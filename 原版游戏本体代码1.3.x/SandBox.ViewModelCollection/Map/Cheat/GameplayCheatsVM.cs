using System;
using System.Collections.Generic;
using SandBox.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace SandBox.ViewModelCollection.Map.Cheat
{
	// Token: 0x02000051 RID: 81
	public class GameplayCheatsVM : ViewModel
	{
		// Token: 0x060004E3 RID: 1251 RVA: 0x00012A98 File Offset: 0x00010C98
		public GameplayCheatsVM(Action onClose, IEnumerable<GameplayCheatBase> cheats)
		{
			this._onClose = onClose;
			this._initialCheatList = cheats;
			this.Cheats = new MBBindingList<CheatItemBaseVM>();
			this._activeCheatGroups = new List<CheatGroupItemVM>();
			this._mainTitleText = new TextObject("{=OYtysXzk}Cheats", null);
			this.FillWithCheats(cheats);
			this.RefreshValues();
		}

		// Token: 0x060004E4 RID: 1252 RVA: 0x00012AF0 File Offset: 0x00010CF0
		public override void RefreshValues()
		{
			base.RefreshValues();
			for (int i = 0; i < this.Cheats.Count; i++)
			{
				this.Cheats[i].RefreshValues();
			}
			if (this._activeCheatGroups.Count > 0)
			{
				TextObject textObject = new TextObject("{=1tiF5JhE}{TITLE} > {SUBTITLE}", null);
				for (int j = 0; j < this._activeCheatGroups.Count; j++)
				{
					if (j == 0)
					{
						textObject.SetTextVariable("TITLE", this._mainTitleText.ToString());
					}
					else
					{
						textObject.SetTextVariable("TITLE", textObject.ToString());
					}
					textObject.SetTextVariable("SUBTITLE", this._activeCheatGroups[j].Name.ToString());
				}
				this.Title = textObject.ToString();
				this.ButtonCloseLabel = GameTexts.FindText("str_back", null).ToString();
				return;
			}
			this.Title = this._mainTitleText.ToString();
			this.ButtonCloseLabel = GameTexts.FindText("str_close", null).ToString();
		}

		// Token: 0x060004E5 RID: 1253 RVA: 0x00012BF7 File Offset: 0x00010DF7
		public override void OnFinalize()
		{
			base.OnFinalize();
			InputKeyItemVM closeInputKey = this.CloseInputKey;
			if (closeInputKey == null)
			{
				return;
			}
			closeInputKey.OnFinalize();
		}

		// Token: 0x060004E6 RID: 1254 RVA: 0x00012C10 File Offset: 0x00010E10
		private void FillWithCheats(IEnumerable<GameplayCheatBase> cheats)
		{
			this.Cheats.Clear();
			foreach (GameplayCheatBase gameplayCheatBase in cheats)
			{
				GameplayCheatItem cheat;
				GameplayCheatGroup cheatGroup;
				if ((cheat = gameplayCheatBase as GameplayCheatItem) != null)
				{
					this.Cheats.Add(new CheatActionItemVM(cheat, new Action<CheatActionItemVM>(this.OnCheatActionExecuted)));
				}
				else if ((cheatGroup = gameplayCheatBase as GameplayCheatGroup) != null)
				{
					this.Cheats.Add(new CheatGroupItemVM(cheatGroup, new Action<CheatGroupItemVM>(this.OnCheatGroupSelected)));
				}
			}
			this.RefreshValues();
		}

		// Token: 0x060004E7 RID: 1255 RVA: 0x00012CB4 File Offset: 0x00010EB4
		private void OnCheatActionExecuted(CheatActionItemVM cheatItem)
		{
			this._activeCheatGroups.Clear();
			this.FillWithCheats(this._initialCheatList);
			TextObject textObject = new TextObject("{=1QAEyN2V}Cheat Used: {CHEAT}", null);
			textObject.SetTextVariable("CHEAT", cheatItem.Name.ToString());
			InformationManager.DisplayMessage(new InformationMessage(textObject.ToString()));
		}

		// Token: 0x060004E8 RID: 1256 RVA: 0x00012D09 File Offset: 0x00010F09
		private void OnCheatGroupSelected(CheatGroupItemVM cheatGroup)
		{
			this._activeCheatGroups.Add(cheatGroup);
			IEnumerable<GameplayCheatBase> enumerable;
			if (cheatGroup == null)
			{
				enumerable = null;
			}
			else
			{
				GameplayCheatGroup cheatGroup2 = cheatGroup.CheatGroup;
				enumerable = ((cheatGroup2 != null) ? cheatGroup2.GetCheats() : null);
			}
			this.FillWithCheats(enumerable ?? this._initialCheatList);
		}

		// Token: 0x060004E9 RID: 1257 RVA: 0x00012D40 File Offset: 0x00010F40
		public void ExecuteClose()
		{
			if (this._activeCheatGroups.Count > 0)
			{
				this._activeCheatGroups.RemoveAt(this._activeCheatGroups.Count - 1);
				if (this._activeCheatGroups.Count > 0)
				{
					this.FillWithCheats(this._activeCheatGroups[this._activeCheatGroups.Count - 1].CheatGroup.GetCheats());
					return;
				}
				this.FillWithCheats(this._initialCheatList);
				return;
			}
			else
			{
				Action onClose = this._onClose;
				if (onClose == null)
				{
					return;
				}
				onClose();
				return;
			}
		}

		// Token: 0x1700016D RID: 365
		// (get) Token: 0x060004EA RID: 1258 RVA: 0x00012DC7 File Offset: 0x00010FC7
		// (set) Token: 0x060004EB RID: 1259 RVA: 0x00012DCF File Offset: 0x00010FCF
		[DataSourceProperty]
		public string Title
		{
			get
			{
				return this._title;
			}
			set
			{
				if (value != this._title)
				{
					this._title = value;
					base.OnPropertyChangedWithValue<string>(value, "Title");
				}
			}
		}

		// Token: 0x1700016E RID: 366
		// (get) Token: 0x060004EC RID: 1260 RVA: 0x00012DF2 File Offset: 0x00010FF2
		// (set) Token: 0x060004ED RID: 1261 RVA: 0x00012DFA File Offset: 0x00010FFA
		[DataSourceProperty]
		public string ButtonCloseLabel
		{
			get
			{
				return this._buttonCloseLabel;
			}
			set
			{
				if (value != this._buttonCloseLabel)
				{
					this._buttonCloseLabel = value;
					base.OnPropertyChangedWithValue<string>(value, "ButtonCloseLabel");
				}
			}
		}

		// Token: 0x1700016F RID: 367
		// (get) Token: 0x060004EE RID: 1262 RVA: 0x00012E1D File Offset: 0x0001101D
		// (set) Token: 0x060004EF RID: 1263 RVA: 0x00012E25 File Offset: 0x00011025
		[DataSourceProperty]
		public MBBindingList<CheatItemBaseVM> Cheats
		{
			get
			{
				return this._cheats;
			}
			set
			{
				if (value != this._cheats)
				{
					this._cheats = value;
					base.OnPropertyChangedWithValue<MBBindingList<CheatItemBaseVM>>(value, "Cheats");
				}
			}
		}

		// Token: 0x060004F0 RID: 1264 RVA: 0x00012E43 File Offset: 0x00011043
		public void SetCloseInputKey(HotKey hotKey)
		{
			this.CloseInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
		}

		// Token: 0x17000170 RID: 368
		// (get) Token: 0x060004F1 RID: 1265 RVA: 0x00012E52 File Offset: 0x00011052
		// (set) Token: 0x060004F2 RID: 1266 RVA: 0x00012E5A File Offset: 0x0001105A
		[DataSourceProperty]
		public InputKeyItemVM CloseInputKey
		{
			get
			{
				return this._closeInputKey;
			}
			set
			{
				if (value != this._closeInputKey)
				{
					this._closeInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "CloseInputKey");
				}
			}
		}

		// Token: 0x04000269 RID: 617
		private readonly Action _onClose;

		// Token: 0x0400026A RID: 618
		private readonly IEnumerable<GameplayCheatBase> _initialCheatList;

		// Token: 0x0400026B RID: 619
		private readonly TextObject _mainTitleText;

		// Token: 0x0400026C RID: 620
		private List<CheatGroupItemVM> _activeCheatGroups;

		// Token: 0x0400026D RID: 621
		private string _title;

		// Token: 0x0400026E RID: 622
		private string _buttonCloseLabel;

		// Token: 0x0400026F RID: 623
		private MBBindingList<CheatItemBaseVM> _cheats;

		// Token: 0x04000270 RID: 624
		private InputKeyItemVM _closeInputKey;
	}
}
