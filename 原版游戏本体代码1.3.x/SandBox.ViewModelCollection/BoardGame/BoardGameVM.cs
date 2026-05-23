using System;
using SandBox.BoardGames;
using SandBox.BoardGames.MissionLogics;
using SandBox.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.ViewModelCollection.BoardGame
{
	// Token: 0x02000060 RID: 96
	public class BoardGameVM : ViewModel
	{
		// Token: 0x060005C6 RID: 1478 RVA: 0x00015434 File Offset: 0x00013634
		public BoardGameVM()
		{
			this._missionBoardGameHandler = Mission.Current.GetMissionBehavior<MissionBoardGameLogic>();
			this.BoardGameType = this._missionBoardGameHandler.CurrentBoardGame.ToString();
			this.IsGameUsingDice = this._missionBoardGameHandler.RequiresDiceRolling();
			this.DiceResult = "-";
			this.Instructions = new BoardGameInstructionsVM(this._missionBoardGameHandler.CurrentBoardGame);
			this.RefreshValues();
		}

		// Token: 0x060005C7 RID: 1479 RVA: 0x000154B0 File Offset: 0x000136B0
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.RollDiceText = GameTexts.FindText("str_roll_dice", null).ToString();
			this.CloseText = GameTexts.FindText("str_close", null).ToString();
			this.ForfeitText = GameTexts.FindText("str_forfeit", null).ToString();
		}

		// Token: 0x060005C8 RID: 1480 RVA: 0x00015505 File Offset: 0x00013705
		public void Activate()
		{
			this.SwitchTurns();
		}

		// Token: 0x060005C9 RID: 1481 RVA: 0x0001550D File Offset: 0x0001370D
		public void DiceRoll(int roll)
		{
			this.DiceResult = roll.ToString();
		}

		// Token: 0x060005CA RID: 1482 RVA: 0x0001551C File Offset: 0x0001371C
		public void SwitchTurns()
		{
			this.IsPlayersTurn = this._missionBoardGameHandler.Board.PlayerTurn == PlayerTurn.PlayerOne || this._missionBoardGameHandler.Board.PlayerTurn == PlayerTurn.PlayerOneWaiting;
			this.TurnOwnerText = (this.IsPlayersTurn ? GameTexts.FindText("str_your_turn", null).ToString() : GameTexts.FindText("str_opponents_turn", null).ToString());
			this.DiceResult = "-";
			this.CanRoll = this.IsPlayersTurn && this.IsGameUsingDice;
		}

		// Token: 0x060005CB RID: 1483 RVA: 0x000155A9 File Offset: 0x000137A9
		public void ExecuteRoll()
		{
			if (this.CanRoll)
			{
				this._missionBoardGameHandler.RollDice();
				this.CanRoll = false;
			}
		}

		// Token: 0x060005CC RID: 1484 RVA: 0x000155C8 File Offset: 0x000137C8
		public void ExecuteForfeit()
		{
			if (this._missionBoardGameHandler.Board.IsReady && this._missionBoardGameHandler.IsGameInProgress)
			{
				TextObject textObject = new TextObject("{=azJulvrp}{?IS_BETTING}You are going to lose {BET_AMOUNT}{GOLD_ICON} if you forfeit.{newline}{?}{\\?}Do you really want to forfeit?", null);
				textObject.SetTextVariable("IS_BETTING", (this._missionBoardGameHandler.BetAmount > 0) ? 1 : 0);
				textObject.SetTextVariable("BET_AMOUNT", this._missionBoardGameHandler.BetAmount);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
				textObject.SetTextVariable("newline", "{=!}\n");
				InformationManager.ShowInquiry(new InquiryData(GameTexts.FindText("str_forfeit", null).ToString(), textObject.ToString(), true, true, new TextObject("{=aeouhelq}Yes", null).ToString(), new TextObject("{=8OkPHu4f}No", null).ToString(), new Action(this._missionBoardGameHandler.ForfeitGame), null, "", 0f, null, null, null), true, false);
			}
		}

		// Token: 0x060005CD RID: 1485 RVA: 0x000156C0 File Offset: 0x000138C0
		public override void OnFinalize()
		{
			base.OnFinalize();
			InputKeyItemVM rollDiceKey = this.RollDiceKey;
			if (rollDiceKey == null)
			{
				return;
			}
			rollDiceKey.OnFinalize();
		}

		// Token: 0x170001B9 RID: 441
		// (get) Token: 0x060005CE RID: 1486 RVA: 0x000156D8 File Offset: 0x000138D8
		// (set) Token: 0x060005CF RID: 1487 RVA: 0x000156E0 File Offset: 0x000138E0
		[DataSourceProperty]
		public BoardGameInstructionsVM Instructions
		{
			get
			{
				return this._instructions;
			}
			set
			{
				if (value != this._instructions)
				{
					this._instructions = value;
					base.OnPropertyChangedWithValue<BoardGameInstructionsVM>(value, "Instructions");
				}
			}
		}

		// Token: 0x170001BA RID: 442
		// (get) Token: 0x060005D0 RID: 1488 RVA: 0x000156FE File Offset: 0x000138FE
		// (set) Token: 0x060005D1 RID: 1489 RVA: 0x00015706 File Offset: 0x00013906
		[DataSourceProperty]
		public bool CanRoll
		{
			get
			{
				return this._canRoll;
			}
			set
			{
				if (value != this._canRoll)
				{
					this._canRoll = value;
					base.OnPropertyChangedWithValue(value, "CanRoll");
				}
			}
		}

		// Token: 0x170001BB RID: 443
		// (get) Token: 0x060005D2 RID: 1490 RVA: 0x00015724 File Offset: 0x00013924
		// (set) Token: 0x060005D3 RID: 1491 RVA: 0x0001572C File Offset: 0x0001392C
		[DataSourceProperty]
		public bool IsPlayersTurn
		{
			get
			{
				return this._isPlayersTurn;
			}
			set
			{
				if (value != this._isPlayersTurn)
				{
					this._isPlayersTurn = value;
					base.OnPropertyChangedWithValue(value, "IsPlayersTurn");
				}
			}
		}

		// Token: 0x170001BC RID: 444
		// (get) Token: 0x060005D4 RID: 1492 RVA: 0x0001574A File Offset: 0x0001394A
		// (set) Token: 0x060005D5 RID: 1493 RVA: 0x00015752 File Offset: 0x00013952
		[DataSourceProperty]
		public bool IsGameUsingDice
		{
			get
			{
				return this._isGameUsingDice;
			}
			set
			{
				if (value != this._isGameUsingDice)
				{
					this._isGameUsingDice = value;
					base.OnPropertyChangedWithValue(value, "IsGameUsingDice");
				}
			}
		}

		// Token: 0x170001BD RID: 445
		// (get) Token: 0x060005D6 RID: 1494 RVA: 0x00015770 File Offset: 0x00013970
		// (set) Token: 0x060005D7 RID: 1495 RVA: 0x00015778 File Offset: 0x00013978
		[DataSourceProperty]
		public string DiceResult
		{
			get
			{
				return this._diceResult;
			}
			set
			{
				if (value != this._diceResult)
				{
					this._diceResult = value;
					base.OnPropertyChangedWithValue<string>(value, "DiceResult");
				}
			}
		}

		// Token: 0x170001BE RID: 446
		// (get) Token: 0x060005D8 RID: 1496 RVA: 0x0001579B File Offset: 0x0001399B
		// (set) Token: 0x060005D9 RID: 1497 RVA: 0x000157A3 File Offset: 0x000139A3
		[DataSourceProperty]
		public string RollDiceText
		{
			get
			{
				return this._rollDiceText;
			}
			set
			{
				if (value != this._rollDiceText)
				{
					this._rollDiceText = value;
					base.OnPropertyChangedWithValue<string>(value, "RollDiceText");
				}
			}
		}

		// Token: 0x170001BF RID: 447
		// (get) Token: 0x060005DA RID: 1498 RVA: 0x000157C6 File Offset: 0x000139C6
		// (set) Token: 0x060005DB RID: 1499 RVA: 0x000157CE File Offset: 0x000139CE
		[DataSourceProperty]
		public string TurnOwnerText
		{
			get
			{
				return this._turnOwnerText;
			}
			set
			{
				if (value != this._turnOwnerText)
				{
					this._turnOwnerText = value;
					base.OnPropertyChangedWithValue<string>(value, "TurnOwnerText");
				}
			}
		}

		// Token: 0x170001C0 RID: 448
		// (get) Token: 0x060005DC RID: 1500 RVA: 0x000157F1 File Offset: 0x000139F1
		// (set) Token: 0x060005DD RID: 1501 RVA: 0x000157F9 File Offset: 0x000139F9
		[DataSourceProperty]
		public string BoardGameType
		{
			get
			{
				return this._boardGameType;
			}
			set
			{
				if (value != this._boardGameType)
				{
					this._boardGameType = value;
					base.OnPropertyChangedWithValue<string>(value, "BoardGameType");
				}
			}
		}

		// Token: 0x170001C1 RID: 449
		// (get) Token: 0x060005DE RID: 1502 RVA: 0x0001581C File Offset: 0x00013A1C
		// (set) Token: 0x060005DF RID: 1503 RVA: 0x00015824 File Offset: 0x00013A24
		[DataSourceProperty]
		public string CloseText
		{
			get
			{
				return this._closeText;
			}
			set
			{
				if (value != this._closeText)
				{
					this._closeText = value;
					base.OnPropertyChangedWithValue<string>(value, "CloseText");
				}
			}
		}

		// Token: 0x170001C2 RID: 450
		// (get) Token: 0x060005E0 RID: 1504 RVA: 0x00015847 File Offset: 0x00013A47
		// (set) Token: 0x060005E1 RID: 1505 RVA: 0x0001584F File Offset: 0x00013A4F
		[DataSourceProperty]
		public string ForfeitText
		{
			get
			{
				return this._forfeitText;
			}
			set
			{
				if (value != this._forfeitText)
				{
					this._forfeitText = value;
					base.OnPropertyChangedWithValue<string>(value, "ForfeitText");
				}
			}
		}

		// Token: 0x060005E2 RID: 1506 RVA: 0x00015872 File Offset: 0x00013A72
		public void SetRollDiceKey(HotKey key)
		{
			this.RollDiceKey = InputKeyItemVM.CreateFromHotKey(key, false);
		}

		// Token: 0x170001C3 RID: 451
		// (get) Token: 0x060005E3 RID: 1507 RVA: 0x00015881 File Offset: 0x00013A81
		// (set) Token: 0x060005E4 RID: 1508 RVA: 0x00015889 File Offset: 0x00013A89
		[DataSourceProperty]
		public InputKeyItemVM RollDiceKey
		{
			get
			{
				return this._rollDiceKey;
			}
			set
			{
				if (value != this._rollDiceKey)
				{
					this._rollDiceKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "RollDiceKey");
				}
			}
		}

		// Token: 0x040002DC RID: 732
		private readonly MissionBoardGameLogic _missionBoardGameHandler;

		// Token: 0x040002DD RID: 733
		private BoardGameInstructionsVM _instructions;

		// Token: 0x040002DE RID: 734
		private string _turnOwnerText;

		// Token: 0x040002DF RID: 735
		private string _boardGameType;

		// Token: 0x040002E0 RID: 736
		private bool _isGameUsingDice;

		// Token: 0x040002E1 RID: 737
		private bool _isPlayersTurn;

		// Token: 0x040002E2 RID: 738
		private bool _canRoll;

		// Token: 0x040002E3 RID: 739
		private string _diceResult;

		// Token: 0x040002E4 RID: 740
		private string _rollDiceText;

		// Token: 0x040002E5 RID: 741
		private string _closeText;

		// Token: 0x040002E6 RID: 742
		private string _forfeitText;

		// Token: 0x040002E7 RID: 743
		private InputKeyItemVM _rollDiceKey;
	}
}
