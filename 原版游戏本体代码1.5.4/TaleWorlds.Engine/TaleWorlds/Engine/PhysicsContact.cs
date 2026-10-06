using System;
using TaleWorlds.DotNet;

namespace TaleWorlds.Engine;

[EngineStruct("rglPhysics_contact", false, null)]
public struct PhysicsContact
{
	[CustomEngineStructMemberData("ignoredMember", true)]
	public readonly PhysicsContactPair ContactPair0;

	[CustomEngineStructMemberData("ignoredMember", true)]
	public readonly PhysicsContactPair ContactPair1;

	[CustomEngineStructMemberData("ignoredMember", true)]
	public readonly PhysicsContactPair ContactPair2;

	[CustomEngineStructMemberData("ignoredMember", true)]
	public readonly PhysicsContactPair ContactPair3;

	[CustomEngineStructMemberData("ignoredMember", true)]
	public readonly PhysicsContactPair ContactPair4;

	[CustomEngineStructMemberData("ignoredMember", true)]
	public readonly PhysicsContactPair ContactPair5;

	[CustomEngineStructMemberData("ignoredMember", true)]
	public readonly PhysicsContactPair ContactPair6;

	[CustomEngineStructMemberData("ignoredMember", true)]
	public readonly PhysicsContactPair ContactPair7;

	[CustomEngineStructMemberData("ignoredMember", true)]
	public readonly PhysicsContactPair ContactPair8;

	[CustomEngineStructMemberData("ignoredMember", true)]
	public readonly PhysicsContactPair ContactPair9;

	[CustomEngineStructMemberData("ignoredMember", true)]
	public readonly PhysicsContactPair ContactPair10;

	[CustomEngineStructMemberData("ignoredMember", true)]
	public readonly PhysicsContactPair ContactPair11;

	[CustomEngineStructMemberData("ignoredMember", true)]
	public readonly PhysicsContactPair ContactPair12;

	[CustomEngineStructMemberData("ignoredMember", true)]
	public readonly PhysicsContactPair ContactPair13;

	[CustomEngineStructMemberData("ignoredMember", true)]
	public readonly PhysicsContactPair ContactPair14;

	[CustomEngineStructMemberData("ignoredMember", true)]
	public readonly PhysicsContactPair ContactPair15;

	public readonly IntPtr body0;

	public readonly IntPtr body1;

	[CustomEngineStructMemberData("body_0_flags")]
	public readonly BodyFlags body0Flags;

	[CustomEngineStructMemberData("body_1_flags")]
	public readonly BodyFlags body1Flags;

	public readonly int NumberOfContactPairs;

	public PhysicsContactPair this[int index] => index switch
	{
		0 => ContactPair0, 
		1 => ContactPair1, 
		2 => ContactPair2, 
		3 => ContactPair3, 
		4 => ContactPair4, 
		5 => ContactPair5, 
		6 => ContactPair6, 
		7 => ContactPair7, 
		8 => ContactPair8, 
		9 => ContactPair9, 
		10 => ContactPair10, 
		11 => ContactPair11, 
		12 => ContactPair12, 
		13 => ContactPair13, 
		14 => ContactPair14, 
		15 => ContactPair15, 
		_ => default(PhysicsContactPair), 
	};
}
