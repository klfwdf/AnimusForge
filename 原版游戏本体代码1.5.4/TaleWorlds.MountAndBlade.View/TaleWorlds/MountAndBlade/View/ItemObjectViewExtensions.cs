using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.View;

public static class ItemObjectViewExtensions
{
	public static MetaMesh GetCraftedMultiMesh(this ItemObject itemObject, bool needBatchedVersion)
	{
		CraftedDataView craftedDataView = CraftedDataViewManager.GetCraftedDataView(itemObject.WeaponDesign);
		if (!needBatchedVersion)
		{
			return craftedDataView?.NonBatchedWeaponMesh.CreateCopy();
		}
		return craftedDataView?.WeaponMesh.CreateCopy();
	}

	public static MetaMesh GetMultiMeshCopy(this ItemObject itemObject)
	{
		MetaMesh craftedMultiMesh = itemObject.GetCraftedMultiMesh(needBatchedVersion: true);
		if (craftedMultiMesh != null)
		{
			return craftedMultiMesh;
		}
		if (string.IsNullOrEmpty(itemObject.MultiMeshName))
		{
			return null;
		}
		return MetaMesh.GetCopy(itemObject.MultiMeshName);
	}

	public static MetaMesh GetMultiMeshCopyWithGenderData(this ItemObject itemObject, bool isFemale, bool useSlimVersion, bool needBatchedVersion)
	{
		MetaMesh craftedMultiMesh = itemObject.GetCraftedMultiMesh(needBatchedVersion);
		if (craftedMultiMesh != null)
		{
			return craftedMultiMesh;
		}
		if (string.IsNullOrEmpty(itemObject.MultiMeshName))
		{
			return null;
		}
		MetaMesh metaMesh = null;
		metaMesh = MetaMesh.GetCopy(isFemale ? (itemObject.MultiMeshName + "_female") : (itemObject.MultiMeshName + "_male"), showErrors: false, mayReturnNull: true);
		if (metaMesh != null)
		{
			return metaMesh;
		}
		string multiMeshName = itemObject.MultiMeshName;
		multiMeshName = ((!isFemale) ? (multiMeshName + (useSlimVersion ? "_slim" : "")) : (multiMeshName + (useSlimVersion ? "_converted_slim" : "_converted")));
		metaMesh = MetaMesh.GetCopy(multiMeshName, showErrors: false, mayReturnNull: true);
		if (metaMesh != null)
		{
			return metaMesh;
		}
		metaMesh = MetaMesh.GetCopy(itemObject.MultiMeshName, showErrors: true, mayReturnNull: true);
		if (metaMesh != null)
		{
			return metaMesh;
		}
		return null;
	}

	public static MatrixFrame GetScaledFrame(this ItemObject itemObject, Mat3 rotationMatrix, MetaMesh metaMesh, float scaleFactor, Vec3 positionShift)
	{
		MatrixFrame identity = MatrixFrame.Identity;
		Vec3 vec = new Vec3(1000000f, 1000000f, 1000000f);
		Vec3 vec2 = new Vec3(-1000000f, -1000000f, -1000000f);
		for (int i = 0; i != metaMesh.MeshCount; i++)
		{
			Vec3 boundingBoxMin = metaMesh.GetMeshAtIndex(i).GetBoundingBoxMin();
			Vec3 boundingBoxMax = metaMesh.GetMeshAtIndex(i).GetBoundingBoxMax();
			Vec3[] array = new Vec3[8]
			{
				rotationMatrix.TransformToParent(new Vec3(boundingBoxMin.x, boundingBoxMin.y, boundingBoxMin.z)),
				rotationMatrix.TransformToParent(new Vec3(boundingBoxMin.x, boundingBoxMin.y, boundingBoxMax.z)),
				rotationMatrix.TransformToParent(new Vec3(boundingBoxMin.x, boundingBoxMax.y, boundingBoxMin.z)),
				rotationMatrix.TransformToParent(new Vec3(boundingBoxMin.x, boundingBoxMax.y, boundingBoxMax.z)),
				rotationMatrix.TransformToParent(new Vec3(boundingBoxMax.x, boundingBoxMin.y, boundingBoxMin.z)),
				rotationMatrix.TransformToParent(new Vec3(boundingBoxMax.x, boundingBoxMin.y, boundingBoxMax.z)),
				rotationMatrix.TransformToParent(new Vec3(boundingBoxMax.x, boundingBoxMax.y, boundingBoxMin.z)),
				rotationMatrix.TransformToParent(new Vec3(boundingBoxMax.x, boundingBoxMax.y, boundingBoxMax.z))
			};
			for (int j = 0; j < 8; j++)
			{
				vec = Vec3.Vec3Min(vec, array[j]);
				vec2 = Vec3.Vec3Max(vec2, array[j]);
			}
		}
		float num = 1f;
		if (itemObject.PrimaryWeapon != null && itemObject.PrimaryWeapon.IsMeleeWeapon)
		{
			num = 0.3f + (float)itemObject.WeaponComponent.PrimaryWeapon.WeaponLength / 1.6f;
			num = MBMath.ClampFloat(num, 0.5f, 1f);
		}
		Vec3 vec3 = (vec + vec2) * 0.5f;
		float num2 = MathF.Max(vec2.x - vec.x, vec2.y - vec.y);
		float num3 = scaleFactor * num / num2;
		identity.origin -= vec3 * num3;
		identity.origin += positionShift;
		identity.rotation = rotationMatrix;
		identity.rotation.ApplyScaleLocal(num3);
		return identity;
	}
}
