using System;
using Mono.Collections.Generic;

namespace Mono.Cecil.Cil
{
	// Token: 0x02000314 RID: 788
	internal interface ICustomDebugInformationProvider : IMetadataTokenProvider
	{
		// Token: 0x17000539 RID: 1337
		// (get) Token: 0x0600147C RID: 5244
		bool HasCustomDebugInformations { get; }

		// Token: 0x1700053A RID: 1338
		// (get) Token: 0x0600147D RID: 5245
		Collection<CustomDebugInformation> CustomDebugInformations { get; }
	}
}
