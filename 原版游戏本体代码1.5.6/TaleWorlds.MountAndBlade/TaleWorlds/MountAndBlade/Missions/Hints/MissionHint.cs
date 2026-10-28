using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.Missions.Hints;

public class MissionHint
{
	public readonly TextObject Description;

	public MissionHint(TextObject description)
	{
		Description = description;
	}

	public static MissionHint CreateWithKeyAndAction(TextObject actionText, string hotKeyId)
	{
		TextObject textObject = GameTexts.FindText("str_key_action").CopyTextObject();
		textObject.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(hotKeyId));
		textObject.SetTextVariable("ACTION", actionText);
		return new MissionHint(textObject);
	}
}
