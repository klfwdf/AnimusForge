using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000974 RID: 2420
	[Flags]
	public enum RegistrationClassContext
	{
		// Token: 0x04002BBC RID: 11196
		InProcessServer = 1,
		// Token: 0x04002BBD RID: 11197
		InProcessHandler = 2,
		// Token: 0x04002BBE RID: 11198
		LocalServer = 4,
		// Token: 0x04002BBF RID: 11199
		InProcessServer16 = 8,
		// Token: 0x04002BC0 RID: 11200
		RemoteServer = 16,
		// Token: 0x04002BC1 RID: 11201
		InProcessHandler16 = 32,
		// Token: 0x04002BC2 RID: 11202
		Reserved1 = 64,
		// Token: 0x04002BC3 RID: 11203
		Reserved2 = 128,
		// Token: 0x04002BC4 RID: 11204
		Reserved3 = 256,
		// Token: 0x04002BC5 RID: 11205
		Reserved4 = 512,
		// Token: 0x04002BC6 RID: 11206
		NoCodeDownload = 1024,
		// Token: 0x04002BC7 RID: 11207
		Reserved5 = 2048,
		// Token: 0x04002BC8 RID: 11208
		NoCustomMarshal = 4096,
		// Token: 0x04002BC9 RID: 11209
		EnableCodeDownload = 8192,
		// Token: 0x04002BCA RID: 11210
		NoFailureLog = 16384,
		// Token: 0x04002BCB RID: 11211
		DisableActivateAsActivator = 32768,
		// Token: 0x04002BCC RID: 11212
		EnableActivateAsActivator = 65536,
		// Token: 0x04002BCD RID: 11213
		FromDefaultContext = 131072
	}
}
