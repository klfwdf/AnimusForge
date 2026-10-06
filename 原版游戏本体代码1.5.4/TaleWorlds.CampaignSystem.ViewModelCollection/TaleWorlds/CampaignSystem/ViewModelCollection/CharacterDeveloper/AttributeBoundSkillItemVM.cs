using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper;

public class AttributeBoundSkillItemVM : ViewModel
{
	private string _name;

	private string _skillId;

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

	public AttributeBoundSkillItemVM(SkillObject skill)
	{
		Name = skill.Name.ToString();
		SkillId = skill.StringId;
	}
}
