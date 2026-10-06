using SandBox.BoardGames;
using SandBox.BoardGames.MissionLogics;
using SandBox.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.ViewModelCollection.BoardGame;

public class BoardGameVM : ViewModel
{
	private readonly MissionBoardGameLogic _missionBoardGameHandler;

	private BoardGameInstructionsVM _instructions;

	private string _turnOwnerText;

	private string _boardGameType;

	private bool _isGameUsingDice;

	private bool _isPlayersTurn;

	private bool _canRoll;

	private string _diceResult;

	private string _rollDiceText;

	private string _closeText;

	private string _forfeitText;

	private InputKeyItemVM _rollDiceKey;

	[DataSourceProperty]
	public BoardGameInstructionsVM Instructions
	{
		get
		{
			return _instructions;
		}
		set
		{
			if (value != _instructions)
			{
				_instructions = value;
				OnPropertyChangedWithValue(value, "Instructions");
			}
		}
	}

	[DataSourceProperty]
	public bool CanRoll
	{
		get
		{
			return _canRoll;
		}
		set
		{
			if (value != _canRoll)
			{
				_canRoll = value;
				OnPropertyChangedWithValue(value, "CanRoll");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPlayersTurn
	{
		get
		{
			return _isPlayersTurn;
		}
		set
		{
			if (value != _isPlayersTurn)
			{
				_isPlayersTurn = value;
				OnPropertyChangedWithValue(value, "IsPlayersTurn");
			}
		}
	}

	[DataSourceProperty]
	public bool IsGameUsingDice
	{
		get
		{
			return _isGameUsingDice;
		}
		set
		{
			if (value != _isGameUsingDice)
			{
				_isGameUsingDice = value;
				OnPropertyChangedWithValue(value, "IsGameUsingDice");
			}
		}
	}

	[DataSourceProperty]
	public string DiceResult
	{
		get
		{
			return _diceResult;
		}
		set
		{
			if (value != _diceResult)
			{
				_diceResult = value;
				OnPropertyChangedWithValue(value, "DiceResult");
			}
		}
	}

	[DataSourceProperty]
	public string RollDiceText
	{
		get
		{
			return _rollDiceText;
		}
		set
		{
			if (value != _rollDiceText)
			{
				_rollDiceText = value;
				OnPropertyChangedWithValue(value, "RollDiceText");
			}
		}
	}

	[DataSourceProperty]
	public string TurnOwnerText
	{
		get
		{
			return _turnOwnerText;
		}
		set
		{
			if (value != _turnOwnerText)
			{
				_turnOwnerText = value;
				OnPropertyChangedWithValue(value, "TurnOwnerText");
			}
		}
	}

	[DataSourceProperty]
	public string BoardGameType
	{
		get
		{
			return _boardGameType;
		}
		set
		{
			if (value != _boardGameType)
			{
				_boardGameType = value;
				OnPropertyChangedWithValue(value, "BoardGameType");
			}
		}
	}

	[DataSourceProperty]
	public string CloseText
	{
		get
		{
			return _closeText;
		}
		set
		{
			if (value != _closeText)
			{
				_closeText = value;
				OnPropertyChangedWithValue(value, "CloseText");
			}
		}
	}

	[DataSourceProperty]
	public string ForfeitText
	{
		get
		{
			return _forfeitText;
		}
		set
		{
			if (value != _forfeitText)
			{
				_forfeitText = value;
				OnPropertyChangedWithValue(value, "ForfeitText");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM RollDiceKey
	{
		get
		{
			return _rollDiceKey;
		}
		set
		{
			if (value != _rollDiceKey)
			{
				_rollDiceKey = value;
				OnPropertyChangedWithValue(value, "RollDiceKey");
			}
		}
	}

	public BoardGameVM()
	{
		_missionBoardGameHandler = Mission.Current.GetMissionBehavior<MissionBoardGameLogic>();
		BoardGameType = _missionBoardGameHandler.CurrentBoardGame.ToString();
		IsGameUsingDice = _missionBoardGameHandler.RequiresDiceRolling();
		DiceResult = "-";
		Instructions = new BoardGameInstructionsVM(_missionBoardGameHandler.CurrentBoardGame);
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		RollDiceText = GameTexts.FindText("str_roll_dice").ToString();
		CloseText = GameTexts.FindText("str_close").ToString();
		ForfeitText = GameTexts.FindText("str_forfeit").ToString();
	}

	public void Activate()
	{
		SwitchTurns();
	}

	public void DiceRoll(int roll)
	{
		DiceResult = roll.ToString();
	}

	public void SwitchTurns()
	{
		IsPlayersTurn = _missionBoardGameHandler.Board.PlayerTurn == PlayerTurn.PlayerOne || _missionBoardGameHandler.Board.PlayerTurn == PlayerTurn.PlayerOneWaiting;
		TurnOwnerText = (IsPlayersTurn ? GameTexts.FindText("str_your_turn").ToString() : GameTexts.FindText("str_opponents_turn").ToString());
		DiceResult = "-";
		CanRoll = IsPlayersTurn && IsGameUsingDice;
	}

	public void ExecuteRoll()
	{
		if (CanRoll)
		{
			_missionBoardGameHandler.RollDice();
			CanRoll = false;
		}
	}

	public void ExecuteForfeit()
	{
		if (_missionBoardGameHandler.Board.IsReady && _missionBoardGameHandler.IsGameInProgress)
		{
			TextObject textObject = new TextObject("{=azJulvrp}{?IS_BETTING}You are going to lose {BET_AMOUNT}{GOLD_ICON} if you forfeit.{newline}{?}{\\?}Do you really want to forfeit?");
			textObject.SetTextVariable("IS_BETTING", (_missionBoardGameHandler.BetAmount > 0) ? 1 : 0);
			textObject.SetTextVariable("BET_AMOUNT", _missionBoardGameHandler.BetAmount);
			textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
			textObject.SetTextVariable("newline", "{=!}\n");
			InformationManager.ShowInquiry(new InquiryData(GameTexts.FindText("str_forfeit").ToString(), textObject.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, new TextObject("{=aeouhelq}Yes").ToString(), new TextObject("{=8OkPHu4f}No").ToString(), _missionBoardGameHandler.ForfeitGame, null), pauseGameActiveState: true);
		}
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		RollDiceKey?.OnFinalize();
	}

	public void SetRollDiceKey(HotKey key)
	{
		RollDiceKey = InputKeyItemVM.CreateFromHotKey(key, isConsoleOnly: false);
	}
}
