using System;

namespace TaleWorlds.Library
{
	// Token: 0x020000A7 RID: 167
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class VirtualDirectoryAttribute : Attribute
	{
		// Token: 0x170000B0 RID: 176
		// (get) Token: 0x0600065C RID: 1628 RVA: 0x0001644C File Offset: 0x0001464C
		// (set) Token: 0x0600065D RID: 1629 RVA: 0x00016454 File Offset: 0x00014654
		public string Name { get; private set; }

		// Token: 0x0600065E RID: 1630 RVA: 0x0001645D File Offset: 0x0001465D
		public VirtualDirectoryAttribute(string name)
		{
			this.Name = name;
		}
	}
}
