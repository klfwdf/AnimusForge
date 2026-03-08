using System;
using System.Collections;

namespace System.Security.AccessControl
{
	// Token: 0x0200020B RID: 523
	public abstract class GenericAcl : ICollection, IEnumerable
	{
		// Token: 0x1700037C RID: 892
		// (get) Token: 0x06001E8B RID: 7819
		public abstract byte Revision { get; }

		// Token: 0x1700037D RID: 893
		// (get) Token: 0x06001E8C RID: 7820
		public abstract int BinaryLength { get; }

		// Token: 0x1700037E RID: 894
		public abstract GenericAce this[int index] { get; set; }

		// Token: 0x06001E8F RID: 7823
		public abstract void GetBinaryForm(byte[] binaryForm, int offset);

		// Token: 0x06001E90 RID: 7824 RVA: 0x0006AD74 File Offset: 0x00068F74
		void ICollection.CopyTo(Array array, int index)
		{
			if (array == null)
			{
				throw new ArgumentNullException("array");
			}
			if (array.Rank != 1)
			{
				throw new RankException(Environment.GetResourceString("Rank_MultiDimNotSupported"));
			}
			if (index < 0)
			{
				throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (array.Length - index < this.Count)
			{
				throw new ArgumentOutOfRangeException("array", Environment.GetResourceString("ArgumentOutOfRange_ArrayTooSmall"));
			}
			for (int i = 0; i < this.Count; i++)
			{
				array.SetValue(this[i], index + i);
			}
		}

		// Token: 0x06001E91 RID: 7825 RVA: 0x0006AE07 File Offset: 0x00069007
		public void CopyTo(GenericAce[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}

		// Token: 0x1700037F RID: 895
		// (get) Token: 0x06001E92 RID: 7826
		public abstract int Count { get; }

		// Token: 0x17000380 RID: 896
		// (get) Token: 0x06001E93 RID: 7827 RVA: 0x0006AE11 File Offset: 0x00069011
		public bool IsSynchronized
		{
			get
			{
				return false;
			}
		}

		// Token: 0x17000381 RID: 897
		// (get) Token: 0x06001E94 RID: 7828 RVA: 0x0006AE14 File Offset: 0x00069014
		public virtual object SyncRoot
		{
			get
			{
				return this;
			}
		}

		// Token: 0x06001E95 RID: 7829 RVA: 0x0006AE17 File Offset: 0x00069017
		IEnumerator IEnumerable.GetEnumerator()
		{
			return new AceEnumerator(this);
		}

		// Token: 0x06001E96 RID: 7830 RVA: 0x0006AE1F File Offset: 0x0006901F
		public AceEnumerator GetEnumerator()
		{
			return ((IEnumerable)this).GetEnumerator() as AceEnumerator;
		}

		// Token: 0x04000B06 RID: 2822
		public static readonly byte AclRevision = 2;

		// Token: 0x04000B07 RID: 2823
		public static readonly byte AclRevisionDS = 4;

		// Token: 0x04000B08 RID: 2824
		public static readonly int MaxBinaryLength = 65535;

		// Token: 0x04000B09 RID: 2825
		internal const int HeaderLength = 8;
	}
}
