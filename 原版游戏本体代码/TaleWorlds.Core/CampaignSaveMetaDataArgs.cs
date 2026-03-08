using System;
using System.Collections.Generic;

namespace TaleWorlds.Core
{
	// Token: 0x020000B5 RID: 181
	public readonly struct CampaignSaveMetaDataArgs
	{
		// Token: 0x06000964 RID: 2404 RVA: 0x0001EA82 File Offset: 0x0001CC82
		public CampaignSaveMetaDataArgs(string[] moduleName, params KeyValuePair<string, string>[] otherArgs)
		{
			this.ModuleNames = moduleName;
			this.OtherData = otherArgs;
		}

		// Token: 0x04000535 RID: 1333
		public readonly string[] ModuleNames;

		// Token: 0x04000536 RID: 1334
		public readonly KeyValuePair<string, string>[] OtherData;
	}
}
