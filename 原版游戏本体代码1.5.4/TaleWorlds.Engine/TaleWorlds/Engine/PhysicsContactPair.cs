using TaleWorlds.DotNet;

namespace TaleWorlds.Engine;

[EngineStruct("rglPhysics_contact_pair", false, null)]
public struct PhysicsContactPair
{
	[CustomEngineStructMemberData("ignoredMember", true)]
	public readonly PhysicsContactInfo Contact0;

	[CustomEngineStructMemberData("ignoredMember", true)]
	public readonly PhysicsContactInfo Contact1;

	[CustomEngineStructMemberData("ignoredMember", true)]
	public readonly PhysicsContactInfo Contact2;

	[CustomEngineStructMemberData("ignoredMember", true)]
	public readonly PhysicsContactInfo Contact3;

	[CustomEngineStructMemberData("ignoredMember", true)]
	public readonly PhysicsContactInfo Contact4;

	[CustomEngineStructMemberData("ignoredMember", true)]
	public readonly PhysicsContactInfo Contact5;

	[CustomEngineStructMemberData("ignoredMember", true)]
	public readonly PhysicsContactInfo Contact6;

	[CustomEngineStructMemberData("ignoredMember", true)]
	public readonly PhysicsContactInfo Contact7;

	[CustomEngineStructMemberData("type")]
	public readonly PhysicsEventType ContactEventType;

	public readonly int NumberOfContacts;

	public PhysicsContactInfo this[int index] => index switch
	{
		0 => Contact0, 
		1 => Contact1, 
		2 => Contact2, 
		3 => Contact3, 
		4 => Contact4, 
		5 => Contact5, 
		6 => Contact6, 
		7 => Contact7, 
		_ => default(PhysicsContactInfo), 
	};
}
