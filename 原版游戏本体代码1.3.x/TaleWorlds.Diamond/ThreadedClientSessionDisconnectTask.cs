using System;

namespace TaleWorlds.Diamond
{
	// Token: 0x0200002E RID: 46
	internal sealed class ThreadedClientSessionDisconnectTask : ThreadedClientSessionTask
	{
		// Token: 0x060000F8 RID: 248 RVA: 0x00003AE2 File Offset: 0x00001CE2
		public ThreadedClientSessionDisconnectTask(IClientSession session)
			: base(session)
		{
		}

		// Token: 0x060000F9 RID: 249 RVA: 0x00003AEB File Offset: 0x00001CEB
		public override void BeginJob()
		{
			base.Session.Disconnect();
		}

		// Token: 0x060000FA RID: 250 RVA: 0x00003AF8 File Offset: 0x00001CF8
		public override void DoMainThreadJob()
		{
			base.Finished = true;
		}
	}
}
