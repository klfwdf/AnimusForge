using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace SandBox.ViewModelCollection.BoardGame;

public class BoardGameInstructionsVM : ViewModel
{
	private readonly CultureObject.BoardGameType _boardGameType;

	private int _currentInstructionIndex;

	private bool _isPreviousButtonEnabled;

	private bool _isNextButtonEnabled;

	private string _instructionsText;

	private string _previousText;

	private string _nextText;

	private string _currentPageText;

	private MBBindingList<BoardGameInstructionVM> _instructionList;

	[DataSourceProperty]
	public bool IsPreviousButtonEnabled
	{
		get
		{
			return _isPreviousButtonEnabled;
		}
		set
		{
			if (value != _isPreviousButtonEnabled)
			{
				_isPreviousButtonEnabled = value;
				OnPropertyChangedWithValue(value, "IsPreviousButtonEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsNextButtonEnabled
	{
		get
		{
			return _isNextButtonEnabled;
		}
		set
		{
			if (value != _isNextButtonEnabled)
			{
				_isNextButtonEnabled = value;
				OnPropertyChangedWithValue(value, "IsNextButtonEnabled");
			}
		}
	}

	[DataSourceProperty]
	public string InstructionsText
	{
		get
		{
			return _instructionsText;
		}
		set
		{
			if (value != _instructionsText)
			{
				_instructionsText = value;
				OnPropertyChangedWithValue(value, "InstructionsText");
			}
		}
	}

	[DataSourceProperty]
	public string PreviousText
	{
		get
		{
			return _previousText;
		}
		set
		{
			if (value != _previousText)
			{
				_previousText = value;
				OnPropertyChangedWithValue(value, "PreviousText");
			}
		}
	}

	[DataSourceProperty]
	public string NextText
	{
		get
		{
			return _nextText;
		}
		set
		{
			if (value != _nextText)
			{
				_nextText = value;
				OnPropertyChangedWithValue(value, "NextText");
			}
		}
	}

	[DataSourceProperty]
	public string CurrentPageText
	{
		get
		{
			return _currentPageText;
		}
		set
		{
			if (value != _currentPageText)
			{
				_currentPageText = value;
				OnPropertyChangedWithValue(value, "CurrentPageText");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<BoardGameInstructionVM> InstructionList
	{
		get
		{
			return _instructionList;
		}
		set
		{
			if (value != _instructionList)
			{
				_instructionList = value;
				OnPropertyChangedWithValue(value, "InstructionList");
			}
		}
	}

	public BoardGameInstructionsVM(CultureObject.BoardGameType boardGameType)
	{
		_boardGameType = boardGameType;
		InstructionList = new MBBindingList<BoardGameInstructionVM>();
		for (int i = 0; i < GetNumberOfInstructions(_boardGameType); i++)
		{
			InstructionList.Add(new BoardGameInstructionVM(_boardGameType, i));
		}
		_currentInstructionIndex = 0;
		if (InstructionList.Count > 0)
		{
			InstructionList[0].IsEnabled = true;
		}
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		InstructionsText = GameTexts.FindText("str_how_to_play").ToString();
		PreviousText = GameTexts.FindText("str_previous").ToString();
		NextText = GameTexts.FindText("str_next").ToString();
		InstructionList.ApplyActionOnAllItems(delegate(BoardGameInstructionVM x)
		{
			x.RefreshValues();
		});
		if (_currentInstructionIndex >= 0 && _currentInstructionIndex < InstructionList.Count)
		{
			TextObject textObject = new TextObject("{=hUSmlhNh}{CURRENT_PAGE}/{TOTAL_PAGES}");
			textObject.SetTextVariable("CURRENT_PAGE", (_currentInstructionIndex + 1).ToString());
			textObject.SetTextVariable("TOTAL_PAGES", InstructionList.Count.ToString());
			CurrentPageText = textObject.ToString();
			IsPreviousButtonEnabled = _currentInstructionIndex != 0;
			IsNextButtonEnabled = _currentInstructionIndex < InstructionList.Count - 1;
		}
	}

	public void ExecuteShowPrevious()
	{
		if (_currentInstructionIndex > 0 && _currentInstructionIndex < InstructionList.Count)
		{
			InstructionList[_currentInstructionIndex].IsEnabled = false;
			_currentInstructionIndex--;
			InstructionList[_currentInstructionIndex].IsEnabled = true;
			RefreshValues();
		}
	}

	public void ExecuteShowNext()
	{
		if (_currentInstructionIndex >= 0 && _currentInstructionIndex < InstructionList.Count - 1)
		{
			InstructionList[_currentInstructionIndex].IsEnabled = false;
			_currentInstructionIndex++;
			InstructionList[_currentInstructionIndex].IsEnabled = true;
			RefreshValues();
		}
	}

	private int GetNumberOfInstructions(CultureObject.BoardGameType game)
	{
		return game switch
		{
			CultureObject.BoardGameType.BaghChal => 4, 
			CultureObject.BoardGameType.Konane => 3, 
			CultureObject.BoardGameType.MuTorere => 2, 
			CultureObject.BoardGameType.Puluc => 5, 
			CultureObject.BoardGameType.Seega => 4, 
			CultureObject.BoardGameType.Tablut => 4, 
			_ => 0, 
		};
	}
}
