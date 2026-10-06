using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class BoundaryWallView : ScriptComponentBehavior
{
	private List<Vec2> _lastPoints = new List<Vec2>();

	private List<Vec2> _lastAttackerPoints = new List<Vec2>();

	private List<Vec2> _lastDefenderPoints = new List<Vec2>();

	private float timer;

	protected internal override void OnInit()
	{
		throw new Exception("This should only be used in editor.");
	}

	protected internal override void OnEditorTick(float dt)
	{
		base.OnEditorTick(dt);
		timer += dt;
		if (!MBEditor.BorderHelpersEnabled() || timer < 0.2f)
		{
			return;
		}
		timer = 0f;
		if (base.Scene == null)
		{
			return;
		}
		bool flag = CalculateBoundaries("walk_area_vertex", ref _lastPoints);
		bool flag2 = CalculateBoundaries("defender_area_vertex", ref _lastDefenderPoints);
		bool flag3 = CalculateBoundaries("attacker_area_vertex", ref _lastAttackerPoints);
		if (_lastPoints.Count >= 3 || _lastDefenderPoints.Count >= 3 || _lastAttackerPoints.Count >= 3)
		{
			if (flag || flag2 || flag3)
			{
				base.GameEntity.ClearEntityComponents(resetAll: true, removeScripts: false, deleteChildEntities: true);
				base.GameEntity.SetName("editor_map_border");
				Mesh mesh = CreateBoundaryMesh(base.Scene, _lastPoints);
				if (mesh != null)
				{
					base.GameEntity.AddMesh(mesh);
				}
				Color color = new Color(0f, 0f, 0.8f);
				Mesh mesh2 = CreateBoundaryMesh(base.Scene, _lastDefenderPoints, color.ToUnsignedInteger());
				if (mesh2 != null)
				{
					base.GameEntity.AddMesh(mesh2);
				}
				Color color2 = new Color(0f, 0.8f, 0.8f);
				Mesh mesh3 = CreateBoundaryMesh(base.Scene, _lastAttackerPoints, color2.ToUnsignedInteger());
				if (mesh3 != null)
				{
					base.GameEntity.AddMesh(mesh3);
				}
			}
		}
		else
		{
			base.GameEntity.ClearEntityComponents(resetAll: true, removeScripts: false, deleteChildEntities: true);
		}
	}

	private bool CalculateBoundaries(string vertexTag, ref List<Vec2> lastPoints)
	{
		IEnumerable<WeakGameEntity> source = base.Scene.FindWeakEntitiesWithTag(vertexTag);
		source = source.Where((WeakGameEntity e) => !e.EntityFlags.HasAnyFlag(EntityFlags.DontSaveToScene));
		int num = source.Count();
		bool flag = false;
		if (num >= 3)
		{
			List<Vec2> source2 = source.Select((WeakGameEntity e) => e.GlobalPosition.AsVec2).ToList();
			Vec2 mid = new Vec2(source2.Average((Vec2 p) => p.x), source2.Average((Vec2 p) => p.y));
			source2 = source2.OrderBy((Vec2 p) => TaleWorlds.Library.MathF.Atan2(p.x - mid.x, p.y - mid.y)).ToList();
			if (lastPoints != null && lastPoints.Count == source2.Count)
			{
				flag = true;
				for (int num2 = 0; num2 < source2.Count; num2++)
				{
					if (source2[num2] != lastPoints[num2])
					{
						flag = false;
						break;
					}
				}
			}
			lastPoints = source2;
			return !flag;
		}
		return false;
	}

	public static Mesh CreateBoundaryMesh(Scene scene, ICollection<Vec2> boundaryPoints, uint meshColor = 536918784u)
	{
		if (boundaryPoints == null || boundaryPoints.Count < 3)
		{
			return null;
		}
		Mesh mesh = Mesh.CreateMesh();
		UIntPtr uIntPtr = mesh.LockEditDataWrite();
		scene.GetBoundingBox(out var min, out var max);
		max.z += 50f;
		min.z -= 50f;
		for (int i = 0; i < boundaryPoints.Count; i++)
		{
			Vec2 point = boundaryPoints.ElementAt(i);
			Vec2 point2 = boundaryPoints.ElementAt((i + 1) % boundaryPoints.Count);
			float height = 0f;
			float height2 = 0f;
			if (!scene.IsAtmosphereIndoor)
			{
				if (!scene.GetHeightAtPoint(point, BodyFlags.CommonCollisionExcludeFlagsForCombat, ref height))
				{
					MBDebug.ShowWarning("GetHeightAtPoint failed at CreateBoundaryEntity!");
					return null;
				}
				if (!scene.GetHeightAtPoint(point2, BodyFlags.CommonCollisionExcludeFlagsForCombat, ref height2))
				{
					MBDebug.ShowWarning("GetHeightAtPoint failed at CreateBoundaryEntity!");
					return null;
				}
			}
			else
			{
				height = min.z;
				height2 = min.z;
			}
			Vec3 vec = point.ToVec3(height);
			Vec3 vec2 = point2.ToVec3(height2);
			Vec3 vec3 = Vec3.Up * 2f;
			Vec3 p = vec;
			Vec3 p2 = vec2;
			Vec3 vec4 = vec;
			Vec3 p3 = vec2;
			p.z = TaleWorlds.Library.MathF.Min(p.z, min.z);
			p2.z = TaleWorlds.Library.MathF.Min(p2.z, min.z);
			vec4.z = TaleWorlds.Library.MathF.Max(vec4.z, max.z);
			p3.z = TaleWorlds.Library.MathF.Max(p3.z, max.z);
			p -= vec3;
			p2 -= vec3;
			vec4 += vec3;
			p3 += vec3;
			mesh.AddTriangle(p, p2, vec4, Vec2.Zero, Vec2.Side, Vec2.Forward, meshColor, uIntPtr);
			mesh.AddTriangle(vec4, p2, p3, Vec2.Forward, Vec2.Side, Vec2.One, meshColor, uIntPtr);
		}
		mesh.SetMaterial("editor_map_border");
		mesh.VisibilityMask = VisibilityMaskFlags.Final | VisibilityMaskFlags.EditModeBorders;
		mesh.SetColorAlpha(150u);
		mesh.SetMeshRenderOrder(250);
		mesh.CullingMode = MBMeshCullingMode.None;
		float vectorArgument = 25f;
		if (MBEditor.IsEditModeOn && scene.IsEditorScene())
		{
			vectorArgument = 100000f;
		}
		IEnumerable<WeakGameEntity> source = scene.FindWeakEntitiesWithTag("walk_area_vertex");
		float vectorArgument2 = (source.Any() ? source.Average((WeakGameEntity ent) => ent.GlobalPosition.z) : 0f);
		mesh.SetVectorArgument(vectorArgument, vectorArgument2, 0f, 0f);
		mesh.ComputeNormals();
		mesh.ComputeTangents();
		mesh.RecomputeBoundingBox();
		mesh.UnlockEditDataWrite(uIntPtr);
		return mesh;
	}
}
