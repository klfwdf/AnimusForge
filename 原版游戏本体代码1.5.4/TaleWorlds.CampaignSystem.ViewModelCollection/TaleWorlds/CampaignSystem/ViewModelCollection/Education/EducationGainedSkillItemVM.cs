using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Education;

public class EducationGainedSkillItemVM : ViewModel
{
	private string _skillId;

	private EncyclopediaSkillVM _skill;

	private bool _hasFocusIncreasedInCurrentStage;

	private bool _hasSkillValueIncreasedInCurrentStage;

	private int _skillValueInt;

	private MBBindingList<BoolItemWithActionVM> _focusPointGainList;

	public SkillObject SkillObj { get; private set; }

	[DataSourceProperty]
	public string SkillId
	{
		get
		{
			return _skillId;
		}
		set
		{
			if (value != _skillId)
			{
				_skillId = value;
				OnPropertyChangedWithValue(value, "SkillId");
			}
		}
	}

	[DataSourceProperty]
	public int SkillValueInt
	{
		get
		{
			return _skillValueInt;
		}
		set
		{
			if (value != _skillValueInt)
			{
				_skillValueInt = value;
				OnPropertyChangedWithValue(value, "SkillValueInt");
			}
		}
	}

	[DataSourceProperty]
	public EncyclopediaSkillVM Skill
	{
		get
		{
			return _skill;
		}
		set
		{
			if (value != _skill)
			{
				_skill = value;
				OnPropertyChangedWithValue(value, "Skill");
			}
		}
	}

	[DataSourceProperty]
	public bool HasFocusIncreasedInCurrentStage
	{
		get
		{
			return _hasFocusIncreasedInCurrentStage;
		}
		set
		{
			if (value != _hasFocusIncreasedInCurrentStage)
			{
				_hasFocusIncreasedInCurrentStage = value;
				OnPropertyChangedWithValue(value, "HasFocusIncreasedInCurrentStage");
			}
		}
	}

	[DataSourceProperty]
	public bool HasSkillValueIncreasedInCurrentStage
	{
		get
		{
			return _hasSkillValueIncreasedInCurrentStage;
		}
		set
		{
			if (value != _hasSkillValueIncreasedInCurrentStage)
			{
				_hasSkillValueIncreasedInCurrentStage = value;
				OnPropertyChangedWithValue(value, "HasSkillValueIncreasedInCurrentStage");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<BoolItemWithActionVM> FocusPointGainList
	{
		get
		{
			return _focusPointGainList;
		}
		set
		{
			if (value != _focusPointGainList)
			{
				_focusPointGainList = value;
				OnPropertyChangedWithValue(value, "FocusPointGainList");
			}
		}
	}

	public EducationGainedSkillItemVM(SkillObject skill)
	{
		FocusPointGainList = new MBBindingList<BoolItemWithActionVM>();
		SkillObj = skill;
		SkillId = SkillObj.StringId;
		Skill = new EncyclopediaSkillVM(skill, 0);
	}

	public void SetFocusValue(int gainedFromOtherStages, int gainedFromCurrentStage)
	{
		FocusPointGainList.Clear();
		for (int i = 0; i < gainedFromOtherStages; i++)
		{
			FocusPointGainList.Add(new BoolItemWithActionVM(null, isActive: false, null));
		}
		for (int j = 0; j < gainedFromCurrentStage; j++)
		{
			FocusPointGainList.Add(new BoolItemWithActionVM(null, isActive: true, null));
		}
		HasFocusIncreasedInCurrentStage = gainedFromCurrentStage > 0;
	}

	public void SetSkillValue(int gaintedFromOtherStages, int gainedFromCurrentStage)
	{
		SkillValueInt = gaintedFromOtherStages + gainedFromCurrentStage;
		HasSkillValueIncreasedInCurrentStage = gainedFromCurrentStage > 0;
	}

	internal void ResetValues()
	{
		SetFocusValue(0, 0);
		SetSkillValue(0, 0);
	}
}
