using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade.View;

public class ConversationTagView
{
	public static string GetSkillMeshName(SkillObject skillEnum, bool isOn = false)
	{
		if (isOn)
		{
			return "skill_icon_" + skillEnum.StringId.ToLower() + "_on";
		}
		return "skill_icon_" + skillEnum.StringId.ToLower() + "_off";
	}
}
