using System;

namespace TaleWorlds.Diamond
{
	// Token: 0x02000030 RID: 48
	internal sealed class ThreadedClientSessionMessageTask : ThreadedClientSessionTask
	{
		// Token: 0x17000038 RID: 56
		// (get) Token: 0x06000102 RID: 258 RVA: 0x00003BE9 File Offset: 0x00001DE9
		// (set) Token: 0x06000103 RID: 259 RVA: 0x00003BF1 File Offset: 0x00001DF1
		public Message Message { get; private set; }

		// Token: 0x06000104 RID: 260 RVA: 0x00003BFA File Offset: 0x00001DFA
		public ThreadedClientSessionMessageTask(IClientSession session, Message message)
			: base(session)
		{
			this.Message = message;
		}

		// Token: 0x06000105 RID: 261 RVA: 0x00003C0A File Offset: 0x00001E0A
		public override void BeginJob()
		{
			base.Session.SendMessage(this.Message);
		}

		// Token: 0x06000106 RID: 262 RVA: 0x00003C1D File Offset: 0x00001E1D
		public override void DoMainThreadJob()
		{
			base.Finished = true;
		}
	}
}
