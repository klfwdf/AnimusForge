using System;

namespace TaleWorlds.DotNet
{
	// Token: 0x02000009 RID: 9
	internal class CustomParameter<T> : DotNetObject
	{
		// Token: 0x17000008 RID: 8
		// (get) Token: 0x06000021 RID: 33 RVA: 0x000027EC File Offset: 0x000009EC
		// (set) Token: 0x06000022 RID: 34 RVA: 0x000027F4 File Offset: 0x000009F4
		public T Target { get; set; }

		// Token: 0x06000023 RID: 35 RVA: 0x000027FD File Offset: 0x000009FD
		public CustomParameter(T target)
		{
			this.Target = target;
		}
	}
}
