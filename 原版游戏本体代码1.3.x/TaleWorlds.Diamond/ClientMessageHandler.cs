using System;

namespace TaleWorlds.Diamond
{
	// Token: 0x02000006 RID: 6
	// (Invoke) Token: 0x06000024 RID: 36
	public delegate void ClientMessageHandler<TMessage>(TMessage message) where TMessage : Message;
}
