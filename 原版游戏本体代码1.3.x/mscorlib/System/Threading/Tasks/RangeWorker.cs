using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Threading.Tasks
{
	// Token: 0x02000553 RID: 1363
	[StructLayout(LayoutKind.Auto)]
	internal struct RangeWorker
	{
		// Token: 0x06004040 RID: 16448 RVA: 0x000EFE34 File Offset: 0x000EE034
		internal RangeWorker(IndexRange[] ranges, int nInitialRange, long nStep, bool use32BitCurrentIndex)
		{
			this.m_indexRanges = ranges;
			this.m_nCurrentIndexRange = nInitialRange;
			this._use32BitCurrentIndex = use32BitCurrentIndex;
			this.m_nStep = nStep;
			this.m_nIncrementValue = nStep;
			this.m_nMaxIncrementValue = 16L * nStep;
		}

		// Token: 0x06004041 RID: 16449 RVA: 0x000EFE68 File Offset: 0x000EE068
		[SecuritySafeCritical]
		internal unsafe bool FindNewWork(out long nFromInclusiveLocal, out long nToExclusiveLocal)
		{
			int num = this.m_indexRanges.Length;
			IndexRange indexRange;
			long num2;
			for (;;)
			{
				indexRange = this.m_indexRanges[this.m_nCurrentIndexRange];
				if (indexRange.m_bRangeFinished == 0)
				{
					if (this.m_indexRanges[this.m_nCurrentIndexRange].m_nSharedCurrentIndexOffset == null)
					{
						Interlocked.CompareExchange<Shared<long>>(ref this.m_indexRanges[this.m_nCurrentIndexRange].m_nSharedCurrentIndexOffset, new Shared<long>(0L), null);
					}
					if (IntPtr.Size == 4 && this._use32BitCurrentIndex)
					{
						fixed (long* ptr = &this.m_indexRanges[this.m_nCurrentIndexRange].m_nSharedCurrentIndexOffset.Value)
						{
							long* location = ptr;
							num2 = (long)Interlocked.Add(ref *(int*)location, (int)this.m_nIncrementValue) - this.m_nIncrementValue;
						}
					}
					else
					{
						num2 = Interlocked.Add(ref this.m_indexRanges[this.m_nCurrentIndexRange].m_nSharedCurrentIndexOffset.Value, this.m_nIncrementValue) - this.m_nIncrementValue;
					}
					if (indexRange.m_nToExclusive - indexRange.m_nFromInclusive > num2)
					{
						break;
					}
					Interlocked.Exchange(ref this.m_indexRanges[this.m_nCurrentIndexRange].m_bRangeFinished, 1);
				}
				this.m_nCurrentIndexRange = (this.m_nCurrentIndexRange + 1) % this.m_indexRanges.Length;
				num--;
				if (num <= 0)
				{
					goto Block_9;
				}
			}
			nFromInclusiveLocal = indexRange.m_nFromInclusive + num2;
			nToExclusiveLocal = nFromInclusiveLocal + this.m_nIncrementValue;
			if (nToExclusiveLocal > indexRange.m_nToExclusive || nToExclusiveLocal < indexRange.m_nFromInclusive)
			{
				nToExclusiveLocal = indexRange.m_nToExclusive;
			}
			if (this.m_nIncrementValue < this.m_nMaxIncrementValue)
			{
				this.m_nIncrementValue *= 2L;
				if (this.m_nIncrementValue > this.m_nMaxIncrementValue)
				{
					this.m_nIncrementValue = this.m_nMaxIncrementValue;
				}
			}
			return true;
			Block_9:
			nFromInclusiveLocal = 0L;
			nToExclusiveLocal = 0L;
			return false;
		}

		// Token: 0x06004042 RID: 16450 RVA: 0x000F001C File Offset: 0x000EE21C
		internal bool FindNewWork32(out int nFromInclusiveLocal32, out int nToExclusiveLocal32)
		{
			long num;
			long num2;
			bool result = this.FindNewWork(out num, out num2);
			nFromInclusiveLocal32 = (int)num;
			nToExclusiveLocal32 = (int)num2;
			return result;
		}

		// Token: 0x04001AD5 RID: 6869
		internal readonly IndexRange[] m_indexRanges;

		// Token: 0x04001AD6 RID: 6870
		internal int m_nCurrentIndexRange;

		// Token: 0x04001AD7 RID: 6871
		internal long m_nStep;

		// Token: 0x04001AD8 RID: 6872
		internal long m_nIncrementValue;

		// Token: 0x04001AD9 RID: 6873
		internal readonly long m_nMaxIncrementValue;

		// Token: 0x04001ADA RID: 6874
		internal readonly bool _use32BitCurrentIndex;
	}
}
