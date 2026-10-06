using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Missions.Multiplayer;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.EndOfRound;

public class MultiplayerEndOfRoundVM : ViewModel
{
	private readonly MissionScoreboardComponent _scoreboardComponent;

	private readonly MissionLobbyComponent _missionLobbyComponent;

	private readonly IRoundComponent _multiplayerRoundComponent;

	private readonly string _victoryText;

	private readonly string _defeatText;

	private readonly TextObject _roundEndReasonAllyTeamSideDepletedTextObject;

	private readonly TextObject _roundEndReasonEnemyTeamSideDepletedTextObject;

	private readonly TextObject _roundEndReasonAllyTeamRoundTimeEndedTextObject;

	private readonly TextObject _roundEndReasonEnemyTeamRoundTimeEndedTextObject;

	private readonly TextObject _roundEndReasonAllyTeamGameModeSpecificEndedTextObject;

	private readonly TextObject _roundEndReasonEnemyTeamGameModeSpecificEndedTextObject;

	private readonly TextObject _roundEndReasonRoundTimeEndedWithDrawTextObject;

	private bool _isShown;

	private bool _hasAttackerMVP;

	private bool _hasDefenderMVP;

	private string _title;

	private string _description;

	private string _cultureId;

	private bool _isRoundWinner;

	private MultiplayerEndOfRoundSideVM _attackerSide;

	private MultiplayerEndOfRoundSideVM _defenderSide;

	private MPPlayerVM _attackerMVP;

	private MPPlayerVM _defenderMVP;

	private string _attackerMVPTitleText;

	private string _defenderMVPTitleText;

	[DataSourceProperty]
	public bool IsShown
	{
		get
		{
			return _isShown;
		}
		set
		{
			if (value != _isShown)
			{
				_isShown = value;
				OnPropertyChangedWithValue(value, "IsShown");
				OnIsShownChanged();
			}
		}
	}

	[DataSourceProperty]
	public bool HasAttackerMVP
	{
		get
		{
			return _hasAttackerMVP;
		}
		set
		{
			if (value != _hasAttackerMVP)
			{
				_hasAttackerMVP = value;
				OnPropertyChangedWithValue(value, "HasAttackerMVP");
			}
		}
	}

	[DataSourceProperty]
	public bool HasDefenderMVP
	{
		get
		{
			return _hasDefenderMVP;
		}
		set
		{
			if (value != _hasDefenderMVP)
			{
				_hasDefenderMVP = value;
				OnPropertyChangedWithValue(value, "HasDefenderMVP");
			}
		}
	}

	[DataSourceProperty]
	public string Title
	{
		get
		{
			return _title;
		}
		set
		{
			if (value != _title)
			{
				_title = value;
				OnPropertyChangedWithValue(value, "Title");
			}
		}
	}

	[DataSourceProperty]
	public string Description
	{
		get
		{
			return _description;
		}
		set
		{
			if (value != _description)
			{
				_description = value;
				OnPropertyChangedWithValue(value, "Description");
			}
		}
	}

	[DataSourceProperty]
	public string CultureId
	{
		get
		{
			return _cultureId;
		}
		set
		{
			if (value != _cultureId)
			{
				_cultureId = value;
				OnPropertyChangedWithValue(value, "CultureId");
			}
		}
	}

	[DataSourceProperty]
	public bool IsRoundWinner
	{
		get
		{
			return _isRoundWinner;
		}
		set
		{
			if (value != _isRoundWinner)
			{
				_isRoundWinner = value;
				OnPropertyChangedWithValue(value, "IsRoundWinner");
			}
		}
	}

	[DataSourceProperty]
	public MultiplayerEndOfRoundSideVM AttackerSide
	{
		get
		{
			return _attackerSide;
		}
		set
		{
			if (value != _attackerSide)
			{
				_attackerSide = value;
				OnPropertyChangedWithValue(value, "AttackerSide");
			}
		}
	}

	[DataSourceProperty]
	public MultiplayerEndOfRoundSideVM DefenderSide
	{
		get
		{
			return _defenderSide;
		}
		set
		{
			if (value != _defenderSide)
			{
				_defenderSide = value;
				OnPropertyChangedWithValue(value, "DefenderSide");
			}
		}
	}

	[DataSourceProperty]
	public MPPlayerVM AttackerMVP
	{
		get
		{
			return _attackerMVP;
		}
		set
		{
			if (value != _attackerMVP)
			{
				_attackerMVP = value;
				OnPropertyChangedWithValue(value, "AttackerMVP");
			}
		}
	}

	[DataSourceProperty]
	public MPPlayerVM DefenderMVP
	{
		get
		{
			return _defenderMVP;
		}
		set
		{
			if (value != _defenderMVP)
			{
				_defenderMVP = value;
				OnPropertyChangedWithValue(value, "DefenderMVP");
			}
		}
	}

