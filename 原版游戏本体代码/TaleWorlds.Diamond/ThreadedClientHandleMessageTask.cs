using System;

namespace TaleWorlds.Diamond
{
	// Token: 0x02000027 RID: 39
	internal sealed class ThreadedClientHandleMessageTask : ThreadedClientTask
	{
		// Token: 0x17000034 RID: 52
		// (get) Token: 0x060000D8 RID: 216 RVA: 0x000036B5 File Offset: 0x000018B5
		// (set) Token: 0x060000D9 RID: 217 RVA: 0x000036BD File Offset: 0x000018BD
		public Message Message { get; private set; }

		// Token: 0x060000DA RID: 218 RVA: 0x000036C6 File Offset: 0x000018C6
		public ThreadedClientHandleMessageTask(IClient client, Message message)
			: base(client)
		{
			this.Message = message;
		}

		// Token: 0x060000DB RID: 219 RVA: 0x000036D6 File Offset: 0x000018D6
		public override void DoJob()
		{
			base.Client.HandleMessage(this.Message);
		}
	}
}
