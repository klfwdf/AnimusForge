using System;
using System.Collections.Generic;

namespace TaleWorlds.Core
{
	// Token: 0x020000C1 RID: 193
	public class MultiSelectionInquiryData
	{
		// Token: 0x06000AB3 RID: 2739 RVA: 0x0002277C File Offset: 0x0002097C
		public MultiSelectionInquiryData(string titleText, string descriptionText, List<InquiryElement> inquiryElements, bool isExitShown, int minSelectableOptionCount, int maxSelectableOptionCount, string affirmativeText, string negativeText, Action<List<InquiryElement>> affirmativeAction, Action<List<InquiryElement>> negativeAction, string soundEventPath = "", bool isSeachAvailable = false)
		{
			this.TitleText = titleText;
			this.DescriptionText = descriptionText;
			this.InquiryElements = inquiryElements;
			this.IsExitShown = isExitShown;
			this.AffirmativeText = affirmativeText;
			this.NegativeText = negativeText;
			this.AffirmativeAction = affirmativeAction;
			this.NegativeAction = negativeAction;
			this.MinSelectableOptionCount = minSelectableOptionCount;
			this.MaxSelectableOptionCount = maxSelectableOptionCount;
			this.SoundEventPath = soundEventPath;
			this.IsSeachAvailable = isSeachAvailable;
		}

		// Token: 0x06000AB4 RID: 2740 RVA: 0x000227EC File Offset: 0x000209EC
		public bool HasSameContentWith(object other)
		{
			MultiSelectionInquiryData multiSelectionInquiryData;
			if ((multiSelectionInquiryData = other as MultiSelectionInquiryData) != null)
			{
				bool flag = true;
				if (this.InquiryElements.Count == multiSelectionInquiryData.InquiryElements.Count)
				{
					for (int i = 0; i < this.InquiryElements.Count; i++)
					{
						if (!this.InquiryElements[i].HasSameContentWith(multiSelectionInquiryData.InquiryElements[i]))
						{
							flag = false;
						}
					}
				}
				else
				{
					flag = false;
				}
				return this.TitleText == multiSelectionInquiryData.TitleText && this.DescriptionText == multiSelectionInquiryData.DescriptionText && flag && this.IsExitShown == multiSelectionInquiryData.IsExitShown && this.AffirmativeText == multiSelectionInquiryData.AffirmativeText && this.NegativeText == multiSelectionInquiryData.NegativeText && this.AffirmativeAction == multiSelectionInquiryData.AffirmativeAction && this.NegativeAction == multiSelectionInquiryData.NegativeAction && this.MinSelectableOptionCount == multiSelectionInquiryData.MinSelectableOptionCount && this.MaxSelectableOptionCount == multiSelectionInquiryData.MaxSelectableOptionCount && this.SoundEventPath == multiSelectionInquiryData.SoundEventPath;
			}
			return false;
		}

		// Token: 0x040005F5 RID: 1525
		public readonly string TitleText;

		// Token: 0x040005F6 RID: 1526
		public readonly string DescriptionText;

		// Token: 0x040005F7 RID: 1527
		public readonly List<InquiryElement> InquiryElements;

		// Token: 0x040005F8 RID: 1528
		public readonly bool IsExitShown;

		// Token: 0x040005F9 RID: 1529
		public readonly int MaxSelectableOptionCount;

		// Token: 0x040005FA RID: 1530
		public readonly int MinSelectableOptionCount;

		// Token: 0x040005FB RID: 1531
		public readonly string SoundEventPath;

		// Token: 0x040005FC RID: 1532
		public readonly string AffirmativeText;

		// Token: 0x040005FD RID: 1533
		public readonly string NegativeText;

		// Token: 0x040005FE RID: 1534
		public readonly Action<List<InquiryElement>> AffirmativeAction;

		// Token: 0x040005FF RID: 1535
		public readonly Action<List<InquiryElement>> NegativeAction;

		// Token: 0x04000600 RID: 1536
		public readonly bool IsSeachAvailable;
	}
}
