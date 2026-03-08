using System;

namespace HarmonyLib
{
	/// <summary>A Harmony annotation</summary>
	// Token: 0x02000075 RID: 117
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Parameter, AllowMultiple = true)]
	public class HarmonyArgument : Attribute
	{
		/// <summary>The name of the original argument</summary>
		// Token: 0x17000016 RID: 22
		// (get) Token: 0x0600020D RID: 525 RVA: 0x0000D7C5 File Offset: 0x0000B9C5
		// (set) Token: 0x0600020E RID: 526 RVA: 0x0000D7CD File Offset: 0x0000B9CD
		public string OriginalName { get; private set; }

		/// <summary>The index of the original argument</summary>
		// Token: 0x17000017 RID: 23
		// (get) Token: 0x0600020F RID: 527 RVA: 0x0000D7D6 File Offset: 0x0000B9D6
		// (set) Token: 0x06000210 RID: 528 RVA: 0x0000D7DE File Offset: 0x0000B9DE
		public int Index { get; private set; }

		/// <summary>The new name of the original argument</summary>
		// Token: 0x17000018 RID: 24
		// (get) Token: 0x06000211 RID: 529 RVA: 0x0000D7E7 File Offset: 0x0000B9E7
		// (set) Token: 0x06000212 RID: 530 RVA: 0x0000D7EF File Offset: 0x0000B9EF
		public string NewName { get; private set; }

		/// <summary>An annotation to declare injected arguments by name</summary>
		// Token: 0x06000213 RID: 531 RVA: 0x0000D7F8 File Offset: 0x0000B9F8
		public HarmonyArgument(string originalName)
			: this(originalName, null)
		{
		}

		/// <summary>An annotation to declare injected arguments by index</summary>
		/// <param name="index">Zero-based index</param>
		// Token: 0x06000214 RID: 532 RVA: 0x0000D802 File Offset: 0x0000BA02
		public HarmonyArgument(int index)
			: this(index, null)
		{
		}

		/// <summary>An annotation to declare injected arguments by renaming them</summary>
		/// <param name="originalName">Name of the original argument</param>
		/// <param name="newName">New name</param>
		// Token: 0x06000215 RID: 533 RVA: 0x0000D80C File Offset: 0x0000BA0C
		public HarmonyArgument(string originalName, string newName)
		{
			this.OriginalName = originalName;
			this.Index = -1;
			this.NewName = newName;
		}

		/// <summary>An annotation to declare injected arguments by index and renaming them</summary>
		/// <param name="index">Zero-based index</param>
		/// <param name="name">New name</param>
		// Token: 0x06000216 RID: 534 RVA: 0x0000D829 File Offset: 0x0000BA29
		public HarmonyArgument(int index, string name)
		{
			this.OriginalName = null;
			this.Index = index;
			this.NewName = name;
		}
	}
}
