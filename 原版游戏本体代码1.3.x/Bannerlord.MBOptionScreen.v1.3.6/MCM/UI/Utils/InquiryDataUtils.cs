using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace MCM.UI.Utils
{
	// Token: 0x02000012 RID: 18
	[NullableContext(1)]
	[Nullable(0)]
	internal static class InquiryDataUtils
	{
		// Token: 0x06000057 RID: 87 RVA: 0x00002C20 File Offset: 0x00000E20
		public static InquiryData CreateTranslatable(string titleText, string text, bool isAffirmativeOptionShown, bool isNegativeOptionShown, string affirmativeText, string negativeText, Action affirmativeAction, Action negativeAction)
		{
			return new InquiryData(new TextObject(titleText, null).ToString(), new TextObject(text, null).ToString(), isAffirmativeOptionShown, isNegativeOptionShown, new TextObject(affirmativeText, null).ToString(), new TextObject(negativeText, null).ToString(), affirmativeAction, negativeAction, "", 0f, null, null, null);
		}

		// Token: 0x06000058 RID: 88 RVA: 0x00002C78 File Offset: 0x00000E78
		public static TextInquiryData CreateTextTranslatable(string titleText, string text, bool isAffirmativeOptionShown, bool isNegativeOptionShown, string affirmativeText, string negativeText, Action<string> affirmativeAction, Action negativeAction)
		{
			return new TextInquiryData(new TextObject(titleText, null).ToString(), new TextObject(text, null).ToString(), isAffirmativeOptionShown, isNegativeOptionShown, new TextObject(affirmativeText, null).ToString(), new TextObject(negativeText, null).ToString(), affirmativeAction, negativeAction, false, null, "", "");
		}

		// Token: 0x06000059 RID: 89 RVA: 0x00002CD0 File Offset: 0x00000ED0
		[return: Nullable(2)]
		public static MultiSelectionInquiryData CreateMulti(string titleText, string descriptionText, List<InquiryElement> inquiryElements, bool isExitShown, int minSelectableOptionCount, int maxSelectableOptionCount, string affirmativeText, string negativeText, Action<List<InquiryElement>> affirmativeAction, Action<List<InquiryElement>> negativeAction)
		{
			return new MultiSelectionInquiryData(titleText, descriptionText, inquiryElements, isExitShown, minSelectableOptionCount, maxSelectableOptionCount, affirmativeText, negativeText, affirmativeAction, negativeAction, "", false);
		}

		// Token: 0x0600005A RID: 90 RVA: 0x00002CF8 File Offset: 0x00000EF8
		[return: Nullable(2)]
		public static MultiSelectionInquiryData CreateMultiTranslatable(string titleText, string descriptionText, List<InquiryElement> inquiryElements, bool isExitShown, int minSelectableOptionCount, int maxSelectableOptionCount, string affirmativeText, string negativeText, Action<List<InquiryElement>> affirmativeAction, Action<List<InquiryElement>> negativeAction)
		{
			return InquiryDataUtils.CreateMulti(new TextObject(titleText, null).ToString(), new TextObject(descriptionText, null).ToString(), inquiryElements, isExitShown, minSelectableOptionCount, maxSelectableOptionCount, new TextObject(affirmativeText, null).ToString(), new TextObject(negativeText, null).ToString(), affirmativeAction, negativeAction);
		}
	}
}
