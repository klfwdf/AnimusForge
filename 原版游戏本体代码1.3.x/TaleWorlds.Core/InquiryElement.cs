using System;
using TaleWorlds.Core.ImageIdentifiers;

namespace TaleWorlds.Core
{
	// Token: 0x02000090 RID: 144
	public class InquiryElement
	{
		// Token: 0x060008AA RID: 2218 RVA: 0x0001CDE7 File Offset: 0x0001AFE7
		public InquiryElement(object identifier, string title, ImageIdentifier imageIdentifier)
		{
			this.Identifier = identifier;
			this.Title = title;
			this.ImageIdentifier = imageIdentifier;
			this.IsEnabled = true;
			this.Hint = null;
		}

		// Token: 0x060008AB RID: 2219 RVA: 0x0001CE12 File Offset: 0x0001B012
		public InquiryElement(object identifier, string title, ImageIdentifier imageIdentifier, bool isEnabled, string hint)
		{
			this.Identifier = identifier;
			this.Title = title;
			this.ImageIdentifier = imageIdentifier;
			this.IsEnabled = isEnabled;
			this.Hint = hint;
		}

		// Token: 0x060008AC RID: 2220 RVA: 0x0001CE40 File Offset: 0x0001B040
		public bool HasSameContentWith(object other)
		{
			InquiryElement inquiryElement;
			if ((inquiryElement = other as InquiryElement) != null)
			{
				if (this.Title == inquiryElement.Title)
				{
					if (this.ImageIdentifier != null || inquiryElement.ImageIdentifier != null)
					{
						ImageIdentifier imageIdentifier = this.ImageIdentifier;
						if (imageIdentifier == null || !imageIdentifier.Equals(inquiryElement.ImageIdentifier))
						{
							return false;
						}
					}
					if (this.Identifier == inquiryElement.Identifier && this.IsEnabled == inquiryElement.IsEnabled)
					{
						return this.Hint == inquiryElement.Hint;
					}
				}
				return false;
			}
			return false;
		}

		// Token: 0x04000459 RID: 1113
		public readonly string Title;

		// Token: 0x0400045A RID: 1114
		public readonly ImageIdentifier ImageIdentifier;

		// Token: 0x0400045B RID: 1115
		public readonly object Identifier;

		// Token: 0x0400045C RID: 1116
		public readonly bool IsEnabled;

		// Token: 0x0400045D RID: 1117
		public readonly string Hint;
	}
}
