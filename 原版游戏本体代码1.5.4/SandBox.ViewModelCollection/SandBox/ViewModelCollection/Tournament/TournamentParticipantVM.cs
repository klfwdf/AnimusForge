using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.Tournament;

public class TournamentParticipantVM : ViewModel
{
	public enum TournamentPlayerState
	{
		EmptyPlayer,
		GenericPlayer,
		MainPlayer
	}

	private TournamentParticipant _latestParticipant;

	private bool _isInitialized;

	private bool _isValid;

	private string _name = "";

	private string _score = "-";

	private bool _isQualifiedForNextRound;

	private int _state = -1;

	private CharacterImageIdentifierVM _visual;

	private Color _teamColor;

	private bool _isDead;

	private bool _isMainHero;

	private CharacterViewModel _character;

	public TournamentParticipant Participant { get; private set; }

	[DataSourceProperty]
	public bool IsInitialized
	{
		get
		{
			return _isInitialized;
		}
		set
		{
			if (value != _isInitialized)
			{
				_isInitialized = value;
				OnPropertyChangedWithValue(value, "IsInitialized");
			}
		}
	}

	[DataSourceProperty]
	public bool IsValid
	{
		get
		{
			return _isValid;
		}
		set
		{
			if (value != _isValid)
			{
				_isValid = value;
				OnPropertyChangedWithValue(value, "IsValid");
			}
		}
	}

	[DataSourceProperty]
	public bool IsDead
	{
		get
		{
			return _isDead;
		}
		set
		{
			if (value != _isDead)
			{
				_isDead = value;
				OnPropertyChangedWithValue(value, "IsDead");
			}
		}
	}

	[DataSourceProperty]
	public bool IsMainHero
	{
		get
		{
			return _isMainHero;
		}
		set
		{
			if (value != _isMainHero)
			{
				_isMainHero = value;
				OnPropertyChangedWithValue(value, "IsMainHero");
			}
		}
	}

	[DataSourceProperty]
	public Color TeamColor
	{
		get
		{
			return _teamColor;
		}
		set
		{
			if (value != _teamColor)
			{
				_teamColor = value;
				OnPropertyChangedWithValue(value, "TeamColor");
			}
		}
	}

	[DataSourceProperty]
	public CharacterImageIdentifierVM Visual
	{
		get
		{
			return _visual;
		}
		set
		{
			if (value != _visual)
			{
				_visual = value;
				OnPropertyChangedWithValue(value, "Visual");
			}
		}
	}

	[DataSourceProperty]
	public int State
	{
		get
		{
			return _state;
		}
		set
		{
			if (value != _state)
			{
				_state = value;
				OnPropertyChangedWithValue(value, "State");
			}
		}
	}

	[DataSourceProperty]
	public bool IsQualifiedForNextRound
	{
		get
		{
			return _isQualifiedForNextRound;
		}
		set
		{
			if (value != _isQualifiedForNextRound)
			{
				_isQualifiedForNextRound = value;
				OnPropertyChangedWithValue(value, "IsQualifiedForNextRound");
			}
		}
	}

	[DataSourceProperty]
	public string Score
	{
		get
		{
			return _score;
		}
		set
		{
			if (value != _score)
			{
				_score = value;
				OnPropertyChangedWithValue(value, "Score");
			}
		}
	}

	[DataSourceProperty]
	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			if (value != _name)
			{
				_name = value;
				OnPropertyChangedWithValue(value, "Name");
			}
		}
	}

	[DataSourceProperty]
	public CharacterViewModel Character
	{
		get
		{
			return _character;
		}
		set
		{
			if (value != _character)
			{
				_character = value;
				OnPropertyChangedWithValue(value, "Character");
			}
		}
	}

	public TournamentParticipantVM()
	{
		_visual = new CharacterImageIdentifierVM(null);
		_character = new CharacterViewModel(CharacterViewModel.StanceTypes.CelebrateVictory);
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		if (IsInitialized)
		{
			Refresh(Participant, TeamColor);
		}
	}

	public void Refresh(TournamentParticipant participant, Color teamColor)
	{
		Participant = participant;
		TeamColor = teamColor;
		State = ((participant != null) ? ((participant.Character != CharacterObject.PlayerCharacter) ? 1 : 2) : 0);
		IsInitialized = true;
		_latestParticipant = participant;
		if (participant != null)
		{
			Name = participant.Character.Name.ToString();
			CharacterCode characterCode = SandBoxUIHelper.GetCharacterCode(participant.Character);
			Character = new CharacterViewModel(CharacterViewModel.StanceTypes.CelebrateVictory);
			Character.FillFrom(participant.Character);
			Visual = new CharacterImageIdentifierVM(characterCode);
			IsValid = true;
			IsMainHero = participant.Character.IsPlayerCharacter;
		}
	}

	public void ExecuteOpenEncyclopedia()
	{
		if (Participant?.Character != null)
		{
			Campaign.Current.EncyclopediaManager.GoToLink(Participant.Character.EncyclopediaLink);
		}
	}

	public void Refresh()
	{
		OnPropertyChanged("Name");
		OnPropertyChanged("Visual");
		OnPropertyChanged("Score");
		OnPropertyChanged("State");
		OnPropertyChanged("TeamColor");
		OnPropertyChanged("IsDead");
		IsMainHero = _latestParticipant?.Character.IsPlayerCharacter ?? false;
	}
}
