using System;
using TaleWorlds.Localization;

namespace TaleWorlds.Diamond
{
	// Token: 0x02000013 RID: 19
	public class AccessObjectResult
	{
		// Token: 0x17000011 RID: 17
		// (get) Token: 0x0600005B RID: 91 RVA: 0x00002937 File Offset: 0x00000B37
		// (set) Token: 0x0600005C RID: 92 RVA: 0x0000293F File Offset: 0x00000B3F
		public AccessObject AccessObject { get; set; }

		// Token: 0x17000012 RID: 18
		// (get) Token: 0x0600005D RID: 93 RVA: 0x00002948 File Offset: 0x00000B48
		// (set) Token: 0x0600005E RID: 94 RVA: 0x00002950 File Offset: 0x00000B50
		public bool Success { get; set; }

		// Token: 0x17000013 RID: 19
		// (get) Token: 0x0600005F RID: 95 RVA: 0x00002959 File Offset: 0x00000B59
		// (set) Token: 0x06000060 RID: 96 RVA: 0x00002961 File Offset: 0x00000B61
		public TextObject FailReason { get; set; }

		// Token: 0x06000062 RID: 98 RVA: 0x00002972 File Offset: 0x00000B72
		public static AccessObjectResult CreateSuccess(AccessObject accessObject)
		{
			return new AccessObjectResult
			{
				Success = true,
				AccessObject = accessObject
			};
		}

		// Token: 0x06000063 RID: 99 RVA: 0x00002987 File Offset: 0x00000B87
		public static AccessObjectResult CreateFailed(TextObject failReason)
		{
			return new AccessObjectResult
			{
				Success = false,
				FailReason = failReason
			};
		}
	}
}
