using System;
using TaleWorlds.Library;

namespace TaleWorlds.Core
{
	// Token: 0x020000AC RID: 172
	public class MBFastRandomSelector<T>
	{
		// Token: 0x1700031B RID: 795
		// (get) Token: 0x06000913 RID: 2323 RVA: 0x0001DC4E File Offset: 0x0001BE4E
		// (set) Token: 0x06000914 RID: 2324 RVA: 0x0001DC56 File Offset: 0x0001BE56
		public ushort RemainingCount { get; private set; }

		// Token: 0x06000915 RID: 2325 RVA: 0x0001DC5F File Offset: 0x0001BE5F
		public MBFastRandomSelector(ushort capacity = 32)
		{
			this.ReallocateIndexArray(capacity);
			this._list = null;
		}

		// Token: 0x06000916 RID: 2326 RVA: 0x0001DC75 File Offset: 0x0001BE75
		public MBFastRandomSelector(MBReadOnlyList<T> list, ushort capacity = 32)
		{
			this.ReallocateIndexArray(capacity);
			this.Initialize(list);
		}

		// Token: 0x06000917 RID: 2327 RVA: 0x0001DC8C File Offset: 0x0001BE8C
		public void Initialize(MBReadOnlyList<T> list)
		{
			if (list != null && list.Count <= 65535)
			{
				this._list = list;
				this.TryExpand();
			}
			else
			{
				Debug.FailedAssert("Cannot initialize random selector as passed list is null or it exceeds " + ushort.MaxValue + " elements).", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\MBFastRandomSelector.cs", "Initialize", 63);
				this._list = null;
			}
			this.Reset();
		}

		// Token: 0x06000918 RID: 2328 RVA: 0x0001DCF0 File Offset: 0x0001BEF0
		public void Reset()
		{
			if (this._list != null)
			{
				if (this._currentVersion < 65535)
				{
					this._currentVersion += 1;
				}
				else
				{
					for (int i = 0; i < this._indexArray.Length; i++)
					{
						this._indexArray[i] = default(MBFastRandomSelector<T>.IndexEntry);
					}
					this._currentVersion = 1;
				}
				this.RemainingCount = (ushort)this._list.Count;
				return;
			}
			this._currentVersion = 1;
			this.RemainingCount = 0;
		}

		// Token: 0x06000919 RID: 2329 RVA: 0x0001DD70 File Offset: 0x0001BF70
		public void Pack()
		{
			if (this._list != null)
			{
				ushort num = (ushort)MathF.Max(32, this._list.Count);
				if (this._indexArray.Length != (int)num)
				{
					this.ReallocateIndexArray(num);
					return;
				}
			}
			else if (this._indexArray.Length != 32)
			{
				this.ReallocateIndexArray(32);
			}
		}

		// Token: 0x0600091A RID: 2330 RVA: 0x0001DDC0 File Offset: 0x0001BFC0
		public bool SelectRandom(out T selection, Predicate<T> conditions = null)
		{
			selection = default(T);
			if (this._list == null)
			{
				return false;
			}
			bool flag = false;
			while (this.RemainingCount > 0 && !flag)
			{
				ushort num = (ushort)MBRandom.RandomInt((int)this.RemainingCount);
				ushort num2 = this.RemainingCount - 1;
				MBFastRandomSelector<T>.IndexEntry indexEntry = this._indexArray[(int)num];
				T t = ((indexEntry.Version == this._currentVersion) ? this._list[(int)indexEntry.Index] : this._list[(int)num]);
				if (conditions == null || conditions(t))
				{
					flag = true;
					selection = t;
				}
				MBFastRandomSelector<T>.IndexEntry indexEntry2 = this._indexArray[(int)num2];
				this._indexArray[(int)num] = ((indexEntry2.Version == this._currentVersion) ? new MBFastRandomSelector<T>.IndexEntry(indexEntry2.Index, this._currentVersion) : new MBFastRandomSelector<T>.IndexEntry(num2, this._currentVersion));
				ushort remainingCount = this.RemainingCount;
				this.RemainingCount = remainingCount - 1;
			}
			return flag;
		}

		// Token: 0x0600091B RID: 2331 RVA: 0x0001DEBC File Offset: 0x0001C0BC
		private void TryExpand()
		{
			if (this._indexArray.Length >= this._list.Count)
			{
				return;
			}
			ushort capacity = (ushort)(this._list.Count * 2);
			this.ReallocateIndexArray(capacity);
		}

		// Token: 0x0600091C RID: 2332 RVA: 0x0001DEF5 File Offset: 0x0001C0F5
		private void ReallocateIndexArray(ushort capacity)
		{
			capacity = (ushort)MBMath.ClampInt((int)capacity, 32, 65535);
			this._indexArray = new MBFastRandomSelector<T>.IndexEntry[(int)capacity];
			this._currentVersion = 1;
		}

		// Token: 0x04000517 RID: 1303
		public const ushort MinimumCapacity = 32;

		// Token: 0x04000518 RID: 1304
		public const ushort MaximumCapacity = 65535;

		// Token: 0x04000519 RID: 1305
		private const ushort InitialVersion = 1;

		// Token: 0x0400051A RID: 1306
		private const ushort MaximumVersion = 65535;

		// Token: 0x0400051C RID: 1308
		private MBReadOnlyList<T> _list;

		// Token: 0x0400051D RID: 1309
		private MBFastRandomSelector<T>.IndexEntry[] _indexArray;

		// Token: 0x0400051E RID: 1310
		private ushort _currentVersion;

		// Token: 0x0200011F RID: 287
		public struct IndexEntry
		{
			// Token: 0x06000C04 RID: 3076 RVA: 0x000263D7 File Offset: 0x000245D7
			public IndexEntry(ushort index, ushort version)
			{
				this.Index = index;
				this.Version = version;
			}

			// Token: 0x040007B9 RID: 1977
			public ushort Index;

			// Token: 0x040007BA RID: 1978
			public ushort Version;
		}
	}
}
