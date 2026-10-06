using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.Scoreboard;

public class SPScoreboardUnitVM : ViewModel
{
	public readonly BasicCharacterObject Character;

	private readonly List<SPScoreboardSkillItemVM> _skills;

	private SPScoreboardStatsVM _score;

	private bool _isHero;

	private bool _isGainedAnySkills;

	private MBBindingList<SPScoreboardSkillItemVM> _gainedSkills;

	[DataSourceProperty]
	public bool IsGainedAnySkills
	{
		get
		{
			return _isGainedAnySkills;
		}
		set
		{
			if (value != _isGainedAnySkills)
			{
				_isGainedAnySkills = value;
				OnPropertyChangedWithValue(value, "IsGainedAnySkills");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<SPScoreboardSkillItemVM> GainedSkills
	{
		get
		{
			return _gainedSkills;
		}
		set
		{
			if (value != _gainedSkills)
			{
				_gainedSkills = value;
				OnPropertyChangedWithValue(value, "GainedSkills");
			}
		}
	}

	[DataSourceProperty]
	public bool IsHero
	{
		get
		{
			return _isHero;
		}
		set
		{
			if (value != _isHero)
			{
				_isHero = value;
				OnPropertyChangedWithValue(value, "IsHero");
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

	public SPScoreboardUnitVM(BasicCharacterObject character)
	{
		Character = character;
		GainedSkills = new MBBindingList<SPScoreboardSkillItemVM>();
		_skills = new List<SPScoreboardSkillItemVM>();
		Score = new SPScoreboardStatsVM(character.Name);
		CharacterCode.CreateFrom(character);
		IsHero = character.IsHero;
		Score.IsMainHero = character == Game.Current.PlayerTroop;
		IsGainedAnySkills = false;
		if (!character.IsHero)
		{
			return;
		}
		foreach (SkillObject objectType in Game.Current.ObjectManager.GetObjectTypeList<SkillObject>())
		{
			_skills.Add(new SPScoreboardSkillItemVM(objectType, character.GetSkillValue(objectType)));
		}
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Score.RefreshValues();
		GainedSkills.ApplyActionOnAllItems(delegate(SPScoreboardSkillItemVM x)
		{
			x.RefreshValues();
		});
	}

	private void ExecuteActivateGainedSkills()
	{
	}

	private void ExecuteDeactivateGainedSkills()
	{
	}

	public void UpdateScores(int numberRemaining, int numberDead, int numberWounded, int numberRouted, int numberKilled, int numberReadyToUpgrade)
	{
		Score.UpdateScores(numberRemaining, numberDead, numberWounded, numberRouted, numberKilled, numberReadyToUpgrade);
	}

	public void UpdateHeroSkills(SkillObject gainedSkill, int currentSkill)
	{
		SPScoreboardSkillItemVM sPScoreboardSkillItemVM = _skills.First((SPScoreboardSkillItemVM s) => s.Skill == gainedSkill);
		sPScoreboardSkillItemVM.UpdateSkill(currentSkill);
		if (!GainedSkills.Contains(sPScoreboardSkillItemVM))
		{
			GainedSkills.Add(sPScoreboardSkillItemVM);
		}
		IsGainedAnySkills = GainedSkills.Count > 0;
	}
}
