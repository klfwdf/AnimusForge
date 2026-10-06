using System.Collections.Generic;

namespace TaleWorlds.Core;

public readonly struct CampaignSaveMetaDataArgs
{
	public readonly string[] ModuleNames;

	public readonly KeyValuePair<string, string>[] OtherData;

	public CampaignSaveMetaDataArgs(string[] moduleName, params KeyValuePair<string, string>[] otherArgs)
	{
		ModuleNames = moduleName;
		OtherData = otherArgs;
	}
}
