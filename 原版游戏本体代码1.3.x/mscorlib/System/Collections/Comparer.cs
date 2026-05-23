using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;

namespace System.Collections
{
	// Token: 0x02000493 RID: 1171
	[ComVisible(true)]
	[Serializable]
	public sealed class Comparer : IComparer, ISerializable
	{
		// Token: 0x06003832 RID: 14386 RVA: 0x000D7CDF File Offset: 0x000D5EDF
		private Comparer()
		{
			this.m_compareInfo = null;
		}

		// Token: 0x06003833 RID: 14387 RVA: 0x000D7CEE File Offset: 0x000D5EEE
		public Comparer(CultureInfo culture)
		{
			if (culture == null)
			{
				throw new ArgumentNullException("culture");
			}
			this.m_compareInfo = culture.CompareInfo;
		}

		// Token: 0x06003834 RID: 14388 RVA: 0x000D7D10 File Offset: 0x000D5F10
		private Comparer(SerializationInfo info, StreamingContext context)
		{
			this.m_compareInfo = null;
			SerializationInfoEnumerator enumerator = info.GetEnumerator();
			while (enumerator.MoveNext())
			{
				string name = enumerator.Name;
				if (name == "CompareInfo")
				{
					this.m_compareInfo = (CompareInfo)info.GetValue("CompareInfo", typeof(CompareInfo));
				}
			}
		}

		// Token: 0x06003835 RID: 14389 RVA: 0x000D7D70 File Offset: 0x000D5F70
		public int Compare(object a, object b)
		{
			if (a == b)
			{
				return 0;
			}
			if (a == null)
			{
				return -1;
			}
			if (b == null)
			{
				return 1;
			}
			if (this.m_compareInfo != null)
			{
				string text = a as string;
				string text2 = b as string;
				if (text != null && text2 != null)
				{
					return this.m_compareInfo.Compare(text, text2);
				}
			}
			IComparable comparable = a as IComparable;
			if (comparable != null)
			{
				return comparable.CompareTo(b);
			}
			IComparable comparable2 = b as IComparable;
			if (comparable2 != null)
			{
				return -comparable2.CompareTo(a);
			}
			throw new ArgumentException(Environment.GetResourceString("Argument_ImplementIComparable"));
		}

		// Token: 0x06003836 RID: 14390 RVA: 0x000D7DEB File Offset: 0x000D5FEB
		[SecurityCritical]
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			if (this.m_compareInfo != null)
			{
				info.AddValue("CompareInfo", this.m_compareInfo);
			}
		}

		// Token: 0x040018D8 RID: 6360
		private CompareInfo m_compareInfo;

		// Token: 0x040018D9 RID: 6361
		public static readonly Comparer Default = new Comparer(CultureInfo.CurrentCulture);

		// Token: 0x040018DA RID: 6362
		public static readonly Comparer DefaultInvariant = new Comparer(CultureInfo.InvariantCulture);

		// Token: 0x040018DB RID: 6363
		private const string CompareInfoName = "CompareInfo";
	}
}
