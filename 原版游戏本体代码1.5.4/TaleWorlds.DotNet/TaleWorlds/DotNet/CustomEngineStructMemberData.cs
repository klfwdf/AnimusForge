using System;

namespace TaleWorlds.DotNet;

public class CustomEngineStructMemberData : Attribute
{
	public string CustomMemberName { get; set; }

	public bool IgnoreMemberOffsetTest { get; set; }

	public bool PublicPrivateModifierFlippedInNative { get; set; }

	public CustomEngineStructMemberData(string customMemberName)
	{
		CustomMemberName = customMemberName;
		IgnoreMemberOffsetTest = false;
		PublicPrivateModifierFlippedInNative = false;
	}

	public CustomEngineStructMemberData(string customMemberName, bool ignoreMemberOffsetTest)
	{
		CustomMemberName = customMemberName;
		IgnoreMemberOffsetTest = ignoreMemberOffsetTest;
		PublicPrivateModifierFlippedInNative = false;
	}

	public CustomEngineStructMemberData(bool publicPrivateModifierFlippedInNative)
	{
		CustomMemberName = null;
		IgnoreMemberOffsetTest = false;
		PublicPrivateModifierFlippedInNative = publicPrivateModifierFlippedInNative;
	}
}
