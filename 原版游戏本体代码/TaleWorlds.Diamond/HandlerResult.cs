using System;

namespace TaleWorlds.Diamond
{
	// Token: 0x0200000C RID: 12
	public class HandlerResult
	{
		// Token: 0x1700000B RID: 11
		// (get) Token: 0x0600003B RID: 59 RVA: 0x000028CE File Offset: 0x00000ACE
		public bool IsSuccessful { get; }

		// Token: 0x1700000C RID: 12
		// (get) Token: 0x0600003C RID: 60 RVA: 0x000028D6 File Offset: 0x00000AD6
		public string Error { get; }

		// Token: 0x1700000D RID: 13
		// (get) Token: 0x0600003D RID: 61 RVA: 0x000028DE File Offset: 0x00000ADE
		public Message NextMessage { get; }

		// Token: 0x0600003E RID: 62 RVA: 0x000028E6 File Offset: 0x00000AE6
		protected HandlerResult(bool isSuccessful, string error = null, Message followUp = null)
		{
			this.IsSuccessful = isSuccessful;
			this.Error = error;
			this.NextMessage = followUp;
		}

		// Token: 0x0600003F RID: 63 RVA: 0x00002903 File Offset: 0x00000B03
		public static HandlerResult CreateSuccessful()
		{
			return new HandlerResult(true, null, null);
		}

		// Token: 0x06000040 RID: 64 RVA: 0x0000290D File Offset: 0x00000B0D
		public static HandlerResult CreateSuccessful(Message nextMessage)
		{
			return new HandlerResult(true, null, nextMessage);
		}

		// Token: 0x06000041 RID: 65 RVA: 0x00002917 File Offset: 0x00000B17
		public static HandlerResult CreateFailed(string error)
		{
			return new HandlerResult(false, error, null);
		}
	}
}
