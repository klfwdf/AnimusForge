using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items;

public class EncyclopediaSkillVM : ViewModel
{
	private readonly SkillObject _skill;

	private string _skillId;

	private int _skillValue;

	private BasicTooltipViewModel _hint;

	[DataSourceProperty]
	public BasicTooltipViewModel Hint
	{
		get
		{
			return _hint;
		}
		set
		{
			if (value != _hint)
			{
				_hint = value;
				OnPropertyChangedWithValue(value, "Hint");
			}
		}
	}

	[DataSourceProperty]
	public int SkillValue
	{
		get
		{
			return _skillValue;
		}
		set
		{
			if (value != _skillValue)
			{
				_skillValue = value;
				OnPropertyChangedWithValue(value, "SkillValue");
			}
		}
	}

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

	public EncyclopediaSkillVM(SkillObject skill, int skillValue)
	{
		_skill = skill;
		SkillValue = skillValue;
		SkillId = skill.StringId;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		string name = _skill.Name.ToString();
		string desc = _skill.Description.ToString();
		Hint = new BasicTooltipViewModel(delegate
		{
			GameTexts.SetVariable("STR1", name);
			GameTexts.SetVariable("STR2", desc);
			return GameTexts.FindText("str_string_newline_string").ToString();
		});
	}
}
