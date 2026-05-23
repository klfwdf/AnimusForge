using System;

namespace TaleWorlds.Library
{
	// Token: 0x0200008B RID: 139
	public struct SaveResultWithMessage
	{
		// Token: 0x1700008E RID: 142
		// (get) Token: 0x06000502 RID: 1282 RVA: 0x000122DE File Offset: 0x000104DE
		public static SaveResultWithMessage Default
		{
			get
			{
				return new SaveResultWithMessage(SaveResult.Success, string.Empty);
			}
		}

		// Token: 0x06000503 RID: 1283 RVA: 0x000122EB File Offset: 0x000104EB
		public SaveResultWithMessage(SaveResult result, string message)
		{
			this.SaveResult = result;
			this.Message = message;
		}

		// Token: 0x0400018C RID: 396
		public readonly SaveResult SaveResult;

		// Token: 0x0400018D RID: 397
		public readonly string Message;
	}
}
