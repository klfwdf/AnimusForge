using System;

namespace TaleWorlds.Diamond
{
	// Token: 0x0200002A RID: 42
	internal sealed class ThreadedClientCantConnectTask : ThreadedClientTask
	{
		// Token: 0x060000E0 RID: 224 RVA: 0x00003715 File Offset: 0x00001915
		public ThreadedClientCantConnectTask(IClient client)
			: base(client)
		{
		}

		// Token: 0x060000E1 RID: 225 RVA: 0x0000371E File Offset: 0x0000191E
		public override void DoJob()
		{
			base.Client.OnCantConnect();
		}
	}
}