	[DataSourceProperty]
	public string AttackerMVPTitleText
	{
		get
		{
			return _attackerMVPTitleText;
		}
		set
		{
			if (value != _attackerMVPTitleText)
			{
				_attackerMVPTitleText = value;
				OnPropertyChangedWithValue(value, "AttackerMVPTitleText");
			}
		}
	}

	[DataSourceProperty]
	public string DefenderMVPTitleText
	{
		get
		{
			return _defenderMVPTitleText;
		}
		set
		{
			if (value != _defenderMVPTitleText)
			{
				_defenderMVPTitleText = value;
				OnPropertyChangedWithValue(value, "DefenderMVPTitleText");
			}
		}
	}

	public MultiplayerEndOfRoundVM(MissionScoreboardComponent scoreboardComponent, MissionLobbyComponent missionLobbyComponent, IRoundComponent multiplayerRoundComponent)
	{
		_scoreboardComponent = scoreboardComponent;
		_multiplayerRoundComponent = multiplayerRoundComponent;
		_missionLobbyComponent = missionLobbyComponent;
		_victoryText = new TextObject("{=RCuCoVgd}ROUND WON").ToString();
		_defeatText = new TextObject("{=Dbkx4v90}ROUND LOST").ToString();
		_roundEndReasonAllyTeamSideDepletedTextObject = new TextObject("{=9M4G8DDd}Your team was wiped out");
		_roundEndReasonEnemyTeamSideDepletedTextObject = new TextObject("{=jPXglGWT}Enemy team was wiped out");
		_roundEndReasonAllyTeamRoundTimeEndedTextObject = new TextObject("{=x1HZy70i}Your team had the upper hand at timeout");
		_roundEndReasonEnemyTeamRoundTimeEndedTextObject = new TextObject("{=Dc3fFblo}Enemy team had the upper hand at timeout");
		_roundEndReasonRoundTimeEndedWithDrawTextObject = new TextObject("{=i3dJSlD0}No team had the upper hand at timeout");
		if (_missionLobbyComponent.MissionType == MultiplayerGameType.Battle || _missionLobbyComponent.MissionType == MultiplayerGameType.Captain || _missionLobbyComponent.MissionType == MultiplayerGameType.Skirmish)
		{
			_roundEndReasonAllyTeamGameModeSpecificEndedTextObject = new TextObject("{=xxuzZJ3G}Your team ran out of morale");
			_roundEndReasonEnemyTeamGameModeSpecificEndedTextObject = new TextObject("{=c6c9eYrD}Enemy team ran out of morale");
		}
		else
		{
			_roundEndReasonAllyTeamGameModeSpecificEndedTextObject = TextObject.GetEmpty();
			_roundEndReasonEnemyTeamGameModeSpecificEndedTextObject = TextObject.GetEmpty();
		}
		AttackerSide = new MultiplayerEndOfRoundSideVM();
		DefenderSide = new MultiplayerEndOfRoundSideVM();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		if (_multiplayerRoundComponent != null)
		{
			Refresh();
		}
	}

