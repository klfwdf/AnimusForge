using System;

namespace TaleWorlds.DotNet
{
	// Token: 0x0200000A RID: 10
	[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
	public class DefineCustomEngineStructMemberData : Attribute
	{
		// Token: 0x17000009 RID: 9
		// (get) Token: 0x06000024 RID: 36 RVA: 0x0000280C File Offset: 0x00000A0C
		// (set) Token: 0x06000025 RID: 37 RVA: 0x00002814 File Offset: 0x00000A14
		public Type Type { get; set; }

		// Token: 0x1700000A RID: 10
		// (get) Token: 0x06000026 RID: 38 RVA: 0x0000281D File Offset: 0x00000A1D
		// (set) Token: 0x06000027 RID: 39 RVA: 0x00002825 File Offset: 0x00000A25
		public string MemberName { get; set; }

		// Token: 0x1700000B RID: 11
		// (get) Token: 0x06000028 RID: 40 RVA: 0x0000282E File Offset: 0x00000A2E
		// (set) Token: 0x06000029 RID: 41 RVA: 0x00002836 File Offset: 0x00000A36
		public string ManagedMemberName { get; set; }

		// Token: 0x0600002A RID: 42 RVA: 0x0000283F File Offset: 0x00000A3F
		public DefineCustomEngineStructMemberData(Type type, string memberName, string managedMemberName)
		{
			this.Type = type;
			this.MemberName = memberName;
			this.ManagedMemberName = managedMemberName;
		}
	}
}
