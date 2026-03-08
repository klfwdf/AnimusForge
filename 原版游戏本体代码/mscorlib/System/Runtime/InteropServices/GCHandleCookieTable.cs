using System;
using System.Collections.Generic;
using System.Threading;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000949 RID: 2377
	internal class GCHandleCookieTable
	{
		// Token: 0x0600609D RID: 24733 RVA: 0x0014C680 File Offset: 0x0014A880
		internal GCHandleCookieTable()
		{
			this.m_HandleList = new IntPtr[10];
			this.m_CycleCounts = new byte[10];
			this.m_HandleToCookieMap = new Dictionary<IntPtr, IntPtr>(10);
			this.m_syncObject = new object();
			for (int i = 0; i < 10; i++)
			{
				this.m_HandleList[i] = IntPtr.Zero;
				this.m_CycleCounts[i] = 0;
			}
		}

		// Token: 0x0600609E RID: 24734 RVA: 0x0014C6F0 File Offset: 0x0014A8F0
		internal IntPtr FindOrAddHandle(IntPtr handle)
		{
			if (handle == IntPtr.Zero)
			{
				return IntPtr.Zero;
			}
			IntPtr intPtr = IntPtr.Zero;
			object syncObject = this.m_syncObject;
			lock (syncObject)
			{
				if (this.m_HandleToCookieMap.ContainsKey(handle))
				{
					return this.m_HandleToCookieMap[handle];
				}
				if (this.m_FreeIndex < this.m_HandleList.Length && Volatile.Read(ref this.m_HandleList[this.m_FreeIndex]) == IntPtr.Zero)
				{
					Volatile.Write(ref this.m_HandleList[this.m_FreeIndex], handle);
					intPtr = this.GetCookieFromData((uint)this.m_FreeIndex, this.m_CycleCounts[this.m_FreeIndex]);
					this.m_FreeIndex++;
				}
				else
				{
					this.m_FreeIndex = 0;
					while (this.m_FreeIndex < 16777215)
					{
						if (this.m_HandleList[this.m_FreeIndex] == IntPtr.Zero)
						{
							Volatile.Write(ref this.m_HandleList[this.m_FreeIndex], handle);
							intPtr = this.GetCookieFromData((uint)this.m_FreeIndex, this.m_CycleCounts[this.m_FreeIndex]);
							this.m_FreeIndex++;
							break;
						}
						if (this.m_FreeIndex + 1 == this.m_HandleList.Length)
						{
							this.GrowArrays();
						}
						this.m_FreeIndex++;
					}
				}
				if (intPtr == IntPtr.Zero)
				{
					throw new OutOfMemoryException(Environment.GetResourceString("OutOfMemory_GCHandleMDA"));
				}
				this.m_HandleToCookieMap.Add(handle, intPtr);
			}
			return intPtr;
		}

		// Token: 0x0600609F RID: 24735 RVA: 0x0014C8C0 File Offset: 0x0014AAC0
		internal IntPtr GetHandle(IntPtr cookie)
		{
			IntPtr zero = IntPtr.Zero;
			if (!this.ValidateCookie(cookie))
			{
				return IntPtr.Zero;
			}
			return Volatile.Read(ref this.m_HandleList[this.GetIndexFromCookie(cookie)]);
		}

		// Token: 0x060060A0 RID: 24736 RVA: 0x0014C900 File Offset: 0x0014AB00
		internal void RemoveHandleIfPresent(IntPtr handle)
		{
			if (handle == IntPtr.Zero)
			{
				return;
			}
			object syncObject = this.m_syncObject;
			lock (syncObject)
			{
				if (this.m_HandleToCookieMap.ContainsKey(handle))
				{
					IntPtr cookie = this.m_HandleToCookieMap[handle];
					if (this.ValidateCookie(cookie))
					{
						int indexFromCookie = this.GetIndexFromCookie(cookie);
						byte[] cycleCounts = this.m_CycleCounts;
						int num = indexFromCookie;
						cycleCounts[num] += 1;
						Volatile.Write(ref this.m_HandleList[indexFromCookie], IntPtr.Zero);
						this.m_HandleToCookieMap.Remove(handle);
						this.m_FreeIndex = indexFromCookie;
					}
				}
			}
		}

		// Token: 0x060060A1 RID: 24737 RVA: 0x0014C9B8 File Offset: 0x0014ABB8
		private bool ValidateCookie(IntPtr cookie)
		{
			int num;
			byte b;
			this.GetDataFromCookie(cookie, out num, out b);
			if (num >= 16777215)
			{
				return false;
			}
			if (num >= this.m_HandleList.Length)
			{
				return false;
			}
			if (Volatile.Read(ref this.m_HandleList[num]) == IntPtr.Zero)
			{
				return false;
			}
			byte b2 = (byte)(AppDomain.CurrentDomain.Id % 255);
			byte b3 = Volatile.Read(ref this.m_CycleCounts[num]) ^ b2;
			return b == b3;
		}

		// Token: 0x060060A2 RID: 24738 RVA: 0x0014CA3C File Offset: 0x0014AC3C
		private void GrowArrays()
		{
			int num = this.m_HandleList.Length;
			IntPtr[] array = new IntPtr[num * 2];
			byte[] array2 = new byte[num * 2];
			Array.Copy(this.m_HandleList, array, num);
			Array.Copy(this.m_CycleCounts, array2, num);
			this.m_HandleList = array;
			this.m_CycleCounts = array2;
		}

		// Token: 0x060060A3 RID: 24739 RVA: 0x0014CA98 File Offset: 0x0014AC98
		private IntPtr GetCookieFromData(uint index, byte cycleCount)
		{
			byte b = (byte)(AppDomain.CurrentDomain.Id % 255);
			return (IntPtr)((long)((long)(cycleCount ^ b) << 24) + (long)((ulong)index) + 1L);
		}

		// Token: 0x060060A4 RID: 24740 RVA: 0x0014CACC File Offset: 0x0014ACCC
		private void GetDataFromCookie(IntPtr cookie, out int index, out byte xorData)
		{
			uint num = (uint)(int)cookie;
			index = (int)((num & 16777215U) - 1U);
			xorData = (byte)((num & 4278190080U) >> 24);
		}

		// Token: 0x060060A5 RID: 24741 RVA: 0x0014CAF8 File Offset: 0x0014ACF8
		private int GetIndexFromCookie(IntPtr cookie)
		{
			uint num = (uint)(int)cookie;
			return (int)((num & 16777215U) - 1U);
		}

		// Token: 0x04002B4A RID: 11082
		private const int InitialHandleCount = 10;

		// Token: 0x04002B4B RID: 11083
		private const int MaxListSize = 16777215;

		// Token: 0x04002B4C RID: 11084
		private const uint CookieMaskIndex = 16777215U;

		// Token: 0x04002B4D RID: 11085
		private const uint CookieMaskSentinal = 4278190080U;

		// Token: 0x04002B4E RID: 11086
		private Dictionary<IntPtr, IntPtr> m_HandleToCookieMap;

		// Token: 0x04002B4F RID: 11087
		private volatile IntPtr[] m_HandleList;

		// Token: 0x04002B50 RID: 11088
		private volatile byte[] m_CycleCounts;

		// Token: 0x04002B51 RID: 11089
		private int m_FreeIndex;

		// Token: 0x04002B52 RID: 11090
		private readonly object m_syncObject;
	}
}
