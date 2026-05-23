using System;

namespace TaleWorlds.Library;

public class TextInquiryData
{
	public string TitleText;

	public string Text = "";

	public readonly bool IsAffirmativeOptionShown;

	public readonly bool IsNegativeOptionShown;

	public readonly bool IsInputObfuscated;

	public readonly string AffirmativeText;

	public readonly string NegativeText;

	public readonly string SoundEventPath;

	public readonly string DefaultInputText;

	public readonly Action<string> AffirmativeAction;

	public readonly Action NegativeAction;

	public readonly Func<string, Tuple<bool, string>> TextCondition;

	public TextInquiryData(string titleText, string text, bool isAffirmativeOptionShown, bool isNegativeOptionShown, string affirmativeText, string negativeText, Action<string> affirmativeAction, Action negativeAction, bool shouldInputBeObfuscated = false, Func<string, Tuple<bool, string>> textCondition = null, string soundEventPath = "", string defaultInputText = "")
	{
		TitleText = titleText;
		Text = text;
		IsAffirmativeOptionShown = isAffirmativeOptionShown;
		IsNegativeOptionShown = isNegativeOptionShown;
		AffirmativeText = affirmativeText;
		NegativeText = negativeText;
		AffirmativeAction = affirmativeAction;
		NegativeAction = negativeAction;
		TextCondition = textCondition;
		IsInputObfuscated = shouldInputBeObfuscated;
		SoundEventPath = soundEventPath;
		DefaultInputText = defaultInputText;
	}

	public bool HasSameContentWith(object other)
	{
		if (other is TextInquiryData textInquiryData)
		{
			if (TitleText == textInquiryData.TitleText && Text == textInquiryData.Text && IsAffirmativeOptionShown == textInquiryData.IsAffirmativeOptionShown && IsNegativeOptionShown == textInquiryData.IsNegativeOptionShown && AffirmativeText == textInquiryData.AffirmativeText && NegativeText == textInquiryData.NegativeText && AffirmativeAction == textInquiryData.AffirmativeAction && NegativeAction == textInquiryData.NegativeAction && TextCondition == textInquiryData.TextCondition && IsInputObfuscated == textInquiryData.IsInputObfuscated && SoundEventPath == textInquiryData.SoundEventPath)
			{
				return DefaultInputText == textInquiryData.DefaultInputText;
			}
			return false;
		}
		return false;
	}
}
