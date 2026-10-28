using System.Collections.Generic;

namespace TaleWorlds.ObjectSystem;

public struct MbObjectXmlInformation(string id, string name, string moduleName, List<string> gameTypesIncluded)
{
	public string Id = id;

	public string Name = name;

	public string ModuleName = moduleName;

	public List<string> GameTypesIncluded = gameTypesIncluded;
}
