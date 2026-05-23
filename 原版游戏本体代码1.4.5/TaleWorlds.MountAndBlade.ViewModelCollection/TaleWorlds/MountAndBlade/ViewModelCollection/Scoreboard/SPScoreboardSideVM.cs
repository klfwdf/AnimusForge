using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.Scoreboard;

public class SPScoreboardSideVM : ViewModel
{
	private MBBindingList<SPScoreboardPartyVM> _parties;

	private MBBindingList<SPScoreboardShipVM> _ships;

	private SPScoreboardStatsVM _score;

	private BannerImageIdentifierVM _bannerVisual;

	private BannerImageIdentifierVM _bannerVisualSmall;

	private SPScoreboardSortControllerVM _sortController;

	private float _morale;

	private BasicTooltipViewModel _moraleHint;

	private bool _isPlayerSide;

	public float CurrentPower { get; private set; }

	public float InitialPower { get; private set; }

	[DataSourceProperty]
	public BannerImageIdentifierVM BannerVisual
	{
		get
		{
			return _bannerVisual;
		}
		set
		{
			if (value != _bannerVisual)
			{
				_bannerVisual = value;
				OnPropertyChangedWithValue(value, "BannerVisual");
			}
		}
	}

	[DataSourceProperty]
	public BannerImageIdentifierVM BannerVisualSmall
	{
		get
		{
			return _bannerVisualSmall;
		}
		set
		{
			if (value != _bannerVisualSmall)
			{
				_bannerVisualSmall = value;
				OnPropertyChangedWithValue(value, "BannerVisualSmall");
			}
		}
	}

