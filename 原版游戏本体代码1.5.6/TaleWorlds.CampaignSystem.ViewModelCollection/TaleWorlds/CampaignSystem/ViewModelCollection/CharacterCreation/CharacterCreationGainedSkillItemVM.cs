using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.CharacterCreation;

public class CharacterCreationGainedSkillItemVM : ViewModel
{
	private string _skillId;

	private EncyclopediaSkillVM _skill;

	private bool _hasIncreasedInCurrentStage;

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
	public bool HasIncreasedInCurrentStage
	{
		get
		{
			return _hasIncreasedInCurrentStage;
		}
		set
		{
			if (value != _hasIncreasedInCurrentStage)
			{
				_hasIncreasedInCurrentStage = value;
				OnPropertyChangedWithValue(value, "HasIncreasedInCurrentStage");
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

	public CharacterCreationGainedSkillItemVM(SkillObject skill)
	{
		FocusPointGainList = new MBBindingList<BoolItemWithActionVM>();
		SkillObj = skill;
		SkillId = SkillObj.StringId;
		Skill = new EncyclopediaSkillVM(skill, 0);
	}

	public void SetValue(int gainedFromOtherStages, int gainedFromCurrentStage)
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
		HasIncreasedInCurrentStage = gainedFromCurrentStage > 0;
	}

	internal void ResetValues()
	{
		SetValue(0, 0);
	}
}
