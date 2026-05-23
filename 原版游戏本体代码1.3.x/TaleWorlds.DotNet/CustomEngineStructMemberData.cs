using System;

namespace TaleWorlds.DotNet
{
	// Token: 0x02000012 RID: 18
	public class CustomEngineStructMemberData : Attribute
	{
		// Token: 0x17000015 RID: 21
		// (get) Token: 0x0600004B RID: 75 RVA: 0x00002E75 File Offset: 0x00001075
		// (set) Token: 0x0600004C RID: 76 RVA: 0x00002E7D File Offset: 0x0000107D
		public string CustomMemberName { get; set; }

		// Token: 0x17000016 RID: 22
		// (get) Token: 0x0600004D RID: 77 RVA: 0x00002E86 File Offset: 0x00001086
		// (set) Token: 0x0600004E RID: 78 RVA: 0x00002E8E File Offset: 0x0000108E
		public bool IgnoreMemberOffsetTest { get; set; }

		// Token: 0x17000017 RID: 23
		// (get) Token: 0x0600004F RID: 79 RVA: 0x00002E97 File Offset: 0x00001097
		// (set) Token: 0x06000050 RID: 80 RVA: 0x00002E9F File Offset: 0x0000109F
		public bool PublicPrivateModifierFlippedInNative { get; set; }

		// Token: 0x06000051 RID: 81 RVA: 0x00002EA8 File Offset: 0x000010A8
		public CustomEngineStructMemberData(string customMemberName)
		{
			this.CustomMemberName = customMemberName;
			this.IgnoreMemberOffsetTest = false;
			this.PublicPrivateModifierFlippedInNative = false;
		}

		// Token: 0x06000052 RID: 82 RVA: 0x00002EC5 File Offset: 0x000010C5
		public CustomEngineStructMemberData(string customMemberName, bool ignoreMemberOffsetTest)
		{
			this.CustomMemberName = customMemberName;
			this.IgnoreMemberOffsetTest = ignoreMemberOffsetTest;
			this.PublicPrivateModifierFlippedInNative = false;
		}

		// Token: 0x06000053 RID: 83 RVA: 0x00002EE2 File Offset: 0x000010E2
		public CustomEngineStructMemberData(bool publicPrivateModifierFlippedInNative)
		{
			this.CustomMemberName = null;
			this.IgnoreMemberOffsetTest = false;
			this.PublicPrivateModifierFlippedInNative = publicPrivateModifierFlippedInNative;
		}
	}
}
