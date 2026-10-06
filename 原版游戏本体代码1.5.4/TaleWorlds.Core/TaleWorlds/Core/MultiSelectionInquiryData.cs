using System;
using System.Collections.Generic;

namespace TaleWorlds.Core;

public class MultiSelectionInquiryData
{
	public readonly string TitleText;

	public readonly string DescriptionText;

	public readonly List<InquiryElement> InquiryElements;

	public readonly bool IsExitShown;

	public readonly int MaxSelectableOptionCount;

	public readonly int MinSelectableOptionCount;

	public readonly string SoundEventPath;

	public readonly string AffirmativeText;

	public readonly string NegativeText;

	public readonly Action<List<InquiryElement>> AffirmativeAction;

	public readonly Action<List<InquiryElement>> NegativeAction;

	public readonly bool IsSeachAvailable;

	public MultiSelectionInquiryData(string titleText, string descriptionText, List<InquiryElement> inquiryElements, bool isExitShown, int minSelectableOptionCount, int maxSelectableOptionCount, string affirmativeText, string negativeText, Action<List<InquiryElement>> affirmativeAction, Action<List<InquiryElement>> negativeAction, string soundEventPath = "", bool isSeachAvailable = false)
	{
		TitleText = titleText;
		DescriptionText = descriptionText;
		InquiryElements = inquiryElements;
		IsExitShown = isExitShown;
		AffirmativeText = affirmativeText;
		NegativeText = negativeText;
		AffirmativeAction = affirmativeAction;
		NegativeAction = negativeAction;
		MinSelectableOptionCount = minSelectableOptionCount;
		MaxSelectableOptionCount = maxSelectableOptionCount;
		SoundEventPath = soundEventPath;
		IsSeachAvailable = isSeachAvailable;
	}

	public bool HasSameContentWith(object other)
	{
		if (other is MultiSelectionInquiryData multiSelectionInquiryData)
		{
			bool flag = true;
			if (InquiryElements.Count == multiSelectionInquiryData.InquiryElements.Count)
			{
				for (int i = 0; i < InquiryElements.Count; i++)
				{
					if (!InquiryElements[i].HasSameContentWith(multiSelectionInquiryData.InquiryElements[i]))
					{
						flag = false;
					}
				}
			}
			else
			{
				flag = false;
			}
			if (TitleText == multiSelectionInquiryData.TitleText && DescriptionText == multiSelectionInquiryData.DescriptionText && flag && IsExitShown == multiSelectionInquiryData.IsExitShown && AffirmativeText == multiSelectionInquiryData.AffirmativeText && NegativeText == multiSelectionInquiryData.NegativeText && AffirmativeAction == multiSelectionInquiryData.AffirmativeAction && NegativeAction == multiSelectionInquiryData.NegativeAction && MinSelectableOptionCount == multiSelectionInquiryData.MinSelectableOptionCount && MaxSelectableOptionCount == multiSelectionInquiryData.MaxSelectableOptionCount)
			{
				return SoundEventPath == multiSelectionInquiryData.SoundEventPath;
			}
			return false;
		}
		return false;
	}
}
