using System;
using System.Runtime.CompilerServices;
using System.Security;
using System.Threading;

namespace System
{
	// Token: 0x02000107 RID: 263
	internal sealed class LocalDataStore
	{
		// Token: 0x06000FD8 RID: 4056 RVA: 0x00030360 File Offset: 0x0002E560
		public LocalDataStore(LocalDataStoreMgr mgr, int InitialCapacity)
		{
			this.m_Manager = mgr;
			this.m_DataTable = new LocalDataStoreElement[InitialCapacity];
		}

		// Token: 0x06000FD9 RID: 4057 RVA: 0x0003037B File Offset: 0x0002E57B
		internal void Dispose()
		{
			this.m_Manager.DeleteLocalDataStore(this);
		}

		// Token: 0x06000FDA RID: 4058 RVA: 0x0003038C File Offset: 0x0002E58C
		public object GetData(LocalDataStoreSlot slot)
		{
			this.m_Manager.ValidateSlot(slot);
			int slot2 = slot.Slot;
			if (slot2 >= 0)
			{
				if (slot2 >= this.m_DataTable.Length)
				{
					return null;
				}
				LocalDataStoreElement localDataStoreElement = this.m_DataTable[slot2];
				if (localDataStoreElement == null)
				{
					return null;
				}
				if (localDataStoreElement.Cookie == slot.Cookie)
				{
					return localDataStoreElement.Value;
				}
			}
			throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_SlotHasBeenFreed"));
		}

		// Token: 0x06000FDB RID: 4059 RVA: 0x000303F0 File Offset: 0x0002E5F0
		public void SetData(LocalDataStoreSlot slot, object data)
		{
			this.m_Manager.ValidateSlot(slot);
			int slot2 = slot.Slot;
			if (slot2 >= 0)
			{
				LocalDataStoreElement localDataStoreElement = ((slot2 < this.m_DataTable.Length) ? this.m_DataTable[slot2] : null);
				if (localDataStoreElement == null)
				{
					localDataStoreElement = this.PopulateElement(slot);
				}
				if (localDataStoreElement.Cookie == slot.Cookie)
				{
					localDataStoreElement.Value = data;
					return;
				}
			}
			throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_SlotHasBeenFreed"));
		}

		// Token: 0x06000FDC RID: 4060 RVA: 0x0003045C File Offset: 0x0002E65C
		internal void FreeData(int slot, long cookie)
		{
			if (slot >= this.m_DataTable.Length)
			{
				return;
			}
			LocalDataStoreElement localDataStoreElement = this.m_DataTable[slot];
			if (localDataStoreElement != null && localDataStoreElement.Cookie == cookie)
			{
				this.m_DataTable[slot] = null;
			}
		}

		// Token: 0x06000FDD RID: 4061 RVA: 0x00030494 File Offset: 0x0002E694
		[SecuritySafeCritical]
		private LocalDataStoreElement PopulateElement(LocalDataStoreSlot slot)
		{
			bool flag = false;
			RuntimeHelpers.PrepareConstrainedRegions();
			LocalDataStoreElement result;
			try
			{
				Monitor.Enter(this.m_Manager, ref flag);
				int slot2 = slot.Slot;
				if (slot2 < 0)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_SlotHasBeenFreed"));
				}
				if (slot2 >= this.m_DataTable.Length)
				{
					int slotTableLength = this.m_Manager.GetSlotTableLength();
					LocalDataStoreElement[] array = new LocalDataStoreElement[slotTableLength];
					Array.Copy(this.m_DataTable, array, this.m_DataTable.Length);
					this.m_DataTable = array;
				}
				if (this.m_DataTable[slot2] == null)
				{
					this.m_DataTable[slot2] = new LocalDataStoreElement(slot.Cookie);
				}
				result = this.m_DataTable[slot2];
			}
			finally
			{
				if (flag)
				{
					Monitor.Exit(this.m_Manager);
				}
			}
			return result;
		}

		// Token: 0x040005AD RID: 1453
		private LocalDataStoreElement[] m_DataTable;

		// Token: 0x040005AE RID: 1454
		private LocalDataStoreMgr m_Manager;
	}
}
