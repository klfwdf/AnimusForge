using System;

namespace TaleWorlds.DotNet;

public class EngineStruct : Attribute
{
	public string EngineType { get; set; }

	public string AlternateDotNetType { get; set; }

	public string EngineEnumPrefix { get; set; }

	public bool IgnoreMemberOffsetTest { get; set; }

	public string[] Conditionals { get; set; }

	public bool FirstCharacterUppercase { get; set; } = true;

	public EngineStruct(string engineType, bool ignoreMemberOffsetTest = false, string[] conditionals = null)
	{
		EngineType = engineType;
		AlternateDotNetType = null;
		EngineEnumPrefix = null;
		IgnoreMemberOffsetTest = ignoreMemberOffsetTest;
		Conditionals = conditionals;
	}

	public EngineStruct(string engineType, string alternateDotNetType, bool ignoreMemberOffsetTest = false, string[] conditionals = null)
	{
		EngineType = engineType;
		AlternateDotNetType = alternateDotNetType;
		EngineEnumPrefix = null;
		IgnoreMemberOffsetTest = ignoreMemberOffsetTest;
		Conditionals = conditionals;
	}

	public EngineStruct(string engineType, bool isEnum, string engineEnumPrefix, bool ignoreMemberOffsetTest = false)
	{
		EngineType = engineType;
		AlternateDotNetType = null;
		EngineEnumPrefix = engineEnumPrefix;
		IgnoreMemberOffsetTest = ignoreMemberOffsetTest;
	}
}
