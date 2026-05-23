using System;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[EngineStruct("rglBounding_box::Plain_bounding_box", false, null)]
public struct BoundingBox
{
	public struct TransformedBoundingBoxPointsContainer
	{
		public Vec3 p0;

		public Vec3 p1;

		public Vec3 p2;

		public Vec3 p3;

		public Vec3 p4;

		public Vec3 p5;

		public Vec3 p6;

		public Vec3 p7;

		public Vec3 this[int index]
		{
			get
			{
				return index switch
				{
					0 => p0, 
					1 => p1, 
					2 => p2, 
					3 => p3, 
					4 => p4, 
					5 => p5, 
					6 => p6, 
					7 => p7, 
					_ => throw new IndexOutOfRangeException($"Invalid index: {index}"), 
				};
			}
			set
			{
				switch (index)
				{
				case 0:
					p0 = value;
					break;
				case 1:
					p1 = value;
					break;
				case 2:
					p2 = value;
					break;
				case 3:
					p3 = value;
					break;
				case 4:
					p4 = value;
					break;
				case 5:
					p5 = value;
					break;
				case 6:
					p6 = value;
					break;
				case 7:
					p7 = value;
					break;
				default:
					throw new IndexOutOfRangeException($"Invalid index: {index}");
				}
			}
		}

		public (Vec3, Vec3) ComputeTransformedMinMax()
		{
			Vec3 vec = new Vec3(float.MaxValue, float.MaxValue, float.MaxValue);
			Vec3 vec2 = new Vec3(float.MinValue, float.MinValue, float.MinValue);
			for (int i = 0; i < 8; i++)
			{
				vec = Vec3.Vec3Min(vec, this[i]);
				vec2 = Vec3.Vec3Max(vec2, this[i]);
			}
			return (vec, vec2);
		}
	}

	[CustomEngineStructMemberData("box_min_")]
	public Vec3 min;

	[CustomEngineStructMemberData("box_max_")]
	public Vec3 max;

	[CustomEngineStructMemberData("box_center_")]
	public Vec3 center;

	[CustomEngineStructMemberData("radius_")]
	public float radius;

	public Vec3 this[int index] => index switch
	{
		0 => new Vec3(min.x, min.y, min.z), 
		1 => new Vec3(max.x, max.y, max.z), 
		2 => new Vec3(min.x, max.y, min.z), 
		3 => new Vec3(max.x, max.y, min.z), 
		4 => new Vec3(min.x, min.y, max.z), 
		5 => new Vec3(max.x, min.y, max.z), 
		6 => new Vec3(min.x, max.y, max.z), 
		7 => new Vec3(max.x, min.y, min.z), 
		_ => throw new IndexOutOfRangeException(), 
	};

	public BoundingBox(in Vec3 point)
	{
		min = point;
		max = point;
		center = point;
		radius = 0f;
	}

	public void RelaxMinMaxWithPoint(in Vec3 point)
	{
		min.x = TaleWorlds.Library.MathF.Min(min.x, point.x);
		min.y = TaleWorlds.Library.MathF.Min(min.y, point.y);
		min.z = TaleWorlds.Library.MathF.Min(min.z, point.z);
		max.x = TaleWorlds.Library.MathF.Max(max.x, point.x);
		max.y = TaleWorlds.Library.MathF.Max(max.y, point.y);
		max.z = TaleWorlds.Library.MathF.Max(max.z, point.z);
	}

	public void RelaxMinMaxWithPointAndRadius(in Vec3 point, float radius)
	{
		min.x = TaleWorlds.Library.MathF.Min(min.x, point.x - radius);
		min.y = TaleWorlds.Library.MathF.Min(min.y, point.y - radius);
		min.z = TaleWorlds.Library.MathF.Min(min.z, point.z - radius);
		max.x = TaleWorlds.Library.MathF.Max(max.x, point.x + radius);
		max.y = TaleWorlds.Library.MathF.Max(max.y, point.y + radius);
		max.z = TaleWorlds.Library.MathF.Max(max.z, point.z + radius);
	}

	public void RecomputeRadius()
	{
		center = 0.5f * (min + max);
		radius = (max - center).Length;
	}

	public TransformedBoundingBoxPointsContainer GetTransformedTipPointsToParent(in MatrixFrame parentFrame)
	{
		TransformedBoundingBoxPointsContainer result = default(TransformedBoundingBoxPointsContainer);
		for (int i = 0; i < 8; i++)
		{
			result[i] = parentFrame.TransformToParent(this[i]);
		}
		return result;
	}

