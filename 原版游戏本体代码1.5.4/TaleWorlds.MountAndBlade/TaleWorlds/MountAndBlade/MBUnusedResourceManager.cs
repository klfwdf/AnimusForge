namespace TaleWorlds.MountAndBlade;

public class MBUnusedResourceManager
{
	public static void SetMeshUsed(string meshName)
	{
		MBAPI.IMBWorld.SetMeshUsed(meshName);
	}

	public static void SetMaterialUsed(string meshName)
	{
		MBAPI.IMBWorld.SetMaterialUsed(meshName);
	}

	public static void SetBodyUsed(string bodyName)
	{
		MBAPI.IMBWorld.SetBodyUsed(bodyName);
	}
}
