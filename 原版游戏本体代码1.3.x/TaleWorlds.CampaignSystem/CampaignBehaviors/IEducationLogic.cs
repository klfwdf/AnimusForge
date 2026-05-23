using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x020003FC RID: 1020
	public interface IEducationLogic
	{
		// Token: 0x06003FAB RID: 16299
		void Finalize(Hero child, List<string> chosenOptions);

		// Token: 0x06003FAC RID: 16300
		void GetOptionProperties(Hero child, string optionKey, List<string> previousChoices, out TextObject optionTitle, out TextObject description, out TextObject effect, out ValueTuple<CharacterAttribute, int>[] attributes, out ValueTuple<SkillObject, int>[] skills, out ValueTuple<SkillObject, int>[] focusPoints, out EducationCampaignBehavior.EducationCharacterProperties[] characterProperties);

		// Token: 0x06003FAD RID: 16301
		void GetPageProperties(Hero child, List<string> previousChoices, out TextObject title, out TextObject description, out TextObject instruction, out EducationCampaignBehavior.EducationCharacterProperties[] defaultProperties, out string[] availableOptions);

		// Token: 0x06003FAE RID: 16302
		void GetStageProperties(Hero child, out int pageCount);

		// Token: 0x06003FAF RID: 16303
		bool IsValidEducationNotification(EducationMapNotification educationMapNotification);
	}
}
