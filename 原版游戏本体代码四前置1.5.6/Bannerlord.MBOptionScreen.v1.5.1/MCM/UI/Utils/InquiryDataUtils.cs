using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace MCM.UI.Utils;

internal static class InquiryDataUtils
{
	public static InquiryData CreateTranslatable(string titleText, string text, bool isAffirmativeOptionShown, bool isNegativeOptionShown, string affirmativeText, string negativeText, Action affirmativeAction, Action negativeAction)
	{
		return new InquiryData(new TextObject(titleText).ToString(), new TextObject(text).ToString(), isAffirmativeOptionShown, isNegativeOptionShown, new TextObject(affirmativeText).ToString(), new TextObject(negativeText).ToString(), affirmativeAction, negativeAction);
	}

	public static TextInquiryData CreateTextTranslatable(string titleText, string text, bool isAffirmativeOptionShown, bool isNegativeOptionShown, string affirmativeText, string negativeText, Action<string> affirmativeAction, Action negativeAction)
	{
		return new TextInquiryData(new TextObject(titleText).ToString(), new TextObject(text).ToString(), isAffirmativeOptionShown, isNegativeOptionShown, new TextObject(affirmativeText).ToString(), new TextObject(negativeText).ToString(), affirmativeAction, negativeAction);
	}

	public static MultiSelectionInquiryData? CreateMulti(string titleText, string descriptionText, List<InquiryElement> inquiryElements, bool isExitShown, int minSelectableOptionCount, int maxSelectableOptionCount, string affirmativeText, string negativeText, Action<List<InquiryElement>> affirmativeAction, Action<List<InquiryElement>> negativeAction)
	{
		return new MultiSelectionInquiryData(titleText, descriptionText, inquiryElements, isExitShown, minSelectableOptionCount, maxSelectableOptionCount, affirmativeText, negativeText, affirmativeAction, negativeAction);
	}

	public static MultiSelectionInquiryData? CreateMultiTranslatable(string titleText, string descriptionText, List<InquiryElement> inquiryElements, bool isExitShown, int minSelectableOptionCount, int maxSelectableOptionCount, string affirmativeText, string negativeText, Action<List<InquiryElement>> affirmativeAction, Action<List<InquiryElement>> negativeAction)
	{
		return CreateMulti(new TextObject(titleText).ToString(), new TextObject(descriptionText).ToString(), inquiryElements, isExitShown, minSelectableOptionCount, maxSelectableOptionCount, new TextObject(affirmativeText).ToString(), new TextObject(negativeText).ToString(), affirmativeAction, negativeAction);
	}
}
