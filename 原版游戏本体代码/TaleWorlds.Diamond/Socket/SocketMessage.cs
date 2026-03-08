using System;
using System.Text;
using TaleWorlds.Library;
using TaleWorlds.Network;

namespace TaleWorlds.Diamond.Socket
{
	// Token: 0x02000033 RID: 51
	[MessageId(1)]
	public class SocketMessage : MessageContract
	{
		// Token: 0x1700003A RID: 58
		// (get) Token: 0x06000119 RID: 281 RVA: 0x00003DD6 File Offset: 0x00001FD6
		// (set) Token: 0x0600011A RID: 282 RVA: 0x00003DDE File Offset: 0x00001FDE
		public Message Message { get; private set; }

		// Token: 0x0600011B RID: 283 RVA: 0x00003DE7 File Offset: 0x00001FE7
		public SocketMessage()
		{
		}

		// Token: 0x0600011C RID: 284 RVA: 0x00003DEF File Offset: 0x00001FEF
		public SocketMessage(Message message)
		{
			this.Message = message;
		}

		// Token: 0x0600011D RID: 285 RVA: 0x00003E00 File Offset: 0x00002000
		public override void SerializeToNetworkMessage(INetworkMessageWriter networkMessage)
		{
			byte[] array = Common.SerializeObjectAsJson(this.Message);
			networkMessage.Write(array.Length);
			for (int i = 0; i < array.Length; i++)
			{
				networkMessage.Write(array[i]);
			}
		}

		// Token: 0x0600011E RID: 286 RVA: 0x00003E3C File Offset: 0x0000203C
		public override void DeserializeFromNetworkMessage(INetworkMessageReader networkMessage)
		{
			byte[] array = new byte[networkMessage.ReadInt32()];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = networkMessage.ReadByte();
			}
			string @string = Encoding.UTF8.GetString(array);
			this.Message = Common.DeserializeObjectFromJson<Message>(@string);
		}
	}
}
