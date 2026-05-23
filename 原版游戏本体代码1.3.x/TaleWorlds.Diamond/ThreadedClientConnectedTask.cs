using System;

namespace TaleWorlds.Diamond
{
	// Token: 0x02000028 RID: 40
	internal sealed class ThreadedClientConnectedTask : ThreadedClientTask
	{
		// Token: 0x060000DC RID: 220 RVA: 0x000036E9 File Offset: 0x000018E9
		public ThreadedClientConnectedTask(IClient client)
			: base(client)
		{
		}

		// Token: 0x060000DD RID: 221 RVA: 0x000036F2 File Offset: 0x000018F2
		public override void DoJob()
		{
			base.Client.OnConnected();
		}
	}
}
