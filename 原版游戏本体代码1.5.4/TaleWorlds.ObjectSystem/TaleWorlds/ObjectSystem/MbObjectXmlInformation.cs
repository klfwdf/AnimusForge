using System.Collections.Generic;

namespace TaleWorlds.ObjectSystem;

public struct MbObjectXmlInformation
{
	public string Id;

	public string Name;

	public string ModuleName;

	public List<string> GameTypesIncluded;

	public MbObjectXmlInformation(string id, string name, string moduleName, List<string> gameTypesIncluded)
	{
		Id = id;
		Name = name;
		ModuleName = moduleName;
		GameTypesIncluded = gameTypesIncluded;
	}
}
