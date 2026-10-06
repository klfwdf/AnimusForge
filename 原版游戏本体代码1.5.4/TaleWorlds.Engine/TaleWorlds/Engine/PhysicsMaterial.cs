using TaleWorlds.DotNet;

namespace TaleWorlds.Engine;

[EngineStruct("int", false, null)]
public readonly struct PhysicsMaterial
{
	[CustomEngineStructMemberData("ignoredMember", true)]
	public readonly int Index;

	public static readonly PhysicsMaterial InvalidPhysicsMaterial = new PhysicsMaterial(-1);

	public bool IsValid => Index >= 0;

	public string Name => GetNameAtIndex(Index);

	internal PhysicsMaterial(int index)
	{
		this = default(PhysicsMaterial);
		Index = index;
	}

	public PhysicsMaterialFlags GetFlags()
	{
		return GetFlagsAtIndex(Index);
	}

	public float GetDynamicFriction()
	{
		return GetDynamicFrictionAtIndex(Index);
	}

	public float GetStaticFriction()
	{
		return GetStaticFrictionAtIndex(Index);
	}

	public float GetRestitution()
	{
		return GetRestitutionAtIndex(Index);
	}

	public float GetLinearDamping()
	{
		return GetLinearDampingAtIndex(Index);
	}

	public float GetAngularDamping()
	{
		return GetAngularDampingAtIndex(Index);
	}

	public bool Equals(PhysicsMaterial m)
	{
		return Index == m.Index;
	}

	public static int GetMaterialCount()
	{
		return EngineApplicationInterface.IPhysicsMaterial.GetMaterialCount();
	}

	public static PhysicsMaterial GetFromName(string id)
	{
		return EngineApplicationInterface.IPhysicsMaterial.GetIndexWithName(id);
	}

	public static string GetNameAtIndex(int index)
	{
		return EngineApplicationInterface.IPhysicsMaterial.GetMaterialNameAtIndex(index);
	}

	public static PhysicsMaterialFlags GetFlagsAtIndex(int index)
	{
		return EngineApplicationInterface.IPhysicsMaterial.GetFlagsAtIndex(index);
	}

	public static float GetRestitutionAtIndex(int index)
	{
		return EngineApplicationInterface.IPhysicsMaterial.GetRestitutionAtIndex(index);
	}

	public static float GetDynamicFrictionAtIndex(int index)
	{
		return EngineApplicationInterface.IPhysicsMaterial.GetDynamicFrictionAtIndex(index);
	}

	public static float GetStaticFrictionAtIndex(int index)
	{
		return EngineApplicationInterface.IPhysicsMaterial.GetStaticFrictionAtIndex(index);
	}

	public static float GetLinearDampingAtIndex(int index)
	{
		return EngineApplicationInterface.IPhysicsMaterial.GetLinearDampingAtIndex(index);
	}

	public static float GetAngularDampingAtIndex(int index)
	{
		return EngineApplicationInterface.IPhysicsMaterial.GetAngularDampingAtIndex(index);
	}

	public static PhysicsMaterial GetFromIndex(int index)
	{
		return new PhysicsMaterial(index);
	}
}
