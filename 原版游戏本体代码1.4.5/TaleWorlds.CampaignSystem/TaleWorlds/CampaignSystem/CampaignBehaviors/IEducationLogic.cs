using System.Collections.Generic;
using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public interface IEducationLogic
{
	void Finalize(Hero child, List<string> chosenOptions);

	void GetOptionProperties(Hero child, string optionKey, List<string> previousChoices, out TextObject optionTitle, out TextObject description, out TextObject effect, out (CharacterAttribute, int)[] attributes, out (SkillObject, int)[] skills, out (SkillObject, int)[] focusPoints, out EducationCampaignBehavior.EducationCharacterProperties[] characterProperties);

	void GetPageProperties(Hero child, List<string> previousChoices, out TextObject title, out TextObject description, out TextObject instruction, out EducationCampaignBehavior.EducationCharacterProperties[] defaultProperties, out string[] availableOptions);

	void GetStageProperties(Hero child, out int pageCount);

	bool IsValidEducationNotification(EducationMapNotification educationMapNotification);
}
