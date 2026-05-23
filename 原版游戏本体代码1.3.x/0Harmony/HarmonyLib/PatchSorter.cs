using System;
using System.Collections.Generic;
using System.Linq;

namespace HarmonyLib
{
	// Token: 0x02000053 RID: 83
	internal class PatchSorter
	{
		// Token: 0x060001A1 RID: 417 RVA: 0x0000C390 File Offset: 0x0000A590
		internal PatchSorter(Patch[] patches, bool debug)
		{
			this.patches = (from x in patches
				select new PatchSorter.PatchSortingWrapper(x)).ToList<PatchSorter.PatchSortingWrapper>();
			this.debug = debug;
			using (List<PatchSorter.PatchSortingWrapper>.Enumerator enumerator = this.patches.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					PatchSorter.PatchSortingWrapper node = enumerator.Current;
					node.AddBeforeDependency(from x in this.patches
						where node.innerPatch.before.Contains(x.innerPatch.owner)
						select x);
					node.AddAfterDependency(from x in this.patches
						where node.innerPatch.after.Contains(x.innerPatch.owner)
						select x);
				}
			}
			this.patches.Sort();
		}

		// Token: 0x060001A2 RID: 418 RVA: 0x0000C474 File Offset: 0x0000A674
		internal Patch[] Sort()
		{
			if (this.sortedPatchArray != null)
			{
				return this.sortedPatchArray;
			}
			this.handledPatches = new HashSet<PatchSorter.PatchSortingWrapper>();
			this.waitingList = new List<PatchSorter.PatchSortingWrapper>();
			this.result = new List<PatchSorter.PatchSortingWrapper>(this.patches.Count);
			Queue<PatchSorter.PatchSortingWrapper> queue = new Queue<PatchSorter.PatchSortingWrapper>(this.patches);
			while (queue.Count != 0)
			{
				foreach (PatchSorter.PatchSortingWrapper node in queue)
				{
					if (node.after.All((PatchSorter.PatchSortingWrapper x) => this.handledPatches.Contains(x)))
					{
						this.AddNodeToResult(node);
						if (node.before.Count != 0)
						{
							this.ProcessWaitingList();
						}
					}
					else
					{
						this.waitingList.Add(node);
					}
				}
				this.CullDependency();
				queue = new Queue<PatchSorter.PatchSortingWrapper>(this.waitingList);
				this.waitingList.Clear();
			}
			this.sortedPatchArray = (from x in this.result
				select x.innerPatch).ToArray<Patch>();
			this.handledPatches = null;
			this.waitingList = null;
			this.patches = null;
			return this.sortedPatchArray;
		}

		// Token: 0x060001A3 RID: 419 RVA: 0x0000C5C0 File Offset: 0x0000A7C0
		internal bool ComparePatchLists(Patch[] patches)
		{
			if (this.sortedPatchArray == null)
			{
				this.Sort();
			}
			return patches != null && this.sortedPatchArray.Length == patches.Length && this.sortedPatchArray.All((Patch x) => patches.Contains(x, new PatchSorter.PatchDetailedComparer()));
		}

		// Token: 0x060001A4 RID: 420 RVA: 0x0000C61C File Offset: 0x0000A81C
		private void CullDependency()
		{
			for (int i = this.waitingList.Count - 1; i >= 0; i--)
			{
				foreach (PatchSorter.PatchSortingWrapper afterNode in this.waitingList[i].after)
				{
					if (!this.handledPatches.Contains(afterNode))
					{
						this.waitingList[i].RemoveAfterDependency(afterNode);
						if (this.debug)
						{
							string part = afterNode.innerPatch.PatchMethod.FullDescription();
							string part2 = this.waitingList[i].innerPatch.PatchMethod.FullDescription();
							FileLog.LogBuffered("Breaking dependance between " + part + " and " + part2);
						}
						return;
					}
				}
			}
		}

		// Token: 0x060001A5 RID: 421 RVA: 0x0000C700 File Offset: 0x0000A900
		private void ProcessWaitingList()
		{
			int waitingListCount = this.waitingList.Count;
			int i = 0;
			while (i < waitingListCount)
			{
				PatchSorter.PatchSortingWrapper node = this.waitingList[i];
				if (node.after.All(new Func<PatchSorter.PatchSortingWrapper, bool>(this.handledPatches.Contains)))
				{
					this.waitingList.Remove(node);
					this.AddNodeToResult(node);
					waitingListCount--;
					i = 0;
				}
				else
				{
					i++;
				}
			}
		}

		// Token: 0x060001A6 RID: 422 RVA: 0x0000C76D File Offset: 0x0000A96D
		internal void AddNodeToResult(PatchSorter.PatchSortingWrapper node)
		{
			this.result.Add(node);
			this.handledPatches.Add(node);
		}

