using System;

namespace TaleWorlds.GauntletUI
{
	// Token: 0x0200001F RID: 31
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
	public class EditorAttribute : Attribute
	{
		// Token: 0x06000252 RID: 594 RVA: 0x0000BE11 File Offset: 0x0000A011
		public EditorAttribute(bool includeInnerProperties = false)
		{
			this.IncludeInnerProperties = includeInnerProperties;
		}

		// Token: 0x0400012E RID: 302
		public readonly bool IncludeInnerProperties;
	}
}
