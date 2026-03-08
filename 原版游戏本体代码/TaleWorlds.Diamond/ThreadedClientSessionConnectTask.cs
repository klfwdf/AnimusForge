using System;

namespace TaleWorlds.Diamond
{
	// Token: 0x0200002D RID: 45
	internal sealed class ThreadedClientSessionConnectTask : ThreadedClientSessionTask
	{
		// Token: 0x060000F5 RID: 245 RVA: 0x00003AC3 File Offset: 0x00001CC3
		public ThreadedClientSessionConnectTask(IClientSession session)
			: base(session)
		{
		}

		// Token: 0x060000F6 RID: 246 RVA: 0x00003ACC File Offset: 0x00001CCC
		public override void BeginJob()
		{
			base.Session.Connect();
		}

		// Token: 0x060000F7 RID: 247 RVA: 0x00003AD9 File Offset: 0x00001CD9
		public override void DoMainThreadJob()
		{
			base.Finished = true;
		}
	}
}
