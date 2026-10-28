using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.Scoreboard;

public class SPScoreboardSkillItemVM : ViewModel
{
	public SkillObject Skill;

	private readonly int _initialValue;

	private int _newValue;

	private string _level;

	private string _imagePath;

	private string _description;

	[DataSourceProperty]
	public string Level
	{
		get
		{
			return _level;
		}
		set
		{
			if (value != _level)
			{
				_level = value;
				OnPropertyChangedWithValue(value, "Level");
			}
		}
	}

	[DataSourceProperty]
	public string SkillId
	{
		get
		{
			return _imagePath;
		}
		set
		{
			if (value != _imagePath)
			{
				_imagePath = value;
				OnPropertyChangedWithValue(value, "SkillId");
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

	public SPScoreboardSkillItemVM(SkillObject skill, int initialValue)
	{
		Skill = skill;
		_initialValue = initialValue;
		_newValue = initialValue;
		SkillId = skill.StringId;
		Description = "(" + initialValue + ")";
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Level = Skill.Name.ToString();
	}

	public void UpdateSkill(int newValue)
	{
		_newValue = newValue;
		Description = "+" + (newValue - _initialValue) + "(" + newValue + ")";
	}

	public bool IsValid()
	{
		return _newValue > _initialValue;
	}
}
