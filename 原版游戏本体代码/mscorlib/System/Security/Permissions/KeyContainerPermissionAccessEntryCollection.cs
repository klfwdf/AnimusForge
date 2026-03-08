using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace System.Security.Permissions
{
	// Token: 0x02000316 RID: 790
	[ComVisible(true)]
	[Serializable]
	public sealed class KeyContainerPermissionAccessEntryCollection : ICollection, IEnumerable
	{
		// Token: 0x060027CD RID: 10189 RVA: 0x00090CC1 File Offset: 0x0008EEC1
		private KeyContainerPermissionAccessEntryCollection()
		{
		}

		// Token: 0x060027CE RID: 10190 RVA: 0x00090CC9 File Offset: 0x0008EEC9
		internal KeyContainerPermissionAccessEntryCollection(KeyContainerPermissionFlags globalFlags)
		{
			this.m_list = new ArrayList();
			this.m_globalFlags = globalFlags;
		}

		// Token: 0x17000516 RID: 1302
		public KeyContainerPermissionAccessEntry this[int index]
		{
			get
			{
				if (index < 0)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumNotStarted"));
				}
				if (index >= this.Count)
				{
					throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
				}
				return (KeyContainerPermissionAccessEntry)this.m_list[index];
			}
		}

		// Token: 0x17000517 RID: 1303
		// (get) Token: 0x060027D0 RID: 10192 RVA: 0x00090D34 File Offset: 0x0008EF34
		public int Count
		{
			get
			{
				return this.m_list.Count;
			}
		}

		// Token: 0x060027D1 RID: 10193 RVA: 0x00090D44 File Offset: 0x0008EF44
		public int Add(KeyContainerPermissionAccessEntry accessEntry)
		{
			if (accessEntry == null)
			{
				throw new ArgumentNullException("accessEntry");
			}
			int num = this.m_list.IndexOf(accessEntry);
			if (num != -1)
			{
				((KeyContainerPermissionAccessEntry)this.m_list[num]).Flags &= accessEntry.Flags;
				return num;
			}
			if (accessEntry.Flags != this.m_globalFlags)
			{
				return this.m_list.Add(accessEntry);
			}
			return -1;
		}

		// Token: 0x060027D2 RID: 10194 RVA: 0x00090DB1 File Offset: 0x0008EFB1
		public void Clear()
		{
			this.m_list.Clear();
		}

		// Token: 0x060027D3 RID: 10195 RVA: 0x00090DBE File Offset: 0x0008EFBE
		public int IndexOf(KeyContainerPermissionAccessEntry accessEntry)
		{
			return this.m_list.IndexOf(accessEntry);
		}

		// Token: 0x060027D4 RID: 10196 RVA: 0x00090DCC File Offset: 0x0008EFCC
		public void Remove(KeyContainerPermissionAccessEntry accessEntry)
		{
			if (accessEntry == null)
			{
				throw new ArgumentNullException("accessEntry");
			}
			this.m_list.Remove(accessEntry);
		}

		// Token: 0x060027D5 RID: 10197 RVA: 0x00090DE8 File Offset: 0x0008EFE8
		public KeyContainerPermissionAccessEntryEnumerator GetEnumerator()
		{
			return new KeyContainerPermissionAccessEntryEnumerator(this);
		}

		// Token: 0x060027D6 RID: 10198 RVA: 0x00090DF0 File Offset: 0x0008EFF0
		IEnumerator IEnumerable.GetEnumerator()
		{
			return new KeyContainerPermissionAccessEntryEnumerator(this);
		}

		// Token: 0x060027D7 RID: 10199 RVA: 0x00090DF8 File Offset: 0x0008EFF8
		void ICollection.CopyTo(Array array, int index)
		{
			if (array == null)
			{
				throw new ArgumentNullException("array");
			}
			if (array.Rank != 1)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_RankMultiDimNotSupported"));
			}
			if (index < 0 || index >= array.Length)
			{
				throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
			}
			if (index + this.Count > array.Length)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
			}
			for (int i = 0; i < this.Count; i++)
			{
				array.SetValue(this[i], index);
				index++;
			}
		}

		// Token: 0x060027D8 RID: 10200 RVA: 0x00090E92 File Offset: 0x0008F092
		public void CopyTo(KeyContainerPermissionAccessEntry[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}

		// Token: 0x17000518 RID: 1304
		// (get) Token: 0x060027D9 RID: 10201 RVA: 0x00090E9C File Offset: 0x0008F09C
		public bool IsSynchronized
		{
			get
			{
				return false;
			}
		}

		// Token: 0x17000519 RID: 1305
		// (get) Token: 0x060027DA RID: 10202 RVA: 0x00090E9F File Offset: 0x0008F09F
		public object SyncRoot
		{
			get
			{
				return this;
			}
		}

		// Token: 0x04000F70 RID: 3952
		private ArrayList m_list;

		// Token: 0x04000F71 RID: 3953
		private KeyContainerPermissionFlags m_globalFlags;
	}
}
