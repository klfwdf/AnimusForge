using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.Globalization
{
	// Token: 0x020003D3 RID: 979
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public class TextElementEnumerator : IEnumerator
	{
		// Token: 0x06003198 RID: 12696 RVA: 0x000BE370 File Offset: 0x000BC570
		internal TextElementEnumerator(string str, int startIndex, int strLen)
		{
			this.str = str;
			this.startIndex = startIndex;
			this.strLen = strLen;
			this.Reset();
		}

		// Token: 0x06003199 RID: 12697 RVA: 0x000BE393 File Offset: 0x000BC593
		[OnDeserializing]
		private void OnDeserializing(StreamingContext ctx)
		{
			this.charLen = -1;
		}

		// Token: 0x0600319A RID: 12698 RVA: 0x000BE39C File Offset: 0x000BC59C
		[OnDeserialized]
		private void OnDeserialized(StreamingContext ctx)
		{
			this.strLen = this.endIndex + 1;
			this.currTextElementLen = this.nextTextElementLen;
			if (this.charLen == -1)
			{
				this.uc = CharUnicodeInfo.InternalGetUnicodeCategory(this.str, this.index, out this.charLen);
			}
		}

		// Token: 0x0600319B RID: 12699 RVA: 0x000BE3E9 File Offset: 0x000BC5E9
		[OnSerializing]
		private void OnSerializing(StreamingContext ctx)
		{
			this.endIndex = this.strLen - 1;
			this.nextTextElementLen = this.currTextElementLen;
		}

		// Token: 0x0600319C RID: 12700 RVA: 0x000BE408 File Offset: 0x000BC608
		[__DynamicallyInvokable]
		public bool MoveNext()
		{
			if (this.index >= this.strLen)
			{
				this.index = this.strLen + 1;
				return false;
			}
			this.currTextElementLen = StringInfo.GetCurrentTextElementLen(this.str, this.index, this.strLen, ref this.uc, ref this.charLen);
			this.index += this.currTextElementLen;
			return true;
		}

		// Token: 0x170006F2 RID: 1778
		// (get) Token: 0x0600319D RID: 12701 RVA: 0x000BE470 File Offset: 0x000BC670
		[__DynamicallyInvokable]
		public object Current
		{
			[__DynamicallyInvokable]
			get
			{
				return this.GetTextElement();
			}
		}

		// Token: 0x0600319E RID: 12702 RVA: 0x000BE478 File Offset: 0x000BC678
		[__DynamicallyInvokable]
		public string GetTextElement()
		{
			if (this.index == this.startIndex)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumNotStarted"));
			}
			if (this.index > this.strLen)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumEnded"));
			}
			return this.str.Substring(this.index - this.currTextElementLen, this.currTextElementLen);
		}

		// Token: 0x170006F3 RID: 1779
		// (get) Token: 0x0600319F RID: 12703 RVA: 0x000BE4DF File Offset: 0x000BC6DF
		[__DynamicallyInvokable]
		public int ElementIndex
		{
			[__DynamicallyInvokable]
			get
			{
				if (this.index == this.startIndex)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumNotStarted"));
				}
				return this.index - this.currTextElementLen;
			}
		}

		// Token: 0x060031A0 RID: 12704 RVA: 0x000BE50C File Offset: 0x000BC70C
		[__DynamicallyInvokable]
		public void Reset()
		{
			this.index = this.startIndex;
			if (this.index < this.strLen)
			{
				this.uc = CharUnicodeInfo.InternalGetUnicodeCategory(this.str, this.index, out this.charLen);
			}
		}

		// Token: 0x04001526 RID: 5414
		private string str;

		// Token: 0x04001527 RID: 5415
		private int index;

		// Token: 0x04001528 RID: 5416
		private int startIndex;

		// Token: 0x04001529 RID: 5417
		[NonSerialized]
		private int strLen;

		// Token: 0x0400152A RID: 5418
		[NonSerialized]
		private int currTextElementLen;

		// Token: 0x0400152B RID: 5419
		[OptionalField(VersionAdded = 2)]
		private UnicodeCategory uc;

		// Token: 0x0400152C RID: 5420
		[OptionalField(VersionAdded = 2)]
		private int charLen;

		// Token: 0x0400152D RID: 5421
		private int endIndex;

		// Token: 0x0400152E RID: 5422
		private int nextTextElementLen;
	}
}
