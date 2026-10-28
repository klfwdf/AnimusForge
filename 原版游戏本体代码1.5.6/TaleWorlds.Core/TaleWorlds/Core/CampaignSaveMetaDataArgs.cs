using System.Collections.Generic;

namespace TaleWorlds.Core;

public readonly struct CampaignSaveMetaDataArgs(string[] moduleName, params KeyValuePair<string, string>[] otherArgs)
{
	public readonly string[] ModuleNames = moduleName;

	public readonly KeyValuePair<string, string>[] OtherData = otherArgs;
}
