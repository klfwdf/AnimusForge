using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.BoardGame;

public class BoardGameInstructionVM : ViewModel
{
	private readonly CultureObject.BoardGameType _game;

	private readonly int _instructionIndex;

	private bool _isEnabled;

	private string _titleText;

	private string _descriptionText;

	private string _gameType;

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
	public string TitleText
	{
		get
		{
			return _titleText;
		}
		set
		{
			if (value != _titleText)
			{
				_titleText = value;
				OnPropertyChangedWithValue(value, "TitleText");
			}
		}
	}

	[DataSourceProperty]
	public string DescriptionText
	{
		get
		{
			return _descriptionText;
		}
		set
		{
			if (value != _descriptionText)
			{
				_descriptionText = value;
				OnPropertyChangedWithValue(value, "DescriptionText");
			}
		}
	}

	[DataSourceProperty]
	public string GameType
	{
		get
		{
			return _gameType;
		}
		set
		{
			if (value != _gameType)
			{
				_gameType = value;
				OnPropertyChangedWithValue(value, "GameType");
			}
		}
	}

	public BoardGameInstructionVM(CultureObject.BoardGameType game, int instructionIndex)
	{
		_game = game;
		_instructionIndex = instructionIndex;
		GameType = _game.ToString();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		GameTexts.SetVariable("newline", "\n");
		TitleText = GameTexts.FindText("str_board_game_title", _game.ToString() + "_" + _instructionIndex).ToString();
		DescriptionText = GameTexts.FindText("str_board_game_instruction", _game.ToString() + "_" + _instructionIndex).ToString();
	}
}
