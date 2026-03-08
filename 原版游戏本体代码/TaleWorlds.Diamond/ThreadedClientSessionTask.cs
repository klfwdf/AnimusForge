using System;

namespace TaleWorlds.Diamond
{
	// Token: 0x0200002C RID: 44
	internal abstract class ThreadedClientSessionTask
	{
		// Token: 0x17000035 RID: 53
		// (get) Token: 0x060000EE RID: 238 RVA: 0x00003A92 File Offset: 0x00001C92
		// (set) Token: 0x060000EF RID: 239 RVA: 0x00003A9A File Offset: 0x00001C9A
		public IClientSession Session { get; private set; }

		// Token: 0x17000036 RID: 54
		// (get) Token: 0x060000F0 RID: 240 RVA: 0x00003AA3 File Offset: 0x00001CA3
		// (set) Token: 0x060000F1 RID: 241 RVA: 0x00003AAB File Offset: 0x00001CAB
		public bool Finished { get; protected set; }

		// Token: 0x060000F2 RID: 242 RVA: 0x00003AB4 File Offset: 0x00001CB4
		protected ThreadedClientSessionTask(IClientSession session)
		{
			this.Session = session;
		}

		// Token: 0x060000F3 RID: 243
		public abstract void BeginJob();

		// Token: 0x060000F4 RID: 244
		public abstract void DoMainThreadJob();
	}
}
