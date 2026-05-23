using System;
using System.Reflection.Emit;

namespace System.Security
{
	// Token: 0x020001D7 RID: 471
	internal class FrameSecurityDescriptorWithResolver : FrameSecurityDescriptor
	{
		// Token: 0x17000336 RID: 822
		// (get) Token: 0x06001C9A RID: 7322 RVA: 0x000620EC File Offset: 0x000602EC
		public DynamicResolver Resolver
		{
			get
			{
				return this.m_resolver;
			}
		}

		// Token: 0x040009FC RID: 2556
		private DynamicResolver m_resolver;
	}
}
