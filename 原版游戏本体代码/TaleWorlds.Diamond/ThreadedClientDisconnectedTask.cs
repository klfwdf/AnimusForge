using System;

namespace TaleWorlds.Diamond
{
	// Token: 0x02000029 RID: 41
	internal sealed class ThreadedClientDisconnectedTask : ThreadedClientTask
	{
		// Token: 0x060000DE RID: 222 RVA: 0x000036FF File Offset: 0x000018FF
		public ThreadedClientDisconnectedTask(IClient client)
			: base(client)
		{
		}

		// Token: 0x060000DF RID: 223 RVA: 0x00003708 File Offset: 0x00001908
		public override void DoJob()
		{
			base.Client.OnDisconnected();
		}
	}
}
