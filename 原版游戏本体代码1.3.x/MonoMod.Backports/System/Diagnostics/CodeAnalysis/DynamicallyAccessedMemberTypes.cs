using System;

namespace System.Diagnostics.CodeAnalysis
{
	// Token: 0x02000054 RID: 84
	[Flags]
	public enum DynamicallyAccessedMemberTypes
	{
		// Token: 0x0400008E RID: 142
		None = 0,
		// Token: 0x0400008F RID: 143
		PublicParameterlessConstructor = 1,
		// Token: 0x04000090 RID: 144
		PublicConstructors = 3,
		// Token: 0x04000091 RID: 145
		NonPublicConstructors = 4,
		// Token: 0x04000092 RID: 146
		PublicMethods = 8,
		// Token: 0x04000093 RID: 147
		NonPublicMethods = 16,
		// Token: 0x04000094 RID: 148
		PublicFields = 32,
		// Token: 0x04000095 RID: 149
		NonPublicFields = 64,
		// Token: 0x04000096 RID: 150
		PublicNestedTypes = 128,
		// Token: 0x04000097 RID: 151
		NonPublicNestedTypes = 256,
		// Token: 0x04000098 RID: 152
		PublicProperties = 512,
		// Token: 0x04000099 RID: 153
		NonPublicProperties = 1024,
		// Token: 0x0400009A RID: 154
		PublicEvents = 2048,
		// Token: 0x0400009B RID: 155
		NonPublicEvents = 4096,
		// Token: 0x0400009C RID: 156
		All = -1
	}
}
