using System;

namespace TaleWorlds.Library;

public class InquiryData
{
	public string TitleText;

	public string Text;

	public readonly float ExpireTime;

	public readonly bool IsAffirmativeOptionShown;

	public readonly bool IsNegativeOptionShown;

	public readonly string AffirmativeText;

	public readonly string NegativeText;

	public readonly string SoundEventPath;

	public readonly Action AffirmativeAction;

	public readonly Action NegativeAction;

	public readonly Action TimeoutAction;

	public readonly Func<(bool, string)> GetIsAffirmativeOptionEnabled;

	public readonly Func<(bool, string)> GetIsNegativeOptionEnabled;

	public InquiryData(string titleText, string text, bool isAffirmativeOptionShown, bool isNegativeOptionShown, string affirmativeText, string negativeText, Action affirmativeAction, Action negativeAction, string soundEventPath = "", float expireTime = 0f, Action timeoutAction = null, Func<(bool, string)> isAffirmativeOptionEnabled = null, Func<(bool, string)> isNegativeOptionEnabled = null)
	{
		TitleText = titleText;
		Text = text;
		IsAffirmativeOptionShown = isAffirmativeOptionShown;
		IsNegativeOptionShown = isNegativeOptionShown;
		GetIsAffirmativeOptionEnabled = isAffirmativeOptionEnabled;
		GetIsNegativeOptionEnabled = isNegativeOptionEnabled;
		AffirmativeText = affirmativeText;
		NegativeText = negativeText;
		AffirmativeAction = affirmativeAction;
		NegativeAction = negativeAction;
		SoundEventPath = soundEventPath;
		ExpireTime = expireTime;
		TimeoutAction = timeoutAction;
	}

	public void SetText(string text)
	{
		Text = text;
	}

	public void SetTitleText(string titleText)
	{
		TitleText = titleText;
	}

	public bool HasSameContentWith(object other)
	{
		if (other is InquiryData inquiryData)
		{
			if (TitleText == inquiryData.TitleText && Text == inquiryData.Text && IsAffirmativeOptionShown == inquiryData.IsAffirmativeOptionShown && IsNegativeOptionShown == inquiryData.IsNegativeOptionShown && GetIsAffirmativeOptionEnabled == inquiryData.GetIsAffirmativeOptionEnabled && GetIsNegativeOptionEnabled == inquiryData.GetIsNegativeOptionEnabled && AffirmativeText == inquiryData.AffirmativeText && NegativeText == inquiryData.NegativeText && AffirmativeAction == inquiryData.AffirmativeAction && NegativeAction == inquiryData.NegativeAction && SoundEventPath == inquiryData.SoundEventPath && ExpireTime == inquiryData.ExpireTime)
			{
				return TimeoutAction == inquiryData.TimeoutAction;
			}
			return false;
		}
		return false;
	}
}
