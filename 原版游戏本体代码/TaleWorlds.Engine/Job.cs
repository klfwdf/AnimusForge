using System;

namespace TaleWorlds.Engine
{
	// Token: 0x02000054 RID: 84
	public class Job
	{
		// Token: 0x17000038 RID: 56
		// (get) Token: 0x06000896 RID: 2198 RVA: 0x00006CD8 File Offset: 0x00004ED8
		// (set) Token: 0x06000897 RID: 2199 RVA: 0x00006CE0 File Offset: 0x00004EE0
		public bool Finished { get; protected set; }

		// Token: 0x06000898 RID: 2200 RVA: 0x00006CE9 File Offset: 0x00004EE9
		public virtual void DoJob(float dt)
		{
		}
	}
}