		// Token: 0x04000122 RID: 290
		private List<PatchSorter.PatchSortingWrapper> patches;

		// Token: 0x04000123 RID: 291
		private HashSet<PatchSorter.PatchSortingWrapper> handledPatches;

		// Token: 0x04000124 RID: 292
		private List<PatchSorter.PatchSortingWrapper> result;

		// Token: 0x04000125 RID: 293
		private List<PatchSorter.PatchSortingWrapper> waitingList;

		// Token: 0x04000126 RID: 294
		private Patch[] sortedPatchArray;

		// Token: 0x04000127 RID: 295
		private readonly bool debug;

		// Token: 0x02000054 RID: 84
		internal class PatchSortingWrapper : IComparable
		{
			// Token: 0x060001A8 RID: 424 RVA: 0x0000C796 File Offset: 0x0000A996
			internal PatchSortingWrapper(Patch patch)
			{
				this.innerPatch = patch;
				this.before = new HashSet<PatchSorter.PatchSortingWrapper>();
				this.after = new HashSet<PatchSorter.PatchSortingWrapper>();
			}

			// Token: 0x060001A9 RID: 425 RVA: 0x0000C7BC File Offset: 0x0000A9BC
			public int CompareTo(object obj)
			{
				PatchSorter.PatchSortingWrapper p = obj as PatchSorter.PatchSortingWrapper;
				return PatchInfoSerialization.PriorityComparer((p != null) ? p.innerPatch : null, this.innerPatch.index, this.innerPatch.priority);
			}

			// Token: 0x060001AA RID: 426 RVA: 0x0000C7F8 File Offset: 0x0000A9F8
			public override bool Equals(object obj)
			{
				PatchSorter.PatchSortingWrapper wrapper = obj as PatchSorter.PatchSortingWrapper;
				return wrapper != null && this.innerPatch.PatchMethod == wrapper.innerPatch.PatchMethod;
			}

			// Token: 0x060001AB RID: 427 RVA: 0x0000C82C File Offset: 0x0000AA2C
			public override int GetHashCode()
			{
				return this.innerPatch.PatchMethod.GetHashCode();
			}

			// Token: 0x060001AC RID: 428 RVA: 0x0000C840 File Offset: 0x0000AA40
			internal void AddBeforeDependency(IEnumerable<PatchSorter.PatchSortingWrapper> dependencies)
			{
				foreach (PatchSorter.PatchSortingWrapper i in dependencies)
				{
					this.before.Add(i);
					i.after.Add(this);
				}
			}

			// Token: 0x060001AD RID: 429 RVA: 0x0000C89C File Offset: 0x0000AA9C
			internal void AddAfterDependency(IEnumerable<PatchSorter.PatchSortingWrapper> dependencies)
			{
				foreach (PatchSorter.PatchSortingWrapper i in dependencies)
				{
					this.after.Add(i);
					i.before.Add(this);
				}
			}

			// Token: 0x060001AE RID: 430 RVA: 0x0000C8F8 File Offset: 0x0000AAF8
			internal void RemoveAfterDependency(PatchSorter.PatchSortingWrapper afterNode)
			{
				this.after.Remove(afterNode);
				afterNode.before.Remove(this);
			}

			// Token: 0x060001AF RID: 431 RVA: 0x0000C914 File Offset: 0x0000AB14
			private void RemoveBeforeDependency(PatchSorter.PatchSortingWrapper beforeNode)
			{
				this.before.Remove(beforeNode);
				beforeNode.after.Remove(this);
			}

			// Token: 0x04000128 RID: 296
			internal readonly HashSet<PatchSorter.PatchSortingWrapper> after;

			// Token: 0x04000129 RID: 297
			internal readonly HashSet<PatchSorter.PatchSortingWrapper> before;

			// Token: 0x0400012A RID: 298
			internal readonly Patch innerPatch;
		}

		// Token: 0x02000055 RID: 85
		private class PatchDetailedComparer : IEqualityComparer<Patch>
		{
			// Token: 0x060001B0 RID: 432 RVA: 0x0000C930 File Offset: 0x0000AB30
			public bool Equals(Patch x, Patch y)
			{
				return y != null && x != null && x.owner == y.owner && x.PatchMethod == y.PatchMethod && x.index == y.index && x.priority == y.priority && x.before.Length == y.before.Length && x.after.Length == y.after.Length && x.before.All(new Func<string, bool>(y.before.Contains<string>)) && x.after.All(new Func<string, bool>(y.after.Contains<string>));
			}

			// Token: 0x060001B1 RID: 433 RVA: 0x0000C9EE File Offset: 0x0000ABEE
			public int GetHashCode(Patch obj)
			{
				return obj.GetHashCode();
			}
		}
	}
}
