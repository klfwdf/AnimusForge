using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security;
using System.Threading;

namespace System
{
	// Token: 0x02000109 RID: 265
	internal sealed class LocalDataStoreMgr
	{
		// Token: 0x06000FE3 RID: 4067 RVA: 0x000305DC File Offset: 0x0002E7DC
		[SecuritySafeCritical]
		public LocalDataStoreHolder CreateLocalDataStore()
		{
			LocalDataStore localDataStore = new LocalDataStore(this, this.m_SlotInfoTable.Length);
			LocalDataStoreHolder result = new LocalDataStoreHolder(localDataStore);
			bool flag = false;
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
				Monitor.Enter(this, ref flag);
				this.m_ManagedLocalDataStores.Add(localDataStore);
			}
			finally
			{
				if (flag)
				{
					Monitor.Exit(this);
				}
			}
			return result;
		}

		// Token: 0x06000FE4 RID: 4068 RVA: 0x00030638 File Offset: 0x0002E838
		[SecuritySafeCritical]
		public void DeleteLocalDataStore(LocalDataStore store)
		{
			bool flag = false;
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
				Monitor.Enter(this, ref flag);
				this.m_ManagedLocalDataStores.Remove(store);
			}
			finally
			{
				if (flag)
				{
					Monitor.Exit(this);
				}
			}
		}

		// Token: 0x06000FE5 RID: 4069 RVA: 0x00030680 File Offset: 0x0002E880
		[SecuritySafeCritical]
		public LocalDataStoreSlot AllocateDataSlot()
		{
			bool flag = false;
			RuntimeHelpers.PrepareConstrainedRegions();
			LocalDataStoreSlot result;
			try
			{
				Monitor.Enter(this, ref flag);
				int num = this.m_SlotInfoTable.Length;
				int num2 = this.m_FirstAvailableSlot;
				while (num2 < num && this.m_SlotInfoTable[num2])
				{
					num2++;
				}
				if (num2 >= num)
				{
					int num3;
					if (num < 512)
					{
						num3 = num * 2;
					}
					else
					{
						num3 = num + 128;
					}
					bool[] array = new bool[num3];
					Array.Copy(this.m_SlotInfoTable, array, num);
					this.m_SlotInfoTable = array;
				}
				this.m_SlotInfoTable[num2] = true;
				int slot = num2;
				long cookieGenerator = this.m_CookieGenerator;
				this.m_CookieGenerator = checked(cookieGenerator + 1L);
				LocalDataStoreSlot localDataStoreSlot = new LocalDataStoreSlot(this, slot, cookieGenerator);
				this.m_FirstAvailableSlot = num2 + 1;
				result = localDataStoreSlot;
			}
			finally
			{
				if (flag)
				{
					Monitor.Exit(this);
				}
			}
			return result;
		}

		// Token: 0x06000FE6 RID: 4070 RVA: 0x0003074C File Offset: 0x0002E94C
		[SecuritySafeCritical]
		public LocalDataStoreSlot AllocateNamedDataSlot(string name)
		{
			bool flag = false;
			RuntimeHelpers.PrepareConstrainedRegions();
			LocalDataStoreSlot result;
			try
			{
				Monitor.Enter(this, ref flag);
				LocalDataStoreSlot localDataStoreSlot = this.AllocateDataSlot();
				this.m_KeyToSlotMap.Add(name, localDataStoreSlot);
				result = localDataStoreSlot;
			}
			finally
			{
				if (flag)
				{
					Monitor.Exit(this);
				}
			}
			return result;
		}

		// Token: 0x06000FE7 RID: 4071 RVA: 0x0003079C File Offset: 0x0002E99C
		[SecuritySafeCritical]
		public LocalDataStoreSlot GetNamedDataSlot(string name)
		{
			bool flag = false;
			RuntimeHelpers.PrepareConstrainedRegions();
			LocalDataStoreSlot result;
			try
			{
				Monitor.Enter(this, ref flag);
				LocalDataStoreSlot valueOrDefault = this.m_KeyToSlotMap.GetValueOrDefault(name);
				if (valueOrDefault == null)
				{
					result = this.AllocateNamedDataSlot(name);
				}
				else
				{
					result = valueOrDefault;
				}
			}
			finally
			{
				if (flag)
				{
					Monitor.Exit(this);
				}
			}
			return result;
		}

		// Token: 0x06000FE8 RID: 4072 RVA: 0x000307F4 File Offset: 0x0002E9F4
		[SecuritySafeCritical]
		public void FreeNamedDataSlot(string name)
		{
			bool flag = false;
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
				Monitor.Enter(this, ref flag);
				this.m_KeyToSlotMap.Remove(name);
			}
			finally
			{
				if (flag)
				{
					Monitor.Exit(this);
				}
			}
		}

		// Token: 0x06000FE9 RID: 4073 RVA: 0x0003083C File Offset: 0x0002EA3C
		[SecuritySafeCritical]
		internal void FreeDataSlot(int slot, long cookie)
		{
			bool flag = false;
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
				Monitor.Enter(this, ref flag);
				for (int i = 0; i < this.m_ManagedLocalDataStores.Count; i++)
				{
					this.m_ManagedLocalDataStores[i].FreeData(slot, cookie);
				}
				this.m_SlotInfoTable[slot] = false;
				if (slot < this.m_FirstAvailableSlot)
				{
					this.m_FirstAvailableSlot = slot;
				}
			}
			finally
			{
				if (flag)
				{
					Monitor.Exit(this);
				}
			}
		}

		// Token: 0x06000FEA RID: 4074 RVA: 0x000308B8 File Offset: 0x0002EAB8
		public void ValidateSlot(LocalDataStoreSlot slot)
		{
			if (slot == null || slot.Manager != this)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_ALSInvalidSlot"));
			}
		}

		// Token: 0x06000FEB RID: 4075 RVA: 0x000308D6 File Offset: 0x0002EAD6
		internal int GetSlotTableLength()
		{
			return this.m_SlotInfoTable.Length;
		}

		// Token: 0x040005B2 RID: 1458
		private const int InitialSlotTableSize = 64;

		// Token: 0x040005B3 RID: 1459
		private const int SlotTableDoubleThreshold = 512;

		// Token: 0x040005B4 RID: 1460
		private const int LargeSlotTableSizeIncrease = 128;

		// Token: 0x040005B5 RID: 1461
		private bool[] m_SlotInfoTable = new bool[64];

		// Token: 0x040005B6 RID: 1462
		private int m_FirstAvailableSlot;

		// Token: 0x040005B7 RID: 1463
		private List<LocalDataStore> m_ManagedLocalDataStores = new List<LocalDataStore>();

		// Token: 0x040005B8 RID: 1464
		private Dictionary<string, LocalDataStoreSlot> m_KeyToSlotMap = new Dictionary<string, LocalDataStoreSlot>();

		// Token: 0x040005B9 RID: 1465
		private long m_CookieGenerator;
	}
}
