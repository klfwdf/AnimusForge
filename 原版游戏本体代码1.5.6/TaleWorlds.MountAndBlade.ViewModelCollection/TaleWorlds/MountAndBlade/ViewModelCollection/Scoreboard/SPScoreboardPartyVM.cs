using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.Scoreboard;

public class SPScoreboardPartyVM : ViewModel
{
	private MBBindingList<SPScoreboardUnitVM> _members;

	private SPScoreboardStatsVM _score;

	public IBattleCombatant BattleCombatant { get; private set; }

	public float CurrentPower { get; private set; }

	public float InitialPower { get; private set; }

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
	public MBBindingList<SPScoreboardUnitVM> Members
	{
		get
		{
			return _members;
		}
		set
		{
			if (value != _members)
			{
				_members = value;
				OnPropertyChangedWithValue(value, "Members");
			}
		}
	}

	public SPScoreboardPartyVM(IBattleCombatant battleCombatant)
	{
		BattleCombatant = battleCombatant;
		Members = new MBBindingList<SPScoreboardUnitVM>();
		Score = new SPScoreboardStatsVM((battleCombatant != null) ? battleCombatant.Name : new TextObject("{=qnxJYAs7}Party"));
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Score.RefreshValues();
		Members.ApplyActionOnAllItems(delegate(SPScoreboardUnitVM x)
		{
			x.RefreshValues();
		});
	}

	public void UpdateScores(BasicCharacterObject character, int numberRemaining, int numberDead, int numberWounded, int numberRouted, int numberKilled, int numberReadyToUpgrade)
	{
		GetUnitAddIfNotExists(character).UpdateScores(numberRemaining, numberDead, numberWounded, numberRouted, numberKilled, numberReadyToUpgrade);
		Score?.UpdateScores(numberRemaining, numberDead, numberWounded, numberRouted, numberKilled, numberReadyToUpgrade);
		RefreshPower();
	}

	public void UpdateHeroSkills(BasicCharacterObject heroCharacter, SkillObject upgradedSkill)
	{
		GetUnitAddIfNotExists(heroCharacter).UpdateHeroSkills(upgradedSkill, heroCharacter.GetSkillValue(upgradedSkill));
	}

	public SPScoreboardUnitVM GetUnitAddIfNotExists(BasicCharacterObject character)
	{
		if (character == Game.Current.PlayerTroop)
		{
			Score.IsMainParty = true;
		}
		SPScoreboardUnitVM sPScoreboardUnitVM = Members.FirstOrDefault((SPScoreboardUnitVM p) => p.Character == character);
		if (sPScoreboardUnitVM == null)
		{
			sPScoreboardUnitVM = new SPScoreboardUnitVM(character);
			Members.Add(sPScoreboardUnitVM);
			Members.Sort(new SPScoreboardSortControllerVM.ItemMemberComparer());
		}
		return sPScoreboardUnitVM;
	}

	public SPScoreboardUnitVM GetUnit(BasicCharacterObject character)
	{
		return Members.FirstOrDefault((SPScoreboardUnitVM p) => p.Character == character);
	}

	internal SPScoreboardStatsVM RemoveUnit(BasicCharacterObject troop)
	{
		SPScoreboardUnitVM sPScoreboardUnitVM = Members.FirstOrDefault((SPScoreboardUnitVM m) => m.Character == troop);
		SPScoreboardStatsVM sPScoreboardStatsVM = null;
		if (sPScoreboardUnitVM != null)
		{
			sPScoreboardStatsVM = sPScoreboardUnitVM.Score.GetScoreForOneAliveMember();
			UpdateScores(troop, -sPScoreboardStatsVM.Remaining, -sPScoreboardStatsVM.Dead, -sPScoreboardStatsVM.Wounded, -sPScoreboardStatsVM.Routed, -sPScoreboardStatsVM.Kill, -sPScoreboardStatsVM.ReadyToUpgrade);
			if (!sPScoreboardUnitVM.Score.IsAnyStatRelevant())
			{
				Members.Remove(sPScoreboardUnitVM);
				if (troop == Game.Current.PlayerTroop)
				{
					Score.IsMainParty = false;
				}
			}
		}
		return sPScoreboardStatsVM;
	}

	internal void AddUnit(BasicCharacterObject unit, SPScoreboardStatsVM scoreToBringOver)
	{
		Score.UpdateScores(scoreToBringOver.Remaining, scoreToBringOver.Dead, scoreToBringOver.Wounded, scoreToBringOver.Routed, scoreToBringOver.Kill, scoreToBringOver.ReadyToUpgrade);
		SPScoreboardUnitVM sPScoreboardUnitVM = Members.FirstOrDefault((SPScoreboardUnitVM m) => m.Character == unit);
		if (sPScoreboardUnitVM == null)
		{
			sPScoreboardUnitVM = new SPScoreboardUnitVM(unit);
			Members.Add(sPScoreboardUnitVM);
			if (unit == Game.Current.PlayerTroop)
			{
				Score.IsMainParty = true;
			}
		}
		sPScoreboardUnitVM.Score.UpdateScores(scoreToBringOver.Remaining, scoreToBringOver.Dead, scoreToBringOver.Wounded, scoreToBringOver.Routed, scoreToBringOver.Kill, scoreToBringOver.ReadyToUpgrade);
	}

	private void RefreshPower()
	{
		CurrentPower = 0f;
		InitialPower = 0f;
		foreach (SPScoreboardUnitVM member in Members)
		{
			CurrentPower += (float)member.Score.Remaining * member.Character.GetPower();
			InitialPower += (float)(member.Score.Dead + member.Score.Routed + member.Score.Wounded + member.Score.Remaining) * member.Character.GetPower();
		}
	}
}
