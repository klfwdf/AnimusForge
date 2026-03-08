using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System
{
	// Token: 0x02000075 RID: 117
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public abstract class StringComparer : IComparer, IEqualityComparer, IComparer<string>, IEqualityComparer<string>
	{
		// Token: 0x17000087 RID: 135
		// (get) Token: 0x06000576 RID: 1398 RVA: 0x000138B7 File Offset: 0x00011AB7
		public static StringComparer InvariantCulture
		{
			get
			{
				return StringComparer._invariantCulture;
			}
		}

		// Token: 0x17000088 RID: 136
		// (get) Token: 0x06000577 RID: 1399 RVA: 0x000138BE File Offset: 0x00011ABE
		public static StringComparer InvariantCultureIgnoreCase
		{
			get
			{
				return StringComparer._invariantCultureIgnoreCase;
			}
		}

		// Token: 0x17000089 RID: 137
		// (get) Token: 0x06000578 RID: 1400 RVA: 0x000138C5 File Offset: 0x00011AC5
		[__DynamicallyInvokable]
		public static StringComparer CurrentCulture
		{
			[__DynamicallyInvokable]
			get
			{
				return new CultureAwareComparer(CultureInfo.CurrentCulture, false);
			}
		}

		// Token: 0x1700008A RID: 138
		// (get) Token: 0x06000579 RID: 1401 RVA: 0x000138D2 File Offset: 0x00011AD2
		[__DynamicallyInvokable]
		public static StringComparer CurrentCultureIgnoreCase
		{
			[__DynamicallyInvokable]
			get
			{
				return new CultureAwareComparer(CultureInfo.CurrentCulture, true);
			}
		}

		// Token: 0x1700008B RID: 139
		// (get) Token: 0x0600057A RID: 1402 RVA: 0x000138DF File Offset: 0x00011ADF
		[__DynamicallyInvokable]
		public static StringComparer Ordinal
		{
			[__DynamicallyInvokable]
			get
			{
				return StringComparer._ordinal;
			}
		}

		// Token: 0x1700008C RID: 140
		// (get) Token: 0x0600057B RID: 1403 RVA: 0x000138E6 File Offset: 0x00011AE6
		[__DynamicallyInvokable]
		public static StringComparer OrdinalIgnoreCase
		{
			[__DynamicallyInvokable]
			get
			{
				return StringComparer._ordinalIgnoreCase;
			}
		}

		// Token: 0x0600057C RID: 1404 RVA: 0x000138ED File Offset: 0x00011AED
		[__DynamicallyInvokable]
		public static StringComparer Create(CultureInfo culture, bool ignoreCase)
		{
			if (culture == null)
			{
				throw new ArgumentNullException("culture");
			}
			return new CultureAwareComparer(culture, ignoreCase);
		}

		// Token: 0x0600057D RID: 1405 RVA: 0x00013904 File Offset: 0x00011B04
		public int Compare(object x, object y)
		{
			if (x == y)
			{
				return 0;
			}
			if (x == null)
			{
				return -1;
			}
			if (y == null)
			{
				return 1;
			}
			string text = x as string;
			if (text != null)
			{
				string text2 = y as string;
				if (text2 != null)
				{
					return this.Compare(text, text2);
				}
			}
			IComparable comparable = x as IComparable;
			if (comparable != null)
			{
				return comparable.CompareTo(y);
			}
			throw new ArgumentException(Environment.GetResourceString("Argument_ImplementIComparable"));
		}

		// Token: 0x0600057E RID: 1406 RVA: 0x00013960 File Offset: 0x00011B60
		public bool Equals(object x, object y)
		{
			if (x == y)
			{
				return true;
			}
			if (x == null || y == null)
			{
				return false;
			}
			string text = x as string;
			if (text != null)
			{
				string text2 = y as string;
				if (text2 != null)
				{
					return this.Equals(text, text2);
				}
			}
			return x.Equals(y);
		}

		// Token: 0x0600057F RID: 1407 RVA: 0x000139A0 File Offset: 0x00011BA0
		public int GetHashCode(object obj)
		{
			if (obj == null)
			{
				throw new ArgumentNullException("obj");
			}
			string text = obj as string;
			if (text != null)
			{
				return this.GetHashCode(text);
			}
			return obj.GetHashCode();
		}

		// Token: 0x06000580 RID: 1408
		[__DynamicallyInvokable]
		public abstract int Compare(string x, string y);

		// Token: 0x06000581 RID: 1409
		[__DynamicallyInvokable]
		public abstract bool Equals(string x, string y);

		// Token: 0x06000582 RID: 1410
		[__DynamicallyInvokable]
		public abstract int GetHashCode(string obj);

		// Token: 0x06000583 RID: 1411 RVA: 0x000139D3 File Offset: 0x00011BD3
		[__DynamicallyInvokable]
		protected StringComparer()
		{
		}

		// Token: 0x0400028E RID: 654
		private static readonly StringComparer _invariantCulture = new CultureAwareComparer(CultureInfo.InvariantCulture, false);

		// Token: 0x0400028F RID: 655
		private static readonly StringComparer _invariantCultureIgnoreCase = new CultureAwareComparer(CultureInfo.InvariantCulture, true);

		// Token: 0x04000290 RID: 656
		private static readonly StringComparer _ordinal = new OrdinalComparer(false);

		// Token: 0x04000291 RID: 657
		private static readonly StringComparer _ordinalIgnoreCase = new OrdinalComparer(true);
	}
}
