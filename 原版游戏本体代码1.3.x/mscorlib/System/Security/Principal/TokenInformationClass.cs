using System;

namespace System.Security.Principal
{
	// Token: 0x02000330 RID: 816
	[Serializable]
	internal enum TokenInformationClass
	{
		// Token: 0x04001059 RID: 4185
		TokenUser = 1,
		// Token: 0x0400105A RID: 4186
		TokenGroups,
		// Token: 0x0400105B RID: 4187
		TokenPrivileges,
		// Token: 0x0400105C RID: 4188
		TokenOwner,
		// Token: 0x0400105D RID: 4189
		TokenPrimaryGroup,
		// Token: 0x0400105E RID: 4190
		TokenDefaultDacl,
		// Token: 0x0400105F RID: 4191
		TokenSource,
		// Token: 0x04001060 RID: 4192
		TokenType,
		// Token: 0x04001061 RID: 4193
		TokenImpersonationLevel,
		// Token: 0x04001062 RID: 4194
		TokenStatistics,
		// Token: 0x04001063 RID: 4195
		TokenRestrictedSids,
		// Token: 0x04001064 RID: 4196
		TokenSessionId,
		// Token: 0x04001065 RID: 4197
		TokenGroupsAndPrivileges,
		// Token: 0x04001066 RID: 4198
		TokenSessionReference,
		// Token: 0x04001067 RID: 4199
		TokenSandBoxInert,
		// Token: 0x04001068 RID: 4200
		TokenAuditPolicy,
		// Token: 0x04001069 RID: 4201
		TokenOrigin,
		// Token: 0x0400106A RID: 4202
		TokenElevationType,
		// Token: 0x0400106B RID: 4203
		TokenLinkedToken,
		// Token: 0x0400106C RID: 4204
		TokenElevation,
		// Token: 0x0400106D RID: 4205
		TokenHasRestrictions,
		// Token: 0x0400106E RID: 4206
		TokenAccessInformation,
		// Token: 0x0400106F RID: 4207
		TokenVirtualizationAllowed,
		// Token: 0x04001070 RID: 4208
		TokenVirtualizationEnabled,
		// Token: 0x04001071 RID: 4209
		TokenIntegrityLevel,
		// Token: 0x04001072 RID: 4210
		TokenUIAccess,
		// Token: 0x04001073 RID: 4211
		TokenMandatoryPolicy,
		// Token: 0x04001074 RID: 4212
		TokenLogonSid,
		// Token: 0x04001075 RID: 4213
		TokenIsAppContainer,
		// Token: 0x04001076 RID: 4214
		TokenCapabilities,
		// Token: 0x04001077 RID: 4215
		TokenAppContainerSid,
		// Token: 0x04001078 RID: 4216
		TokenAppContainerNumber,
		// Token: 0x04001079 RID: 4217
		TokenUserClaimAttributes,
		// Token: 0x0400107A RID: 4218
		TokenDeviceClaimAttributes,
		// Token: 0x0400107B RID: 4219
		TokenRestrictedUserClaimAttributes,
		// Token: 0x0400107C RID: 4220
		TokenRestrictedDeviceClaimAttributes,
		// Token: 0x0400107D RID: 4221
		TokenDeviceGroups,
		// Token: 0x0400107E RID: 4222
		TokenRestrictedDeviceGroups,
		// Token: 0x0400107F RID: 4223
		MaxTokenInfoClass
	}
}
