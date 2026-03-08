using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.Globalization
{
	// Token: 0x020003D1 RID: 977
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public class StringInfo
	{
		// Token: 0x0600316A RID: 12650 RVA: 0x000BDCB6 File Offset: 0x000BBEB6
		[__DynamicallyInvokable]
		public StringInfo()
			: this("")
		{
		}

		// Token: 0x0600316B RID: 12651 RVA: 0x000BDCC3 File Offset: 0x000BBEC3
		[__DynamicallyInvokable]
		public StringInfo(string value)
		{
			this.String = value;
		}

		// Token: 0x0600316C RID: 12652 RVA: 0x000BDCD2 File Offset: 0x000BBED2
		[OnDeserializing]
		private void OnDeserializing(StreamingContext ctx)
		{
			this.m_str = string.Empty;
		}

		// Token: 0x0600316D RID: 12653 RVA: 0x000BDCDF File Offset: 0x000BBEDF
		[OnDeserialized]
		private void OnDeserialized(StreamingContext ctx)
		{
			if (this.m_str.Length == 0)
			{
				this.m_indexes = null;
			}
		}

		// Token: 0x0600316E RID: 12654 RVA: 0x000BDCF8 File Offset: 0x000BBEF8
		[ComVisible(false)]
		[__DynamicallyInvokable]
		public override bool Equals(object value)
		{
			StringInfo stringInfo = value as StringInfo;
			return stringInfo != null && this.m_str.Equals(stringInfo.m_str);
		}

		// Token: 0x0600316F RID: 12655 RVA: 0x000BDD22 File Offset: 0x000BBF22
		[ComVisible(false)]
		[__DynamicallyInvokable]
		public override int GetHashCode()
		{
			return this.m_str.GetHashCode();
		}

		// Token: 0x170006E9 RID: 1769
		// (get) Token: 0x06003170 RID: 12656 RVA: 0x000BDD2F File Offset: 0x000BBF2F
		private int[] Indexes
		{
			get
			{
				if (this.m_indexes == null && 0 < this.String.Length)
				{
					this.m_indexes = StringInfo.ParseCombiningCharacters(this.String);
				}
				return this.m_indexes;
			}
		}

		// Token: 0x170006EA RID: 1770
		// (get) Token: 0x06003171 RID: 12657 RVA: 0x000BDD5E File Offset: 0x000BBF5E
		// (set) Token: 0x06003172 RID: 12658 RVA: 0x000BDD66 File Offset: 0x000BBF66
		[__DynamicallyInvokable]
		public string String
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_str;
			}
			[__DynamicallyInvokable]
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("String", Environment.GetResourceString("ArgumentNull_String"));
				}
				this.m_str = value;
				this.m_indexes = null;
			}
		}

		// Token: 0x170006EB RID: 1771
		// (get) Token: 0x06003173 RID: 12659 RVA: 0x000BDD8E File Offset: 0x000BBF8E
		[__DynamicallyInvokable]
		public int LengthInTextElements
		{
			[__DynamicallyInvokable]
			get
			{
				if (this.Indexes == null)
				{
					return 0;
				}
				return this.Indexes.Length;
			}
		}

		// Token: 0x06003174 RID: 12660 RVA: 0x000BDDA4 File Offset: 0x000BBFA4
		public string SubstringByTextElements(int startingTextElement)
		{
			if (this.Indexes != null)
			{
				return this.SubstringByTextElements(startingTextElement, this.Indexes.Length - startingTextElement);
			}
			if (startingTextElement < 0)
			{
				throw new ArgumentOutOfRangeException("startingTextElement", Environment.GetResourceString("ArgumentOutOfRange_NeedPosNum"));
			}
			throw new ArgumentOutOfRangeException("startingTextElement", Environment.GetResourceString("Arg_ArgumentOutOfRangeException"));
		}

		// Token: 0x06003175 RID: 12661 RVA: 0x000BDDF8 File Offset: 0x000BBFF8
		public string SubstringByTextElements(int startingTextElement, int lengthInTextElements)
		{
			if (startingTextElement < 0)
			{
				throw new ArgumentOutOfRangeException("startingTextElement", Environment.GetResourceString("ArgumentOutOfRange_NeedPosNum"));
			}
			if (this.String.Length == 0 || startingTextElement >= this.Indexes.Length)
			{
				throw new ArgumentOutOfRangeException("startingTextElement", Environment.GetResourceString("Arg_ArgumentOutOfRangeException"));
			}
			if (lengthInTextElements < 0)
			{
				throw new ArgumentOutOfRangeException("lengthInTextElements", Environment.GetResourceString("ArgumentOutOfRange_NeedPosNum"));
			}
			if (startingTextElement > this.Indexes.Length - lengthInTextElements)
			{
				throw new ArgumentOutOfRangeException("lengthInTextElements", Environment.GetResourceString("Arg_ArgumentOutOfRangeException"));
			}
			int num = this.Indexes[startingTextElement];
			if (startingTextElement + lengthInTextElements == this.Indexes.Length)
			{
				return this.String.Substring(num);
			}
			return this.String.Substring(num, this.Indexes[lengthInTextElements + startingTextElement] - num);
		}

		// Token: 0x06003176 RID: 12662 RVA: 0x000BDEC1 File Offset: 0x000BC0C1
		[__DynamicallyInvokable]
		public static string GetNextTextElement(string str)
		{
			return StringInfo.GetNextTextElement(str, 0);
		}

		// Token: 0x06003177 RID: 12663 RVA: 0x000BDECC File Offset: 0x000BC0CC
		internal static int GetCurrentTextElementLen(string str, int index, int len, ref UnicodeCategory ucCurrent, ref int currentCharCount)
		{
			if (index + currentCharCount == len)
			{
				return currentCharCount;
			}
			int num;
			UnicodeCategory unicodeCategory = CharUnicodeInfo.InternalGetUnicodeCategory(str, index + currentCharCount, out num);
			if (CharUnicodeInfo.IsCombiningCategory(unicodeCategory) && !CharUnicodeInfo.IsCombiningCategory(ucCurrent) && ucCurrent != UnicodeCategory.Format && ucCurrent != UnicodeCategory.Control && ucCurrent != UnicodeCategory.OtherNotAssigned && ucCurrent != UnicodeCategory.Surrogate)
			{
				int num2 = index;
				for (index += currentCharCount + num; index < len; index += num)
				{
					unicodeCategory = CharUnicodeInfo.InternalGetUnicodeCategory(str, index, out num);
					if (!CharUnicodeInfo.IsCombiningCategory(unicodeCategory))
					{
						ucCurrent = unicodeCategory;
						currentCharCount = num;
						break;
					}
				}
				return index - num2;
			}
			int result = currentCharCount;
			ucCurrent = unicodeCategory;
			currentCharCount = num;
			return result;
		}

		// Token: 0x06003178 RID: 12664 RVA: 0x000BDF60 File Offset: 0x000BC160
		[__DynamicallyInvokable]
		public static string GetNextTextElement(string str, int index)
		{
			if (str == null)
			{
				throw new ArgumentNullException("str");
			}
			int length = str.Length;
			if (index >= 0 && index < length)
			{
				int num;
				UnicodeCategory unicodeCategory = CharUnicodeInfo.InternalGetUnicodeCategory(str, index, out num);
				return str.Substring(index, StringInfo.GetCurrentTextElementLen(str, index, length, ref unicodeCategory, ref num));
			}
			if (index == length)
			{
				return string.Empty;
			}
			throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
		}

		// Token: 0x06003179 RID: 12665 RVA: 0x000BDFC6 File Offset: 0x000BC1C6
		[__DynamicallyInvokable]
		public static TextElementEnumerator GetTextElementEnumerator(string str)
		{
			return StringInfo.GetTextElementEnumerator(str, 0);
		}

		// Token: 0x0600317A RID: 12666 RVA: 0x000BDFD0 File Offset: 0x000BC1D0
		[__DynamicallyInvokable]
		public static TextElementEnumerator GetTextElementEnumerator(string str, int index)
		{
			if (str == null)
			{
				throw new ArgumentNullException("str");
			}
			int length = str.Length;
			if (index < 0 || index > length)
			{
				throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
			}
			return new TextElementEnumerator(str, index, length);
		}

		// Token: 0x0600317B RID: 12667 RVA: 0x000BE018 File Offset: 0x000BC218
		[__DynamicallyInvokable]
		public static int[] ParseCombiningCharacters(string str)
		{
			if (str == null)
			{
				throw new ArgumentNullException("str");
			}
			int length = str.Length;
			int[] array = new int[length];
			if (length == 0)
			{
				return array;
			}
			int num = 0;
			int i = 0;
			int num2;
			UnicodeCategory unicodeCategory = CharUnicodeInfo.InternalGetUnicodeCategory(str, 0, out num2);
			while (i < length)
			{
				array[num++] = i;
				i += StringInfo.GetCurrentTextElementLen(str, i, length, ref unicodeCategory, ref num2);
			}
			if (num < length)
			{
				int[] array2 = new int[num];
				Array.Copy(array, array2, num);
				return array2;
			}
			return array;
		}

		// Token: 0x0400151F RID: 5407
		[OptionalField(VersionAdded = 2)]
		private string m_str;

		// Token: 0x04001520 RID: 5408
		[NonSerialized]
		private int[] m_indexes;
	}
}