	public void Refresh()
	{
		BattleSideEnum allyBattleSideEnum = BattleSideEnum.None;
		BattleSideEnum battleSideEnum = BattleSideEnum.None;
		MissionPeer missionPeer = GameNetwork.MyPeer?.GetComponent<MissionPeer>();
		if (missionPeer != null && missionPeer.Team != null)
		{
			allyBattleSideEnum = missionPeer.Team.Side;
			battleSideEnum = ((allyBattleSideEnum != BattleSideEnum.Attacker) ? BattleSideEnum.Attacker : BattleSideEnum.Defender);
		}
		bool num = allyBattleSideEnum == BattleSideEnum.Attacker;
		MissionScoreboardComponent.MissionScoreboardSide missionScoreboardSide = _scoreboardComponent.Sides.FirstOrDefault((MissionScoreboardComponent.MissionScoreboardSide s) => s != null && s.Side == BattleSideEnum.Attacker);
		MissionScoreboardComponent.MissionScoreboardSide missionScoreboardSide2 = _scoreboardComponent.Sides.FirstOrDefault((MissionScoreboardComponent.MissionScoreboardSide s) => s != null && s.Side == BattleSideEnum.Defender);
		MissionScoreboardComponent.MissionScoreboardSide missionScoreboardSide3 = (num ? missionScoreboardSide : missionScoreboardSide2);
		MissionScoreboardComponent.MissionScoreboardSide missionScoreboardSide4 = (num ? missionScoreboardSide2 : missionScoreboardSide);
		BasicCultureObject culture = missionScoreboardSide3.GetCulture();
		BasicCultureObject culture2 = missionScoreboardSide4.GetCulture();
		bool isWinner = _multiplayerRoundComponent.RoundWinner == allyBattleSideEnum;
		bool isWinner2 = _multiplayerRoundComponent.RoundWinner == battleSideEnum;
		AttackerMVPTitleText = GetMVPTitleText(culture);
		DefenderMVPTitleText = GetMVPTitleText(culture2);
		MultiplayerBattleColors multiplayerBattleColors = MultiplayerBattleColors.CreateWith(culture, culture2);
		AttackerSide.SetData(culture, missionScoreboardSide3.SideScore, isWinner, multiplayerBattleColors.AttackerColors);
		DefenderSide.SetData(culture2, missionScoreboardSide4.SideScore, isWinner2, multiplayerBattleColors.DefenderColors);
		if (_scoreboardComponent.Sides.FirstOrDefault((MissionScoreboardComponent.MissionScoreboardSide s) => s != null && s.Side == allyBattleSideEnum) != null && _multiplayerRoundComponent != null)
		{
			bool flag = false;
			if (_multiplayerRoundComponent.RoundWinner == allyBattleSideEnum)
			{
				IsRoundWinner = true;
				Title = _victoryText;
			}
			else if (_multiplayerRoundComponent.RoundWinner == battleSideEnum)
			{
				IsRoundWinner = false;
				Title = _defeatText;
			}
			else
			{
				flag = true;
			}
			switch (_multiplayerRoundComponent.RoundEndReason)
			{
			case RoundEndReason.SideDepleted:
				Description = (IsRoundWinner ? _roundEndReasonEnemyTeamSideDepletedTextObject.ToString() : _roundEndReasonAllyTeamSideDepletedTextObject.ToString());
				break;
			case RoundEndReason.GameModeSpecificEnded:
				Description = (IsRoundWinner ? _roundEndReasonEnemyTeamGameModeSpecificEndedTextObject.ToString() : _roundEndReasonAllyTeamGameModeSpecificEndedTextObject.ToString());
				break;
			case RoundEndReason.RoundTimeEnded:
				Description = (IsRoundWinner ? _roundEndReasonAllyTeamRoundTimeEndedTextObject.ToString() : (flag ? _roundEndReasonRoundTimeEndedWithDrawTextObject.ToString() : _roundEndReasonEnemyTeamRoundTimeEndedTextObject.ToString()));
				break;
			}
		}
	}

	public void OnMVPSelected(MissionPeer mvpPeer)
	{
		BasicCharacterObject basicCharacterObject = MBObjectManager.Instance.GetObject<BasicCharacterObject>("mp_character");
		basicCharacterObject.UpdatePlayerCharacterBodyProperties(mvpPeer.Peer.BodyProperties, mvpPeer.Peer.Race, mvpPeer.Peer.IsFemale);
		basicCharacterObject.Age = mvpPeer.Peer.BodyProperties.Age;
		MissionPeer obj = GameNetwork.MyPeer?.GetComponent<MissionPeer>();
		if (mvpPeer.Team?.Side == obj.Team?.Side)
		{
			AttackerMVP = new MPPlayerVM(mvpPeer);
			AttackerMVP.RefreshDivision();
			AttackerMVP.RefreshPreview(basicCharacterObject, mvpPeer.Peer.BodyProperties.DynamicProperties, mvpPeer.Peer.IsFemale);
			HasAttackerMVP = true;
		}
		else
		{
			DefenderMVP = new MPPlayerVM(mvpPeer);
			DefenderMVP.RefreshDivision();
			DefenderMVP.RefreshPreview(basicCharacterObject, mvpPeer.Peer.BodyProperties.DynamicProperties, mvpPeer.Peer.IsFemale);
			HasDefenderMVP = true;
		}
	}

	private string GetMVPTitleText(BasicCultureObject culture)
	{
		if (culture.StringId == "vlandia")
		{
			return new TextObject("{=3VosbFR0}Vlandian Champion").ToString();
		}
		if (culture.StringId == "sturgia")
		{
			return new TextObject("{=AGUXiN8u}Voivode").ToString();
		}
		if (culture.StringId == "khuzait")
		{
			return new TextObject("{=F2h2cT4q}Khan's Chosen").ToString();
		}
		if (culture.StringId == "battania")
		{
			return new TextObject("{=eWPN3HmE}Hero of Battania").ToString();
		}
		if (culture.StringId == "aserai")
		{
			return new TextObject("{=5zNfxZ7B}War Prince").ToString();
		}
		if (culture.StringId == "empire")
		{
			return new TextObject("{=wwbIcqsq}Conqueror").ToString();
		}
		Debug.FailedAssert("Invalid Culture ID for MVP Title", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection\\EndOfRound\\MultiplayerEndOfRoundVM.cs", "GetMVPTitleText", 205);
		return string.Empty;
	}

	private void OnIsShownChanged()
	{
		if (!IsShown)
		{
			HasAttackerMVP = false;
			HasDefenderMVP = false;
		}
	}
}
