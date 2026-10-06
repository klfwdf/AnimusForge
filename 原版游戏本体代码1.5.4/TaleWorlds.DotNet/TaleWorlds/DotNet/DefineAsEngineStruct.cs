using System;

namespace TaleWorlds.DotNet;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class DefineAsEngineStruct : Attribute
{
	public Type Type { get; set; }

	public string EngineType { get; set; }

	public bool IgnoreMemberOffsetTest { get; set; }

	public string EngineEnumPrefix { get; set; }

	public string[] Conditionals { get; set; }

	public DefineAsEngineStruct(Type type, string engineType, bool ignoreMemberOffsetTest = false, string engineEnumPrefix = null, string[] conditionals = null)
	{
		Type = type;
		EngineType = engineType;
		IgnoreMemberOffsetTest = ignoreMemberOffsetTest;
		EngineEnumPrefix = engineEnumPrefix;
		Conditionals = conditionals;
	}
}
