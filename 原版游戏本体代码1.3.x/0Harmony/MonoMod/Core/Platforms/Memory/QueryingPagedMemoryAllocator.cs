using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using MonoMod.Utils;

namespace MonoMod.Core.Platforms.Memory
{
	// Token: 0x02000555 RID: 1365
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class QueryingPagedMemoryAllocator : PagedMemoryAllocator
	{
		// Token: 0x06001E9B RID: 7835 RVA: 0x00064048 File Offset: 0x00062248
		public QueryingPagedMemoryAllocator(QueryingMemoryPageAllocatorBase alloc)
			: base((IntPtr)((UIntPtr)Helpers.ThrowIfNull<QueryingMemoryPageAllocatorBase>(alloc, "alloc").PageSize))
		{
			this.pageAlloc = alloc;
		}

		// Token: 0x06001E9C RID: 7836 RVA: 0x00064068 File Offset: 0x00062268
		protected override bool TryAllocateNewPage(AllocationRequest request, [<24b3ba8a-00b7-40fc-a603-2711fa115297>MaybeNullWhen(false)] out IAllocatedMemory allocated)
		{
			IntPtr allocBase;
			if (!this.pageAlloc.TryAllocatePage(base.PageSize, request.Executable, out allocBase))
			{
				allocated = null;
				return false;
			}
			PagedMemoryAllocator.Page pageObj = new PagedMemoryAllocator.Page(this, allocBase, (uint)base.PageSize, request.Executable);
			base.InsertAllocatedPage(pageObj);
			PagedMemoryAllocator.PageAllocation alloc;
			if (!pageObj.TryAllocate((uint)request.Size, (uint)request.Alignment, out alloc))
			{
				base.RegisterForCleanup(pageObj);
				allocated = null;
				return false;
			}
			allocated = alloc;
			return true;
		}

		// Token: 0x06001E9D RID: 7837 RVA: 0x000640DC File Offset: 0x000622DC
		protected override bool TryAllocateNewPage(PositionedAllocationRequest request, [NativeInteger] IntPtr targetPage, [NativeInteger] IntPtr lowPageBound, [NativeInteger] IntPtr highPageBound, [<24b3ba8a-00b7-40fc-a603-2711fa115297>MaybeNullWhen(false)] out IAllocatedMemory allocated)
		{
			IntPtr target = request.Target;
			IntPtr lowPage = targetPage;
			IntPtr highPage = targetPage + base.PageSize;
			while (lowPage >= lowPageBound || highPage < highPageBound)
			{
				while (highPage < highPageBound)
				{
					if (lowPage >= lowPageBound && target - lowPage <= highPage - target)
					{
						break;
					}
					if (this.TryAllocNewPage(request, ref highPage, true, out allocated))
					{
						return true;
					}
				}
				while (lowPage >= lowPageBound && (highPage >= highPageBound || target - lowPage < highPage - target))
				{
					if (this.TryAllocNewPage(request, ref lowPage, false, out allocated))
					{
						return true;
					}
				}
			}
			allocated = null;
			return false;
		}

		// Token: 0x06001E9E RID: 7838 RVA: 0x00064150 File Offset: 0x00062350
		private bool TryAllocNewPage(PositionedAllocationRequest request, [NativeInteger] ref IntPtr page, bool goingUp, [<24b3ba8a-00b7-40fc-a603-2711fa115297>MaybeNullWhen(false)] out IAllocatedMemory allocated)
		{
			bool isFree;
			IntPtr baseAddr;
			IntPtr allocSize;
			if (this.pageAlloc.TryQueryPage(page, out isFree, out baseAddr, out allocSize))
			{
				IntPtr allocBase;
				if (isFree && this.pageAlloc.TryAllocatePage(page, base.PageSize, request.Base.Executable, out allocBase))
				{
					PagedMemoryAllocator.Page pageObj = new PagedMemoryAllocator.Page(this, allocBase, (uint)base.PageSize, request.Base.Executable);
					base.InsertAllocatedPage(pageObj);
					PagedMemoryAllocator.PageAllocation alloc;
					if (pageObj.TryAllocate((uint)request.Base.Size, (uint)request.Base.Alignment, out alloc))
					{
						allocated = alloc;
						return true;
					}
					base.RegisterForCleanup(pageObj);
				}
				if (goingUp)
				{
					page = baseAddr + allocSize;
				}
				else
				{
					page = baseAddr - base.PageSize;
				}
				allocated = null;
				return false;
			}
			if (goingUp)
			{
				page += base.PageSize;
			}
			else
			{
				page -= base.PageSize;
			}
			allocated = null;
			return false;
		}

		// Token: 0x06001E9F RID: 7839 RVA: 0x00064240 File Offset: 0x00062440
		protected override bool TryFreePage(PagedMemoryAllocator.Page page, [Nullable(2)] [<24b3ba8a-00b7-40fc-a603-2711fa115297>NotNullWhen(false)] out string errorMsg)
		{
			return this.pageAlloc.TryFreePage(page.BaseAddr, out errorMsg);
		}

		// Token: 0x040012BB RID: 4795
		private readonly QueryingMemoryPageAllocatorBase pageAlloc;
	}
}
