using System;

namespace TaleWorlds.Library
{
	// Token: 0x0200005B RID: 91
	public class ListChangedEventArgs : EventArgs
	{
		// Token: 0x0600029F RID: 671 RVA: 0x00007D08 File Offset: 0x00005F08
		public ListChangedEventArgs(ListChangedType listChangedType, int newIndex)
		{
			this.ListChangedType = listChangedType;
			this.NewIndex = newIndex;
			this.OldIndex = -1;
		}

		// Token: 0x060002A0 RID: 672 RVA: 0x00007D25 File Offset: 0x00005F25
		public ListChangedEventArgs(ListChangedType listChangedType, int newIndex, int oldIndex)
		{
			this.ListChangedType = listChangedType;
			this.NewIndex = newIndex;
			this.OldIndex = oldIndex;
		}

		// Token: 0x1700003B RID: 59
		// (get) Token: 0x060002A1 RID: 673 RVA: 0x00007D42 File Offset: 0x00005F42
		// (set) Token: 0x060002A2 RID: 674 RVA: 0x00007D4A File Offset: 0x00005F4A
		public ListChangedType ListChangedType { get; private set; }

		// Token: 0x1700003C RID: 60
		// (get) Token: 0x060002A3 RID: 675 RVA: 0x00007D53 File Offset: 0x00005F53
		// (set) Token: 0x060002A4 RID: 676 RVA: 0x00007D5B File Offset: 0x00005F5B
		public int NewIndex { get; private set; }

		// Token: 0x1700003D RID: 61
		// (get) Token: 0x060002A5 RID: 677 RVA: 0x00007D64 File Offset: 0x00005F64
		// (set) Token: 0x060002A6 RID: 678 RVA: 0x00007D6C File Offset: 0x00005F6C
		public int OldIndex { get; private set; }
	}
}
