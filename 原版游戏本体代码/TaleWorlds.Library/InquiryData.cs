using System;

namespace TaleWorlds.Library
{
	// Token: 0x0200003D RID: 61
	public class InquiryData
	{
		// Token: 0x060001FD RID: 509 RVA: 0x00007770 File Offset: 0x00005970
		public InquiryData(string titleText, string text, bool isAffirmativeOptionShown, bool isNegativeOptionShown, string affirmativeText, string negativeText, Action affirmativeAction, Action negativeAction, string soundEventPath = "", float expireTime = 0f, Action timeoutAction = null, Func<ValueTuple<bool, string>> isAffirmativeOptionEnabled = null, Func<ValueTuple<bool, string>> isNegativeOptionEnabled = null)
		{
			this.TitleText = titleText;
			this.Text = text;
			this.IsAffirmativeOptionShown = isAffirmativeOptionShown;
			this.IsNegativeOptionShown = isNegativeOptionShown;
			this.GetIsAffirmativeOptionEnabled = isAffirmativeOptionEnabled;
			this.GetIsNegativeOptionEnabled = isNegativeOptionEnabled;
			this.AffirmativeText = affirmativeText;
			this.NegativeText = negativeText;
			this.AffirmativeAction = affirmativeAction;
			this.NegativeAction = negativeAction;
			this.SoundEventPath = soundEventPath;
			this.ExpireTime = expireTime;
			this.TimeoutAction = timeoutAction;
		}

		// Token: 0x060001FE RID: 510 RVA: 0x000077E8 File Offset: 0x000059E8
		public void SetText(string text)
		{
			this.Text = text;
		}

		// Token: 0x060001FF RID: 511 RVA: 0x000077F1 File Offset: 0x000059F1
		public void SetTitleText(string titleText)
		{
			this.TitleText = titleText;
		}

		// Token: 0x06000200 RID: 512 RVA: 0x000077FC File Offset: 0x000059FC
		public bool HasSameContentWith(object other)
		{
			InquiryData inquiryData;
			return (inquiryData = other as InquiryData) != null && (this.TitleText == inquiryData.TitleText && this.Text == inquiryData.Text && this.IsAffirmativeOptionShown == inquiryData.IsAffirmativeOptionShown && this.IsNegativeOptionShown == inquiryData.IsNegativeOptionShown && this.GetIsAffirmativeOptionEnabled == inquiryData.GetIsAffirmativeOptionEnabled && this.GetIsNegativeOptionEnabled == inquiryData.GetIsNegativeOptionEnabled && this.AffirmativeText == inquiryData.AffirmativeText && this.NegativeText == inquiryData.NegativeText && this.AffirmativeAction == inquiryData.AffirmativeAction && this.NegativeAction == inquiryData.NegativeAction && this.SoundEventPath == inquiryData.SoundEventPath && this.ExpireTime == inquiryData.ExpireTime) && this.TimeoutAction == inquiryData.TimeoutAction;
		}

		// Token: 0x040000BC RID: 188
		public string TitleText;

		// Token: 0x040000BD RID: 189
		public string Text;

		// Token: 0x040000BE RID: 190
		public readonly float ExpireTime;

		// Token: 0x040000BF RID: 191
		public readonly bool IsAffirmativeOptionShown;

		// Token: 0x040000C0 RID: 192
		public readonly bool IsNegativeOptionShown;

		// Token: 0x040000C1 RID: 193
		public readonly string AffirmativeText;

		// Token: 0x040000C2 RID: 194
		public readonly string NegativeText;

		// Token: 0x040000C3 RID: 195
		public readonly string SoundEventPath;

		// Token: 0x040000C4 RID: 196
		public readonly Action AffirmativeAction;

		// Token: 0x040000C5 RID: 197
		public readonly Action NegativeAction;

		// Token: 0x040000C6 RID: 198
		public readonly Action TimeoutAction;

		// Token: 0x040000C7 RID: 199
		public readonly Func<ValueTuple<bool, string>> GetIsAffirmativeOptionEnabled;

		// Token: 0x040000C8 RID: 200
		public readonly Func<ValueTuple<bool, string>> GetIsNegativeOptionEnabled;
	}
}