	public TransformedBoundingBoxPointsContainer GetTransformedTipPointsToChild(in MatrixFrame childFrame)
	{
		TransformedBoundingBoxPointsContainer result = default(TransformedBoundingBoxPointsContainer);
		for (int i = 0; i < 8; i++)
		{
			result[i] = childFrame.TransformToLocal(this[i]);
		}
		return result;
	}

	public void RelaxWithBoundingBox(BoundingBox modifiedBoundingBox)
	{
		for (int i = 0; i < 8; i++)
		{
			RelaxMinMaxWithPoint(modifiedBoundingBox[i]);
		}
	}

	public void RelaxWithArbitraryBoundingBox(BoundingBox otherBoundingBox, MatrixFrame otherGlobalFrame, MatrixFrame globalFrameOfThisBoundingBox)
	{
		TransformedBoundingBoxPointsContainer transformedTipPointsToParent = otherBoundingBox.GetTransformedTipPointsToParent(in otherGlobalFrame);
		for (int i = 0; i < 8; i++)
		{
			RelaxMinMaxWithPoint(globalFrameOfThisBoundingBox.TransformToLocal(transformedTipPointsToParent[i]));
		}
	}

	public void RelaxWithChildBoundingBox(BoundingBox childBoundingBox, MatrixFrame childFrame)
	{
		TransformedBoundingBoxPointsContainer transformedTipPointsToParent = childBoundingBox.GetTransformedTipPointsToParent(in childFrame);
		for (int i = 0; i < 8; i++)
		{
			RelaxMinMaxWithPoint(transformedTipPointsToParent[i]);
		}
	}

	public void BeginRelaxation()
	{
		min = new Vec3(100000000f, 100000000f, 100000000f);
		max = new Vec3(-100000000f, -100000000f, -100000000f);
		radius = 0f;
		center = new Vec3(0f, 0f, 0f, -1f);
	}

	private static bool ModifyPlane(ref float plane, float otherPlane, float modifyAmount, float changeTolerance, bool isMin)
	{
		bool result = false;
		if (isMin)
		{
			if (otherPlane < plane)
			{
				plane = otherPlane - modifyAmount;
				result = true;
			}
			else if (otherPlane - plane >= modifyAmount * 2.5f)
			{
				plane = otherPlane - changeTolerance;
				result = true;
			}
		}
		else if (otherPlane > plane)
		{
			plane = otherPlane + modifyAmount;
			result = true;
		}
		else if (plane - otherPlane >= modifyAmount * 2.5f)
		{
			plane = otherPlane + changeTolerance;
			result = true;
		}
		return result;
	}

	public static bool ArrangeWithAnotherBoundingBox(ref BoundingBox boundingBox, BoundingBox otherBoundingBox, float changeAmount)
	{
		bool flag = false;
		float changeTolerance = changeAmount * 0.25f;
		flag = ModifyPlane(ref boundingBox.min.x, otherBoundingBox.min.x, changeAmount, changeTolerance, isMin: true) || flag;
		flag = ModifyPlane(ref boundingBox.max.x, otherBoundingBox.max.x, changeAmount, changeTolerance, isMin: false) || flag;
		flag = ModifyPlane(ref boundingBox.min.y, otherBoundingBox.min.y, changeAmount, changeTolerance, isMin: true) || flag;
		flag = ModifyPlane(ref boundingBox.max.y, otherBoundingBox.max.y, changeAmount, changeTolerance, isMin: false) || flag;
		flag = ModifyPlane(ref boundingBox.min.z, otherBoundingBox.min.z, changeAmount, changeTolerance, isMin: true) || flag;
		flag = ModifyPlane(ref boundingBox.max.z, otherBoundingBox.max.z, changeAmount, changeTolerance, isMin: false) || flag;
		if (flag)
		{
			boundingBox.RecomputeRadius();
		}
		return flag;
	}

	public bool PointInsideBox(Vec3 point, float epsilon)
	{
		if (point.x - epsilon <= max.x && point.x + epsilon >= min.x && point.y - epsilon <= max.y && point.y + epsilon >= min.y)
		{
			if (point.z - epsilon <= max.z)
			{
				return point.z + epsilon >= min.z;
			}
			return false;
		}
		return false;
	}

	public static float GetLongestHalfDimensionOfBoundingBox(BoundingBox boundingBox)
	{
		Vec3 vec = boundingBox.max - boundingBox.center;
		Vec3 vec2 = boundingBox.center - boundingBox.min;
		return TaleWorlds.Library.MathF.Max(TaleWorlds.Library.MathF.Max(vec.x, vec.y, vec.z), TaleWorlds.Library.MathF.Max(vec2.x, vec2.y, vec2.z));
	}

	public void RenderBoundingBox()
	{
	}
}
