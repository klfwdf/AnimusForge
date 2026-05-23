using System;

namespace TaleWorlds.Library
{
	// Token: 0x020000A8 RID: 168
	public class VirtualFileAttribute : Attribute
	{
		// Token: 0x170000B1 RID: 177
		// (get) Token: 0x0600065F RID: 1631 RVA: 0x0001646C File Offset: 0x0001466C
		// (set) Token: 0x06000660 RID: 1632 RVA: 0x00016474 File Offset: 0x00014674
		public string Name { get; private set; }

		// Token: 0x170000B2 RID: 178
		// (get) Token: 0x06000661 RID: 1633 RVA: 0x0001647D File Offset: 0x0001467D
		// (set) Token: 0x06000662 RID: 1634 RVA: 0x00016485 File Offset: 0x00014685
		public string Content { get; private set; }

		// Token: 0x06000663 RID: 1635 RVA: 0x0001648E File Offset: 0x0001468E
		public VirtualFileAttribute(string name, string content)
		{
			this.Name = name;
			this.Content = content;
		}
	}
}
