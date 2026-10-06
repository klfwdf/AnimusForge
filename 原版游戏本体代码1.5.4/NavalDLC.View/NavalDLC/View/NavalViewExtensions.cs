using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace NavalDLC.View;

public static class NavalViewExtensions
{
	public static BoundingBox GetBoundingBoxIncludingChildren(this GameEntity entity)
	{
		BoundingBox boundingBox = default(BoundingBox);
		GetBoundingBoxIncludingChildrenAux(entity, ref boundingBox);
		boundingBox.RecomputeRadius();
		return boundingBox;
	}

	private static void GetBoundingBoxIncludingChildrenAux(GameEntity entity, ref BoundingBox boundingBox)
	{
		int componentCount = entity.GetComponentCount(GameEntity.ComponentType.MetaMesh);
		for (int i = 0; i < componentCount; i++)
		{
			MetaMesh metaMesh = entity.GetMetaMesh(i);
			if (metaMesh != null)
			{
				BoundingBox boundingBox2 = metaMesh.GetBoundingBox();
				boundingBox.RelaxMinMaxWithPoint(in boundingBox2.min);
				boundingBox.RelaxMinMaxWithPoint(in boundingBox2.max);
			}
		}
		Mesh firstMesh = entity.GetFirstMesh();
		if (firstMesh != null)
		{
			boundingBox.RelaxMinMaxWithPoint(firstMesh.GetBoundingBoxMin());
			boundingBox.RelaxMinMaxWithPoint(firstMesh.GetBoundingBoxMax());
		}
		for (int j = 0; j < entity.ChildCount; j++)
		{
			GetBoundingBoxIncludingChildrenAux(entity.GetChild(j), ref boundingBox);
		}
	}

	public static void FitEntityInsideView(this Camera camera, Vec3 normalizedCameraOffset, GameEntity entity)
	{
		entity.RecomputeBoundingBox();
		float boundingBoxRadius = entity.GetBoundingBoxRadius();
		Vec3 vec = entity.GetFrame().origin + (entity.GetBoundingBoxMin() + entity.GetBoundingBoxMax()) * 0.5f;
		float num = boundingBoxRadius / MathF.Abs(MathF.Sin(camera.HorizontalFov * 0.5f));
		Vec3 position = vec + normalizedCameraOffset * num;
		camera.LookAt(position, vec, Vec3.Up);
	}
}
