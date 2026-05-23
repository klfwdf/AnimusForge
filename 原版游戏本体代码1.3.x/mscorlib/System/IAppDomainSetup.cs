using System;
using System.Runtime.InteropServices;

namespace System
{
	// Token: 0x0200009F RID: 159
	[Guid("27FFF232-A7A8-40dd-8D4A-734AD59FCD41")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComVisible(true)]
	public interface IAppDomainSetup
	{
		// Token: 0x17000125 RID: 293
		// (get) Token: 0x0600094E RID: 2382
		// (set) Token: 0x0600094F RID: 2383
		string ApplicationBase { get; set; }

		// Token: 0x17000126 RID: 294
		// (get) Token: 0x06000950 RID: 2384
		// (set) Token: 0x06000951 RID: 2385
		string ApplicationName { get; set; }

		// Token: 0x17000127 RID: 295
		// (get) Token: 0x06000952 RID: 2386
		// (set) Token: 0x06000953 RID: 2387
		string CachePath { get; set; }

		// Token: 0x17000128 RID: 296
		// (get) Token: 0x06000954 RID: 2388
		// (set) Token: 0x06000955 RID: 2389
		string ConfigurationFile { get; set; }

		// Token: 0x17000129 RID: 297
		// (get) Token: 0x06000956 RID: 2390
		// (set) Token: 0x06000957 RID: 2391
		string DynamicBase { get; set; }

		// Token: 0x1700012A RID: 298
		// (get) Token: 0x06000958 RID: 2392
		// (set) Token: 0x06000959 RID: 2393
		string LicenseFile { get; set; }

		// Token: 0x1700012B RID: 299
		// (get) Token: 0x0600095A RID: 2394
		// (set) Token: 0x0600095B RID: 2395
		string PrivateBinPath { get; set; }

		// Token: 0x1700012C RID: 300
		// (get) Token: 0x0600095C RID: 2396
		// (set) Token: 0x0600095D RID: 2397
		string PrivateBinPathProbe { get; set; }

		// Token: 0x1700012D RID: 301
		// (get) Token: 0x0600095E RID: 2398
		// (set) Token: 0x0600095F RID: 2399
		string ShadowCopyDirectories { get; set; }

		// Token: 0x1700012E RID: 302
		// (get) Token: 0x06000960 RID: 2400
		// (set) Token: 0x06000961 RID: 2401
		string ShadowCopyFiles { get; set; }
	}
}
