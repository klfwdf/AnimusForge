using System;

namespace System.Runtime.CompilerServices
{
	// Token: 0x020008C4 RID: 2244
	[AttributeUsage(AttributeTargets.Assembly)]
	[Serializable]
	public sealed class DefaultDependencyAttribute : Attribute
	{
		// Token: 0x06005DBD RID: 23997 RVA: 0x00149947 File Offset: 0x00147B47
		public DefaultDependencyAttribute(LoadHint loadHintArgument)
		{
			this.loadHint = loadHintArgument;
		}

		// Token: 0x1700101A RID: 4122
		// (get) Token: 0x06005DBE RID: 23998 RVA: 0x00149956 File Offset: 0x00147B56
		public LoadHint LoadHint
		{
			get
			{
				return this.loadHint;
			}
		}

		// Token: 0x04002A31 RID: 10801
		private LoadHint loadHint;
	}
}