	[DataSourceProperty]
	public SPScoreboardStatsVM Score
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
	public MBBindingList<SPScoreboardPartyVM> Parties
	{
		get
		{
			return _parties;
		}
		set
		{
			if (value != _parties)
			{
				_parties = value;
				OnPropertyChangedWithValue(value, "Parties");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<SPScoreboardShipVM> Ships
	{
		get
		{
			return _ships;
		}
		set
		{
			if (value != _ships)
			{
				_ships = value;
				OnPropertyChangedWithValue(value, "Ships");
			}
		}
	}

	[DataSourceProperty]
	public SPScoreboardSortControllerVM SortController
	{
		get
		{
			return _sortController;
		}
		set
		{
			if (value != _sortController)
			{
				_sortController = value;
				OnPropertyChanged("SortController");
			}
		}
	}

	[DataSourceProperty]
	public float Morale
	{
		get
		{
			return _morale;
		}
		set
		{
			if (value != _morale)
			{
				_morale = value;
				OnPropertyChangedWithValue(value, "Morale");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel MoraleHint
	{
		get
		{
			return _moraleHint;
		}
		set
		{
			if (value != _moraleHint)
			{
				_moraleHint = value;
				OnPropertyChangedWithValue(value, "MoraleHint");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPlayerSide
	{
		get
		{
			return _isPlayerSide;
		}
		set
		{
			if (value != _isPlayerSide)
			{
				_isPlayerSide = value;
				OnPropertyChangedWithValue(value, "IsPlayerSide");
			}
		}
	}

	public SPScoreboardSideVM(TextObject name, Banner sideFlag, bool isSimulation, bool isPlayerSide)
	{
		SPScoreboardSideVM sPScoreboardSideVM = this;
		Parties = new MBBindingList<SPScoreboardPartyVM>();
		Ships = new MBBindingList<SPScoreboardShipVM>();
		Score = new SPScoreboardStatsVM(name);
		IsPlayerSide = isPlayerSide;
		MBBindingList<SPScoreboardPartyVM> listToControl = Parties;
		SortController = new SPScoreboardSortControllerVM(ref listToControl);
		Parties = listToControl;
		if (sideFlag != null)
		{
			BannerVisual = new BannerImageIdentifierVM(sideFlag, nineGrid: true);
			BannerVisualSmall = new BannerImageIdentifierVM(sideFlag);
		}
		MoraleHint = new BasicTooltipViewModel(() => sPScoreboardSideVM.GetMoraleHintStr(isSimulation));
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Score.RefreshValues();
		Parties.ApplyActionOnAllItems(delegate(SPScoreboardPartyVM x)
		{
			x.RefreshValues();
		});
		Ships.ApplyActionOnAllItems(delegate(SPScoreboardShipVM x)
		{
			x.RefreshValues();
		});
	}

	private string GetMoraleHintStr(bool isSimulation)
	{
		return GameTexts.FindText("str_LEFT_colon_RIGHT_wSpaceAfterColon").SetTextVariable("LEFT", isSimulation ? GameTexts.FindText("str_morale").ToString() : new TextObject("{=trPyg7mr}Battle Morale").ToString()).SetTextVariable("RIGHT", MathF.Round(Morale).ToString())
			.ToString();
	}

	public void UpdateScores(IBattleCombatant battleCombatant, bool isPlayerParty, BasicCharacterObject character, int numberRemaining, int numberDead, int numberWounded, int numberRouted, int numberKilled, int numberReadyToUpgrade)
	{
		GetPartyAddIfNotExists(battleCombatant, isPlayerParty).UpdateScores(character, numberRemaining, numberDead, numberWounded, numberRouted, numberKilled, numberReadyToUpgrade);
		Score.UpdateScores(numberRemaining, numberDead, numberWounded, numberRouted, numberKilled, numberReadyToUpgrade);
		RefreshPower();
	}

	public void UpdateHeroSkills(IBattleCombatant battleCombatant, bool isPlayerParty, BasicCharacterObject heroCharacter, SkillObject upgradedSkill)
	{
		GetPartyAddIfNotExists(battleCombatant, isPlayerParty).UpdateHeroSkills(heroCharacter, upgradedSkill);
	}

	public SPScoreboardPartyVM GetPartyAddIfNotExists(IBattleCombatant battleCombatant, bool isPlayerParty)
	{
		SPScoreboardPartyVM sPScoreboardPartyVM = Parties.FirstOrDefault((SPScoreboardPartyVM p) => p.BattleCombatant == battleCombatant);
		if (sPScoreboardPartyVM == null)
		{
			sPScoreboardPartyVM = new SPScoreboardPartyVM(battleCombatant);
			if (isPlayerParty)
			{
				Parties.Insert(0, sPScoreboardPartyVM);
			}
			else
			{
				Parties.Add(sPScoreboardPartyVM);
			}
		}
		return sPScoreboardPartyVM;
	}

	public SPScoreboardPartyVM GetParty(IBattleCombatant battleCombatant)
	{
		return Parties.FirstOrDefault((SPScoreboardPartyVM p) => p.BattleCombatant == battleCombatant);
	}

	public SPScoreboardStatsVM RemoveTroop(IBattleCombatant battleCombatant, BasicCharacterObject troop)
	{
		SPScoreboardPartyVM sPScoreboardPartyVM = Parties.FirstOrDefault((SPScoreboardPartyVM p) => p.BattleCombatant == battleCombatant);
		SPScoreboardStatsVM sPScoreboardStatsVM = sPScoreboardPartyVM.RemoveUnit(troop);
		if (sPScoreboardPartyVM.Members.Count == 0)
		{
			Parties.Remove(sPScoreboardPartyVM);
		}
		Score.UpdateScores(-sPScoreboardStatsVM.Remaining, -sPScoreboardStatsVM.Dead, -sPScoreboardStatsVM.Wounded, -sPScoreboardStatsVM.Routed, -sPScoreboardStatsVM.Kill, -sPScoreboardStatsVM.ReadyToUpgrade);
		return sPScoreboardStatsVM;
	}

	public void AddTroop(IBattleCombatant battleCombatant, BasicCharacterObject currentTroop, SPScoreboardStatsVM scoreToBringOver)
	{
		Parties.FirstOrDefault((SPScoreboardPartyVM p) => p.BattleCombatant == battleCombatant).AddUnit(currentTroop, scoreToBringOver);
		Score.UpdateScores(scoreToBringOver.Remaining, scoreToBringOver.Dead, scoreToBringOver.Wounded, scoreToBringOver.Routed, scoreToBringOver.Kill, scoreToBringOver.ReadyToUpgrade);
	}

	public SPScoreboardShipVM GetShipAddIfNotExists(IShipOrigin ship, string shipType, IBattleCombatant owner, TeamSideEnum teamSideEnum)
	{
		SPScoreboardShipVM sPScoreboardShipVM = Ships.FirstOrDefault((SPScoreboardShipVM p) => p.Ship == ship);
		if (sPScoreboardShipVM == null)
		{
			sPScoreboardShipVM = new SPScoreboardShipVM(ship, shipType, owner, teamSideEnum);
			Ships.Add(sPScoreboardShipVM);
		}
		return sPScoreboardShipVM;
	}

	private void RefreshPower()
	{
		CurrentPower = 0f;
		InitialPower = 0f;
		foreach (SPScoreboardPartyVM party in _parties)
		{
			InitialPower += party.InitialPower;
			CurrentPower += party.CurrentPower;
		}
	}
}
